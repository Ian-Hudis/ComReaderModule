using ComReaderModule;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using static ComReaderModule.USBSniff;

namespace ComReaderModule
{
    public static class DataInterpreter
    {

        // the object for putting variables into the json file
        public static UpdateJSon Updatejson = new();

        // Keep a short rolling buffer of recent raw logs for the Debuglog array (last 10 entries)
        private static readonly Queue<string> _recentLogs = new();
        private const int MaxDebugLogs = 10;
        private static readonly object _recentLogsLock = new();

        // Message reassembly buffer for fragmented USB data
        private static readonly StringBuilder _messageBuffer = new();
        private static readonly object _bufferLock = new();

        public static void ResetState()
        {
            lock (_recentLogsLock)
            {
                _recentLogs.Clear();
            }

            lock (_bufferLock)
            {
                _messageBuffer.Clear();
            }

            // A fresh instance also clears the change-detection cache held by UpdateJSon.
            Updatejson = new UpdateJSon();
        }

        public static void DataExtraction(string extractedText, DataDirection direction)
        {
            if (string.IsNullOrWhiteSpace(extractedText))
                return;

            // Only process messages that are device -> PC responses
            if (direction != DataDirection.MachineToPc)
            {
                // Discard PC->Machine fragments; they're not data
                return;
            }

            // Accumulate the incoming fragment into the buffer
            lock (_bufferLock)
            {
                _messageBuffer.Append(extractedText);
                Console.WriteLine($"[Buffer] Added: '{extractedText}' | Buffer now: '{_messageBuffer}'");

                // Try to extract and process complete messages from the buffer
                ProcessCompleteMessages();
            }
        }

        private static void ProcessCompleteMessages()
        {
            string buffer = _messageBuffer.ToString();
            bool foundAny = false;

            while (true)
            {
                // 1. Discard garbage before marker: look for # or ! followed by 9921
                int markerPos = buffer.IndexOf("#9921", StringComparison.OrdinalIgnoreCase);
                if (markerPos < 0)
                {
                    markerPos = buffer.IndexOf("!9921", StringComparison.OrdinalIgnoreCase);
                }

                if (markerPos < 0)
                {
                    // No marker found; if buffer is large, truncate to keep only trailing bytes
                    // (in case garbage is accumulating faster than new markers arrive)
                    if (buffer.Length > 300)
                    {
                        Console.WriteLine($"[Buffer] Discarding garbage ({buffer.Length} chars without marker), keeping last 100");
                        buffer = buffer[^100..];
                    }
                    else if (buffer.Length > 0)
                    {
                        Console.WriteLine($"[Buffer] No marker found yet, buffer={buffer.Length} chars");
                    }

                    _messageBuffer.Clear();
                    _messageBuffer.Append(buffer);
                    break;
                }

                // 2. Discard everything BEFORE the marker
                if (markerPos > 0)
                {
                    Console.WriteLine($"[Buffer] Discarding {markerPos} chars of garbage before marker");
                    buffer = buffer[markerPos..];
                }

                // 3. Look for next marker or end of message
                int nextMarker = buffer.IndexOf("#9921", 1, StringComparison.OrdinalIgnoreCase);
                if (nextMarker < 0)
                {
                    nextMarker = buffer.IndexOf("!9921", 1, StringComparison.OrdinalIgnoreCase);
                }

                // Known message format: #9921 + 2 cmd + X-axis(14 chars) + Y-axis(14 chars) [+ Z(8)]
                // Minimum: 5 + 2 + 14 + 14 = 35 chars for complete X and Y data
                // If we haven't seen 35 chars yet, wait for more
                if (buffer.Length < 35)
                {
                    Console.WriteLine($"[Buffer] Incomplete (only {buffer.Length} chars, need 35 for X+Y), waiting for more...");
                    _messageBuffer.Clear();
                    _messageBuffer.Append(buffer);
                    break;
                }

                // Decide message end: either next marker or buffer end
                int messageEnd = nextMarker > 0 ? nextMarker : buffer.Length;

                // Extract message (keep the marker this time, it's needed for parsing)
                string completeMessage = buffer[0..messageEnd].Trim();

                // Extra validation: check that we have mostly hex-valid characters
                // Allow: #, !, 0-9, A-F, a-f only (plus the protocol marker)
                if (IsValidHexMessage(completeMessage))
                {
                    Console.WriteLine($"[Buffer] Extracted complete message ({completeMessage.Length} chars): '{completeMessage}'");
                    ProcessMessage(completeMessage);
                    foundAny = true;
                }
                else
                {
                    Console.WriteLine($"[Buffer] Skipping invalid message (contains non-hex chars): '{completeMessage}'");
                }

                // Remove this message from buffer and continue
                buffer = buffer[messageEnd..];
                _messageBuffer.Clear();
                _messageBuffer.Append(buffer);

                if (buffer.Length == 0)
                {
                    Console.WriteLine($"[Buffer] Buffer cleared, ready for new messages");
                    break;
                }
            }

            if (foundAny)
            {
                lock (_recentLogsLock)
                {
                    Updatejson.ControlData.Debuglog = new List<string>(_recentLogs);
                }
                Updatejson.UpdateJsonData(Updatejson.ControlData);
            }
        }

        private static bool IsValidHexMessage(string msg)
        {
            if (string.IsNullOrWhiteSpace(msg))
                return false;

            // Check each character: allow marker, hex digits
            foreach (char c in msg)
            {
                // Allow: # ! 0-9 A-F a-f
                if (c != '#' && c != '!' && 
                    !char.IsDigit(c) && 
                    !(c >= 'A' && c <= 'F') && 
                    !(c >= 'a' && c <= 'f'))
                {
                    return false;
                }
            }

            return true;
        }

        private static void ProcessMessage(string completeMessage)
        {
            if (string.IsNullOrWhiteSpace(completeMessage))
                return;

            // Record in debug log
            lock (_recentLogsLock)
            {
                if (_recentLogs.Count >= MaxDebugLogs)
                    _recentLogs.Dequeue();
                _recentLogs.Enqueue(completeMessage);
            }

            // Parse data for messages with the marker
            string text = Filter_hex(completeMessage);
            Console.WriteLine($"[Process] After filter: '{text}'");

            (double X, double Y, double Z) position = Grabdata(text, Updatejson.ControlData.Variable1, Updatejson.ControlData.Variable2, Updatejson.ControlData.Variable3);

            // Put the data back into the package
            if (position.X > 1 && position.X < 60 && Updatejson.ControlData.Variable1 > 60)
            {
                Updatejson.ControlData.PartCount++;
            }

            Updatejson.ControlData.Variable1 = position.X;
            Updatejson.ControlData.Variable2 = position.Y;
            Updatejson.ControlData.Variable3 = position.Z;

            Console.WriteLine($"[Process] Parsed -> X={position.X}, Y={position.Y}, Z={position.Z}");

            if (Updatejson?.ControlData.Variable1 > 50)
            {
                Updatejson.ControlData.MachineStatus = "RUNNING";
            }
            else
            {
                Updatejson.ControlData.MachineStatus = "IDLE";
            }
        }
       

        private static string Filter_hex(string data)
        {
            if (data == null)
            {
                return string.Empty;
            }

            // Match the Excel Filter.txt logic: only remove o, O, P, p characters
            return string.Concat(data.Where(c => c != 'o' && c != 'O' && c != 'p' && c != 'P'));
        }
        

        private static (double X, double Y, double Z) Grabdata(string Data, double PreviousX, double PreviousY, double PreviousZ)
        {
            if (string.IsNullOrWhiteSpace(Data))
            {
                return (PreviousX, PreviousY, PreviousZ);
            }

            try
            {
                double x = PreviousX;
                double y = PreviousY;
                double z = PreviousZ;

                // 1) X and Y are extracted relative to the "992140" marker per Excel logic
                int idx992140 = Data.IndexOf("992140", StringComparison.OrdinalIgnoreCase);
                if (idx992140 >= 0)
                {
                    string cleanHex = Data[(idx992140 + "992140".Length)..];
                    Console.WriteLine($"[Parse] Found '992140' marker. cleanHex='{cleanHex}'");

                    // Excel: X = MID(clean_hex, 2, 14) -> 1-based index 2 => zero-based 1
                    if (cleanHex.Length >= 1 + 14)
                    {
                        string axisX = cleanHex.Substring(1, 14);
                        Console.WriteLine($"[Parse] axisX (raw 14)='{axisX}'");
                        x = DecodeLittleEndian(axisX, PreviousX);
                        Console.WriteLine($"[Parse] Decoded X={x}");
                    }

                    // Excel: Y = MID(clean_hex, 16, 14) -> zero-based 15
                    if (cleanHex.Length >= 15 + 14)
                    {
                        string axisY = cleanHex.Substring(15, 14);
                        Console.WriteLine($"[Parse] axisY (raw 14)='{axisY}'");
                        y = DecodeLittleEndian(axisY, PreviousY);
                        Console.WriteLine($"[Parse] Decoded Y={y}");
                    }
                }

                // 2) Z is extracted relative to the '#9921' marker per Excel logic
                int idxHash = Data.IndexOf("#9921", StringComparison.OrdinalIgnoreCase);
                if (idxHash >= 0)
                {
                    string cleanHexHash = Data[(idxHash + "#9921".Length)..];
                    Console.WriteLine($"[Parse] Found '#9921' marker. cleanHexHash='{cleanHexHash}'");

                    // Excel: Z = MID(clean_hex, 25, 8) -> zero-based 24
                    if (cleanHexHash.Length >= 24 + 8)
                    {
                        string axisZ = cleanHexHash.Substring(24, 8);
                        Console.WriteLine($"[Parse] axisZ (raw 8)='{axisZ}'");
                        z = DecodeLittleEndian(axisZ, PreviousZ);
                        Console.WriteLine($"[Parse] Decoded Z={z}");
                    }
                }

                return (x, y, z);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ParseError] {ex.Message}");
                return (PreviousX, PreviousY, PreviousZ);
            }
        }

        private static int Findaxis(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
                return 0;

            int marker = data.IndexOf("#9921", StringComparison.OrdinalIgnoreCase);

            if (marker < 0)
                return 0;

            // After #9921 (5 chars) and command byte (2 chars), calculate available data
            string cleanHex = data[(marker + 7)..];;

            // Each full axis is 14 hex chars
            // Partial Z is 8 hex chars
            int fullAxes = cleanHex.Length / 14;
            return Math.Min(fullAxes, 3); // Cap at 3 axes
        }

        private static double DecodeLittleEndian(string section, double previousValue)
        {
            if (string.IsNullOrWhiteSpace(section) || section.Length < 8)
                return previousValue;

            // Per Excel: use RIGHT(section,8) - take the last 8 characters
            string raw = section.Length > 8 ? section[^8..] : section;

            if (raw.Length < 8)
                return previousValue;

            // Byte-swap: [6:8] + [4:6] + [2:4] + [0:2]
            string swapped =
                raw.Substring(6, 2) +
                raw.Substring(4, 2) +
                raw.Substring(2, 2) +
                raw.Substring(0, 2);

            try
            {
                uint unsignedValue = uint.Parse(swapped, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                int signedValue = unchecked((int)unsignedValue);
                double result = signedValue / 512.0;

                return Math.Abs(result) > 1000 ? previousValue : result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DecodeError] section='{section}' err={ex.Message}");
                return previousValue;
            }
        }
                   
    }
}

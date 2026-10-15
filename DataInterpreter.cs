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

        public static void ResetState()
        {
            lock (_recentLogsLock)
            {
                _recentLogs.Clear();
            }

            // A fresh instance also clears the change-detection cache held by UpdateJSon.
            Updatejson = new UpdateJSon();
        }

        public static void DataExtraction(string extractedText, DataDirection direction)
        {
            if (string.IsNullOrWhiteSpace(extractedText))
                return;

            // Always record the raw message into the recent logs buffer (used for Debuglog[] in UIData.json)
            lock (_recentLogsLock)
            {
                if (_recentLogs.Count >= MaxDebugLogs)
                    _recentLogs.Dequeue();
                _recentLogs.Enqueue(extractedText);
            }

            // Only process messages that are device -> PC responses
            if (direction != DataDirection.MachineToPc)
            {
                // keep recent logs but do not attempt to parse ping traffic
                return;
            }

            // Look for #9921 marker (flexible to handle different data formats)
            int markerIndex = extractedText.IndexOf("#9921", StringComparison.OrdinalIgnoreCase);
            if (markerIndex >= 0)
            {
                // Parse data for messages with the marker
                string text = Filter_hex(extractedText); // filter the data to get rid of error reads

                (double X, double Y, double Z) position = Grabdata(text, Updatejson.ControlData.Variable1, Updatejson.ControlData.Variable2, Updatejson.ControlData.Variable3);

                // put the data back into the package
                if (position.X > 1 && position.X < 60 && Updatejson.ControlData.Variable1 > 60)
                {
                    Updatejson.ControlData.PartCount++;
                }

                Updatejson.ControlData.Variable1 = position.X;
                Updatejson.ControlData.Variable2 = position.Y;
                Updatejson.ControlData.Variable3 = position.Z;

                if (Updatejson?.ControlData.Variable1 > 50)
                {
                    Updatejson.ControlData.MachineStatus = "RUNNING";
                }
                else
                {
                    Updatejson.ControlData.MachineStatus = "IDLE";
                }
            }

            // Always populate Debuglog with the recent raw messages (most recent last)
            // This ensures all messages (including test messages) appear in the log
            lock (_recentLogsLock)
            {
                Updatejson.ControlData.Debuglog = new List<string>(_recentLogs);
            }

            // Always insert the data into the json file (updates Debuglog regardless of marker presence)
            Updatejson.UpdateJsonData(Updatejson.ControlData);
        }
       

        private static string Filter_hex(string data)
        {
            if (data == null)
            {
                return "";
            }

            // Characters to remove: @ ` / ? - _ ' o O p P %
            char[] unwantedChars = { '@', '`', '/', '?', '-', '_', '\'', 'o', 'O', 'p', 'P', '%' };

            // Remove unwanted characters anywhere in the string
            return string.Concat(data.Where(c => !unwantedChars.Contains(c)));
        }
        

        private static (double X, double Y, double Z) Grabdata(string Data, double PreviousX, double PreviousY, double PreviousZ)
        {
            int axes = Findaxis(Data);

            if (string.IsNullOrWhiteSpace(Data))
            {
                return (PreviousX, PreviousY, PreviousZ);
            }

            try
            {
                // Look for #9921 marker
                int marker = Data.IndexOf("#9921", StringComparison.OrdinalIgnoreCase);

                if (marker < 0)
                {
                    return (PreviousX, PreviousY, PreviousZ);
                }

                // Skip the #9921 (5 chars) and the command byte (2 hex chars) to get to data
                string cleanHex = Data[(marker + 7)..];;

                double x = PreviousX;
                double y = PreviousY;
                double z = PreviousZ;

                // Extract X-axis (14 hex characters = 7 bytes)
                if (axes >= 1 && cleanHex.Length >= 14)
                {
                    x = DecodeLittleEndian(cleanHex.Substring(0, 14), PreviousX);
                } 
                
                // Extract Y-axis (14 hex characters = 7 bytes)
                if (axes >= 2 && cleanHex.Length >= 28)
                {
                    y = DecodeLittleEndian(cleanHex.Substring(14, 14), PreviousY);
                }

                // Extract Z-axis (8 hex characters = 4 bytes) - appears after the Y data
                if (axes >= 3 && cleanHex.Length >= 36)
                {
                    z = DecodeLittleEndian(cleanHex.Substring(28, 8), PreviousZ);
                }

                return (x, y, z);
            }
            catch
            {
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
            if (section.Length < 8)
                return previousValue;

            string raw = section[^8..];

            string swapped =
                raw.Substring(6, 2) +
                raw.Substring(4, 2) +
                raw.Substring(2, 2) +
                raw.Substring(0, 2);

            try
            {
                uint unsignedValue = uint.Parse(
                    swapped,
                    NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture);

                int signedValue = unchecked((int)unsignedValue);
                double result = signedValue / 512.0;

                return Math.Abs(result) > 1000
                    ? previousValue
                    : result;
            }
            catch
            {
                return previousValue;
            }
        }
    }
}

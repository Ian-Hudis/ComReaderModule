using System;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ComReaderModule
{
    public class USBSniff
    {
        private Process _snifferProcess = new();
        private readonly StringBuilder _lineBuffer = new();
        private bool _isRunning;


        public void Start()
        {

            ConfigerationData configdata = ReadConfigFile();

            Console.WriteLine("Starting autonomous USB sniffer...");

            _snifferProcess = new Process();

            // Use the full literal path
            _snifferProcess.StartInfo.FileName = configdata.toolPath;

            // -A is the "All devices" flag that fixed your manual test
            // -o - pipes the raw pcap data to the StandardOutput stream
            _snifferProcess.StartInfo.Arguments = @"-d \\.\"+ configdata.toolID + " -A -o -";

            _snifferProcess.StartInfo.UseShellExecute = false;
            _snifferProcess.StartInfo.RedirectStandardOutput = true;
            _snifferProcess.StartInfo.RedirectStandardError = true; // For debugging
            _snifferProcess.StartInfo.CreateNoWindow = true;

            // Important: Set the working directory to the tool's folder
            //_snifferProcess.StartInfo.WorkingDirectory = @"C:\Program Files\USBPcap\";
            _snifferProcess.StartInfo.WorkingDirectory = configdata.location;

            _snifferProcess.Start();
            _isRunning = true;

            // Start a thread to read the RAW binary stream
            Task.Run(() => ReadStream(_snifferProcess.StandardOutput.BaseStream));


            Task.Run(() => ReadErrorStream(_snifferProcess.StandardError));
        }


        private void ReadErrorStream(StreamReader errorReader)
        {
            try
            {
                while (_isRunning && !_snifferProcess.HasExited)
                {
                    string? line = errorReader.ReadLine();
                    if (!string.IsNullOrEmpty(line))
                    {
                        Console.WriteLine($"[USBPcap Error]: {line}");
                    }
                }
            }
            catch
            {
                // Suppress reader thread disposal exceptions on shutdown
            }
        }


        public void Stop()
        { 
            _isRunning = false;
            if (_snifferProcess != null && !_snifferProcess.HasExited)
            {
                _snifferProcess.Kill();
                _snifferProcess.Dispose();
            }
            Console.WriteLine("Sniffer stopped.");
        }


        private void ReadStream(Stream stream)
        {
            byte[] globalHeader = new byte[24];
            stream.ReadExactly(globalHeader, 0, 24);

            byte[] packetHeader = new byte[16];

            while (_isRunning && !_snifferProcess.HasExited)
            {
                int headerRead = stream.Read(packetHeader, 0, 16);
                if (headerRead < 16) break;

                uint capturedLen = BitConverter.ToUInt32(packetHeader, 8);

                if (capturedLen > 0 && capturedLen < 65535)
                {
                    byte[] packetData = new byte[capturedLen];
                    stream.ReadExactly(packetData, 0, (int)capturedLen);

                    // 1. Unmask the direction using the endpoint flag bit
                    DataDirection direction = DataDirection.PcToMachine;
                    if (packetData.Length > 16)
                    {
                        bool isIncoming = (packetData[16] & 0x01) == 1;
                        direction = isIncoming ? DataDirection.MachineToPc : DataDirection.PcToMachine;
                    }

                    // 2. Use the DataConstructor helper to extract the precise payload window
                    byte[] rawPayload = DataConstructor.ExtractRawPayload(packetData);
                    if (rawPayload == null || rawPayload.Length == 0) continue;

                    // 3. Convert only the valid payload bytes into text
                    StringBuilder sb = new();
                    foreach (byte b in rawPayload)
                    {
                        // Only pull out genuine human-readable text and layout spacing
                        if ((b >= 32 && b <= 126) || b == 13 || b == 10 || b == 9)
                        {
                            sb.Append((char)b);
                        }
                    }

                    string extractedText = sb.ToString().Trim();

                    // 4. Print clean lines matching Hercules formatting
                    if (!string.IsNullOrWhiteSpace(extractedText))
                    {
                        PrintAndLogMessage(extractedText, direction);
                    }
                }
            }
        }
        private struct ConfigerationData
        {
            public string toolPath;
            public string toolID;
            public string location;
        }

        private static ConfigerationData ReadConfigFile()
        {
            ConfigerationData inputdata = new() // make object with defaults, in case config file is missing or malformed
            {
                location = @"C:\Program Files\USBPcap\", // default location
                toolPath = @"C:\Program Files\USBPcap\USBPcapCMD.exe", // default location
                toolID = "USBPcap1" // default ID
            };

            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config.ini");
            if (File.Exists(configPath))
            {
                try
                {
                    var config = File.ReadLines(configPath)
                        .Select(line => line.Trim())
                        .Where(static line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith(';'))
                        .Select(line => line.Split('=', 2))
                        .Where(parts => parts.Length == 2)
                        .ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim().Trim('"').Trim(';').Trim('"'));

                    if (config.TryGetValue("USBPcap_Location", out var customPath))
                    {
                        inputdata.toolPath = customPath;
                        if (customPath != null)
                        {
#pragma warning disable CS8601 // Possible null reference assignment.
                            inputdata.location = Path.GetDirectoryName(customPath);
#pragma warning restore CS8601 // Possible null reference assignment.
                        }
                        else
                        {
                            Console.WriteLine("Custom path is null, using default location.");
                        }
                    }
                    if (config.TryGetValue("USBID", out var customId))
                    {
                        inputdata.toolID = customId;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading Config.ini, using defaults: {ex.Message}");
                }
            }

            return inputdata;
        }

        /*
        private void FindIndexBit(byte[] data)
        {
            string rawText = Encoding.ASCII.GetString(data);

            if (rawText.Contains("MEAS") || rawText.Contains("USBS"))
            {
                // 2. We found a valid packet! Let's find the Port ID.
                // Usually it's in the first 30 bytes.
                Console.WriteLine($"--- Packet Found! Length: {data.Length} ---");

                // This loop looks for the number '3' in the header
                for (int i = 0; i < Math.Min(data.Length, 40); i++)
                {
                    if (data[i] == 3)
                    {
                        Console.WriteLine($"Found '3' at Index: {i}");
                    }
                }
            }
        }
        */

        private readonly DataLogging datalog = new(); // this is the object for storing the data


        public enum DataDirection
        {
            PcToMachine,
            MachineToPc
        }

        private DataDirection? lastDirection = null;

        private void PrintAndLogMessage(string message, DataDirection direction) // log the data
        {
            string label = direction == DataDirection.PcToMachine ? "[PC] " : "[MACHINE] "; // sets the prefix
            ConsoleColor color = direction == DataDirection.PcToMachine ? ConsoleColor.Magenta : ConsoleColor.Gray; // sets terminal color

            Console.ForegroundColor = color;

            if (lastDirection == null)
            {
                // First packet ever received: print the initial prefix label
                Console.Write(label);
                datalog.LogData(label);
            }
            else if (direction != lastDirection)
            {
                // Direction changed: wrap up the previous sender's line, break down, and print the new label prefix
                Console.WriteLine();
                datalog.LogData(Environment.NewLine);

                Console.Write(label);
                datalog.LogData(label);
            }

            // 2. Continuous Writing: Always write the actual message data block text
            Console.Write(message);
            datalog.LogData(message);

            Console.ResetColor();
            lastDirection = direction; // Keep track for the next incoming packet
        }

    }
}

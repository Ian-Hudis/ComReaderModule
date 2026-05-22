using System;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            byte[] globalHeader = new byte[24]; // PCAP global header
            stream.ReadExactly(globalHeader, 0, 24);

            byte[] packetHeader = new byte[16];

            while (_isRunning && !_snifferProcess.HasExited)
            {
                int headerRead = stream.Read(packetHeader, 0, 16);
                if (headerRead < 16) break;

                // Parse packet header
                uint capturedLen = BitConverter.ToUInt32(packetHeader, 8);

                if (capturedLen > 0 && capturedLen < 65535)
                {
                    byte[] packetData = new byte[capturedLen];
                    stream.ReadExactly(packetData, 0, (int)capturedLen);
                    ProcessUsbData(packetData);
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
                            inputdata.location = Path.GetDirectoryName(customPath);
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

        private readonly DataLogging datalog = new();
        private void ProcessUsbData(byte[] data)
        {
            ////Print it to the console: We use Write (not WriteLine) to show exactly how it arrives
            //string junk = Encoding.GetEncoding("ISO-8859-1").GetString(data);
            // Console.Write(junk);
            string extracted = ExtractSerialString(data, new StringBuilder());

            if (IsValidSerialData(extracted))
            {
                Console.Write(extracted);

                datalog.LogData(extracted);
            }

        }

        private static bool IsValidSerialData(string data)
        {
            return !string.IsNullOrEmpty(data) && !data.StartsWith("USB");
        }

        private static string ExtractSerialString(byte[] data, StringBuilder sb)
        {
            ArgumentNullException.ThrowIfNull(sb);
            int longestStart = 0;
            int longestLength = 0;
            int currentStart = 0;
            int currentLength = 0;

            for (int i = 0; i < data.Length; i++)
            {
                byte b = data[i];

                // Printable ASCII (32-126) + CR (13) + LF (10) + Tab (9)
                if ((b >= 32 && b <= 126) || b == 13 || b == 10 || b == 9)
                {
                    if (currentLength == 0)
                        currentStart = i;

                    currentLength++;
                }
                else
                {
                    // Non-printable byte breaks the string
                    if (currentLength > longestLength)
                    {
                        longestStart = currentStart;
                        longestLength = currentLength;
                    }
                    currentLength = 0;
                }
            }

            // Check the last sequence
            if (currentLength > longestLength)
            {
                longestStart = currentStart;
                longestLength = currentLength;
            }

            // Extract the longest clean sequence (usually the actual data)
            if (longestLength > 1) // Ignore very short strings
            {
                byte[] payload = new byte[longestLength];
                Buffer.BlockCopy(data, longestStart, payload, 0, longestLength);
                return Encoding.ASCII.GetString(payload).Trim();
            }

            return "";
        }

    }
}

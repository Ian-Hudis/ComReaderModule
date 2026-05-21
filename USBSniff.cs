using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComReaderModule
{
    public class USBSniff
    {
        private Process _snifferProcess;
        private readonly StringBuilder _lineBuffer = new();
        private bool _isRunning;

        public void Start()
        {
            Console.WriteLine("Starting autonomous USB sniffer...");

            _snifferProcess = new Process();
            // Use the full literal path
            _snifferProcess.StartInfo.FileName = @"C:\Program Files\USBPcap\USBPcapCMD.exe";

            // -A is the "All devices" flag that fixed your manual test
            // -o - pipes the raw pcap data to the StandardOutput stream
            _snifferProcess.StartInfo.Arguments = @"-d \\.\USBPcap1 -A -o -";

            _snifferProcess.StartInfo.UseShellExecute = false;
            _snifferProcess.StartInfo.RedirectStandardOutput = true;
            _snifferProcess.StartInfo.RedirectStandardError = true; // For debugging
            _snifferProcess.StartInfo.CreateNoWindow = true;

            // Important: Set the working directory to the tool's folder
            _snifferProcess.StartInfo.WorkingDirectory = @"C:\Program Files\USBPcap\";

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
            stream.Read(globalHeader, 0, 24);

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
                    stream.Read(packetData, 0, (int)capturedLen);
                    ProcessUsbData(packetData);
                }
            }

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

        DataLogging datalog = new();
        private void ProcessUsbData(byte[] data)
        {
            ////Print it to the console: We use Write (not WriteLine) to show exactly how it arrives
            //string junk = Encoding.GetEncoding("ISO-8859-1").GetString(data);
            // Console.Write(junk);
            string extracted = ExtractSerialString(data);

            if (IsValidSerialData(extracted))
            {
                Console.Write(extracted);

                datalog.LogData(extracted);
            }

        }

        private bool IsValidSerialData(string data)
        {

            return !string.IsNullOrEmpty(data) && !data.StartsWith("USB");

        }

        private string ExtractSerialString(byte[] data)
        {
            // Strategy: Find the longest contiguous sequence of printable ASCII 
            // This avoids header/footer junk

            var sb = new StringBuilder();
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

            return null;
        }



    }
}

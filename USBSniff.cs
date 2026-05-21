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

        private void ReadStream(Stream stream)
        {

            byte[] buffer = new byte[4096];
            while (_isRunning && !_snifferProcess.HasExited)
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                {
                    // Pass the raw chunk to your processing logic
                    byte[] chunk = new byte[bytesRead];
                    Buffer.BlockCopy(buffer, 0, chunk, 0, bytesRead);
                    ProcessUsbData(chunk);
                    //FindIndexBit(chunk);
                }
            }
        }

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

        private int _currentBaud = 0;
        private void ProcessUsbData(byte[] data)
        {
            ////Print it to the console: We use Write (not WriteLine) to show exactly how it arrives
            string junk = Encoding.GetEncoding("ISO-8859-1").GetString(data);
            Console.Write(junk);
            

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
    }
}

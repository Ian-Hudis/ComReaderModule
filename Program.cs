using static ComReaderModule.USBSniff;

/// Ian Hudis
/// 5/21/2026
/// This is open source software, feel free to use and modify as you see fit.

namespace ComReaderModule
{
#pragma warning disable IDE0060 // Remove unused parameter
    class Program
    {
        static void Main(string[] args)
        {
            USBSniff usbSniffer = new();
            usbSniffer.Start();
            bool snifferActive = true;

            // Sanity check loop: enter "test" to verify the data pipeline updates UIData.json
            while (true)
            {
                Console.Write("Enter command ('test' = verify pipeline, 'run <file>' = replay sample data, 'exit' = quit): ");
                string? input = Console.ReadLine();

                if (input?.ToLower() == "test")
                {
                    Console.WriteLine("Sending 'test' through data pipeline...");
                    // Send "test" through the same DataExtraction channel as USB data
                    // This will appear in UIData.json Debuglog and verify the entire pipeline works
                    DataInterpreter.DataExtraction("test", DataDirection.MachineToPc);
                    Console.WriteLine("Test message sent to UIData.json Debuglog");
                }
                else if (input is not null && input.TrimStart().StartsWith("run", StringComparison.OrdinalIgnoreCase))
                {
                    string requested = input.TrimStart()["run".Length..].Trim();

                    if (requested.Length == 0)
                    {
                        Console.WriteLine("Usage: run <file>   (for example: run DataReference09012026.txt)");
                        continue;
                    }

                    // Stopping the sniffer keeps live traffic out of the replayed results.
                    // Tracking the flag also stops a second 'run' disposing the process twice.
                    if (snifferActive)
                    {
                        usbSniffer.Stop();
                        snifferActive = false;
                        Console.WriteLine("Sniffer stopped so the replay sees sample data only.");
                    }

                    SampleReplay.Run(requested);
                }
                else if (input?.ToLower() == "exit")
                {
                    break;
                }
            }

            // Proper cleanup
            if (snifferActive)
            {
                usbSniffer.Stop();
            }
        }

    }

#pragma warning restore IDE0060 // Remove unused parameter
}

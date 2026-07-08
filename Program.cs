using SharpPcap;
using SharpPcap.LibPcap;

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

            // Prevent the console from closing immediately
            Console.ReadLine();

            // Proper cleanup
            usbSniffer.Stop();
        }

    }

#pragma warning restore IDE0060 // Remove unused parameter
}

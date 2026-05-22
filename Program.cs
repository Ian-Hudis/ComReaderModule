using SharpPcap;
using SharpPcap.LibPcap;

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

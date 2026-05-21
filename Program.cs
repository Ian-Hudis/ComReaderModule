using SharpPcap;
using SharpPcap.LibPcap;

namespace ComReaderModule
{

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


}

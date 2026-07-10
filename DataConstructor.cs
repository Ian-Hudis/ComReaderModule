using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ComReaderModule.USBSniff;

namespace ComReaderModule
{
    internal static class DataConstructor
    {

        //! written by ai please test
        public static byte[] ExtractRawPayload(byte[] data)
        {
            if (data == null || data.Length <= 27) return null;

            // Read USBPcap header length to find the start of the USB frame
            ushort pcapHeaderLen = BitConverter.ToUInt16(data, 0);
            if (pcapHeaderLen >= data.Length) return null;

            int payloadOffset = pcapHeaderLen;
            int payloadLength = data.Length - payloadOffset;

            if (payloadLength >= 4)
            {
                // Command Block Wrapper (USBC) - Payload starts 15 bytes in
                if (data[payloadOffset] == 'U' && data[payloadOffset + 1] == 'S' && data[payloadOffset + 2] == 'B' && data[payloadOffset + 3] == 'C')
                {
                    if (payloadLength > 15)
                    {
                        return data[(payloadOffset + 15)..]; // C# Range syntax to slice the array
                    }
                    return null;
                }

                // Command Status Wrapper (USBS) - Empty status frame, drop it
                if (data[payloadOffset] == 'U' && data[payloadOffset + 1] == 'S' && data[payloadOffset + 2] == 'B' && data[payloadOffset + 3] == 'S')
                {
                    return null;
                }
            }

            // --- DEVICE TO HOST DATA (MACHINE -> PC RESPONSE DATA) ---
            if (payloadLength > 12)
            {
                // Search inside the window for where the real alphanumeric text stream starts.
                // This isolates the raw machine response lines ("1.234E+03 Vs" or "AS 4") 
                // by skipping past structural prefix symbols.
                for (int i = 0; i < Math.Min(16, payloadLength); i++)
                {
                    byte b = data[payloadOffset + i];

                    // Check for common real starting characters of a machine reply:
                    // Alphanumeric values, numbers, decimals, or spaces (ASCII 32, 45-57, 65-90)
                    if ((b >= '0' && b <= '9') || (b >= 'A' && b <= 'Z') || b == ' ' || b == '-')
                    {
                        return data[(payloadOffset + i)..];
                    }
                }
            }

            // Return the raw byte array segment if no wrappers are present
            return data[payloadOffset..];
        }
        //! written by ai please test
    }
}

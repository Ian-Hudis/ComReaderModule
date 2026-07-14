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

        //! written by ai but tested
#pragma warning disable CS8603 // Possible null reference return.
        public static byte[] ExtractRawPayload(byte[] data, out string hardwareIdentifier)
        {
            hardwareIdentifier = "UNKNOWN"; // Fallback identifier

            if (data == null || data.Length <= 27) return null;

            // Read USBPcap header length to find the start of the USB frame
            ushort pcapHeaderLen = BitConverter.ToUInt16(data, 0);
            if (pcapHeaderLen >= data.Length) return null;


            // HARDWARE PSEUDO-HEADER EXTRACTION:
            if (pcapHeaderLen >= 22)
            {
                // Offset 19 is a 16-bit field for Windows USB Device Address
                ushort deviceAddress = BitConverter.ToUInt16(data, 19);

                // Offset 21 is a single byte representing the physical Endpoint Address number
                byte endpointRaw = data[21];
                int endpointNum = endpointRaw & 0x0F; // Extract just the endpoint number

                hardwareIdentifier = $"DEV {deviceAddress} EP {endpointNum}";
            }


            int payloadOffset = pcapHeaderLen;
            int payloadLength = data.Length - payloadOffset;
            byte[] extractedSegment = null;

            // 1. Immediately drop USBS status packets (typically 13 bytes)
            if (payloadLength >= 4)
            {
                if (data[payloadOffset] == 'U' && data[payloadOffset + 1] == 'S' && data[payloadOffset + 2] == 'B' && data[payloadOffset + 3] == 'S')
                {
                    return null;
                }

                // Command Block Wrapper (USBC) - PC -> Machine
                if (payloadLength >= 15 && data[payloadOffset] == 'U' && data[payloadOffset + 1] == 'S' && data[payloadOffset + 2] == 'B' && data[payloadOffset + 3] == 'C')
                {
                    int commandLength = data[payloadOffset + 14];
                    if (commandLength > 0 && (payloadOffset + 15 + commandLength) <= data.Length)
                    {
                        extractedSegment = data[(payloadOffset + 15)..(payloadOffset + 15 + commandLength)];
                    }
                    else if (payloadLength > 15)
                    {
                        extractedSegment = data[(payloadOffset + 15)..];
                    }
                }
            }

            // --- DEVICE TO HOST DATA (MACHINE -> PC RESPONSE DATA) ---
            if (extractedSegment == null && payloadLength > 12)
            {
                // Search inside the window for where the real alphanumeric text stream starts.
                for (int i = 0; i < Math.Min(16, payloadLength); i++)
                {
                    byte b = data[payloadOffset + i];

                    // FIX: Removed 'b == ' '' from the starting condition. 
                    // Real strings begin with letters, numbers, signs, or query symbols.
                    if ((b >= '0' && b <= '9') || (b >= 'A' && b <= 'Z') || (b >= 'a' && b <= 'z') || b == '-' || b == '?')
                    {
                        extractedSegment = data[(payloadOffset + i)..];
                        break;
                    }
                }
            }

            // Default fallback if no special wrappers matched
            if (extractedSegment == null)
            {
                extractedSegment = data[payloadOffset..];
            }

            // --- TRUNCATE AT SERIAL ENDPOINT DELIMITER (\r or \n) ---
            for (int i = 0; i < extractedSegment.Length; i++)
            {
                if (extractedSegment[i] == 13 || extractedSegment[i] == 10)
                {
                    extractedSegment = extractedSegment[..i];
                    break;
                }
            }

            // --- VALIDATION: REJECT EMPTY PROTOCOL PACKETS ---
            // Allow any standard printable ASCII characters (indexes 32 through 126)
            bool hasPrintableContent = extractedSegment.Any(b => b >= 32 && b <= 126);

            return hasPrintableContent ? extractedSegment : null;
        }
#pragma warning restore CS8603 // Possible null reference return.
        //! written by ai but tested
    }
}

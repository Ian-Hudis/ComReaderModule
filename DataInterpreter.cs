using ComReaderModule;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using static ComReaderModule.USBSniff;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ExactMachineDemoDash.ExactEpoxy
{
    public static class DataInterpreter
    {

        public static UpdateJSon Updatejson = new(); // the object for putting variables into the json file

        public static void DataExtraction(string extractedText, DataDirection direction/*, string hardwareIdentifier*/)
        {
            string text = Filter_hex(extractedText); // filter the data to get rid of error reads (non hex values)

            (double X, double Y, double Z) position = Grabdata(text, Updatejson.ControlData.Variable1, Updatejson.ControlData.Variable2, Updatejson.ControlData.Variable3);

            // put the data back into the package

            if(position.X >1 && position.X<60 && Updatejson.ControlData.Variable1 > 60)
            {
                Updatejson.ControlData.PartCount++;
            }

            Updatejson.ControlData.Variable1 = position.X;
            Updatejson.ControlData.Variable2 = position.Y;
            Updatejson.ControlData.Variable3 = position.Z;

            if(Updatejson?.ControlData.Variable1 > 50)
            {
                Updatejson.ControlData.MachineStatus = "RUNNING";
            }
            else
            {
                Updatejson.ControlData.MachineStatus = "IDLE";
            }

            // Insert the data into the json file
            Updatejson.UpdateJsonData(Updatejson.ControlData);

            //Read.MachineStatus = "OFFLINE";
        }
       

        private static string Filter_hex(string data)
        {
            if (data == null)
            { 
                return ""; 
            }

            // Characters to remove from both Excel formulas: @ ` / ? - _ ' o O p P
            char[] unwantedChars = { '@', '`', '/', '?', '-', '_', '\'', 'o', 'O', 'p', 'P' };

            // Remove unwanted characters anywhere in the string
            return string.Concat(data.Where(c => !unwantedChars.Contains(c)));
        }
        

        private static (double X, double Y, double Z) Grabdata(string Data, double PreviousX, double PreviousY, double PreviousZ)
        {
            int axes = Findaxis(Data);

            if (string.IsNullOrWhiteSpace(Data))
            {
                return (PreviousX, PreviousY, PreviousZ);
            }

            try
            {
                int marker = Data.IndexOf("992140", StringComparison.OrdinalIgnoreCase);

                if (marker < 0)
                {
                    return (PreviousX, PreviousY, PreviousZ);
                }

                string cleanHex = Data[(marker + 6)..];

                double x = PreviousX;
                double y = PreviousY;
                double z = PreviousZ; 

                if (axes >= 1 && cleanHex.Length >= 15)
                {
                    x = DecodeLittleEndian(cleanHex.Substring(1, 14), PreviousX);
                }
                    
                if (axes >= 2 && cleanHex.Length >= 29)
                {
                    y = DecodeLittleEndian(cleanHex.Substring(15, 14), PreviousY);
                }

                int zMarker = Data.IndexOf("#9921",StringComparison.OrdinalIgnoreCase);
                if (zMarker >= 0)
                {
                    string zHex = Data[(zMarker + 5)..];

                    if (zHex.Length >= 32)
                    {
                        z = DecodeLittleEndian(zHex.Substring(24, 8), PreviousZ);
                    } 
                }

                return (x, y, z);
            }
            catch
            {
                return (PreviousX, PreviousY, PreviousZ);
            }
        }

        private static int Findaxis(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
                return 0;

            int marker = data.IndexOf("992140", StringComparison.OrdinalIgnoreCase);

            if (marker < 0)
                return 0;

            string cleanHex = data[(marker + 6)..];
            return cleanHex.Length / 14;
        }

        private static double DecodeLittleEndian(
        string section,
        double previousValue)
        {
            string raw = section[^8..];

            string swapped =
                raw.Substring(6, 2) +
                raw.Substring(4, 2) +
                raw.Substring(2, 2) +
                raw.Substring(0, 2);

            uint unsignedValue = uint.Parse(
                swapped,
                NumberStyles.HexNumber,
                CultureInfo.InvariantCulture);

            int signedValue = unchecked((int)unsignedValue);
            double result = signedValue / 512.0;

            return Math.Abs(result) > 1000
                ? previousValue
                : result;
        }

    }
}

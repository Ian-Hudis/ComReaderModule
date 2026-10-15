using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace ComReaderModule
{
    public static class FileLocation
    {
        // Use absolute path to ensure the JSON file is located next to the running executable
        public static readonly string UIfilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory ?? string.Empty, "UIData.json");
        public static readonly object FileLock = new();
    }

    public class UpdateJSon
    {

        public ControllerData ControlData = new();

        public class ControllerData
        {
            public string MachineStatus { get; set; } = string.Empty;
            public int PartCount { get; set; }
            public double Variable1 { get; set; }
            public double Variable2 { get; set; }
            public double Variable3 { get; set; }
            public List<string>? Debuglog { get; set; }            
        }


        // Store the last values to detect changes. Use sentinel values so the first write is always performed.

        private string? _lastMachineStatus = null;
        private int _lastPartCount = int.MinValue;
        private double _lastVariable1 = double.NaN;
        private double _lastVariable2 = double.NaN;
        private double _lastVariable3 = double.NaN;

        public void UpdateJsonData(ControllerData data)
        {
            // Only update if data has actually changed
            if (data.MachineStatus == _lastMachineStatus &&
                data.PartCount == _lastPartCount &&
                data.Variable1 == _lastVariable1 &&
                data.Variable2 == _lastVariable2 &&
                data.Variable3 == _lastVariable3)
            {
                return; // No changes, skip file write
            }

            string filePath = FileLocation.UIfilePath;

            try
            {
                lock (FileLocation.FileLock)
                {
                    string jsonString = File.ReadAllText(filePath);
                    JsonNode rootNode = JsonNode.Parse(jsonString);

                    rootNode["MachineStatus"] = data.MachineStatus;
                    rootNode["PartCount"] = data.PartCount;
                    rootNode["Variable1"] = data.Variable1;
                    rootNode["Variable2"] = data.Variable2;
                    rootNode["Variable3"] = data.Variable3;

                    // Update Debuglog array
                    if (data.Debuglog != null && data.Debuglog.Count > 0)
                    {
                        // Create a new array from the Debuglog list
                        var debuglogArray = JsonNode.Parse(System.Text.Json.JsonSerializer.Serialize(data.Debuglog));
                        rootNode["Debuglog"] = debuglogArray;
                    }
                    else if (!rootNode.AsObject().ContainsKey("Debuglog"))
                    {
                        // Initialize empty Debuglog array if it doesn't exist
                        rootNode["Debuglog"] = JsonNode.Parse("[]");
                    }

                    var options = new JsonSerializerOptions { WriteIndented = true };
                    string jsonContent = rootNode.ToJsonString(options);

                    // Use FileStream with AutoFlush to ensure immediate disk write (critical for Release mode)
                    using (FileStream fs = new(filePath, FileMode.Create, FileAccess.Write, FileShare.Read))
                    using (StreamWriter sw = new(fs))
                    {
                        sw.AutoFlush = true;  // Forces Windows to write to disk immediately
                        sw.Write(jsonContent);
                    }
                }

                // Update cached values after successful write
                _lastMachineStatus = data.MachineStatus;
                _lastPartCount = data.PartCount;
                _lastVariable1 = data.Variable1;
                _lastVariable2 = data.Variable2;
                _lastVariable3 = data.Variable3;
            }
            catch (IOException ex)
            {
                // Handles cases where the file is locked by another process
              //  MessageBox.Show($"Could not update file: {ex.Message}", "File Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
              Console.WriteLine(ex.ToString());
            }
            catch (JsonException ex)
            {
                // Handles cases where the JSON file contains corrupted/malformed text
               // MessageBox.Show($"Invalid JSON file format: {ex.Message}", "JSON Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
               Console.WriteLine(ex.Message);
            }
        }
    }
}

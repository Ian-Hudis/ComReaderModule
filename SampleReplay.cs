using System;
using System.IO;
using static ComReaderModule.USBSniff;

namespace ComReaderModule
{
    internal static class SampleReplay
    {
        private const string SampleFolder = "sampledata";

        public static void Run(string fileName)
        {
            string path = ResolvePath(fileName);
            if (path == "")
            {
                ReportMissing(fileName);
                return;
            }

            DataInterpreter.ResetState();

            string[] lines = File.ReadAllLines(path);

            int toMachine = 0;
            int toPc = 0;
            int markerFrames = 0;

            double lastX = double.NaN;
            double lastY = double.NaN;
            double lastZ = double.NaN;
            int lastCount = int.MinValue;
            string lastStatus = "";

            foreach (string line in lines)
            {
                if (!TryParseLine(line, out string payload, out DataDirection direction))
                {
                    continue;
                }

                DataInterpreter.DataExtraction(payload, direction);

                if (direction == DataDirection.PcToMachine)
                {
                    toMachine++;
                    continue;
                }

                toPc++;

                if (payload.Contains("#9921", StringComparison.OrdinalIgnoreCase))
                {
                    markerFrames++;
                }

                UpdateJSon.ControllerData current = DataInterpreter.Updatejson.ControlData;
                if (current.Variable1 == lastX &&
                    current.Variable2 == lastY &&
                    current.Variable3 == lastZ &&
                    current.PartCount == lastCount &&
                    current.MachineStatus == lastStatus)
                {
                    continue;
                }

                lastX = current.Variable1;
                lastY = current.Variable2;
                lastZ = current.Variable3;
                lastCount = current.PartCount;
                lastStatus = current.MachineStatus;

                Console.WriteLine(
                    $"  {lastStatus,-8} V1={lastX,-14} V2={lastY,-14} V3={lastZ,-14} parts={lastCount,-4} <= {payload}");
            }

            PrintSummary(path, lines.Length, toMachine, toPc, markerFrames);
        }

        private static bool TryParseLine(string line, out string payload, out DataDirection direction)
        {
            payload = "";
            direction = DataDirection.MachineToPc;

            if (string.IsNullOrWhiteSpace(line))
            {
                return false;
            }

            string trimmed = line.Trim();
            int labelEnd = IndexOfLabelEnd(trimmed);

            if (labelEnd < 0)
            {
                payload = trimmed;
                return true;
            }

            string prefix = trimmed[..(labelEnd + 1)];
            payload = trimmed[(labelEnd + 1)..].Trim();
            direction = prefix.Contains("[PC]", StringComparison.OrdinalIgnoreCase)
                ? DataDirection.PcToMachine
                : DataDirection.MachineToPc;

            return payload.Length > 0;
        }

        private static int IndexOfLabelEnd(string line)
        {
            for (int i = 0; i < line.Length - 1; i++)
            {
                if (line[i] == ']' && char.IsWhiteSpace(line[i + 1]))
                {
                    return i;
                }
            }

            return -1;
        }

        private static string ResolvePath(string fileName)
        {
            if (fileName.Length == 0)
            {
                return "";
            }

            if (File.Exists(fileName))
            {
                return Path.GetFullPath(fileName);
            }

            string alongsideSamples = Path.Combine(SampleFolder, Path.GetFileName(fileName));
            if (File.Exists(alongsideSamples))
            {
                return Path.GetFullPath(alongsideSamples);
            }

            return "";
        }

        private static void ReportMissing(string fileName)
        {
            Console.WriteLine($"[REPLAY] Could not find '{fileName}'.");
            Console.WriteLine("[REPLAY] Looked in the working directory and in the sampledata folder.");

            if (!Directory.Exists(SampleFolder))
            {
                Console.WriteLine("[REPLAY] There is no sampledata folder next to the working directory.");
                return;
            }

            string[] samples = Directory.GetFiles(SampleFolder, "*.txt");
            if (samples.Length == 0)
            {
                Console.WriteLine("[REPLAY] The sampledata folder contains no .txt files.");
                return;
            }

            Console.WriteLine("[REPLAY] Available sample files:");
            foreach (string sample in samples)
            {
                Console.WriteLine($"[REPLAY]   run {Path.GetFileName(sample)}");
            }
        }

        private static void PrintSummary(string path, int lineCount, int toMachine, int toPc, int markerFrames)
        {
            UpdateJSon.ControllerData result = DataInterpreter.Updatejson.ControlData;

            Console.WriteLine();
            Console.WriteLine($"[REPLAY] File:               {path}");
            Console.WriteLine($"[REPLAY] Lines read:         {lineCount}");
            Console.WriteLine($"[REPLAY] PC -> machine:      {toMachine} (the parser skips these)");
            Console.WriteLine($"[REPLAY] Machine -> PC:      {toPc}");
            Console.WriteLine($"[REPLAY] Frames with #9921:  {markerFrames}");
            Console.WriteLine();
            Console.WriteLine($"[REPLAY] Variable1:         {result.Variable1}");
            Console.WriteLine($"[REPLAY] Variable2:         {result.Variable2}");

            if (result.Variable3 == 0)
            {
                Console.WriteLine("[REPLAY] Variable3:         0 (this file's payloads never carry a Z axis)");
            }
            else
            {
                Console.WriteLine($"[REPLAY] Variable3:         {result.Variable3}");
            }

            Console.WriteLine($"[REPLAY] PartCount:         {result.PartCount}");
            Console.WriteLine($"[REPLAY] MachineStatus:     {result.MachineStatus}");
            Console.WriteLine();
            Console.WriteLine($"[REPLAY] Check {FileLocation.UIfilePath}");
        }
    }
}

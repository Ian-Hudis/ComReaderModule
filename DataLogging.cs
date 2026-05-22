using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ComReaderModule
{
    internal class DataLogging
    {
        private string _currentLogPath = "";

        private void InitializeLogFile()
        {
            try
            {
                // Force the path to look directly at the app's running folder
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string folderPath = Path.Combine(baseDir, "data");

                // Force create the directory and verify it exists
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                int fileIndex = 1;
                string filePath;

                do
                {
                    filePath = Path.Combine(folderPath, $"log{fileIndex}.txt");
                    fileIndex++;
                }
                while (File.Exists(filePath));

                _currentLogPath = filePath;

                // Print to the console screen exactly where the file is being made
                //Console.WriteLine($"\n[LOG SYSTEM] Creating log file at: {Path.GetFullPath(_currentLogPath)}");
                Console.WriteLine("\n");
                // Immediately write text and force it to disk to establish the file
                //File.WriteAllText(_currentLogPath, $"--- Started log: {DateTime.Now} ---\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[LOG ERROR] Failed to initialize log: {ex.Message}");
            }
        }

        public void LogData(string data)
        {
            // If the path hasn't been set up yet, run initialization
            if (string.IsNullOrEmpty(_currentLogPath))
            {
                InitializeLogFile();
            }

            // If initialization failed completely, don't crash, just exit
            if (string.IsNullOrEmpty(_currentLogPath)) return;

            try
            {
                // Open the file with options that force it to bypass cache and write instantly
                using FileStream fs = new(_currentLogPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                using StreamWriter sw = new(fs);
                sw.AutoFlush = true; // Crucial: forces Windows to write to disk immediately
                sw.Write(data);
            }
            catch (Exception ex)
            {
                // If it's failing to write, this will tell us why (e.g., Permissions)
                Console.WriteLine($"\n[LOG ERROR] Failed to write data: {ex.Message}");
            }
        }
    }
}


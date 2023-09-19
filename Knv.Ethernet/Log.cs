namespace Knv.Ethernet
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;

    public class Log
    {
        readonly List<string> LogLines = new List<string>();

        public void LogWriteLine(string line)
        {
            var dt = DateTime.Now;
            line = line.Trim(new char[] { ' ', '\r', '\n' });
            LogLines.Add($"{dt:yyyy}.{dt:MM}.{dt:dd} {dt:HH}:{dt:mm}:{dt:ss}:{dt:fff} {line}");
        }

        public string LogSave(string directory)
        {
            var logfile = LogSave(directory, "");
            return logfile;
        }

        public string LogSave(string directory, string prefix)
        {
            long utcTimestamp = new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds();
            var logfile = LogSave(directory, prefix, utcTimestamp);
            return logfile;
        }


        public string LogSave(string directory, string prefix, long utcTimestamp)
        {
            if (!System.IO.File.Exists(directory))
                System.IO.Directory.CreateDirectory(directory);

            LogWriteLine("Log Saved.");
            var dt = DateTimeOffset.FromUnixTimeSeconds(utcTimestamp);
            dt = dt.AddHours(2); //Ezzel meg van oldva a UTC->Local idő kérdése...
            var fileName = $"{prefix}_{dt:yyyy}{dt:MM}{dt:dd}_{dt:HH}{dt:mm}{dt:ss}.log";
            string path = $"{directory}\\{fileName}";
            using (var file = new System.IO.StreamWriter(path, true, Encoding.ASCII))
                LogLines.ForEach(file.WriteLine);
            return path;
        }

        public void OpenLogByNpp(string path)
        {
            if (System.IO.File.Exists(path))
            {
                Process myProcess = new Process();
                myProcess.StartInfo.FileName = "notepad++.exe";
                myProcess.StartInfo.Arguments = $"{path}";
                myProcess.Start();
                return;
            }
        }
    }
}

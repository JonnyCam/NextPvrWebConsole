﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Threading;

namespace NextPvrWebConsole
{
    public class Logger
    {
        private static Mutex mutexLock = new Mutex();

        public static void ELog(string Message, params object[] Parameters)
        {
            Log("ERROR: " + Message, Parameters);
        }

        public static void ILog(string Message, params object[] Parameters)
        {
            Log("INFO: " + Message, Parameters);
        }

        public static void ServiceELog(string Message, params object[] Parameters)
        {
            LogToFile("NextPVRWebConsoleService", "ERROR: " + Message, Parameters);
        }

        public static void ServiceILog(string Message, params object[] Parameters)
        {
            LogToFile("NextPVRWebConsoleService", "INFO: " + Message, Parameters);
        }

        public static void Log(string Message, params object[] Parameters)
        {
            LogToFile("NextPVRWebConsole", Message, Parameters);
        }

        private static void LogToFile(string FileNamePrefix, string Message, params object[] Parameters)
        {
            mutexLock.WaitOne();
            try
            {
                string logfile = GetLogFileName(FileNamePrefix);
                if (logfile == null)
                    return;
                File.AppendAllText(logfile, "{0} [{1}]: {2}{3}".FormatStr(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), System.Threading.Thread.CurrentThread.ManagedThreadId, Message.FormatStr(Parameters), Environment.NewLine));
            }
            catch (Exception) { }
            finally
            {
                mutexLock.ReleaseMutex();
            }
        }

        public static void DeleteOldLogFiles()
        {
            Logger.Log("Deleting old log files");
            try
            {
                foreach (FileInfo file in new DirectoryInfo(Globals.WebConsoleLoggingDirectory).GetFiles("*_*.log"))
                {
                    if (file.CreationTime < DateTime.Now.AddDays(-6))
                    {
                        try
                        {
                            file.Delete();
                        }
                        catch (Exception) { Logger.Log("Failed to delete log file: " + file.FullName); }
                    }
                }
            }
            catch (Exception ex) { Logger.Log("Failed to delete old log files: " + ex.Message); }
        }

        private static string GetLogFileName(string Prefix = "NextPVRWebConsole")
        {
            try
            {
                string logDir = Globals.WebConsoleLoggingDirectory;
                if (!Directory.Exists(logDir))
                    Directory.CreateDirectory(logDir);

                string namePattern = Path.Combine(logDir, "{0}_{1}{2}.log");
                string name = null;
                int count = 0;
                do
                {
                    string suffix = "" + (char)('a' + count);
                    name = namePattern.FormatStr(Prefix, DateTime.Now.ToString("MMMdd"), suffix);
                    if (!File.Exists(name))
                    {
                        // new file, write out standard info
                        File.AppendAllText(name, new String('=', 60) + Environment.NewLine +
                                                "NextPVR Version: " + Globals.NextPvrVersion + Environment.NewLine +
                                                "NextPVR Web Console Version: " + Globals.NextPvrWebConsoleVersion + Environment.NewLine +
                                                "NextPVR Web Console Database Version: " + Globals.DB_VERSION + Environment.NewLine +
                                                new String('=', 60) + Environment.NewLine + Environment.NewLine);
                        return name;
                    }
                    if (new FileInfo(name).Length < 1024 * 1024) // 1 meg file limit
                        return name;

                } while (count++ < 'z' - 'a');
                return null;
            }
            catch (Exception) { return null; }            
        }
    }
}
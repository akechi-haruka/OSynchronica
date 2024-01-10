using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Management;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace OAS.Util.Logging {
    public class Log {

        private static string _logFileName = "Log\\Main.log";
        public static String LogFileName {
            get { return _logFileName; }
            set {
                if (!open) {
                    _logFileName = value;
                } else {
                    throw new ArgumentException("Can't change log file after Log.Init().");
                }
            }
        }

        public static bool DrawOnScreen { get; set; }
        public static bool AutoFlush { get; set; }
        public static bool LogDebug { get; set; }

        private static DateTime initTime = DateTime.Now;
        private static bool open;
        public static object logLock = new object();
        private static StreamWriter log;

        public static void Init(bool diagnosticLogInfo = true, int logrotateCount = 5) {
            initTime = DateTime.Now;

#if DEBUG
            LogDebug = true;
#endif
            open = true;

            if (_logFileName != null) {
                DirectoryInfo logfolder = Directory.GetParent(_logFileName);
                if (!logfolder.Exists) {
                    logfolder.Create();
                }


                try {
                    if (logrotateCount > 0) {
                        if (File.Exists(LogFileName + "." + logrotateCount)) {
                            File.Delete(LogFileName + "." + logrotateCount);
                        }
                        for (int i = logrotateCount - 1; i >= 1; i--) {
                            if (File.Exists(LogFileName + "." + i)) {
                                File.Move(LogFileName + "." + i, LogFileName + "." + (i + 1));
                            }
                        }
                        if (File.Exists(LogFileName)) {
                            File.Move(LogFileName, LogFileName + ".1");
                        }
                    }
                } catch {
                    WriteError("Log rotation failed (multiple instances?)");
                }

                try {
                    log = File.AppendText(LogFileName);
                } catch {
                    WriteError("Log file open failed (multiple instances?)");
                }
            }

            WriteTraced("Log Initialized");
#if DEBUG
            WriteWarning("This is a debug build. Proceed with caution and have fun.", "Debug");
#endif

            if (diagnosticLogInfo) {
                Write("It is currently " + DateTime.Now);
                Write("The Timezone is " + TimeZoneInfo.Local.StandardName);
                Write("App Information:");
                Write("    Name: " + Assembly.GetCallingAssembly().FullName);
                Write("    Path: " + Assembly.GetCallingAssembly().Location.Replace(Environment.UserName, "*****"));
                Write("    Version: " + Assembly.GetCallingAssembly().GetName().Version);
                Write("    Codebase: " + Assembly.GetCallingAssembly().GetName().CodeBase.Replace(Environment.UserName, "*****"));
                Write("    Arguments: " + String.Join(" ", Environment.GetCommandLineArgs()));
                Write("    Loaded Libraries: ");
                foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies()) {
                    Write("      " + a.ToString());
                }
                Write("System Information:");
                Write("    CWD: " + Environment.CurrentDirectory.Replace(Environment.UserName, "*****"));
                Write("    OS Version: " + Environment.OSVersion);
                Write("    CPU Cores: " + Environment.ProcessorCount);
                Write("    .NET Version: " + Environment.Version);
                Write("    Language: " + CultureInfo.InstalledUICulture.EnglishName);

                Write("GPU Information:");

                try {
                    WriteManagementInfo();
                } catch (Exception ex) {
                    WriteWarning("GPU data unavailable: " + ex.Message);
                }
            }
        }

        private static void WriteManagementInfo() {
            ManagementObjectSearcher objvide = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");

            int gpu_num = 1;
            foreach (ManagementObject obj in objvide.Get()) {
                Write(" - GPU #" + (gpu_num++) + ":");
                Write("    Name: " + obj["Name"]);
                Write("    DeviceID: " + obj["DeviceID"]);
                Write("    AdapterRAM: " + obj["AdapterRAM"]);
                Write("    AdapterDACType: " + obj["AdapterDACType"]);
                Write("    Monochrome: " + obj["Monochrome"]);
                Write("    InstalledDisplayDrivers: " + obj["InstalledDisplayDrivers"]);
                Write("    DriverVersion: " + obj["DriverVersion"]);
                Write("    VideoProcessor: " + obj["VideoProcessor"]);
                Write("    VideoArchitecture: " + obj["VideoArchitecture"]);
                Write("    VideoMemoryType: " + obj["VideoMemoryType"]);
            }
        }

        private static void WriteOut(String message, ConsoleColor c, String section) {
            if (section.Equals("Debug")) {
                if (!LogDebug) {
                    return;
                }
            }
            lock (logLock) {
                Console.ForegroundColor = c;
                String o;
                if (message != null) {
                    String fullSection;
                    if (section.StartsWith("<") && section.EndsWith(">")) {
                        fullSection = section;
                    } else {
                        fullSection = "[" + section + "]";
                    }
                    o = "[" + ((DateTime.Now - initTime).TotalMilliseconds).ToString("N").PadLeft(14) + "]" + fullSection + " " + message;
                } else {
                    o = "";
                }
                o = o.Replace("\a", ""); // HACK: fixes random beeping caused by outputting Japanese to non-Japanese terminal
                Console.WriteLine(o);
                if (Debugger.IsAttached) {
                    Debugger.Log(0, section, message + "\r\n");
                }
                if (!open) { return; }
                log?.WriteLine(o);
                if (AutoFlush) {
                    Flush();
                }
            }
        }

        public static void Write() {
            WriteOut(null, ConsoleColor.White, null);
        }

        public static void WriteTraced(String message, [CallerFilePath] string callerFilePath = null, [CallerMemberName] string callerFunc = null) {
            WriteOut(message, ConsoleColor.White, "<" + Path.GetFileNameWithoutExtension(callerFilePath) + ":" + callerFunc + ">");
        }

        public static void Write(String message, String section = "Core") {
            WriteOut(message, ConsoleColor.White, section);
        }

        public static void WriteWarning(String message, String section = "Core") {
            WriteOut("WARN: " + message, ConsoleColor.Yellow, section);
        }

        public static void WriteError(String message, String section = "Core") {
            WriteOut("ERR: " + message, ConsoleColor.Red, section);
        }

        public static void WriteFault(Exception ex, String message = null, String section = "Core", [CallerFilePath] string callerFilePath = null, [CallerMemberName] string callerFunc = null) {
            WriteOut("FATAL: " + message + "\n" + ex, ConsoleColor.Magenta, section + "<" + Path.GetFileNameWithoutExtension(callerFilePath) + ":" + callerFunc + ">");
        }

        public static void Flush() {
            if (!open) { return; }
            try {
                log?.Flush();
            } catch { }
        }

        public static void Close() {
            lock (logLock) {
                open = false;
                try {
                    log?.Flush();
                } catch { }
                log?.Close();
                log = null;
            }
        }
    }
}

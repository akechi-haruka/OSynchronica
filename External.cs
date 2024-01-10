using OAS.Util.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OSynchronica {
    
    public class External {

        private static String current_program;

        public static int Call(string program, string args) {
            current_program = program;
            Process p = new Process();
            p.StartInfo = new ProcessStartInfo(program, args) {
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false
            };
            p.EnableRaisingEvents = true;
            p.OutputDataReceived += P_OutputDataReceived;
            p.ErrorDataReceived += P_ErrorDataReceived;

            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
            p.WaitForExit();
            return p.ExitCode;
        }

        public static int CallFFMPEG(string args, bool throwOnError = false) {
            Log.WriteTraced("Running FFMPEG...");
            Log.Write(args, "Debug");
            int ret = Call("ffmpeg.exe", args);
            if (ret != 0 && throwOnError) {
                throw new IOException("ffmpeg " + args + " returned error code " + ret);
            }
            return ret;
        }

        public static int CallWannaCRI(string args, bool throwOnError = false) {
            Log.WriteTraced("Dealing with Criware...");
            Log.Write(args, "Debug");
            int ret = Call("WannaCri.exe", args);
            if (ret != 0 && throwOnError) {
                throw new IOException("WannaCRI " + args + " returned error code " + ret);
            }
            return ret;
        }

        public static int CallMedianoche(string args, bool throwOnError = false) {
            Log.WriteTraced("Dealing with Criware...");
            Log.Write(args, "Debug");
            int ret = Call("medianoche.exe", args);
            if (ret != 0 && throwOnError) {
                throw new IOException("Medianoche " + args + " returned error code " + ret);
            }
            do {
                Thread.Sleep(1000);
            } while (Process.GetProcessesByName("medianoche.exe").Length > 0);
            return ret;
        }

        public static int CallSquirrel(string args, bool throwOnError = false) {
            Log.Write("Squirreling...", "Debug");
            Log.Write(args, "Debug");
            int ret = Call("Squirrel.exe", args);
            if (ret != 0 && throwOnError) {
                throw new IOException("Squirrel " + args + " returned error code " + ret);
            }
            return ret;
        }



        private static void P_ErrorDataReceived(object sender, DataReceivedEventArgs e) {
            if (e.Data != null && !Program.o.ExternalQuiet) {
                Program.WriteLineIfVerbose("["+ current_program + "] " + e.Data);
            }
        }

        private static void P_OutputDataReceived(object sender, DataReceivedEventArgs e) {
            if (e.Data != null && !Program.o.ExternalQuiet) {
                Program.WriteLineIfVerbose("[" + current_program + "] " + e.Data);
            }
        }

    }
}

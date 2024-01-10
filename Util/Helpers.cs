using MapsetParser.objects.hitobjects;
using OAS.Util.Logging;
using SynchronicaFumenLibrary.Events;
using SynchronicaFumenLibrary.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace OSynchronica.Util {
    public class Helpers {

        public static void CleanCreateDirectory(string path, bool always = false) {
            if (Program.o.CleanFolders || always) {
                if (Directory.Exists(path)) {
                    Directory.Delete(path, true);
                }
                Directory.CreateDirectory(path);
            } else {
                if (!Directory.Exists(path)) {
                    Directory.CreateDirectory(path);
                }
            }
        }

        public static void CopyOverwrite(string v1, string v2) {
            Log.Write("Copying " + v1 + " to " + v2 + "...", "Debug");
            File.Copy(v1, v2, true);
        }

        public static string MsToString(double time) {
            String prefix = " ";
            if (time < 0) {
                time *= -1;
                prefix = "-";
            }
            TimeSpan t = TimeSpan.FromMilliseconds(time);

            return $"[{prefix}{t.Minutes:D1}:{t.Seconds:D2}:{t.Milliseconds:D3}]";
        }
    }
}

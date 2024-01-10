using OAS.Util.Logging;
using OSynchronica.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSynchronica.Conversion {
    
    public class VideoToUSM {

        public const String TAG = "VideoToUSM";

        public static void Convert(String videoin, String videoout) {

            Program.WriteLineIfVerbose(videoin);

            if (Program.o.CleanFolders || !File.Exists(videoout)) {
                String mp4videoin = "tmp/tmp.avi";
                String tmpout = "tmp/tmp.usm";
                External.CallFFMPEG("-i \"" + videoin + "\" -an " + mp4videoin, true);
                External.CallMedianoche("-in=" + mp4videoin + " -out=" + tmpout + " -work_dir=tmp");
                Helpers.CopyOverwrite(tmpout, videoout);
            } else {
                Log.Write("Skipping usm conversion, already exists.", TAG);
                Program.WriteLineIfVerbose(videoout);
            }
        }

    }
}

using DirectXTexNet;
using OAS.Util.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSynchronica.Conversion {
    public class PNG2DDS2NUT {

        public const String TAG = "PNG2DDS2NUT";

        public unsafe static void ConvertPNGToDDS(String from, String to) {

            Log.Write("Converting " + from + " to " + to + "...", "Debug");
            try {
                var img = TexHelper.Instance.LoadFromWICFile(from, WIC_FLAGS.NONE);
                img.SaveToDDSFile(DDS_FLAGS.FORCE_DX9_LEGACY, to);
            }catch(Exception ex) {
                Log.WriteFault(ex, "Failed converting " + from + " to " + to, TAG);
                throw;
            }
        }

        public static void ConvertPNGToNUT(String from, String to) {
            String outbgtmp = "tmp/tmp.dds";
            ConvertPNGToDDS(from, outbgtmp);
            ConvertDDSToNut(outbgtmp, to);
        }

        public static void ConvertDDSToNut(String from, String to) {
            External.CallSquirrel(from + " " + to, true);
        }
    }
}

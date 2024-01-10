using OAS.Util.Logging;
using OSynchronica.Conversion;
using OSynchronica.Util;
using SynchronicaFumenLibrary;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSynchronica {
    
    public class SyncImageGenerator {
        private const String FONT_NAME = "Futura Lt BT";

        public static void CreateTitleText(Song song, string basedir, Color col, StringAlignment align, int w, int h) {
            Bitmap bitmap;
            string alignstring = "left";
            if (align == StringAlignment.Center) {
                alignstring = "center";
            } else if (align == StringAlignment.Far) {
                throw new ArgumentException("Alignment " + align + " not supported by Synchronica.");
            }
            string dirname = "title_" + col.Name.ToLower() + "_" + alignstring + "_" + w + "x" + h;
            string defaultfile = "default/" + dirname + ".png";
            if (File.Exists(defaultfile)) {
                bitmap = new Bitmap(Image.FromFile(defaultfile));
            } else {
                bitmap = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            }
            Graphics g = Graphics.FromImage(bitmap);

            Font font = new Font(FONT_NAME, 14);
            Brush brush = new SolidBrush(col);

            if (align == StringAlignment.Near) {
                g.DrawString(song.name, font, brush, new PointF(0, 0));
            } else if (align == StringAlignment.Center) {
                SizeF size = g.MeasureString(song.name, font);
                g.DrawString(song.name, font, brush, new PointF(size.Width / 2, 0));
            } else if (align == StringAlignment.Far) {
                throw new ArgumentException("Alignment " + align + " not supported by Synchronica.");
            }

            string targetdir = basedir + "/" + dirname;
            Helpers.CleanCreateDirectory(targetdir);
            String pngfile = "tmp/tmp.png";
            string targetfile = targetdir + "/s" + song.GetIdString() + "_" + dirname + ".nut";
            bitmap.Save(pngfile, ImageFormat.Png);
            PNG2DDS2NUT.ConvertPNGToNUT(pngfile, targetfile);

        }

        public static void CreateSongTitle(Song song, string basedir, int w, int h, bool shifted = false) {
            Bitmap bitmap;
            string defaultfile = "default/title_artist_" + w + "x" + h + ".png";
            if (File.Exists(defaultfile)) {
                bitmap = new Bitmap(Image.FromFile(defaultfile));
            } else {
                bitmap = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            }
            Graphics g = Graphics.FromImage(bitmap);

            Font font = new Font(FONT_NAME, 14);
            Brush brush = new SolidBrush(Color.Black);

            g.DrawString(song.name, font, brush, shifted ? new PointF(100, 27) : new PointF(0, 0));
            g.DrawString(song.details, font, brush, shifted ? new PointF(100, 70) : new PointF(0, 15));

            string targetdir = basedir + "/title_artist_" + w + "x" + h;
            Helpers.CleanCreateDirectory(targetdir);
            String pngfile = "tmp/tmp.png";
            string targetfile = targetdir + "/s" + song.GetIdString() + "_title_artist_" + w + "x" + h + ".nut";
            bitmap.Save(pngfile, ImageFormat.Png);
            PNG2DDS2NUT.ConvertPNGToNUT(pngfile, targetfile);
        }

        public static void CreateJacket(Song song, string jacketdds, string basedir, int size) {
            string targetdir = basedir + "/jacket_" + size + "x" + size;
            Helpers.CleanCreateDirectory(targetdir);
            string targetfile = targetdir + "/s" + song.GetIdString() + "_jacket_" + size + "x" + size + ".nut";
            PNG2DDS2NUT.ConvertDDSToNut(jacketdds, targetfile);
        }
    }
}

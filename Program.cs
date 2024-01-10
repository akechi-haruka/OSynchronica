using CommandLine;
using MapsetParser.objects;
using MapsetParser.objects.events;
using MapsetParser.settings;
using OAS.Util.Logging;
using OSynchronica.Conversion;
using OSynchronica.Util;
using SynchronicaFumenLibrary;
using SynchronicaFumenLibrary.Events;
using System;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;

namespace OSynchronica {

    public class Program {

        public class Options {
            [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
            public bool Verbose { get; set; }

            [Option("external-quiet", Required = false, HelpText = "Output nothing for program invocations such as ffmpeg.")]
            public bool ExternalQuiet { get; set; }

            [Option("stack-to-hold", Required = false, HelpText = "Convert stacked notes to a hold note.")]
            public bool StackToHold { get; set; }

            [Option("spinner-to-hold", Required = false, HelpText = "Convert spinners to a hold note.")]
            public bool SpinnerToHold { get; set; }

            [Option("no-reverse-to-hold", Required = false, HelpText = "Do not convert small fiddle sliders to a hold note.")]
            public bool NoReverseToHold { get; set; }

            [Option("jacket", Required = false, HelpText = "Path to song jacket image.")]
            public String JacketPath { get; set; }

            [Option("beatmap-jacket", Required = false, HelpText = "Use the beatmap background as jacket (not recommended).")]
            public bool UseBeatmapBackground { get; set; }

            [Option("clean", Required = false, HelpText = "Clean the output folder before conversion.")]
            public bool CleanFolders { get; set; }

            [Option("no-video", Required = false, HelpText = "Do not load videos.")]
            public bool NoVideo { get; set; }

            [Option("clone-vflip", Required = false, HelpText = "don't")]
            public bool CloneVFlip { get; set; }

            [Option("tags", Required = false, HelpText = "Comma-seperated list of categories for this song. (Allowed: JPOP,VOCALOID,ANIME,GAME,CLASSIC,ORIGINAL,VARIETY)")]
            public string Tags { get; set; }

            [Option("keep-wav", Required = false, HelpText = "Keep the .wav file in the sound folder, even while having a nus3bank.")]
            public bool KeepWav { get; set; }

            [Value(0, Required = true, HelpText = "The file to convert", MetaName = "Input File")]
            public string InputFile { get; set; }

            [Value(1, Required = true, HelpText = "The folder to output converted song data to", MetaName = "Output Folder")]
            public string Output { get; set; }

            [Value(2, Required = true, HelpText = "The song id to use in Synchronica. Has to be unique. Custom songs start at 600 and go up to 999.", MetaName = "Song ID")]
            public int SongId { get; set; }
        }

        static void RunOptions(Options opts) {
            o = opts;
        }

        public static Options o;

        public static void WriteLineIfVerbose(string v) {
            if (o.Verbose) {
                Log.Write(v, "Debug");
            }
        }

        public static int Main(string[] args) {

            Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            Parser.Default.ParseArguments<Options>(args).WithParsed(RunOptions);
            if (o == null) {
                return 1;
            }

            Log.Init(false, 0);
            Log.LogDebug = o.Verbose;

            if (!File.Exists(o.InputFile)) {
                Log.WriteError("File not found: " + o.InputFile);
                return 2;
            }

            string tmp = "tmp";
            try {
                Helpers.CleanCreateDirectory(tmp, true);
            }catch(Exception ex) {
                Log.WriteError("Failed to clean tmp directory: " + ex.Message);
                Log.WriteError("If OSynchronica was terminated before, make sure no stray ffmpeg processes are running.");
                return 8;
            }
            Helpers.CleanCreateDirectory(o.Output);

            string file = o.InputFile;
            WriteLineIfVerbose("Input: " + file);
            if (file.EndsWith(".osz")) {
                Log.Write("Unpacking .osz...");
                ZipFile.ExtractToDirectory(file, tmp);
                file = tmp;
            }

            WriteLineIfVerbose("Reading osu set...");
            BeatmapSet bs;
            try {
                bs = new BeatmapSet(file);
            } catch (Exception ex) {
                Log.WriteError("Failed to read " + file + ": " + ex);
                return 3;
            }

            if (bs.beatmaps.Count == 0) {
                Log.WriteError("No beatmaps found.");
                return 4;
            }

            Log.Write("Reading metadata...");
            Beatmap metamap = bs.beatmaps[0];
            MetadataSettings metadata = metamap.metadataSettings;

            Song song = new Song {
                id = o.SongId,
                name = metadata.titleUnicode,
                filename = metadata.title.Replace(' ', '_').Replace('?', '_'),
                details = metadata.artistUnicode,
                preview_ms_start = (int)metamap.generalSettings.previewTime,
                preview_ms_end = (int)metamap.generalSettings.previewTime + 30000,
                ranking = 18 // todo
            };
            song.dirname = "s" + song.GetIdString() + "_" + song.filename;
            if (o.Tags == null) {
                foreach (String tag in metadata.tags.Split(' ')) {
                    song.tags.Add(tag.Trim().ToUpper());
                }
            } else {
                song.tags.AddRange(o.Tags.Split(','));
            }

            while (song.tags.Count < 5) {
                song.tags.Add("NONE");
            }

            if (!Fumen.OFFICIAL_TAG_LIST.Contains(song.tags[0])) {
                Log.WriteError("First tag (given: "+song.tags[0]+") must be either empty, or one of " + String.Join(',', Fumen.OFFICIAL_TAG_LIST));
                Log.Write("Specify --tags to bypass this error.");
                return 9;
            }

            Tuple<Double, Double> bpmrange = OsuHelpers.GetMinAndMaxBPM(metamap.timingLines);
            song.bpm = bpmrange.Item1;
            if (bpmrange.Item2 > bpmrange.Item1 + 0.1F) { // float issues. only set bpm range if there is actually 2 different BPM in the song
                song.bpm2 = bpmrange.Item2;
            }
            WriteLineIfVerbose("BPM range: " + song.bpm + " - " + song.bpm2);

            String jacket = null;
            if (o.UseBeatmapBackground) {

                if (metamap.backgrounds.Count == 0) {
                    Log.WriteError("Specified --beatmap-jacket but beatmap does not have backgrounds!");
                    return 6;
                }

                String injacket = file + "/" + metamap.backgrounds[0].path;
                jacket = tmp + "/tmpimage.dds";
                PNG2DDS2NUT.ConvertPNGToDDS(injacket, jacket);
            } else if (o.JacketPath != null) {
                if (!File.Exists(o.JacketPath)) {
                    Log.WriteError("Jacket file not found: " + o.JacketPath);
                    return 5;
                }
                if (o.JacketPath.EndsWith(".dds")) {
                    WriteLineIfVerbose("Jacket is .dds, using directly.");
                    jacket = o.JacketPath;
                } else {
                    jacket = tmp + "/tmpimage.dds";
                    WriteLineIfVerbose("Jacket is not .dds, converting...");
                    PNG2DDS2NUT.ConvertPNGToDDS(o.JacketPath, jacket);
                }
            } else {
                jacket = "default/default.dds";
            }

            WriteLineIfVerbose("Jacket file being used: " + jacket);

            String songpackdir = o.Output + "/songpack/s" + song.GetIdString() + "_" + song.filename;
            CreateDefaultDirectories(songpackdir);

            Log.Write("Converting audio...");
            String audioout = songpackdir + "/s" + song.GetIdString() + "_" + song.filename + ".wav";
            MP3ToWAVToNu3.ConvertMP3ToWav(bs.GetAudioFilePath(), audioout);

            String sounddir = o.Output + "/sound";
            Helpers.CleanCreateDirectory(sounddir);

            String soundtmp = tmp + "\\tmp.nus3bank";
            String soundout = sounddir + "/" + song.dirname + ".nus3bank";
            MP3ToWAVToNu3.ConvertWavToNu3(audioout, soundtmp, song.preview_ms_start);
            Helpers.CopyOverwrite(soundtmp, soundout);

            if (!o.KeepWav) {
                Log.Write("Deleting " + audioout + " due to --keep-wav not set.", "Debug");
                File.Delete(audioout);
                Log.Write("Creating banklink...");
                File.WriteAllText(songpackdir + "/" + song.dirname + ".banklink", song.dirname + ".nus3bank\tosync_conv\tosync_conv\r\n");
            }

            TimelineEvent background_fumen_event = null;
            if (metamap.videos.Count > 0 && !o.NoVideo) {
                Video metavideo = metamap.videos[0];
                Log.Write("Detected beatmap video, converting video...");
                String videoin = file + "/" + metavideo.path;
                String videoout = songpackdir + "/asset/bg_00.usm";

                VideoToUSM.Convert(videoin, videoout);

                int offset = metavideo.offset;
                if (offset < 0) {
                    Log.WriteError("UNIMPLEMENTED: Video position is negative: " + Helpers.MsToString(offset));
                    offset = 0;
                }

                background_fumen_event = Fumen.CreateDummy(offset, "!bg_00");

                WriteLineIfVerbose("Writing asset_list.xtal...");
                File.WriteAllText(songpackdir + "/asset_list.xtal", "return [\"bg_00\": [\"type\":\"movie\", \"path\":\"asset/bg_00.usm\", \"preset\":\"movie_no_loop\"]]; ");
            } else if (metamap.backgrounds.Count > 0) {
                Background metabg = metamap.backgrounds[0];
                Log.Write("Detected no video but background, converting background...");
                String bgin = file + "/" + metabg.path;
                String bgout = songpackdir + "/asset/bg_00";
                PNG2DDS2NUT.ConvertPNGToNUT(bgin, bgout);

                background_fumen_event = Fumen.CreateDummy(0, "!bg_00");

                Log.WriteWarning("UNIMPLEMENTED: background image placed at " + Helpers.MsToString(0) + " rather than " + Helpers.MsToString(-5000));
                //videoevent = Fumen.CreateDummy(-5000, "!bg_00");

                WriteLineIfVerbose("Writing asset_list.xtal...");
                File.WriteAllText(songpackdir + "/asset_list.xtal", "return [\"bg_00\": [\"type\":\"texture\", \"path\":\"asset/bg_00.nut\", \"preset\":\"bg_1280x720\"]]; ");
            } else {
                Log.WriteWarning("No background video or background image found!");
            }

            String fumendir = songpackdir + "/fumen";
            Helpers.CleanCreateDirectory(fumendir);
            BM2Fumen.Convert(song, bs, fumendir, background_fumen_event);

            String songtexturedir = o.Output + "/songtexture";
            Helpers.CleanCreateDirectory(songtexturedir);
            Helpers.CleanCreateDirectory(songtexturedir);
            Log.Write("Generating jacket files...");
            SyncImageGenerator.CreateJacket(song, jacket, songtexturedir, 64);
            SyncImageGenerator.CreateJacket(song, jacket, songtexturedir, 128);
            SyncImageGenerator.CreateJacket(song, jacket, songtexturedir, 256);
            SyncImageGenerator.CreateJacket(song, jacket, songtexturedir, 512);
            Log.Write("Generating song title images...");
            SyncImageGenerator.CreateSongTitle(song, songtexturedir, 168, 48);
            SyncImageGenerator.CreateSongTitle(song, songtexturedir, 396, 96);
            SyncImageGenerator.CreateSongTitle(song, songtexturedir, 512, 108, true);
            SyncImageGenerator.CreateTitleText(song, songtexturedir, Color.White, StringAlignment.Center, 344, 36);
            SyncImageGenerator.CreateTitleText(song, songtexturedir, Color.White, StringAlignment.Near, 200, 28);
            SyncImageGenerator.CreateTitleText(song, songtexturedir, Color.Black, StringAlignment.Center, 320, 24);
            SyncImageGenerator.CreateTitleText(song, songtexturedir, Color.Black, StringAlignment.Near, 320, 24);

            Log.Write("Done!");

            Console.WriteLine("Code to append in songdata_default.xtal:");
            Console.WriteLine($"{song.id}:[\"ID\": {song.id}, \"FILENAME\": \"{song.dirname}\", \"SONGNAME\": \"{song.name}\", \"DETAILS\": \"{song.details}\", \"SONG_UNLOCK\": true, \"RATING_TARGET\": \"TRUE\", \"COURSE_UNLOCK\": [{torn(song.HasDifficulty(0))}, {torn(song.HasDifficulty(1))}, {torn(song.HasDifficulty(2))}, {torn(song.HasDifficulty(3))}, {torn(song.HasDifficulty(4))}, ], \"LV\": [{song.GetDifficultyRating(0)}, {song.GetDifficultyRating(1)}, {song.GetDifficultyRating(2)}, {song.GetDifficultyRating(3)}, {song.GetDifficultyRating(4)}, ], \"BPM\": [{song.bpm}, {song.bpm}, ], \"RECOMMEND\": false, \"RANKING\": {song.ranking}, \"TAG\": [\"{song.tags[0]}\", \"{song.tags[1]}\", \"{song.tags[2]}\", \"{song.tags[3]}\", \"{song.tags[4]}\", ], \"ATTRIBUTE\": [:], \"SABI\": [{song.preview_ms_start}, {song.preview_ms_end}, ], \"EXPIRATION_DATE\": null, 	]");


            return 0;
        }

        private static void CreateDefaultDirectories(string songpackdir) {
            Helpers.CleanCreateDirectory(songpackdir);
            Helpers.CleanCreateDirectory(songpackdir + "/asset");
            Helpers.CleanCreateDirectory(songpackdir + "/preset");
            Helpers.CleanCreateDirectory(songpackdir + "/vtag");
            Helpers.CopyOverwrite("default/asset_list.xtal", songpackdir + "/asset_list.xtal");
            Helpers.CopyOverwrite("default/pre_count.xtal", songpackdir + "/pre_count.xtal");
            Helpers.CopyOverwrite("default/bg_00.xtal", songpackdir + "/vtag/bg_00.xtal");
        }

        private static String torn(bool b) {
            return b ? "true" : "null";
        }

        
    }
}

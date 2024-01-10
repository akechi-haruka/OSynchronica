using MapsetParser.objects;
using MapsetParser.objects.hitobjects;
using MapsetParser.objects.timinglines;
using OAS.Util.Logging;
using OAS.Util.Logging;
using OSynchronica.Util;
using SynchronicaFumenLibrary;
using SynchronicaFumenLibrary.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace OSynchronica.Conversion {
    
    public class BM2Fumen {

        public const String TAG = "BM2Fumen";

        public static void Convert(Song song, BeatmapSet bs, String fumendir, TimelineEvent background_fumen_event) {
            int dn = 0;
            foreach (Beatmap bm in bs.beatmaps) {
                Log.Write("Parsing: " + bs + " " + bm);
                Fumen f = new Fumen {
                    timeline = new List<TimelineEvent>()
                };
                f.timeline.Add(Fumen.CreateBPMChange(song.bpm, 0));
                // todo: timing changes?
                double currentbpm = song.bpm;
                foreach (TimingLine tl in bs.beatmaps[0].timingLines) {
                    if (tl is UninheritedLine uil && tl.offset > 0 && uil.bpm != currentbpm) {
                        f.timeline.Add(Fumen.CreateBPMChange(song.bpm, uil.offset));
                        currentbpm = uil.bpm;
                    }
                }
                if (background_fumen_event != null) {
                    f.timeline.Add(background_fumen_event);
                }
                ParseBeatmapToFumen(bm, f);

                String fumenfile = fumendir + "/s" + song.GetIdString() + "_" + song.filename + "_d" + (dn++) + ".json";

                f.Save(fumenfile);
                song.charts.Add(f);
            }
        }

        private static void ParseBeatmapToFumen(Beatmap bm, Fumen f) {

            Log.Write("Converting: " + bm, TAG);

            foreach (HitObject obj in bm.hitObjects) {

                double time = obj.time;
                float x = obj.Position.X / OsuHelpers.OSU_PLAYFIELD_WIDTH;
                float y = obj.Position.Y / OsuHelpers.OSU_PLAYFIELD_HEIGHT;
                bool is_off_beat = false; // ???

                if (obj is Stackable st) {
                    if (st.stackIndex > 2) {
                        Log.WriteWarning(Helpers.MsToString(st.time) + " " + st.stackIndex + " stacked object(s)!", TAG);
                    }
                }
                if (obj is Circle) {

                    f.timeline.Add(Fumen.CreateTap(time, x, y, is_off_beat));
                    if (Program.o.CloneVFlip) {
                        f.timeline.Add(Fumen.CreateTap(time, 1 - x, y, is_off_beat));
                    }
                } else if (obj is Slider slider) {
                    if (!Program.o.NoReverseToHold && slider.edgeAmount > 2) {
                        // handle --no-reverse-to-hold
                        f.timeline.Add(Fumen.CreateHold(time, x, y, (float)slider.GetLength(), is_off_beat));
                        if (Program.o.CloneVFlip) {
                            f.timeline.Add(Fumen.CreateHold(time, 1 - x, y, (float)slider.GetLength(), is_off_beat));
                        }
                    } else if (slider.nodePositions.Count <= 1) {
                        Log.WriteError(Helpers.MsToString(slider.time) + " Wrong slider: " + slider.nodePositions.Count + " node(s)!", TAG);
                    } else if (slider.nodePositions.Count == 2) {
                        // handle swipe notes
                        Vector2 n1 = slider.nodePositions[0];
                        Vector2 n2 = slider.nodePositions[1];
                        int direction = (int)OsuHelpers.GetAngle(n1.X, n1.Y, n2.X, n2.Y);
                        Marker tn = Fumen.CreateSwipe(time, x, y, direction, is_off_beat);
                        if (slider.hitSound == HitObject.HitSound.Finish) {
                            tn.marker_type = "flick";
                        } else {
                            tn.marker_type = "swipe";
                        }
                        f.timeline.Add(tn);
                        if (Program.o.CloneVFlip) {
                            Marker tn2 = Fumen.CreateSwipe(time, 1 - x, y, 360 - direction, is_off_beat);
                            if (slider.hitSound == HitObject.HitSound.Finish) {
                                tn2.marker_type = "flick";
                            }
                            f.timeline.Add(tn2);
                        }
                    } else {
                        // make a move note
                        if (slider.redAnchorPositions.Count > 0) {
                            Log.WriteError(Helpers.MsToString(slider.time) + " Red anchors are not supported in Synchronica!", TAG);
                        }
                        f.timeline.Add(Fumen.CreateMove(time, x, y, (float)slider.GetCurveDuration(), OsuHelpers.BeatmapCurveToFumenCurve(slider.curveType), OsuHelpers.BeatmapPointsToFumenPoints(slider.nodePositions), is_off_beat));
                        if (Program.o.CloneVFlip) {
                            f.timeline.Add(Fumen.CreateMove(time, 1 - x, y, (float)slider.GetCurveDuration(), OsuHelpers.BeatmapCurveToFumenCurve(slider.curveType), OsuHelpers.BeatmapPointsToFumenPoints(slider.nodePositions, true), is_off_beat));
                        }
                    }
                } else if (obj is Spinner spinner) {
                    if (Program.o.SpinnerToHold) {
                        f.timeline.Add(Fumen.CreateHold(time, x, y, (float)spinner.GetLength(), is_off_beat));
                        if (Program.o.CloneVFlip) {
                            f.timeline.Add(Fumen.CreateHold(time, 1 - x, y, (float)spinner.GetLength(), is_off_beat));
                        }
                    } else {
                        f.timeline.Add(Fumen.CreateTap(time, x, y, is_off_beat));
                        if (Program.o.CloneVFlip) {
                            f.timeline.Add(Fumen.CreateTap(time, 1 - x, y, is_off_beat));
                        }
                    }
                }

            }

            int link = 0;
            TimelineEvent prev = null;
            foreach (TimelineEvent e in f.timeline) {
                if (prev != null && prev.time == e.time) {
                    if (prev is Marker m1 && e is Marker m2) {
                        int newlink = ++link;
                        if (!m1.link_id.Contains(newlink)) {
                            m1.link_id.Add(newlink);
                        }
                        if (!m2.link_id.Contains(newlink)) {
                            m2.link_id.Add(newlink);
                        }
                    }
                }
                prev = e;
            }


            Log.Write(bm.hitObjects.Count + " hitobjects converted to " + f.timeline.Count + " timeline events", TAG);
            if (link == 0) {
                Log.WriteWarning("No note links were created!", TAG);
            } else {
                Program.WriteLineIfVerbose(link + " note links created");
            }
        }

    }
}

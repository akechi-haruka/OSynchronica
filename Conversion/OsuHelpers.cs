using MapsetParser.objects;
using MapsetParser.objects.hitobjects;
using MapsetParser.objects.timinglines;
using SynchronicaFumenLibrary.Events;
using SynchronicaFumenLibrary.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace OSynchronica.Conversion {
    public class OsuHelpers {

        public const float OSU_PLAYFIELD_WIDTH = 512F;
        public const float OSU_PLAYFIELD_HEIGHT = 384F;

        public static MoveMarker.PathType BeatmapCurveToFumenCurve(Slider.CurveType curveType) {
            switch (curveType) {
                case Slider.CurveType.Linear: return MoveMarker.PathType.Line;
                case Slider.CurveType.Bezier: return MoveMarker.PathType.Bezier;
                default: throw new ArgumentException("unknown move pathtype: " + curveType);
            }
        }

        public static List<Point> BeatmapPointsToFumenPoints(List<Vector2> nodePositions, bool vflip = false) {
            List<Point> list = new List<Point>();

            foreach (Vector2 pos in nodePositions) {
                if (vflip) {
                    list.Add(new Point() {
                        x = 1 - (pos.X / OSU_PLAYFIELD_WIDTH),
                        y = pos.Y / OSU_PLAYFIELD_HEIGHT
                    });
                } else {
                    list.Add(new Point() {
                        x = pos.X / OSU_PLAYFIELD_WIDTH,
                        y = pos.Y / OSU_PLAYFIELD_HEIGHT
                    });
                }
            }

            return list;
        }

        public static double GetAngle(double cx, double cy, double ex, double ey) {
            double dy = ey - cy;
            double dx = ex - cx;
            double theta = Math.Atan2(dy, dx);
            theta *= 180 / Math.PI;
            theta -= 90;
            if (theta < 0) {
                theta += 360;
            }
            return theta;
        }

        public static Tuple<Double, Double> GetMinAndMaxBPM(List<TimingLine> timingLines) {
            UninheritedLine initialTime = (UninheritedLine)timingLines[0];
            double offset = initialTime.offset;
            double minbpm = initialTime.bpm;
            double maxbpm = minbpm;
            foreach (TimingLine tl in timingLines) {
                if (tl is UninheritedLine) {
                    double newbpm = ((UninheritedLine)tl).bpm;
                    if (newbpm != initialTime.bpm) {
                        if (newbpm < minbpm) {
                            minbpm = newbpm;
                        }
                        if (newbpm > maxbpm) {
                            maxbpm = newbpm;
                        }
                    }
                }
            }
            return new Tuple<Double, Double>(minbpm, maxbpm);
        }
    }
}

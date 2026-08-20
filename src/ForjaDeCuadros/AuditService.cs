using System;
using System.Collections.Generic;
using System.Linq;

namespace ForjaDeCuadros
{
    public static class AuditService
    {
        public static AuditReport Audit(IReadOnlyList<ProcessedFrame> frames, ProcessingOptions options)
        {
            var report = new AuditReport();
            if (frames.Count == 16) Add(report, FindingLevel.Pass, "16 cuadros procesados.");
            else Add(report, FindingLevel.Error, "Se esperaban 16 cuadros y hay " + frames.Count + ".");

            report.UniqueFrames = frames.Select(frame => frame.Sha256).Distinct(StringComparer.Ordinal).Count();
            if (report.UniqueFrames == 16) Add(report, FindingLevel.Pass, "16/16 huellas RGBA unicas.");
            else Add(report, FindingLevel.Error, "Solo " + report.UniqueFrames + "/16 cuadros son unicos.");

            int blanks = frames.Count(frame => frame.Bounds.IsEmpty);
            Add(report, blanks == 0 ? FindingLevel.Pass : FindingLevel.Error, blanks == 0 ? "Ningun cuadro vacio." : blanks + " cuadros vacios.");

            int clipped = frames.Count(frame => frame.Buffer.TouchesBorder());
            Add(report, clipped == 0 ? FindingLevel.Pass : FindingLevel.Error, clipped == 0 ? "Sin pixeles recortados por el canvas." : clipped + " cuadros tocan el borde del canvas.");

            var heights = frames.Where(frame => !frame.Bounds.IsEmpty).Select(frame => (double)frame.Bounds.Height).ToArray();
            if (heights.Length > 0)
            {
                double median = Median(heights);
                report.HeightDriftPercent = median <= 0 ? 0 : (heights.Max() - heights.Min()) / median * 100.0;
                FindingLevel level = report.HeightDriftPercent <= 6 ? FindingLevel.Pass : report.HeightDriftPercent <= 12 ? FindingLevel.Warning : FindingLevel.Error;
                Add(report, level, "Variacion de altura: " + report.HeightDriftPercent.ToString("0.0") + " %.");
            }

            if (options.RegistrationMode != RegistrationMode.CameraLocked && frames.Count > 0)
            {
                int groundDrift = frames.Max(frame => Math.Abs(frame.Bounds.Bottom - options.GroundY));
                Add(report, groundDrift <= 1 ? FindingLevel.Pass : FindingLevel.Error, "Deriva maxima de suelo: " + groundDrift + " px.");
            }

            if (options.RegistrationMode == RegistrationMode.RootAndGround && frames.Count > 0)
            {
                double rootDrift = frames.Max(frame => Math.Abs(frame.RootX - options.RootX));
                Add(report, rootDrift <= 2.5 ? FindingLevel.Pass : FindingLevel.Warning, "Deriva maxima de raiz: " + rootDrift.ToString("0.0") + " px.");
            }

            if (frames.Count >= 3)
            {
                var internalDifferences = new List<double>();
                for (int index = 0; index < frames.Count - 1; index++) internalDifferences.Add(FrameBuffer.MeanAbsoluteDifference(frames[index].Buffer, frames[index + 1].Buffer));
                double typical = Median(internalDifferences.ToArray());
                double seam = FrameBuffer.MeanAbsoluteDifference(frames[frames.Count - 1].Buffer, frames[0].Buffer);
                report.LoopSeamRatio = typical <= 0.0001 ? 0 : seam / typical;
                FindingLevel seamLevel = report.LoopSeamRatio <= 1.8 ? FindingLevel.Pass : report.LoopSeamRatio <= 3.0 ? FindingLevel.Warning : FindingLevel.Error;
                Add(report, seamLevel, "Relacion de cambio 16→01: " + report.LoopSeamRatio.ToString("0.00") + "× respecto del paso tipico.");
            }

            Add(report, FindingLevel.Info, "Revision humana obligatoria: anatomia continua, direccion de pies y equipo rigido sin deformacion.");
            return report;
        }

        private static void Add(AuditReport report, FindingLevel level, string message) => report.Findings.Add(new AuditFinding { Level = level, Message = message });

        private static double Median(double[] values)
        {
            if (values.Length == 0) return 0;
            Array.Sort(values);
            int middle = values.Length / 2;
            return values.Length % 2 == 0 ? (values[middle - 1] + values[middle]) / 2.0 : values[middle];
        }
    }
}

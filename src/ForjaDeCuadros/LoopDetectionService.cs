using System;
using System.Collections.Generic;
using System.Linq;

namespace ForjaDeCuadros
{
    public sealed class LoopDetectionResult
    {
        public bool IsAvailable { get; init; }
        public int StartIndex { get; init; }
        public int EndExclusiveIndex { get; init; }
        public double Score { get; init; }
        public double Confidence { get; init; }
        public IReadOnlyList<int> SelectedIndices { get; init; } = Array.Empty<int>();
        public bool IsStrongMatch => Confidence >= 0.55;
    }

    public static class LoopDetectionService
    {
        public const int RequiredFrames = 16;
        private const int SignatureGrid = 20;

        public static LoopDetectionResult Detect(IReadOnlyList<FrameBuffer> frames, int outputFrames = RequiredFrames)
        {
            if (frames == null) throw new ArgumentNullException(nameof(frames));
            if (outputFrames < 2) throw new ArgumentOutOfRangeException(nameof(outputFrames));
            if (frames.Count < outputFrames) return new LoopDetectionResult();

            if (frames.Count == outputFrames)
            {
                return new LoopDetectionResult
                {
                    IsAvailable = true,
                    StartIndex = 0,
                    EndExclusiveIndex = frames.Count,
                    Confidence = 0,
                    Score = 1,
                    SelectedIndices = Enumerable.Range(0, outputFrames).ToArray()
                };
            }

            double[][] signatures = frames.Select(CreateSignature).ToArray();
            return DetectSignatures(signatures, outputFrames);
        }

        public static LoopDetectionResult DetectSignatures(IReadOnlyList<double[]> signatures, int outputFrames = RequiredFrames)
        {
            if (signatures == null) throw new ArgumentNullException(nameof(signatures));
            if (outputFrames < 2) throw new ArgumentOutOfRangeException(nameof(outputFrames));
            if (signatures.Count < outputFrames) return new LoopDetectionResult();
            if (signatures.Any(signature => signature == null || signature.Length == 0)) throw new ArgumentException("Todas las firmas deben tener datos.", nameof(signatures));
            int signatureLength = signatures[0].Length;
            if (signatures.Any(signature => signature.Length != signatureLength)) throw new ArgumentException("Todas las firmas deben tener la misma longitud.", nameof(signatures));

            if (signatures.Count == outputFrames)
            {
                return new LoopDetectionResult
                {
                    IsAvailable = true,
                    StartIndex = 0,
                    EndExclusiveIndex = signatures.Count,
                    Confidence = 0,
                    Score = 1,
                    SelectedIndices = Enumerable.Range(0, outputFrames).ToArray()
                };
            }

            double referenceDistance = ReferenceDistance(signatures);
            int bestStart = 0;
            int bestEnd = outputFrames;
            double bestScore = double.PositiveInfinity;

            for (int start = 0; start + outputFrames < signatures.Count; start++)
            {
                for (int end = start + outputFrames; end < signatures.Count; end++)
                {
                    double seam = Distance(signatures[start], signatures[end]);
                    double outgoing = DeltaDistance(signatures[start], signatures[start + 1], signatures[end - 1], signatures[end]);
                    double motion = AverageMotion(signatures, start, end);
                    double stillnessPenalty = referenceDistance > 0.000001 && motion < referenceDistance * 0.035
                        ? (referenceDistance * 0.035 - motion) * 0.35
                        : 0;
                    double score = seam * 0.72 + outgoing * 0.28 + stillnessPenalty;

                    if (score < bestScore - 0.0000001 ||
                        (Math.Abs(score - bestScore) <= 0.0000001 && (start < bestStart || start == bestStart && end < bestEnd)))
                    {
                        bestScore = score;
                        bestStart = start;
                        bestEnd = end;
                    }
                }
            }

            double normalized = bestScore / Math.Max(0.000001, referenceDistance);
            double confidence = Math.Clamp(1.0 - normalized, 0.0, 1.0);
            return new LoopDetectionResult
            {
                IsAvailable = true,
                StartIndex = bestStart,
                EndExclusiveIndex = bestEnd,
                Score = bestScore,
                Confidence = confidence,
                SelectedIndices = SelectEvenly(bestStart, bestEnd, outputFrames)
            };
        }

        public static IReadOnlyList<int> SelectEvenly(int startIndex, int endExclusiveIndex, int count)
        {
            if (startIndex < 0 || endExclusiveIndex <= startIndex) throw new ArgumentOutOfRangeException(nameof(startIndex));
            int length = endExclusiveIndex - startIndex;
            if (count < 2 || length < count) throw new ArgumentOutOfRangeException(nameof(count));
            var result = new int[count];
            for (int slot = 0; slot < count; slot++)
            {
                result[slot] = startIndex + (int)Math.Floor(slot * length / (double)count);
            }
            return result;
        }

        private static double[] CreateSignature(FrameBuffer frame)
        {
            var signature = new double[SignatureGrid * SignatureGrid * 4];
            var corners = new[]
            {
                frame.PixelAt(0, 0), frame.PixelAt(frame.Width - 1, 0),
                frame.PixelAt(0, frame.Height - 1), frame.PixelAt(frame.Width - 1, frame.Height - 1)
            };
            double backgroundR = corners.Average(pixel => pixel.R) / 255.0;
            double backgroundG = corners.Average(pixel => pixel.G) / 255.0;
            double backgroundB = corners.Average(pixel => pixel.B) / 255.0;
            int cursor = 0;
            for (int gridY = 0; gridY < SignatureGrid; gridY++)
            {
                int y = Math.Min(frame.Height - 1, (int)Math.Round((gridY + 0.5) * frame.Height / SignatureGrid - 0.5));
                for (int gridX = 0; gridX < SignatureGrid; gridX++)
                {
                    int x = Math.Min(frame.Width - 1, (int)Math.Round((gridX + 0.5) * frame.Width / SignatureGrid - 0.5));
                    var pixel = frame.PixelAt(x, y);
                    double r = pixel.R / 255.0;
                    double g = pixel.G / 255.0;
                    double b = pixel.B / 255.0;
                    double a = pixel.A / 255.0;
                    double foreground = Math.Clamp(Math.Sqrt(
                        Math.Pow(r - backgroundR, 2) +
                        Math.Pow(g - backgroundG, 2) +
                        Math.Pow(b - backgroundB, 2)) * 1.35, 0.0, 1.0) * a;
                    signature[cursor++] = r * (0.35 + foreground * 0.65);
                    signature[cursor++] = g * (0.35 + foreground * 0.65);
                    signature[cursor++] = b * (0.35 + foreground * 0.65);
                    signature[cursor++] = foreground;
                }
            }
            return signature;
        }

        private static double Distance(double[] left, double[] right)
        {
            double total = 0;
            for (int index = 0; index < left.Length; index++) total += Math.Abs(left[index] - right[index]);
            return total / left.Length;
        }

        private static double DeltaDistance(double[] firstA, double[] firstB, double[] secondA, double[] secondB)
        {
            double total = 0;
            for (int index = 0; index < firstA.Length; index++)
            {
                total += Math.Abs((firstB[index] - firstA[index]) - (secondB[index] - secondA[index]));
            }
            return total / firstA.Length;
        }

        private static double AverageMotion(IReadOnlyList<double[]> signatures, int start, int end)
        {
            double total = 0;
            for (int index = start; index < end; index++) total += Distance(signatures[index], signatures[index + 1]);
            return total / Math.Max(1, end - start);
        }

        private static double ReferenceDistance(IReadOnlyList<double[]> signatures)
        {
            var distances = new List<double>();
            int stride = Math.Max(1, signatures.Count / 24);
            int separation = Math.Max(2, signatures.Count / 5);
            for (int left = 0; left < signatures.Count; left += stride)
            {
                for (int right = left + separation; right < signatures.Count; right += stride)
                {
                    distances.Add(Distance(signatures[left], signatures[right]));
                }
            }
            // Called with at least three signatures: left=0, right=separation
            // always contributes a sample before this median is calculated.
            distances.Sort();
            return Math.Max(0.000001, distances[distances.Count / 2]);
        }
    }
}

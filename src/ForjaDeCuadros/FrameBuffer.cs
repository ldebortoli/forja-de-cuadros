using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ForjaDeCuadros
{
    public sealed class FrameBuffer
    {
        public FrameBuffer(int width, int height, byte[] pixels)
        {
            Width = width;
            Height = height;
            Pixels = pixels;
            if (pixels.Length != width * height * 4) throw new ArgumentException("Tamano de buffer invalido.", nameof(pixels));
        }

        public int Width { get; }
        public int Height { get; }
        public byte[] Pixels { get; }

        public static FrameBuffer LoadPng(string path)
        {
            BitmapDecoder decoder;
            using (var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            }
            BitmapSource source = decoder.Frames[0];
            if (source.Format != PixelFormats.Bgra32)
            {
                var converted = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
                converted.Freeze();
                source = converted;
            }
            int stride = source.PixelWidth * 4;
            var pixels = new byte[stride * source.PixelHeight];
            source.CopyPixels(pixels, stride, 0);
            return new FrameBuffer(source.PixelWidth, source.PixelHeight, pixels);
        }

        public static FrameBuffer CreateSolid(int width, int height, byte r, byte g, byte b, byte a = 255)
        {
            var pixels = new byte[width * height * 4];
            for (int i = 0; i < pixels.Length; i += 4)
            {
                pixels[i] = b; pixels[i + 1] = g; pixels[i + 2] = r; pixels[i + 3] = a;
            }
            return new FrameBuffer(width, height, pixels);
        }

        public FrameBuffer Clone() => new FrameBuffer(Width, Height, (byte[])Pixels.Clone());

        public FrameBuffer CompositeOnColor(byte r, byte g, byte b)
        {
            var output = Clone();
            for (int i = 0; i < output.Pixels.Length; i += 4)
            {
                int alpha = output.Pixels[i + 3];
                int inverseAlpha = 255 - alpha;
                output.Pixels[i] = (byte)((output.Pixels[i] * alpha + b * inverseAlpha + 127) / 255);
                output.Pixels[i + 1] = (byte)((output.Pixels[i + 1] * alpha + g * inverseAlpha + 127) / 255);
                output.Pixels[i + 2] = (byte)((output.Pixels[i + 2] * alpha + r * inverseAlpha + 127) / 255);
                output.Pixels[i + 3] = 255;
            }
            return output;
        }

        public void DrawRectangle(int x, int y, int width, int height, byte r, byte g, byte b, byte a = 255)
        {
            int left = Math.Max(0, x);
            int top = Math.Max(0, y);
            int right = Math.Min(Width, x + width);
            int bottom = Math.Min(Height, y + height);
            for (int py = top; py < bottom; py++)
            {
                for (int px = left; px < right; px++)
                {
                    int index = (py * Width + px) * 4;
                    Pixels[index] = b; Pixels[index + 1] = g; Pixels[index + 2] = r; Pixels[index + 3] = a;
                }
            }
        }

        public void SavePng(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
            var bitmap = BitmapSource.Create(Width, Height, 96, 96, PixelFormats.Bgra32, null, Pixels, Width * 4);
            bitmap.Freeze();
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var stream = File.Create(path);
            encoder.Save(stream);
        }

        public BitmapSource ToBitmapSource()
        {
            var bitmap = BitmapSource.Create(Width, Height, 96, 96, PixelFormats.Bgra32, null, Pixels, Width * 4);
            bitmap.Freeze();
            return bitmap;
        }

        public (byte R, byte G, byte B, byte A) PixelAt(int x, int y)
        {
            x = Math.Clamp(x, 0, Width - 1);
            y = Math.Clamp(y, 0, Height - 1);
            int i = (y * Width + x) * 4;
            return (Pixels[i + 2], Pixels[i + 1], Pixels[i], Pixels[i + 3]);
        }

        public FrameBuffer ApplyChroma(ProcessingOptions options)
        {
            var output = Clone();
            if (!options.ChromaEnabled) return output;

            double threshold = options.ChromaTolerance * 2.25;
            double softness = Math.Max(1, options.EdgeSoftness * 2.0);
            int keyDominant = options.KeyG >= options.KeyR && options.KeyG >= options.KeyB ? 1 : options.KeyR >= options.KeyB ? 2 : 0;
            RgbToHsv(options.KeyR, options.KeyG, options.KeyB, out double keyHue, out double keySaturation, out double keyValue);

            for (int i = 0; i < output.Pixels.Length; i += 4)
            {
                double b = output.Pixels[i];
                double g = output.Pixels[i + 1];
                double r = output.Pixels[i + 2];
                double originalAlpha = output.Pixels[i + 3] / 255.0;
                RgbToHsv(r, g, b, out double hue, out double saturation, out double value);
                double hueDistance = Math.Abs(hue - keyHue);
                hueDistance = Math.Min(hueDistance, 360.0 - hueDistance) / 180.0;
                double saturationDistance = Math.Abs(saturation - keySaturation);
                double valueDistance = Math.Abs(value - keyValue);
                double distance = Math.Sqrt(Math.Pow(hueDistance * 1.30, 2) + Math.Pow(saturationDistance * 0.55, 2) + Math.Pow(valueDistance * 0.15, 2)) * 255.0;
                double keep = Math.Clamp((distance - threshold) / softness, 0.0, 1.0);
                double alpha = originalAlpha * keep;

                double spill = (1.0 - keep) * options.SpillSuppression;
                if (keyDominant == 1)
                {
                    double neutral = Math.Max(r, b);
                    g = Math.Max(neutral, g - Math.Max(0, g - neutral) * spill);
                }
                else if (keyDominant == 2)
                {
                    double neutral = Math.Max(g, b);
                    r = Math.Max(neutral, r - Math.Max(0, r - neutral) * spill);
                }
                else
                {
                    double neutral = Math.Max(r, g);
                    b = Math.Max(neutral, b - Math.Max(0, b - neutral) * spill);
                }

                output.Pixels[i] = (byte)Math.Clamp((int)Math.Round(b), 0, 255);
                output.Pixels[i + 1] = (byte)Math.Clamp((int)Math.Round(g), 0, 255);
                output.Pixels[i + 2] = (byte)Math.Clamp((int)Math.Round(r), 0, 255);
                output.Pixels[i + 3] = (byte)Math.Clamp((int)Math.Round(alpha * 255), 0, 255);
                if (output.Pixels[i + 3] == 0) output.Pixels[i] = output.Pixels[i + 1] = output.Pixels[i + 2] = 0;
            }

            if (options.HaloPixels > 0) output.ErodeAlpha(options.HaloPixels);
            if (options.IslandCleanupPixels > 0) output.RemoveSmallIslands(options.IslandCleanupPixels);
            return output;
        }

        private static void RgbToHsv(double r, double g, double b, out double hue, out double saturation, out double value)
        {
            r /= 255.0; g /= 255.0; b /= 255.0;
            double maximum = Math.Max(r, Math.Max(g, b));
            double minimum = Math.Min(r, Math.Min(g, b));
            double delta = maximum - minimum;
            value = maximum;
            saturation = maximum <= 0 ? 0 : delta / maximum;
            if (delta <= 0.000001) hue = 0;
            else if (maximum == r) hue = 60.0 * (((g - b) / delta) % 6.0);
            else if (maximum == g) hue = 60.0 * (((b - r) / delta) + 2.0);
            else hue = 60.0 * (((r - g) / delta) + 4.0);
            if (hue < 0) hue += 360.0;
        }

        private void ErodeAlpha(int radius)
        {
            radius = Math.Clamp(radius, 1, 3);
            var sourceAlpha = new byte[Width * Height];
            for (int i = 0; i < sourceAlpha.Length; i++) sourceAlpha[i] = Pixels[i * 4 + 3];
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    byte minimum = 255;
                    for (int oy = -radius; oy <= radius; oy++)
                    {
                        int sy = y + oy;
                        if (sy < 0 || sy >= Height) { minimum = 0; continue; }
                        for (int ox = -radius; ox <= radius; ox++)
                        {
                            int sx = x + ox;
                            if (sx < 0 || sx >= Width) { minimum = 0; continue; }
                            byte alpha = sourceAlpha[sy * Width + sx];
                            if (alpha < minimum) minimum = alpha;
                        }
                    }
                    int index = (y * Width + x) * 4;
                    byte oldAlpha = Pixels[index + 3];
                    if (oldAlpha > 0 && minimum < oldAlpha)
                    {
                        Pixels[index + 3] = minimum;
                    }
                }
            }
        }

        private void RemoveSmallIslands(int maximumPixels)
        {
            maximumPixels = Math.Clamp(maximumPixels, 1, 4096);
            var visited = new bool[Width * Height];
            var queue = new int[Width * Height];
            var component = new int[Math.Min(Width * Height, maximumPixels + 1)];
            for (int start = 0; start < Width * Height; start++)
            {
                if (visited[start] || Pixels[start * 4 + 3] <= 8) continue;
                int head = 0, tail = 0, stored = 0;
                queue[tail++] = start;
                visited[start] = true;
                while (head < tail)
                {
                    int current = queue[head++];
                    if (stored < component.Length) component[stored++] = current;
                    int x = current % Width;
                    int y = current / Width;
                    for (int oy = -1; oy <= 1; oy++)
                    {
                        int ny = y + oy;
                        if (ny < 0 || ny >= Height) continue;
                        for (int ox = -1; ox <= 1; ox++)
                        {
                            if (ox == 0 && oy == 0) continue;
                            int nx = x + ox;
                            if (nx < 0 || nx >= Width) continue;
                            int neighbor = ny * Width + nx;
                            if (visited[neighbor] || Pixels[neighbor * 4 + 3] <= 8) continue;
                            visited[neighbor] = true;
                            queue[tail++] = neighbor;
                        }
                    }
                }
                if (tail > maximumPixels) continue;
                for (int index = 0; index < stored; index++)
                {
                    int pixelIndex = component[index] * 4;
                    Pixels[pixelIndex] = Pixels[pixelIndex + 1] = Pixels[pixelIndex + 2] = Pixels[pixelIndex + 3] = 0;
                }
            }
        }

        public PixelBounds FindBounds(byte alphaThreshold = 8)
        {
            int left = Width, top = Height, right = -1, bottom = -1;
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (Pixels[(y * Width + x) * 4 + 3] <= alphaThreshold) continue;
                    if (x < left) left = x;
                    if (x > right) right = x;
                    if (y < top) top = y;
                    if (y > bottom) bottom = y;
                }
            }
            return right < left ? PixelBounds.Empty : new PixelBounds(left, top, right, bottom);
        }

        public double FindRootX(PixelBounds bounds)
        {
            if (bounds.IsEmpty) return Width / 2.0;
            int bandHeight = Math.Max(3, (int)Math.Round(bounds.Height * 0.16));
            int bandTop = Math.Max(bounds.Top, bounds.Bottom - bandHeight + 1);
            double weightedX = 0;
            double weight = 0;
            for (int y = bandTop; y <= bounds.Bottom; y++)
            {
                for (int x = bounds.Left; x <= bounds.Right; x++)
                {
                    byte alpha = Pixels[(y * Width + x) * 4 + 3];
                    if (alpha <= 16) continue;
                    weightedX += x * alpha;
                    weight += alpha;
                }
            }
            return weight > 0 ? weightedX / weight : (bounds.Left + bounds.Right) / 2.0;
        }

        public FrameBuffer RenderToCanvas(int canvasWidth, int canvasHeight, double scale, double offsetX, double offsetY)
        {
            var output = new FrameBuffer(canvasWidth, canvasHeight, new byte[canvasWidth * canvasHeight * 4]);
            for (int y = 0; y < canvasHeight; y++)
            {
                double sourceY = (y - offsetY) / scale;
                if (sourceY < -1 || sourceY >= Height) continue;
                int y0 = (int)Math.Floor(sourceY);
                int y1 = y0 + 1;
                double fy = sourceY - y0;
                for (int x = 0; x < canvasWidth; x++)
                {
                    double sourceX = (x - offsetX) / scale;
                    if (sourceX < -1 || sourceX >= Width) continue;
                    int x0 = (int)Math.Floor(sourceX);
                    int x1 = x0 + 1;
                    double fx = sourceX - x0;
                    SamplePremultiplied(x0, y0, x1, y1, fx, fy, out double b, out double g, out double r, out double a);
                    int index = (y * canvasWidth + x) * 4;
                    if (a <= 0.0001) continue;
                    output.Pixels[index] = (byte)Math.Clamp((int)Math.Round(b / a), 0, 255);
                    output.Pixels[index + 1] = (byte)Math.Clamp((int)Math.Round(g / a), 0, 255);
                    output.Pixels[index + 2] = (byte)Math.Clamp((int)Math.Round(r / a), 0, 255);
                    output.Pixels[index + 3] = (byte)Math.Clamp((int)Math.Round(a * 255), 0, 255);
                }
            }
            return output;
        }

        private void SamplePremultiplied(int x0, int y0, int x1, int y1, double fx, double fy, out double b, out double g, out double r, out double a)
        {
            double w00 = (1 - fx) * (1 - fy);
            double w10 = fx * (1 - fy);
            double w01 = (1 - fx) * fy;
            double w11 = fx * fy;
            b = g = r = a = 0;
            AddSample(x0, y0, w00, ref b, ref g, ref r, ref a);
            AddSample(x1, y0, w10, ref b, ref g, ref r, ref a);
            AddSample(x0, y1, w01, ref b, ref g, ref r, ref a);
            AddSample(x1, y1, w11, ref b, ref g, ref r, ref a);
        }

        private void AddSample(int x, int y, double weight, ref double b, ref double g, ref double r, ref double a)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height || weight <= 0) return;
            int index = (y * Width + x) * 4;
            double alpha = Pixels[index + 3] / 255.0;
            a += alpha * weight;
            b += Pixels[index] * alpha * weight;
            g += Pixels[index + 1] * alpha * weight;
            r += Pixels[index + 2] * alpha * weight;
        }

        public void Blit(FrameBuffer source, int destinationX, int destinationY)
        {
            for (int y = 0; y < source.Height; y++)
            {
                int dy = destinationY + y;
                if (dy < 0 || dy >= Height) continue;
                for (int x = 0; x < source.Width; x++)
                {
                    int dx = destinationX + x;
                    if (dx < 0 || dx >= Width) continue;
                    Buffer.BlockCopy(source.Pixels, (y * source.Width + x) * 4, Pixels, (dy * Width + dx) * 4, 4);
                }
            }
        }

        public string Sha256()
        {
            using var sha = SHA256.Create();
            return string.Concat(sha.ComputeHash(Pixels).Select(value => value.ToString("x2")));
        }

        public bool TouchesBorder(byte threshold = 8)
        {
            for (int x = 0; x < Width; x++)
            {
                if (Pixels[x * 4 + 3] > threshold || Pixels[((Height - 1) * Width + x) * 4 + 3] > threshold) return true;
            }
            for (int y = 0; y < Height; y++)
            {
                if (Pixels[(y * Width) * 4 + 3] > threshold || Pixels[(y * Width + Width - 1) * 4 + 3] > threshold) return true;
            }
            return false;
        }

        public static double MeanAbsoluteDifference(FrameBuffer a, FrameBuffer b)
        {
            if (a.Width != b.Width || a.Height != b.Height) return double.PositiveInfinity;
            long total = 0;
            for (int i = 0; i < a.Pixels.Length; i++) total += Math.Abs(a.Pixels[i] - b.Pixels[i]);
            return total / (double)a.Pixels.Length;
        }
    }
}

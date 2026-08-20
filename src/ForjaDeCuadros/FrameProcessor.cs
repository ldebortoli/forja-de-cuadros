using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ForjaDeCuadros
{
    public static class FrameProcessor
    {
        private sealed class SourceFrame
        {
            public FrameItem Item { get; set; } = null!;
            public FrameBuffer Buffer { get; set; } = null!;
            public PixelBounds Bounds { get; set; }
            public double RootX { get; set; }
        }

        public static List<ProcessedFrame> Process(IReadOnlyList<FrameItem> selected, ProcessingOptions options, string outputFolder)
        {
            if (selected.Count != 16) throw new InvalidOperationException("Selecciona exactamente 16 cuadros.");
            ValidateOptions(options);
            Directory.CreateDirectory(outputFolder);

            var sources = selected.OrderBy(item => item.Number).Select(item =>
            {
                var buffer = FrameBuffer.LoadPng(item.ImagePath).ApplyChroma(options);
                var bounds = buffer.FindBounds();
                return new SourceFrame { Item = item, Buffer = buffer, Bounds = bounds, RootX = buffer.FindRootX(bounds) };
            }).ToList();

            if (sources.Any(source => source.Bounds.IsEmpty)) throw new InvalidOperationException("Al menos un cuadro quedo vacio despues del chroma.");
            int maxWidth = sources.Max(source => source.Bounds.Width);
            int maxHeight = sources.Max(source => source.Bounds.Height);
            double availableWidth = Math.Max(1, options.CanvasWidth - options.Padding * 2);
            double availableHeight = Math.Max(1, options.GroundY - options.Padding);
            double scale = Math.Min(availableWidth / maxWidth, availableHeight / maxHeight);
            if (scale <= 0 || double.IsNaN(scale) || double.IsInfinity(scale)) throw new InvalidOperationException("No se pudo calcular una escala valida.");

            var reference = sources[0];
            var results = new List<ProcessedFrame>(16);
            for (int index = 0; index < sources.Count; index++)
            {
                var source = sources[index];
                double offsetX;
                double offsetY;
                if (options.RegistrationMode == RegistrationMode.RootAndGround)
                {
                    offsetX = AlignRoot(options.RootX, source.RootX, scale);
                    offsetY = AlignGround(options.GroundY, source.Bounds.Bottom, scale);
                }
                else if (options.RegistrationMode == RegistrationMode.GroundPreserveMotion)
                {
                    offsetX = AlignRoot(options.RootX, reference.RootX, scale);
                    offsetY = AlignGround(options.GroundY, source.Bounds.Bottom, scale);
                }
                else
                {
                    offsetX = AlignRoot(options.RootX, reference.RootX, scale);
                    offsetY = AlignGround(options.GroundY, reference.Bounds.Bottom, scale);
                }

                FrameBuffer rendered = source.Buffer.RenderToCanvas(options.CanvasWidth, options.CanvasHeight, scale, offsetX, offsetY);
                PixelBounds renderedBounds = rendered.FindBounds();
                string path = Path.Combine(outputFolder, "frame_" + (index + 1).ToString("00") + ".png");
                rendered.SavePng(path);
                results.Add(new ProcessedFrame
                {
                    Number = index + 1,
                    Timestamp = source.Item.Timestamp,
                    FilePath = path,
                    Buffer = rendered,
                    Bounds = renderedBounds,
                    RootX = rendered.FindRootX(renderedBounds),
                    Sha256 = rendered.Sha256()
                });
            }
            return results;
        }

        private static double AlignRoot(int targetRootX, double sourceRootX, double scale)
        {
            return targetRootX + 0.5 - (sourceRootX + 0.5) * scale;
        }

        private static double AlignGround(int targetGroundY, int sourceBottom, double scale)
        {
            return targetGroundY + 1.0 - (sourceBottom + 1.0) * scale;
        }

        private static void ValidateOptions(ProcessingOptions options)
        {
            if (options.CanvasWidth < 24 || options.CanvasWidth > 2048 || options.CanvasHeight < 24 || options.CanvasHeight > 2048)
                throw new InvalidOperationException("El canvas debe estar entre 24 y 2048 pixeles.");
            if (options.GroundY < 1 || options.GroundY >= options.CanvasHeight) throw new InvalidOperationException("La linea de suelo debe quedar dentro del canvas.");
            if (options.RootX < 0 || options.RootX >= options.CanvasWidth) throw new InvalidOperationException("La raiz X debe quedar dentro del canvas.");
        }
    }
}

using System;
using System.IO;

namespace ForjaDeCuadros
{
    public sealed class PreparedImageResult
    {
        public string OutputPath { get; init; } = string.Empty;
        public int Width { get; init; }
        public int Height { get; init; }
        public int TransparentPixelCount { get; init; }
        public int PixelCount { get; init; }
        public bool HadTransparency => TransparentPixelCount > 0;
    }

    public static class ImagePreparationService
    {
        public static string CreateOutputPath(string sourcePath, string outputFolder, string presetName)
        {
            if (string.IsNullOrWhiteSpace(sourcePath)) throw new ArgumentException("Elegí primero una imagen.", nameof(sourcePath));
            if (string.IsNullOrWhiteSpace(outputFolder)) throw new ArgumentException("La carpeta de salida no puede estar vacía.", nameof(outputFolder));

            string sourceName = Path.GetFileNameWithoutExtension(sourcePath);
            string safeName = string.Join("-", sourceName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).Trim('-', ' ');
            if (string.IsNullOrWhiteSpace(safeName)) safeName = "imagen";
            string safePreset = string.IsNullOrWhiteSpace(presetName) ? "chroma" : presetName.Trim().ToLowerInvariant();
            string suffix = DateTime.Now.ToString("yyyyMMdd-HHmmssfff") + "-" + Guid.NewGuid().ToString("N").Substring(0, 6);
            return Path.Combine(outputFolder, safeName + "-" + safePreset + "-" + suffix + ".png");
        }

        public static PreparedImageResult Prepare(string sourcePath, string outputPath, byte keyR, byte keyG, byte keyB)
        {
            if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath)) throw new FileNotFoundException("No encuentro la imagen elegida.", sourcePath);
            if (string.IsNullOrWhiteSpace(outputPath)) throw new ArgumentException("La ruta de salida no puede estar vacía.", nameof(outputPath));

            FrameBuffer source;
            try
            {
                source = FrameBuffer.LoadPng(sourcePath);
            }
            catch (Exception exception) when (exception is NotSupportedException || exception is FileFormatException)
            {
                throw new InvalidOperationException("Windows no pudo abrir este formato. Usá preferentemente PNG con transparencia; WebP funciona si su códec está instalado.", exception);
            }

            int transparentPixels = 0;
            for (int index = 3; index < source.Pixels.Length; index += 4)
            {
                if (source.Pixels[index] < 255) transparentPixels++;
            }

            source.CompositeOnColor(keyR, keyG, keyB).SavePng(outputPath);
            return new PreparedImageResult
            {
                OutputPath = outputPath,
                Width = source.Width,
                Height = source.Height,
                TransparentPixelCount = transparentPixels,
                PixelCount = source.Width * source.Height
            };
        }
    }
}

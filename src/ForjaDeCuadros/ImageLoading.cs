using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace ForjaDeCuadros
{
    public static class ImageLoading
    {
        public static BitmapImage LoadBitmap(string path, int decodeWidth = 0)
        {
            var bitmap = new BitmapImage();
            using (var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                if (decodeWidth > 0) bitmap.DecodePixelWidth = decodeWidth;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
            }
            bitmap.Freeze();
            return bitmap;
        }
    }
}

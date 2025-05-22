using System;
using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;

namespace SegmentationLibrary
{
    public static class ImageHelper
    {
        /// <summary>
        /// Преобразует изображение в формате Bitmap в формат BitmapImage, совместимый с WPF.
        /// Использует поток памяти для временного хранения изображения в формате PNG.
        /// </summary>
        public static BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            if (bitmap == null) throw new ArgumentNullException(nameof(bitmap));

            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                memory.Position = 0;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                return bitmapImage;
            }
        }
    }
}
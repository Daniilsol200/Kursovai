using System;
using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;

namespace SegmentationLibrary
{
    /// <summary>
    /// Статический класс, предоставляющий вспомогательные методы для преобразования изображений.
    /// Предназначен для конвертации изображений между форматами Bitmap и BitmapImage.
    /// </summary>
    public static class ImageHelper
    {
        /// <summary>
        /// Преобразует изображение в формате Bitmap в формат BitmapImage, совместимый с WPF.
        /// Использует поток памяти для временного хранения изображения в формате PNG.
        /// </summary>
        /// <param name="bitmap">Исходное изображение в формате Bitmap для преобразования.</param>
        /// <returns>Изображение в формате BitmapImage, готовое для отображения в WPF.</returns>
        /// <exception cref="ArgumentNullException">Выбрасывается, если параметр <paramref name="bitmap"/> равен null.</exception>
        /// <exception cref="IOException">Выбрасывается, если возникает ошибка при работе с потоком памяти.</exception>
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
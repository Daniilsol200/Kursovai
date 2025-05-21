using System;
using System.Drawing;

namespace SegmentationLibrary
{
    /// <summary>
    /// Статический класс, предоставляющий вспомогательные методы для сегментации изображений.
    /// Содержит методы для извлечения данных пикселей и вычисления метрик качества сегментации.
    /// </summary>
    public static class SegmentationUtils
    {
        /// <summary>
        /// Извлекает RGB-данные пикселей из изображения в виде массива наблюдений.
        /// Каждое наблюдение представляет собой массив из трёх значений (R, G, B).
        /// </summary>

        public static double[][] GetPixelData(Bitmap bitmap)
        {
            if (bitmap == null) throw new ArgumentNullException(nameof(bitmap));

            int width = bitmap.Width;
            int height = bitmap.Height;
            double[][] data = new double[width * height][];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color pixel = bitmap.GetPixel(x, y);
                    data[y * width + x] = new double[] { pixel.R, pixel.G, pixel.B };
                }
            }
            return data;
        }

        /// <summary>
        /// Вычисляет метрику WCSS (Within-Cluster Sum of Squares) для оценки качества сегментации.
        /// WCSS измеряет сумму квадратов расстояний от пикселей до центроидов их кластеров.
        /// </summary>

        public static double CalculateWcss(double[][] pixels, int[] labels, double[,] centroids)
        {
            if (pixels == null) throw new ArgumentNullException(nameof(pixels));
            if (labels == null) throw new ArgumentNullException(nameof(labels));
            if (centroids == null) throw new ArgumentNullException(nameof(centroids));
            if (pixels.Length != labels.Length) throw new ArgumentException("The length of pixels and labels must match.", nameof(labels));
            if (centroids.GetLength(1) != 3) throw new ArgumentException("Centroids must have 3 dimensions (R, G, B).", nameof(centroids));

            double wcss = 0;
            for (int i = 0; i < pixels.Length; i++)
            {
                int cluster = labels[i];
                if (cluster < 0 || cluster >= centroids.GetLength(0))
                    throw new ArgumentException($"Label {cluster} at index {i} is out of bounds for centroids.", nameof(labels));

                double dist = 0;
                for (int j = 0; j < 3; j++)
                {
                    double diff = pixels[i][j] - centroids[cluster, j];
                    dist += diff * diff;
                }
                wcss += dist;
            }
            return wcss;
        }
    }
}
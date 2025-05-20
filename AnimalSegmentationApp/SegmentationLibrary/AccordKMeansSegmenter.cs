using System;
using System.Drawing;
using Accord.MachineLearning;

namespace SegmentationLibrary
{
    /// <summary>
    /// Класс, реализующий сегментацию изображений с использованием алгоритма K-Means из библиотеки Accord.
    /// Реализует интерфейс <see cref="ISegmenter"/> для унификации работы с различными сегментаторами.
    /// </summary>
    public class AccordKMeansSegmenter : ISegmenter
    {
        /// <summary>
        /// Выполняет сегментацию входного изображения на основе заданного количества кластеров (k).
        /// Использует алгоритм K-Means из библиотеки Accord с настройками по умолчанию (10 итераций).
        /// </summary>

        public Bitmap Segment(Bitmap bitmap, int k)
        {
            return Segment(bitmap, k, null, 10).Image;
        }

        /// <summary>
        /// Выполняет сегментацию входного изображения с использованием алгоритма K-Means из библиотеки Accord.
        /// Позволяет указать начальные центроиды и максимальное количество итераций.
        /// </summary>
        /// <param name="bitmap">Входное изображение в формате Bitmap, которое будет сегментировано.</param>
        /// <param name="k">Количество кластеров, на которые будет разделено изображение.</param>
        /// <param name="initialCentroids">Двумерный массив начальных центроидов (опционально). Формат: [k, 3], где 3 — значения RGB (R, G, B).</param>
        /// <param name="maxIterations">Максимальное количество итераций алгоритма (по умолчанию 10).</param>
        public (Bitmap Image, int[] Labels, double[,] Centroids) Segment(Bitmap bitmap, int k, double[,] initialCentroids, int maxIterations = 10)
        {
            if (bitmap == null) throw new ArgumentNullException(nameof(bitmap));
            if (k <= 0) throw new ArgumentException("Number of clusters must be positive.", nameof(k));
            if (maxIterations <= 0) throw new ArgumentException("Maximum iterations must be positive.", nameof(maxIterations));

            int width = bitmap.Width;
            int height = bitmap.Height;
            double[][] observations = new double[width * height][];

            // Извлечение RGB-значений пикселей
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color pixel = bitmap.GetPixel(x, y);
                    observations[y * width + x] = new double[] { pixel.R, pixel.G, pixel.B };
                }
            }

            // Настройка K-Means
            KMeans kmeans = new KMeans(k)
            {
                MaxIterations = maxIterations,
                Tolerance = 0.01
            };

            // Инициализация начальных центроидов (с копированием)
            if (initialCentroids != null)
            {
                if (initialCentroids.GetLength(0) != k || initialCentroids.GetLength(1) != 3)
                {
                    throw new ArgumentException($"Initial centroids must have dimensions [{k}, 3].", nameof(initialCentroids));
                }

                kmeans.Clusters.Centroids = new double[k][];
                for (int i = 0; i < k; i++)
                {
                    // Копируем значения, чтобы не изменять входной массив
                    kmeans.Clusters.Centroids[i] = new double[] { initialCentroids[i, 0], initialCentroids[i, 1], initialCentroids[i, 2] };
                }
            }

            // Выполнение кластеризации
            var clusters = kmeans.Learn(observations);
            int[] labels = clusters.Decide(observations);

            // Создание сегментированного изображения
            Bitmap result = new Bitmap(width, height);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int idx = y * width + x;
                    int clusterIdx = labels[idx];
                    double[] centroid = kmeans.Clusters.Centroids[clusterIdx];
                    Color color = Color.FromArgb(
                        (int)Math.Max(0, Math.Min(255, centroid[0])),
                        (int)Math.Max(0, Math.Min(255, centroid[1])),
                        (int)Math.Max(0, Math.Min(255, centroid[2])));
                    result.SetPixel(x, y, color);
                }
            }

            // Преобразование центроидов в double[,]
            double[,] centroids = new double[k, 3];
            for (int i = 0; i < k; i++)
            {
                centroids[i, 0] = kmeans.Clusters.Centroids[i][0];
                centroids[i, 1] = kmeans.Clusters.Centroids[i][1];
                centroids[i, 2] = kmeans.Clusters.Centroids[i][2];
            }

            return (result, labels, centroids);
        }
    }
}
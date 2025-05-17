using System;
using System.Drawing;

namespace SegmentationLibrary
{
    /// <summary>
    /// Класс, реализующий пользовательский алгоритм K-Means для сегментации изображений.
    /// Реализует интерфейс <see cref="ISegmenter"/> для унификации работы с различными сегментаторами.
    /// </summary>
    public class CustomKMeans : ISegmenter
    {
        private readonly Random rand = new Random();

        /// <summary>
        /// Выполняет сегментацию входного изображения на основе заданного количества кластеров (k).
        /// </summary>
        /// <param name="bitmap">Входное изображение в формате Bitmap, которое будет сегментировано.</param>
        /// <param name="k">Количество кластеров, на которые будет разделено изображение.</param>
        /// <returns>Сегментированное изображение в формате Bitmap.</returns>
        public Bitmap Segment(Bitmap bitmap, int k)
        {
            return Segment(bitmap, k, null, 10).Image;
        }

        /// <summary>
        /// Выполняет сегментацию входного изображения с использованием пользовательского алгоритма K-Means.
        /// Позволяет указать начальные центроиды и максимальное количество итераций.
        /// </summary>
        /// <param name="bitmap">Входное изображение в формате Bitmap, которое будет сегментировано.</param>
        /// <param name="k">Количество кластеров, на которые будет разделено изображение.</param>
        /// <param name="initialCentroids">Двумерный массив начальных центроидов (опционально). Формат: [k, 3], где 3 — значения RGB (R, G, B).</param>
        /// <param name="maxIterations">Максимальное количество итераций алгоритма (по умолчанию 10).</param>
        /// <returns>Кортеж, содержащий сегментированное изображение, метки кластеров и финальные центроиды.</returns>
        public (Bitmap Image, int[] Labels, double[,] Centroids) Segment(Bitmap bitmap, int k, double[,] initialCentroids, int maxIterations = 10)
        {
            // Проверка входных параметров
            if (bitmap == null) throw new ArgumentNullException(nameof(bitmap));
            if (k <= 0) throw new ArgumentException("Number of clusters must be positive.", nameof(k));
            if (maxIterations <= 0) throw new ArgumentException("Maximum iterations must be positive.", nameof(maxIterations));

            int width = bitmap.Width;
            int height = bitmap.Height;
            double[,] features = new double[width * height, 3];

            // Извлечение признаков (RGB) из пикселей
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color pixel = bitmap.GetPixel(x, y);
                    int idx = y * width + x;
                    features[idx, 0] = pixel.R;
                    features[idx, 1] = pixel.G;
                    features[idx, 2] = pixel.B;
                }
            }

            // Инициализация центроидов
            double[,] centroids = new double[k, 3];
            if (initialCentroids != null && initialCentroids.GetLength(0) == k && initialCentroids.GetLength(1) == 3)
            {
                // Копируем начальные центроиды, чтобы не изменять входной массив
                for (int i = 0; i < k; i++)
                {
                    centroids[i, 0] = initialCentroids[i, 0];
                    centroids[i, 1] = initialCentroids[i, 1];
                    centroids[i, 2] = initialCentroids[i, 2];
                }
            }
            else
            {
                // Случайная инициализация центроидов
                for (int i = 0; i < k; i++)
                {
                    int randomPixel = rand.Next(features.GetLength(0));
                    centroids[i, 0] = features[randomPixel, 0];
                    centroids[i, 1] = features[randomPixel, 1];
                    centroids[i, 2] = features[randomPixel, 2];
                }
            }

            int[] labels = new int[features.GetLength(0)];
            bool centroidsChanged = true;

            // Итерации K-Means
            for (int iter = 0; iter < maxIterations && centroidsChanged; iter++)
            {
                centroidsChanged = false;

                // Шаг 1: Назначение пикселей кластерам
                for (int i = 0; i < features.GetLength(0); i++)
                {
                    double minDist = double.MaxValue;
                    int bestCluster = 0;
                    for (int j = 0; j < k; j++)
                    {
                        double dist = EuclideanDistance(
                            new double[] { features[i, 0], features[i, 1], features[i, 2] },
                            new double[] { centroids[j, 0], centroids[j, 1], centroids[j, 2] }
                        );
                        if (dist < minDist)
                        {
                            minDist = dist;
                            bestCluster = j;
                        }
                    }
                    labels[i] = bestCluster;
                }

                // Шаг 2: Пересчёт центроидов
                double[,] newCentroids = new double[k, 3];
                int[] counts = new int[k];

                for (int i = 0; i < features.GetLength(0); i++)
                {
                    int cluster = labels[i];
                    newCentroids[cluster, 0] += features[i, 0];
                    newCentroids[cluster, 1] += features[i, 1];
                    newCentroids[cluster, 2] += features[i, 2];
                    counts[cluster]++;
                }

                // Обновление центроидов и обработка пустых кластеров
                for (int j = 0; j < k; j++)
                {
                    if (counts[j] == 0)
                    {
                        // Пустой кластер: выбираем случайный пиксель как новый центроид
                        int randomPixel = rand.Next(features.GetLength(0));
                        centroids[j, 0] = features[randomPixel, 0];
                        centroids[j, 1] = features[randomPixel, 1];
                        centroids[j, 2] = features[randomPixel, 2];
                        centroidsChanged = true;
                    }
                    else
                    {
                        double oldR = centroids[j, 0];
                        double oldG = centroids[j, 1];
                        double oldB = centroids[j, 2];

                        centroids[j, 0] = newCentroids[j, 0] / counts[j];
                        centroids[j, 1] = newCentroids[j, 1] / counts[j];
                        centroids[j, 2] = newCentroids[j, 2] / counts[j];

                        // Проверяем, изменились ли центроиды
                        if (Math.Abs(oldR - centroids[j, 0]) > 0.1 ||
                            Math.Abs(oldG - centroids[j, 1]) > 0.1 ||
                            Math.Abs(oldB - centroids[j, 2]) > 0.1)
                        {
                            centroidsChanged = true;
                        }
                    }
                }
            }

            // Создание сегментированного изображения
            Bitmap result = new Bitmap(width, height);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int idx = y * width + x;
                    int cluster = labels[idx];
                    int r = (int)Math.Max(0, Math.Min(255, centroids[cluster, 0]));
                    int g = (int)Math.Max(0, Math.Min(255, centroids[cluster, 1]));
                    int b = (int)Math.Max(0, Math.Min(255, centroids[cluster, 2]));
                    Color color = Color.FromArgb(r, g, b);
                    result.SetPixel(x, y, color);
                }
            }

            return (result, labels, centroids);
        }

        /// <summary>
        /// Вычисляет евклидово расстояние между двумя точками в трёхмерном пространстве (RGB).
        /// </summary>
        /// <returns>Евклидово расстояние между точками <paramref name="a"/> и <paramref name="b"/>.</returns>
        private double EuclideanDistance(double[] a, double[] b)
        {
            if (a.Length != b.Length)
                throw new ArgumentException("Arrays must have the same length.", nameof(b));

            double sum = 0;
            for (int i = 0; i < a.Length; i++)
            {
                double diff = a[i] - b[i];
                sum += diff * diff;
            }
            return Math.Sqrt(sum);
        }
    }
}
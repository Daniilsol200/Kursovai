using System;
using System.Drawing;
using Accord.MachineLearning;

namespace SegmentationLibrary
{
    public class AccordKMeansSegmenter : ISegmenter
    {
        public Bitmap Segment(Bitmap bitmap, int k)
        {
            return Segment(bitmap, k, null, 10).Image;
        }

        public (Bitmap Image, int[] Labels, double[,] Centroids) Segment(Bitmap bitmap, int k, double[,] initialCentroids, int maxIterations = 10)
        {
            var (image, labels, centroidsArray) = SegmentWithArrayCentroids(bitmap, k, initialCentroids, maxIterations);
            double[,] centroids = new double[k, 3];
            for (int i = 0; i < k; i++)
            {
                centroids[i, 0] = centroidsArray[i][0];
                centroids[i, 1] = centroidsArray[i][1];
                centroids[i, 2] = centroidsArray[i][2];
            }
            return (image, labels, centroids);
        }

        public (Bitmap Image, int[] Labels, double[][] Centroids) SegmentWithArrayCentroids(Bitmap bitmap, int k, double[,] initialCentroids, int maxIterations = 10)
        {
            if (bitmap == null) throw new ArgumentNullException(nameof(bitmap));
            if (k <= 0) throw new ArgumentException("Number of clusters must be positive.", nameof(k));

            int width = bitmap.Width;
            int height = bitmap.Height;
            double[][] observations = new double[width * height][];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color pixel = bitmap.GetPixel(x, y);
                    observations[y * width + x] = new double[] { pixel.R, pixel.G, pixel.B };
                }
            }

            KMeans kmeans = new KMeans(k)
            {
                MaxIterations = maxIterations,
                Tolerance = 0.01
            };

            if (initialCentroids != null && initialCentroids.GetLength(0) == k && initialCentroids.GetLength(1) == 3)
            {
                kmeans.Clusters.Centroids = new double[k][];
                for (int i = 0; i < k; i++)
                {
                    kmeans.Clusters.Centroids[i] = new double[] { initialCentroids[i, 0], initialCentroids[i, 1], initialCentroids[i, 2] };
                }
            }

            var clusters = kmeans.Learn(observations);
            int[] labels = new int[observations.Length];
            for (int i = 0; i < observations.Length; i++)
            {
                labels[i] = clusters.Decide(observations[i]);
            }

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

            return (result, labels, kmeans.Clusters.Centroids);
        }
    }
}
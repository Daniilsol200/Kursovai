using System;
using System.Drawing;

namespace SegmentationLibrary
{
    public class CustomKMeans : ISegmenter
    {
        private readonly Random rand = new Random();

        public Bitmap Segment(Bitmap bitmap, int k)
        {
            return Segment(bitmap, k, null, 10).Image;
        }

        public (Bitmap Image, int[] Labels, double[,] Centroids) Segment(Bitmap bitmap, int k, double[,] initialCentroids, int maxIterations = 10)
        {
            int width = bitmap.Width;
            int height = bitmap.Height;
            double[,] features = new double[width * height, 3];

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

            double[,] centroids;
            if (initialCentroids != null && initialCentroids.GetLength(0) == k && initialCentroids.GetLength(1) == 3)
            {
                centroids = initialCentroids;
            }
            else
            {
                centroids = new double[k, 3];
                for (int i = 0; i < k; i++)
                {
                    int randomPixel = rand.Next(features.GetLength(0));
                    centroids[i, 0] = features[randomPixel, 0];
                    centroids[i, 1] = features[randomPixel, 1];
                    centroids[i, 2] = features[randomPixel, 2];
                }
            }

            int[] labels = new int[features.GetLength(0)];
            for (int iter = 0; iter < maxIterations; iter++)
            {
                for (int i = 0; i < features.GetLength(0); i++)
                {
                    double minDist = double.MaxValue;
                    for (int j = 0; j < k; j++)
                    {
                        double[] pixelFeatures = new double[3] { features[i, 0], features[i, 1], features[i, 2] };
                        double[] centroidFeatures = new double[3] { centroids[j, 0], centroids[j, 1], centroids[j, 2] };
                        double dist = EuclideanDistance(pixelFeatures, centroidFeatures);
                        if (dist < minDist)
                        {
                            minDist = dist;
                            labels[i] = j;
                        }
                    }
                }

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

                for (int j = 0; j < k; j++)
                {
                    if (counts[j] > 0)
                    {
                        centroids[j, 0] = newCentroids[j, 0] / counts[j];
                        centroids[j, 1] = newCentroids[j, 1] / counts[j];
                        centroids[j, 2] = newCentroids[j, 2] / counts[j];
                    }
                }
            }

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

        private double EuclideanDistance(double[] a, double[] b)
        {
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
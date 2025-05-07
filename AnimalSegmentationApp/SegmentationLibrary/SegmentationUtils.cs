using System.Drawing;

namespace SegmentationLibrary
{
    public static class SegmentationUtils
    {
        public static double[][] GetPixelData(Bitmap bitmap)
        {
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

        public static double CalculateWcss(double[][] pixels, int[] labels, double[,] centroids)
        {
            double wcss = 0;
            for (int i = 0; i < pixels.Length; i++)
            {
                int cluster = labels[i];
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
using System;
using System.Windows;
using System.Windows.Controls;
using System.Drawing;
using Microsoft.Win32;
using SegmentationLibrary;
using System.Diagnostics;

namespace AnimalSegmentation
{
    public partial class MainWindow : Window
    {
        private Bitmap originalBitmap;
        private CustomKMeans customSegmenter = new CustomKMeans();
        private AccordKMeansSegmenter accordSegmenter = new AccordKMeansSegmenter();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void LoadImageButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                originalBitmap = new Bitmap(openFileDialog.FileName);
                OriginalImage.Source = ImageHelper.BitmapToImageSource(originalBitmap);
                SegmentButton.IsEnabled = true;

                OriginalTimeText.Text = "Processing Time: 0 ms";
                OriginalQualityText.Text = "Quality Score: N/A";
            }
        }

        private void SegmentButton_Click(object sender, RoutedEventArgs e)
        {
            if (originalBitmap != null)
            {
                try
                {
                    int k = int.Parse(KTextBox.Text);
                    int maxIterations = int.Parse(MaxIterationsTextBox.Text);

                    Stopwatch stopwatch = new Stopwatch();
                    double[][] pixelData = GetPixelData(originalBitmap);

                    // Сегментация с CustomKMeans
                    stopwatch.Start();
                    var (customImage, customLabels, customCentroids) = customSegmenter.Segment(originalBitmap, k, null, maxIterations);
                    stopwatch.Stop();
                    CustomSegmentedImage.Source = ImageHelper.BitmapToImageSource(customImage);
                    CustomTimeText.Text = $"Processing Time: {stopwatch.ElapsedMilliseconds} ms";
                    double customWcss = CalculateWcss(pixelData, customLabels, customCentroids);
                    CustomQualityText.Text = $"Quality Score: {customWcss:F2}";

                    // Сегментация с AccordKMeans
                    stopwatch.Restart();
                    var (accordImage, accordLabels, accordCentroids) = accordSegmenter.Segment(originalBitmap, k, null, maxIterations);
                    stopwatch.Stop();
                    AccordSegmentedImage.Source = ImageHelper.BitmapToImageSource(accordImage);
                    AccordTimeText.Text = $"Processing Time: {stopwatch.ElapsedMilliseconds} ms";
                    double accordWcss = CalculateWcss(pixelData, accordLabels, accordCentroids);
                    AccordQualityText.Text = $"Quality Score: {accordWcss:F2}";
                }
                catch (FormatException)
                {
                    MessageBox.Show("Please enter valid numbers for K and Max Iterations.");
                }
            }
        }

        private double[][] GetPixelData(Bitmap bitmap)
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

        private double CalculateWcss(double[][] pixels, int[] labels, double[,] centroids)
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
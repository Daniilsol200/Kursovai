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
            KTextBox.TextChanged += KTextBox_TextChanged;
            CentroidsStackPanel.Visibility = Visibility.Collapsed;
            UpdateCentroidsPanel();
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

                    double[,] initialCentroids = null;
                    if (UseCustomCentroidsCheckBox.IsChecked == true)
                    {
                        initialCentroids = new double[k, 3];
                        for (int i = 0; i < k; i++)
                        {
                            var panel = (StackPanel)CentroidsPanel.Children[i];
                            initialCentroids[i, 0] = int.Parse(((TextBox)panel.Children[1]).Text); // R
                            initialCentroids[i, 1] = int.Parse(((TextBox)panel.Children[3]).Text); // G
                            initialCentroids[i, 2] = int.Parse(((TextBox)panel.Children[5]).Text); // B
                        }
                    }

                    Stopwatch stopwatch = new Stopwatch();
                    double[][] pixelData = GetPixelData(originalBitmap);

                    // Сегментация с CustomKMeans
                    stopwatch.Start();
                    var (customImage, customLabels, customCentroids) = customSegmenter.Segment(originalBitmap, k, initialCentroids, maxIterations);
                    stopwatch.Stop();
                    CustomSegmentedImage.Source = ImageHelper.BitmapToImageSource(customImage);
                    CustomTimeText.Text = $"Processing Time: {stopwatch.ElapsedMilliseconds} ms";
                    double customWcss = CalculateWcss(pixelData, customLabels, customCentroids);
                    CustomQualityText.Text = $"Quality Score: {customWcss:F2}";

                    // Сегментация с AccordKMeans
                    stopwatch.Restart();
                    var (accordImage, accordLabels, accordCentroids) = accordSegmenter.Segment(originalBitmap, k, initialCentroids, maxIterations);
                    stopwatch.Stop();
                    AccordSegmentedImage.Source = ImageHelper.BitmapToImageSource(accordImage);
                    AccordTimeText.Text = $"Processing Time: {stopwatch.ElapsedMilliseconds} ms";
                    double accordWcss = CalculateWcss(pixelData, accordLabels, accordCentroids);
                    AccordQualityText.Text = $"Quality Score: {accordWcss:F2}";
                }
                catch (FormatException)
                {
                    MessageBox.Show("Please enter valid numbers for K, Max Iterations, and Centroids.");
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

        private void KTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateCentroidsPanel();
        }

        private void UpdateCentroidsPanel()
        {
            CentroidsPanel.Children.Clear();
            if (int.TryParse(KTextBox.Text, out int k) && k > 0)
            {
                for (int i = 0; i < k; i++)
                {
                    var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 5) };
                    panel.Children.Add(new TextBlock { Text = $"Cluster {i}: ", VerticalAlignment = VerticalAlignment.Center });
                    panel.Children.Add(new TextBox { Width = 50, Text = $"{i * 50}", Margin = new Thickness(0, 0, 5, 0) }); // R
                    panel.Children.Add(new TextBlock { Text = ",", VerticalAlignment = VerticalAlignment.Center });
                    panel.Children.Add(new TextBox { Width = 50, Text = $"{i * 100}", Margin = new Thickness(0, 0, 5, 0) }); // G
                    panel.Children.Add(new TextBlock { Text = ",", VerticalAlignment = VerticalAlignment.Center });
                    panel.Children.Add(new TextBox { Width = 50, Text = $"{i * 150}", Margin = new Thickness(0, 0, 5, 0) }); // B
                    CentroidsPanel.Children.Add(panel);
                }
            }
        }

        private void UseCustomCentroidsCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            UpdateCentroidsPanel();
            CentroidsStackPanel.Visibility = Visibility.Visible;
        }

        private void UseCustomCentroidsCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            CentroidsStackPanel.Visibility = Visibility.Collapsed;
        }
    }
}
﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Drawing;
using Microsoft.Win32;
using SegmentationLibrary;
using System.Diagnostics;
using System.Collections.Generic;

namespace AnimalSegmentation
{
    public partial class MainWindow : Window
    {
        private Bitmap originalBitmap;
        private CustomKMeans customSegmenter = new CustomKMeans();
        private AccordKMeansSegmenter accordSegmenter = new AccordKMeansSegmenter();
        private double[,] userCentroids;
        private List<TextBox[]> centroidTextBoxes = new List<TextBox[]>();
        private bool useCustomCentroids = true;

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                if (CentroidInputs == null)
                {
                    MessageBox.Show("CentroidInputs null после InitializeComponent! Проверьте XAML и сборку.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка в InitializeComponent: {ex.Message}");
            }
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (CentroidInputs == null)
            {
                MessageBox.Show("CentroidInputs null в MainWindow_Loaded! UI не загружен корректно.");
                return;
            }

            if (int.TryParse(KTextBox?.Text, out int k) && k > 0)
            {
                UpdateCentroidInputs(k);
            }
            else
            {
                UpdateCentroidInputs(3);
            }
        }

        private void UseCustomCentroidsCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            useCustomCentroids = true;
            if (CentroidInputs != null) CentroidInputs.IsEnabled = true;
            if (ApplyCentroidsButton != null) ApplyCentroidsButton.IsEnabled = true;
        }

        private void UseCustomCentroidsCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            useCustomCentroids = false;
            if (CentroidInputs != null) CentroidInputs.IsEnabled = false;
            if (ApplyCentroidsButton != null) ApplyCentroidsButton.IsEnabled = false;
            userCentroids = null;
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
                if (OriginalImage != null) OriginalImage.Source = ImageHelper.BitmapToImageSource(originalBitmap);
                if (SegmentButton != null) SegmentButton.IsEnabled = true;

                if (OriginalTimeText != null) OriginalTimeText.Text = "Время обработки: 0 мс";
                if (OriginalQualityText != null) OriginalQualityText.Text = "Оценка качества: N/A";
            }
        }

        private void KTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded && CentroidInputs != null && int.TryParse(KTextBox?.Text, out int k) && k > 0)
            {
                UpdateCentroidInputs(k);
                userCentroids = null;
            }
        }

        private void UpdateCentroidInputs(int k)
        {
            if (CentroidInputs == null)
            {
                MessageBox.Show("Ошибка: CentroidInputs не инициализирован в UpdateCentroidInputs.");
                return;
            }

            centroidTextBoxes.Clear();
            CentroidInputs.Children.Clear();

            for (int i = 0; i < k; i++)
            {
                StackPanel sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 5) };
                TextBlock label = new TextBlock { Text = $"Центроид {i + 1} (R,G,B): ", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 5, 0) };
                TextBox tbR = new TextBox { Width = 50, Margin = new Thickness(0, 0, 5, 0), Text = "128" };
                TextBox tbG = new TextBox { Width = 50, Margin = new Thickness(0, 0, 5, 0), Text = "128" };
                TextBox tbB = new TextBox { Width = 50, Text = "128" };
                sp.Children.Add(label);
                sp.Children.Add(tbR);
                sp.Children.Add(tbG);
                sp.Children.Add(tbB);
                CentroidInputs.Children.Add(sp);
                centroidTextBoxes.Add(new[] { tbR, tbG, tbB });
            }

            CentroidInputs.IsEnabled = useCustomCentroids;
            if (ApplyCentroidsButton != null) ApplyCentroidsButton.IsEnabled = useCustomCentroids;
        }

        private void ApplyCentroidsButton_Click(object sender, RoutedEventArgs e)
        {
            if (!useCustomCentroids)
            {
                MessageBox.Show("Пользовательские центроиды отключены. Включите флажок, чтобы применить центроиды.");
                return;
            }

            try
            {
                int k = int.Parse(KTextBox?.Text ?? "3");
                if (centroidTextBoxes.Count != k)
                {
                    MessageBox.Show($"Пожалуйста, укажите ровно {k} центроидов.");
                    return;
                }

                userCentroids = new double[k, 3];
                for (int i = 0; i < k; i++)
                {
                    if (!double.TryParse(centroidTextBoxes[i][0].Text, out double r) ||
                        !double.TryParse(centroidTextBoxes[i][1].Text, out double g) ||
                        !double.TryParse(centroidTextBoxes[i][2].Text, out double b) ||
                        r < 0 || r > 255 || g < 0 || g > 255 || b < 0 || b > 255)
                    {
                        MessageBox.Show($"Недопустимые RGB-значения для центроида {i + 1}. Введите числа от 0 до 255.");
                        userCentroids = null;
                        return;
                    }
                    userCentroids[i, 0] = r;
                    userCentroids[i, 1] = g;
                    userCentroids[i, 2] = b;
                }
                MessageBox.Show("Центроиды успешно применены!");
            }
            catch (FormatException)
            {
                MessageBox.Show("Пожалуйста, введите корректное число для K.");
                userCentroids = null;
            }
        }

        private void SegmentButton_Click(object sender, RoutedEventArgs e)
        {
            if (originalBitmap != null)
            {
                try
                {
                    int k = int.Parse(KTextBox?.Text ?? "3");
                    int maxIterations = int.Parse(MaxIterationsTextBox?.Text ?? "10");

                    double[,] centroidsToUse = useCustomCentroids ? userCentroids : null;
                    if (useCustomCentroids && centroidsToUse != null && (centroidsToUse.GetLength(0) != k || centroidsToUse.GetLength(1) != 3))
                    {
                        MessageBox.Show($"Центроиды не соответствуют K={k}. Пожалуйста, примените корректные центроиды или отключите пользовательские центроиды.");
                        return;
                    }

                    Stopwatch stopwatch = new Stopwatch();
                    double[][] pixelData = GetPixelData(originalBitmap);

                    stopwatch.Start();
                    var (customImage, customLabels, customCentroids) = customSegmenter.Segment(originalBitmap, k, centroidsToUse, maxIterations);
                    stopwatch.Stop();
                    if (CustomSegmentedImage != null) CustomSegmentedImage.Source = ImageHelper.BitmapToImageSource(customImage);
                    if (CustomTimeText != null) CustomTimeText.Text = $"Время обработки: {stopwatch.ElapsedMilliseconds} мс";
                    double customWcss = CalculateWcss(pixelData, customLabels, customCentroids);
                    if (CustomQualityText != null) CustomQualityText.Text = $"Оценка качества: {customWcss:F2}";

                    string customCentroidLog = "Конечные центроиды CustomKMeans:\n";
                    for (int i = 0; i < customCentroids.GetLength(0); i++)
                    {
                        customCentroidLog += $"Центроид {i + 1}: R={customCentroids[i, 0]:F2}, G={customCentroids[i, 1]:F2}, B={customCentroids[i, 2]:F2}\n";
                    }
                    MessageBox.Show(customCentroidLog);

                    stopwatch.Restart();
                    var (accordImage, accordLabels, accordCentroids) = accordSegmenter.Segment(originalBitmap, k, centroidsToUse, maxIterations);
                    stopwatch.Stop();
                    if (AccordSegmentedImage != null) AccordSegmentedImage.Source = ImageHelper.BitmapToImageSource(accordImage);
                    if (AccordTimeText != null) AccordTimeText.Text = $"Время обработки: {stopwatch.ElapsedMilliseconds} мс";
                    double accordWcss = CalculateWcss(pixelData, accordLabels, accordCentroids);
                    if (AccordQualityText != null) AccordQualityText.Text = $"Оценка качества: {accordWcss:F2}";

                    string accordCentroidLog = "Конечные центроиды AccordKMeans:\n";
                    for (int i = 0; i < accordCentroids.GetLength(0); i++)
                    {
                        accordCentroidLog += $"Центроид {i + 1}: R={accordCentroids[i, 0]:F2}, G={accordCentroids[i, 1]:F2}, B={accordCentroids[i, 2]:F2}\n";
                    }
                    MessageBox.Show(accordCentroidLog);
                }
                catch (FormatException)
                {
                    MessageBox.Show("Пожалуйста, введите корректные числа для K и максимального числа итераций.");
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
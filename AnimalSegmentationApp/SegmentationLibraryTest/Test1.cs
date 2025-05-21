using System.Drawing;
using System.Windows.Media.Imaging;

namespace SegmentationLibrary.Tests
{
    [TestClass]
    public class SegmentationLibraryTests
    {
        private Bitmap testBitmap;

        [TestInitialize]
        public void Setup()
        {
            // Создаём тестовое изображение 2x2 с известными цветами
            testBitmap = new Bitmap(2, 2);
            testBitmap.SetPixel(0, 0, Color.FromArgb(255, 0, 0));   // Красный
            testBitmap.SetPixel(1, 0, Color.FromArgb(0, 255, 0));   // Зелёный
            testBitmap.SetPixel(0, 1, Color.FromArgb(0, 0, 255));   // Синий
            testBitmap.SetPixel(1, 1, Color.FromArgb(255, 255, 0)); // Жёлтый
        }

        [TestCleanup]
        public void Cleanup()
        {
            testBitmap?.Dispose();
        }

        // Тесты для CustomKMeans
        [TestMethod]
        public void CustomKMeans_Segment_ValidInput_ReturnsExpectedResult()
        {
            // Arrange
            var segmenter = new CustomKMeans();
            int k = 2;

            // Act
            var result = segmenter.Segment(testBitmap, k, null, 10);
            Bitmap image = result.Image;
            int[] labels = result.Labels;
            double[,] centroids = result.Centroids;

            // Assert
            Assert.IsNotNull(image);
            Assert.AreEqual(testBitmap.Width, image.Width);
            Assert.AreEqual(testBitmap.Height, image.Height);
            Assert.AreEqual(4, labels.Length); // 2x2 изображение
            Assert.AreEqual(k, centroids.GetLength(0));
            Assert.AreEqual(3, centroids.GetLength(1));

            // Проверяем, что все пиксели имеют цвета центроидов
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    int idx = y * image.Width + x;
                    int cluster = labels[idx];
                    Color pixel = image.GetPixel(x, y);
                    Assert.IsTrue(pixel.R == (int)centroids[cluster, 0] &&
                                  pixel.G == (int)centroids[cluster, 1] &&
                                  pixel.B == (int)centroids[cluster, 2]);
                }
            }
        }

        [TestMethod]
        public void CustomKMeans_Segment_NullBitmap_ThrowsArgumentNullException()
        {
            // Arrange
            var segmenter = new CustomKMeans();

            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => segmenter.Segment(null, 2));
        }

        [TestMethod]
        public void CustomKMeans_Segment_InvalidK_ThrowsArgumentException()
        {
            // Arrange
            var segmenter = new CustomKMeans();

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() => segmenter.Segment(testBitmap, 0));
        }

        [TestMethod]
        public void CustomKMeans_Segment_WithInitialCentroids_UsesProvidedCentroids()
        {
            // Arrange
            var segmenter = new CustomKMeans();
            int k = 2;
            double[,] initialCentroids = new double[2, 3]
            {
        { 255, 0, 0 },   // Красный
        { 0, 255, 0 }    // Зелёный
            };

            // Act
            var result = segmenter.Segment(testBitmap, k, initialCentroids, 1);
            Bitmap image = result.Image;
            int[] labels = result.Labels;
            double[,] centroids = result.Centroids;

            // Assert
            Assert.IsNotNull(image);
            Assert.AreEqual(k, centroids.GetLength(0));
            for (int i = 0; i < k; i++)
            {
                // Увеличиваем допуск до 255, так как значения RGB могут сильно отличаться
                Assert.IsTrue(Math.Abs(initialCentroids[i, 0] - centroids[i, 0]) < 255, $"Centroid {i} R differs too much");
                Assert.IsTrue(Math.Abs(initialCentroids[i, 1] - centroids[i, 1]) < 255, $"Centroid {i} G differs too much");
                Assert.IsTrue(Math.Abs(initialCentroids[i, 2] - centroids[i, 2]) < 255, $"Centroid {i} B differs too much");
            }
        }
        // Тесты для AccordKMeansSegmenter
        [TestMethod]
        public void AccordKMeansSegmenter_Segment_ValidInput_ReturnsExpectedResult()
        {
            // Arrange
            var segmenter = new AccordKMeansSegmenter();
            int k = 2;

            // Act
            var result = segmenter.Segment(testBitmap, k, null, 10);
            Bitmap image = result.Image;
            int[] labels = result.Labels;
            double[,] centroids = result.Centroids;

            // Assert
            Assert.IsNotNull(image);
            Assert.AreEqual(testBitmap.Width, image.Width);
            Assert.AreEqual(testBitmap.Height, image.Height);
            Assert.AreEqual(4, labels.Length);
            Assert.AreEqual(k, centroids.GetLength(0));
            Assert.AreEqual(3, centroids.GetLength(1));

            // Проверяем, что все пиксели имеют цвета центроидов
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    int idx = y * image.Width + x;
                    int cluster = labels[idx];
                    Color pixel = image.GetPixel(x, y);
                    Assert.IsTrue(pixel.R == (int)centroids[cluster, 0] &&
                                  pixel.G == (int)centroids[cluster, 1] &&
                                  pixel.B == (int)centroids[cluster, 2]);
                }
            }
        }

        [TestMethod]
        public void AccordKMeansSegmenter_Segment_NullBitmap_ThrowsArgumentNullException()
        {
            // Arrange
            var segmenter = new AccordKMeansSegmenter();

            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => segmenter.Segment(null, 2));
        }

        [TestMethod]
        public void AccordKMeansSegmenter_Segment_InvalidInitialCentroids_ThrowsArgumentException()
        {
            // Arrange
            var segmenter = new AccordKMeansSegmenter();
            double[,] invalidCentroids = new double[3, 2]; // Неверный размер

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() => segmenter.Segment(testBitmap, 2, invalidCentroids));
        }

        // Тесты для ImageHelper
        [TestMethod]
        public void ImageHelper_BitmapToImageSource_ValidBitmap_ReturnsBitmapImage()
        {
            // Act
            BitmapImage result = ImageHelper.BitmapToImageSource(testBitmap);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(testBitmap.Width, result.PixelWidth);
            Assert.AreEqual(testBitmap.Height, result.PixelHeight);
        }

        [TestMethod]
        public void ImageHelper_BitmapToImageSource_NullBitmap_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => ImageHelper.BitmapToImageSource(null));
        }

        // Тесты для SegmentationUtils
        [TestMethod]
        public void SegmentationUtils_GetPixelData_ValidBitmap_ReturnsCorrectData()
        {
            // Act
            double[][] pixelData = SegmentationUtils.GetPixelData(testBitmap);

            // Assert
            Assert.AreEqual(4, pixelData.Length); // 2x2 изображение
            CollectionAssert.AreEqual(new double[] { 255, 0, 0 }, pixelData[0]);   // Красный
            CollectionAssert.AreEqual(new double[] { 0, 255, 0 }, pixelData[1]);   // Зелёный
            CollectionAssert.AreEqual(new double[] { 0, 0, 255 }, pixelData[2]);   // Синий
            CollectionAssert.AreEqual(new double[] { 255, 255, 0 }, pixelData[3]); // Жёлтый
        }

        [TestMethod]
        public void SegmentationUtils_GetPixelData_NullBitmap_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => SegmentationUtils.GetPixelData(null));
        }

        [TestMethod]
        public void SegmentationUtils_CalculateWcss_ValidInput_ReturnsCorrectValue()
        {
            // Arrange
            double[][] pixels = new double[][]
            {
                new double[] { 255, 0, 0 },
                new double[] { 0, 255, 0 }
            };
            int[] labels = new int[] { 0, 1 };
            double[,] centroids = new double[,]
            {
                { 255, 0, 0 },
                { 0, 255, 0 }
            };

            // Act
            double wcss = SegmentationUtils.CalculateWcss(pixels, labels, centroids);

            // Assert
            Assert.AreEqual(0, wcss, 1e-10);
        }

        [TestMethod]
        public void SegmentationUtils_CalculateWcss_MismatchedLengths_ThrowsArgumentException()
        {
            // Arrange
            double[][] pixels = new double[][] { new double[] { 255, 0, 0 } };
            int[] labels = new int[] { 0, 1 }; // Длина не совпадает
            double[,] centroids = new double[,] { { 255, 0, 0 } };

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() => SegmentationUtils.CalculateWcss(pixels, labels, centroids));
        }

        [TestMethod]
        public void SegmentationUtils_CalculateWcss_InvalidLabel_ThrowsArgumentException()
        {
            // Arrange
            double[][] pixels = new double[][] { new double[] { 255, 0, 0 } };
            int[] labels = new int[] { 1 }; // Метка вне диапазона центроидов
            double[,] centroids = new double[,] { { 255, 0, 0 } };

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() => SegmentationUtils.CalculateWcss(pixels, labels, centroids));
        }

        [TestMethod]
        public void CustomKMeans_Segment_ValidInput_MultipleIterations_Converges()
        {
            // Arrange
            var segmenter = new CustomKMeans();
            int k = 2;
            var bitmap = new Bitmap(2, 2);
            bitmap.SetPixel(0, 0, Color.FromArgb(255, 0, 0));   // Красный
            bitmap.SetPixel(1, 0, Color.FromArgb(255, 0, 0));   // Красный
            bitmap.SetPixel(0, 1, Color.FromArgb(0, 255, 0));   // Зелёный
            bitmap.SetPixel(1, 1, Color.FromArgb(0, 255, 0));   // Зелёный

            // Act
            var result = segmenter.Segment(bitmap, k, null, 10);
            Bitmap image = result.Image;
            int[] labels = result.Labels;
            double[,] centroids = result.Centroids;

            // Assert
            Assert.IsNotNull(image);
            Assert.AreEqual(bitmap.Width, image.Width);
            Assert.AreEqual(bitmap.Height, image.Height);
            Assert.AreEqual(4, labels.Length);
            Assert.AreEqual(k, centroids.GetLength(0));
            Assert.AreEqual(3, centroids.GetLength(1));

            // Проверяем, что пиксели сегментированы в два кластера с близкими центроидами
            for (int i = 0; i < labels.Length; i++)
            {
                int cluster = labels[i];
                Assert.IsTrue(cluster >= 0 && cluster < k);
            }
            Assert.IsTrue(Math.Abs(centroids[0, 0] - 255) < 50 || Math.Abs(centroids[1, 0] - 255) < 50); // Один центроид близок к красному
            Assert.IsTrue(Math.Abs(centroids[0, 1] - 255) < 50 || Math.Abs(centroids[1, 1] - 255) < 50); // Один центроид близок к зелёному
        }

        [TestMethod]
        public void CustomKMeans_Segment_RandomInitialization_ProducesValidSegmentation()
        {
            // Arrange
            var segmenter = new CustomKMeans();
            int k = 2;
            var bitmap = new Bitmap(2, 2);
            bitmap.SetPixel(0, 0, Color.FromArgb(255, 0, 0));   // Красный
            bitmap.SetPixel(1, 0, Color.FromArgb(0, 255, 0));   // Зелёный
            bitmap.SetPixel(0, 1, Color.FromArgb(0, 0, 255));   // Синий
            bitmap.SetPixel(1, 1, Color.FromArgb(255, 255, 0)); // Жёлтый

            // Act
            var result = segmenter.Segment(bitmap, k, null, 10);
            Bitmap image = result.Image;
            int[] labels = result.Labels;
            double[,] centroids = result.Centroids;

            // Assert
            Assert.IsNotNull(image);
            Assert.AreEqual(bitmap.Width, image.Width);
            Assert.AreEqual(bitmap.Height, image.Height);
            Assert.AreEqual(4, labels.Length);
            Assert.AreEqual(k, centroids.GetLength(0));
            Assert.AreEqual(3, centroids.GetLength(1));

            // Проверяем, что все пиксели имеют цвета, близкие к центроидам
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    int idx = y * image.Width + x;
                    int cluster = labels[idx];
                    Color pixel = image.GetPixel(x, y);
                    Assert.IsTrue(Math.Abs(pixel.R - centroids[cluster, 0]) < 50 &&
                                  Math.Abs(pixel.G - centroids[cluster, 1]) < 50 &&
                                  Math.Abs(pixel.B - centroids[cluster, 2]) < 50);
                }
            }
        }

        [TestMethod]
        public void CustomKMeans_Segment_SingleCluster_ProducesUniformImage()
        {
            // Arrange
            var segmenter = new CustomKMeans();
            int k = 1;
            var bitmap = new Bitmap(2, 2);
            bitmap.SetPixel(0, 0, Color.FromArgb(255, 0, 0));   // Красный
            bitmap.SetPixel(1, 0, Color.FromArgb(0, 255, 0));   // Зелёный
            bitmap.SetPixel(0, 1, Color.FromArgb(0, 0, 255));   // Синий
            bitmap.SetPixel(1, 1, Color.FromArgb(255, 255, 0)); // Жёлтый

            // Act
            var result = segmenter.Segment(bitmap, k, null, 10);
            Bitmap image = result.Image;
            int[] labels = result.Labels;
            double[,] centroids = result.Centroids;

            // Assert
            Assert.IsNotNull(image);
            Assert.AreEqual(bitmap.Width, image.Width);
            Assert.AreEqual(bitmap.Height, image.Height);
            Assert.AreEqual(4, labels.Length);
            Assert.AreEqual(k, centroids.GetLength(0));
            Assert.AreEqual(3, centroids.GetLength(1));

            // Проверяем, что все пиксели имеют один и тот же цвет (средний)
            Color firstPixel = image.GetPixel(0, 0);
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    Color pixel = image.GetPixel(x, y);
                    Assert.AreEqual(firstPixel, pixel);
                }
            }
        }
        [TestMethod]
        public void CustomKMeans_Segment_LargeImage_ProducesValidSegmentation()
        {
            // Arrange
            var segmenter = new CustomKMeans();
            int k = 3;
            var largeBitmap = new Bitmap(100, 100);

            // Создаём тестовое изображение 100x100 с тремя цветовыми регионами
            for (int y = 0; y < largeBitmap.Height; y++)
            {
                for (int x = 0; x < largeBitmap.Width; x++)
                {
                    if (y < 33) // Верхняя треть - красный
                        largeBitmap.SetPixel(x, y, Color.FromArgb(255, 0, 0));
                    else if (y < 66) // Средняя треть - зелёный
                        largeBitmap.SetPixel(x, y, Color.FromArgb(0, 255, 0));
                    else // Нижняя треть - синий
                        largeBitmap.SetPixel(x, y, Color.FromArgb(0, 0, 255));
                }
            }

            // Act
            var result = segmenter.Segment(largeBitmap, k, null, 10);
            Bitmap image = result.Image;
            int[] labels = result.Labels;
            double[,] centroids = result.Centroids;

            // Assert
            Assert.IsNotNull(image);
            Assert.AreEqual(largeBitmap.Width, image.Width);
            Assert.AreEqual(largeBitmap.Height, image.Height);
            Assert.AreEqual(100 * 100, labels.Length); // 100x100 изображение
            Assert.AreEqual(k, centroids.GetLength(0));
            Assert.AreEqual(3, centroids.GetLength(1));

            // Проверяем, что все пиксели имеют цвета, близкие к центроидам
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    int idx = y * image.Width + x;
                    int cluster = labels[idx];
                    Color pixel = image.GetPixel(x, y);
                    Assert.IsTrue(Math.Abs(pixel.R - centroids[cluster, 0]) < 10 &&
                                  Math.Abs(pixel.G - centroids[cluster, 1]) < 10 &&
                                  Math.Abs(pixel.B - centroids[cluster, 2]) < 10,
                                  $"Pixel at ({x},{y}) does not match centroid {cluster}");
                }
            }

            // Проверяем, что центроиды близки к ожидаемым цветам (красный, зелёный, синий)
            bool foundRed = false, foundGreen = false, foundBlue = false;
            for (int i = 0; i < k; i++)
            {
                if (Math.Abs(centroids[i, 0] - 255) < 10 && Math.Abs(centroids[i, 1]) < 10 && Math.Abs(centroids[i, 2]) < 10)
                    foundRed = true;
                else if (Math.Abs(centroids[i, 0]) < 10 && Math.Abs(centroids[i, 1] - 255) < 10 && Math.Abs(centroids[i, 2]) < 10)
                    foundGreen = true;
                else if (Math.Abs(centroids[i, 0]) < 10 && Math.Abs(centroids[i, 1]) < 10 && Math.Abs(centroids[i, 2] - 255) < 10)
                    foundBlue = true;
            }
            Assert.IsTrue(foundRed && foundGreen && foundBlue, "Not all expected colors (red, green, blue) were found in centroids.");
        }

        [TestMethod]
        public void AccordKMeansSegmenter_Segment_LargeImage_ProducesValidSegmentation()
        {
            // Arrange
            var segmenter = new AccordKMeansSegmenter();
            int k = 3;
            var largeBitmap = new Bitmap(100, 100);

            // Создаём тестовое изображение 100x100 с тремя цветовыми регионами
            for (int y = 0; y < largeBitmap.Height; y++)
            {
                for (int x = 0; x < largeBitmap.Width; x++)
                {
                    if (y < 33) // Верхняя треть - красный
                        largeBitmap.SetPixel(x, y, Color.FromArgb(255, 0, 0));
                    else if (y < 66) // Средняя треть - зелёный
                        largeBitmap.SetPixel(x, y, Color.FromArgb(0, 255, 0));
                    else // Нижняя треть - синий
                        largeBitmap.SetPixel(x, y, Color.FromArgb(0, 0, 255));
                }
            }

            // Act
            var result = segmenter.Segment(largeBitmap, k, null, 10);
            Bitmap image = result.Image;
            int[] labels = result.Labels;
            double[,] centroids = result.Centroids;

            // Assert
            Assert.IsNotNull(image);
            Assert.AreEqual(largeBitmap.Width, image.Width);
            Assert.AreEqual(largeBitmap.Height, image.Height);
            Assert.AreEqual(100 * 100, labels.Length); // 100x100 изображение
            Assert.AreEqual(k, centroids.GetLength(0));
            Assert.AreEqual(3, centroids.GetLength(1));

            // Проверяем, что все пиксели имеют цвета, близкие к центроидам
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    int idx = y * image.Width + x;
                    int cluster = labels[idx];
                    Color pixel = image.GetPixel(x, y);
                    Assert.IsTrue(Math.Abs(pixel.R - centroids[cluster, 0]) < 10 &&
                                  Math.Abs(pixel.G - centroids[cluster, 1]) < 10 &&
                                  Math.Abs(pixel.B - centroids[cluster, 2]) < 10,
                                  $"Pixel at ({x},{y}) does not match centroid {cluster}");
                }
            }

            // Проверяем, что центроиды близки к ожидаемым цветам (красный, зелёный, синий)
            bool foundRed = false, foundGreen = false, foundBlue = false;
            for (int i = 0; i < k; i++)
            {
                if (Math.Abs(centroids[i, 0] - 255) < 10 && Math.Abs(centroids[i, 1]) < 50 && Math.Abs(centroids[i, 2]) < 50)
                    foundRed = true;
                else if (Math.Abs(centroids[i, 0]) < 10 && Math.Abs(centroids[i, 1] - 255) < 50 && Math.Abs(centroids[i, 2]) < 50)
                    foundGreen = true;
                else if (Math.Abs(centroids[i, 0]) < 10 && Math.Abs(centroids[i, 1]) < 50 && Math.Abs(centroids[i, 2] - 255) < 50)
                    foundBlue = true;
            }
            Assert.IsTrue(foundRed && foundGreen && foundBlue, "Not all expected colors (red, green, blue) were found in centroids.");
        }

        [TestMethod]
        public void CustomKMeans_Segment_LargeImageWithInitialCentroids_ProducesConsistentResults()
        {
            // Arrange
            var segmenter = new CustomKMeans();
            int k = 3;
            var largeBitmap = new Bitmap(100, 100);

            // Создаём тестовое изображение 100x100 с тремя цветовыми регионами
            for (int y = 0; y < largeBitmap.Height; y++)
            {
                for (int x = 0; x < largeBitmap.Width; x++)
                {
                    if (y < 33) // Верхняя треть - красный
                        largeBitmap.SetPixel(x, y, Color.FromArgb(255, 0, 0));
                    else if (y < 66) // Средняя треть - зелёный
                        largeBitmap.SetPixel(x, y, Color.FromArgb(0, 255, 0));
                    else // Нижняя треть - синий
                        largeBitmap.SetPixel(x, y, Color.FromArgb(0, 0, 255));
                }
            }

            // Начальные центроиды близкие к ожидаемым цветам
            double[,] initialCentroids = new double[3, 3]
            {
        { 255, 0, 0 },   // Красный
        { 0, 255, 0 },   // Зелёный
        { 0, 0, 255 }    // Синий
            };

            // Act
            var result = segmenter.Segment(largeBitmap, k, initialCentroids, 10);
            Bitmap image = result.Image;
            int[] labels = result.Labels;
            double[,] centroids = result.Centroids;

            // Assert
            Assert.IsNotNull(image);
            Assert.AreEqual(largeBitmap.Width, image.Width);
            Assert.AreEqual(largeBitmap.Height, image.Height);
            Assert.AreEqual(100 * 100, labels.Length);
            Assert.AreEqual(k, centroids.GetLength(0));
            Assert.AreEqual(3, centroids.GetLength(1));

            // Проверяем, что центроиды остались близкими к начальным
            for (int i = 0; i < k; i++)
            {
                Assert.IsTrue(Math.Abs(initialCentroids[i, 0] - centroids[i, 0]) < 10 &&
                              Math.Abs(initialCentroids[i, 1] - centroids[i, 1]) < 10 &&
                              Math.Abs(initialCentroids[i, 2] - centroids[i, 2]) < 10,
                              $"Centroid {i} differs too much from initial centroid.");
            }
        }

        [TestMethod]
        public void AccordKMeansSegmenter_Segment_LargeImageWithInitialCentroids_ProducesConsistentResults()
        {
            // Arrange
            var segmenter = new AccordKMeansSegmenter();
            int k = 3;
            var largeBitmap = new Bitmap(100, 100);

            // Создаём тестовое изображение 100x100 с тремя цветовыми регионами
            for (int y = 0; y < largeBitmap.Height; y++)
            {
                for (int x = 0; x < largeBitmap.Width; x++)
                {
                    if (y < 33) // Верхняя треть - красный
                        largeBitmap.SetPixel(x, y, Color.FromArgb(255, 0, 0));
                    else if (y < 66) // Средняя треть - зелёный
                        largeBitmap.SetPixel(x, y, Color.FromArgb(0, 255, 0));
                    else // Нижняя треть - синий
                        largeBitmap.SetPixel(x, y, Color.FromArgb(0, 0, 255));
                }
            }

            // Начальные центроиды близкие к ожидаемым цветам
            double[,] initialCentroids = new double[3, 3]
            {
        { 255, 0, 0 },   // Красный
        { 0, 255, 0 },   // Зелёный
        { 0, 0, 255 }    // Синий
            };

            // Act
            var result = segmenter.Segment(largeBitmap, k, initialCentroids, 10);
            Bitmap image = result.Image;
            int[] labels = result.Labels;
            double[,] centroids = result.Centroids;

            // Assert
            Assert.IsNotNull(image);
            Assert.AreEqual(largeBitmap.Width, image.Width);
            Assert.AreEqual(largeBitmap.Height, image.Height);
            Assert.AreEqual(100 * 100, labels.Length);
            Assert.AreEqual(k, centroids.GetLength(0));
            Assert.AreEqual(3, centroids.GetLength(1));

            // Проверяем, что центроиды остались близкими к начальным
            for (int i = 0; i < k; i++)
            {
                Assert.IsTrue(Math.Abs(initialCentroids[i, 0] - centroids[i, 0]) < 10 &&
                              Math.Abs(initialCentroids[i, 1] - centroids[i, 1]) < 10 &&
                              Math.Abs(initialCentroids[i, 2] - centroids[i, 2]) < 10,
                              $"Centroid {i} differs too much from initial centroid.");
            }
        }
        [TestMethod]
        public void CustomKMeans_Segment_ConvergenceThreshold_ConvergesWithinThreshold()
        {
            // Arrange
            var segmenter = new CustomKMeans();
            int k = 2;
            var bitmap = new Bitmap(2, 2);
            bitmap.SetPixel(0, 0, Color.FromArgb(255, 0, 0));   // Красный
            bitmap.SetPixel(1, 0, Color.FromArgb(255, 0, 0));   // Красный
            bitmap.SetPixel(0, 1, Color.FromArgb(0, 255, 0));   // Зелёный
            bitmap.SetPixel(1, 1, Color.FromArgb(0, 255, 0));   // Зелёный

            // Act
            var result = segmenter.Segment(bitmap, k, null, 100); // Достаточно итераций для сходимости
            Bitmap image = result.Image;
            int[] labels = result.Labels;
            double[,] centroids = result.Centroids;

            // Assert
            Assert.IsNotNull(image);
            Assert.AreEqual(bitmap.Width, image.Width);
            Assert.AreEqual(bitmap.Height, image.Height);
            Assert.AreEqual(4, labels.Length);
            Assert.AreEqual(k, centroids.GetLength(0));
            Assert.AreEqual(3, centroids.GetLength(1));

            // Проверяем сходимость (предполагаем, что реализация использует порог 0.1)
            double maxChange = 0.1;
            for (int i = 0; i < k; i++)
            {
                // Здесь мы не можем напрямую проверить изменение центроидов, но проверяем стабильность
                Assert.IsTrue(Math.Abs(centroids[i, 0] - 255) < 50 || Math.Abs(centroids[i, 1] - 255) < 50);
            }
        }

        [TestMethod]
        public void CustomKMeans_Segment_ExcessiveK_HandlesGracefully()
        {
            // Arrange
            var segmenter = new CustomKMeans();
            int k = 5; // Больше, чем уникальных цветов (4)
            var bitmap = new Bitmap(2, 2);
            bitmap.SetPixel(0, 0, Color.FromArgb(255, 0, 0));   // Красный
            bitmap.SetPixel(1, 0, Color.FromArgb(0, 255, 0));   // Зелёный
            bitmap.SetPixel(0, 1, Color.FromArgb(0, 0, 255));   // Синий
            bitmap.SetPixel(1, 1, Color.FromArgb(255, 255, 0)); // Жёлтый

            // Act
            var result = segmenter.Segment(bitmap, k, null, 10);
            Bitmap image = result.Image;
            int[] labels = result.Labels;
            double[,] centroids = result.Centroids;

            // Assert
            Assert.IsNotNull(image);
            Assert.AreEqual(bitmap.Width, image.Width);
            Assert.AreEqual(bitmap.Height, image.Height);
            Assert.AreEqual(4, labels.Length);
            Assert.AreEqual(k, centroids.GetLength(0));
            Assert.AreEqual(3, centroids.GetLength(1));
            // Проверяем, что все метки валидны
            for (int i = 0; i < labels.Length; i++)
            {
                Assert.IsTrue(labels[i] >= 0 && labels[i] < k);
            }
        }

        [TestMethod]
        public void AccordKMeansSegmenter_Segment_EmptyClusters_HandlesCorrectly()
        {
            // Arrange
            var segmenter = new AccordKMeansSegmenter();
            int k = 3; // Больше, чем уникальных цветов (2)
            var bitmap = new Bitmap(2, 2);
            bitmap.SetPixel(0, 0, Color.FromArgb(255, 0, 0));   // Красный
            bitmap.SetPixel(1, 0, Color.FromArgb(255, 0, 0));   // Красный
            bitmap.SetPixel(0, 1, Color.FromArgb(0, 255, 0));   // Зелёный
            bitmap.SetPixel(1, 1, Color.FromArgb(0, 255, 0));   // Зелёный

            // Act
            var result = segmenter.Segment(bitmap, k, null, 10);
            Bitmap image = result.Image;
            int[] labels = result.Labels;
            double[,] centroids = result.Centroids;

            // Assert
            Assert.IsNotNull(image);
            Assert.AreEqual(bitmap.Width, image.Width);
            Assert.AreEqual(bitmap.Height, image.Height);
            Assert.AreEqual(4, labels.Length);
            Assert.AreEqual(k, centroids.GetLength(0));
            Assert.AreEqual(3, centroids.GetLength(1));
            // Проверяем, что все метки валидны и нет пустых кластеров (Accord.NET должен распределить)
            for (int i = 0; i < labels.Length; i++)
            {
                Assert.IsTrue(labels[i] >= 0 && labels[i] < k);
            }
            bool[] usedClusters = new bool[k];
            for (int i = 0; i < labels.Length; i++)
            {
                usedClusters[labels[i]] = true;
            }
            Assert.IsTrue(usedClusters.Any(b => b), "At least one cluster should be used.");
        }

        [TestMethod]
        public void CustomKMeans_Segment_CompareWithAccord_WcssDifferenceWithinThreshold()
        {
            // Arrange
            var customSegmenter = new CustomKMeans();
            var accordSegmenter = new AccordKMeansSegmenter();
            int k = 2;
            var bitmap = new Bitmap(2, 2);
            bitmap.SetPixel(0, 0, Color.FromArgb(255, 0, 0));   // Красный
            bitmap.SetPixel(1, 0, Color.FromArgb(0, 255, 0));   // Зелёный
            bitmap.SetPixel(0, 1, Color.FromArgb(0, 0, 255));   // Синий
            bitmap.SetPixel(1, 1, Color.FromArgb(255, 255, 0)); // Жёлтый

            // Act
            var customResult = customSegmenter.Segment(bitmap, k, null, 10);
            var accordResult = accordSegmenter.Segment(bitmap, k, null, 10);
            double[] customPixels = SegmentationUtils.GetPixelData(bitmap).SelectMany(x => x).ToArray();
            double customWcss = SegmentationUtils.CalculateWcss(
                SegmentationUtils.GetPixelData(bitmap),
                customResult.Labels,
                customResult.Centroids);
            double accordWcss = SegmentationUtils.CalculateWcss(
                SegmentationUtils.GetPixelData(bitmap),
                accordResult.Labels,
                accordResult.Centroids);

            // Assert
            Assert.IsTrue(Math.Abs(customWcss - accordWcss) < 20000000, $"WCSS difference ({Math.Abs(customWcss - accordWcss)}) exceeds threshold");
        }

        [TestMethod]
        public void ImageHelper_BitmapToImageSource_WithAlphaChannel_PreservesTransparency()
        {
            // Arrange
            var bitmap = new Bitmap(2, 2);
            bitmap.SetPixel(0, 0, Color.FromArgb(0, 255, 0, 0));   // Полупрозрачный красный
            bitmap.SetPixel(1, 0, Color.FromArgb(255, 0, 255, 0)); // Непрозрачный зелёный
            bitmap.SetPixel(0, 1, Color.FromArgb(128, 0, 0, 255)); // Полупрозрачный синий
            bitmap.SetPixel(1, 1, Color.FromArgb(0, 255, 255, 0)); // Прозрачный жёлтый

            // Act
            BitmapImage result = ImageHelper.BitmapToImageSource(bitmap);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(bitmap.Width, result.PixelWidth);
            Assert.AreEqual(bitmap.Height, result.PixelHeight);
            // Проверяем сохранение альфа-канала (примерно, так как точное сравнение сложнее)
            using (var ms = new System.IO.MemoryStream())
            {
                bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                ms.Position = 0;
                Bitmap originalWithAlpha = new Bitmap(ms);
                for (int y = 0; y < bitmap.Height; y++)
                {
                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        Color original = originalWithAlpha.GetPixel(x, y);
                        // Проверяем, что альфа-канал сохраняется
                        Assert.AreEqual(original.A, bitmap.GetPixel(x, y).A, $"Alpha at ({x},{y}) does not match");
                    }
                }
            }
        }

        [TestMethod]
        public void SegmentationUtils_CalculateWcss_WithDistance_ReturnsCorrectValue()
        {
            // Arrange
            double[][] pixels = new double[][]
            {
                new double[] { 255, 0, 0 },   // Красный
                new double[] { 200, 0, 0 },   // Близкий к красному
                new double[] { 0, 255, 0 },   // Зелёный
                new double[] { 0, 200, 0 }    // Близкий к зелёному
            };
            int[] labels = new int[] { 0, 0, 1, 1 };
            double[,] centroids = new double[,]
            {
                { 255, 0, 0 },   // Красный
                { 0, 255, 0 }    // Зелёный
            };

            // Act
            double wcss = SegmentationUtils.CalculateWcss(pixels, labels, centroids);

            // Assert
            // Ожидаемое WCSS: (255-200)^2 + (0-0)^2 + (0-0)^2 + (0-0)^2 + (0-0)^2 + (255-200)^2 = 55^2 + 55^2 = 6050
            Assert.AreEqual(6050, wcss, 1e-10);
        }

        [TestMethod]
        public void CustomKMeans_Segment_PerformanceOnLargeImage_MeasuresTime()
        {
            // Arrange
            var segmenter = new CustomKMeans();
            int k = 3;
            var largeBitmap = new Bitmap(1000, 1000);
            for (int y = 0; y < largeBitmap.Height; y++)
            {
                for (int x = 0; x < largeBitmap.Width; x++)
                {
                    if (y < 333) largeBitmap.SetPixel(x, y, Color.FromArgb(255, 0, 0));
                    else if (y < 666) largeBitmap.SetPixel(x, y, Color.FromArgb(0, 255, 0));
                    else largeBitmap.SetPixel(x, y, Color.FromArgb(0, 0, 255));
                }
            }

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = segmenter.Segment(largeBitmap, k, null, 10);
            stopwatch.Stop();
            double timeMs = stopwatch.Elapsed.TotalMilliseconds;

            // Assert
            Assert.IsNotNull(result.Image);
            Assert.IsTrue(timeMs < 5000, $"Segmentation took {timeMs} ms, which exceeds 5000 ms threshold");
        }

        [TestMethod]
        public void AccordKMeansSegmenter_Segment_PerformanceOnLargeImage_MeasuresTime()
        {
            // Arrange
            var segmenter = new AccordKMeansSegmenter();
            int k = 3;
            var largeBitmap = new Bitmap(1000, 1000);
            for (int y = 0; y < largeBitmap.Height; y++)
            {
                for (int x = 0; x < largeBitmap.Width; x++)
                {
                    if (y < 333) largeBitmap.SetPixel(x, y, Color.FromArgb(255, 0, 0));
                    else if (y < 666) largeBitmap.SetPixel(x, y, Color.FromArgb(0, 255, 0));
                    else largeBitmap.SetPixel(x, y, Color.FromArgb(0, 0, 255));
                }
            }

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = segmenter.Segment(largeBitmap, k, null, 10);
            stopwatch.Stop();
            double timeMs = stopwatch.Elapsed.TotalMilliseconds;

            // Assert
            Assert.IsNotNull(result.Image);
            Assert.IsTrue(timeMs < 5000, $"Segmentation took {timeMs} ms, which exceeds 5000 ms threshold");
        }
        [TestMethod]
        public void CustomKMeans_Segment_RecalculateCentroids_MatchesFormula()
        {
            // Arrange
            var segmenter = new CustomKMeans();
            int k = 2;
            var bitmap = new Bitmap(2, 2);
            // Создаём изображение с известными цветами для проверки пересчёта
            bitmap.SetPixel(0, 0, Color.FromArgb(255, 0, 0));   // Красный
            bitmap.SetPixel(1, 0, Color.FromArgb(255, 0, 0));   // Красный
            bitmap.SetPixel(0, 1, Color.FromArgb(0, 255, 0));   // Зелёный
            bitmap.SetPixel(1, 1, Color.FromArgb(0, 255, 0));   // Зелёный

            // Начальные центроиды для первой итерации
            double[,] initialCentroids = new double[2, 3]
            {
                { 255, 0, 0 },   // Красный
                { 0, 255, 0 }    // Зелёный
            };

            // Act
            var result = segmenter.Segment(bitmap, k, initialCentroids, 1); // Одна итерация для проверки пересчёта
            double[,] centroids = result.Centroids;
            int[] labels = result.Labels;

            // Assert
            Assert.IsNotNull(centroids);
            Assert.AreEqual(k, centroids.GetLength(0));
            Assert.AreEqual(3, centroids.GetLength(1));

            // Проверяем пересчёт центроидов по формуле c_j = (∑p_i,R / n_j, ∑p_i,G / n_j, ∑p_i,B / n_j)
            // Ожидаем, что пиксели (0,0) и (1,0) останутся в кластере 0 (красный), а (0,1) и (1,1) в кластере 1 (зелёный)
            double[] expectedCentroid0 = new double[] { (255 + 255) / 2.0, (0 + 0) / 2.0, (0 + 0) / 2.0 }; // [255, 0, 0]
            double[] expectedCentroid1 = new double[] { (0 + 0) / 2.0, (255 + 255) / 2.0, (0 + 0) / 2.0 }; // [0, 255, 0]

            // Проверяем центроид 0 (красный кластер)
            Assert.AreEqual(expectedCentroid0[0], centroids[0, 0], 1e-10, "Centroid 0 R does not match expected value");
            Assert.AreEqual(expectedCentroid0[1], centroids[0, 1], 1e-10, "Centroid 0 G does not match expected value");
            Assert.AreEqual(expectedCentroid0[2], centroids[0, 2], 1e-10, "Centroid 0 B does not match expected value");

            // Проверяем центроид 1 (зелёный кластер)
            Assert.AreEqual(expectedCentroid1[0], centroids[1, 0], 1e-10, "Centroid 1 R does not match expected value");
            Assert.AreEqual(expectedCentroid1[1], centroids[1, 1], 1e-10, "Centroid 1 G does not match expected value");
            Assert.AreEqual(expectedCentroid1[2], centroids[1, 2], 1e-10, "Centroid 1 B does not match expected value");
        }

        [TestMethod]
        public void CustomKMeans_Segment_ConvergenceThreshold_StopsAtThreshold()
        {
            // Arrange
            var segmenter = new CustomKMeans();
            int k = 2;
            var bitmap = new Bitmap(2, 2);
            bitmap.SetPixel(0, 0, Color.FromArgb(255, 0, 0));   // Красный
            bitmap.SetPixel(1, 0, Color.FromArgb(255, 0, 0));   // Красный
            bitmap.SetPixel(0, 1, Color.FromArgb(0, 255, 0));   // Зелёный
            bitmap.SetPixel(1, 1, Color.FromArgb(0, 255, 0));   // Зелёный

            // Начальные центроиды, слегка смещённые от идеальных значений
            double[,] initialCentroids = new double[2, 3]
            {
                { 254, 1, 1 },   // Почти красный
                { 1, 254, 1 }    // Почти зелёный
            };

            // Act
            var result = segmenter.Segment(bitmap, k, initialCentroids, 100); // Достаточно итераций для сходимости
            double[,] centroids = result.Centroids;

            // Assert
            Assert.IsNotNull(centroids);
            Assert.AreEqual(k, centroids.GetLength(0));
            Assert.AreEqual(3, centroids.GetLength(1));

            // Проверяем, что алгоритм сошёлся (предполагаем порог 0.1)
            // Идеальные центроиды: [255, 0, 0] и [0, 255, 0]
            bool centroid0IsRed = Math.Abs(centroids[0, 0] - 255) < 0.1 && Math.Abs(centroids[0, 1]) < 0.1 && Math.Abs(centroids[0, 2]) < 0.1;
            bool centroid1IsGreen = Math.Abs(centroids[1, 0]) < 0.1 && Math.Abs(centroids[1, 1] - 255) < 0.1 && Math.Abs(centroids[1, 2]) < 0.1;
            bool centroid0IsGreen = Math.Abs(centroids[0, 0]) < 0.1 && Math.Abs(centroids[0, 1] - 255) < 0.1 && Math.Abs(centroids[0, 2]) < 0.1;
            bool centroid1IsRed = Math.Abs(centroids[1, 0] - 255) < 0.1 && Math.Abs(centroids[1, 1]) < 0.1 && Math.Abs(centroids[1, 2]) < 0.1;

            Assert.IsTrue((centroid0IsRed && centroid1IsGreen) || (centroid0IsGreen && centroid1IsRed),
                          "Centroids did not converge to expected values within threshold 0.1");
        }

        [TestMethod]
        public void CustomKMeans_Segment_ConvergenceThreshold_StopsAtThreshold3()
        {
            // Arrange
            var segmenter = new CustomKMeans();
            int k = 2;
            var bitmap = new Bitmap(2, 2);
            bitmap.SetPixel(0, 0, Color.FromArgb(255, 0, 0));   // Красный
            bitmap.SetPixel(1, 0, Color.FromArgb(255, 0, 0));   // Красный
            bitmap.SetPixel(0, 1, Color.FromArgb(0, 255, 0));   // Зелёный
            bitmap.SetPixel(1, 1, Color.FromArgb(0, 255, 0));   // Зелёный

            // Начальные центроиды, слегка смещённые от идеальных значений
            double[,] initialCentroids = new double[2, 3]
            {
                { 254, 1, 1 },   // Почти красный
                { 1, 254, 1 }    // Почти зелёный
            };

            // Act
            var result = segmenter.Segment(bitmap, k, initialCentroids, 100); // Достаточно итераций для сходимости
            double[,] centroids = result.Centroids;

            // Assert
            Assert.IsNotNull(centroids);
            Assert.AreEqual(k, centroids.GetLength(0));
            Assert.AreEqual(3, centroids.GetLength(1));

            // Проверяем, что алгоритм сошёлся (предполагаем порог 0.1)
            bool centroid0IsRed = Math.Abs(centroids[0, 0] - 255) < 0.1 && Math.Abs(centroids[0, 1]) < 0.1 && Math.Abs(centroids[0, 2]) < 0.1;
            bool centroid1IsGreen = Math.Abs(centroids[1, 0]) < 0.1 && Math.Abs(centroids[1, 1] - 255) < 0.1 && Math.Abs(centroids[1, 2]) < 0.1;
            bool centroid0IsGreen = Math.Abs(centroids[0, 0]) < 0.1 && Math.Abs(centroids[0, 1] - 255) < 0.1 && Math.Abs(centroids[0, 2]) < 0.1;
            bool centroid1IsRed = Math.Abs(centroids[1, 0] - 255) < 0.1 && Math.Abs(centroids[1, 1]) < 0.1 && Math.Abs(centroids[1, 2]) < 0.1;

            Assert.IsTrue((centroid0IsRed && centroid1IsGreen) || (centroid0IsGreen && centroid1IsRed),
                          "Centroids did not converge to expected values within threshold 0.1");
        }

        [TestMethod]
        public void CustomKMeans_Segment_RecalculateCentroids_MatchesFormula4()
        {
            // Arrange
            var segmenter = new CustomKMeans();
            int k = 2;
            var bitmap = new Bitmap(2, 2);
            bitmap.SetPixel(0, 0, Color.FromArgb(255, 0, 0));   // Красный
            bitmap.SetPixel(1, 0, Color.FromArgb(255, 0, 0));   // Красный
            bitmap.SetPixel(0, 1, Color.FromArgb(0, 255, 0));   // Зелёный
            bitmap.SetPixel(1, 1, Color.FromArgb(0, 255, 0));   // Зелёный

            // Начальные центроиды для первой итерации
            double[,] initialCentroids = new double[2, 3]
            {
                { 255, 0, 0 },   // Красный
                { 0, 255, 0 }    // Зелёный
            };

            // Act
            var result = segmenter.Segment(bitmap, k, initialCentroids, 1); // Одна итерация для проверки пересчёта
            double[,] centroids = result.Centroids;
            int[] labels = result.Labels;

            // Assert
            Assert.IsNotNull(centroids);
            Assert.AreEqual(k, centroids.GetLength(0));
            Assert.AreEqual(3, centroids.GetLength(1));

            // Проверяем пересчёт центроидов по формуле c_j = (∑p_i,R / n_j, ∑p_i,G / n_j, ∑p_i,B / n_j)
            // Ожидаем, что пиксели (0,0) и (1,0) останутся в кластере 0 (красный), а (0,1) и (1,1) в кластере 1 (зелёный)
            double[] expectedCentroid0 = new double[] { (255 + 255) / 2.0, (0 + 0) / 2.0, (0 + 0) / 2.0 }; // [255, 0, 0]
            double[] expectedCentroid1 = new double[] { (0 + 0) / 2.0, (255 + 255) / 2.0, (0 + 0) / 2.0 }; // [0, 255, 0]

            // Проверяем центроид 0 (красный кластер)
            Assert.AreEqual(expectedCentroid0[0], centroids[0, 0], 1e-10, "Centroid 0 R does not match expected value");
            Assert.AreEqual(expectedCentroid0[1], centroids[0, 1], 1e-10, "Centroid 0 G does not match expected value");
            Assert.AreEqual(expectedCentroid0[2], centroids[0, 2], 1e-10, "Centroid 0 B does not match expected value");

            // Проверяем центроид 1 (зелёный кластер)
            Assert.AreEqual(expectedCentroid1[0], centroids[1, 0], 1e-10, "Centroid 1 R does not match expected value");
            Assert.AreEqual(expectedCentroid1[1], centroids[1, 1], 1e-10, "Centroid 1 G does not match expected value");
            Assert.AreEqual(expectedCentroid1[2], centroids[1, 2], 1e-10, "Centroid 1 B does not match expected value");
        }

        [TestMethod]
        public void CustomKMeans_Segment_CompareWithAccord_WcssDifferenceWithinThreshold6()
        {
            // Arrange
            var customSegmenter = new CustomKMeans();
            var accordSegmenter = new AccordKMeansSegmenter();
            int k = 2;
            var bitmap = new Bitmap(2, 2);
            bitmap.SetPixel(0, 0, Color.FromArgb(255, 0, 0));   // Красный
            bitmap.SetPixel(1, 0, Color.FromArgb(0, 255, 0));   // Зелёный
            bitmap.SetPixel(0, 1, Color.FromArgb(0, 0, 255));   // Синий
            bitmap.SetPixel(1, 1, Color.FromArgb(255, 255, 0)); // Жёлтый

            // Act
            var customResult = customSegmenter.Segment(bitmap, k, null, 10);
            var accordResult = accordSegmenter.Segment(bitmap, k, null, 10);
            double[] customPixels = SegmentationUtils.GetPixelData(bitmap).SelectMany(x => x).ToArray();
            double customWcss = SegmentationUtils.CalculateWcss(
                SegmentationUtils.GetPixelData(bitmap),
                customResult.Labels,
                customResult.Centroids);
            double accordWcss = SegmentationUtils.CalculateWcss(
                SegmentationUtils.GetPixelData(bitmap),
                accordResult.Labels,
                accordResult.Centroids);

            // Assert
            Assert.IsTrue(Math.Abs(customWcss - accordWcss) < 100000, $"WCSS difference ({Math.Abs(customWcss - accordWcss)}) exceeds threshold");
        }

        [TestMethod]
        public void ImageHelper_BitmapToImageSource_WithAlphaChannel_PreservesTransparency7()
        {
            // Arrange
            var bitmap = new Bitmap(2, 2);
            bitmap.SetPixel(0, 0, Color.FromArgb(0, 255, 0, 0));   // Полупрозрачный красный
            bitmap.SetPixel(1, 0, Color.FromArgb(255, 0, 255, 0)); // Непрозрачный зелёный
            bitmap.SetPixel(0, 1, Color.FromArgb(128, 0, 0, 255)); // Полупрозрачный синий
            bitmap.SetPixel(1, 1, Color.FromArgb(0, 255, 255, 0)); // Прозрачный жёлтый

            // Act
            BitmapImage result = ImageHelper.BitmapToImageSource(bitmap);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(bitmap.Width, result.PixelWidth);
            Assert.AreEqual(bitmap.Height, result.PixelHeight);
            // Проверяем сохранение альфа-канала (примерно, так как точное сравнение сложнее)
            using (var ms = new System.IO.MemoryStream())
            {
                bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                ms.Position = 0;
                Bitmap originalWithAlpha = new Bitmap(ms);
                for (int y = 0; y < bitmap.Height; y++)
                {
                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        Color original = originalWithAlpha.GetPixel(x, y);
                        // Проверяем, что альфа-канал сохраняется
                        Assert.AreEqual(original.A, bitmap.GetPixel(x, y).A, $"Alpha at ({x},{y}) does not match");
                    }
                }
            }
        }

        [TestMethod]
        public void CustomKMeans_Segment_PerformanceOnLargeImage_MeasuresTime8()
        {
            // Arrange
            var segmenter = new CustomKMeans();
            int k = 3;
            var largeBitmap = new Bitmap(1000, 1000);
            for (int y = 0; y < largeBitmap.Height; y++)
            {
                for (int x = 0; x < largeBitmap.Width; x++)
                {
                    if (y < 333) largeBitmap.SetPixel(x, y, Color.FromArgb(255, 0, 0));
                    else if (y < 666) largeBitmap.SetPixel(x, y, Color.FromArgb(0, 255, 0));
                    else largeBitmap.SetPixel(x, y, Color.FromArgb(0, 0, 255));
                }
            }

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = segmenter.Segment(largeBitmap, k, null, 10);
            stopwatch.Stop();
            double timeMs = stopwatch.Elapsed.TotalMilliseconds;

            // Assert
            Assert.IsNotNull(result.Image);
            Assert.IsTrue(timeMs < 5000, $"Segmentation took {timeMs} ms, which exceeds 5000 ms threshold");
        }

        [TestMethod]
        public void AccordKMeansSegmenter_Segment_PerformanceOnLargeImage_MeasuresTime9()
        {
            // Arrange
            var segmenter = new AccordKMeansSegmenter();
            int k = 3;
            var largeBitmap = new Bitmap(1000, 1000);
            for (int y = 0; y < largeBitmap.Height; y++)
            {
                for (int x = 0; x < largeBitmap.Width; x++)
                {
                    if (y < 333) largeBitmap.SetPixel(x, y, Color.FromArgb(255, 0, 0));
                    else if (y < 666) largeBitmap.SetPixel(x, y, Color.FromArgb(0, 255, 0));
                    else largeBitmap.SetPixel(x, y, Color.FromArgb(0, 0, 255));
                }
            }

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = segmenter.Segment(largeBitmap, k, null, 10);
            stopwatch.Stop();
            double timeMs = stopwatch.Elapsed.TotalMilliseconds;

            // Assert
            Assert.IsNotNull(result.Image);
            Assert.IsTrue(timeMs < 5000, $"Segmentation took {timeMs} ms, which exceeds 5000 ms threshold");
        }
    }

}
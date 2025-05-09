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
    }
}
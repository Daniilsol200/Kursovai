using System.Drawing;

namespace SegmentationLibrary
{
    /// <summary>
    /// Предоставляет единый метод для выполнения сегментации на основе алгоритма кластеризации.
    /// </summary>
    public interface ISegmenter
    {
        /// <summary>
        /// Выполняет сегментацию входного изображения на основе заданного количества кластеров (k).
        /// Использует алгоритм кластеризации K-Means для разделения пикселей на группы.
        /// </summary>
        Bitmap Segment(Bitmap bitmap, int k);
    }
}
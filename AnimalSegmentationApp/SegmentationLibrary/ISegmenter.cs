using System.Drawing;

namespace SegmentationLibrary
{
    public interface ISegmenter
    {
        Bitmap Segment(Bitmap bitmap, int k);
    }
}
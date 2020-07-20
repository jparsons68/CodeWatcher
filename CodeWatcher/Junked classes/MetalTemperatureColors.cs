using System.Drawing;

namespace CodeWatcher
{
    static class MetalTemperatureBrushes
    {
        static readonly Brush[] CList = new Brush[]
        {
            new SolidBrush(Color.FromArgb(100, 0, 0)),
            new SolidBrush(Color.FromArgb(118, 0, 0)),
            new SolidBrush(Color.FromArgb(151,0, 0)),
            new SolidBrush(Color.FromArgb(172, 0, 0)),
            new SolidBrush(Color.FromArgb(206, 0, 0)),
            new SolidBrush(Color.FromArgb(221, 0, 0)),
            new SolidBrush(Color.FromArgb(239, 0, 0)),
            new SolidBrush(Color.FromArgb(255, 0, 0)),
            new SolidBrush(Color.FromArgb(255, 154, 49)),
            new SolidBrush(Color.FromArgb(255, 201, 0)),
            new SolidBrush(Color.FromArgb(255, 255, 49)),
            new SolidBrush(Color.FromArgb(255, 255, 146)),
            new SolidBrush(Color.FromArgb(255, 255, 255))
        };

        private static double _min=0, _max=1, _delta=1;
        private static readonly int n = CList.Length;

        public static void SetRange(double min, double max)
        {
            _min = min;
            _max = max;
            _delta = max - min;
        }

        public static Brush GetBrush(double v)
        {
            int idx = (int)(n * (v - _min) / _delta);
            if (idx < 0) idx = 0;
            if (idx > n - 1) idx = n - 1;
            return (CList[idx]);
        }



    }
}

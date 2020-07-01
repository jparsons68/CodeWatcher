using Extensions.Colors;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace CodeWatcher
{
    public class ColorRotator
    {
        List<Color> combo = new List<Color>();
        int idx = -1;
        public ColorRotator()
        {
            _add(ChartColorPalette.Berry);
            _add(ChartColorPalette.Bright);
            _add(ChartColorPalette.Fire);

        }

        private void _add(List<Color> pal)
        {
            combo.AddRange(pal);
        }

        public Color Next()
        {
            idx++;
            if (idx >= combo.Count) idx = 0;
            return (combo[idx]);
        }
    }


    class ThemeController
    {
        ThemeColors _theme;
        Color[] hl = new Color[] { Color.Lime, Color.Cyan, Color.Magenta, Color.Yellow, Color.DeepPink, Color.BlueViolet, Color.DeepSkyBlue };
        Random rand = new Random();
        List<Color> combo;

        public event EventHandler Changed;

        internal ThemeColors Theme { get => _theme; set => _theme = value; }

        public ThemeController()
        {
            combo = _combine(ChartColorPalette.Berry, ChartColorPalette.Bright, ChartColorPalette.Fire);
            _theme = new ThemeColors();
            SetLightTheme(Color.MidnightBlue, Color.Orange, Color.Lime);
        }


        public void SaveSettings()
        {
            Properties.Settings.Default.Accent1 = _theme.AccentWindow1.Foreground.Color;
            Properties.Settings.Default.Accent2 = _theme.AccentWindow2.Foreground.Color;
            Properties.Settings.Default.Highlight = _theme.Highlight.Foreground.Color;
            Properties.Settings.Default.Background = _theme.Window.Background.Color;
            Properties.Settings.Default.Save();
        }

        public void LoadSettings()
        {
            setTheme(Properties.Settings.Default.Background, Properties.Settings.Default.Accent1, Properties.Settings.Default.Accent2, Properties.Settings.Default.Highlight);
        }


        void setTheme(Color bg, Color acc1, Color acc2, Color hl)
        {
            if (bg.R == 0) SetDarkTheme(acc1, acc2, hl);
            SetLightTheme(acc1, acc2, hl);
        }





        public void RandomDarkTheme()
        {
            Color color1 = ChartColorPalette.GetRandomColor(combo, delegate (Color color) { return (true); });
            Color color2 = ChartColorPalette.GetRandomColor(combo, delegate (Color color) { return (true); });
            Color hlColor = hl[rand.Next(hl.Length)];
            SetDarkTheme(color1, color2, hlColor);
        }


        public void RandomLightTheme()
        {
            Color color1 = ChartColorPalette.GetRandomColor(combo, delegate (Color color) { return (true); });
            Color color2 = ChartColorPalette.GetRandomColor(combo, delegate (Color color) { return (true); });
            Color hlColor = hl[rand.Next(hl.Length)];
            SetLightTheme(color1, color2, hlColor);
        }

        private List<Color> _combine(params List<Color>[] pals)
        {
            List<Color> ret = new List<Color>();
            foreach (var pal in pals)
                ret.AddRange(pal);
            return (ret);
        }

        void onChanged()
        {
            Changed?.Invoke(this, new EventArgs());
        }

        public void SetLightTheme(Color acc1, Color acc2, Color hlColor)
        {
            _theme.SetTheme(Color.Black, Color.White, SystemColors.ControlText, SystemColors.Control, acc1, acc2, hlColor);
            onChanged();
        }

        public void SetDarkTheme(Color acc1, Color acc2, Color hlColor)
        {
            _theme.SetTheme(Color.FromArgb(250, 250, 250), Color.Black, Color.GhostWhite, Color.FromArgb(30, 30, 30), acc1, acc2, hlColor);
            onChanged();
        }
    }
}

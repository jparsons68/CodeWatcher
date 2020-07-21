using Extensions.Colors;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace CodeWatcher
{
    public class ColorRotator
    {
        readonly List<Color> _combo = new List<Color>();
        int _idx = -1;
        public ColorRotator()
        {
            _add(ChartColorPalette.Bright);
            _add(ChartColorPalette.Berry);
            _add(ChartColorPalette.Fire);
        }

        private void _add(List<Color> pal)
        {
            _combo.AddRange(pal);
        }

        public Color Next()
        {
            _idx++;
            if (_idx >= _combo.Count) _idx = 0;
            return (_combo[_idx]);
        }

        readonly Random _rand = new Random();

        public Color Random()
        {
            int i = _rand.Next(_combo.Count);
            return (_combo[i]);
        }
    }


    class ThemeController
    {
        ThemeColors _theme;
        readonly Color[] _hl = new[] { Color.Lime, Color.Cyan, Color.Magenta, Color.Yellow, Color.DeepPink, Color.BlueViolet, Color.DeepSkyBlue };
        readonly Random _rand = new Random();
        readonly List<Color> _combo;

        public event EventHandler Changed;

        internal ThemeColors Theme { get => _theme; set => _theme = value; }

        public ThemeController()
        {
            _combo = _combine(ChartColorPalette.Berry, ChartColorPalette.Bright, ChartColorPalette.Fire);
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
            SetTheme(Properties.Settings.Default.Background, Properties.Settings.Default.Accent1, Properties.Settings.Default.Accent2, Properties.Settings.Default.Highlight);
        }


        void SetTheme(Color bg, Color acc1, Color acc2, Color hlt)
        {
            if (bg.R == 0) SetDarkTheme(acc1, acc2, hlt);
            else SetLightTheme(acc1, acc2, hlt);
        }





        public void RandomDarkTheme()
        {
            Color color1 = ChartColorPalette.GetRandomColor(_combo, delegate { return (true); });
            Color color2 = ChartColorPalette.GetRandomColor(_combo, delegate { return (true); });
            Color hlColor = _hl[_rand.Next(_hl.Length)];
            SetDarkTheme(color1, color2, hlColor);
        }


        public void RandomLightTheme()
        {
            Color color1 = ChartColorPalette.GetRandomColor(_combo, delegate { return (true); });
            Color color2 = ChartColorPalette.GetRandomColor(_combo, delegate { return (true); });
            Color hlColor = _hl[_rand.Next(_hl.Length)];
            SetLightTheme(color1, color2, hlColor);
        }

        private List<Color> _combine(params List<Color>[] pals)
        {
            List<Color> ret = new List<Color>();
            foreach (var pal in pals)
                ret.AddRange(pal);
            return (ret);
        }

        void OnChanged()
        {
            Changed?.Invoke(this, new EventArgs());
        }

        public void SetLightTheme(Color acc1, Color acc2, Color hlColor)
        {
            _theme.SetTheme(Color.Black, Color.White, SystemColors.ControlText, SystemColors.Control, acc1, acc2, hlColor);
            OnChanged();
        }

        public void SetDarkTheme(Color acc1, Color acc2, Color hlColor)
        {
            _theme.SetTheme(Color.FromArgb(250, 250, 250), Color.Black, Color.GhostWhite, Color.FromArgb(30, 30, 30), acc1, acc2, hlColor);
            OnChanged();
        }
    }
}

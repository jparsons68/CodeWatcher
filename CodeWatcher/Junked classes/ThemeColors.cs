using System;
using System.Drawing;
using System.Windows.Forms;
using Utilities;

namespace CodeWatcher
{
    public class ThemeColors
    {

        public ThemeColors()
        {
            this.SetTheme(Color.Black, Color.White, SystemColors.ControlText, SystemColors.Control, Color.SteelBlue, Color.DarkOrange, Color.LimeGreen);
        }


        public void SetTheme(Color windowFG, Color windowBG, Color controlFG, Color controlBG, Color accent1, Color accent2, Color highLight)
        {
            // control BG, FG
            // button FG, BG
            // windowFG, gray1, gray2, pale1, pale2
            // windowBG, gray1, gray2, pale1, pale2
            // accent1, gray1, gray2, pale1, pale2
            // accent2, gray1, gray2, pale1, pale2

            // make sure base colors dont exceed 80% lum and 80% 
            //windowFG = LimitColor(windowFG);
            //controlFG = LimitColor(controlFG);
            accent1 = LimitColor(accent1);
            accent2 = LimitColor(accent2);

            Window = new ColorWay(windowFG, windowBG);
            Control = new ColorWay(controlFG, controlBG);
            AccentWindow1 = new ColorWay(accent1, windowBG);
            AccentWindow2 = new ColorWay(accent2, windowBG);
            AccentControl1 = new ColorWay(accent1, controlBG);
            AccentControl2 = new ColorWay(accent2, controlBG);
            Highlight = new ColorWay(highLight, windowBG);

            _slightPen = null;
            _transPen = null;
            _vtransPen = null;
        }

        static public Color LimitColor(Color c)
        {
            var fg = ColorUtilities.RGB_to_HSL(c);
            fg.L = Math.Min(0.6, fg.L);
            fg.S = Math.Min(0.6, fg.S);
            return (ColorUtilities.HSL_to_RGB(fg));
        }


        int WIDTH = 70;
        int WIDTHSPACE = 70;
        int HEIGHT = 30;
        int HEIGHTSPACE = 35;
        public void DrawSwatch(Graphics g, int x, int y)
        {
            int sx = x;
            int sy = y;

            WIDTHSPACE = 75;
            DrawSwatch(g, x, y, WIDTH * 4, HEIGHTSPACE * 8, Window.Background.Brush);
            x = sx;
            x += WIDTHSPACE / 2;
            y += HEIGHTSPACE / 2;
            DrawSwatch(g, x, y, WIDTH, Window, "window"); x += WIDTHSPACE;
            DrawSwatch(g, x, y, WIDTH, AccentWindow1, "acc1"); x += WIDTHSPACE;
            DrawSwatch(g, x, y, WIDTH, AccentWindow2, "acc2"); x += WIDTHSPACE;


            y = sy;
            x += WIDTH;

            DrawSwatch(g, x, y, WIDTH * 4, HEIGHTSPACE * 8, Control.Background.Brush);
            x += WIDTHSPACE / 2;
            y += HEIGHTSPACE / 2;
            DrawSwatch(g, x, y, WIDTH, Control, "control"); x += WIDTHSPACE;
            DrawSwatch(g, x, y, WIDTH, AccentControl1, "accControl1"); x += WIDTHSPACE;
            DrawSwatch(g, x, y, WIDTH, AccentControl2, "accControl2"); x += WIDTHSPACE;

        }

        private void DrawSwatch(Graphics g, int x, int y, int w, int h, Brush brush)
        {
            g.FillRectangle(brush, x, y, w, h);
        }

        private void DrawSwatch(Graphics g, int x, int y, int wIDTH, ColorWay cw, string txt)
        {
            Brush br = Brushes.LightBlue;
            var font = new Font(FontFamily.GenericSansSerif, 10f);
            g.DrawString(txt, font, br, x, y - 20);
            g.FillRectangle(cw.Foreground.Brush, x, y, wIDTH, HEIGHT); g.DrawString("FG", font, br, x, y); y += HEIGHTSPACE;
            g.FillRectangle(cw.Background.Brush, x, y, wIDTH, HEIGHT); g.DrawString("BG", font, br, x, y); y += HEIGHTSPACE;
            y += 10;
            g.FillRectangle(cw.Bright.Brush, x, y, wIDTH, HEIGHT); g.DrawString("Bright", font, br, x, y); y += HEIGHTSPACE;
            g.FillRectangle(cw.High.Brush, x, y, wIDTH, HEIGHT); g.DrawString("Hi", font, br, x, y); y += HEIGHTSPACE;
            g.FillRectangle(cw.Medium.Brush, x, y, wIDTH, HEIGHT); g.DrawString("Med", font, br, x, y); y += HEIGHTSPACE;
            g.FillRectangle(cw.Low.Brush, x, y, wIDTH, HEIGHT); g.DrawString("Low", font, br, x, y); y += HEIGHTSPACE;
            g.FillRectangle(cw.VLow.Brush, x, y, wIDTH, HEIGHT); g.DrawString("VLow", font, br, x, y); y += HEIGHTSPACE;
        }

        public ColorWay Window { get; private set; }
        public ColorWay Control { get; private set; }
        public ColorWay AccentWindow1 { get; private set; }
        public ColorWay AccentWindow2 { get; private set; }
        public ColorWay Highlight { get; private set; }
        public ColorWay AccentControl1 { get; private set; }
        public ColorWay AccentControl2 { get; private set; }

        Pen _slightPen = null;
        Pen _transPen = null;
        Pen _vtransPen = null;

        public Pen SlightTranslucentPen
        { get { if (_slightPen == null) _slightPen = new Pen(Color.FromArgb(200, Window.Foreground.Color)); return (_slightPen); } }
        public Pen TranslucentPen
        { get { if (_transPen == null) _transPen = new Pen(Color.FromArgb(100, Window.Foreground.Color)); return (_transPen); } }
        public Pen VeryTranslucentPen
        { get { if (_vtransPen == null) _vtransPen = new Pen(Color.FromArgb(30, Window.Foreground.Color)); return (_vtransPen); } }
        internal void Impose(Form form1)
        {
            FindAndRecolorControls(form1);
        }


        private void FindAndRecolorControls(ToolStripItemCollection items, int lvl)
        {

            foreach (ToolStripItem tsi in items)
            {
                ToolStripMenuItem tsmi = tsi as ToolStripMenuItem;

                tsi.BackColor = lvl == 0 ? Control.Background.Color : Window.Background.Color;
                tsi.ForeColor = lvl == 0 ? Control.Foreground.Color : Window.Foreground.Color;

                IERSInterface.ToolStripTextBoxWithLabel tbWl = tsi as IERSInterface.ToolStripTextBoxWithLabel;


                ToolStripControlHost host = tsi as ToolStripControlHost;
                if (host != null)
                {
                    host.BackColor = Window.Background.Color;
                    host.ForeColor = Window.Foreground.Color;
                }
                if (tbWl != null)
                {
                    tbWl.BackColor = Window.Background.Color;
                    tbWl.ForeColor = Window.Foreground.Color;
                    tbWl.TextBoxWithLabelControl.Label.BackColor = Window.Background.Color;
                    tbWl.TextBoxWithLabelControl.Label.ForeColor = Window.Foreground.Color;
                    tbWl.TextBoxWithLabelControl.BackColor = Window.Background.Color;
                    tbWl.TextBoxWithLabelControl.ForeColor = Window.Foreground.Color;
                    //tbWl.TextBox.BackColor = Window.Background.Color;
                    //tbWl.TextBox.ForeColor = Window.Foreground.Color;
                }
                if (tsmi != null) FindAndRecolorControls(tsmi.DropDownItems, lvl + 1);
            }
        }
        void FindAndRecolorControls(Control control)
        {

            foreach (Control cc in control.Controls)
            {
                if (cc is MenuStrip)
                {
                    MenuStrip mnu = cc as MenuStrip;
                    //mnu.Items.AddRange
                    FindAndRecolorControls(mnu.Items, 0);
                    cc.BackColor = Control.Background.Color;
                    cc.ForeColor = Control.Foreground.Color;
                }
                if (cc is Label)
                {
                    cc.BackColor = Control.Background.Color;
                    cc.ForeColor = Control.Foreground.Color;
                }
                else if (cc is ComboBox)
                {
                    cc.BackColor = Window.Background.Color;
                    cc.ForeColor = Window.Foreground.Color;
                }
                else if (cc is IERSInterface.ComboBoxEnumControl)
                {
                    cc.BackColor = Window.Background.Color;
                    cc.ForeColor = Window.Foreground.Color;
                }
                if (cc is EventScroller)
                {
                    cc.BackColor = Control.Background.Color;
                    cc.ForeColor = Control.Foreground.Color;
                }
                else if (cc is Button)
                {
                    Button btn = cc as Button;
                    cc.BackColor = Control.Low.Color;
                    cc.ForeColor = Control.Foreground.Color;
                }
                else if (cc is TextBox)
                {
                    TextBox tbox = cc as TextBox;
                    cc.BackColor = AccentWindow1.VLow.Color;
                    cc.ForeColor = AccentWindow1.Bright.Color;
                }
                else if (cc is FlowLayoutPanel)
                {
                    cc.BackColor = Control.Background.Color;
                    cc.ForeColor = Control.Foreground.Color;
                }
                else if (cc is ChartLib.DoubleBuffer)
                {
                    cc.BackColor = Window.Background.Color;
                }
                else if (cc is SplitContainer)
                {
                    var split = cc as SplitContainer;
                    split.BackColor = Control.Background.Color;
                    split.ForeColor = Control.Foreground.Color;
                }
                else if (cc is Panel)
                {
                    var panel = cc as Panel;
                    if (!(cc is SplitterPanel))
                    {
                        if (cc.BackgroundImage == null)
                            cc.BackColor = Control.Background.Color;
                        cc.ForeColor = Control.Foreground.Color;
                    }
                }
                else
                {
                    var p = cc as PropertyGrid;

                    if (p != null)
                    {
                        var bgCol = Window.Background.Color;
                        var fgCol = Window.Foreground.Color;
                        p.BackColor = bgCol;
                        p.CategorySplitterColor = fgCol;
                        p.HelpBackColor = bgCol;
                        p.LineColor = fgCol;
                        p.SelectedItemWithFocusBackColor = fgCol;
                        p.ViewBackColor = bgCol;
                        p.ViewBorderColor = fgCol;
                        p.HelpBorderColor = fgCol;
                    }
                }

                if (cc.HasChildren)
                    FindAndRecolorControls(cc);

            }
        }

    }

    public class ColorWay
    {
        public ColorWay(Color fg, Color bg)
        {
            Foreground = new ColorComp(fg);
            Background = new ColorComp(bg);

            if (Background.ContrastPen.Color.R == 0)
            {
                // light bg
                Bright = new ColorComp(ColorUtilities.Brighten(fg, -0.6)); // super high contrast
                High = interpolateToGray(fg, bg, 0.35); //closest to FG
                Medium = interpolateToGray(fg, bg, 0.70);
                Low = interpolateToGray(fg, bg, 0.85);
                VLow = interpolateToGray(fg, bg, 0.95);
            }
            else
            {
                // dark bg
                Bright = new ColorComp(ColorUtilities.Brighten(fg, 0.60));
                High = interpolateToGray(fg, bg, 0.25);//closest to FG
                Medium = interpolateToGray(fg, bg, 0.60);
                Low = interpolateToGray(fg, bg, 0.75);
                VLow = interpolateToGray(fg, bg, 0.85);
            }
        }

        ColorComp interpolateToGray(Color c1, Color c2, double t)
        {
            byte r = linear(c1.R, c2.R, t);
            byte g = linear(c1.G, c2.G, t);
            byte b = linear(c1.B, c2.B, t);
            return (new ColorComp(Color.FromArgb(255, r, g, b)));
        }
        byte linear(byte a, byte b, double t)
        {
            double d = (b - a) * t + a;
            int v = (int)Math.Round(d, MidpointRounding.AwayFromZero);
            if (v > 255) return (255);
            if (v < 0) return (0);
            return ((byte)v);

        }
        public ColorComp Foreground { get; }
        public ColorComp Background { get; }
        public ColorComp Bright { get; }
        public ColorComp High { get; }
        public ColorComp Medium { get; }
        public ColorComp Low { get; }
        public ColorComp VLow { get; }

    }

    public class ColorComp
    {
        Brush br;
        Brush contrastBr;
        Pen pen, pen2, contrastPen;
        public ColorComp(Color color)
        {
            Color = color;
        }
        public Color Color { get; }
        public Brush Brush { get { if (br == null) br = new SolidBrush(Color); return (br); } }
        public Brush ContrastBrush
        {
            get
            {
                if (contrastBr == null) contrastBr = ColorUtilities.GetContrastBrush(Color); return (contrastBr);
            }
        }

        public Pen ContrastPen
        {
            get
            {
                if (contrastPen == null) contrastPen = ColorUtilities.GetContrastPen(Color); return (contrastPen);
            }
        }
        public Pen Pen { get { if (pen == null) pen = new Pen(Color); return (pen); } }
        public Pen Pen2 { get { if (pen2 == null) pen2 = new Pen(Color, 2f); return (pen2); } }
    }

}
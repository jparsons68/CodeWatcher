using IERSInterface;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace CodeWatcher
{
    public class SortButton : Label
    {
        ToolTipHelper ttH = new ToolTipHelper();
        private SortBy _sortBy = SortBy.ALPHABETICAL;

        private bool _mouseIsDown;
        private bool _mouseInside;
        public SortButton()
        {
            ttH.Add(this, "*");
            this.Image = Properties.Resources.forward;
            this.Size = new Size(Properties.Resources.forward.Width + 4, Properties.Resources.forward.Height + 4);
            this.ImageAlign = ContentAlignment.MiddleCenter;

            this.MouseClick += SortButton_MouseClick;
            this.MouseDown += SortButton_MouseDown;
            this.MouseUp += SortButton_MouseUp;
            this.MouseEnter += SortButton_MouseEnter;
            this.MouseLeave += SortButton_MouseLeave;
            this.MouseMove += SortButton_MouseMove;
        }


        private void SortButton_MouseClick(object sender, MouseEventArgs e)
        {
            SortBy = SortBy.Next();
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            Rectangle rect = pevent.ClipRectangle;
            pevent.Graphics.FillRectangle(Theme.Window.VLow.Brush, rect);
            if (_mouseIsDown && _mouseInside)
            {
                pevent.Graphics.FillRectangle(SystemBrushes.Highlight, rect);
                rect.Width--;
                rect.Height--;
                pevent.Graphics.DrawRectangle(Theme.Window.Medium.Pen, rect);
            }
            else if (_mouseIsDown || _mouseInside)
            {
                pevent.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(50, Theme.Window.Foreground.Color)), pevent.ClipRectangle);
                rect.Width--;
                rect.Height--;
                pevent.Graphics.DrawRectangle(Theme.Window.Low.Pen, rect);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            //base.OnPaint(e);
            Rectangle rect = e.ClipRectangle;
            int dim = Height / 6;
            rect.Inflate(-dim, -dim);
            e.Graphics.DrawImage(this.Image, rect);

        }



        private void SortButton_MouseLeave(object sender, EventArgs e)
        {
            _mouseInside = false;
            Refresh();
        }

        private void SortButton_MouseEnter(object sender, EventArgs e)
        {
            _mouseInside = true;
            Refresh();
        }

        private void SortButton_MouseMove(object sender, MouseEventArgs e)
        {
            bool tmp = this.ClientRectangle.Contains(e.Location);
            if (tmp != _mouseInside)
            {
                _mouseInside = tmp;
                Refresh();
            }
        }
        private void SortButton_MouseUp(object sender, MouseEventArgs e)
        {
            _mouseIsDown = false;
            Refresh();
        }

        private void SortButton_MouseDown(object sender, MouseEventArgs e)
        {
            _mouseIsDown = true;
            Refresh();
        }

        public SortBy SortBy
        {
            get { return (_sortBy); }
            set
            {
                _sortBy = value;
                Bitmap fwd, rev, fst, rst;
                Bitmap img;

                if (Theme.Window.Background.Color.R == 0) // dark
                {
                    fwd = Properties.Resources.forwardInv;
                    rev = Properties.Resources.reverseInv;
                    rst = Properties.Resources.revorderInv;
                    fst = Properties.Resources.alphaorderInv;
                }
                else
                {
                    fwd = Properties.Resources.forward;
                    rev = Properties.Resources.reverse;
                    rst = Properties.Resources.revorder;
                    fst = Properties.Resources.alphaorder;
                }

                string helpTxt = null;
                switch (_sortBy)
                {
                    case SortBy.MOST_RECENT_FIRST:
                        helpTxt = "Sort by most recent first";
                        img = rev;
                        break;
                    case SortBy.EARLIEST_FIRST:
                        helpTxt = "Sort by earliest first";
                        img = fwd;
                        break;
                    case SortBy.ALPHABETICAL:
                        helpTxt = "Sort Alphabetical";
                        img = fst;
                        break;
                    case SortBy.REVERSE_ALPHABETICAL:
                        helpTxt = "Sort Reverse Alphabetical";
                        img = rst;
                        break;
                    default:
                        img = fst;
                        break;
                }

                this.Image = img;
                ttH.ToolTip.IsBalloon = false;
                ttH.Clear();
                ttH.Add(this, helpTxt);
                this.Refresh();
            }
        }

        public ThemeColors Theme { get; set; }

        public void Set(string txt)
        {
            SortBy sb;
            if (Enum.TryParse(txt, out sb)) SortBy = sb;
        }
    }
}

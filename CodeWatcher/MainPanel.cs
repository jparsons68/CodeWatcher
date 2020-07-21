using ChartLib;
using ClipperLib;
using QSGeometry;
using Syncfusion.Windows.Forms.Tools;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Utilities;
using Path = System.Collections.Generic.List<ClipperLib.IntPoint>;
using Paths = System.Collections.Generic.List<System.Collections.Generic.List<ClipperLib.IntPoint>>;

namespace CodeWatcher
{
    [Flags]
    public enum MainArea
    {
        NONE = 0,
        PROJECT_COLUMN = 1,
        PROJECT_LABEL = 2,
        PROJECT_ROW = 4,
        PRESENT_AREA = 8,

        DAYBOX = 32,
        DAYINFO = 64,
        MAIN = 128,
        TOPHEADER = 512,

        SELECTED_WHOLE = 8192,
        SELECTED_START = 16384,
        SELECTED_END = 32768,
    }

    public enum TimePeriod
    {
        [Description("1 minute")]
        ONEMINUTE = 1,
        [Description("5 minutes")]
        FIVEMINUTES = 5,
        [Description("1 hour")]
        ONEHOUR = 60,
        [Description("2 hours")]
        TWOHOURS = 120,
        [Description("8 hours")]
        EIGHTHOURS = 480,
        [Description("1 day")]
        ONEDAY = 1440,
        [Description("1 week")]
        ONEWEEK = 10080,
        [Description("1 month")]
        ONEMONTH = 43200,
        [Description("3 months")]
        THREEMONTHS = 129600,
        [Description("6 months")]
        SIXMONTHS = 259200,
        [Description("1 year")]
        ONEYEAR = 525600,
        [Description("All time")]
        ALLTIME = -1,
    }

    class MainPanel
    {
        readonly FileChangeWatcher _fcWatcher;
        readonly DoubleBuffer _doubleBuffer;
        readonly ThemeColors _theme;
        readonly Font _font;
        readonly Font _smallfont;
        readonly Font _fixedWidthFont;
        readonly TooltipContainer _ttC;
        bool _toPresent = true;
        DateTime _dt0;
        DateTime _dt1;
        int _iDaySpan = 30;
        double _dDaySpan = 30;
        int _rowHeight;
        private int _defaultRowHeight { get { return _font.Height * 2; } }
        private int _minRowHeight { get { return _font.Height; } }
        private int _maxRowHeight { get { return _doubleBuffer.Height - TOPHEADINGHEIGHT; } }
        private int _fitRowHeight
        {
            get
            {
                return (_doubleBuffer.Height - TOPHEADINGHEIGHT) /
                       _fcWatcher.Table.CountProjects(DataState.True, DataState.Ignore);
            }
        }
        int _jOffset, _maxJOffset = 10;
        int _scrollDays;
        private readonly Point[] ptsLeftTri = new[] { new Point(0, 0), new Point(0, -10), new Point(10, -10), };
        private readonly Point[] ptsRightTri = new[] { new Point(0, 0), new Point(0, -10), new Point(-10, -10), };
        const int BORDER = 10;
        const int TOPHEADINGHEIGHT = 75;
        const int PROJCOLWIDTH = 150;
        const int TICKH = 8;
        const int MINBOXW = 2;
        const int FUTCOLWIDTH = 20;
        const int ROWHEIGHTMAX = 1000;
        const int ROWHEIGHTINC = 3;

        private readonly Brush txtBgBrush;
        private readonly Brush txtFgBrush;

        private readonly Pen txtUlPen;
        private readonly Pen txtBrPen;
        private readonly Pen transLitePen1;
        private readonly Pen transLitePen2;
        private readonly Pen transDarkPen1;
        private readonly Pen transDarkPen2;
        private readonly StatusStripLabel _labelDATE;
        private readonly StatusStripLabel _labelWORK;
        private readonly StatusStripLabel _labelTIME;
        //private bool _showInfoColumn = true;
        private bool _showIdleLine;
        private bool _compensatedScaling = true;
        private bool _showVerticalCursor = true;
        private EditSort _editSort = EditSort.NAME;
        private readonly SortButton _sortButton;


        string _toCode(MainArea ma) { return ma.ToString(); }


        public MainPanel(FileChangeWatcher fcw, DoubleBuffer doubleBuffer, ThemeColors theme)
        {
            _fcWatcher = fcw;
            _doubleBuffer = doubleBuffer;
            _theme = theme;
            _font = _doubleBuffer.Font;
            _smallfont = new Font(_font.FontFamily, _font.Size * 0.7f);
            _fixedWidthFont = new Font(FontFamily.GenericMonospace, _font.Size);
            _initCompensatedEventScaling();

            txtBgBrush = new SolidBrush(Color.FromArgb(20, 20, 20));
            txtFgBrush = new SolidBrush(Color.FromArgb(230, 230, 230));
            txtUlPen = new Pen(Color.FromArgb(80, 80, 80));
            txtBrPen = new Pen(Color.FromArgb(180, 180, 180));

            transLitePen1 = new Pen(Color.FromArgb(150, Color.White));
            transLitePen2 = new Pen(Color.FromArgb(50, Color.White));
            transDarkPen1 = new Pen(Color.FromArgb(50, Color.Black));
            transDarkPen2 = new Pen(Color.FromArgb(150, Color.Black));

            _doubleBuffer.MouseWheel += DoubleBuffer1_MouseWheel;
            _doubleBuffer.MouseMove += _doubleBuffer_MouseMove;
            _doubleBuffer.MouseDown += _doubleBuffer_MouseDown;
            _doubleBuffer.MouseUp += _doubleBuffer_MouseUp;
            _doubleBuffer.MouseDoubleClick += _doubleBuffer_MouseDoubleClick;
            _doubleBuffer.PaintEvent += _doubleBuffer_PaintEvent;
            _ttC = new TooltipContainer(_doubleBuffer);
            MidnightNotifier.DayChanged += (s, e) => { _doubleBuffer.Invalidate(); };// make sure it ticks over to next day

            var statusStrip = new StatusStrip { Dock = DockStyle.Bottom };
            StatusStripLabel label0 = new StatusStripLabel { Text = @"Date:" };
            label0.Font = new Font(label0.Font, FontStyle.Bold);
            _labelDATE = new StatusStripLabel();
            StatusStripLabel label4 = new StatusStripLabel { Text = @"Time:" };
            label4.Font = new Font(label4.Font, FontStyle.Bold);
            _labelTIME = new StatusStripLabel();
            StatusStripLabel label3 = new StatusStripLabel { Text = @"Total Work Selected:" };
            label3.Font = new Font(label3.Font, FontStyle.Bold);
            _labelWORK = new StatusStripLabel();
            _labelDATE.AutoSize = false;
            _labelDATE.Size = new Size(200, _labelDATE.Height);
            _labelDATE.TextAlign = ContentAlignment.MiddleLeft;
            _labelTIME.AutoSize = false;
            _labelTIME.Size = new Size(200, _labelTIME.Height);
            _labelTIME.TextAlign = ContentAlignment.MiddleLeft;
            _labelWORK.AutoSize = false;
            _labelWORK.Size = new Size(200, _labelWORK.Height);
            _labelWORK.TextAlign = ContentAlignment.MiddleLeft;

            statusStrip.Items.Add(label0);
            statusStrip.Items.Add(_labelDATE);
            statusStrip.Items.Add(label4);
            statusStrip.Items.Add(_labelTIME);
            statusStrip.Items.Add(label3);
            statusStrip.Items.Add(_labelWORK);
            doubleBuffer.Parent.Controls.Add(statusStrip);

            _sortButton = new SortButton();


            _sortButton.MouseClick += SortButton_MouseClick;
            _sortButton.Theme = _theme;
            doubleBuffer.Parent.Controls.Add(_sortButton);
            _sortButton.BringToFront();


            _rowHeight = _defaultRowHeight;
        }


        private void SortButton_MouseClick(object sender, MouseEventArgs e)
        {
            _fcWatcher.SortProjectsBy = _sortButton.SortBy;
            _doubleBuffer.Refresh();
        }

        public void IncrementDayStart(int clicks)
        {
            _incDayStart(clicks * _scrollDays);
            _doubleBuffer.Refresh();
        }

        internal void IncrementRowHeight(int clicks)
        {
            _incRowHeight(clicks * ROWHEIGHTINC);
            _doubleBuffer.Refresh();
        }

        internal void IncrementRow(int clicks)
        {
            _incJoffset(clicks);
            _doubleBuffer.Refresh();
        }
        internal void ShowPresentToLast(TimePeriod timePeriod)
        {
            switch (timePeriod)
            {
                case TimePeriod.ONEWEEK:
                    _jOffset = 0;
                    _toPresent = true;
                    DaySpan = 7;
                    _doubleBuffer.Refresh();
                    break;
                case TimePeriod.ONEMONTH:
                    _jOffset = 0;
                    _toPresent = true;
                    DaySpan = FileChangeTable.InclusiveDaySpan(DateTime.Now.AddMonths(-1), DateTime.Now);
                    _doubleBuffer.Refresh();
                    break;
                case TimePeriod.THREEMONTHS:
                    _jOffset = 0;
                    _toPresent = true;
                    DaySpan = FileChangeTable.InclusiveDaySpan(DateTime.Now.AddMonths(-3), DateTime.Now);
                    _doubleBuffer.Refresh();
                    break;
                case TimePeriod.SIXMONTHS:
                    _jOffset = 0;
                    _toPresent = true;
                    DaySpan = FileChangeTable.InclusiveDaySpan(DateTime.Now.AddMonths(-6), DateTime.Now);
                    _doubleBuffer.Refresh();
                    break;
                case TimePeriod.ONEYEAR:
                    _jOffset = 0;
                    _toPresent = true;
                    DaySpan = FileChangeTable.InclusiveDaySpan(DateTime.Now.AddYears(-1), DateTime.Now);
                    _doubleBuffer.Refresh();
                    break;
                case TimePeriod.ALLTIME:
                    _jOffset = 0;
                    _toPresent = true;
                    DaySpan = FileChangeTable.InclusiveDaySpan(_fcWatcher.Table.StartTime, DateTime.Now);
                    _doubleBuffer.Refresh();
                    break;
            }
        }

        internal void ScrollToTop()
        {
            _jOffset = 0;
            _doubleBuffer.Refresh();
        }

        internal void SaveSettings()
        {
            Properties.Settings.Default.MinDateTime = _dt0;
            Properties.Settings.Default.MaxDateTime = _dt1;
            Properties.Settings.Default.ToPresent = _toPresent;
            Properties.Settings.Default.Joffset = _jOffset;
            Properties.Settings.Default.RowHeight = _rowHeight;
            //Properties.Settings.Default.ShowInfoColumn = _showInfoColumn;
            Properties.Settings.Default.ShowIdleLine = _showIdleLine;
            Properties.Settings.Default.ShowVerticalCursor = _showVerticalCursor;
            Properties.Settings.Default.CompensatedScaling = _compensatedScaling;
            Properties.Settings.Default.EditSort = _editSort;
            Properties.Settings.Default.Save();
        }

        internal void LoadSettings()
        {
            _dt0 = Properties.Settings.Default.MinDateTime;
            _dt1 = Properties.Settings.Default.MaxDateTime;
            _toPresent = Properties.Settings.Default.ToPresent;
            _jOffset = Properties.Settings.Default.Joffset;
            _rowHeight = Properties.Settings.Default.RowHeight;
            //_showInfoColumn = Properties.Settings.Default.ShowInfoColumn;
            _showIdleLine = Properties.Settings.Default.ShowIdleLine;
            _showVerticalCursor = Properties.Settings.Default.ShowVerticalCursor;
            _compensatedScaling = Properties.Settings.Default.CompensatedScaling;
            _editSort = Properties.Settings.Default.EditSort;
            _fcWatcher.SortProjectsBy = _sortButton.SortBy;
        }

        // compensated - don't show dead of night at same scale
        // 9 to 5
        //                       zero, early,   late,     end
        double[] _frac = new[] { 0.0, 1.0 / 5, 4.0 / 5, 1.0 };
        double[] _hrsf = new[] { 0.0, 9.0, 17.0, 24.0 };

        private JSpline1D jSplineFwd;
        private JSpline1D jSplineRev;
        private void _initCompensatedEventScaling()
        {
            jSplineFwd = new JSpline1D(JSplineType.Linear, new List<double>(_frac), new List<double>(_hrsf));
            jSplineRev = new JSpline1D(JSplineType.Linear, new List<double>(_hrsf), new List<double>(_frac));
        }
        private DateTime _dateTimeAtPtr(int x, bool comp)
        {
            try
            {
                double days = (x - _leftDemark) / _boxSpace;
                if (comp)
                {
                    double wholeDays = (int)days;
                    double fractDay = days - wholeDays;

                    // spline to give hours
                    var hrs = jSplineFwd.Calculate(fractDay);

                    return (_dt0.Date.AddDays(wholeDays).AddHours(hrs));
                }
                else
                {
                    return (_dt0.Date.AddDays(days));
                }
            }
            catch (Exception)
            {
                return (_dt0);
            }
        }
        private int _screenxAtDateTime(DateTime dt)
        {
            if (_compensatedScaling)
            {
                // start of day box
                var dt0 = dt.Date;

                // fractional hours into this day
                var hrs = (dt - dt0).TotalHours;
                // get fractional position (0-1) in this day
                double frac = jSplineRev.Calculate(hrs);

                int sx = (int)((dt0 - _dt0.Date).TotalDays * _boxSpace + _leftDemark + 0.5 + _boxSpace * frac);
                return (sx);
            }
            else
            {
                // full
                int sx = (int)((dt - _dt0.Date).TotalDays * _boxSpace + _leftDemark + 0.5);
                return (sx);
            }
        }

        private Rectangle _mainClipRect;
        private int _leftDemark;
        private float _boxSpace = 10;

        private void _doubleBuffer_PaintEvent(object sender, PaintEventArgs e)
        {
            try
            {
                MetalTemperatureBrushes.SetRange(0.0, 1.0);
                Graphics g = e.Graphics;
                int width = e.ClipRectangle.Width;
                int height = e.ClipRectangle.Height;
                _ttC.Clear();

                g.Clear(_theme.Window.Background.Color);
                FileChangeTable table;
                if (_fcWatcher?.Table == null || (table = _fcWatcher.Table) == null || table.ItemCount == 0)
                {
                    g.DrawString("NO DATA!", _font, Brushes.Red, BORDER, TOPHEADINGHEIGHT);
                    return;
                }

                DateTime today = DateTime.Now.Date;
                // day range
                if (_toPresent) _dt1 = today;
                _dt0 = _calcT0(_dt1);

                int fH = _font.Height;
                _leftDemark = BORDER + PROJCOLWIDTH;
                int playWidth = width - _leftDemark - FUTCOLWIDTH;
                _boxSpace = playWidth / DaySpan;
                int boxW = Math.Max((int)_boxSpace, MINBOXW);//not too small, always visible
                int boxCenterOff = boxW / 2;
                _scrollDays = Math.Max(1, (int)(50 / _boxSpace));

                bool showDayOfWeek = _boxSpace > fH;
                bool showDayOfMonth = _boxSpace > fH;
                bool showMondays = _boxSpace * 7 > fH * 2;
                bool showDayVerticals = _boxSpace > fH;
                int activityIndHeight = _rowHeight - 2;
                _mainClipRect = new Rectangle(_leftDemark, 0, width - _leftDemark, height);

                Rectangle mainR = new Rectangle(_leftDemark, TOPHEADINGHEIGHT, width - _leftDemark, height - TOPHEADINGHEIGHT);
                _ttC.Add(_toCode(MainArea.MAIN), mainR, null, null);
                Rectangle topR = new Rectangle(0, 0, width, TOPHEADINGHEIGHT);
                _ttC.Add(_toCode(MainArea.TOPHEADER), topR, null, null);

                // projects with activity in shown time range
                int peakOpsInDay = table.GetPeakOpsInDay();


                int playHeight = (height - TOPHEADINGHEIGHT);
                _maxJOffset = table.CountProjects(DataState.True, DataState.Ignore) - (playHeight / _rowHeight);
                if (_maxJOffset < 0) _maxJOffset = 0;
                _incJoffset(0);

                Brush fgBr = _theme.Window.Foreground.Brush;
                Brush bgBr = _theme.Window.Background.Brush;
                Brush bgGrayBr = _theme.Window.Low.Brush;
                Brush mo1Br = _theme.AccentWindow1.High.Brush;
                Brush mo2Br = _theme.AccentWindow2.High.Brush;
                Brush mo1CBr = _theme.AccentWindow1.High.ContrastBrush;
                Brush mo2CBr = _theme.AccentWindow2.High.ContrastBrush;
                Brush weekendBrushA = _theme.AccentWindow1.Low.Brush;
                Brush weekendBrushB = _theme.AccentWindow1.VLow.Brush;
                Brush hiliteBr = _theme.Highlight.Foreground.Brush;
                bool presentDayShown = false;

                _sortButton.SortBy = _fcWatcher.SortProjectsBy;
                var loc = _doubleBuffer.Location;
                _sortButton.Size = new Size(fH, fH);
                _sortButton.Location = new Point(loc.X + PROJCOLWIDTH + BORDER - _sortButton.Width-1, loc.Y + TOPHEADINGHEIGHT - _sortButton.Height);

                // draw
                g.DrawString("Project", _font, fgBr, BORDER, TOPHEADINGHEIGHT - fH);

                _ttC.Add(_toCode(MainArea.PROJECT_COLUMN), new Rectangle(0, 0, _leftDemark, height), null, this);

                Rectangle? pDayInfoRect = null;
                List<float> verts = new List<float>();
                List<float> dayVerts = new List<float>();
                int yMonth = TOPHEADINGHEIGHT;
                // row per project
                var y = TOPHEADINGHEIGHT;
                int ic = 0;
                int idxJ = -1;
                foreach (var proj in table.EachVisibleProject())
                {
                    idxJ++;
                    if (idxJ < _jOffset) continue;
                    if (y > height) break;

                    ic++;
                    float fx = BORDER;
                    Rectangle rectBg = new Rectangle(0, y, width, _rowHeight);
                    Brush dayBrush;
                    Brush weekendBrush;
                    if (idxJ % 2 == 1) { dayBrush = bgGrayBr; weekendBrush = weekendBrushA; }
                    else { dayBrush = bgBr; weekendBrush = weekendBrushB; }
                    g.FillRectangle(dayBrush, rectBg);

                    var py = y + (_rowHeight - _font.Height) / 2;
                    var clipRect = rectBg;
                    clipRect.Width = _leftDemark;
                    g.SetClip(clipRect);
                    g.FillRectangle(proj.Brush, clipRect);

                    g.DrawString(proj.EditCount + " [" + proj.SelectedEditCount + "]",
                        _font, proj.ContrastBrush, fx, py + (int)(fH * 1.2));

                    if (proj.Selected)
                    if (proj.Selected)
                    {
                        Rectangle selRect = clipRect;
                        selRect.Width = 5;
                        selRect.Height--;
                        g.FillRectangle(_theme.Window.Background.Brush, selRect);
                        selRect.Width--;
                        g.FillRectangle(hiliteBr, selRect);
                    }

                    Rectangle textRect = clipRect;
                    textRect.X = (int)fx;
                    textRect.Y = py;
                    textRect.Width = PROJCOLWIDTH - BORDER - BORDER;
                    textRect.Height = fH;
                    textRect.Inflate(0, 2);

                    g.ResetClip();

                    Rectangle iClip = clipRect;
                    iClip.Intersect(textRect);
                    g.SetClip(iClip);
                    _drawInsetBorderRectangle(g, textRect);
                    g.DrawString(proj.Name, _font, txtFgBrush, (int)fx, py);
                    g.ResetClip();


                    

                    _drawShadedTopBottom(g, clipRect);



                    Rectangle pRect = rectBg;
                    pRect.Width = _leftDemark;
                    _ttC.Add(_toCode(MainArea.PROJECT_ROW), rectBg, null, proj);
                    _ttC.Add(_toCode(MainArea.PROJECT_LABEL), pRect, proj.Path, proj);

                    fx = _leftDemark;
                    int icDay = -1;
                    int year = -999;

                    g.SetClip(_mainClipRect);
                    // iterate thru days
                    foreach (DateTime date in FileChangeTable.EachDay(_dt0, _dt1))
                    {
                        var pDay = proj.GetDay(date);
                        if (date == today) presentDayShown = true;

                        icDay++;
                        if (fx > width) break;
                        if (ic == 1)
                        {
                            int tY = (int)(y - 2.5 * fH);
                            int boxCenter = (int)fx + boxCenterOff;
                            if (showDayOfWeek)
                            {
                                _drawCenteredString(g,
                                  _boxSpace > 100 ?
                                      date.ToString("dddd") :
                                      date.ToString("ddd").Substring(0, 1),
                                    _font, fgBr, boxCenter, tY); tY -= fH;
                            }

                            if (showDayOfMonth || (icDay == 0 && showMondays)) { _drawCenteredString(g, date.Day.ToString(), _font, fgBr, boxCenter, tY); tY -= fH; }
                            else if (showMondays)
                            {
                                if (date.DayOfWeek == DayOfWeek.Monday)
                                    _drawCenteredString(g, date.Day.ToString(), _font, fgBr, boxCenter, tY);
                                tY -= fH;
                            }

                            if (icDay == 0 || date.Day == 1) //MONTH
                            {
                                yMonth = tY + fH;
                                g.FillRectangle(date.Month % 2 == 0 ? mo1Br : mo2Br, (int)(fx + 0.5), tY, width - fx, fH);
                                g.DrawString(date.ToString("MMMM"), _font, date.Month % 2 == 0 ? mo1CBr : mo2CBr, fx, tY); tY -= fH;
                            }

                            if (year != date.Year) //YEAR
                            {
                                year = date.Year;
                                g.DrawString(date.Year.ToString(), _font, fgBr, fx, tY);
                            }
                            // START OF WEEK
                            if (date.DayOfWeek == DayOfWeek.Monday ||
                                date.DayOfWeek == DayOfWeek.Saturday)
                                verts.Add(fx);

                            if (showDayVerticals) dayVerts.Add(fx);


                            // ticks
                            if (_boxSpace > 200)
                                foreach (DateTime dtTick in FileChangeTable.EachTimeSpan(date.AddHours(1), date.AddDays(0.95),
                                    TimeSpan.FromHours(1)))
                                {

                                    var tkX = _screenxAtDateTime(dtTick);
                                    g.DrawLine(_theme.Window.Medium.Pen, tkX, TOPHEADINGHEIGHT, tkX, TOPHEADINGHEIGHT - TICKH);
                                    if (dtTick.Hour >= 9 && dtTick.Hour <= 17)
                                    {
                                        string txt = dtTick.ToString("%h");
                                        var siz = g.MeasureString(txt, _smallfont);
                                        g.DrawString(txt, _smallfont, _theme.Window.Medium.Brush,
                                            tkX - siz.Width / 2, TOPHEADINGHEIGHT - TICKH - _smallfont.Height);
                                    }
                                }

                        }


                        // weekend
                        if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                        {
                            Rectangle dayRect = rectBg;
                            dayRect.X = (int)(fx + 0.5);
                            dayRect.Width = (int)(_boxSpace + 1);
                            g.FillRectangle(weekendBrush, dayRect);
                        }

                        // day
                        if (pDay != null && pDay.Count > 0)
                        {
                            Rectangle dayRect = rectBg;
                            dayRect.X = (int)(fx + 0.5);
                            dayRect.Width = (int)(_boxSpace + 1);
                            Brush br = MetalTemperatureBrushes.GetBrush(((double)pDay.Count) / peakOpsInDay);
                            g.FillRectangle(br, dayRect);
                            _ttC.Add(_toCode(MainArea.DAYBOX), dayRect, null, pDay);
                            if (pDay == _pDayInfoDay) pDayInfoRect = dayRect;
                        }


                        fx += _boxSpace;
                    }



                    // ACTIVITY TRACE
                    if (_boxSpace > fH)
                    {
                        int edW = _screenxAtDateTime(_dt0.AddMinutes(ActivityTraceBuilder.PerEditMinutes)) - _screenxAtDateTime(_dt0);
                        if (edW < 2) edW = 2;

                        int aY = y + _rowHeight - activityIndHeight - 1;
                        foreach (var fci in proj.Collection)
                        {
                            if (fci.ChangeType.HasAny(ActionTypes.USER_IDLE | ActionTypes.SUSPEND)) continue;
                            int sx0 = _screenxAtDateTime(fci.DateTime);
                            g.FillRectangle(_theme.Window.Medium.Brush, sx0, aY, edW, activityIndHeight);
                        }

                        // REDLINES
                        if (_showIdleLine)
                            foreach (var fci in proj.Collection)
                            {
                                if (!fci.ChangeType.HasAny(ActionTypes.USER_IDLE | ActionTypes.SUSPEND)) continue;
                                int sx0 = _screenxAtDateTime(fci.DateTime);
                                g.FillRectangle(Brushes.Red, sx0, aY, 1, activityIndHeight);
                            }
                    }


                    g.ResetClip();
                    y += _rowHeight;
                }// end proj loop



                if (table.SelectionState)
                {
                    // ReSharper disable once IdentifierTypo
                    const int SELBARH = 6;
                    const int SELHANDLEW = 15;
                    const int SELHANDLEOFF = 5;
                    Color clr = _theme.Window.Foreground.Color;
                    Brush brTrans = new SolidBrush(Color.FromArgb(40, clr));
                    Brush brBdr = new SolidBrush(Color.FromArgb(230, clr));
                    int sx0 = _screenxAtDateTime((DateTime)table.SelectionStartDate);
                    int sx1 = _screenxAtDateTime((DateTime)table.SelectionEndDate);
                    Rectangle rect = new Rectangle(sx0, TOPHEADINGHEIGHT, sx1 - sx0, height);

                    Rectangle mainSideR = mainR;
                    mainSideR.Y = 0;
                    mainSideR.Height = height;
                    g.SetClip(mainSideR);

                    g.FillRectangle(brTrans, rect);
                    Rectangle rectBrd = rect;
                    rectBrd.Height = SELBARH;
                    rectBrd.Y = TOPHEADINGHEIGHT - rectBrd.Height;
                    g.FillRectangle(brBdr, rectBrd);
                    g.TranslateTransform(rectBrd.X, rectBrd.Bottom);
                    g.FillPolygon(brTrans, ptsLeftTri);
                    g.DrawPolygon(_theme.Window.Medium.Pen, ptsLeftTri);
                    g.ResetTransform();
                    g.TranslateTransform(rectBrd.Right, rectBrd.Bottom);
                    g.FillPolygon(brTrans, ptsRightTri);
                    g.DrawPolygon(_theme.Window.Medium.Pen, ptsRightTri);
                    g.ResetTransform();
                    rect.Y = 0;
                    rect.Height = TOPHEADINGHEIGHT;
                    _ttC.Add(_toCode(MainArea.SELECTED_WHOLE), rect, null, null);
                    Rectangle rectEdge = rect;
                    rectEdge.X = rect.Left - SELHANDLEOFF;
                    rectEdge.Width = SELHANDLEW;
                    _ttC.Add(_toCode(MainArea.SELECTED_START), rectEdge, null, null);
                    rectEdge.X = rect.Right - SELHANDLEW + SELHANDLEOFF;
                    _ttC.Add(_toCode(MainArea.SELECTED_END), rectEdge, null, null);

                    foreach (var abBlock in table.Activity.Collection)
                    {
                        sx0 = _screenxAtDateTime(abBlock.StartDate);
                        sx1 = _screenxAtDateTime(abBlock.EndDate);
                        int aW = Math.Max(sx1 - sx0, 2);
                        g.FillRectangle(_theme.Highlight.Medium.Brush, sx0, TOPHEADINGHEIGHT - SELBARH + 1, aW,
                            SELBARH - 2);
                    }
                    g.ResetClip();
                }





                Rectangle futRect = new Rectangle(_doubleBuffer.Width - FUTCOLWIDTH, 0, FUTCOLWIDTH, _doubleBuffer.Height);
                _ttC.Add(_toCode(MainArea.PRESENT_AREA), futRect, "Scroll to present", null);
                Brush presBr = new SolidBrush(Color.FromArgb(100, _theme.Window.Medium.Color));
                g.FillRectangle(presBr, futRect);


                _labelWORK.Text = table.Activity.Summary;

                // horizontal lines
                g.DrawLine(_theme.Window.Medium.Pen, 0, TOPHEADINGHEIGHT, width, TOPHEADINGHEIGHT);
                // vertical lines
                g.DrawLine(_theme.Window.High.Pen2, _leftDemark, 0, _leftDemark, height);
                g.SetClip(_mainClipRect);
                foreach (var vx in dayVerts) g.DrawLine(_theme.VeryTranslucentPen, vx, yMonth, vx, height);
                foreach (var vx in verts) g.DrawLine(_theme.TranslucentPen, vx, yMonth, vx, height);
                g.ResetClip();

                if (_ptrTrack != null)
                    g.DrawLine(_theme.Highlight.Foreground.Pen, ((Point)_ptrTrack).X, TOPHEADINGHEIGHT, ((Point)_ptrTrack).X, height);

                _drawDayInfo(g, pDayInfoRect);



                if (presentDayShown && _toPresent == false)
                {
                    _toPresent = true;
                    // wipe, go back to start!
                    _doubleBuffer_PaintEvent(sender, e);
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void _drawShadedTopBottom(Graphics graphics, Rectangle rect)
        {
            graphics.DrawLine(transLitePen1, rect.Left, rect.Top, rect.Right, rect.Top);
            graphics.DrawLine(transLitePen2, rect.Left, rect.Top + 1, rect.Right, rect.Top + 1);
            graphics.DrawLine(transDarkPen2, rect.Left, rect.Bottom - 1, rect.Right, rect.Bottom - 1);
            graphics.DrawLine(transDarkPen1, rect.Left, rect.Bottom - 2, rect.Right, rect.Bottom - 2);
        }

        private void _drawInsetBorderRectangle(Graphics graphics, Rectangle textRect)
        {
            graphics.FillRectangle(txtBgBrush, textRect);
            graphics.DrawRectangle(txtUlPen, textRect);
            textRect.Width += 4;
            textRect.Height += 4;
            textRect.X -= 5;
            textRect.Y -= 5;
            graphics.DrawRectangle(txtBrPen, textRect);
        }


        private string _niceDay(DateTime dateTime)
        {
            return (dateTime.ToString("ddd dd MMMM yyyy"));
        }

        internal void CopyWorkSummaryToClipboard()
        {
            string summary = _fcWatcher.Table.GetWorkSummary();
            Clipboard.SetText(summary);
        }

        public void ShowIdleLine(bool chk)
        {
            _showIdleLine = chk;
            _doubleBuffer.Refresh();
        }

        public void ShowCompensatedScale(bool chk)
        {
            _compensatedScaling = chk;
            _doubleBuffer.Refresh();
        }

        public void ShowVerticalCursor(bool chk)
        {
            _showVerticalCursor = chk;
            _doubleBuffer.Refresh();
        }

        public void SetEditSort(EditSort srt)
        {
            _editSort = srt;
            _setDayInfo(_pDayInfoDay);
            _doubleBuffer.Refresh();
        }


        internal void RemoveTimeSelectionEdits()
        {
            var dR = MessageBox.Show(@"Remove edits in Time Selection?", @"This will PERMANENTLY remove them! Are you sure?",
                MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
            if (dR == DialogResult.Yes)
            {
                _fcWatcher.Table.RemoveTimeSelectionEdits();
                _doubleBuffer.Refresh();
            }
        }


        private void _drawCenteredString(Graphics g, string txt, Font font, Brush br, int x, int y)
        {
            var siz = g.MeasureString(txt, font);
            g.DrawString(txt, font, br, x - siz.Width / 2, y);
        }


        void _incJoffset(int inc)
        {
            _jOffset += inc;
            if (_jOffset < 0) _jOffset = 0;
            if (_jOffset > _maxJOffset) _jOffset = _maxJOffset;
        }


        void _incDayStart(int inc)
        {
            _toPresent = false;
            _dt0 = _dt0.AddDays(inc);

            if (_fcWatcher?.Table != null)
            {
                var tmpT1 = _calcT1(_dt0);
                var today = DateTime.Now.Date;
                if (tmpT1 > today) tmpT1 = today;
                _dt0 = _calcT0(tmpT1);
            }

            _dt1 = _calcT1(_dt0);
        }


        void _incDaySpan(int inc)
        {
            if (DDaySpan > 7)
            {
                DDaySpan = (int)(DDaySpan * Math.Pow(1.2, inc));
                if (!_toPresent) _dt1 = _calcT1(_dt0);
            }
            else if (inc > 0)
            {
                DDaySpan += 1;
            }
            else if (inc < 0)
            {
                DDaySpan -= 1;
            }
        }

        void _incRowHeight(int inc)
        {
            _rowHeight += inc;
            if (_rowHeight > ROWHEIGHTMAX) _rowHeight = ROWHEIGHTMAX;
            if (_rowHeight < _minRowHeight) _rowHeight = _minRowHeight;
        }
        double DDaySpan
        {
            get => (_dDaySpan);
            set
            {
                _dDaySpan = value; _iDaySpan = (int)Math.Round(_dDaySpan, MidpointRounding.AwayFromZero);
                if (DaySpan < 1) DaySpan = 1;
            }
        }
        int DaySpan
        {
            get => (_iDaySpan);
            set { _iDaySpan = value; if (_iDaySpan < 1) _iDaySpan = 1; _dDaySpan = _iDaySpan; }
        }

        DateTime _calcT0(DateTime dt1) { return (dt1.AddDays(-(DaySpan - 1))); }
        DateTime _calcT1(DateTime dt0) { return (dt0.AddDays(DaySpan - 1)); }


        private void _doubleBuffer_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (_ttC.IsDataRegionAtPointer(e, _toCode(MainArea.TOPHEADER)))
            {
                DateTime dtMin = _dt0.Date < _fcWatcher.Table.StartTime.Date ? _dt0.Date : _fcWatcher.Table.StartTime.Date;
                DateTime dtMax = _dt1.Date > _fcWatcher.Table.EndTime.Date ? _dt1.Date : _fcWatcher.Table.EndTime.Date;
                _fcWatcher.Table.SetTimeSelection(dtMin, dtMax.AddDays(1).Date, true);
                _doubleBuffer.Refresh();
            }
        }

        private DateTime? _dtHandleInit0;
        private DateTime? _dtHandleInit1;
        private DateTime _dtInitClick;
        private MainArea _dtMod = MainArea.NONE;
        private void _doubleBuffer_MouseDown(object sender, MouseEventArgs e)
        {
            if (_fcWatcher == null) return;

            // time selection:
            // select handle grabs and changes
            // no handle click starts selection
            // drag continues
            // + shift + whole select moves whole thing
            _dtHandleInit0 = _fcWatcher.Table.SelectionStartDate;
            _dtHandleInit1 = _fcWatcher.Table.SelectionEndDate;
            _dtInitClick = _dateTimeAtPtr(e.X, false);
            if (_ttC.IsDataRegionAtPointer(e, _toCode(MainArea.SELECTED_START)))
            {
                _dtMod = MainArea.SELECTED_START;
                _doubleBuffer.Refresh();
            }
            else if (_ttC.IsDataRegionAtPointer(e, _toCode(MainArea.SELECTED_END)))
            {
                _dtMod = MainArea.SELECTED_END;
                _doubleBuffer.Refresh();
            }
            else if (_ttC.IsDataRegionAtPointer(e, _toCode(MainArea.SELECTED_WHOLE)) && ChartViewer.IsModifierHeld(Keys.Shift))
            {
                _dtMod = MainArea.SELECTED_WHOLE;
                _doubleBuffer.Refresh();

            }
            else if (_ttC.IsDataRegionAtPointer(e, _toCode(MainArea.TOPHEADER)))
            {
                // no handle click STARTS selection
                _dtHandleInit0 = _dateTimeAtPtr(e.X, false);
                _dtHandleInit1 = _dateTimeAtPtr(e.X, false);
                _fcWatcher.Table.SetTimeSelection(_dtHandleInit0, _dtHandleInit1, true);
                _dtMod = MainArea.SELECTED_END;
                _doubleBuffer.Refresh();
            }


            FileChangeProject proj = _ttC.GetDataAtPointer(e, _toCode(MainArea.PROJECT_LABEL)) as FileChangeProject;


            // if only 1 selected and right clicked, select this one only
            if (e.Button == MouseButtons.Right)
            {
                int count = _fcWatcher.Table.CountProjects(DataState.True, DataState.True);
                if (proj != null && count <= 1)
                {
                    // force this one right-clicked to be selected
                    _fcWatcher.Table.SelectProject(proj, SelectionBehavior.SelectOnly);
                    _doubleBuffer.Refresh();
                    return;
                }
            }

            if (e.Button != MouseButtons.Left) return;

            if (proj != null)
            {
                if (ChartViewer.IsModifierHeld(Keys.Alt))
                {
                    // make project invisible
                    proj.Visible = false;
                    _fcWatcher.UpdateActivity();
                    _fcWatcher.FireEvent();
                    return;
                }
                else if (ChartViewer.IsModifierHeld(Keys.Shift))
                {
                    // append project selection, keep all just toggle this one
                    _fcWatcher.Table.SelectProject(proj, SelectionBehavior.AppendToggle);
                    _doubleBuffer.Refresh();
                    return;
                }
                else
                {
                    // if selected, and there are others selected, leave only this selected
                    // if no others selected toggle it
                    _fcWatcher.Table.SelectProject(proj,
                        e.Button == MouseButtons.Left
                            ? SelectionBehavior.UnselectOnToggle
                            : SelectionBehavior.UnselectToggle);
                    _doubleBuffer.Refresh();
                    return;
                }
            }


            if (_ttC.IsDataRegionAtPointer(e, _toCode(MainArea.PROJECT_COLUMN)))
            {
                _fcWatcher.Table.SelectProject(null, SelectionBehavior.Unselect);
                _doubleBuffer.Refresh();
                return;
            }

            // view to present day
            if (_ttC.IsDataRegionAtPointer(e, _toCode(MainArea.PRESENT_AREA)))
            {
                var today = DateTime.Now.Date;
                _dt1 = today;
                _dt0 = _calcT0(today);
                _doubleBuffer.Refresh();
                return;
            }


            if (_ttC.GetDataAtPointer(e, _toCode(MainArea.DAYINFO)) is FileChangeDay pDay)
            {
                _setDayInfo(null);
                _doubleBuffer.Refresh();
                return;
            }

            pDay = _ttC.GetDataAtPointer(e, _toCode(MainArea.DAYBOX)) as FileChangeDay;
            if (pDay != null)
            {
                _setDayInfo(pDay == _pDayInfoDay ? null : pDay);
                _doubleBuffer.Refresh();
            }
        }

        private Point? _ptrTrack;

        private void _doubleBuffer_MouseMove(object sender, MouseEventArgs e)
        {
            if (_fcWatcher == null) return;

            DateTime mDt0 = _dateTimeAtPtr(e.X, _compensatedScaling);
            _labelDATE.Text = mDt0.ToString("D");

            int px = 2;
            TimeSpan deltaSpan = _dateTimeAtPtr(100 + px, false) - _dateTimeAtPtr(100, false);
            DateTime dispTime;
            if (deltaSpan.TotalDays >= 1.0)
            {
                var dayInc = (int)GeneralUtilities.Round125Above(deltaSpan.TotalDays);
                dispTime = FileChangeTable.Round(mDt0, new TimeSpan(dayInc, 0, 0, 0));
            }
            else if (deltaSpan.TotalHours >= 1.0)
            {
                var hrInc = (int)GeneralUtilities.Round125Above(deltaSpan.TotalHours);
                dispTime = FileChangeTable.Round(mDt0, new TimeSpan(0, hrInc, 0, 0));
            }
            else if (deltaSpan.TotalMinutes >= 1.0)
            {
                var minInc = (int)GeneralUtilities.Round125Above(deltaSpan.TotalMinutes);
                dispTime = FileChangeTable.Round(mDt0, new TimeSpan(0, 0, minInc, 0));
            }
            else
            {
                dispTime = mDt0;
            }



            _labelTIME.Text = dispTime.ToString("h:mmtt");

            Point? ptr = null;
            if (_showVerticalCursor) ptr = new Point(_screenxAtDateTime(dispTime), e.Y);
            if (ptr != _ptrTrack)
            {
                _ptrTrack = ptr;
                _doubleBuffer.Refresh();
            }

            TimeSpan deltaOffset = mDt0 - _dtInitClick;
            switch (_dtMod)
            {
                case MainArea.NONE:
                    break;
                case MainArea.SELECTED_WHOLE:
                    _fcWatcher.Table.SetTimeSelection(_dtHandleInit0 + deltaOffset, _dtHandleInit1 + deltaOffset, true);
                    _doubleBuffer.Refresh();
                    break;
                case MainArea.SELECTED_START:
                    _fcWatcher.Table.SetTimeSelection(_dtHandleInit0 + deltaOffset, _dtHandleInit1, true);
                    _doubleBuffer.Refresh();
                    break;
                case MainArea.SELECTED_END:
                    _fcWatcher.Table.SetTimeSelection(_dtHandleInit0, _dtHandleInit1 + deltaOffset, true);
                    _doubleBuffer.Refresh();
                    break;
            }

        }

        private void _doubleBuffer_MouseUp(object sender, MouseEventArgs e)
        {
            bool refresh = false;

            if (_dtMod != MainArea.NONE)
            {
                _dtMod = MainArea.NONE;
                _fcWatcher.UpdateActivity();
                refresh = true;
            }

            if (refresh) _doubleBuffer.Refresh();
        }

        private string _dayInfo;
        private FileChangeDay _pDayInfoDay;
        private void _setDayInfo(FileChangeDay pDay)
        {
            _pDayInfoDay = pDay;

            if (_pDayInfoDay != null)
            {
                _dayInfo = pDay.Project.Name + Environment.NewLine +
                           _niceDay(pDay.DateTime) + Environment.NewLine +
                           Environment.NewLine +
                           "   Start: " + pDay.EstimatedStart.ToString("h:mm tt") + Environment.NewLine +
                           "     End: " + pDay.EstimatedEnd.ToString("h:mm tt") + Environment.NewLine +
                           "Duration: " + pDay.EstimatedDuration.ToString(@"hh\:mm") + Environment.NewLine +
                           "   Edits: " + pDay.EditCount + Environment.NewLine +
                           Environment.NewLine +
                           pDay.GetChangedFiles(_editSort);
            }
            else
            {
                _dayInfo = null;
            }
        }

        private void _drawDayInfo(Graphics g, Rectangle? dayRectangle)
        {
            if (_pDayInfoDay == null) return;
            if (dayRectangle == null) return;

            int offset = _fixedWidthFont.Height;
            Rectangle dRect = (Rectangle)dayRectangle;
            var matches = Regex.Matches(_dayInfo, Environment.NewLine);
            int lineCount = matches.Count + 1;

            // main area rectangle attempt to fit inside this:
            Rectangle smRect = _mainClipRect;
            smRect.Height -= TOPHEADINGHEIGHT;
            smRect.Y = TOPHEADINGHEIGHT;
            smRect.Inflate(-5, -5);

            for (int columns = 1; columns <= 4; columns++)
            {
                // try to fit it into columns
                int iLineStep = (int)Math.Round((double)lineCount / columns, MidpointRounding.AwayFromZero);
                List<string> strList = new List<string>();
                int colWidth = 0;
                int boxHeight = 0;
                int iLine1 = 0;
                int i0 = 0;
                for (int i = 0; i < columns; i++)
                {
                    iLine1 += iLineStep;
                    int i1 = iLine1 < lineCount ? matches[iLine1 - 1].Index : _dayInfo.Length - 1;

                    string strSub = _dayInfo.Substring(i0, i1 - i0 + 1).Trim();
                    strList.Add(strSub);

                    var siz = g.MeasureString(strSub, _fixedWidthFont);
                    colWidth = Math.Max(colWidth, (int)(siz.Width + 0.5));
                    boxHeight = Math.Max(boxHeight, (int)(siz.Height + 0.5));
                    i0 = i1 + 1;
                }

                // multiply up widths
                int boxWidth = colWidth * columns + (columns - 1) * offset * 2;

                // try to fit
                Rectangle rect = new Rectangle(dRect.X + offset, dRect.Y + offset, boxWidth, boxHeight);
                rect.Inflate(2, 2);
                if (rect.Right > smRect.Right) rect.X = smRect.Right - rect.Width;
                if (rect.Left < smRect.Left) rect.X = smRect.Left;
                if (rect.Bottom > smRect.Bottom) rect.Y = smRect.Bottom - rect.Height;
                if (rect.Top < smRect.Top) rect.Y = smRect.Top;

                if (rect.Bottom <= smRect.Bottom)
                {
                    // border around
                    Paths clip1 = _pathFromRect(dRect);
                    Paths clip2 = _pathFromRect(Rectangle.Round(rect));
                    Paths union = new Paths();
                    Clipper c = new Clipper();
                    c.AddPaths(clip1, PolyType.ptSubject, true);
                    c.AddPaths(clip2, PolyType.ptClip, true);
                    c.Execute(ClipType.ctUnion, union, PolyFillType.pftNonZero, PolyFillType.pftNonZero);
                    Paths solution = new Paths();
                    ClipperOffset co = new ClipperOffset();
                    co.AddPath(union[0], JoinType.jtRound, EndType.etClosedPolygon);
                    co.Execute(ref solution, 1.0);

                    g.DrawRectangle(_theme.Window.Medium.Pen, rect);
                    g.FillRectangle(_theme.Window.VLow.Brush, rect);
                    g.DrawRectangle(_theme.Window.Medium.Pen, rect);
                    solution.ForEach(poly => _drawPoly(g, _theme.Highlight.Foreground.Pen2, poly));

                    int x = rect.X + 2;
                    foreach (var subStr in strList)
                    {
                        g.DrawString(subStr, _fixedWidthFont, _theme.Window.High.Brush, x, rect.Y + 3);
                        x += (colWidth + offset * 2);
                    }

                    _ttC.Add(_toCode(MainArea.DAYINFO), rect, null, _pDayInfoDay);
                    break;
                }
            }

        }

        private void _drawPoly(Graphics g, Pen pen, Path poly)
        {
            if (poly.Count > 1)
                g.DrawPolygon(pen, poly.Select(pt => new Point((int)pt.X, (int)pt.Y)).ToArray());
        }

        private Paths _pathFromRect(Rectangle rect)
        {
            Paths clip = new Paths(1) { new Path(4) };
            clip[0].Add(new IntPoint(rect.Left, rect.Top));
            clip[0].Add(new IntPoint(rect.Left, rect.Bottom));
            clip[0].Add(new IntPoint(rect.Right, rect.Bottom));
            clip[0].Add(new IntPoint(rect.Right, rect.Top));
            return (clip);
        }

        private void DoubleBuffer1_MouseWheel(object sender, MouseEventArgs e)
        {
            int click = e.Delta / 120;

            if (ChartViewer.IsModifierHeld(Keys.Shift) && ChartViewer.IsModifierHeld(Keys.Control))
            {
                // day at mouse..
                var cX = (_doubleBuffer.Width + _leftDemark) / 2;
                double ptrProp = ((double)(cX - _leftDemark)) / (_doubleBuffer.Width - _leftDemark - FUTCOLWIDTH);
                var ptrDate = _dt0.AddDays(ptrProp * DDaySpan);
                _incDaySpan(-click);

                // keep moused-over day at center
                var dtAtPtr = _dateTimeAtPtr(e.X, _compensatedScaling);

                // re-center
                _dt0 = ptrDate.AddDays(-(ptrProp * DDaySpan));
                _dt1 = _calcT1(_dt0);
                _toPresent = false;
                _doubleBuffer.Refresh();
            }
            else if (ChartViewer.IsModifierHeld(Keys.Control))
            {
                _incRowHeight(click * 3);
                _doubleBuffer.Refresh();
            }
            else if (ChartViewer.IsModifierHeld(Keys.Shift))
            {
                _incDayStart(_scrollDays * click);
                _doubleBuffer.Refresh();
            }
            else
            {
                _incJoffset(-click);
                _doubleBuffer.Refresh();
            }
        }

        // ReSharper disable once UnusedMember.Global
        public FileChangeProject GetProjectAtPointer()
        {
            var ptr = _doubleBuffer.PointToClient(Cursor.Position);
            FileChangeProject proj = _ttC.GetDataAtPointer(ptr, _toCode(MainArea.PROJECT_LABEL)) as FileChangeProject;
            return (proj);
        }

        readonly ColorForm _colorForm = new ColorForm();
        public void EditColor()
        {
            int count = _fcWatcher.Table.CountProjects(DataState.True, DataState.True);
            if (count == 0) return;

            // get the first project onscreen that is selected
            DataRegion dReg = null;
            _fcWatcher.Table.ProjectCollection.ForEach(proj =>
            {
                if (dReg == null)
                    dReg = _ttC.GetDataRegionWithData(_toCode(MainArea.PROJECT_LABEL), proj);
            });
            FileChangeProject firstSelProject = _fcWatcher.Table.ProjectCollection.FirstOrDefault(proj => proj.Selected && proj.Visible);

            //var fProj = _fcWatcher.Table.ProjectCollection.FirstOrDefault(proj => proj.Selected && proj.Visible);
            var pt = dReg != null
                ? _doubleBuffer.PointToScreen(new Point(dReg.Rectangle.Right - 7, dReg.Rectangle.Top))
                : _doubleBuffer.PointToScreen(new Point(_leftDemark - 7, TOPHEADINGHEIGHT));

            if (firstSelProject != null)
            {
                _colorForm.SelectedColor = count > 1 ? Color.Gray : firstSelProject.Color;
                _colorForm.InfoText = count > 1 ? "multiple" : firstSelProject.Name;
            }
            else
            {
                _colorForm.SelectedColor = Color.Gray;
                _colorForm.InfoText = "";
            }
            _colorForm.Location = pt;
            _fcWatcher.Table.ProjectCollection.ForEach(proj =>
            {
                if (proj.Visible) _colorForm.AppendUserColors(proj.Color);
            });

            var dR = _colorForm.ShowDialog();

            if (dR == DialogResult.OK)
                _fcWatcher.Table.ProjectCollection.ForEach(proj =>
                {
                    if (proj.Selected && proj.Visible) proj.Color = _colorForm.SelectedColor;
                });
            _doubleBuffer.Refresh();
        }

        public void RandomColors()
        {
            _fcWatcher.Table.ProjectCollection.ForEach(proj => { if (proj.Selected && proj.Visible) proj.RandomizeColor(); });
            _doubleBuffer.Refresh();
        }

        List<int> _rowHeightStore = new List<int>();

        public void StepRowHeights()
        {
            // maybe keep last height that is not max or min (if any)
            int lastH = _rowHeightStore.LastOrDefault(h => h != _minRowHeight && h != _maxRowHeight && h != _defaultRowHeight &&
                h != _fitRowHeight);

            // add back
            _rowHeightStore.Clear();
            if (_rowHeight != _minRowHeight && _rowHeight != _maxRowHeight && _rowHeight != _defaultRowHeight && _rowHeight != _fitRowHeight)
                _rowHeightStore.Add(_rowHeight);
            else if (lastH != 0) _rowHeightStore.Add(lastH);
            _rowHeightStore.Add(_defaultRowHeight);
            _rowHeightStore.Add(_minRowHeight);
            _rowHeightStore.Add(_maxRowHeight);
            _rowHeightStore.Add(_fitRowHeight);
            _rowHeightStore.RemoveAll(h => h > _maxRowHeight);
            _rowHeightStore = _rowHeightStore.Distinct().ToList();

            _rowHeightStore.Sort();

            int idx = _rowHeightStore.IndexOf(_rowHeight);
            if (idx == -1) idx = _rowHeightStore.Count - 1;
            idx++;
            idx = idx % _rowHeightStore.Count;

            // user set
            _rowHeight = _rowHeightStore[idx];
            _doubleBuffer.Refresh();
        }
        public void SameRandomColor()
        {
            var color = FileChangeProject.ColorRotator.Random();
            _fcWatcher.Table.ProjectCollection.ForEach(proj => { if (proj.Selected && proj.Visible) proj.Color = color; });
            _doubleBuffer.Refresh();
        }

        // ReSharper disable once UnusedMember.Global
        public MainArea GetAreasPointedAt()
        {
            var ptr = _doubleBuffer.PointToClient(Cursor.Position);

            MainArea areas = MainArea.NONE;
            _ttC.Collection.ForEach(dr =>
            {
                if (dr.Rectangle.Contains(ptr))
                {
                    if (Enum.TryParse(dr.Code, out MainArea tmp)) areas |= tmp;
                }
            });

            return (areas);
        }

        // ReSharper disable once UnusedMember.Global
        public bool AnyAreasPointedAt(MainArea areaReq)
        {
            var ptr = _doubleBuffer.PointToClient(Cursor.Position);

            var dR = _ttC.Collection.FirstOrDefault(dr => (dr.Rectangle.Contains(ptr) &&
                                                           Enum.TryParse(dr.Code, out MainArea tmp) &&
                                                           areaReq.HasFlag(tmp)));

            return (dR != null);
        }

    }

}

using ChartLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace CodeWatcher
{
    public enum TIMEPERIOD
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

    enum MP_ACTION
    {
        NONE,
        SELECTING_TIMERANGE,
        APPENDSELECTING_TIMERANGE,
        MOVING_EDGE,
        CREATE_TIMERANGE
    }
    class MainPanel
    {
        FileChangeWatcher _fcWatcher;
        DoubleBuffer _doubleBuffer;
        ThemeColors _theme;
        Font _font;
        TooltipContainer ttC;
        bool _toPresent = true;
        DateTime dt0;
        DateTime dt1;
        int _iDaySpan = 30;
        double _dDaySpan = 30;
        int _rowHeight;
        int _jOffset = 0, _maxJOffset = 10;
        int _scrollDays;

        int BORDER = 10;
        int BORDERHEADER = 60;
        int PROJCOLWIDTH = 100;
        int INFOCOLUMNWIDTH = 100;
        int MINBOXH = 4;
        int MINBOXW = 2;
        int MINGAPPEDBOXSPACE = 7;
        int TINYSPACE = 2;
        int FUTCOLWIDTH = 20;
        int ROWHEIGHTMAX = 100;
        int ROWHEIGHTINC = 3;

        double REDZONE = 0.8;
        double ORANGEZONE = 0.6;
        double YELLOWZONE = 0.4;
        double GREENZONE = 0.2;

        Brush _transBr;

        TextBox tBox;

        public bool _showInfoColumn = true;

        public MainPanel(FileChangeWatcher fcw, DoubleBuffer doubleBuffer, ThemeColors theme)
        {
            _fcWatcher = fcw;
            _doubleBuffer = doubleBuffer;
            _theme = theme;
            _font = _doubleBuffer.Font;
            _doubleBuffer.MouseWheel += DoubleBuffer1_MouseWheel;
            _doubleBuffer.MouseClick += _doubleBuffer_MouseClick;
            _doubleBuffer.MouseMove += _doubleBuffer_MouseMove;
            _doubleBuffer.MouseDown += _doubleBuffer_MouseDown;
            _doubleBuffer.MouseUp += _doubleBuffer_MouseUp;
            _doubleBuffer.PaintEvent += _doubleBuffer_PaintEvent;
            ttC = new TooltipContainer(_doubleBuffer);
            MidnightNotifier.DayChanged += (s, e) => { _doubleBuffer.Invalidate(); };// make sure it ticks over to next day

            tBox = new TextBox();
            tBox.Dock = DockStyle.Bottom;
            tBox.ReadOnly = true;
            doubleBuffer.Parent.Controls.Add(tBox);

            _rowHeight = _font.Height * 2;
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
        internal void ShowPresentToLast(TIMEPERIOD timePeriod)
        {
            switch (timePeriod)
            {
                case TIMEPERIOD.ONEWEEK:
                    _jOffset = 0;
                    _toPresent = true;
                    IDaySpan = 7;
                    _doubleBuffer.Refresh();
                    break;
                case TIMEPERIOD.ONEMONTH:
                    _jOffset = 0;
                    _toPresent = true;
                    IDaySpan = FileChangeTable.InclusiveDaySpan(DateTime.Now.AddMonths(-1), DateTime.Now);
                    _doubleBuffer.Refresh();
                    break;
                case TIMEPERIOD.THREEMONTHS:
                    _jOffset = 0;
                    _toPresent = true;
                    IDaySpan = FileChangeTable.InclusiveDaySpan(DateTime.Now.AddMonths(-3), DateTime.Now);
                    _doubleBuffer.Refresh();
                    break;
                case TIMEPERIOD.SIXMONTHS:
                    _jOffset = 0;
                    _toPresent = true;
                    IDaySpan = FileChangeTable.InclusiveDaySpan(DateTime.Now.AddMonths(-6), DateTime.Now);
                    _doubleBuffer.Refresh();
                    break;
                case TIMEPERIOD.ONEYEAR:
                    _jOffset = 0;
                    _toPresent = true;
                    IDaySpan = FileChangeTable.InclusiveDaySpan(DateTime.Now.AddYears(-1), DateTime.Now);
                    _doubleBuffer.Refresh();
                    break;
                case TIMEPERIOD.ALLTIME:
                    _jOffset = 0;
                    _toPresent = true;
                    IDaySpan = FileChangeTable.InclusiveDaySpan(_fcWatcher.Table.StartTime, DateTime.Now);
                    _doubleBuffer.Refresh();
                    break;
                default:
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
            Properties.Settings.Default.MinDateTime = dt0;
            Properties.Settings.Default.MaxDateTime = dt1;
            Properties.Settings.Default.ToPresent = _toPresent;
            Properties.Settings.Default.Joffset = _jOffset;
            Properties.Settings.Default.RowHeight = _rowHeight;
            Properties.Settings.Default.ShowInfoColumn = _showInfoColumn;

            Properties.Settings.Default.Save();
        }
        internal void LoadSettings()
        {
            dt0 = Properties.Settings.Default.MinDateTime;
            dt1 = Properties.Settings.Default.MaxDateTime;
            _toPresent = Properties.Settings.Default.ToPresent;
            _jOffset = Properties.Settings.Default.Joffset;
            _rowHeight = Properties.Settings.Default.RowHeight;
            _showInfoColumn = Properties.Settings.Default.ShowInfoColumn;
        }




        private DateTime _dateTimeAtPtr(int x)
        {
            try
            {
                double d = (double)((x - leftDemark) / boxSpace);
                return (dt0.AddDays(d));
            }
            catch (Exception ex)
            {
                return (dt0);
            }
        }
        private int _screenxAtPtr(DateTime dt)
        {
            int sx = (int)((dt - dt0.Date).TotalDays * boxSpace + leftDemark + 0.5 - boxOff);
            return (sx);
        }


        float leftDemark;
        float boxSpace;
        float boxOff;
        string CODE_PROJLABEL = "projLabel";
        string CODE_PROJROW = "projRow";

        private void _doubleBuffer_PaintEvent(object sender, PaintEventArgs e)
        {
            try
            {
                _transBr = new SolidBrush(Color.FromArgb(100, _theme.AccentControl2.Bright.Color));
                Graphics g = e.Graphics;
                int width = e.ClipRectangle.Width;
                int height = e.ClipRectangle.Height;
                ttC.Clear();

                g.Clear(_theme.Window.Background.Color);
                FileChangeTable _table = null;
                if (_fcWatcher == null ||
                _fcWatcher.Table == null ||
                (_table = _fcWatcher.Table) == null ||
               _table.ItemCount == 0)
                {
                    g.DrawString("NO DATA!", _font, Brushes.Red, BORDER, BORDERHEADER);
                    return;
                }

                DateTime today = DateTime.Now.Date;
                // day range
                if (_toPresent) dt1 = today;
                dt0 = _calcT0(dt1);

                //g.DrawString(dt0.ToString(), _font, Brushes.Red, 150, 5);
                //g.DrawString(dt1.ToString(), _font, Brushes.Red, 350, 5);
                //g.DrawString("TOPRESENT:" + _toPresent, _font, _toPresent ? Brushes.Blue : Brushes.DarkRed, 500, 5);
                //g.DrawString("DAYS:" + IDaySpan, _font, _toPresent ? Brushes.Blue : Brushes.DarkRed, 600, 5);
                //g.DrawString("DATAFROM:" + _table.StartTime.Date, _font, Brushes.DarkMagenta, 700, 5);

                INFOCOLUMNWIDTH = 70;
                int THINBANNERHEIGHT = 4;
                int fH = _font.Height;
                leftDemark = BORDER + PROJCOLWIDTH + (_showInfoColumn ? INFOCOLUMNWIDTH : 0);
                float playWidth = (width - leftDemark - FUTCOLWIDTH);
                boxSpace = playWidth / (IDaySpan);
                int boxW = (int)(boxSpace > MINGAPPEDBOXSPACE ? (boxSpace - TINYSPACE * 2) : boxSpace);
                if (boxW < MINBOXW) boxW = MINBOXW;//not too small, always visible
                int boxCenterOff = boxW / 2;
                boxOff = (boxSpace - boxW) / 2;
                _scrollDays = Math.Max(1, (int)(50 / boxSpace));

                bool showDayOfWeek = boxSpace > fH;
                bool showDayOfMonth = boxSpace > fH;
                bool showMondays = boxSpace * 7 > fH * 2;
                bool showDayVerticals = boxSpace > fH;
                bool showFullBanner = _rowHeight > fH * 2;
                int boxH = showFullBanner ? _rowHeight - fH - 2 : _rowHeight - THINBANNERHEIGHT;
                Rectangle mainClipRect = new Rectangle((int)leftDemark, 0, width, height);

                // projects with activity in shown time range
                //List<FileChangeProject> projWithActivity = _table.GetProjectsWithActivity(dt0, dt1);
                int peakOpsInDay = _table.GetPeakOpsInDay();


                int playHeight = (height - BORDERHEADER);
                _maxJOffset = _table.VisibleProjectCount - (playHeight / _rowHeight);
                if (_maxJOffset < 0) _maxJOffset = 0;
                _incJoffset(0);

                Brush br;
                Brush fgBr = _theme.Window.Foreground.Brush;
                Brush bgBr = _theme.Window.Background.Brush;
                Brush bgLightGrayBr = _theme.Window.VLow.Brush;
                Brush bgGrayBr = _theme.Window.Low.Brush;
                Brush mo1Br = _theme.AccentWindow1.High.Brush;
                Brush mo2Br = _theme.AccentWindow2.High.Brush;
                Brush mo1CBr = _theme.AccentWindow1.High.ContrastBrush;
                Brush mo2CBr = _theme.AccentWindow2.High.ContrastBrush;
                Brush weekendBrushA = _theme.AccentWindow1.Low.Brush;
                Brush weekendBrushB = _theme.AccentWindow1.VLow.Brush;
                Brush hiliteBr = _theme.Highlight.Foreground.Brush;
                Brush titleBr = _theme.Window.Medium.Brush;
                Brush hiliteTxtBr = _theme.Highlight.Foreground.ContrastBrush;
                Brush titleTxtBr = _theme.Window.Medium.ContrastBrush;
                bool presentDayShown = false;
                // draw
                float fx;
                int y;


                g.DrawString("Project", _font, fgBr, BORDER, BORDERHEADER - fH);
                if (_showInfoColumn)
                    g.DrawString("wd:hr:mn", _font, fgBr, BORDER + BORDER + PROJCOLWIDTH, BORDERHEADER - fH);



                List<float> verts = new List<float>();
                List<float> dayVerts = new List<float>();
                int yMonth = BORDERHEADER;
                // row per project
                y = BORDERHEADER;
                int ic = 0;
                int idxJ = -1;
                foreach (var proj in _table.EachVisibleProject())
                {
                    idxJ++;
                    if (idxJ < _jOffset) continue;
                    if (y > height) break;

                    ic++;
                    fx = BORDER;
                    Rectangle rectBG = new Rectangle(0, y, width, _rowHeight);
                    Brush dayBrush;
                    Brush weekendBrush;
                    if (idxJ % 2 == 1) { dayBrush = bgGrayBr; weekendBrush = weekendBrushA; }
                    else { dayBrush = bgBr; weekendBrush = weekendBrushB; }
                    g.FillRectangle(dayBrush, rectBG);

                    var py = y + (_rowHeight - _font.Height) / 2;
                    var clipRect = rectBG;
                    clipRect.Width = (int)leftDemark;
                    g.SetClip(clipRect);
                    g.FillRectangle(proj.Brush, clipRect);
                    g.DrawString(proj.Name, _font, proj.ContrastBrush, (int)fx, py);

                    if (_showInfoColumn)
                    {
                        var infoRect = clipRect;
                        infoRect.X = clipRect.Right - INFOCOLUMNWIDTH;
                        infoRect.Width = INFOCOLUMNWIDTH;
                        g.FillRectangle(bgLightGrayBr, infoRect);
                        g.DrawString(proj.ActivityTrace.Summary, _font, fgBr, infoRect.X + BORDER, py);
                    }


                    g.ResetClip();

                    Rectangle pRect = rectBG;
                    pRect.Width = (int)leftDemark;
                    ttC.Add(CODE_PROJROW, rectBG, null, proj);
                    ttC.Add(CODE_PROJLABEL, pRect, proj.Path, proj);

                    fx = leftDemark;
                    int icDay = -1;
                    int year = -999;




                    g.SetClip(mainClipRect);
                    // iterate thru days
                    foreach (DateTime date in FileChangeTable.EachDay(dt0, dt1))
                    {
                        var projDay = proj.GetDay(date);
                        if (date == today) presentDayShown = true;

                        icDay++;
                        if (fx > width) break;
                        if (ic == 1)
                        {
                            int tY = y - fH;
                            int boxCenter = (int)fx + boxCenterOff;
                            if (showDayOfWeek) { _drawCenteredString(g, date.ToString("ddd").Substring(0, 1), _font, fgBr, boxCenter, tY); tY -= fH; }

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
                                g.FillRectangle(date.Month % 2 == 0 ? mo1Br : mo2Br, (int)(fx - boxOff + 0.5), tY, width - fx, fH);
                                g.DrawString(date.ToString("MMMM"), _font, date.Month % 2 == 0 ? mo1CBr : mo2CBr, fx, tY); tY -= fH;
                            }

                            if (year != date.Year) //YEAR
                            {
                                year = date.Year;
                                g.DrawString(date.Year.ToString(), _font, fgBr, fx, tY); tY -= fH;
                            }
                            // START OF WEEK
                            if (date.DayOfWeek == DayOfWeek.Monday ||
                                date.DayOfWeek == DayOfWeek.Saturday)
                                verts.Add(fx - boxOff);

                            if (showDayVerticals) dayVerts.Add(fx - boxOff);
                        }

                        if (date.DayOfWeek == DayOfWeek.Saturday ||
                            date.DayOfWeek == DayOfWeek.Sunday)
                        {
                            Rectangle weRect = rectBG;
                            weRect.X = (int)(fx - boxOff + 0.5);
                            weRect.Width = (int)(boxSpace + 1);
                            g.FillRectangle(weekendBrush, weRect);
                        }

                        if (projDay != null && projDay.Count > 0)
                        {
                            // blue, green, yellow, orange, red
                            //        5+      10+    15+    20+
                            double activity = ((double)projDay.Count) / peakOpsInDay;
                            int bH = boxH;
                            //  int bH = (int)(boxH * activity);
                            //     if (bH < MINBOXH) bH = MINBOXH; 
                            Rectangle rect = new Rectangle((int)fx, y + _rowHeight - bH, boxW, bH);
                            if (activity > REDZONE) br = Brushes.Red;
                            else if (activity > ORANGEZONE) br = Brushes.Orange;
                            else if (activity > YELLOWZONE) br = Brushes.Gold;
                            else if (activity > GREENZONE) br = Brushes.ForestGreen;
                            else br = Brushes.CadetBlue;

                            g.FillRectangle(br, rect);
                        }


                        fx += boxSpace;
                    }


                    int EDGE_WIDTH = 13;
                    int EDGE_HALFWIDTH = 6;
                    int MINRECTWIDTH = 1;
                    int EDGE_VISIBLEWIDTH = 4;
                    int EDGE_VISIBLEOFFSET = 2;
                    foreach (var trb in proj.TimeBoxCollection)
                    {
                        int sx0 = _screenxAtPtr(trb.StartDate);
                        int sx1 = _screenxAtPtr(trb.EndDate);
                        Rectangle trbRect = rectBG;
                        trbRect.X = sx0;
                        trbRect.Width = Math.Max(sx1 - sx0, MINRECTWIDTH);
                        g.FillRectangle(_transBr, trbRect);

                        // banner
                        Rectangle innerRect = trbRect;
                        innerRect.Height = showFullBanner ? fH : THINBANNERHEIGHT; //        Math.Min(fH, _rowHeight / 2);
                        g.FillRectangle(trb.WholeState.HasFlag(TRB_STATE.SELECTED) ? hiliteBr : titleBr, innerRect);
                        if (showFullBanner)
                            g.DrawString(trb.StartDate.ToString("dd/MM/yyyy") + " " + trb.TimeSpan.TotalDays.ToString("f0") + " days",
                                _font, trb.WholeState.HasFlag(TRB_STATE.SELECTED) ? hiliteTxtBr : titleTxtBr,
                                trbRect.X + 2, trbRect.Y);

                        ttC.Add(_toCode(TRB_PART.WHOLE), trbRect, null, trb);


                        // EDGES
                        // g.DrawString(trb.StartDate.ToString(), _font, Brushes.OrangeRed, trbRect.X + 2, trbRect.Y);
                        //g.DrawString(trb.EndDate.ToString(), _font, Brushes.OrangeRed, trbRect.Right, trbRect.Y + fH);

                        int cX = (trbRect.Left + trbRect.Right) / 2;
                        int r = Math.Min(cX, trbRect.Left + EDGE_HALFWIDTH);
                        int l = r - EDGE_WIDTH;
                        Rectangle rectEdge = trbRect;
                        rectEdge.X = l;
                        rectEdge.Width = EDGE_WIDTH;
                        ttC.Add(_toCode(TRB_PART.START), rectEdge, null, trb);
                        if (trb.StartState.HasFlag(TRB_STATE.SELECTED))
                        {
                            rectEdge.X = trbRect.X - EDGE_VISIBLEOFFSET;
                            rectEdge.Width = EDGE_VISIBLEWIDTH;
                            g.FillRectangle(hiliteBr, rectEdge);
                        }

                        l = Math.Max(cX, trbRect.Right - EDGE_HALFWIDTH);
                        r = l + EDGE_WIDTH;
                        rectEdge = trbRect;
                        rectEdge.X = l;
                        rectEdge.Width = EDGE_WIDTH;
                        ttC.Add(_toCode(TRB_PART.END), rectEdge, null, trb);
                        if (trb.EndState.HasFlag(TRB_STATE.SELECTED))
                        {
                            rectEdge.X = trbRect.Right - EDGE_VISIBLEOFFSET;
                            rectEdge.Width = EDGE_VISIBLEWIDTH;
                            g.FillRectangle(hiliteBr, rectEdge);
                        }
                        g.DrawRectangle(_theme.Window.High.Pen2, trbRect);
                    }


                    // ACTIVITY TRACE
                    if (boxSpace > fH && 
                        proj.ActivityTrace != null && proj.ActivityTrace.ActivityLine != null)
                        foreach (var aLine in proj.ActivityTrace.ActivityLine)
                        {
                            if (aLine.ActivityAmount == 0) continue;
                            int sx0 = _screenxAtPtr(aLine.StartDate);
                            int sx1 = _screenxAtPtr(aLine.EndDate);
                            int traceH = (int)(aLine.ActivityAmount * boxH);
                            if (traceH < 3) traceH = 3;
                            //int traceY = y + _rowHeight - traceH;
                            int aW = Math.Max(sx1 - sx0, 3);
                            g.FillRectangle(_theme.Window.Bright.Brush, sx0, y + _rowHeight - traceH, aW, traceH);
                        }

                    //string work = proj.GetSelectedWorkSummary();
                    //if (work != null) g.DrawString(work, _font, fgBr, leftDemark, y);

                    g.ResetClip();
                    y += _rowHeight;
                }

                g.DrawString(_table.WorkSummary, _font, fgBr, 400, 100);

                // horiz
                g.DrawLine(_theme.Window.Medium.Pen, 0, BORDERHEADER, width, BORDERHEADER);
                // vert
                g.DrawLine(_theme.Window.High.Pen2, leftDemark, 0, leftDemark, height);
                g.SetClip(mainClipRect);
                foreach (var vx in dayVerts) g.DrawLine(_theme.VeryTranslucentPen, vx, yMonth, vx, height);
                foreach (var vx in verts) g.DrawLine(_theme.TranslucentPen, vx, yMonth, vx, height);
                g.ResetClip();

                // theme.DrawSwatch(g, 300, 200);


                if (_action == MP_ACTION.SELECTING_TIMERANGE ||
                    _action == MP_ACTION.APPENDSELECTING_TIMERANGE)
                {
                    int bx = Math.Min(box0.X, box1.X);
                    int by = Math.Min(box0.Y, box1.Y);
                    int bw = Math.Abs(box0.X - box1.X);
                    int bh = Math.Abs(box0.Y - box1.Y);
                    Rectangle rect = new Rectangle(bx, by, bw, bh);
                    g.DrawRectangle(_theme.Highlight.High.Pen2, rect);
                }



                if (presentDayShown && _toPresent == false)
                {
                    _toPresent = true;
                    // wipe, go back to start!
                    _doubleBuffer_PaintEvent(sender, e);
                }
            }
            catch (Exception ex)
            {

            }
        }

        internal void CopyWorkSummaryToClipboard()
        {
            string summary = _fcWatcher.Table.GetWorkSummary();
            Clipboard.SetText(summary);
        }

        internal void ShowInfoColumn(bool chk)
        {
            _showInfoColumn = chk;
            _doubleBuffer.Refresh();
        }


        internal void TimeBoxSelectAll()
        {
            _fcWatcher.Table.SetState(TRB_PART.WHOLE, TRB_STATE.SELECTED);
            _doubleBuffer.Refresh();
        }
        internal void TimeBoxDeleteSelected()
        {
            _fcWatcher.Table.TimeBoxDelete(TRB_PART.WHOLE, TRB_STATE.SELECTED);
            _doubleBuffer.Refresh();
        }

        private string _toCode(TRB_PART part)
        {
            return (part.ToString());
        }
        private TRB_PART _fromPartCode(string str)
        {
            TRB_PART part;
            if (Enum.TryParse(str, out part)) return (part);
            return (TRB_PART.NONE);
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
            dt0 = dt0.AddDays(inc);

            if (_fcWatcher != null && _fcWatcher.Table != null)
            {
                if (dt0 < _fcWatcher.Table.StartTime.Date) dt0 = _fcWatcher.Table.StartTime.Date;
                var tmpT1 = _calcT1(dt0);
                var today = DateTime.Now.Date;
                if (tmpT1 > today) tmpT1 = today;
                dt0 = _calcT0(tmpT1);
            }

            dt1 = _calcT1(dt0);
            //Console.WriteLine(dt0.ToString() + " -> " + dt1.ToString());
        }


        void _incDaySpan(int inc)
        {
            DDaySpan = (int)(DDaySpan * Math.Pow(1.2, inc));
            if (!_toPresent) dt1 = _calcT1(dt0);
        }

        void _incRowHeight(int inc)
        {
            _rowHeight += inc;
            if (_rowHeight > ROWHEIGHTMAX) _rowHeight = ROWHEIGHTMAX;
            if (_rowHeight < _font.Height) _rowHeight = _font.Height;
        }
        double DDaySpan
        {
            get { return (_dDaySpan); }
            set
            {
                _dDaySpan = value; _iDaySpan = (int)Math.Round(_dDaySpan, MidpointRounding.AwayFromZero);
                if (IDaySpan < 7) IDaySpan = 7;
            }
        }
        int IDaySpan
        { get { return (_iDaySpan); } set { _iDaySpan = value; if (_iDaySpan < 7) _iDaySpan = 7; _dDaySpan = _iDaySpan; } }

        DateTime _calcT0(DateTime dt) { return (dt.AddDays(-(IDaySpan - 1))); }
        DateTime _calcT1(DateTime dt) { return (dt.AddDays(IDaySpan - 1)); }

        Point box0;
        Point box1;
        Point boxMin;
        Point boxMax;
        TRB_PART partMov;
        TimeBox trbMov;
        TimeSpan snapTime;
        MP_ACTION _action = MP_ACTION.NONE;
        bool _multi = false;

        private void _initBox(Point location)
        {
            box0 = location;
            box1 = location;
            boxMin = location;
            boxMax = location;
        }
        private void _updateBox(Point location)
        {
            box1 = location;
            boxMin = new Point(Math.Min(Math.Min(box0.X, box1.X), boxMin.X), Math.Min(Math.Min(box0.Y, box1.Y), boxMin.Y));
            boxMax = new Point(Math.Max(Math.Max(box0.X, box1.X), boxMax.X), Math.Max(Math.Max(box0.Y, box1.Y), boxMax.Y));
        }
        private Rectangle _getBoxRectangle()
        {
            int x = Math.Min(box0.X, box1.X);
            int y = Math.Min(box0.Y, box1.Y);
            int w = Math.Abs(box0.X - box1.X);
            int h = Math.Abs(box0.Y - box1.Y);
            Rectangle rect = new Rectangle(x, y, w, h);
            rect.Inflate(rect.Width == 0 ? 2 : 0, rect.Height == 0 ? 2 : 0);
            return (rect);
        }
        private Rectangle _getBoxMaxRectangle()
        {
            int x = Math.Min(box0.X, box1.X);
            int y = Math.Min(box0.Y, box1.Y);
            int w = Math.Abs(box0.X - box1.X);
            int h = Math.Abs(box0.Y - box1.Y);
            Rectangle rect = new Rectangle(boxMin.X, boxMin.Y, boxMax.X - boxMin.X, boxMax.Y - boxMin.Y);
            rect.Inflate(rect.Width == 0 ? 2 : 0, rect.Height == 0 ? 2 : 0);
            return (rect);
        }

        private void _doubleBuffer_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            if (ChartViewer.IsModifierHeld(Keys.Control))
            {
                // create
                _action = MP_ACTION.CREATE_TIMERANGE;
                _multi = ChartViewer.IsModifierHeld(Keys.Shift);
                // start box select
                _initBox(e.Location);
                // get snapshot of ALL timeranges
                _fcWatcher.Table.UnsetState(TRB_PART.WHOLE, TRB_STATE.SELECTED);
                _storeTimeBoxes();
                _createTimeBoxes(true);
                _doubleBuffer.Refresh();
            }
            else if (ChartViewer.IsModifierHeld(Keys.Shift))
            {
                // un/append selection
                _action = MP_ACTION.APPENDSELECTING_TIMERANGE;
                _initBox(e.Location);
                // other??
                _storeTimeBoxes();
                _selectWithBox(true);
                _doubleBuffer.Refresh();
            }
            else
            {
                // grab edge?
                partMov = TRB_PART.NONE;
                trbMov = ttC.GetDataAtPointer(e, _toCode(TRB_PART.START)) as TimeBox;
                if (trbMov != null) { partMov = TRB_PART.START; }
                else
                {
                    trbMov = ttC.GetDataAtPointer(e, _toCode(TRB_PART.END)) as TimeBox;
                    if (trbMov != null) { partMov = TRB_PART.END; }
                }

                if (trbMov != null)
                {
                    _action = MP_ACTION.MOVING_EDGE;
                    // if trbMov is one of whole selected, move many
                    if (trbMov.WholeState.HasFlag(TRB_STATE.SELECTED))
                    {
                    }
                    else
                    {
                        // otherwise whole select it and carry on
                        _fcWatcher.Table.WriteState(TRB_PART.WHOLE, TRB_STATE.NONE); // deselect all
                        trbMov.SetState(TRB_PART.WHOLE, TRB_STATE.SELECTED);// then select
                    }
                    snapTime = _dateTimeAtPtr(110) - _dateTimeAtPtr(100);
                    DateTime mDT0 = _dateTimeAtPtr(e.X);
                    _fcWatcher.Table.MoveSelectedEdges(trbMov, partMov, mDT0, snapTime, TRB_ACTION.BEGIN);
                }
                else
                {
                    // start box select
                    _action = MP_ACTION.SELECTING_TIMERANGE;
                    _initBox(e.Location);
                    // other??
                    _selectWithBox(false);
                    _doubleBuffer.Refresh();
                }
            }
        }

        private void _doubleBuffer_MouseMove(object sender, MouseEventArgs e)
        {
            if (_fcWatcher == null) return;

            DateTime mDT0 = _dateTimeAtPtr(e.X);
            tBox.Text = mDT0.ToString();

            _updateBox(e.Location);

            switch (_action)
            {
                case MP_ACTION.NONE:
                    break;
                case MP_ACTION.CREATE_TIMERANGE:
                    _restoreTimeBoxes();
                    _createTimeBoxes(false);
                    _doubleBuffer.Refresh();
                    break;
                case MP_ACTION.SELECTING_TIMERANGE:
                    _selectWithBox(false);
                    _doubleBuffer.Refresh();
                    break;
                case MP_ACTION.APPENDSELECTING_TIMERANGE:
                    _reselectTimeBoxes();
                    _selectWithBox(true);
                    _doubleBuffer.Refresh();
                    break;
                case MP_ACTION.MOVING_EDGE:
                    if (trbMov != null)
                        _fcWatcher.Table.MoveSelectedEdges(trbMov, partMov, mDT0, snapTime, TRB_ACTION.MOVE);
                    _doubleBuffer.Refresh();
                    break;
                default:
                    break;
            }
        }

        private void _doubleBuffer_MouseUp(object sender, MouseEventArgs e)
        {
            bool refresh = false;

            switch (_action)
            {
                case MP_ACTION.NONE:
                    break;
                case MP_ACTION.CREATE_TIMERANGE:
                    ActivityTraceBuilder.Build(_fcWatcher.Table);
                    refresh = true;
                    break;
                case MP_ACTION.SELECTING_TIMERANGE:
                    refresh = true;
                    break;
                case MP_ACTION.APPENDSELECTING_TIMERANGE:
                    refresh = true;
                    break;
                case MP_ACTION.MOVING_EDGE:
                    // clear selected/armed edges
                    DateTime mDT0 = _dateTimeAtPtr(e.X);
                    _fcWatcher.Table.MoveSelectedEdges(trbMov, partMov, mDT0, snapTime, TRB_ACTION.FINISH);
                    _fcWatcher.Table.UnsetState(TRB_PART.START | TRB_PART.END, TRB_STATE.SELECTED);

                    refresh = true;
                    break;
                default:
                    break;
            }

            _action = MP_ACTION.NONE;
            trbMov = null;
            if (refresh) _doubleBuffer.Refresh();
        }



        class TIMERANGESTORE
        {
            public FileChangeProject Project;
            public List<TimeBox> RangeCollection = new List<TimeBox>();
            public TIMERANGESTORE(FileChangeProject proj)
            {
                Project = proj;
                // copy OF
                foreach (var trb in proj.TimeBoxCollection)
                    RangeCollection.Add(trb.Duplicate());
            }
        }

        List<TIMERANGESTORE> _trbStoreList = new List<TIMERANGESTORE>();
        private void _storeTimeBoxes()
        {
            _trbStoreList = new List<TIMERANGESTORE>();
            foreach (var proj in _fcWatcher.Table.ProjectCollection)
                _trbStoreList.Add(new TIMERANGESTORE(proj));
        }
        private void _restoreTimeBoxes()
        {
            foreach (var store in _trbStoreList)
            {
                store.Project.TimeBoxClear();
                store.Project.TimeBoxAdd(store.RangeCollection);
            }
        }
        private void _reselectTimeBoxes()
        {
            foreach (var store in _trbStoreList)
                foreach (var src in store.RangeCollection)
                {
                    TimeBox dst = store.Project.TimeBoxGet(src.UID);
                    if (dst != null) dst.CopyStates(src);
                }
        }

        bool _created = false;
        DateTime _createT0, _createT1;
        Rectangle _createRect;
        Point _createInitPoint;
        private void _createTimeBoxes(bool init)
        {
            if (init) { _created = false; _createInitPoint = box0; }

            // get box/proj intersects
            _createRect = _getBoxMaxRectangle();
            // ususally, we restrict to our initial click project row
            if (_multi == false) { _createRect.Height = 1; _createRect.Y = _createInitPoint.Y; }

            var projList = ttC.GetAllDataTouching(_createRect, CODE_PROJROW);

            _createT0 = _dateTimeAtPtr(box0.X);
            _createT1 = _dateTimeAtPtr(box1.X);

            if (_createRect.Width > boxSpace / 2) _created = true;

            // range too
            if (_created)
            {
                foreach (var item in projList)
                {
                    FileChangeProject proj = item as FileChangeProject;
                    if (proj == null) continue;
                    // for all proj, delete in range
                    var t0 = FileChangeTable.Round(_createT0, new TimeSpan(1, 0, 0, 0));
                    var t1 = FileChangeTable.Round(_createT1, new TimeSpan(1, 0, 0, 0));
                    if (_createT1 > _createT0) { if ((t1 - t0).TotalDays < 1) t1 = t0.AddDays(1); }
                    else { if ((t0 - t1).TotalDays < 1) t1 = t0.AddDays(-1); }
                    if (t1 < t0) { var t = t1; t1 = t0; t0 = t; }//swap
                    TimeBox tbFromBox = new TimeBox(t0, t1);
                    //Console.WriteLine("BOX:" + t0.ToString() + " - " + t1.ToString());
                    //Console.WriteLine("PROJ:");
                    //proj.RangeCollection.ForEach(trb => Console.WriteLine("  " + trb.StartDate.ToString() + " - " + trb.EndDate.ToString()));
                    //Console.WriteLine("REM:");
                    var remainingTRBs = TimeBox.Subtract(tbFromBox, proj.TimeBoxCollection);
                    //remainingTRBs.ForEach(trb => Console.WriteLine("  " + trb.StartDate.ToString() + " - " + trb.EndDate.ToString()));
                    remainingTRBs.ForEach(trb => trb.SetState(TRB_PART.WHOLE, TRB_STATE.SELECTED));
                    proj.TimeBoxAdd(remainingTRBs);
                }
            }

        }

        private void _selectWithBox(bool append)
        {
            Rectangle rect = _getBoxRectangle();
            var selList = ttC.GetAllDataTouching(rect, _toCode(TRB_PART.WHOLE));

            if (append == false) // desel all
                _fcWatcher.Table.WriteState(TRB_PART.WHOLE | TRB_PART.END | TRB_PART.START, TRB_STATE.NONE);

            bool tog = (rect.Width < 5 && rect.Height < 5);
            // add/remove from selection
            foreach (var obj in selList)
            {
                TimeBox trb = obj as TimeBox;
                if (trb == null) continue;
                if (tog) trb.ToggleState(TRB_PART.WHOLE, TRB_STATE.SELECTED);
                else trb.SetState(TRB_PART.WHOLE, TRB_STATE.SELECTED);
            }
        }



        private void _doubleBuffer_MouseClick(object sender, MouseEventArgs e)
        {
            if (_fcWatcher == null) return;

            // make project invisible
            FileChangeProject proj = ttC.GetDataAtPointer(e, CODE_PROJLABEL) as FileChangeProject;
            if (proj != null && ChartViewer.IsModifierHeld(Keys.Alt))
            {
                proj.Visible = false;
                _fcWatcher.FireEvent();
                return;
            }
        }

        private void DoubleBuffer1_MouseWheel(object sender, MouseEventArgs e)
        {
            int click = e.Delta / 120;

            if (ChartViewer.IsModifierHeld(Keys.Shift) && ChartViewer.IsModifierHeld(Keys.Control))
            {
                // day at mouse..
                double ptrProp = ((double)(e.X - leftDemark)) / (_doubleBuffer.Width - leftDemark - FUTCOLWIDTH);
                var ptrDate = dt0.AddDays(ptrProp * DDaySpan);
                _incDaySpan(-click);
                // re-center
                dt0 = ptrDate.AddDays(-(ptrProp * DDaySpan));
                dt1 = _calcT1(dt0);
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

    }

}

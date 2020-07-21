using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ChartLib;

namespace CodeWatcher
{
    public partial class EventScroller : UserControl
    {
        FileChangeWatcher _fcWatcher;
        DateTime _t0, _t1;
        double _timePeriodHrs = 1;
        double _timeGraticuleMinuteInc = 10;
        private bool _userTime;
        string _timeFormat = "d";
        readonly Timer _timer;
        bool _dirtyTable;
        readonly TooltipContainer _ttC;

        public EventScroller()
        {
            InitializeComponent();

            comboBoxEnumControl1.EnumType = typeof(TimePeriod);
            comboBoxEnumControl1.DefaultEnum = TimePeriod.ONEHOUR;
            comboBoxEnumControl1.SelectedItem = TimePeriod.ONEHOUR;
            comboBoxEnumControl1.SelectedIndexChanged += comboBoxEnumControl_SelectedIndexChanged;

            doubleBuffer1.MouseWheel += DoubleBuffer1_MouseWheel;
            doubleBuffer1.MouseClick += DoubleBuffer1_MouseClick;
            _timer = new Timer { Interval = 1000 };
            _timer.Tick += Timer_Tick;
            _timer.Start();

            _ttC = new TooltipContainer(doubleBuffer1);
            _dirtyTable = true;

            _t1 = DateTime.Now;
            _updateTimePeriod();

            Application.Idle += Application_Idle;
        }

        private void DoubleBuffer1_MouseClick(object sender, MouseEventArgs e)
        {
            if (_ttC.IsDataRegionAtPointer(e, CODE_PRESENT_TIME))
            {
                _userTime = false;
                _updateTimePeriod();
                doubleBuffer1.Refresh();
            }
        }

        private void DoubleBuffer1_MouseWheel(object sender, MouseEventArgs e)
        {
            int click = e.Delta / 120;

            // get one click time span
            const int oneClickScreenDy = 20;
            var span = (_posToDateTime(oneClickScreenDy) - _t0);
            span = TimeSpan.FromTicks(span.Ticks * click);
            _t1 = _t1 - span;
            if (_t1 < DateTime.Now) { _userTime = true; }
            else { _userTime = false; _t1 = DateTime.Now; }

            _t0 = _t1.AddHours(-_timePeriodHrs);

            doubleBuffer1.Refresh();
        }

        private void Application_Idle(object sender, EventArgs e)
        {
            if (_dirtyTable)
            {
                _dirtyTable = false;
                doubleBuffer1.Refresh();
            }
        }
        public void UpdateAll()
        {
            doubleBuffer1.Refresh();
        }

        public FileChangeWatcher FileChangeWatcher
        {
            get => _fcWatcher;
            set
            {
                _fcWatcher = value;
                if (_fcWatcher != null)
                {
                    _fcWatcher.Changed += _fcWatcher_Changed;
                    _fcWatcher.Error += _fcWatcher_Error;
                }
                doubleBuffer1.Invalidate();
            }
        }

        public TimePeriod TimePeriod
        {
            get => (TimePeriod)comboBoxEnumControl1.SelectedItem;
            set
            {
                comboBoxEnumControl1.SelectedItem = value;
                _dirtyTable = true;
            }
        }

        internal ThemeColors Theme
        {
            get => _theme; set
            {
                _theme = value;
                this.Invalidate(true);
            }
        }

        ThemeColors _theme = new ThemeColors();



        private void _fcWatcher_Error(object sender, System.IO.ErrorEventArgs e)
        {

            if (this.InvokeRequired)
            {
                _dirtyTable = true;
            }
            else
                this.Invalidate(true);
        }

        private void _fcWatcher_Changed(object sender, DoWorkEventArgs e)
        {
            _dirtyTable = true;
        }

        private void _updateTimePeriod()
        {
            // alter time
            switch (TimePeriod)
            {
                case TimePeriod.ONEMINUTE:
                    _timePeriodHrs = TimeConvert.Minutes2Hours(1.0); _timeGraticuleMinuteInc = TimeConvert.Sec2Minutes(10); _timeFormat = "HH:mm:ss";
                    break;
                case TimePeriod.FIVEMINUTES:
                    _timePeriodHrs = TimeConvert.Minutes2Hours(5.0); _timeGraticuleMinuteInc = TimeConvert.Sec2Minutes(30); _timeFormat = "HH:mm:ss";
                    break;
                case TimePeriod.TWOHOURS:
                    _timePeriodHrs = 2; _timeGraticuleMinuteInc = 10; _timeFormat = "HH:mm";
                    break;
                case TimePeriod.EIGHTHOURS:
                    _timePeriodHrs = 8; _timeGraticuleMinuteInc = TimeConvert.Hours2Minutes(1); _timeFormat = "HH:mm";
                    break;
                case TimePeriod.ONEDAY:
                    _timePeriodHrs = 24; _timeGraticuleMinuteInc = TimeConvert.Hours2Minutes(1); _timeFormat = "HH:mm";
                    break;
                case TimePeriod.ONEWEEK:
                    _timePeriodHrs = TimeConvert.Days2Hours(7); _timeGraticuleMinuteInc = TimeConvert.Days2Minutes(1); _timeFormat = "ddd dd MMMM";
                    break;
                case TimePeriod.ONEMONTH:
                    _timePeriodHrs = TimeConvert.Days2Hours(30); _timeGraticuleMinuteInc = TimeConvert.Days2Minutes(1); _timeFormat = "ddd dd MMMM";
                    break;
                case TimePeriod.THREEMONTHS:
                    _timePeriodHrs = TimeConvert.Days2Hours(365 * 0.25); _timeGraticuleMinuteInc = TimeConvert.Days2Minutes(7); _timeFormat = "ddd dd MMMM";
                    break;
                case TimePeriod.ONEYEAR:
                    _timePeriodHrs = TimeConvert.Days2Hours(365); _timeGraticuleMinuteInc = TimeConvert.Month2Minutes(1); _timeFormat = "MMMM yyyy";
                    break;
                default:
                    _timePeriodHrs = 1; _timeGraticuleMinuteInc = 10; _timeFormat = "HH:mm";
                    break;
            }

            _timer.Interval = (int) TimeConvert.Minutes2Ms(1);

            // shows time range as we decide
            if (_userTime == false)
                _t1 = DateTime.Now;

            _t0 = _t1.AddHours(-_timePeriodHrs);

            doubleBuffer1.Invalidate();
        }


        private void Timer_Tick(object sender, EventArgs e)
        {
            _flash = !_flash;

            if (_userTime == false)
            {
                _updateTimePeriod();
                doubleBuffer1.Invalidate();
            }

        }

        private void comboBoxEnumControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            _updateTimePeriod();
        }

        readonly int L_BDR = 10;
        readonly int B_BDR = 40;
        readonly string CODE_PRESENT_TIME = "presentTime";
        private bool _flash;

        private void doubleBuffer1_PaintEvent(object sender, PaintEventArgs e)
        {
            _ttC.Clear();

            var g = e.Graphics;

            g.Clear(_theme.Window.Background.Color);
            var table = _fcWatcher?.Table;
            if (table == null) return;


            int c = doubleBuffer1.Width / 2;
            int w = doubleBuffer1.Width - L_BDR;
            int y;

            // graticule
            // to whole minutes at increment
            TimeSpan inc = TimeSpan.FromMinutes(_timeGraticuleMinuteInc);
            var roundT0 = FileChangeTable.Floor(_t0, inc);
            var roundT1 = FileChangeTable.Floor(_t1, inc);
            foreach (DateTime dt in FileChangeTable.EachTimeSpan(roundT0, roundT1, inc))
            {
                if (dt > _t1) continue;
                y = _dateTimeToPos(dt);
                g.DrawLine(_theme.Window.Medium.Pen, 0, y, doubleBuffer1.Width, y);
                g.DrawString(dt.ToString(_timeFormat), Font, _theme.Window.Medium.Brush, L_BDR, y - Font.Height);
            }


            Rectangle futureRect = new Rectangle(0, doubleBuffer1.Height - B_BDR, doubleBuffer1.Width, B_BDR);
            g.FillRectangle(_userTime ? _theme.AccentWindow2.Medium.Brush : _theme.Window.Medium.Brush, futureRect);
            Pen pen = _userTime ? _theme.AccentWindow2.High.Pen2 : _theme.AccentWindow1.High.Pen2;
            g.DrawLine(pen, 0, futureRect.Top, doubleBuffer1.Width, futureRect.Top);
            _ttC.Add(CODE_PRESENT_TIME, futureRect, "Click to move cursor to present.", null);
            if (_userTime == false) g.DrawString("Now", Font, _flash ? pen.Brush : _theme.Window.Medium.Brush, L_BDR, futureRect.Y - Font.Height);
            // put events out in time period
            Brush br;
            foreach (var fci in table.ItemCollection)
            {
                if (!fci.IsInPeriod(_t0, _t1)) continue;
                // time: change: proj: filename 

                string txt;
                y = _dateTimeToPos(fci.DateTime);
                Brush cbr;
                Rectangle rect;
                if (fci.ChangeType.HasAny(ActionTypes.RESUME | ActionTypes.USER_IDLE | ActionTypes.SUSPEND))
                {
                    txt = fci.DateTime.ToString(_timeFormat) + ": " +
                          fci.Project.Name + ": " +
                          fci.ChangeType;
                    br = _theme.Window.Low.Brush;
                    cbr = _theme.Window.Low.ContrastBrush;
                    rect = new Rectangle(c, y, w - L_BDR, FontHeight);
                }
                else
                {
                    txt = fci.DateTime.ToString(_timeFormat) + ": " +
                          fci.Project.Name + ": " + fci.Name + ": " +
                          fci.ChangeType;
                    br = fci.Project.Brush;
                    cbr = fci.Project.ContrastBrush;
                    rect = new Rectangle(L_BDR, y, c - L_BDR, FontHeight);
                }
                if (fci.AuxInfo != null) txt = fci.AuxInfo + ": " + txt;

                g.FillRectangle(br, rect);
                g.SetClip(rect);
                g.DrawString(txt, Font, cbr, rect.X, rect.Y);
                g.ResetClip();
            }

            int dbY = 0;
            br = _theme.Window.Foreground.Brush;
            g.DrawString("Last save:" + (table.LastSave != null ? ((DateTime)table.LastSave).ToString(_timeFormat) : "-"), Font, br, 200, dbY); dbY += Font.Height;
            g.DrawString("Last #:" + table.SaveCount.ToString(), Font, br, 200, dbY); dbY += Font.Height;
            g.DrawString("Next save:" + (table.NextSave != null ? ((DateTime)table.NextSave).ToString(_timeFormat) : "-"), Font, br, 200, dbY); dbY += Font.Height;
            g.DrawString("Time remaining:" + (table.NextSave != null ? table.MsRemaining.ToString("F0") : "-"), Font, br, 200, dbY);

            g.ResetClip();
        }


        private int _dateTimeToPos(DateTime dateTime)
        {
            const int y0 = 0;
            int y1 = doubleBuffer1.Height - B_BDR;
            double dm = (_t1 - _t0).TotalMinutes;
            double mT = (dateTime - _t0).TotalMinutes;

            return ((int)Math.Round(y0 + (y1 - y0) * mT / dm, MidpointRounding.AwayFromZero));
        }

        private DateTime _posToDateTime(int y)
        {
            const int y0 = 0;
            int y1 = doubleBuffer1.Height - B_BDR;
            double dm = (_t1 - _t0).TotalMinutes;
            // get minutes after _t0
            double dy = y - y0;
            double mT = dm * dy / (y1 - y0);
            return (_t0.AddMinutes(mT));
        }



    }


}

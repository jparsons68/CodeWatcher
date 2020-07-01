using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace CodeWatcher
{
    public partial class EventScroller : UserControl
    {
        FileChangeWatcher _fcWatcher;
        DateTime _t0, _t1;
        double _timePeriodHrs = 1;
        double _timeGraticuleMinuteInc = 10;
        bool _userTime = false;
        string _timeFormat = "d";
        Timer _timer;
        bool _dirtyTable = false;

        public EventScroller()
        {
            InitializeComponent();

            comboBoxEnumControl1.EnumType = typeof(TIMEPERIOD);
            comboBoxEnumControl1.DefaultEnum = TIMEPERIOD.ONEHOUR;
            comboBoxEnumControl1.SelectedItem = TIMEPERIOD.ONEHOUR;
            comboBoxEnumControl1.SelectedIndexChanged += comboBoxEnumControl_SelectedIndexChanged;

            doubleBuffer1.MouseWheel += DoubleBuffer1_MouseWheel;

            _timer = new Timer();
            _timer.Interval = _minutes2ms(1.0);
            _timer.Tick += Timer_Tick;
            _timer.Start();

            _dirtyTable = true;

            _t1 = DateTime.Now;
            _updateTimePeriod();

            Application.Idle += Application_Idle;
        } 

         
        private void DoubleBuffer1_MouseWheel(object sender, MouseEventArgs e)
        {
            int click = e.Delta / 120;
             
            // get one click time span
            int oneClickScreenDY = 20;
            var span = (_posToDateTime(oneClickScreenDY) - _t0);
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

        public TIMEPERIOD TimePeriod
        {
            get
            {
                return (TIMEPERIOD)comboBoxEnumControl1.SelectedItem;
            }
            set
            {
                comboBoxEnumControl1.SelectedItem = value;
                _dirtyTable = true;
            }
        }

        internal ThemeColors Theme { get => _theme; set { _theme = value;
                this.Invalidate(true); } }

        ThemeColors _theme = new ThemeColors();

        

        private void _fcWatcher_Error(object sender, System.IO.ErrorEventArgs e)
        {
            this.Invalidate(true);
        }

        private void _fcWatcher_Changed(object sender, DoWorkEventArgs e)
        {
            _dirtyTable = true;
        }

        private void _updateTimePeriod()
        {
            // alter time
            int tInterval = _minutes2ms(1);
            switch (TimePeriod)
            {
                case TIMEPERIOD.ONEMINUTE:
                    _timePeriodHrs = _minutes2hours(1.0); _timeGraticuleMinuteInc = _sec2minutes(10); _timeFormat = "HH:mm:ss";
                    tInterval = 1000;
                    break;
                case TIMEPERIOD.FIVEMINUTES:
                    _timePeriodHrs = _minutes2hours(5.0); _timeGraticuleMinuteInc = _sec2minutes(30); _timeFormat = "HH:mm:ss";
                    tInterval = 1000;
                    break;
                case TIMEPERIOD.ONEHOUR:
                default:
                    _timePeriodHrs = 1; _timeGraticuleMinuteInc = 10; _timeFormat = "HH:mm";
                    break;
                case TIMEPERIOD.TWOHOURS:
                    _timePeriodHrs = 2; _timeGraticuleMinuteInc = 10; _timeFormat = "HH:mm";
                    break;
                case TIMEPERIOD.EIGHTHOURS:
                    _timePeriodHrs = 8; _timeGraticuleMinuteInc = _hours2minutes(1); _timeFormat = "HH:mm";
                    break;
                case TIMEPERIOD.ONEDAY:
                    _timePeriodHrs = 24; _timeGraticuleMinuteInc = _hours2minutes(1); _timeFormat = "HH:mm";
                    break;
                case TIMEPERIOD.ONEWEEK:
                    _timePeriodHrs = _days2hours(7); _timeGraticuleMinuteInc = _days2minutes(1); _timeFormat = "ddd dd MMMM";
                    break;
                case TIMEPERIOD.ONEMONTH:
                    _timePeriodHrs = _days2hours(30); _timeGraticuleMinuteInc = _days2minutes(1); _timeFormat = "ddd dd MMMM";
                    break;
                case TIMEPERIOD.THREEMONTHS:
                    _timePeriodHrs = _days2hours(365 * 0.25); _timeGraticuleMinuteInc = _days2minutes(7); _timeFormat = "ddd dd MMMM";
                    break;
                case TIMEPERIOD.ONEYEAR:
                    _timePeriodHrs = _days2hours(365); _timeGraticuleMinuteInc = _month2minutes(1); _timeFormat = "MMMM yyyy";
                    break;
            }
            _timer.Interval = tInterval;

            // shows time range as we decide
            if (_userTime == false)
                _t1 = DateTime.Now;

            _t0 = _t1.AddHours(-_timePeriodHrs);

            doubleBuffer1.Invalidate();
        }


        private int _minutes2ms(double minutes)
        {
            return ((int)(minutes * 60 * 1000));
        }

        private double _sec2minutes(double seconds)
        {
            return (seconds / 60.0);
        }
        private double _minutes2hours(double seconds)
        {
            return (seconds / 60.0);
        }
        private double _hours2minutes(double hours)
        {
            return (hours * 60.0);
        }
        private double _days2minutes(double days)
        {
            return (days * 24 * 60);
        }
        private double _days2hours(double days)
        {
            return (days * 24);
        }
        private double _month2minutes(double months)
        {

            return (months * (365.0 / 12.0) * 24 * 60);
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
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

        int L_BDR = 10;
        int B_BDR = 40;

        private void doubleBuffer1_PaintEvent(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;

            g.Clear(_theme.Window.Background.Color);
            if (_fcWatcher == null) return;
            if (_fcWatcher.Table == null) return;
            var _table = _fcWatcher.Table;
            if (_table == null) return;

            

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


            // put events out in time period
            foreach (var fci in _table.ItemCollection)
            {
                if (!fci.IsInPeriod(_t0, _t1)) continue;
                // time: change: proj: filename 
                string txt = fci.DateTime.ToString(_timeFormat) + ": " +
                    fci.ChangeType.ToString() + ": " + fci.Project.Name + ": " + fci.Name;

                if (fci.AuxInfo != null) txt = fci.AuxInfo + ": " + txt;

                y = _dateTimeToPos(fci.DateTime);
                Rectangle rect = new Rectangle(L_BDR, y, w, FontHeight);
                g.FillRectangle(fci.Project.Brush, rect);

                g.DrawString(txt, Font, fci.Project.ContrastBrush, rect.X, rect.Y);
            }
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            _userTime = false;
            _updateTimePeriod();
            doubleBuffer1.Refresh();
        }

        private int _dateTimeToPos(DateTime dateTime)
        {
            int y0 = 0;
            int y1 = doubleBuffer1.Height - B_BDR;
            double dm = (_t1 - _t0).TotalMinutes;
            double mT = (dateTime - _t0).TotalMinutes;

            return ((int)Math.Round(y0 + (y1 - y0) * mT / dm, MidpointRounding.AwayFromZero));
        }

        private DateTime _posToDateTime(int y)
        {
            int y0 = 0;
            int y1 = doubleBuffer1.Height - B_BDR;
            double dm = (_t1 - _t0).TotalMinutes;
            // get minutes after _t0
            double dy = y - y0;
            double mT = dm * dy / (y1 - y0);
            return (_t0.AddMinutes(mT));
        }



    }


}

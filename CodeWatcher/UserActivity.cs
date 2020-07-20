using System;
using System.Runtime.InteropServices;
using System.Timers;

namespace CodeWatcher
{
    class UserActivity
    {
        [DllImport("user32.dll", SetLastError = false)]
        private static extern bool GetLastInputInfo(ref Lastinputinfo plii);
        private static readonly DateTime SystemStartup = DateTime.Now.AddMilliseconds(-Environment.TickCount);

        Timer _timer;
        int _idleTime;
        bool _eventFired;

        public event EventHandler UserIdleEvent;

        [StructLayout(LayoutKind.Sequential)]
        private struct Lastinputinfo
        {
            public uint cbSize;
            public readonly int dwTime;
        }

        public static DateTime LastInput => SystemStartup.AddMilliseconds(LastInputTicks);
        public static TimeSpan TimeElapsedSinceLastInput => DateTime.Now.Subtract(LastInput);

        private static int LastInputTicks
        {
            get
            {
                var lii = new Lastinputinfo { cbSize = (uint)Marshal.SizeOf(typeof(Lastinputinfo)) };
                GetLastInputInfo(ref lii);
                return lii.dwTime;
            }
        }

        public int IdleTime // in milliSec
        {
            get { return (_idleTime); }
            set
            {
                _idleTime = value;
                // ReSharper disable once PossibleLossOfFraction
                _timer.Interval = _idleTime / 4; // check more often than idle period
            }
        }

        public UserActivity(int idleTime)
        {
            _timer = new Timer();
            _timer.Elapsed += Timer_Elapsed;
            IdleTime = idleTime;
            _timer.Start();
        }

        public UserActivity() : this((int)TimeConvert.Minutes2Ms(5))
        {
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (TimeElapsedSinceLastInput.TotalMilliseconds >= IdleTime)
            {
                if (_eventFired == false)
                {
                    _eventFired = true;
                    UserIdleEvent?.Invoke(this, new EventArgs());
                }
            }
            else
                _eventFired = false;

        }

    }
}
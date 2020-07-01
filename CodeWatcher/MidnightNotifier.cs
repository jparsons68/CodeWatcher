using Microsoft.Win32;
using System;
using System.Timers;

namespace CodeWatcher
{
    static class MidnightNotifier
    {
        public static event EventHandler<EventArgs> DayChanged;
        private static readonly Timer timer;

        static MidnightNotifier()
        {
            timer = new Timer(GetSleepTime());
            timer.Elapsed += (s, e) =>
            {
                OnDayChanged();
                timer.Interval = GetSleepTime();
            };
            timer.Start();

            SystemEvents.TimeChanged += OnSystemTimeChanged;
        }

        private static double GetSleepTime()
        {
            var midnightTonight = DateTime.Today.AddDays(1);
            var differenceInMilliseconds = (midnightTonight - DateTime.Now).TotalMilliseconds;
            differenceInMilliseconds += 10000; // add a bit of slop, to be sure
            return differenceInMilliseconds;
        }

        private static void OnDayChanged()
        {
            DayChanged?.Invoke(null, new EventArgs());
        }

        private static void OnSystemTimeChanged(object sender, EventArgs e)
        {
            timer.Interval = GetSleepTime();
        }

    }
}
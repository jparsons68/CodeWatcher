using System;
using System.Collections.Generic;

namespace CodeWatcher
{
    public enum Tcunit
    {
        NONE = 0,
        MILLISECONDS,
        SECONDS,
        MINUTES,
        HOURS,
        DAYS,
        WORKINGDAYS,
        WEEKS,
        MONTHS,
        YEARS
    }



    public static class TimeConvert
    {
        static List<double> _lookup;
        static TimeConvert()
        {
            _lookup = new List<double>();
            _lookup.Add(1);
            _lookup.Add(1);
            _lookup.Add(1000);
            _lookup.Add(60000);
            _lookup.Add(3600000);
            _lookup.Add(86400000); // day
            _lookup.Add(27000000); // working day (7.5 hours)
            _lookup.Add(86400000 * 7);
            _lookup.Add((365.0 / 12.0) * 86400000);
            _lookup.Add(3.1536E10);

            Test(); 
        }

        public static void Test  ()
        {
            ConsoleConvert(1, Tcunit.SECONDS, Tcunit.MILLISECONDS);
            ConsoleConvert(10, Tcunit.SECONDS, Tcunit.MILLISECONDS);
            ConsoleConvert(1, Tcunit.MILLISECONDS, Tcunit.SECONDS);
            ConsoleConvert(2000, Tcunit.MILLISECONDS, Tcunit.SECONDS);
            ConsoleConvert(2, Tcunit.MINUTES, Tcunit.SECONDS);
            ConsoleConvert(3, Tcunit.MONTHS, Tcunit.YEARS);
            ConsoleConvert(0.25, Tcunit.YEARS, Tcunit.WEEKS);
            // 4.5 hours
            ConsoleConvert(60*60*4.5, Tcunit.SECONDS, Tcunit.HOURS);
            ConsoleConvert(7, Tcunit.DAYS, Tcunit.HOURS);
            ConsoleConvert(1, Tcunit.WEEKS, Tcunit.HOURS);
            ConsoleConvert(2, Tcunit.YEARS, Tcunit.MILLISECONDS);
        }
        public static double Convert(double t, Tcunit from, Tcunit to)
        {
            var res = t * _lookup[(int) from] / _lookup[(int) to];
            return (res);
        }
        public static void ConsoleConvert(double t, Tcunit from, Tcunit to)
        {
            double res = Convert(t, from, to);
            Console.WriteLine(t + @" " + from + @" = " + res + @" " + to);
        }

        public static double Minutes2Ms(double t) { return Convert(t, Tcunit.MINUTES, Tcunit.MILLISECONDS); }
        public static double Sec2Minutes(double t) { return Convert(t, Tcunit.SECONDS, Tcunit.MINUTES); }
        public static double Minutes2Hours(double t) { return Convert(t, Tcunit.MINUTES, Tcunit.HOURS); }
        public static double Hours2Minutes(double t) { return Convert(t, Tcunit.HOURS, Tcunit.MINUTES); }
        public static double Days2Minutes(double t) { return Convert(t, Tcunit.DAYS, Tcunit.MINUTES); }
        public static double Days2Hours(double t) { return Convert(t, Tcunit.DAYS, Tcunit.HOURS); }
        public static double Month2Minutes(double t) { return Convert(t, Tcunit.MONTHS, Tcunit.MINUTES); }
    }
}

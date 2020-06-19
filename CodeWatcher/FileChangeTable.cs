using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CodeWatcher
{
    public class FileChangeTable
    {
        string _logPath;
        bool _dirtyList = false;

        public FileChangeTable(string logPath)
        {
            LogPath = logPath;
        }

        public string LogPath
        {
            get => _logPath;

            set
            {
                if (_logPath != value)
                {
                    _logPath = value;
                    _clearItemCollection();
                    _readFromFileToItemCollection(_logPath);
                    _sortAndSanitize();
                    _setTimeRange();
                    _organizeIntoProjects();
                }
            }
        }

        public int DayCount
        {
            get
            {
                var proj = ProjectCollection.FirstOrDefault();
                return (proj != null ? proj.DaysCollection.Count : 0);
            }
        }
        public int ItemCount { get { return ItemCollection.Count; } }

        public List<FileChangeItem> ItemCollection { get; set; } = new List<FileChangeItem>();
        public List<FileChangeProject> ProjectCollection { get; set; } = new List<FileChangeProject>();
        public DateTime StartTime { get; private set; }
        public DateTime EndTime { get; private set; }

        public bool Write()
        {
            return (_writeToLogFile());
        }
        public bool AutoWrite()
        {
            if (_dirtyList) return (_writeToLogFile());
            return (false);
        }
        private void _clearItemCollection()
        {
            if (ItemCollection.Count > 0)
            {
                ItemCollection.Clear();
                _dirtyList = true;
            }
        }
         
        private void _readFromFileToItemCollection(string path)
        {
            int counter = 0;
            string line;
            try
            {
                using (StreamReader file = new StreamReader(path))
                {
                    while ((line = file.ReadLine()) != null)
                    {
                        FileChangeItem fci = new FileChangeItem(line);
                        _add(fci);
                        counter++;
                    }
                }
            }
            catch
            {

            }
        }

        WatcherChangeTypes[] _priorityOrder = new WatcherChangeTypes[] {
            WatcherChangeTypes.Deleted,
            WatcherChangeTypes.Renamed,
            WatcherChangeTypes.Created,
            WatcherChangeTypes.Changed};
        private void _sortAndSanitize()
        {
            int cmp;
            ItemCollection.Sort((x, y) =>
                        ((cmp = DateTime.Compare(x.DateTime, y.DateTime)) != 0 ? cmp :
                        Array.IndexOf(_priorityOrder, x.ChangeType) - Array.IndexOf(_priorityOrder, y.ChangeType)));

            // remove later ones with same datetime
            if (ItemCollection.Count > 1)
            {
                var distinctItems = ItemCollection.Distinct(new DistinctItemComparer());
                ItemCollection = distinctItems.ToList();
                _dirtyList = true;
            }
        }

        internal int GetDayIndex(DateTime dateTime)
        {
            try
            {
                return ProjectCollection.First().GetDayIndex(dateTime);
            }
            catch
            {
                return (-1);
            }
        }

        class DistinctItemComparer : IEqualityComparer<FileChangeItem>
        {
            public bool Equals(FileChangeItem x, FileChangeItem y)
            {
                return x.Path == y.Path &&
                    x.DateTime == y.DateTime;
            }

            public int GetHashCode(FileChangeItem fci)
            {
                return fci.Path.GetHashCode() ^
                    fci.DateTime.GetHashCode();
                //fci.ChangeType.GetHashCode() ;
            }
        }





        private void _add(FileChangeItem fci)
        {
            if (fci != null && fci.ProjectPath != null)
            {
                ItemCollection.Add(fci);
                fci.Table = this;
                _dirtyList = true;
            }
        }

        internal int GetPeakOpsInDay(DateTime dt0, DateTime dt1)
        {
            return (ProjectCollection.Max(proj => proj.GetPeakOpsInDay(dt0, dt1)));
        }

        internal List<FileChangeProject> GetProjectsWithActivity(DateTime dt0, DateTime dt1)
        {
            return (ProjectCollection.Where(proj => proj.CountChanges(dt0, dt1) > 0).ToList());
        }

        private void _setTimeRange()
        {
            // actual time range
            DateTime t0 = DateTime.MaxValue;
            DateTime t1 = DateTime.MinValue;
            foreach (var fci in ItemCollection)
            {
                if (fci.DateTime < t0) t0 = fci.DateTime;
                if (fci.DateTime > t1) t1 = fci.DateTime;
            }
            StartTime = t0;
            EndTime = t1;
        }
        private void _organizeIntoProjects()
        {
            // get projects listing 
            // ragged and by day collections
            // row per project
            this.ProjectCollection.Clear();
            foreach (var fci in ItemCollection)
            {
                FileChangeProject project = _getProject(fci);
                if (project == null)
                {
                    project = new FileChangeProject(fci.ProjectPath, this);
                    ProjectCollection.Add(project);
                }
                project.Add(fci);
            }

            ProjectCollection.Sort((x, y) => string.Compare(x.Name, y.Name));

        }

        private FileChangeProject _getProject(FileChangeItem fci)
        {
            return (ProjectCollection.FirstOrDefault(project => project.Path == fci.ProjectPath));
        }

        internal bool Add(FileChangeItem fci)
        {
            if (FileChangeItem.EqualPathAndTime(fci, ItemCollection.LastOrDefault()))
                return (false);
            _add(fci);
            _sortAndSanitize();
            _setTimeRange();
            _organizeIntoProjects();
            return (true);
        }

        private bool _writeToLogFile()
        {
            if (LogPath == null) return (false);

            _dirtyList = false;
            try
            {
                using (StreamWriter file = new StreamWriter(LogPath, false))
                {
                    foreach (var fci in ItemCollection)
                        file.WriteLine(fci.ToString());
                }
                return (true);
            }
            catch
            {

            }
            return (false);
        }

        public static DateTime Round(DateTime date, TimeSpan span)
        {
            long ticks = (date.Ticks + (span.Ticks / 2) + 1) / span.Ticks;
            return new DateTime(ticks * span.Ticks, date.Kind);
        }
        public static DateTime Ceiling(DateTime date, TimeSpan span)
        {
            long ticks = (date.Ticks + span.Ticks - 1) / span.Ticks;
            return new DateTime(ticks * span.Ticks, date.Kind);
        }
        public static DateTime Floor(DateTime date, TimeSpan span)
        {
            long ticks = date.Ticks / span.Ticks;
            return new DateTime(ticks * span.Ticks, date.Kind);
        }
        public static bool Equal(DateTime dt1, DateTime dt2, TimeSpan span)
        {
            return DateTime.Equals(Floor(dt1, span), Floor(dt2, span));
        }


        public static IEnumerable<DateTime> EachDay(DateTime from, DateTime thru)
        {
            for (var day = from.Date; day.Date <= thru.Date; day = day.AddDays(1))
                yield return day;
        }

        public static IEnumerable<DateTime> EachTimeSpan(DateTime from, DateTime thru, TimeSpan inc)
        {
            for (var min = from; min <= thru; min = min + inc)
                yield return min;
        }
    }
}

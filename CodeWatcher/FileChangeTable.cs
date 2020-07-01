using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CodeWatcher
{
    public partial class FileChangeTable
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
                    ActivityTraceBuilder.Build(this);
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

        public bool AutoWrite()
        {
            if (_dirtyList) return (Write());
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
                        FileChangeItem fci = FileChangeItem.GetFileChangeItem(line);
                        _add(fci);
                        counter++;
                    }
                }
            }
            catch
            {

            }
        }

        static WatcherChangeTypes[] _priorityOrder = new WatcherChangeTypes[] {
            WatcherChangeTypes.Deleted,
            WatcherChangeTypes.Renamed,
            WatcherChangeTypes.Created,
            WatcherChangeTypes.Changed};
        internal static List<FileChangeItem> SortAndSanitize(List<FileChangeItem> collection)
        {
            int cmp;
            collection.Sort((x, y) =>
                        ((cmp = DateTime.Compare(x.DateTime, y.DateTime)) != 0 ? cmp :
                        Array.IndexOf(_priorityOrder, x.ChangeType) - Array.IndexOf(_priorityOrder, y.ChangeType)));

            // remove later ones with same datetime
            if (collection.Count > 1)
            {
                var distinctItems = collection.Distinct(new DistinctItemComparer());
                return (distinctItems.ToList());
            }
            return (collection);
        }
        private void _sortAndSanitize()
        {
            ItemCollection = SortAndSanitize(ItemCollection);
            _dirtyList = true;
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
                return fci.Path.GetHashCode() ^ fci.DateTime.GetHashCode();
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
            return ProjectCollection.Max(proj => proj.GetPeakOpsInDay(dt0, dt1));
        }
        internal int GetPeakOpsInDay()
        {
            return (GetPeakOpsInDay(StartTime, EndTime));
        }


        internal List<FileChangeProject> GetProjectsWithActivity(DateTime dt0, DateTime dt1)
        {
            return (ProjectCollection.Where(proj => proj.CountChanges(dt0, dt1) > 0).ToList());
        }

        public IEnumerable<FileChangeProject> EachVisibleProject()
        {
            foreach (var proj in ProjectCollection)
                if (proj.Visible) yield return proj;
        }
        public int VisibleProjectCount
        {
            get
            {
                return ProjectCollection.Sum(proj => proj.Visible ? 1 : 0);
            }
        }

        public int TotalMinutesSelected { get; private set; }
        public string WorkSummary { get; private set; }

        private void _setTimeRange()
        {
            // actual time range
            if (ItemCollection.Count > 0)
            {
                StartTime = ItemCollection.First().DateTime;
                EndTime = ItemCollection.Last().DateTime;
            }
            else
            {
                StartTime = DateTime.MaxValue;
                EndTime = DateTime.MinValue;
            }
        }
        private void _organizeIntoProjects()
        {
            // get projects listing 
            // ragged and by day collections
            // row per project
            List<FileChangeProject> temp = new List<FileChangeProject>(this.ProjectCollection);

            this.ProjectCollection.Clear();
            foreach (var fci in ItemCollection)
            {
                FileChangeProject project = _getProject(fci);
                if (project == null)
                {
                    // try to get matching from old list
                    project = temp.FirstOrDefault(proj => proj.Path == fci.ProjectPath);
                    if (project != null) { project.Reinitialize(); }
                    else project = new FileChangeProject(fci.ProjectPath, this);
                    ProjectCollection.Add(project);
                }
                project.Add(fci);
            }

            ProjectCollection.Sort((x, y) => string.Compare(x.Name, y.Name));

            //ClearActivityGrid();
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
            ActivityTraceBuilder.Build(this);
            return (true);
        }

        static internal bool Write(List<FileChangeItem> collection, string path)
        {
            if (path == null) return (false);

            try
            {
                using (StreamWriter file = new StreamWriter(path, false))
                {
                    foreach (var fci in collection)
                        file.WriteLine(fci.ToString());
                }
                return (true);
            }
            catch
            {

            }
            return (false);
        }

        public bool Write()
        {
            if (LogPath == null) return (false);
            _dirtyList = false;
            return (Write(ItemCollection, LogPath));
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


        public static int InclusiveDaySpan(DateTime t0, DateTime t1)
        {
            var span = (t1.Date - t0.Date);
            return ((t1.Date - t0.Date).Days + 1);
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
        static readonly Random rnd = new Random();
        public static DateTime GetRandomDate(DateTime from, DateTime to)
        {
            var range = to - from;
            var randTimeSpan = new TimeSpan((long)(rnd.NextDouble() * range.Ticks));
            return from + randTimeSpan;
        }

        internal string GetWorkSummary()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Work Summary");
            // each project with stuff, zero or otherwise
            DateTime? dt0 = GetDateTime(TRB_KIND.MINIMUM, TRB_PART.START, TRB_PART.START, TRB_STATE.ANY);
            DateTime? dt1 = GetDateTime(TRB_KIND.MAXIMUM, TRB_PART.END, TRB_PART.END, TRB_STATE.ANY);
            if (dt0 != null && dt1 != null)
            {
                string str = null;

                sb.AppendLine("");
                sb.AppendLine("PROJECTS");

                sb.AppendLine("project, first day, last day, hours, time, edits, path");
                foreach (var proj in ProjectCollection)
                {
                    if (proj.TimeBoxCollection.Count == 0) continue; // did not select it so ignore
                    str = proj.Name + ", ";

                    if (proj.ActivityTrace == null ||
                        proj.ActivityTrace.Collection.Count == 0)
                        str += "NO WORK";
                    else
                    {
                        // total time
                        // first day worked
                        // last day worked
                        str += proj.ActivityTrace.Collection.First().StartDate.Date.ToString("dd/MM/yyyy");
                        str += ", ";
                        str += proj.ActivityTrace.Collection.Last().EndDate.Date.ToString("dd/MM/yyyy");
                        str += ", ";
                        str += (proj.ActivityTrace.TotalMinutes / 60).ToString("F2");
                        str += ", ";
                        str += proj.ActivityTrace.Summary;
                        str += ", ";
                        str += proj.ActivityTrace.EditCount.ToString();
                        str += ", ";
                        str += ("\"" + proj.Path + "\"");
                        str += ", ";
                    }
                    sb.AppendLine(str);
                }

                // TOTAL
                double totalMinutes = ProjectCollection.Sum(proj => proj.ActivityTrace != null ? proj.ActivityTrace.TotalMinutes : 0.0);

                sb.AppendLine("");
                sb.AppendLine("TOTAL HOURS,  " + (totalMinutes / 60).ToString());
                sb.AppendLine("TOTAL TIME, " + ActivityTrace.FormatSummary(totalMinutes));

                // dates worked as table
                sb.AppendLine("");
                sb.AppendLine("TABLE");
                sb.AppendLine(((DateTime)dt0).ToString("dd/MM/yyyy") + " to " + ((DateTime)dt1).ToString("dd/MM/yyyy"));
                str = "";
                foreach (DateTime dt in FileChangeTable.EachDay((DateTime)dt0, (DateTime)dt1))
                    str += (dt.ToString("dd/MM/yyyy") + ", ");
                sb.AppendLine(str);

                foreach (var proj in ProjectCollection)
                {
                    if (proj.TimeBoxCollection.Count == 0) continue; // did not select it so ignore
                    str = proj.Name + ", ";

                    foreach (DateTime dt in FileChangeTable.EachDay((DateTime)dt0, (DateTime)dt1))
                    {
                        var pDay = proj.GetDay(dt);
                        str += ((pDay.Count > 0) ? "TRUE," : "FALSE,");
                    }

                    sb.AppendLine(str);
                }
            }



            return (sb.ToString());
        }
    }
}

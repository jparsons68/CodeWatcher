using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Timers;

namespace CodeWatcher
{
    public partial class FileChangeTable
    {
        string _logPath;
        private readonly TimerPlus _saveTimer;
        private readonly object _tableLock = new object();

        public DateTime? SelectionStartDate { get; private set; }
        public DateTime? SelectionEndDate { get; private set; }

        public bool SelectionState => (SelectionStartDate != null && SelectionEndDate != null);

        public bool IsInSelectedRange(DateTime dt)
        {
            return SelectionState && IsInRange(dt, SelectionStartDate, SelectionEndDate);
        }

        public static bool IsInRange(DateTime dt, DateTime? start, DateTime? end)
        {
            return (start != null && end != null && (dt >= start) && (dt < end));
        }

        public static bool IsInRange(DateTime dt, DateTime start, DateTime end)
        {
            return ((dt >= start) && (dt < end));
        }

        public void ClearTimeSelection()
        {
            SelectionStartDate = null;
            SelectionEndDate = null;
            ActivityTraceBuilder.Build(this);
        }

        public void RemoveTimeSelectionEdits()
        {
            if (SelectionStartDate == null || SelectionEndDate == null) return;
            foreach (var proj in EachVisibleProject())
            {
                proj.RemoveEdits(SelectionStartDate, SelectionEndDate);
            }
        }


        public FileChangeTable(string logPath)
        {
            _saveTimer = new TimerPlus {Interval = 10000, AutoReset = false};
            _saveTimer.Elapsed += _saveTimer_Elapsed;
            _saveTimer.Enabled = false;

            LogPath = logPath;
        }


        public string LogPath
        {
            get => _logPath;

            set
            {
                if (_logPath != value)
                {
                    lock (_tableLock)
                    {
                        _logPath = value;
                        _readFromFileToItemCollection(_logPath);
                        _sortAndSanitize();
                        _setTimeExtents();
                        _organizeIntoProjects();
                        _applySavedProjectSettings();
                        ActivityTraceBuilder.Build(this);
                    }
                }
            }
        }


        public int ItemCount => ItemCollection.Count;

        public List<ActivityItem> ItemCollection { get; set; } = new List<ActivityItem>();
        public List<FileChangeProject> ProjectCollection { get; set; } = new List<FileChangeProject>();
        public DateTime StartTime { get; private set; }
        public DateTime EndTime { get; private set; }


        internal void SetTimeSelection(DateTime? dt0, DateTime? dt1, bool recalc)
        {
            if (dt0 != null && dt1 != null)
            {
                var daySpan = new TimeSpan(1, 0, 0, 0);
                if (dt1 > dt0)
                {
                    SelectionStartDate = FileChangeTable.Round(dt0, daySpan);
                    SelectionEndDate = FileChangeTable.Round(dt1, daySpan);
                }
                else
                {
                    SelectionStartDate = FileChangeTable.Round(dt1, daySpan);
                    SelectionEndDate = FileChangeTable.Round(dt0, daySpan);
                }
            }
            else
            {
                SelectionStartDate = null;
                SelectionEndDate = null;
            }

            if (recalc) ActivityTraceBuilder.Build(this);
        }

        SortBy _sortProjectsBy = SortBy.ALPHABETICAL;
        public SortBy SortProjectsBy
        {
            get => (_sortProjectsBy);
            set
            {
                if (_sortProjectsBy != value)
                {
                    _sortProjectsBy = value;
                    _sortProjects();
                }
            }
        }

        public bool AutoWrite()
        {
            if (_saveTimer.Enabled)
            {
                _saveTimer.Stop();
                _saveTimer_Elapsed(null, null);
            }
            return (false);
        }

        public DateTime? NextSave => (_saveTimer.DueTime);
        public DateTime? LastSave { get; private set; }
        public uint SaveCount { get; private set; }
        public double MsRemaining => _saveTimer.MsRemaining;

        void _setDirty(bool b = true)
        {
            if (b) _saveTimer.Start();
            else _saveTimer.Stop();
        }

        private void _saveTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Write(5);
        }

        private static readonly ActionTypes[] _priorityOrder = new[] {
            ActionTypes.DELETED,
            ActionTypes.RENAMED,
            ActionTypes.CREATED,
            ActionTypes.CHANGED,
            ActionTypes.USER_IDLE,
        ActionTypes.RESUME,
        ActionTypes.SUSPEND,
        ActionTypes.NONE
        };
        internal static List<ActivityItem> SortAndSanitize(List<ActivityItem> collection)
        {
            int cmp;

            collection.RemoveAll(fci => !fci.IsPermittedPath);
            collection.RemoveAll(fci =>
            {
                string ext = Path.GetExtension(fci.Path);
                return (ext != ".cs");
            });


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
            _setDirty();
        }



        class DistinctItemComparer : IEqualityComparer<ActivityItem>
        {
            public bool Equals(ActivityItem x, ActivityItem y)
            {
                return
                    y != null && (x != null && ((x.Path ?? "") == (y.Path ?? "") &&
                                                x.DateTime == y.DateTime));
            }

            public int GetHashCode(ActivityItem fci)
            {
                string path = fci.Path == null ? "" : fci.Path;
                return path.GetHashCode() ^ fci.DateTime.GetHashCode();
            }
        }


        private void _add(ActivityItem fci)
        {
            if (fci != null && fci.IsValid)
            {
                ItemCollection.Add(fci);
                fci.Table = this;
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

        public IEnumerable<FileChangeProject> EachVisibleProject()
        {
            foreach (var proj in ProjectCollection)
                if (proj.Visible) yield return proj;
        }



        public int CountProjects(DataState viz, DataState sel)
        {
            int count = this.ProjectCollection.Sum(p =>
            {
                bool vS = _getDataState(viz, p.Visible);
                bool sS = _getDataState(sel, p.Selected);
                return (vS && sS ? 1 : 0);
            });
            return (count);
        }

        private bool _getDataState(DataState dataState, bool state)
        {
            switch (dataState)
            {
                case DataState.False:
                    return (!state);
                case DataState.True:
                    return (state);
                case DataState.TrueOrFalse:
                case DataState.Ignore:
                    return (true);
                default:
                    return (false);
            }
        }


        public FileChangeActivity Activity { get; } = new FileChangeActivity();

        private void _setTimeExtents()
        {
            // actual time extents
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
                    if (project != null)
                    {
                        project.Reinitialize();
                    }
                    else project = new FileChangeProject(fci.ProjectPath, this);

                    ProjectCollection.Add(project);
                }

                if (fci.DateTime.Date == DateTime.Today)
                {

                }
                project.Add(fci);
            }

            _sortProjects();
        }

        private void _sortProjects()
        {

            switch (SortProjectsBy)
            {
                case SortBy.MOST_RECENT_FIRST:
                    _sortTime(-1);
                    break;
                case SortBy.EARLIEST_FIRST:
                    _sortTime(1);
                    break;
                case SortBy.ALPHABETICAL:
                    _sortAlpha(1);
                    break;
                case SortBy.REVERSE_ALPHABETICAL:
                    _sortAlpha(-1);
                    break;
            }
        }


        private void _sortAlpha(int dir)
        {
            ProjectCollection.Sort((x, y) => dir * string.CompareOrdinal(x.Name, y.Name));
        }

        private void _sortTime(int dir)
        {
            ProjectCollection.Sort((x, y) =>
            {
                var lastX = x.Collection.LastOrDefault();
                var lastY = y.Collection.LastOrDefault();
                int cmp;
                if (lastX == null && lastY == null) cmp = -string.CompareOrdinal(x.Name, y.Name);
                else if (lastX == null) cmp = -1;
                else if (lastY == null) cmp = 1;
                else cmp = DateTime.Compare(lastX.DateTime, lastY.DateTime);
                return (dir * cmp);
            });
        }

        private FileChangeProject _getProject(ActivityItem fci)
        {
            return (ProjectCollection.FirstOrDefault(project => project.Path == fci.ProjectPath));
        }

        internal bool Add(ActivityItem fci)
        {
            lock (_tableLock)
            {
                bool state = false;
                if (!ActivityItem.EqualPathAndTime(fci, ItemCollection.LastOrDefault()))
                {
                    _add(fci);
                    _sortAndSanitize();
                    _setTimeExtents();
                    _organizeIntoProjects();
                    ActivityTraceBuilder.Build(this);
                    state = true;
                }

                return (state);
            }
        }

        internal bool AddAcrossProjects(ActivityItem fci)
        {
            lock (_tableLock)
            {
                int n = 0;
                foreach (var proj in ProjectCollection)
                {
                    var last = proj.Collection.LastOrDefault();
                    if (last != null && // go something to idle from
                        (!last.ChangeType.HasAny(ActionTypes.SUSPEND | ActionTypes.USER_IDLE)) &&
                        (DateTime.Now - last.DateTime).TotalMinutes <= ActivityTraceBuilder.PerEditMinutes
                    ) // within normal idle period
                    {
                        ActivityItem projFci = new ActivityItem(fci, proj.Path);
                        _add(projFci);
                        n++;
                    }
                }

                if (n > 0)
                {
                    _sortAndSanitize();
                    _setTimeExtents();
                    _organizeIntoProjects();
                    ActivityTraceBuilder.Build(this);
                }
                return (n > 0);
            }
        }


        static readonly string _PROJECT_HEADER = "PROJECTS";
        static readonly string _PROJECT_ENDER = "PROJECTS END";
        static readonly string _SORTHEAD = "SORT:";
        static readonly string _SELECTSTART = "START:";
        static readonly string _SELECTEND = "END:";
        internal static bool Write(List<FileChangeProject> projectCollection,
                                   List<ActivityItem> itemCollection,
                                   SortBy sortProjectsBy,
                                   DateTime? dt0,
                                   DateTime? dt1,
                                   string path)
        {
            if (path == null) return (false);

            try
            {
                using (StreamWriter file = new StreamWriter(path, false))
                {
                    file.WriteLine(_PROJECT_HEADER);
                    file.WriteLine(_SORTHEAD + sortProjectsBy);
                    file.WriteLine(_SELECTSTART + dt0);
                    file.WriteLine(_SELECTEND + dt1);
                    file.WriteLine("");

                    if (projectCollection != null)
                        foreach (var proj in projectCollection)
                            file.WriteLine(proj.GetSetting());
                    file.WriteLine(_PROJECT_ENDER);

                    if (itemCollection != null)
                        foreach (var fci in itemCollection)
                            file.WriteLine(fci.ToString());
                }
                return (true);
            }
            catch (Exception)
            {
                // ignored
            }

            return (false);
        }

        public bool Write(int nAttempts = 1, int msWait = 500)
        {
            lock (_tableLock)
            {
                if (LogPath == null) return (false);

                _setDirty(false);

                int count = 0;
                bool written = false;
                while (count < nAttempts && written == false)
                {
                    count++;
                    if (count > 1) Console.WriteLine(@"Attempt to save #" + count);
                    string tmp = Path.GetTempFileName();

                    written = Write(this.ProjectCollection, this.ItemCollection, this.SortProjectsBy, this.SelectionStartDate, this.SelectionEndDate, tmp);

                    if (written)
                    {
                        try
                        {
                            // compare sizes 
                            FileInfo fiTmp = new FileInfo(tmp);
                            FileInfo fiDst = new FileInfo(LogPath);

                            if (fiTmp.Length < fiDst.Length)
                                File.Copy(tmp, LogPath, true);
                            else
                                File.Copy(tmp, LogPath, true);

                            File.Delete(tmp);
                            LastSave = DateTime.Now;
                            SaveCount++;
                        }
                        catch
                        {
                            written = false;
                        }
                    }

                    if (!written)
                        System.Threading.Thread.Sleep(msWait);
                }

                return (written);
            }
        }


        List<string> _savedProjectSettings;

        private void _applySavedProjectSettings()
        {
            if (_savedProjectSettings == null) return;

            foreach (var proj in ProjectCollection)
                proj.ExtractAndApplySetting(_savedProjectSettings);

            _savedProjectSettings = null;
        }

        private void _readFromFileToItemCollection(string path)
        {
            try
            {
                ItemCollection.Clear();
                SelectionStartDate = null;
                SelectionEndDate = null;

                _savedProjectSettings = null;
                DateTime? readDt0 = null;
                DateTime? readDt1 = null;

                using (StreamReader file = new StreamReader(path))
                {
                    bool projectRead = false;
                    string line;
                    while ((line = file.ReadLine()) != null)
                    {
                        if (line == _PROJECT_HEADER)
                        {
                            projectRead = true;
                            continue;
                        }
                        if (line == _PROJECT_ENDER)
                        {
                            projectRead = false;
                            continue;
                        }

                        if (projectRead)
                        {
                            var extr = Extract(line, _SORTHEAD);
                            if (extr != null && Enum.TryParse(extr, true, out SortBy srt))
                            {
                                this.SortProjectsBy = srt;
                                continue;
                            }
                            extr = Extract(line, _SELECTSTART);
                            if (extr != null && DateTime.TryParse(extr, out var tmpDt0))
                            {
                                readDt0 = tmpDt0;
                                continue;
                            }

                            extr = Extract(line, _SELECTEND);
                            if (extr != null && DateTime.TryParse(extr, out var tmpDt1))
                            {
                                readDt1 = tmpDt1;
                                continue;
                            }

                            if (_savedProjectSettings == null)
                                _savedProjectSettings = new List<string>();
                            _savedProjectSettings.Add(line); // deal with these after project organize
                        }
                        else
                        {
                            ActivityItem fci = ActivityItem.GetFileChangeItem(line);
                            _add(fci);
                        }
                    }
                }

                SetTimeSelection(readDt0, readDt1, false);
            }
            catch (Exception)
            {
                // ignored
            }
        }


        public static string Extract(string src, string key)
        {
            return (src != null && src.StartsWith(key) ? src.Substring(key.Length) : null);
        }

        public static DateTime Round(DateTime date, TimeSpan span)
        {
            long ticks = (date.Ticks + (span.Ticks / 2) + 1) / span.Ticks;
            return new DateTime(ticks * span.Ticks, date.Kind);
        }

        public static DateTime Round(DateTime? date, TimeSpan span)
        {
            if (date != null) return (Round((DateTime)date, span));
            return default;
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

        private static readonly Random Rnd = new Random();
        public static DateTime GetRandomDate(DateTime from, DateTime to)
        {
            var range = to - from;
            var randTimeSpan = new TimeSpan((long)(Rnd.NextDouble() * range.Ticks));
            return from + randTimeSpan;
        }

        internal string GetWorkSummary()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Work Summary");
            // each project with stuff, zero or otherwise

            if (SelectionStartDate != null && SelectionEndDate != null)
            {
                DateTime dt0 = ((DateTime)SelectionStartDate).Date;
                DateTime dt1 = ((DateTime)SelectionEndDate).Date;
                dt1 = dt1.AddDays(-1);
                sb.AppendLine("");
                sb.AppendLine("PROJECTS WORKED");
                foreach (var proj in EachVisibleProject())
                {
                    sb.AppendLine(proj.Name + ", " + proj.Path);
                }

                sb.AppendLine();
                sb.AppendLine("first day:" + this.Activity.Collection.First().StartDate.ToString("dd/MM/yyyy"));
                sb.AppendLine("last day:" + this.Activity.Collection.Last().EndDate.ToString("dd/MM/yyyy"));
                sb.AppendLine("hours:" + (TimeConvert.Minutes2Hours(Activity.TotalMinutes)).ToString("F2"));
                sb.AppendLine("time:" + Activity.Summary);
                sb.AppendLine("edits:" + Activity.EditCount);
                sb.AppendLine("path:");

                // dates worked as table
                sb.AppendLine("");
                sb.AppendLine("TABLE");
                sb.AppendLine(dt0.ToString("dd/MM/yyyy") + " to " + dt1.ToString("dd/MM/yyyy"));
                var str = "";
                foreach (DateTime dt in FileChangeTable.EachDay(dt0, dt1))
                    str += (dt.ToString("dd/MM/yyyy") + ", ");
                sb.AppendLine(str);

                foreach (var proj in EachVisibleProject())
                {
                    str = proj.Name + ", ";

                    foreach (DateTime dt in FileChangeTable.EachDay(dt0, dt1))
                    {
                        var pDay = proj.GetDay(dt);
                        str += ((pDay != null && pDay.Count > 0) ? "TRUE," : "FALSE,");
                    }

                    sb.AppendLine(str);
                }
            }



            return (sb.ToString());
        }

        public void UpdateActivity()
        {
            ActivityTraceBuilder.Build(this);
        }

    }

    public class FileChangeActivity
    {
        public List<ActivityBaseBlock> Collection { get; set; }
        public double TotalMinutes { get; set; }
        public int EditCount { get; set; }
        public string Summary => (ActivityTraceBuilder.FormatSummary(TotalMinutes));

        public void Clear()
        {
            Collection = null;
            EditCount = 0;
            TotalMinutes = 0;
        }
    }
}

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
        //bool _dirtyList;
        private readonly TimerPlus _saveTimer;
        private readonly object tableLock = new object();

        public bool SelectionState { get; set; }
        public DateTime SelectionStartDate { get; set; }
        public DateTime SelectionEndDate { get; set; }

        public bool IsInSelectedRange(DateTime dt)
        {
            return (SelectionState && (dt >= SelectionStartDate) && (dt < SelectionEndDate));
        }


        public FileChangeTable(string logPath)
        {
            _saveTimer = new TimerPlus();
            _saveTimer.Interval = 10000;
            _saveTimer.AutoReset = false;
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
                    lock (tableLock)
                    {
                        _logPath = value;
                        _readFromFileToItemCollection(_logPath);
                        _sortAndSanitize();
                        _setTimeExtents();
                        _organizeIntoProjects();
                        _applySavedProjectSettings();
                        ActivityTraceBuilder.Buildv2(this);
                    }
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

        public List<ActivityItem> ItemCollection { get; set; } = new List<ActivityItem>();
        public List<FileChangeProject> ProjectCollection { get; set; } = new List<FileChangeProject>();
        public DateTime StartTime { get; private set; }
        public DateTime EndTime { get; private set; }


        internal void SetTimeSelection(DateTime dt0, DateTime dt1)
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





        SortBy _sortProjectsBy = SortBy.Alphabetical;
        public SortBy SortProjectsBy
        {
            get { return (_sortProjectsBy); }
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
            //if (_dirtyList) return (Write(4));
            return (false);
        }

        public DateTime? NextSave { get { return (_saveTimer.DueTime); } }
        public DateTime? LastSave { get; private set; }
        public uint SaveCount { get; private set; }
        public double MsRemaining { get { return _saveTimer.MsRemaining; } }

        void _setDirty(bool b = true)
        {
            //_dirtyList = true;
            if (b) _saveTimer.Start();
            else _saveTimer.Stop();
        }

        private void _saveTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            bool state = Write(5);
            //Console.WriteLine(@"SAVE: " + state);
        }

        static ActionTypes[] _priorityOrder = new ActionTypes[] {
            ActionTypes.Deleted,
            ActionTypes.Renamed,
            ActionTypes.Created,
            ActionTypes.Changed,
            ActionTypes.UserIdle,
        ActionTypes.Resume,
        ActionTypes.Suspend,
        ActionTypes.None
        };
        internal static List<ActivityItem> SortAndSanitize(List<ActivityItem> collection)
        {
            int cmp;

            collection.RemoveAll(fci => !fci.IsPermittedPath);

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

        class DistinctItemComparer : IEqualityComparer<ActivityItem>
        {
            public bool Equals(ActivityItem x, ActivityItem y)
            {
                return
                    y != null && (x != null && ((x.Path == null ? "" : x.Path) == (y.Path == null ? "" : y.Path) &&
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
                //_dirtyList = true;
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


        //public string ActivitySummary { get { return (ActivityTrace.FormatSummary(TotalMinutesSelected)); } }
        // public double TotalMinutesSelected { get; internal set; }

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
                case SortBy.MostRecentFirst:
                    _sortTime(-1);
                    break;
                case SortBy.EarliestFirst:
                    _sortTime(1);
                    break;
                case SortBy.Alphabetical:
                    _sortAlpha(1);
                    break;
                case SortBy.ReverseAlphabetical:
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
            lock (tableLock)
            {
                uint addId = _getAddID();
                //Console.WriteLine(@"Add " + addId);
                bool state = false;
                if (!ActivityItem.EqualPathAndTime(fci, ItemCollection.LastOrDefault()))
                {
                    _add(fci);
                    _sortAndSanitize();
                    _setTimeExtents();
                    _organizeIntoProjects();
                    ActivityTraceBuilder.Buildv2(this);
                    state = true;
                }

                //Console.WriteLine(@"Add END " + addId);
                return (state);
            }
        }

        private uint _addId;
        private uint _getAddID()
        {
            _addId++;
            return (_addId);
        }

        internal bool AddAcrossProjects(ActivityItem fci)
        {
            lock (tableLock)
            {
                uint addId = _getAddID();
                //Console.WriteLine(@"Add Across " + addId);
                int n = 0;
                foreach (var proj in ProjectCollection)
                {
                    var last = proj.Collection.LastOrDefault();
                    if (last != null && // go something to idle from
                        (!last.ChangeType.HasAny(ActionTypes.Suspend | ActionTypes.UserIdle)) &&
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
                    ActivityTraceBuilder.Buildv2(this);
                }

                //Console.WriteLine(@"Add Across END " + addId + @",   " + (n > 0 ? (n + " idles") : "did nothing"));
                return (n > 0);
            }
        }






        static string _PROJECT_HEADER = "PROJECTS";
        static string _PROJECT_ENDER = "PROJECTS END";
        static string _SORTHEAD = "SORT:";
        static internal bool Write(List<FileChangeProject> projectCollection,
                                   List<ActivityItem> itemCollection,
                                   SortBy sortProjectsBy,
                                   string path)
        {
            if (path == null) return (false);

            try
            {
                using (StreamWriter file = new StreamWriter(path, false))
                {
                    file.WriteLine(_PROJECT_HEADER);
                    file.WriteLine(_SORTHEAD + sortProjectsBy);
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
            lock (tableLock)
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

                    written = Write(this.ProjectCollection, this.ItemCollection, this.SortProjectsBy, tmp);

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
                _savedProjectSettings = null;

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
                            if (line.StartsWith(_SORTHEAD))
                            {
                                if (Enum.TryParse(line.Substring(_SORTHEAD.Length), out SortBy srt))
                                    this.SortProjectsBy = srt;
                            }
                            else
                            {
                                if (_savedProjectSettings == null)
                                    _savedProjectSettings = new List<string>();
                                _savedProjectSettings.Add(line);// deal with these after project organize
                            }
                        }
                        else
                        {
                            ActivityItem fci = ActivityItem.GetFileChangeItem(line);
                            _add(fci);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // ignored
            }
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
            DateTime? dt0 = GetDateTime(TRB_KIND.MINIMUM, TRB_PART.START, TRB_PART.START, TRB_STATE.ANY);
            DateTime? dt1 = GetDateTime(TRB_KIND.MAXIMUM, TRB_PART.END, TRB_PART.END, TRB_STATE.ANY);
            if (dt0 != null && dt1 != null)
            {
                dt1 = ((DateTime)dt1).AddDays(-1);
                sb.AppendLine("");
                sb.AppendLine("PROJECTS WORKED");
                foreach (var proj in EachVisibleProject())
                {
                    if (proj.TimeBoxCollection.Count == 0) continue; // did not select it so ignore
                    sb.AppendLine(proj.Name + ", " + proj.Path);
                }

                sb.AppendLine();
                sb.AppendLine("first day:" + this.Activity.Collection.First().StartDate.ToString("dd/MM/yyyy"));
                sb.AppendLine("last day:" + this.Activity.Collection.Last().EndDate.ToString("dd/MM/yyyy"));
                sb.AppendLine("hours:" + (TimeConvert.Minutes2Hours(Activity.TotalMinutes)).ToString("F2"));
                sb.AppendLine("time:" + Activity.Summary);
                sb.AppendLine("edits:" + Activity.EditCount);
                sb.AppendLine("path:");

                // TOTAL
                //sb.AppendLine("");
                //sb.AppendLine("TOTAL HOURS,  " + (TotalMinutesSelected / 60).ToString(CultureInfo.InvariantCulture));
                //sb.AppendLine("TOTAL TIME, " + ActivityTrace.FormatSummary(TotalMinutesSelected));

                // dates worked as table
                sb.AppendLine("");
                sb.AppendLine("TABLE");
                sb.AppendLine(((DateTime)dt0).ToString("dd/MM/yyyy") + " to " + ((DateTime)dt1).ToString("dd/MM/yyyy"));
                var str = "";
                foreach (DateTime dt in FileChangeTable.EachDay((DateTime)dt0, (DateTime)dt1))
                    str += (dt.ToString("dd/MM/yyyy") + ", ");
                sb.AppendLine(str);

                foreach (var proj in EachVisibleProject())
                {
                    if (proj.TimeBoxCollection.Count == 0) continue; // did not select it so ignore
                    str = proj.Name + ", ";

                    foreach (DateTime dt in FileChangeTable.EachDay((DateTime)dt0, (DateTime)dt1))
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
            ActivityTraceBuilder.Buildv2(this);
        }


    
    }

    public class FileChangeActivity
    {
        public List<ActivityBaseBlock> Collection { get; set; }
        public double TotalMinutes { get; set; }
        public int EditCount { get; set; }
        public string Summary { get { return (ActivityTraceBuilder.FormatSummary(TotalMinutes)); } }

        public void Clear()
        {
            Collection = null;
            EditCount = 0;
            TotalMinutes = 0;
        }
    }
}

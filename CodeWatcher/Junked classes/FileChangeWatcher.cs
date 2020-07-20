using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Timers;

namespace CodeWatcher
{
    public class FileChangeWatcher : IDisposable
    {
        FileSystemWatcher _watcher;
        UserActivity _userAct;
        string _watchPath;
        string _logPath;
        public double _userIdleMinutes = 5;
        public event EventHandler<DoWorkEventArgs> Changed;
        public event ErrorEventHandler Error;
        public event EventHandler SortProjectsByChanged;
        public double UserIdleMinutes // minutes
        {
            get { return (_userIdleMinutes); }
            set
            {
                _userIdleMinutes = value;
                if (_userAct != null)
                    _userAct.IdleTime = (int)TimeConvert.Minutes2Ms(_userIdleMinutes);
            }
        }


        public FileChangeTable Table { get; private set; }

        void _initiateWatcher()
        {
            Table = new FileChangeTable(LogPath);

            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Dispose();
            }

            _watcher = null;

            if (WatchPath == null || Directory.Exists(WatchPath) == false) return;

            _watcher = new FileSystemWatcher(WatchPath)
            {
                NotifyFilter = NotifyFilters.LastAccess |
                               NotifyFilters.LastWrite |
                               NotifyFilters.FileName |
                               NotifyFilters.DirectoryName |
                               NotifyFilters.Size |
                               NotifyFilters.CreationTime |
                               NotifyFilters.Attributes |
                               NotifyFilters.Security,
                Filter = "*.*",
                IncludeSubdirectories = true
            };

            _watcher.Changed += Watcher_Changed;
            _watcher.Created += Watcher_Created;
            _watcher.Deleted += Watcher_Deleted;
            _watcher.Renamed += Watcher_Renamed;

            _userAct = new UserActivity();
            _userAct.UserIdleEvent += UserAct_UserIdleEvent;
            UserIdleMinutes = 5;
//#if DEBUG == true
 //           UserIdleMinutes = 0.2;
//#endif

            SystemEvents.PowerModeChanged += OnPowerChange;
        }


        public string[] Extensions { get; set; }

        public string LogPath
        {
            get => _logPath;
            set
            {
                if (_logPath != value)
                {
                    _logPath = value;
                    _initiateWatcher();
                }
            }
        }

        public string WatchPath
        {
            get => _watchPath;
            set
            {
                if (_watchPath != value)
                {
                    _watchPath = value;
                    _initiateWatcher();
                }
            }
        }

        public bool IsWatching => (_watcher != null && _watcher.EnableRaisingEvents);

        public SortBy SortProjectsBy
        {
            get { return (Table.SortProjectsBy); }
            set
            {
                Table.SortProjectsBy = value;
                SortProjectsByChanged?.Invoke(this, new EventArgs());
            }
        }

        public void Start()
        {
            if (_watcher != null)
                _watcher.EnableRaisingEvents = true;
        }

        public void Stop()
        {
            if (_watcher != null)
                _watcher.EnableRaisingEvents = false;
        }


        public void FireEvent(ActivityItem fci)
        {
            try
            {
                // ignore logfile changing!
                if (File.Exists(LogPath) && File.Exists(fci.Path) && PathUtility.IsSameFile(fci.Path, LogPath)) return;
                // other ignores
                if (!fci.IsPermittedPath) return;

                if (Table.Add(fci))
                    Changed?.Invoke(this, new DoWorkEventArgs(fci));
            }
            catch (Exception ex)
            {
                Error?.Invoke(this, new ErrorEventArgs(ex));
            }
        }

        public void FireEventAcrossProjects(ActivityItem fci)
        {
            try
            {
                if (Table.AddAcrossProjects(fci))
                    Changed?.Invoke(this, new DoWorkEventArgs(fci));
            }
            catch (Exception ex)
            {
                Error?.Invoke(this, new ErrorEventArgs(ex));
            }
        }

        public void FireEvent()
        {
            try
            {
                Changed?.Invoke(this, new DoWorkEventArgs(null));
            }
            catch (Exception ex)
            {
                Error?.Invoke(this, new ErrorEventArgs(ex));
            }
        }

        bool isProperChange(FileSystemEventArgs e)
        {
            if (e.ChangeType.HasAny(WatcherChangeTypes.Changed | WatcherChangeTypes.Renamed))
            {
                if (!File.Exists(e.FullPath))
                    return (false);
                var lastWrite = File.GetLastWriteTime(e.FullPath);
                if ((DateTime.Now - lastWrite).TotalMinutes > 1.0)
                    return (false);
            }
            return (true);
        }

        private void OnChanged(FileSystemEventArgs e)
        {
            string ext = Path.GetExtension(e.FullPath);
            if (Extensions.Contains(ext) && isProperChange(e))
            {
                ActivityItem fci = new ActivityItem(e);
                FireEvent(fci);
            }
        }

        // from user idling
        private void OnChanged()
        {
            // fire this event to all the relevant projects
            ActivityItem fci = new ActivityItem();
            FireEventAcrossProjects(fci);
        }

        // from power up/down
        private void OnChanged(PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.StatusChange) return;
            ActivityItem fci = new ActivityItem(e);
            FireEventAcrossProjects(fci);
        }


        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Renamed) return;
            if (e.ChangeType == WatcherChangeTypes.Deleted) return;
            if (e.ChangeType == WatcherChangeTypes.Created) return;
            OnChanged(e);
        }

        private void Watcher_Renamed(object sender, RenamedEventArgs e)
        {
            OnChanged(e);
        }

        private void Watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            OnChanged(e);
        }

        private void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            OnChanged(e);
        }


        private void UserAct_UserIdleEvent(object sender, EventArgs e)
        {
            OnChanged();
        }

        private void OnPowerChange(object s, PowerModeChangedEventArgs e)
        {
            OnChanged(e);
        }


        public void Dispose(bool disposing)
        {
            if (disposing)
            {
                _watcher?.Dispose();
            }
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        internal void ClearAll()
        {
            try
            {
                File.Copy(LogPath, LogPath + "_bak");
                File.Delete(LogPath);
            }
            catch (Exception)
            {
                // ignored
            }

            var sort = SortProjectsBy;
            Table = new FileChangeTable(LogPath);
            Table.SortProjectsBy = sort;// persist this setting
            Changed?.Invoke(this, new DoWorkEventArgs(null));
        }

        Timer _testTimer;
        FileChangeTester _tester;
        readonly Random _randTest = new Random();
        int _maxMs;
        internal void StartBackgroundTest(int maximumMsInterval)
        {
            if (_tester != null) return; // already running
            _tester = new FileChangeTester(WatchPath);

            if (_testTimer == null)
            {
                _testTimer = new Timer(100) { Enabled = false };
                _testTimer.Elapsed += Timer_Elapsed;
            }
            _maxMs = maximumMsInterval;

            _testTimer.Interval = 100; // first one quickly
            _testTimer.Start();
        }

        internal void StopBackgroundTest()
        {
            _testTimer?.Stop();
            _tester = null;
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _testTimer.Interval = _randTest.Next(100, _maxMs);
            var fci = _tester.GetFileChangeTestItem(DateTime.Now);
            if (fci == null) return;
            fci.AuxInfo = "TESTING";
            try
            {
                _watcher.EnableRaisingEvents = false;
                FireEvent(fci);
            }
            finally
            {
                _watcher.EnableRaisingEvents = true;
            }
        }

        internal void AutoWrite()
        {
            Table?.AutoWrite();
        }

        internal void Write()
        {
            Table?.Write();
        }

        public double PerEditMinutes
        {
            get { return (ActivityTraceBuilder.PerEditMinutes); }
            set { ActivityTraceBuilder.PerEditMinutes = value; }
        }

        public bool UseIdleEvents
        {
            get { return (ActivityTraceBuilder.UseIdleEvents); }
            set { ActivityTraceBuilder.UseIdleEvents = value; }
        }

        public void UpdateActivity()
        {
            Table.UpdateActivity();
        }
    }
}

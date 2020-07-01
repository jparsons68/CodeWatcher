using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Timers;

namespace CodeWatcher
{
    public class FileChangeWatcher : IDisposable
    {
        Timer _timer;
        FileSystemWatcher _watcher;
        string _watchPath;
        string _logPath;
        public event EventHandler<DoWorkEventArgs> Changed;
        public event ErrorEventHandler Error;
        int _saveInterval = 3;

        public FileChangeTable Table { get; private set; }

        public FileChangeWatcher()
        {

        }

        void _initiateWatcher()
        {
            Table = new FileChangeTable(LogPath);

            if (_watcher != null) { _watcher.EnableRaisingEvents = false; _watcher.Dispose(); }
            _watcher = null;

            if (WatchPath == null || Directory.Exists(WatchPath) == false) return;

            _watcher = new FileSystemWatcher(WatchPath);
            _watcher.NotifyFilter =
                    NotifyFilters.LastAccess |
                    NotifyFilters.LastWrite |
                    NotifyFilters.FileName |
                    NotifyFilters.DirectoryName |
                    NotifyFilters.Size |
                    NotifyFilters.CreationTime |
                    NotifyFilters.Attributes |
                    NotifyFilters.Security;
            _watcher.Filter = "*.*";
            _watcher.IncludeSubdirectories = true;

            _watcher.Changed += Watcher_Changed;
            _watcher.Created += Watcher_Created;
            _watcher.Deleted += Watcher_Deleted;
            _watcher.Renamed += Watcher_Renamed;

            _timer = new Timer();
            _timer.Elapsed += _timer_Elapsed;
            _timer.Interval = _saveInterval * 60 * 1000;
            _timer.Start();
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (Table != null)
                Table.AutoWrite();
        }

        public string[] Extensions { get; set; }
        public string LogPath { get => _logPath; set { if (_logPath != value) { _logPath = value; _initiateWatcher(); } } }
        public string WatchPath { get => _watchPath; set { if (_watchPath != value) { _watchPath = value; _initiateWatcher(); } } }

        public bool IsWatching
        {
            get { return (_watcher != null && _watcher.EnableRaisingEvents); }
        }

        public int AutoSaveMinutesInterval { get { return (_saveInterval); } internal set { _saveInterval = value; if (_timer != null) _timer.Interval = _saveInterval; } }

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


        public void FireEvent(FileChangeItem fci)
        {
            try
            {
                if (System.IO.File.Exists(LogPath) && System.IO.File.Exists(fci.Path) && PathUtility.IsSameFile(fci.Path, LogPath)) return;
                if (Table.Add(fci))
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

        private void OnChanged(FileSystemEventArgs e)
        {
            string ext = System.IO.Path.GetExtension(e.FullPath);
            if (Extensions.Contains(ext))
            {
                FileChangeItem fci = new FileChangeItem(e);
                FireEvent(fci);
            }
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

        public void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_watcher != null) _watcher.Dispose();
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
            catch { }
            Table = new FileChangeTable(LogPath);
            Changed?.Invoke(this, new DoWorkEventArgs(null));
        }

        Timer timer = null;
        FileChangeTester tester;
        int testRuns;
        Random randTest = new Random();
        int maxMS;
        internal void StartBackgroundTest(int maximumMSInterval)
        {
            if (tester != null) return; // already running
            tester = new FileChangeTester(WatchPath);

            if (timer == null)
            {
                timer = new Timer(100);
                timer.Enabled = false;
                timer.Elapsed += Timer_Elapsed;
            }
            maxMS = maximumMSInterval;

            timer.Interval = 100; // first one quickly
            testRuns = 0;
            timer.Start();
        }

        internal void StopBackgroundTest()
        {
            if (timer != null) timer.Stop();
            tester = null;
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            timer.Interval = randTest.Next(100, maxMS);
            var fci = tester.GetFileChangeTestItem(DateTime.Now);
            if (fci == null) return;
            fci.AuxInfo = "TESTING";
            testRuns++;
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
            if (Table != null) Table.AutoWrite();
        }
    }
}

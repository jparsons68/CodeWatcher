using System;
using System.Globalization;
using System.IO;

namespace CodeWatcher
{
    public class FileChangeItem
    {
        DateTime _dateTime;
        static string dtFormat = "yyyy MM dd HH mm ss";
        public string Path { get; private set; }
        public string ProjectPath { get; private set; }
        public DateTime DateTime { get { return _dateTime; } private set { _dateTime = FileChangeTable.Round(value, oneSec); } }
        public WatcherChangeTypes ChangeType { get; private set; }

        public FileChangeProject Project { get; internal set; }
        public FileChangeProjectDay Day { get; internal set; }

        public string Name { get { return (System.IO.Path.GetFileName(Path)); } }

        public string AuxInfo { get; internal set; }
        public FileChangeTable Table { get; internal set; }
        public bool TimeBoxContained
        {
            get
            {
                return (Project != null && Project.TimeBoxContains(this.DateTime));
            }
        }

        public static FileChangeItem GetFileChangeItem(string str)
        {
            var fci = new FileChangeItem(str);
            return (fci.ProjectPath == null ? null : fci);
        }
        public static FileChangeItem GetFileChangeItem(string path, DateTime dt, WatcherChangeTypes changeType)
        {
            var fci = new FileChangeItem(path, dt, changeType);
            return (fci.ProjectPath == null ? null : fci);
        }
        public FileChangeItem(string str)
        {
            DateTime dt;
            string path;
            WatcherChangeTypes ct;
            string auxInfo = null;
            try
            {
                var part = str.Split("<>".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                path = part[0];
                CultureInfo provider = CultureInfo.InvariantCulture;
                dt = DateTime.ParseExact(part[2], dtFormat, provider);
                ct = (WatcherChangeTypes)Enum.Parse(typeof(WatcherChangeTypes), part[4]);
                if (part.Length > 6) auxInfo = part[6];
            }
            catch (Exception ex)
            {
                Console.WriteLine("File Parse error:" + ex.ToString());
                path = null;
                dt = DateTime.MinValue;
                ct = WatcherChangeTypes.Changed;
            }

            Path = path;
            ProjectPath = _getProjectPath();
            DateTime = dt;
            ChangeType = ct;
            AuxInfo = auxInfo;
        }

        public FileChangeItem(string path, DateTime dt, WatcherChangeTypes changeType)
        {
            Path = path;
            ProjectPath = _getProjectPath();
            DateTime = dt;
            ChangeType = changeType;
        }

        public FileChangeItem(FileSystemEventArgs e)
        {
            Path = e.FullPath;
            ProjectPath = _getProjectPath();
            DateTime = DateTime.Now;
            ChangeType = e.ChangeType;
        }

        public override string ToString()
        {
            string line = "<" + Path + "> <" + DateTime.ToString(dtFormat) + "> <" + ChangeType.ToString() + ">";
            if (AuxInfo != null) line += (" <" + AuxInfo + ">");
            return (line);
        }

        internal bool IsInPeriod(DateTime startTime, DateTime endTime)
        {
            return (DateTime >= startTime && DateTime <= endTime);
        }
        private string _getProjectPath()
        {
            string myFolder = System.IO.Path.GetDirectoryName(Path);
            return (_getProjectPath(myFolder));
        }

        private string _getProjectPath(string myFolder)
        {
            if (myFolder == null) return (null);

            var files = Directory.GetFiles(myFolder, "*.csproj");
            if (files != null && files.Length > 0)
            {
                return (myFolder);
            }
            else
            {
                try
                {
                    myFolder = System.IO.Directory.GetParent(myFolder).FullName;
                }
                catch (Exception)
                {
                    return (null);
                }
                return (_getProjectPath(myFolder));
            }
        }

        static TimeSpan oneSec = new TimeSpan(0, 0, 1);
        internal static bool EqualPathAndTime(FileChangeItem fci1, FileChangeItem fci2)
        {
            if (fci1 == null || fci2 == null) return (false);
            return (
                fci1.Path == fci2.Path &&
                FileChangeTable.Equal(fci1.DateTime, fci2.DateTime, oneSec));
        }
    }
}

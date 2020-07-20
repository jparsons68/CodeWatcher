using Microsoft.Win32;
using System;
using System.Globalization;
using System.IO;

namespace CodeWatcher
{

    // based on naming of WatcherChangeTypes
    // but with some additions
    [Flags]
    public enum ActionTypes
    {
        None = 0,
        Created = 1,
        Deleted = 2,
        Changed = 4,
        Renamed = 8,

        Suspend = 16,
        Resume = 32,

        UserIdle = 64,

    }

    public enum SortBy
    {
        MostRecentFirst,
        EarliestFirst,
        Alphabetical,
        ReverseAlphabetical
    }


    public static class EnumExtensions
    {
        public static int Count<T>(this T source) where T : IConvertible//enum
        {
            if (!typeof(T).IsEnum)
                throw new ArgumentException("T must be an enumerated type");

            return Enum.GetNames(typeof(T)).Length;
        }

        public static string BinaryString(this Enum value, int width)
        {
            int i = Convert.ToInt32(value);
            return Convert.ToString(i, 2).PadLeft(32, '0').Substring(32 - width);
        }
        public static bool HasAny(this Enum value, Enum flags)
        {
            return
                value != null && ((Convert.ToInt32(value) & Convert.ToInt32(flags)) != 0);
        }


        public static T Next<T>(this T src) where T : struct
        {
            if (!typeof(T).IsEnum) throw new ArgumentException(String.Format("Argument {0} is not an Enum", typeof(T).FullName));

            T[] Arr = (T[])Enum.GetValues(src.GetType());
            int j = Array.IndexOf<T>(Arr, src) + 1;
            return (Arr.Length == j) ? Arr[0] : Arr[j];
        }
    }

    public class ActivityItem
    {




        DateTime _dateTime;
        static string dtFormat = "yyyy MM dd HH mm ss";
        public string Path { get; private set; }
        public string ProjectPath { get; set; }
        public DateTime DateTime { get { return _dateTime; } private set { _dateTime = FileChangeTable.Round(value, oneSec); } }
        public ActionTypes ChangeType { get; private set; }

        public FileChangeProject Project { get; internal set; }
        public FileChangeDay Day { get; internal set; }

        public string Name { get { return (Path != null ? System.IO.Path.GetFileName(Path) : null); } }

        public string AuxInfo { get; internal set; }
        public FileChangeTable Table { get; internal set; }
        public bool TimeBoxContained
        {
            get
            {
                try
                {
                    return (Project.Table.IsInSelectedRange(this.DateTime));
                }
                catch
                {
                    return (false);
                }
            }
        }

        public static ActivityItem GetFileChangeItem(string str)
        {
            var fci = new ActivityItem(str);
            return (fci.IsValid ? fci : null);
        }
        public static ActivityItem GetFileChangeItem(string path, DateTime dt, ActionTypes changeType)
        {
            var fci = new ActivityItem(path, dt, changeType);
            return (fci.IsValid ? fci : null);
        }
        public ActivityItem(string str)
        {
            DateTime dt;
            string path;
            ActionTypes ct;
            string auxInfo = null;
            try
            {
                var part = _splitByChevrons(str);
                path = part[0].Length > 0 ? part[0] : null;
                CultureInfo provider = CultureInfo.InvariantCulture;
                dt = DateTime.ParseExact(part[1], dtFormat, provider);
                ct = (ActionTypes)Enum.Parse(typeof(ActionTypes), part[2]);
                if (part.Length > 3) auxInfo = part[3];
            }
            catch (Exception ex)
            {
                Console.WriteLine("File Parse error:" + ex.ToString());
                path = null;
                dt = DateTime.MinValue;
                ct = ActionTypes.Changed;
            }

            Path = path;
            ProjectPath = _getProjectPath();
            DateTime = dt;
            ChangeType = ct;
            AuxInfo = auxInfo;
        }

        public ActivityItem(ActivityItem fci, string path)
        {
            Path = path;
            ProjectPath = path;
            DateTime = fci.DateTime;
            ChangeType = fci.ChangeType;
            AuxInfo = fci.AuxInfo;
        }

        private string[] _splitByChevrons(string str)
        {
            System.Collections.Generic.List<string> ret = new System.Collections.Generic.List<string>();

            while (true)
            {
                int idx0 = str.IndexOf("<", StringComparison.Ordinal);
                if (idx0 == -1) break;
                int idx1 = str.IndexOf(">", idx0 + 1, StringComparison.Ordinal);
                if (idx1 == -1) break;

                string part = str.Substring(idx0 + 1, idx1 - idx0 - 1);
                ret.Add(part);

                str = str.Substring(idx1 + 1);
            }

            return ret.ToArray();
        }


        public ActivityItem(string path, DateTime dt, ActionTypes changeType)
        {
            Path = path;
            ProjectPath = _getProjectPath();
            DateTime = dt;
            ChangeType = changeType;
        }

        public ActivityItem(FileSystemEventArgs e)
        {
            Path = e.FullPath;
            ProjectPath = _getProjectPath();
            DateTime = DateTime.Now;
            ChangeType = _convert(e.ChangeType);
        }

        public ActivityItem(PowerModeChangedEventArgs e)
        {
            Path = null;
            ProjectPath = null;
            DateTime = DateTime.Now;
            ChangeType = _convert(e.Mode);
        }

        public ActivityItem()
        {
            Path = null;
            ProjectPath = null;
            DateTime = DateTime.Now;
            ChangeType = ActionTypes.UserIdle;
        }

        private ActionTypes _convert(PowerModes mode)
        {
            switch (mode)
            {
                case PowerModes.Resume: return (ActionTypes.Resume);
                case PowerModes.Suspend: return (ActionTypes.Suspend);
                default: return (ActionTypes.None);
            }
        }

        private ActionTypes _convert(WatcherChangeTypes changeType)
        {
            switch (changeType)
            {
                case WatcherChangeTypes.Created: return (ActionTypes.Created);
                case WatcherChangeTypes.Deleted: return (ActionTypes.Deleted);
                case WatcherChangeTypes.Changed: return (ActionTypes.Changed);
                case WatcherChangeTypes.Renamed: return (ActionTypes.Renamed);
                default: return (ActionTypes.None);
            }
        }

        public override string ToString()
        {
            string line = "<" + Path + "><" + DateTime.ToString(dtFormat) + "><" + ChangeType.ToString() + ">";
            if (AuxInfo != null) line += ("<" + AuxInfo + ">");
            return (line);
        }

        internal bool IsValid
        {
            get
            {
                switch (ChangeType)
                {
                    case ActionTypes.None:
                        return (false);
                    case ActionTypes.Created:
                    case ActionTypes.Deleted:
                    case ActionTypes.Changed:
                    case ActionTypes.Renamed:
                        return (ProjectPath != null);
                    default:
                        return (true);
                }
            }
        }

        public bool IsPermittedPath
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Path)) return (false);
                if (Path.Contains("\\obj\\")) return (false);
                if (Path.Contains("\\bin\\")) return (false);
                return (true);
            }
        }

        internal bool IsInPeriod(DateTime startTime, DateTime endTime)
        {
            return (DateTime >= startTime && DateTime <= endTime);
        }
        private string _getProjectPath()
        {
            if (string.IsNullOrEmpty(Path)) return (null);

            string ext = System.IO.Path.GetExtension(Path);
            string myFolder;
            if (ext == "") myFolder = Path;
            else myFolder = System.IO.Path.GetDirectoryName(Path);
            return (_getProjectPath(myFolder));
        }

        private string _getProjectPath(string myFolder)
        {
            if (myFolder == null) return (null);

            var files = Directory.GetFiles(myFolder, "*.csproj");
            if (files.Length > 0)
            {
                return (myFolder);
            }
            else
            {
                try
                {
                    myFolder = Directory.GetParent(myFolder).FullName;
                }
                catch (Exception)
                {
                    return (null);
                }
                return (_getProjectPath(myFolder));
            }
        }

        static TimeSpan oneSec = new TimeSpan(0, 0, 1);

        internal static bool EqualPathAndTime(ActivityItem fci1, ActivityItem fci2)
        {
            if (fci1 == null || fci2 == null) return (false);
            if (fci1.Path == null || fci2.Path == null) return (false);
            return (
                fci1.Path == fci2.Path &&
                FileChangeTable.Equal(fci1.DateTime, fci2.DateTime, oneSec));
        }
    }
}

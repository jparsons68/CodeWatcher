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
        NONE = 0,
        CREATED = 1,
        DELETED = 2,
        CHANGED = 4,
        RENAMED = 8,

        SUSPEND = 16,
        RESUME = 32,

        USER_IDLE = 64,
    }

    public enum SortBy
    {
        MOST_RECENT_FIRST,
        EARLIEST_FIRST,
        ALPHABETICAL,
        REVERSE_ALPHABETICAL
    }


    public static class EnumExtensions
    {
        // ReSharper disable once UnusedMember.Global
        public static int Count<T>(this T source) where T : IConvertible//enum
        {
            if (!typeof(T).IsEnum)
                throw new ArgumentException("T must be an enumerated type");

            return Enum.GetNames(typeof(T)).Length;
        }

        // ReSharper disable once UnusedMember.Global
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
            if (!typeof(T).IsEnum) throw new ArgumentException($"Argument {typeof(T).FullName} is not an Enum");

            T[] arr = (T[])Enum.GetValues(src.GetType());
            int j = Array.IndexOf(arr, src) + 1;
            return (arr.Length == j) ? arr[0] : arr[j];
        }
    }

    public class ActivityItem
    {




        DateTime _dateTime;
        private const string DtFormat = "yyyy MM dd HH mm ss";
        public string Path { get; }
        public string ProjectPath { get; set; }
        public DateTime DateTime { get => _dateTime;
            private set => _dateTime = FileChangeTable.Round(value, OneSec);
        }
        public ActionTypes ChangeType { get; }

        public FileChangeProject Project { get; internal set; }
        public FileChangeDay Day { get; internal set; }

        public string Name => System.IO.Path.GetFileName(Path);

        public string AuxInfo { get; internal set; }
        public FileChangeTable Table { get; internal set; }
        public bool IsInSelectedRange
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
                dt = DateTime.ParseExact(part[1], DtFormat, provider);
                ct = (ActionTypes)Enum.Parse(typeof(ActionTypes), part[2], true);
                if (part.Length > 3) auxInfo = part[3];
            }
            catch (Exception ex)
            {
                Console.WriteLine(@"File Parse error:" + ex);
                path = null;
                dt = DateTime.MinValue;
                ct = ActionTypes.CHANGED;
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
            ChangeType = ActionTypes.USER_IDLE;
        }

        private ActionTypes _convert(PowerModes mode)
        {
            switch (mode)
            {
                case PowerModes.Resume: return (ActionTypes.RESUME);
                case PowerModes.Suspend: return (ActionTypes.SUSPEND);
                default: return (ActionTypes.NONE);
            }
        }

        private ActionTypes _convert(WatcherChangeTypes changeType)
        {
            switch (changeType)
            {
                case WatcherChangeTypes.Created: return (ActionTypes.CREATED);
                case WatcherChangeTypes.Deleted: return (ActionTypes.DELETED);
                case WatcherChangeTypes.Changed: return (ActionTypes.CHANGED);
                case WatcherChangeTypes.Renamed: return (ActionTypes.RENAMED);
                default: return (ActionTypes.NONE);
            }
        }

        public override string ToString()
        {
            string line = "<" + Path + "><" + DateTime.ToString(DtFormat) + "><" + ChangeType.ToString() + ">";
            if (AuxInfo != null) line += ("<" + AuxInfo + ">");
            return (line);
        }

        internal bool IsValid
        {
            get
            {
                switch (ChangeType)
                {
                    case ActionTypes.NONE:
                        return (false);
                    case ActionTypes.CREATED:
                    case ActionTypes.DELETED:
                    case ActionTypes.CHANGED:
                    case ActionTypes.RENAMED:
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
            var myFolder = ext == "" ? Path : System.IO.Path.GetDirectoryName(Path);
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

        static readonly TimeSpan OneSec = new TimeSpan(0, 0, 1);

        internal static bool EqualPathAndTime(ActivityItem fci1, ActivityItem fci2)
        {
            if (fci1 == null || fci2 == null) return (false);
            if (fci1.Path == null || fci2.Path == null) return (false);
            return (
                fci1.Path == fci2.Path &&
                FileChangeTable.Equal(fci1.DateTime, fci2.DateTime, OneSec));
        }
    }
}

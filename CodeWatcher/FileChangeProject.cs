using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace CodeWatcher
{
    public class FileChangeProject
    {
        public FileChangeProject(string path, FileChangeTable table)
        {
            this.Path = path;
            this.Table = table;
            var t0 = table.StartTime.Date;
            var t1 = table.EndTime.Date;
            foreach (DateTime day in FileChangeTable.EachDay(t0, t1))
                DaysCollection.Add(new FileChangeProjectDay(day, this));
        }

        public override string ToString()
        {
            return Name + " : " + Path + " Changes:" + CountChanges();
        }
        Color _color = Color.Gray;
        Brush _brush = null;

        public Color Color
        {
            get
            {
                _initColors();
                return (_color);
            }
        }

        public Brush Brush
        {
            get
            {
                _initColors();
                return (_brush);
            }
        }

        private void _initColors()
        {
            if (_brush == null)
            {
                int hC = Path != null ? Path.GetHashCode() : 0;
                _color = Color.FromArgb(hC);
                _color = Color.FromArgb(255, _color);
                _brush = new SolidBrush(_color);
            }
        }

        public string Name { get { return System.IO.Path.GetFileName(Path); } }
        public string Path { get; set; }
        public FileChangeTable Table { get; }
        public DateTime StartTime { get; }
        public DateTime EndTime { get; }
        public List<FileChangeItem> Collection { get; set; } = new List<FileChangeItem>();
        public List<FileChangeProjectDay> DaysCollection { get; set; } = new List<FileChangeProjectDay>();

        internal void Add(FileChangeItem fci)
        {
            if (fci != null)
            {
                // to straight listing
                Collection.Add(fci);
                if (fci != null) fci.Project = this;
                // to day schedule listing
                FileChangeProjectDay projectDay = _getDay(fci.DateTime);
                if (projectDay != null)
                    projectDay.Add(fci);
            }
        }

        public int CountChanges(DateTime dt0, DateTime dt1)
        {
            return (DaysCollection.Sum(pDay => pDay.IsInPeriod(dt0, dt1) ? pDay.Count : 0));
        }
        public int CountChanges()
        {
            return (DaysCollection.Sum(pDay => pDay.Count));
        }

        internal int GetPeakOpsInDay(DateTime dt0, DateTime dt1)
        {
            return (DaysCollection.Max(pDay => pDay.IsInPeriod(dt0, dt1) ? pDay.Count : 0));
        }
        private FileChangeProjectDay _getDay(DateTime dateTime)
        {
            return DaysCollection.FirstOrDefault(pDay => pDay.IsOnDay(dateTime));
        }

        internal int GetDayIndex(DateTime dateTime)
        {
            int idx = DaysCollection.FindIndex(pDay => pDay.IsOnDay(dateTime));
            return (idx);
        }
        internal FileChangeProjectDay GetDay(int idx)
        {
            return ((idx >= 0 && idx < DaysCollection.Count) ? DaysCollection[idx] : null);
        }

    }
}

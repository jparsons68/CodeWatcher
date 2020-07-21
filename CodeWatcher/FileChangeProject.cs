using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace CodeWatcher
{
    public class FileChangeProject
    {
        public FileChangeProject(string path, FileChangeTable table)
        {
            this.Path = path;
            this.Table = table;
            Reinitialize();
        }

        public void Reinitialize()
        {
            Collection.Clear();
            DaysCollection.Clear();
            var t0 = Table.StartTime.Date;
            var t1 = Table.EndTime.Date;
            foreach (DateTime day in FileChangeTable.EachDay(t0, t1))
                DaysCollection.Add(new FileChangeDay(day, this));
        }


        public bool Visible { get; set; } = true;
        public override string ToString()
        {
            return Name + " : " + Path + " Changes:" + CountChanges();
        }

        Color _color = Color.Gray;
        Brush _brush;
        Brush _contBrush;

        public Color Color
        {
            get
            {
                _initColors();
                return (_color);
            }
            set
            {
                _color = value;
                _rebrush();
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
        public Brush ContrastBrush
        {
            get
            {
                _initColors();
                return (_contBrush);
            }
        }

        public static ColorRotator ColorRotator = new ColorRotator();
        private void _initColors()
        {
            if (_brush == null)
            {
                _color = ColorRotator.Next();
                _color = ThemeColors.LimitColor(_color);
                _rebrush();
            }
        }

        private void _rebrush()
        {
            _brush = new SolidBrush(_color);
            _contBrush = Utilities.ColorUtilities.GetContrastBrush(_color);
        }

        public void RandomizeColor()
        {
            _color = ColorRotator.Random();
            _color = ThemeColors.LimitColor(_color);
            _rebrush();
        }

        public string Name => System.IO.Path.GetFileName(Path);
        public string Path { get; set; }
        public FileChangeTable Table { get; }
        public List<ActivityItem> Collection { get; set; } = new List<ActivityItem>();
        public List<FileChangeDay> DaysCollection { get; set; } = new List<FileChangeDay>();
        public bool Selected { get; set; }
        public int EditCount { get; set; }
        public int SelectedEditCount { get; set; }

        internal void Add(ActivityItem fci)
        {
            if (fci != null)
            {
                // to straight listing
                Collection.Add(fci);
                fci.Project = this;
                // to day schedule listing
                FileChangeDay projectDay = _getDay(fci.DateTime);
                projectDay?.Add(fci);
            }
        }

        public int CountChanges(DateTime dt0, DateTime dt1)
        {
            return (DaysCollection.Sum(pDay => pDay != null && pDay.IsInPeriod(dt0, dt1) ? pDay.Count : 0));
        }
        public int CountChanges()
        {
            return (DaysCollection.Sum(pDay => pDay.Count));
        }

        internal int GetPeakOpsInDay(DateTime dt0, DateTime dt1)
        {
            return (DaysCollection.Max(pDay => pDay.IsInPeriod(dt0, dt1) ? pDay.Count : 0));
        }

        public void RemoveEdits(DateTime? start, DateTime? end)
        {
            this.Collection.RemoveAll(fci =>
            {
                bool contains = FileChangeTable.IsInRange(fci.DateTime, start, end);
                if (contains) //also remove from big list
                    this.Table.ItemCollection.Remove(fci);
                return (contains);
            });
        }

        private FileChangeDay _getDay(DateTime dateTime)
        {
            return DaysCollection.FirstOrDefault(pDay => pDay.IsOnDay(dateTime));
        }

        internal string GetSetting()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("PATH:" + Path);
            sb.AppendLine("VISIBLE:" + Visible.ToString());
            sb.AppendLine("COLOR:" + ColorTranslator.ToHtml(Color));
            sb.AppendLine("END");
            return (sb.ToString());
        }





        internal void ExtractAndApplySetting(List<string> savedProjectSettings)
        {
            try
            {
                // find the right place
                bool found = false;
                bool viz = true;
                Color? color = null;
                foreach (var line in savedProjectSettings)
                {
                    if (!found)
                    {
                        string path = FileChangeTable.Extract(line, "PATH:");
                        if (path != null && path == Path) found = true;
                    }
                    else
                    {
                        if (line == "END") break;

                        string txt = FileChangeTable.Extract(line, "VISIBLE:");
                        if (txt != null) viz = bool.Parse(txt);

                        var txtCol = FileChangeTable.Extract(line, "COLOR:");
                        if (txtCol != null) color = ColorTranslator.FromHtml(txtCol);
                    }

                }

                if (found)
                {
                    Visible = viz;
                    if (color != null) { _brush = null; this.Color = (Color)color; }
                }
            }
            catch
            {
                // ignored
            }
        }


        internal int GetDayIndex(DateTime dateTime)
        {
            int idx = DaysCollection.FindIndex(pDay => pDay.IsOnDay(dateTime));
            return (idx);
        }
        internal FileChangeDay GetDay(int idx)
        {
            return ((idx >= 0 && idx < DaysCollection.Count) ? DaysCollection[idx] : null);
        }
        internal FileChangeDay GetDay(DateTime date)
        {
            var firstDay = DaysCollection.FirstOrDefault();
            if (firstDay == null) return (null);
            int idx = (date - firstDay.DateTime).Days;
            FileChangeDay pDay = (idx >= 0 && idx < DaysCollection.Count) ? DaysCollection[idx] : null;
            return (pDay);
        }

    }

}

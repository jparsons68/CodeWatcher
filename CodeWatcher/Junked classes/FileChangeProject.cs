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

        internal bool TimeBoxContains(DateTime dt)
        {
            return (TimeBoxCollection.FirstOrDefault(trb => trb.Contains(dt)) != null);
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

        static public  ColorRotator ColorRotator = new ColorRotator();
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

        public string Name { get { return System.IO.Path.GetFileName(Path); } }
        public string Path { get; set; }
        public FileChangeTable Table { get; }
        public List<ActivityItem> Collection { get; set; } = new List<ActivityItem>();
        public List<FileChangeDay> DaysCollection { get; set; } = new List<FileChangeDay>();
        public List<TimeBox> TimeBoxCollection { get; set; } = new List<TimeBox>();
        //internal ActivityTrace ActivityTrace { get; set; }
        //internal bool IsActivityTraced { get { return (ActivityTrace != null && ActivityTrace.ScopeInBox.Collection.Count > 0); } }
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

        public DateTime? GetDateTime(TRB_KIND kind, TRB_PART part, TRB_PART where, TRB_STATE whereState)
        {
            DateTime? dt = null;
            switch (kind)
            {
                case TRB_KIND.MINIMUM:
                    dt = TimeBoxCollection.Where(trb => trb.GetDateTime(where, whereState) != null).Min(trb => trb.GetDateTime(part));
                    break;
                case TRB_KIND.MAXIMUM:
                    dt = TimeBoxCollection.Where(trb => trb.GetDateTime(where, whereState) != null).Max(trb => trb.GetDateTime(part));
                    break;
                case TRB_KIND.FIRST:
                    {
                        var atrb =
                         TimeBoxCollection.Where(trb => trb.GetDateTime(where, whereState) != null).FirstOrDefault(trb => null != trb.GetDateTime(part));
                        dt = atrb != null ? atrb.GetDateTime(part) : null;
                    }
                    break;
                case TRB_KIND.LAST:
                    {
                        var atrb =
                         TimeBoxCollection.Where(trb => trb.GetDateTime(where, whereState) != null).LastOrDefault(trb => null != trb.GetDateTime(part));
                        dt = atrb != null ? atrb.GetDateTime(part) : null;
                    }
                    break;
            }
            return (dt);
        }


        internal void SetDateTime(TRB_PART part, TRB_STATE partState, DateTime dt)
        {
            TimeBoxCollection.ForEach(trb => trb.SetDateTime(part, partState, dt));
        }

        internal void TimeBoxProcess(Action<TimeBox> p)
        {
            TimeBoxCollection.ForEach(trb => trb.Process(p));
        }
        internal List<DateTime> GetDateTimes(TRB_PART part, TRB_STATE partState)
        {
            List<DateTime> ret = new List<DateTime>();
            foreach (var trb in TimeBoxCollection)
            {
                var tmp = trb.GetDateTimes(part, partState);
                if (tmp.Count > 0) ret.AddRange(tmp);
            }

            return (ret);
        }






        internal void WriteState(TRB_PART part, TRB_STATE state)
        {
            TimeBoxCollection.ForEach(trb => trb.WriteState(part, state));
        }
        internal void WriteState(TRB_PART part, TRB_PART where, TRB_STATE whereState, TRB_STATE state)
        {
            TimeBoxCollection.ForEach(trb => trb.WriteState(part, where, whereState, state));
        }
        internal void WriteState(TRB_PART part, TRB_PART where, TRB_STATE state)
        {
            TimeBoxCollection.ForEach(trb => trb.WriteState(part, where, state));
        }

        internal void SetState(TRB_PART part, TRB_STATE state)
        {
            TimeBoxCollection.ForEach(trb => trb.SetState(part, state));
        }
        internal void SetState(TRB_PART part, TRB_PART where, TRB_STATE whereState, TRB_STATE state)
        {
            TimeBoxCollection.ForEach(trb => trb.SetState(part, where, whereState, state));
        }
        internal void SetState(TRB_PART part, TRB_PART where, TRB_STATE state)
        {
            TimeBoxCollection.ForEach(trb => trb.SetState(part, where, state));
        }

        internal void UnsetState(TRB_PART part, TRB_STATE state)
        {
            TimeBoxCollection.ForEach(trb => trb.UnsetState(part, state));
        }
        internal void UnsetState(TRB_PART part, TRB_PART where, TRB_STATE whereState, TRB_STATE state)
        {
            TimeBoxCollection.ForEach(trb => trb.UnsetState(part, where, whereState, state));
        }
        internal void UnsetState(TRB_PART part, TRB_PART where, TRB_STATE state)
        {
            TimeBoxCollection.ForEach(trb => trb.UnsetState(part, where, state));
        }

        internal void ToggleState(TRB_PART part, TRB_STATE state)
        {
            TimeBoxCollection.ForEach(trb => trb.ToggleState(part, state));
        }
        internal void ToggleState(TRB_PART part, TRB_PART where, TRB_STATE whereState, TRB_STATE state)
        {
            TimeBoxCollection.ForEach(trb => trb.ToggleState(part, where, whereState, state));
        }
        internal void ToggleState(TRB_PART part, TRB_PART where, TRB_STATE state)
        {
            TimeBoxCollection.ForEach(trb => trb.ToggleState(part, where, state));
        }


        internal void DeleteTimeBoxes(TRB_PART part, TRB_STATE state)
        {
            TimeBoxCollection.RemoveAll(trb => trb.HasState(part, state));
        }
        internal void TimeBoxClearContainedEdits(TRB_PART part, TRB_STATE state)
        {
            // edits within time boxes
            TimeBoxCollection.ForEach(trb =>
            {
                if (trb.HasState(part, state)) 
                    trb.ClearContainedEdits();
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
            string txt = ColorTranslator.ToHtml(Color);
            sb.AppendLine("COLOR:" + txt);
            foreach (var trb in TimeBoxCollection) sb.AppendLine("TIMEBOX:" + trb.GetSetting());
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
                bool gotColor = false;
                Color color = Color.Gray;
                List<TimeBox> tboxes = new List<TimeBox>();
                foreach (var line in savedProjectSettings)
                {
                    string path = _extract("PATH:", line);
                    if (path != null && path == Path) { found = true; continue; }
                    if (!found) continue;
                    if (line == "END") break;
                    string txt = _extract("VISIBLE:", line);
                    if (txt != null) { viz = bool.Parse(txt); continue; }

                    txt = _extract("COLOR:", line);
                    if (txt != null) { color = ColorTranslator.FromHtml(txt); gotColor = true; continue; }

                    txt = _extract("TIMEBOX:", line);
                    if (txt != null)
                    {
                        TimeBox trb = TimeBox.FromSetting(txt);
                        if (trb != null) tboxes.Add(trb);
                    }
                }

                if (found)
                {
                    Visible = viz;
                    if (gotColor) { _color = color; _brush = null; }
                    TimeBoxCollection.Clear();
                    TimeBoxAdd(tboxes);
                }
            }
            catch
            {
                // ignored
            }
        }

        private string _extract(string head, string txt)
        {
            if (txt.StartsWith(head)) return (txt.Substring(head.Length));
            return (null);
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

        internal bool HasState(TRB_PART part, TRB_STATE state)
        {
            return (TimeBoxCollection.FirstOrDefault(trb => trb.HasState(part, state)) != null);
        }

        internal TimeBox TimeBoxAdd(DateTime start, DateTime end)
        {
            // add time box
            var trb = new TimeBox(start, end, this);
            TimeBoxCollection.Add(trb);
            TimeBox._sort(TimeBoxCollection);
            TimeBoxMerge();
            return (trb);
        }
        internal void TimeBoxAdd(List<TimeBox> collection)
        {
            foreach (var src in collection)
            {
                var trb = new TimeBox(src.StartDate, src.EndDate, this);
                trb.CopyStates(src);
                TimeBoxCollection.Add(trb);
            }
            TimeBox._sort(TimeBoxCollection);
            TimeBoxMerge();
        }

        internal void TimeBoxSort()
        {
            TimeBox._sort(TimeBoxCollection);
        }

        internal void TimeBoxMerge()
        {
            // carry out on sorted
            for (int i = 0; i < TimeBoxCollection.Count - 1; i++)
            {
                var tbA = TimeBoxCollection[i];
                var tbB = TimeBoxCollection[i + 1];
                if (tbA.IntersectInclusive(tbB))
                {
                    tbA.StartState = tbA.StartState | tbB.StartState;
                    tbA.EndState = tbA.EndState | tbB.EndState;
                    tbA.WholeState = tbA.WholeState | tbB.WholeState;
                    tbA.EndDate = tbB.EndDate;
                    TimeBoxCollection.RemoveAt(i + 1);
                    i--;
                }
            }
        }

        internal void TimeBoxClear()
        {
            TimeBoxCollection.Clear();
        }

        internal TimeBox TimeBoxGet(Guid uid)
        {
            return (TimeBoxCollection.FirstOrDefault(trb => trb.UID == uid));
        }

       
    }

}

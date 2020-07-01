using MoreLinq;
using System;
using System.Collections;
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
            Reinitialize();
        }

        public void Reinitialize()
        {
            var t0 = Table.StartTime.Date;
            var t1 = Table.EndTime.Date;
            foreach (DateTime day in FileChangeTable.EachDay(t0, t1))
                DaysCollection.Add(new FileChangeProjectDay(day, this));
        }






        int _dt2minuteIndex(DateTime dt) { return ((int)((dt - Table.StartTime.Date).TotalMinutes)); }



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
        Brush _brush = null;
        Brush _contBrush = null;

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
        public Brush ContrastBrush
        {
            get
            {
                _initColors();
                return (_contBrush);
            }
        }

        static ColorRotator colorRotator = new ColorRotator();
        private void _initColors()
        {
            if (_brush == null)
            {
                //int hC = Path != null ? Path.GetHashCode() : 0;
                //_color = Color.FromArgb(hC);
                //_color = Color.FromArgb(255, _color);
                _color = colorRotator.Next();
                _color = ThemeColors.LimitColor(_color);
                _brush = new SolidBrush(_color);
                _contBrush = Utilities.ColorUtilities.GetContrastBrush(_color);
            }
        }

        public string Name { get { return System.IO.Path.GetFileName(Path); } }
        public string Path { get; set; }
        public FileChangeTable Table { get; }
        public List<FileChangeItem> Collection { get; set; } = new List<FileChangeItem>();
        public List<FileChangeProjectDay> DaysCollection { get; set; } = new List<FileChangeProjectDay>();
        public List<TimeBox> TimeBoxCollection { get; set; } = new List<TimeBox>();
        internal ActivityTrace ActivityTrace { get; set; }
        internal bool IsActivityTraced { get { return (ActivityTrace != null && ActivityTrace.Collection.Count > 0); } }

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
                default:
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
        internal FileChangeProjectDay GetDay(DateTime date)
        {
            var firstDay = DaysCollection.FirstOrDefault();
            if (firstDay == null) return (null);
            int idx = (date - firstDay.DateTime).Days;
            FileChangeProjectDay pDay = (idx >= 0 && idx < DaysCollection.Count) ? DaysCollection[idx] : null;
            return (pDay);
        }

        internal bool HasState(TRB_PART part, TRB_STATE state)
        {
            return (TimeBoxCollection.FirstOrDefault(trb => trb.HasState(part, state)) != null);
        }




        //internal void RemoveOverlapRanges(DateTime start, DateTime end)
        // {

        //     List<TimeBox> tmp = new List<TimeBox>();
        //     // first: deletes that split..
        //     foreach (var trb in RangeCollection)
        //     {
        //         if (start > trb.StartDate && end < trb.EndDate)
        //         {
        //             tmp.Add(new TimeBox(end, trb.EndDate, this));
        //             trb.EndDate = start;
        //         }
        //     }

        //     if (tmp.Count > 0)
        //     {
        //         RangeCollection.AddRange(tmp);
        //         TimeBox._sort(RangeCollection);
        //     }

        //     // plain remove
        //     RangeCollection.RemoveAll(trb => (start <= trb.StartDate && end >= trb.EndDate));

        //     // truncates
        //     foreach (var trb in RangeCollection)
        //     {
        //         if (end > trb.StartDate && end <= trb.EndDate) trb.StartDate = end;
        //         else if (start >= trb.StartDate && start < trb.EndDate) trb.EndDate = start;


        //     }

        //     RangeCollection.RemoveAll(trb => trb.StartDate == trb.EndDate);// remove zeros
        // }

        internal TimeBox TimeBoxAdd(DateTime start, DateTime end)
        {
            // add time box
            var trb = new TimeBox(start, end, this);
            TimeBoxCollection.Add(trb);
            TimeBox._sort(TimeBoxCollection);
            TimeBoxMerge();
            //Table.ClearActivityGrid();
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
            //Table.ClearActivityGrid();
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
            //Table.ClearActivityGrid();
        }

        internal TimeBox TimeBoxGet(Guid uid)
        {
            return (TimeBoxCollection.FirstOrDefault(trb => trb.UID == uid));
        }

    }

    public partial class FileChangeTable
    {

        public TimeBox AddRange(DateTime start, DateTime end, FileChangeProject project)
        {
            if (project == null) return (null);

            var span = end - start;
            if (span.TotalMinutes <= 0) return (null);

            // remove overlapping ranges
            project.TimeBoxCollection.RemoveAll(trb => trb.IntersectExclusive(start, end));
            // add range
            var tRange = new TimeBox(start, end, project);
            project.TimeBoxCollection.Add(tRange);
            // sort range
            TimeBox._sort(project.TimeBoxCollection);
            //ClearActivityGrid();
            return (tRange);
        }



        public TRB_PART GetParts(TRB_STATE state)
        {
            TRB_PART trB_PART = TRB_PART.NONE;

            foreach (var proj in ProjectCollection)
            {
                foreach (var range in proj.TimeBoxCollection)
                {
                    if (range.HasState(TRB_PART.START, state)) trB_PART |= TRB_PART.START;
                    if (range.HasState(TRB_PART.END, state)) trB_PART |= TRB_PART.END;
                    if (range.HasState(TRB_PART.WHOLE, state)) trB_PART |= TRB_PART.WHOLE;
                }
            }
            return (trB_PART);
        }



        public DateTime? GetDateTime(TRB_KIND kind, TRB_PART part, TRB_PART where, TRB_STATE whereState)
        {
            DateTime? dt = null;
            switch (kind)
            {
                case TRB_KIND.MINIMUM:
                    dt = ProjectCollection.Min(proj => proj.GetDateTime(kind, part, where, whereState));
                    //dt = ProjectCollection.Where(proj => proj.GetPosition(trB_PART, state, kind) != null).Min(proj => proj.GetPosition(trB_PART, state, kind));
                    break;
                case TRB_KIND.MAXIMUM:
                    dt = ProjectCollection.Max(proj => proj.GetDateTime(kind, part, where, whereState));
                    break;
                case TRB_KIND.FIRST:
                    break;
                case TRB_KIND.LAST:
                    break;
                default:
                    break;
            }
            return (dt);
        }

        public List<DateTime> GetDateTimes(TRB_PART part, TRB_STATE partState)
        {
            List<DateTime> ret = new List<DateTime>();
            foreach (var proj in ProjectCollection)
            {
                List<DateTime> tmp = proj.GetDateTimes(part, partState);
                if (tmp != null && tmp.Count > 0) ret.AddRange(tmp);
            }
            return (ret);
        }
        internal void SetDateTime(TRB_PART part, TRB_STATE partState, DateTime dt)
        {
            ProjectCollection.ForEach(proj => proj.SetDateTime(part, partState, dt));
        }

        private void TimeBoxProcess(Action<TimeBox> p)
        {
            ProjectCollection.ForEach(proj => proj.TimeBoxProcess(p));
        }
        internal void WriteState(TRB_PART part, TRB_STATE state)
        {
            ProjectCollection.ForEach(proj => proj.WriteState(part, state));
        }
        internal void WriteState(TRB_PART part, TRB_PART where, TRB_STATE whereState, TRB_STATE state)
        {
            ProjectCollection.ForEach(proj => proj.WriteState(part, where, whereState, state));
        }
        internal void WriteState(TRB_PART part, TRB_PART where, TRB_STATE state)
        {
            ProjectCollection.ForEach(proj => proj.WriteState(part, where, state));
        }

        internal void SetState(TRB_PART part, TRB_STATE state)
        {
            ProjectCollection.ForEach(proj => proj.SetState(part, state));
        }
        internal void SetState(TRB_PART part, TRB_PART where, TRB_STATE whereState, TRB_STATE state)
        {
            ProjectCollection.ForEach(proj => proj.SetState(part, where, whereState, state));
        }
        internal void SetState(TRB_PART part, TRB_PART where, TRB_STATE state)
        {
            ProjectCollection.ForEach(proj => proj.SetState(part, where, state));
        }

        internal void UnsetState(TRB_PART part, TRB_STATE state)
        {
            ProjectCollection.ForEach(proj => proj.UnsetState(part, state));
        }
        internal void UnsetState(TRB_PART part, TRB_PART where, TRB_STATE whereState, TRB_STATE state)
        {
            ProjectCollection.ForEach(proj => proj.UnsetState(part, where, whereState, state));
        }
        internal void UnsetState(TRB_PART part, TRB_PART where, TRB_STATE state)
        {
            ProjectCollection.ForEach(proj => proj.UnsetState(part, where, state));
        }

        internal void ToggleState(TRB_PART part, TRB_STATE state)
        {
            ProjectCollection.ForEach(proj => proj.ToggleState(part, state));
        }
        internal void ToggleState(TRB_PART part, TRB_PART where, TRB_STATE whereState, TRB_STATE state)
        {
            ProjectCollection.ForEach(proj => proj.ToggleState(part, where, whereState, state));
        }
        internal void ToggleState(TRB_PART part, TRB_PART where, TRB_STATE state)
        {
            ProjectCollection.ForEach(proj => proj.ToggleState(part, where, state));
        }


        internal void TimeBoxDelete(TRB_PART part, TRB_STATE state)
        {
            ProjectCollection.ForEach(proj => proj.DeleteTimeBoxes(part, state));
            ActivityTraceBuilder.Build(this);
        }

        TRB_PART movingPart;
        DateTime startDT;
        TimeSpan spanL;
        TimeSpan spanR;
        TimeSpan ptrAdjTS;
        TimeBox trbClicked;

        public static void writeOut(TRB_STATE state, bool withHeader = true)
        {
            if (withHeader)
                Console.WriteLine("a  sWAS : aNY,   sNAPPED, WARNING, ARMED, SELECTED");
            Console.WriteLine(Convert.ToString((int)state, 2).PadLeft(32, '0').Substring(25));
        }
        public void MoveSelectedEdges(TimeBox trbClicked, TRB_PART partMov, DateTime dt, TimeSpan snapSpan, TRB_ACTION action)
        {
            // rules..
            // don't move other edges ???
            if (action == TRB_ACTION.BEGIN)
            {
                var clickedDT = (DateTime)trbClicked.GetDateTime(partMov);
                startDT = dt;
                this.trbClicked = trbClicked;

                ptrAdjTS = dt - (DateTime)trbClicked.GetDateTime(partMov);

                // 1 unset all edges
                UnsetState(TRB_PART.START | TRB_PART.END, TRB_STATE.SELECTED);
                // 2 set edges accordingly 
                SetState(partMov, TRB_PART.WHOLE, TRB_STATE.SELECTED);

                //ClearActivityGrid();
                // 3. what edge are we moving, start or end?
                movingPart = GetParts(TRB_STATE.SELECTED);
                // 4.ensure just one
                if (movingPart.HasFlag(TRB_PART.START)) movingPart = TRB_PART.START;
                if (movingPart.HasFlag(TRB_PART.END)) movingPart = TRB_PART.END;

                TimeBox adj;
                spanL = TimeSpan.MinValue;
                spanR = TimeSpan.MaxValue;


                if (movingPart == TRB_PART.START)
                {
                    // left move
                    // 5. Get the minimum distance to the previous end
                    foreach (var proj in ProjectCollection)
                    {
                        foreach (var trb in proj.TimeBoxCollection)
                        {
                            if (trb.HasState(movingPart, TRB_STATE.SELECTED))
                            {
                                trb.StoreDates();
                                adj = _getAdj(trb, -1);
                                if (adj != null) spanL = _max(spanL, adj.EndDate - trb.StartDate);
                                spanR = _min(spanR, trb.EndDate - trb.StartDate);
                            }
                        }
                    }
                }
                else if (movingPart == TRB_PART.END)
                {
                    foreach (var proj in ProjectCollection)
                    {
                        foreach (var trb in proj.TimeBoxCollection)
                        {
                            if (trb.HasState(movingPart, TRB_STATE.SELECTED))
                            {
                                trb.StoreDates();
                                adj = _getAdj(trb, +1);
                                if (adj != null) spanR = _min(spanR, adj.StartDate - trb.EndDate);
                                spanL = _max(spanL, trb.StartDate - trb.EndDate);
                            }
                        }
                    }
                }
            }

            bool snapped = false;
            if (snapSpan.TotalMinutes > 0)
            {
                // snap the active edge only
                snapped = _snapToUnselectedEdges(ref dt, snapSpan);
            }

            TimeSpan spanDT = dt - startDT;
            if (spanDT < spanL) spanDT = spanL;
            if (spanDT > spanR) spanDT = spanR;

            foreach (var proj in ProjectCollection)
            {
                foreach (var trb in proj.TimeBoxCollection)
                {
                    if (trb.HasState(movingPart, TRB_STATE.SELECTED))
                        trb.ShiftDateTime(movingPart, spanDT);
                }
            }

            if (snapped) SetState(TRB_PART.END, TRB_PART.END, TRB_STATE.SELECTED, TRB_STATE.SNAPPED);
            else UnsetState(TRB_PART.END, TRB_PART.END, TRB_STATE.SELECTED, TRB_STATE.SNAPPED);


            // finally snap the unsnapped to others nearest day
            if (action == TRB_ACTION.FINISH)
            {
                // condition: Selected but not snapped
                TimeBoxProcess(delegate (TimeBox trb)
                {
                    if (trb.HasState(movingPart, TRB_STATE.SELECTED) &&
                        trb.HasState(movingPart, TRB_STATE.SNAPPED | TRB_STATE.NOT))
                    {
                        // snap to whole day
                        var myDT = (DateTime)trb.GetDateTime(movingPart);
                        var snapDT = FileChangeTable.Round(myDT, new TimeSpan(1, 0, 0, 0));

                        var thisSpan = snapDT - trb.GetStoredDateTime(movingPart);
                        if (thisSpan < spanL) thisSpan = spanL;
                        if (thisSpan > spanR) thisSpan = spanR;
                        trb.ShiftDateTime(movingPart, (TimeSpan)thisSpan);
                    }
                });

                ProjectCollection.ForEach(proj =>
                {
                    if (proj.HasState(TRB_PART.WHOLE, TRB_STATE.SELECTED))
                    {
                        proj.TimeBoxSort();
                        proj.TimeBoxMerge();
                    }
                });

                ActivityTraceBuilder.Build(this);
                movingPart = TRB_PART.NONE;
            }

        }

        private TimeSpan _min(TimeSpan spanA, TimeSpan spanB)
        {
            return (spanA < spanB ? spanA : spanB);
        }
        private TimeSpan _max(TimeSpan spanA, TimeSpan spanB)
        {
            return (spanA > spanB ? spanA : spanB);
        }

        private TimeBox _getAdj(TimeBox trb, int dir)
        {
            int idx = trb.Project.TimeBoxCollection.IndexOf(trb);
            if (idx == -1) return (null);
            idx += dir;
            if (idx >= 0 && idx < trb.Project.TimeBoxCollection.Count)
                return (trb.Project.TimeBoxCollection[idx]);
            return (null);
        }

        private bool _snapToUnselectedEdges(ref DateTime dt, TimeSpan snapSpan)
        {
            bool snapped = false;

            UnsetState(TRB_PART.START | TRB_PART.END, TRB_STATE.SNAPPED_TO);

            DateTime myDT = dt - ptrAdjTS;
            TimeSpan closeSpan = TimeSpan.MaxValue;
            TimeBox trbClose = null;
            TRB_PART closePart = TRB_PART.NONE;
            foreach (var proj in ProjectCollection)
            {
                foreach (var trb in proj.TimeBoxCollection)
                {
                    if (trb.HasState(TRB_PART.WHOLE, TRB_STATE.NOT | TRB_STATE.SELECTED))
                    {
                        var tmpSpan = (trb.StartDate - myDT).Duration();
                        if (tmpSpan < closeSpan) { trbClose = trb; closeSpan = tmpSpan; closePart = TRB_PART.START; }
                        tmpSpan = (trb.EndDate - myDT).Duration();
                        if (tmpSpan < closeSpan) { trbClose = trb; closeSpan = tmpSpan; closePart = TRB_PART.END; }
                    }
                }
            }

            if (trbClose != null && closeSpan <= snapSpan)
            {
                snapped = true;
                dt = (DateTime)trbClose.GetDateTime(closePart);
                dt = dt + ptrAdjTS;
                trbClose.SetState(closePart, TRB_STATE.SNAPPED_TO);
            }




            //if (snapDT.Count > 0)
            //{
            //    DateTime closest = snapDT.MinBy(st => (st - myDT).Duration().TotalDays).First();
            //    if ((closest - myDT).Duration().TotalDays <= snapSpan.TotalDays)
            //    {
            //        snapped = true;

            //        dt = closest;
            //        dt = dt + ptrAdjTS;
            //    }
            //}
            return (snapped);
        }
    }

}

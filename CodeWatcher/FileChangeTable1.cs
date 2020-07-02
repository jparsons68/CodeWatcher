using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeWatcher
{
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

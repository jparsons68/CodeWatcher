using System;
using System.Linq;

namespace CodeWatcher
{
    public partial class FileChangeTable
    {
        public DateTime? GetDateTime(TRB_KIND kind, TRB_PART part, TRB_PART where, TRB_STATE whereState)
        {
            DateTime? dt = null;
            switch (kind)
            {
                case TRB_KIND.MINIMUM:
                    dt = ProjectCollection.Min(proj => proj.GetDateTime(kind, part, where, whereState));
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


        internal bool HasState(TRB_PART part, TRB_STATE state)
        {
            return (ProjectCollection.FirstOrDefault(proj => proj.HasState(part, state)) != null);
        }


        internal void TimeBoxDelete(TRB_PART part, TRB_STATE state, bool visibleOnly = true)
        {
            ProjectCollection.ForEach(proj =>
                {
                    if (proj.Visible || !visibleOnly)
                        proj.DeleteTimeBoxes(part, state);
                }
            );
            ActivityTraceBuilder.Buildv2(this);
        }

        internal void TimeBoxClearContainedEdits(TRB_PART part, TRB_STATE state, bool visibleOnly = true)
        {
            lock (tableLock)
            {
                ProjectCollection.ForEach(proj =>
                    {
                        if (proj.Visible || !visibleOnly)
                            proj.TimeBoxClearContainedEdits(part, state);
                    }
                );
                _sortAndSanitize();
                _setTimeExtents();
                _organizeIntoProjects();
                ActivityTraceBuilder.Buildv2(this);
            }
        }

        TRB_PART movingPart;
        DateTime startDT;
        TimeSpan spanL;
        TimeSpan spanR;
        TimeSpan ptrAdjTS_0;
        TimeSpan ptrAdjTS_1;

        public static void writeOut(TRB_STATE state, bool withHeader = true)
        {
            if (withHeader)
                Console.WriteLine("a  sWAS : aNY,   sNAPPED, WARNING, ARMED, SELECTED");
            Console.WriteLine(Convert.ToString((int)state, 2).PadLeft(32, '0').Substring(25));
        }

        public void MoveSelectedEdges(TimeBox trbClicked, TRB_PART partMov, DateTime dt, TimeSpan snapSpan,
            TRB_ACTION action)
        {
            // rules..
            // don't move other edges ???
            if (action == TRB_ACTION.BEGIN)
            {
                startDT = dt;

                ptrAdjTS_0 = dt - (DateTime)trbClicked.GetDateTime(TRB_PART.START);
                ptrAdjTS_1 = dt - (DateTime)trbClicked.GetDateTime(TRB_PART.END);

                // 1 unset all edges
                UnsetState(TRB_PART.START | TRB_PART.END, TRB_STATE.SELECTED);
                // 2 set edges accordingly 
                SetState(partMov, TRB_PART.WHOLE, TRB_STATE.SELECTED);

                // 3. what edge are we moving, start or end?
                movingPart = partMov;

                TimeBox adj;
                spanL = TimeSpan.MinValue;
                spanR = TimeSpan.MaxValue;

                if (movingPart.HasFlag(TRB_PART.START))
                {
                    // left move
                    // 4. Get the minimum distance to the previous end
                    foreach (var proj in EachVisibleProject())
                    {
                        if (!proj.Visible) continue;
                        foreach (var trb in proj.TimeBoxCollection)
                        {
                            if (trb.HasState(movingPart, TRB_STATE.SELECTED))
                            {
                                trb.StoreDates();
                                adj = _getAdj(trb, -1);
                                if (adj != null) spanL = _max(spanL, adj.EndDate - trb.StartDate);
                                if (!movingPart.HasFlag(TRB_PART.END))
                                    spanR = _min(spanR, trb.EndDate - trb.StartDate);
                            }
                        }
                    }
                }

                if (movingPart.HasFlag(TRB_PART.END))
                {
                    // right move
                    // 5. Get the MAximuim distance to the next start
                    foreach (var proj in EachVisibleProject())
                    {
                        if (!proj.Visible) continue;
                        foreach (var trb in proj.TimeBoxCollection)
                        {
                            if (trb.HasState(movingPart, TRB_STATE.SELECTED))
                            {
                                trb.StoreDates();
                                adj = _getAdj(trb, +1);
                                if (adj != null) spanR = _min(spanR, adj.StartDate - trb.EndDate);
                                if (!movingPart.HasFlag(TRB_PART.START))
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

            foreach (var proj in EachVisibleProject())
            {
                if (!proj.Visible) continue;
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

                foreach (var proj in EachVisibleProject())
                {
                    if (!proj.Visible) continue;
                    if (proj.HasState(TRB_PART.WHOLE, TRB_STATE.SELECTED))
                    {
                        proj.TimeBoxSort();
                        proj.TimeBoxMerge();
                    }
                }

                ActivityTraceBuilder.Buildv2(this);
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

            DateTime myDT_0 = dt - ptrAdjTS_0;
            DateTime myDT_1 = dt - ptrAdjTS_1;
            TimeSpan closeSpan = TimeSpan.MaxValue;
            TimeBox trbClose = null;
            TRB_PART closePart = TRB_PART.NONE;
            TRB_PART movingPartThatSnapped = TRB_PART.NONE;

            foreach (var proj in EachVisibleProject())
            {
                if (!proj.Visible) continue;
                ;
                foreach (var trb in proj.TimeBoxCollection)
                {
                    if (trb.HasState(TRB_PART.WHOLE, TRB_STATE.NOT | TRB_STATE.SELECTED))
                    {
                        TimeSpan tmpSpan;

                        if (movingPart.HasFlag(TRB_PART.START))
                        {
                            tmpSpan = (trb.StartDate - myDT_0).Duration();
                            if (tmpSpan < closeSpan)
                            {
                                trbClose = trb;
                                closeSpan = tmpSpan;
                                closePart = TRB_PART.START;
                                movingPartThatSnapped = TRB_PART.START;
                            }

                            tmpSpan = (trb.EndDate - myDT_0).Duration();
                            if (tmpSpan < closeSpan)
                            {
                                trbClose = trb;
                                closeSpan = tmpSpan;
                                closePart = TRB_PART.END;
                                movingPartThatSnapped = TRB_PART.START;
                            }
                        }

                        if (movingPart.HasFlag(TRB_PART.END))
                        {
                            tmpSpan = (trb.StartDate - myDT_1).Duration();
                            if (tmpSpan < closeSpan)
                            {
                                trbClose = trb;
                                closeSpan = tmpSpan;
                                closePart = TRB_PART.START;
                                movingPartThatSnapped = TRB_PART.END;
                            }

                            tmpSpan = (trb.EndDate - myDT_1).Duration();
                            if (tmpSpan < closeSpan)
                            {
                                trbClose = trb;
                                closeSpan = tmpSpan;
                                closePart = TRB_PART.END;
                                movingPartThatSnapped = TRB_PART.END;
                            }
                        }
                    }
                }
            }

            if (trbClose != null && closeSpan <= snapSpan)
            {
                snapped = true;
                dt = (DateTime)trbClose.GetDateTime(closePart);
                dt = dt + (movingPartThatSnapped == TRB_PART.START ? ptrAdjTS_0 : ptrAdjTS_1);
                trbClose.SetState(closePart, TRB_STATE.SNAPPED_TO);
            }

            return (snapped);
        }


        void _unselectAllProjects()
        {
            this.ProjectCollection.ForEach(p => p.Selected = false);
        }

        public void SelectProject(FileChangeProject proj, SelectionBehavior bhav)
        {
            switch (bhav)
            {
                case SelectionBehavior.AppendToggle:
                    // append project selection, keep all just toggle this one
                    if (proj != null) proj.Selected = !proj.Selected;
                    break;
                case SelectionBehavior.Append:
                    // append project selection, keep all just toggle this one
                    if (proj != null) proj.Selected = true;
                    break;
                case SelectionBehavior.Unselect:
                    _unselectAllProjects();
                    break;

                case SelectionBehavior.SelectOnly:
                    _unselectAllProjects();
                    if (proj != null) proj.Selected = true;
                    break;

                case SelectionBehavior.UnselectOnToggle:
                    if (proj != null)
                    {
                        // count visible, selected projects
                        int nVisSel = this.ProjectCollection.Sum(p => p.Visible && p.Selected ? 1 : 0);
                        bool pState = proj.Selected;
                        // unselect all others
                        _unselectAllProjects();
                        // > 1 project selected ? project selected  : toggle project
                        proj.Selected = nVisSel > 1 ? true : !pState;
                    }
                    else
                        _unselectAllProjects();

                    break;

                case SelectionBehavior.UnselectToggle:
                    if (proj != null)
                    {
                        // unselect others
                        bool pState = proj.Selected;
                        _unselectAllProjects();
                        // toggle proj
                        proj.Selected = !pState;
                    }
                    else
                        _unselectAllProjects();

                    break;
            }
        }
    }

    public enum SelectionBehavior
    {
        None,
        AppendToggle,
        UnselectToggle,
        UnselectOnToggle,
        Unselect,
        Append,
        SelectOnly
    }

    public enum DataState
    {
        False,
        True,
        TrueOrFalse,
        Ignore
    }

}

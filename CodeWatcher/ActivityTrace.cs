using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeWatcher
{
    static class ActivityTraceBuilder
    {
        // needs update when
        // (re)startup Y
        // item added Y
        // item deleted(never happens) NA
        public static double PerEditMinutes { get; set; } = 60;
        public static bool UseIdleEvents { get; set; } = true;

        public static void ClearActivity(FileChangeTable table)
        {
            table.Activity.Clear();

            foreach (var proj in table.ProjectCollection)
            {
                proj.EditCount = 0;
                proj.SelectedEditCount = 0;
            }
        }


        public static string FormatSummary(double tms)
        {
            int totalMinutes = (int)tms;
            int wdays, hours, mins;
            if (totalMinutes > 0)
            {
                wdays = (int)TimeConvert.Convert(totalMinutes, Tcunit.MINUTES, Tcunit.WORKINGDAYS); // working days
                hours = (int)TimeConvert.Convert(
                    totalMinutes - TimeConvert.Convert(wdays, Tcunit.WORKINGDAYS, Tcunit.MINUTES),
                    Tcunit.MINUTES, Tcunit.HOURS);
                mins = (int)(totalMinutes - TimeConvert.Convert(hours, Tcunit.HOURS, Tcunit.MINUTES) -
                              TimeConvert.Convert(wdays, Tcunit.WORKINGDAYS, Tcunit.MINUTES));
            }
            else
            {
                wdays = hours = mins = 0;
            }

            return $"{wdays:00}:{hours:00}:{mins:00}";
        }




        public static void Buildv2(FileChangeTable table)
        {
            ClearActivity(table);

            // 1 . MAKE list of everything selected
            List<ActivityItem> masterList = new List<ActivityItem>();

            foreach (var proj in table.ProjectCollection)
            {
                if (proj.Visible)
                {
                    foreach (var fci in proj.Collection)
                    {
                        bool isEdit =
                            !fci.ChangeType.HasAny(ActionTypes.Resume | ActionTypes.Suspend | ActionTypes.UserIdle);

                        if (isEdit)
                            fci.Project.EditCount++;

                        if (fci.IsInSelectedRange)
                        {
                            masterList.Add(fci);
                            if (isEdit)
                                fci.Project.SelectedEditCount++;
                        }
                    }
                }
            }

            // sort nicely
            masterList.Sort((x, y) => DateTime.Compare(x.DateTime, y.DateTime));


            // apply smoothing
            ActivityItem s0 = null;
            DateTime endActivity = DateTime.MinValue;
            List<ActivityBaseBlock> activityBaseBlocks = new List<ActivityBaseBlock>();

            int editCount = 0;
            foreach (var fci in masterList)
            {
                if (fci.ChangeType.HasFlag(ActionTypes.Resume)) continue;
                else if (UseIdleEvents && fci.ChangeType.HasAny(ActionTypes.Suspend | ActionTypes.UserIdle))
                { // end at the user's idle
                    endActivity = fci.DateTime;
                    _addActivityBlock();
                    s0 = null;
                }
                else
                {
                    editCount++;
                    if (fci.DateTime > endActivity)
                    {
                        _addActivityBlock();
                        s0 = fci;
                    }
                    endActivity = fci.DateTime.AddMinutes(ActivityTraceBuilder.PerEditMinutes);
                }
            }
            _addActivityBlock();

            table.Activity.Collection = activityBaseBlocks;
            table.Activity.EditCount = editCount;
            table.Activity.TotalMinutes = activityBaseBlocks.Sum(blk => (blk.EndDate - blk.StartDate).TotalMinutes);

            void _addActivityBlock()
            {
                if (s0 != null)
                {
                    ActivityBaseBlock blk = new ActivityBaseBlock(s0.DateTime, endActivity);
                    activityBaseBlocks.Add(blk);
                }
            }

        }

    }

    public class ActivityBaseBlock
    {
        public ActivityBaseBlock(DateTime start, DateTime end)
        {
            this.StartDate = start;
            this.EndDate = end;
        }
        public ActivityBaseBlock(DateTime start)
        {
            this.StartDate = start;
        }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

}



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
        // timebox create Y
        // timebox delete Y
        // timebox update Y
        public static double PerEditMinutes { get; set; } = 60;
        public static bool UseIdleEvents { get; set; } = true;

        public static void Clear(FileChangeTable table)
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
            table.Activity.Clear();

            // 1 . MAKE list of everything selected
            List<ActivityItem> masterList = new List<ActivityItem>();

            foreach (var proj in table.ProjectCollection)
            {
                if (proj.Visible == false) return;

                foreach (var fci in proj.Collection)
                {
                    bool isEdit =
                        !fci.ChangeType.HasAny(ActionTypes.Resume | ActionTypes.Suspend | ActionTypes.UserIdle);

                    if (isEdit)
                        fci.Project.EditCount++;

                    if (fci.TimeBoxContained)
                    {
                        masterList.Add(fci);
                        if (isEdit)
                            fci.Project.SelectedEditCount++;
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

        // public static void BuildX(FileChangeTable table)
        // {
        //     List<ActivityProjectBlock> masterList = new List<ActivityProjectBlock>();
        //     foreach (var proj in table.ProjectCollection)
        //     {
        //         // build traces for ALL and just the selecteds
        //         ActivityTrace trace = new ActivityTrace(proj);
        //         proj.ActivityTrace = trace;
        //
        //         // 1 add start, end times to composite list for trace line construction
        //         foreach (var actBlk in trace.ScopeAll.Collection)
        //         {
        //             masterList.Add(new ActivityProjectBlock(actBlk.StartDate));
        //             masterList.Add(new ActivityProjectBlock(actBlk.EndDate));
        //         }
        //     }
        //
        //
        //     // 2. sort
        //     masterList.Sort((x, y) => DateTime.Compare(x.StartDate, y.StartDate));
        //     // 3. remove duplicates
        //     masterList = masterList.DistinctBy(x => x.StartDate).ToList();
        //
        //     // 4 each activity add event if eventsStart <= act < event
        //     // act.addProj
        //     foreach (var act in masterList)
        //     {
        //         // what is happening at this time?
        //         foreach (var proj in table.ProjectCollection)
        //         {
        //             if (proj.IsActivityTraced == false) continue;
        //             ActivityBaseBlock happen = proj.ActivityTrace.ScopeInBox.GetActivity(act.StartDate);
        //             if (happen != null) act.Add(proj);
        //         }
        //     }
        //
        //
        //
        //
        //
        //
        //     // extract project activities trace
        //     // match project
        //     foreach (var proj in table.ProjectCollection)
        //     {
        //         List<ActivityLineBlock> activityLine = null;
        //         if (proj.IsActivityTraced)
        //         {
        //             activityLine = new List<ActivityLineBlock>();
        //             ActivityLineBlock alBlk = null;
        //             foreach (var act in masterList)
        //             {
        //                 double amt = (act.IsMatch(proj)) ? 1.0 / act.ProjectCount : 0.0;
        //                 if (alBlk != null && Math.Abs(amt - alBlk.ActivityAmount) < Double.Epsilon) continue; // same, no need to add
        //                 alBlk = new ActivityLineBlock(act.StartDate, amt);
        //                 activityLine.Add(alBlk);
        //             }
        //
        //             for (int i = 1; i < activityLine.Count; i++)
        //                 activityLine[i - 1].EndDate = activityLine[i].StartDate;
        //             if (activityLine.Count > 0) activityLine.Last().EndDate = proj.TimeBoxCollection.Last().EndDate;
        //         }
        //
        //         proj.ActivityTrace.Add(activityLine);
        //     }
        //
        //     table.TotalMinutesSelected = table.ProjectCollection.Sum(proj => proj.ActivityTrace != null ? proj.ActivityTrace.TotalMinutesSelected : 0.0);
        // }
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

    // class ActivityProjectBlock : ActivityBaseBlock
    // {
    //     public ActivityProjectBlock(DateTime start) : base(start)
    //     {
    //     }
    //
    //     List<FileChangeProject> Collection { get; set; } = new List<FileChangeProject>();
    //
    //     public int ProjectCount { get { return (Collection.Count); } }
    //     internal void Add(FileChangeProject proj)
    //     {
    //         Collection.Add(proj);
    //     }
    //
    //     internal bool IsMatch(FileChangeProject proj)
    //     {
    //         return (Collection.Contains(proj));
    //     }
    // }

    // class ActivityLineBlock : ActivityBaseBlock
    // {
    //     public double ActivityAmount { get; }
    //
    //     public double EffectiveMinutes
    //     {
    //         get
    //         {
    //             return (ActivityAmount == 0 ? 0.0 : ActivityAmount * (EndDate - StartDate).TotalMinutes);
    //         }
    //     }
    //     public ActivityLineBlock(DateTime start, double amt) : base(start)
    //     {
    //         this.ActivityAmount = amt;
    //     }
    // }



    // class ActivityScope
    // {
    //     public List<ActivityBaseBlock> Collection { get; private set; } = new List<ActivityBaseBlock>();
    //     public int EditCount { get; private set; }
    //
    //
    //     public void Clear()
    //     {
    //         Collection.Clear();
    //         EditCount = 0;
    //     }
    //
    //     internal ActivityBaseBlock GetActivity(DateTime dt)
    //     {
    //         ActivityBaseBlock blk = new ActivityBaseBlock(dt);
    //         int idx = Collection.BinarySearch(blk, Comparer<ActivityBaseBlock>.Create(
    //             (a, b) => DateTime.Compare(a.StartDate, b.StartDate)));
    //         if (idx < 0) // larger at ~idx
    //             idx = ~idx - 1;
    //         if (idx < 0) return (null);
    //         blk = Collection[idx];
    //         if (dt >= blk.StartDate && dt < blk.EndDate) return (blk);
    //         return (null);
    //     }
    //
    //     public void Build(FileChangeProject project, bool useTimeBoxSelection)
    //     {
    //
    //         Collection.Clear();
    //         EditCount = 0;
    //
    //         if (project.Visible == false) return;
    //
    //         ActivityItem s0 = null;
    //         DateTime endActivity = DateTime.MinValue;
    //         foreach (var fci in project.Collection)
    //         {
    //             if (useTimeBoxSelection && fci.TimeBoxContained == false) continue;
    //             if (fci.ChangeType.HasFlag(ActionTypes.Resume)) continue;
    //             else if (fci.ChangeType.HasAny(ActionTypes.Suspend | ActionTypes.UserIdle))
    //             { // end at the user's idle
    //                 endActivity = fci.DateTime;
    //                 _addActivityBlock();
    //                 s0 = null;
    //             }
    //             else
    //             {
    //                 EditCount++;
    //                 if (fci.DateTime > endActivity)
    //                 {
    //                     _addActivityBlock();
    //                     s0 = fci;
    //                 }
    //                 endActivity = fci.DateTime.AddMinutes(ActivityTraceBuilder.PerEditMinutes);
    //             }
    //         }
    //         _addActivityBlock();
    //
    //         void _addActivityBlock()
    //         {
    //             if (s0 != null)
    //             {
    //                 ActivityBaseBlock blk = new ActivityBaseBlock(s0.DateTime, endActivity);
    //                 Collection.Add(blk);
    //             }
    //         }
    //     }
    // }


    // class ActivityTrace
    // {
    //     public ActivityScope ScopeAll { get; set; } = new ActivityScope();
    //     public ActivityScope ScopeInBox { get; set; } = new ActivityScope();
    //     //
    //     // public List<ActivityBaseBlock> Collection { get; private set; } = new List<ActivityBaseBlock>();
    //     // public int EditCount { get; private set; }
    //
    //     public List<ActivityLineBlock> ActivityLine { get; private set; }
    //     public double TotalMinutesSelected { get; private set; }
    //     public FileChangeProject Project { get; private set; }
    //     public string Summary { get; private set; }
    //
    //
    //     public ActivityTrace(FileChangeProject project)
    //     {
    //         this.Project = project;
    //
    //         ScopeAll.Build(project, false);
    //         ScopeInBox.Build(project, true);
    //     }
    //
    //
    //     internal void Add(List<ActivityLineBlock> activityLine)
    //     {
    //         ActivityLine = activityLine;
    //         TotalMinutesSelected = ActivityLine != null ? ActivityLine.Sum(alb => alb.EffectiveMinutes) : 0;
    //         Summary = FormatSummary(TotalMinutesSelected);
    //     }
    //
    //     public static string FormatSummary(double tms)
    //     {
    //         int totalMinutes = (int)tms;
    //         int wdays, hours, mins;
    //         if (totalMinutes > 0)
    //         {
    //             wdays = (int)TimeConvert.Convert(totalMinutes, Tcunit.MINUTES, Tcunit.WORKINGDAYS); // working days
    //             hours = (int)TimeConvert.Convert(
    //                 totalMinutes - TimeConvert.Convert(wdays, Tcunit.WORKINGDAYS, Tcunit.MINUTES),
    //                 Tcunit.MINUTES, Tcunit.HOURS);
    //             mins = (int)(totalMinutes - TimeConvert.Convert(hours, Tcunit.HOURS, Tcunit.MINUTES) -
    //                           TimeConvert.Convert(wdays, Tcunit.WORKINGDAYS, Tcunit.MINUTES));
    //         }
    //         else
    //         {
    //             wdays = hours = mins = 0;
    //         }
    //
    //         return $"{wdays:00}:{hours:00}:{mins:00}";
    //     }
    // }
}



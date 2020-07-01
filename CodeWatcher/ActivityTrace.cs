using MoreLinq;
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

        public static void Clear(FileChangeTable table)
        {
            foreach (var proj in table.ProjectCollection)
            {
                proj.ActivityTrace = null;
            }
        }


        public static void Build(FileChangeTable table)
        {
            List<ActivityProjectBlock> masterList = new List<ActivityProjectBlock>();
            foreach (var proj in table.ProjectCollection)
            {
                ActivityTrace trace = new ActivityTrace(proj);
                proj.ActivityTrace = trace;

                // 1 add start, end times to composite list
                foreach (var actBlk in trace.Collection)
                {
                    masterList.Add(new ActivityProjectBlock(actBlk.StartDate));
                    masterList.Add(new ActivityProjectBlock(actBlk.EndDate));
                }
            }

            // 2. sort
            masterList.Sort((x, y) => DateTime.Compare(x.StartDate, y.StartDate));
            // 3. remove duplicates
            masterList = masterList.DistinctBy(x => x.StartDate).ToList();

            // 4 each activity add event if eventsStart <= act < event
            // act.addProj
            foreach (var act in masterList)
            {
                // what is happening at this time?
                foreach (var proj in table.ProjectCollection)
                {
                    if (proj.IsActivityTraced == false) continue;
                    ActivityBaseBlock happn = proj.ActivityTrace.GetActivity(act.StartDate);
                    if (happn != null)
                        act.Add(proj);
                }
            }

            // extract project activities trace
            // match project
            foreach (var proj in table.ProjectCollection)
            {
                if (proj.IsActivityTraced == false) continue;
                List<ActivityLineBlock> activityLine = new List<ActivityLineBlock>();
                ActivityLineBlock alBlk = null;
                foreach (var act in masterList)
                {
                    double amt = (act.IsMatch(proj)) ? 1.0 / act.ProjectCount : 0.0;
                    if (alBlk != null && amt == alBlk.ActivityAmount) continue; // same, no need to add
                    alBlk = new ActivityLineBlock(act.StartDate, amt);
                    activityLine.Add(alBlk);
                }

                for (int i = 1; i < activityLine.Count; i++)
                    activityLine[i - 1].EndDate = activityLine[i].StartDate;
                if (activityLine.Count > 0) activityLine.Last().EndDate = proj.TimeBoxCollection.Last().EndDate;
                proj.ActivityTrace.Add(activityLine);
            }
        }
    }

    class ActivityBaseBlock
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

    class ActivityProjectBlock : ActivityBaseBlock
    {
        public ActivityProjectBlock(DateTime start) : base(start)
        {
        }

        List<FileChangeProject> Collection { get; set; } = new List<FileChangeProject>();

        public int ProjectCount { get { return (Collection.Count); } }
        internal void Add(FileChangeProject proj)
        {
            Collection.Add(proj);
        }

        internal bool IsMatch(FileChangeProject proj)
        {
            return (Collection.Contains(proj));
        }
    }

    class ActivityLineBlock : ActivityBaseBlock
    {
        public double ActivityAmount { get; }

        public double EffectiveMinutes
        {
            get
            {
                return (ActivityAmount == 0 ? 0.0 : ActivityAmount * (EndDate - StartDate).TotalMinutes);
            }
        }
        public ActivityLineBlock(DateTime start, double amt) : base(start)
        {
            this.ActivityAmount = amt;
        }
    }

    class ActivityTrace
    {
        public List<ActivityBaseBlock> Collection { get; private set; } = new List<ActivityBaseBlock>();

        public List<ActivityLineBlock> ActivityLine { get; private set; }
        public FileChangeProject Project { get; private set; }
        public string Summary { get; private set; }
        public double TotalMinutes { get; private set; }
        public int EditCount { get; private set; }
        public ActivityTrace(FileChangeProject project)
        {
            this.Project = project;
            int ACTIVITY_DURATION = 60;
            DateTime endActivity = DateTime.MinValue;
            FileChangeItem s0 = null;
            Collection.Clear();
            EditCount = 0;
            foreach (var fci in project.Collection)
            {
                if (fci.TimeBoxContained == false) continue;
                EditCount++;
                if (fci.DateTime > endActivity)
                {
                    _addActivityBlock();
                    s0 = fci;
                }
                endActivity = fci.DateTime.AddMinutes(ACTIVITY_DURATION);
            }
            _addActivityBlock();

            void _addActivityBlock()
            {
                if (s0 != null)
                {
                    ActivityBaseBlock blk = new ActivityBaseBlock(s0.DateTime, endActivity);
                    Collection.Add(blk);
                }
            }
        }

        internal ActivityBaseBlock GetActivity(DateTime dt)
        {
            ActivityBaseBlock blk = new ActivityBaseBlock(dt);
            int idx = Collection.BinarySearch(blk, Comparer<ActivityBaseBlock>.Create(
                             (a, b) => DateTime.Compare(a.StartDate, b.StartDate)));
            if (idx < 0) // larger at ~idx
                idx = ~idx - 1;
            if (idx < 0) return (null);
            blk = Collection[idx];
            if (dt >= blk.StartDate && dt < blk.EndDate) return (blk);
            return (null);
        }

        internal void Add(List<ActivityLineBlock> activityLine)
        {
            ActivityLine = activityLine;
            TotalMinutes = ActivityLine.Sum(alb => alb.EffectiveMinutes);
            Summary = FormatSummary(TotalMinutes);
        }

        public static string FormatSummary(double tms)
        {
            int totalMinutes = (int)tms;
            if (totalMinutes == 0) return (null);
            int wdays = totalMinutes / 450;// working days
            int hours = (totalMinutes - wdays * 450) / 60;
            int mins = totalMinutes - (hours * 60) - (wdays * 450);
            return string.Format("{0:00}:{1:00}:{2:00}", wdays, hours, mins);
        }

        public static string FormatSummaryFull(double tms)
        {
            int totalMinutes = (int)tms;
            if (totalMinutes == 0) return (null);

            int days = totalMinutes / 450;// working days
            int hours = (totalMinutes - days * 450) / 60;
            int mins = totalMinutes - (hours * 60) - (days * 450);

            if (days > 0) // more than a day
                return (days + (days == 1 ? " day, " : " days, ") + hours + (hours == 1 ? " hour, " : " hours, ") + mins + " minutes");
            else if (hours > 0)
                return (hours + (hours == 1 ? " hour, " : " hours, ") + mins + " minutes");
            else
                return (mins + " minutes");
        }
    }
}



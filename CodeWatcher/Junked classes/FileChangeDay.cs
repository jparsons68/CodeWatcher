using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeWatcher
{
    public class FileChangeDay
    {
        public FileChangeProject Project { get; private set; }
        public DateTime DateTime { get; private set; }
        public List<ActivityItem> Collection { get; private set; } = new List<ActivityItem>();


        public DateTime EstimatedStart
        {
            get { return (Collection.FirstOrDefault().DateTime); }

        }
        public DateTime EstimatedEnd
        {
            get
            {
                var last = Collection.LastOrDefault();
                if (last == null) return (DateTime.Now);
                var dT = last.DateTime.AddMinutes(ActivityTraceBuilder.PerEditMinutes);
                return (dT);
            }
        }

        public TimeSpan EstimatedDuration
        {
            get
            {
                return (EstimatedEnd - EstimatedStart);
            }
        }


        public int Count
        {
            get { return Collection.Count; }
        }

        public FileChangeDay(DateTime dateTime, FileChangeProject project)
        {
            DateTime = dateTime.Date;
            Project = project;
        }

        internal void Add(ActivityItem fci)
        {
            Collection.Add(fci);
            fci.Day = this;
        }

        internal bool IsOnDay(DateTime dt)
        {
            return (dt.Date == DateTime);
        }

        internal bool IsInPeriod(DateTime dt0, DateTime dt1)
        {
            return DateTime >= dt0 && DateTime <= dt1;
        }

        public string GetChangedFiles()
        {
            var grps = Collection.GroupBy(fci => fci.Path);
            List<string> pathsList = grps.Select(grp => grp.First().Name + " : " + _pluralize(grp.Count(), "edit")).ToList();
            pathsList.Sort();
            return (string.Join(Environment.NewLine, pathsList));
        }

        string _pluralize(int n, string itemName)
        {
            if (n == 0 || n > 1)
                return (n + " " + itemName + "s");
            else
                return (n + " " + itemName);
        }
    }
}

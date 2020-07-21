using System;
using System.Collections.Generic;
using System.Linq;
using MoreLinq.Extensions;

namespace CodeWatcher
{
    public enum EditSort
    {
        NAME,
        EDIT_COUNT,
        FIRST_EDIT,
        LAST_EDIT
    }
    public class FileChangeDay
    {
        public FileChangeProject Project { get; private set; }
        public DateTime DateTime { get; private set; }
        public List<ActivityItem> Collection { get; private set; } = new List<ActivityItem>();


        public DateTime EstimatedStart
        {
            get
            {
                var fci = Collection.FirstOrDefault();
                return fci != null ? fci.DateTime : DateTime;
            }

        }
        public DateTime EstimatedEnd
        {
            get
            {
                var fci = Collection.LastOrDefault();
                if (fci == null) return (DateTime);
                var dT = fci.DateTime.AddMinutes(ActivityTraceBuilder.PerEditMinutes);
                return (dT);
            }
        }

        public TimeSpan EstimatedDuration { get { return (EstimatedEnd - EstimatedStart); } }


        public int Count { get { return Collection.Count; } }

        public object EditCount
        {
            get
            {
                return (Collection.Sum(fci =>
                    fci.ChangeType.HasAny(ActionTypes.RESUME | ActionTypes.SUSPEND | ActionTypes.USER_IDLE) ? 0 : 1));
            }
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

        // sort by
        // edits
        // name

        public string GetChangedFiles(EditSort sort)
        {
            List<string> pathsList;


            int colW = Collection.Max(fci => fci.Name.Length);
            colW++;
            // time (usually last time edited)
            string header;
            switch (sort)
            {
                case EditSort.NAME:
                default:
                    header = "File\u2193".PadRight((colW)) + " Edits" + "   Time";
                    pathsList = Collection
                        .OrderByDescending(fci => fci.DateTime)
                        .OrderBy(fci => fci.Name)
                        .GroupBy(fci => fci.Path)
                        .Select(g => _format(g, colW))
                        .ToList();
                    break;

                case EditSort.EDIT_COUNT:
                    header = "File".PadRight((colW)) + " Edits\u2191" + "  Time";
                    pathsList = Collection
                        .OrderByDescending(fci => fci.DateTime)
                        .GroupBy(fci => fci.Path)
                        .OrderByDescending(grp => grp.Count())
                        .Select(g => _format(g, colW))
                        .ToList();
                    break;

                case EditSort.FIRST_EDIT:
                    header = "File".PadRight((colW)) + " Edits" + "   Time" + "\u2193";
                    pathsList = Collection
                        .OrderBy(fci => fci.DateTime)
                        .GroupBy(fci => fci.Path)
                        .Select(g => _format(g, colW))
                        .ToList();
                    break;
                case EditSort.LAST_EDIT:
                    header = "File".PadRight((colW)) + " Edits" + "   Time" + "\u2191";
                    pathsList = Collection
                        .OrderByDescending(fci => fci.DateTime)
                        .GroupBy(fci => fci.Path)
                        .Select(g => _format(g, colW))
                        .ToList();
                    break;
            }

            pathsList.Insert(0, header);
            return string.Join(Environment.NewLine, pathsList);
        }

        private string _format(IGrouping<string, ActivityItem> g, int colW)
        {
            return g.First().Name.PadRight(colW) + "  " + g.Count().ToString().PadLeft(4) + "   " +
                   g.First().DateTime.ToString("hh:mmtt");
        }

        string _pluralize(int n, string itemName)
        {
            if (n == 0 || n > 1) return (n + " " + itemName + "s");
            else return (n + " " + itemName);
        }
    }
}

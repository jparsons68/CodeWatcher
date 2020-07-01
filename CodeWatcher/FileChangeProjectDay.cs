using System;
using System.Collections.Generic;

namespace CodeWatcher
{
    public class FileChangeProjectDay
    {
        public FileChangeProject Project { get; private set; }
        public DateTime DateTime { get; private set; }
        public List<FileChangeItem> Collection { get; private set; } = new List<FileChangeItem>();
        public int Count { get { return Collection.Count; } }

        public FileChangeProjectDay(DateTime dateTime, FileChangeProject project)
        {
            DateTime = dateTime.Date;
            Project = project; 
        }


        internal void Add(FileChangeItem fci)
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
    }
}

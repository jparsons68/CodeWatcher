using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CodeWatcher
{
    public class FileChangeTester
    {
        Random rand = new Random();
        List<string> files = new List<string>();

        public FileChangeTester(string rootPath)
        {
            files = new List<string>();
            AddPath(rootPath);
        }

        public void AddPath(string rootPath)
        {
            try
            {
                var tmp = Directory.GetFiles(rootPath, "*.cs", SearchOption.AllDirectories).Where(fl => (!fl.Contains(@"\obj\") && !fl.Contains(@"\bin\"))).ToList();
                if (tmp.Count > 0) files.AddRange(tmp);
            }
            catch
            { }
        }
        public ActivityItem GetFileChangeTestItem(DateTime dt)
        {
            if (files.Count == 0) return (null);
            // given number of events to generate
            // randomly get a matching file
            int ic = 1000;
            while (ic >= 0)
            {
                ic--;
                string path = files[rand.Next(files.Count)];
                ActivityItem fci = ActivityItem.GetFileChangeItem(path, dt, _randomChange());
                if (fci != null) return (fci);
            }
            return (null);
        }

        public bool GenerateTestLog(string path, int length, DateTime from, DateTime to)
        {
            if (files.Count == 0) return (false);

            List<ActivityItem> collection = new List<ActivityItem>();
            for (int i = 0; i < length; i++)
            {
                DateTime dt = FileChangeTable.GetRandomDate(from, to);
                var fci = GetFileChangeTestItem(dt);
                if (fci != null)
                    collection.Add(fci);
            }
            collection = FileChangeTable.SortAndSanitize(collection);

            FileChangeTable.Write(null, collection, SortBy.Alphabetical, path);

            return (true);
        }
        ActionTypes _randomChange()
        {
            Array values = Enum.GetValues(typeof(ActionTypes));
            while (true)
            {
                ActionTypes randomBar = (ActionTypes)values.GetValue(rand.Next(values.Length));
                if (randomBar.HasAny(ActionTypes.Suspend |ActionTypes.Resume |ActionTypes.Deleted)) continue;
                return (randomBar);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CodeWatcher
{
    public class FileChangeTester
    {
        readonly Random _rand = new Random();
        readonly List<string> _files;

        public FileChangeTester(string rootPath)
        {
            _files = new List<string>();
            AddPath(rootPath);
        }

        public void AddPath(string rootPath)
        {
            try
            {
                var tmp = Directory.GetFiles(rootPath, "*.cs", SearchOption.AllDirectories).Where(fl => (!fl.Contains(@"\obj\") && !fl.Contains(@"\bin\"))).ToList();
                if (tmp.Count > 0) _files.AddRange(tmp);
            }
            catch
            {
                // ignored
            }
        }
        public ActivityItem GetFileChangeTestItem(DateTime dt)
        {
            if (_files.Count == 0) return (null);
            // given number of events to generate
            // randomly get a matching file
            int ic = 1000;
            while (ic >= 0)
            {
                ic--;
                string path = _files[_rand.Next(_files.Count)];
                ActivityItem fci = ActivityItem.GetFileChangeItem(path, dt, _randomChange());
                if (fci != null) return (fci);
            }
            return (null);
        }

        public bool GenerateTestLog(string path, int length, DateTime from, DateTime to)
        {
            if (_files.Count == 0) return (false);

            List<ActivityItem> collection = new List<ActivityItem>();
            for (int i = 0; i < length; i++)
            {
                DateTime dt = FileChangeTable.GetRandomDate(from, to);
                var fci = GetFileChangeTestItem(dt);
                if (fci != null)
                    collection.Add(fci);
            }
            collection = FileChangeTable.SortAndSanitize(collection);

            FileChangeTable.Write(null, collection, SortBy.ALPHABETICAL, null, null, path);

            return (true);
        }
        ActionTypes _randomChange()
        {
            Array values = Enum.GetValues(typeof(ActionTypes));
            while (true)
            {
                ActionTypes randomBar = (ActionTypes)values.GetValue(_rand.Next(values.Length));
                if (randomBar.HasAny(ActionTypes.SUSPEND |ActionTypes.RESUME |ActionTypes.DELETED)) continue;
                return (randomBar);
            }
        }
    }
}

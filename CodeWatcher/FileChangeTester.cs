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
        public FileChangeItem GetFileChangeTestItem(DateTime dt)
        {
            if (files.Count == 0) return (null);
            // given number of events to generate
            // randomly get a matching file
            string path = files[rand.Next(files.Count)];
            FileChangeItem fci = new FileChangeItem(path, dt, _randomChange());
            return (fci);
        }

        WatcherChangeTypes _randomChange()
        {
            Array values = Enum.GetValues(typeof(WatcherChangeTypes));
            while (true)
            {
                WatcherChangeTypes randomBar = (WatcherChangeTypes)values.GetValue(rand.Next(values.Length));
                if (randomBar == WatcherChangeTypes.Deleted ||
                    randomBar == WatcherChangeTypes.All) continue;
                return (randomBar);
            }
        }
    }
}

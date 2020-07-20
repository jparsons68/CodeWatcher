using System;
using System.Linq;

namespace CodeWatcher
{
    public partial class FileChangeTable
    {
       
        DateTime startDT;
        TimeSpan spanL;
        TimeSpan spanR;
        TimeSpan ptrAdjTS_0;
        TimeSpan ptrAdjTS_1;

        private TimeSpan _min(TimeSpan spanA, TimeSpan spanB)
        {
            return (spanA < spanB ? spanA : spanB);
        }

        private TimeSpan _max(TimeSpan spanA, TimeSpan spanB)
        {
            return (spanA > spanB ? spanA : spanB);
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

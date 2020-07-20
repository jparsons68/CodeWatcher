using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;

namespace CodeWatcher
{
    public partial class ProjectListForm : IERSInterface.BaseForm
    {
        FileChangeWatcher _fcWatcher;

        public ProjectListForm()
        {
            InitializeComponent();
            ShowOK = false;
        }

        private void ProjectListForm_Load(object sender, EventArgs e)
        {

        }

        private void ProjectListForm_Shown(object sender, EventArgs e)
        {
            _rePop();
        }



        public FileChangeWatcher FileChangeWatcher
        {
            get => _fcWatcher;
            set
            {
                _fcWatcher = value;
                if (_fcWatcher != null)
                {
                    _fcWatcher.Changed += _fcWatcher_Changed;
                    _fcWatcher.Error += _fcWatcher_Error;
                    _fcWatcher.SortProjectsByChanged += _fcWatcher_SortProjectsByChanged;
                }

                _rePop();
            }
        }

        private void _fcWatcher_SortProjectsByChanged(object sender, EventArgs e)
        {
            _rePop();
        }

        List<FileChangeProject> _localProjList = new List<FileChangeProject>();
        bool amPopulating = false;
        private void _rePop()
        {
            if (amPopulating) return;

            amPopulating = true;
            var _table = (_fcWatcher != null) ? _fcWatcher.Table : null;

            if (_equal(_localProjList, _table) == false)
            {
                checkedListBox1.Items.Clear();
                _localProjList.Clear();
                foreach (var proj in _table.ProjectCollection)
                {
                    _localProjList.Add(proj);
                    checkedListBox1.Items.Add(proj, proj.Visible);
                }
            }

            int i = 0;
            foreach (var proj in _localProjList)
            {
                checkedListBox1.SetItemChecked(i, proj.Visible);
                i++;
            }

            amPopulating = false;
        }

        private bool _equal(List<FileChangeProject> myList, FileChangeTable table)
        {
            if (myList.Count == 0 && table == null) return (true);
            if (myList.Count == 0 && table.ProjectCollection.Count == 0) return (true);

            if (myList.Count != table.ProjectCollection.Count) return (false);

            for (int i = 0; i < table.ProjectCollection.Count; i++)
            {
                var proj = table.ProjectCollection[i];
                if (proj != myList[i]) return (false);
            }
            return (true);
        }

        private void _fcWatcher_Error(object sender, ErrorEventArgs e)
        {

        }

        private void _fcWatcher_Changed(object sender, DoWorkEventArgs e)
        {
            _rePop();
        }

        private void checkedListBox1_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (e.NewValue == CheckState.Checked)
                _localProjList[e.Index].Visible = true;
            else
                _localProjList[e.Index].Visible = false;

            _fcWatcher.FireEvent();
        }
    }
}

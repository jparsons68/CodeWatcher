using IERSInterface;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace CodeWatcher
{
    public partial class Form1 : Form
    {
        bool _dirtyTable;

        ErrorEventArgs _dirtyErrorEvent;
        readonly FileChangeWatcher _fcWatcher;
        bool _loadingSettings; // prevents events making save before first load!!
        readonly MainPanel _mainPanel;
        readonly ThemeController _themeController;

        public Form1()
        {
            InitializeComponent();



#if DEBUG == true


            for (int i = 0; i < 16; i++)
            {
                ActionTypes at_A = (ActionTypes)i;
                string s1 = "";
                string s2 = "";
                string s3 = "";
                s1 += (at_A.BinaryString(4) + "  :  ");
                s2 += ("ANY   :  ");
                s3 += ("HAS   :  ");
                for (int j = 0; j < 16; j++)
                {
                    ActionTypes at_B = (ActionTypes)j;
                    s1 += (at_B.BinaryString(4) + "  ");
                    s2 += (at_A.HasAny(at_B) ? "TRUE  " : "FALSE ");
                    s3 += (at_A.HasFlag(at_B) ? "TRUE  " : "FALSE ");
                }

                Console.WriteLine(s1);
                Console.WriteLine(s2);
                Console.WriteLine(s3);
                Console.WriteLine();
            }

#endif

            notifyIcon1.BalloonTipClosed += (sender, e) =>
            {
                var thisIcon = (NotifyIcon)sender;
                thisIcon.Visible = false;
                thisIcon.Dispose();
            };

            // watch this file structure
            _fcWatcher = new FileChangeWatcher { Extensions = new[] { ".cs" } };
            _fcWatcher.Changed += FolderChangeWatcher_Changed;
            _fcWatcher.Error += FolderChangeWatcher_Error;

            eventScroller1.FileChangeWatcher = _fcWatcher;

            _themeController = new ThemeController();
            _themeController.Changed += Themer_Changed;

            _mainPanel = new MainPanel(_fcWatcher, doubleBuffer1, _themeController.Theme);

            this.Icon = notifyIcon1.Icon;
            BaseForm.ApplicationIcon = notifyIcon1.Icon;

            toolStripTextBoxWithLabelIdleTime.LabelText = "Idle Time(m)";
            toolStripTextBoxWithLabelTimePerEdit.LabelText = "Per Edit Time(m)";
            toolStripTextBoxWithLabelIdleTime.Set(_fcWatcher.UserIdleMinutes);
            toolStripTextBoxWithLabelTimePerEdit.Set(_fcWatcher.PerEditMinutes);
            toolStripTextBoxWithLabelIdleTime.TextBox.EnterPressed += TextBox_EnterPressed;
            toolStripTextBoxWithLabelTimePerEdit.TextBox.EnterPressed += TextBox_EnterPressed1;
            useIdleEventsToolStripMenuItem.Checked = _fcWatcher.UseIdleEvents;
            advancedToolStripMenuItem.DropDownClosed += AdvancedToolStripMenuItem_DropDownClosed;

            foreach (var eSrt in Enum.GetValues(typeof(EditSort)))
            {
                ToolStripRadioButtonMenuItem rb = new ToolStripRadioButtonMenuItem(eSrt.ToString());
                rb.Tag = eSrt;
                dayInfoSortToolStripMenuItem.DropDownItems.Add(rb);
                rb.CheckedChanged += Rb_CheckedChanged;
            }

            Application.Idle += Application_Idle;
        }


        private void _setEditSort(EditSort editSort)
        {
            foreach (ToolStripItem dropDownItem in dayInfoSortToolStripMenuItem.DropDownItems)
            {
                ToolStripRadioButtonMenuItem rb = dropDownItem as ToolStripRadioButtonMenuItem;
                if (rb != null && ((EditSort)rb.Tag) == editSort) rb.Checked = true;
            }
        }
        private void Rb_CheckedChanged(object sender, EventArgs e)
        {
            ToolStripRadioButtonMenuItem rb = sender as ToolStripRadioButtonMenuItem;
            if (rb != null && rb.Checked) _mainPanel.SetEditSort((EditSort)rb.Tag);
        }

        private void AdvancedToolStripMenuItem_DropDownClosed(object sender, EventArgs e)
        {
            // update
            bool needsUpdate = false;
            double tmpIdle = toolStripTextBoxWithLabelIdleTime.ParseDouble();
            double tmpEdit = toolStripTextBoxWithLabelTimePerEdit.ParseDouble();
            if (!double.IsNaN(tmpIdle) && _fcWatcher.UserIdleMinutes != tmpIdle)
            {
                _fcWatcher.UserIdleMinutes = tmpIdle;
                needsUpdate = true;
            }

            if (!double.IsNaN(tmpEdit) && _fcWatcher.PerEditMinutes != tmpEdit)
            {
                _fcWatcher.PerEditMinutes = tmpEdit;
                needsUpdate = true;
            }

            if (_fcWatcher.UseIdleEvents != useIdleEventsToolStripMenuItem.Checked)
            {
                _fcWatcher.UseIdleEvents = useIdleEventsToolStripMenuItem.Checked;
                needsUpdate = true;
            }
            if (needsUpdate)
            {
                _fcWatcher.UpdateActivity();
                doubleBuffer1.Refresh();
            }
        }

        private void TextBox_EnterPressed1(object sender, KeyPressEventArgs e)
        {
            advancedToolStripMenuItem.HideDropDown();
        }

        private void TextBox_EnterPressed(object sender, KeyPressEventArgs e)
        {
            advancedToolStripMenuItem.HideDropDown();
        }

        private void useIdleEventsToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            AdvancedToolStripMenuItem_DropDownClosed(null, null);
        }

        private void Themer_Changed(object sender, EventArgs e)
        {
            eventScroller1.Theme = _themeController.Theme;
            _themeController.Theme.Impose(this);
            doubleBuffer1.Invalidate();
        }

        private void Application_Idle(object sender, EventArgs e)
        {
            if (_dirtyTable)
            {
                _dirtyTable = false;
                doubleBuffer1.Invalidate();
            }

            if (_dirtyErrorEvent != null)
            {
                textBoxWATCHOUTPUT.Text = @"ERROR:";
                textBoxWATCHOUTPUT.Text = _dirtyErrorEvent + Environment.NewLine;
                _dirtyErrorEvent = null;
            }
        }


        private delegate void AppendTextCallback(object sender, DoWorkEventArgs e);
        private void FolderChangeWatcher_Changed(object sender, DoWorkEventArgs e)
        {
            if (e.Argument is ActivityItem fci)
            {
                if (textBoxWATCHOUTPUT.InvokeRequired)
                {
                    AppendTextCallback d = FolderChangeWatcher_Changed;
                    textBoxWATCHOUTPUT.BeginInvoke(d, sender, e);
                }
                else
                    textBoxWATCHOUTPUT.AppendText(fci + Environment.NewLine);

            }
            _dirtyTable = true;
        }

        private void FolderChangeWatcher_Error(object sender, ErrorEventArgs e)
        {
            _dirtyErrorEvent = e;
        }


        private void _saveSettings()
        {
            if (_loadingSettings) return;
            Properties.Settings.Default.LogFile = fileFieldAndBrowserLOG.FileName;
            Properties.Settings.Default.Path = fileFieldAndBrowserWATCH.FileName;
            Properties.Settings.Default.Location = this.Location;
            Properties.Settings.Default.Size = this.Size;
            Properties.Settings.Default.EsTimePeriod = (int)eventScroller1.TimePeriod;
            Properties.Settings.Default.ShowPaths = showPathsToolStripMenuItem.Checked;
            Properties.Settings.Default.ShowTools = showToolbarToolStripMenuItem.Checked;
            Properties.Settings.Default.UserIdleMinutes = _fcWatcher.UserIdleMinutes;
            Properties.Settings.Default.PerEditMinutes = _fcWatcher.PerEditMinutes;
            Properties.Settings.Default.UseIdleEvents = _fcWatcher.UseIdleEvents;
            Properties.Settings.Default.Save();
            _themeController.SaveSettings();
            _mainPanel.SaveSettings();

            _fcWatcher?.AutoWrite();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            _loadingSettings = true;
            fileFieldAndBrowserLOG.FileName = Properties.Settings.Default.LogFile;
            fileFieldAndBrowserWATCH.FileName = Properties.Settings.Default.Path;
            this.Location = Properties.Settings.Default.Location;
            this.Size = Properties.Settings.Default.Size;

            eventScroller1.TimePeriod = (TimePeriod)Properties.Settings.Default.EsTimePeriod;

            showPathsToolStripMenuItem.Checked = Properties.Settings.Default.ShowPaths;
            showToolbarToolStripMenuItem.Checked = Properties.Settings.Default.ShowTools;
            _fcWatcher.UserIdleMinutes = Properties.Settings.Default.UserIdleMinutes;
            toolStripTextBoxWithLabelIdleTime.Set(_fcWatcher.UserIdleMinutes);
            _fcWatcher.PerEditMinutes = Properties.Settings.Default.PerEditMinutes;
            toolStripTextBoxWithLabelTimePerEdit.Set(_fcWatcher.PerEditMinutes);

            _fcWatcher.UseIdleEvents = Properties.Settings.Default.UseIdleEvents;
            useIdleEventsToolStripMenuItem.Checked = _fcWatcher.UseIdleEvents;

            showIdleRedLineToolStripMenuItem.Checked = Properties.Settings.Default.ShowIdleLine;

            _themeController.LoadSettings();
            _themeController.Theme.Impose(this);
            _mainPanel.LoadSettings();
            _loadingSettings = false;

            _setEditSort(Properties.Settings.Default.EditSort);
            startCollectionToolStripMenuItem_Click(this, null);

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing) e.Cancel = true;
            // to hide from taskbar
            hideToolStripMenuItem_Click(sender, null);
            _fcWatcher.Write();
        }

        private const int WM_SYSCOMMAND = 0x0112;
        private const int SC_MINIMIZE = 0xf020;
        // no minimize
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_SYSCOMMAND)
            {
                if (m.WParam.ToInt32() == SC_MINIMIZE)
                {
                    m.Result = IntPtr.Zero;
                    hideToolStripMenuItem_Click(this, null);
                    return;
                }
            }
            base.WndProc(ref m);
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
        }
        private void Form1_Shown(object sender, EventArgs e)
        {
            // hides on startup, no big deal
            _themeController.Theme.Impose(this);
#if DEBUG == false
            this.Hide();
#endif
        }

        private void showToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Show();
            this.Activate();
        }

        private void hideToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _saveSettings();
            this.Hide();
        }

        private void exitToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            _mainPanel.IncrementDayStart(-1);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            _mainPanel.IncrementDayStart(1);
        }

        private void buttonINC_Click(object sender, EventArgs e)
        {
            _mainPanel.IncrementRowHeight(1);
        }
        private void buttonDEC_Click(object sender, EventArgs e)
        {
            _mainPanel.IncrementRowHeight(-1);
        }

        private void lastWeekToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _mainPanel.ShowPresentToLast(TimePeriod.ONEWEEK);
        }

        private void lastMonthToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _mainPanel.ShowPresentToLast(TimePeriod.ONEMONTH);
        }

        private void last3MonthsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _mainPanel.ShowPresentToLast(TimePeriod.THREEMONTHS);
        }

        private void last6MonthsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _mainPanel.ShowPresentToLast(TimePeriod.SIXMONTHS);
        }

        private void lastYearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _mainPanel.ShowPresentToLast(TimePeriod.ONEYEAR);
        }

        private void allTimeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _mainPanel.ShowPresentToLast(TimePeriod.ALLTIME);
        }


        private void testToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _fcWatcher.StartBackgroundTest(10000);
        }

        private void stopTestToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _fcWatcher.StopBackgroundTest();
        }

        private void clearRecordToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dR = MessageBox.Show(@"Clear?", @"This will PERMANENTLY clear the record. Are you sure?",
                MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
            if (dR == DialogResult.Yes)
            {
                _fcWatcher?.ClearAll();
                _mainPanel.ScrollToTop();
                _mainPanel.ShowPresentToLast(TimePeriod.ONEWEEK);
            }
        }

        private void pauseCollectionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_fcWatcher != null)
            {
                _fcWatcher.Stop();
                textBoxWATCHOUTPUT.AppendText("Paused Collection\r\n");
            }
        }

        private void startCollectionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_fcWatcher != null)
            {

                _fcWatcher.LogPath = fileFieldAndBrowserLOG.FileName;
                _fcWatcher.WatchPath = fileFieldAndBrowserWATCH.FileName;
                _fcWatcher.Start();
                textBoxWATCHOUTPUT.AppendText("Start Collection\r\n");
                _dirtyTable = true;
                eventScroller1.UpdateAll();
                _saveSettings();
            }
        }
        private void restartCollectionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_fcWatcher != null)
            {
                textBoxWATCHOUTPUT.AppendText("Restart Collection\r\n");
                _fcWatcher.Stop();
                _fcWatcher.LogPath = fileFieldAndBrowserLOG.FileName;
                _fcWatcher.WatchPath = fileFieldAndBrowserWATCH.FileName;
                _fcWatcher.Start();
                _dirtyTable = true;
                eventScroller1.UpdateAll();
                _saveSettings();
            }
        }
        private void editLogFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Create a new process info structure.
            pauseCollectionToolStripMenuItem_Click(sender, e);
            ProcessStartInfo pInfo = new ProcessStartInfo { FileName = fileFieldAndBrowserLOG.FileName };
            //Set the file name member of the process info structure.
            //Start the process.
            Process p = Process.Start(pInfo);
            //Wait for the window to finish loading.
            if (p != null)
            {
                p.WaitForInputIdle();
                //Wait for the process to end.
                p.WaitForExit();
            }

            startCollectionToolStripMenuItem_Click(sender, e);
            MessageBox.Show(@"Code continuing...");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            textBoxWATCHOUTPUT.Clear();
        }
        private void button6_Click(object sender, EventArgs e)
        {
            _mainPanel.IncrementRow(-1);
        }
        private void button7_Click(object sender, EventArgs e)
        {
            _mainPanel.IncrementRow(1);
        }




        private void generateTestLogFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                FileChangeTester myTester = new FileChangeTester(_fcWatcher.WatchPath);
                string testLogPath = Path.Combine(Path.GetDirectoryName(_fcWatcher.LogPath) ?? string.Empty, "testLog.txt");
                myTester.GenerateTestLog(testLogPath, 1000, DateTime.Now.AddYears(-1), DateTime.Now);
            }
            catch
            {
                // ignored
            }
        }

        private void fileFieldAndBrowserWATCH_Changed(object sender, EventArgs e)
        {
            if (_loadingSettings) return;
            restartCollectionToolStripMenuItem_Click(sender, e);
        }
        private void fileFieldAndBrowserLOG_Changed(object sender, EventArgs e)
        {
            if (_loadingSettings) return;
            restartCollectionToolStripMenuItem_Click(sender, e);
        }



        private void lightToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _themeController.SetLightTheme(Color.MidnightBlue, Color.Orange, Color.Lime);
        }

        private void darkToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _themeController.SetDarkTheme(Color.ForestGreen, Color.DodgerBlue, Color.Lime);
        }

        private void specialToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _themeController.SetDarkTheme(Color.Orange, Color.Maroon, Color.Lime);
        }

        private void darkRandomToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _themeController.RandomDarkTheme();
        }

        private void lightRandomToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _themeController.RandomLightTheme();
        }


        ProjectListForm _projectListForm;
        private void projectListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_projectListForm == null)
            {
                _projectListForm = new ProjectListForm { FileChangeWatcher = _fcWatcher };
            }

            _projectListForm.Show(this);
        }



        private void copyWorkSummaryToClipboardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _mainPanel.CopyWorkSummaryToClipboard();
        }

        private void clearEditsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _mainPanel.RemoveTimeSelectionEdits();
        }

        private void timeBoxToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            contextMenuStrip1_Opening(null, null);
        }
        private void projectsToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            contextMenuStrip1_Opening(null, null);
        }
        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            // over the project listing or main area
            //bool pcArea = mainPanel.AnyAreasPointedAt(MainArea.PROJECT_COLUMN);
            bool projectsSelected = _fcWatcher.Table.CountProjects(DataState.True, DataState.True) > 0;

            // main menus
            clearEditsInSelectedTimeBoxesToolStripMenuItem.Enabled = _fcWatcher.Table.SelectionState;
            projectColorToolStripMenuItem.Enabled = projectsSelected;
            projectRandomColorToolStripMenuItem.Enabled = projectsSelected;
            projectSameRandomColorToolStripMenuItem.Enabled = projectsSelected;

            // context menus
            colorToolStripMenuItem.Enabled = projectsSelected;
            randomColorToolStripMenuItem.Enabled = projectsSelected;
            sameRandomColorToolStripMenuItem.Enabled = projectsSelected;
        }

        private void showIdleRedLineToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            _mainPanel.ShowIdleLine(showIdleRedLineToolStripMenuItem.Checked);
        }
        private void compensatedDayScalingToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            _mainPanel.ShowCompensatedScale(compensatedDayScalingToolStripMenuItem.Checked);
        }
        private void showVerticalCursorToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            _mainPanel.ShowVerticalCursor(showVerticalCursorToolStripMenuItem.Checked);
        }

        private void showToolbarToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            flowLayoutPanel1.Visible = showToolbarToolStripMenuItem.Checked;
        }

        private void showPathsToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            panelPATHS.Visible = showPathsToolStripMenuItem.Checked;
        }

        private void colorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _mainPanel.EditColor();
        }

        private void randomColorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _mainPanel.RandomColors();
        }

        private void projectColorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _mainPanel.EditColor();
        }

        private void projectRandomColorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _mainPanel.RandomColors();
        }

        private void projectSameRandomColorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _mainPanel.SameRandomColor();
        }

        private void sameRandomColorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _mainPanel.SameRandomColor();
        }

        private void clearTimeSelectionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _fcWatcher.Table.ClearTimeSelection();
            doubleBuffer1.Refresh();
        }


        protected override bool ProcessDialogKey(Keys keyData)
        {
            if (Form.ModifierKeys == Keys.None && keyData == Keys.Escape)
            {
                _fcWatcher.Table.ClearTimeSelection();
                doubleBuffer1.Refresh();
                return true;
            }

            if (Form.ModifierKeys == Keys.None && keyData == Keys.Oemtilde)
            {
                _mainPanel.StepRowHeights();
                return (true);
            }

            return base.ProcessDialogKey(keyData);
        }

    }
}

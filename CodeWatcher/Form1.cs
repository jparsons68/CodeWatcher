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
        FileChangeWatcher _fcWatcher;
        bool _loadingSettings = false; // prevents events making save before first load!!
        MainPanel mainPanel;
        ThemeController themeController;

        public Form1()
        {
            InitializeComponent();

#if DEBUG == true
            TimeBox.TestSubtract(); 
#endif

            notifyIcon1.BalloonTipClosed += (sender, e) =>
            {
                var thisIcon = (NotifyIcon)sender;
                thisIcon.Visible = false;
                thisIcon.Dispose();
            };

            // watch this file structure
            _fcWatcher = new FileChangeWatcher();
            _fcWatcher.Extensions = new string[] { ".cs", ".sln", ".proj" };
            _fcWatcher.Changed += FolderChangeWatcher_Changed;
            _fcWatcher.Error += FolderChangeWatcher_Error;
            _fcWatcher.AutoSaveMinutesInterval = 3;

            eventScroller1.FileChangeWatcher = _fcWatcher;

            themeController = new ThemeController();
            themeController.Changed += Themer_Changed;

            mainPanel = new MainPanel(_fcWatcher, doubleBuffer1, themeController.Theme);

            this.Icon = notifyIcon1.Icon;
            BaseForm.ApplicationIcon = notifyIcon1.Icon;


            Application.Idle += Application_Idle;
        }

        private void Themer_Changed(object sender, EventArgs e)
        {
            eventScroller1.Theme = themeController.Theme;
            themeController.Theme.Impose(this);
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
                textBoxWATCHOUTPUT.Text = "ERROR:";
                textBoxWATCHOUTPUT.Text = _dirtyErrorEvent.ToString() + Environment.NewLine;
                _dirtyErrorEvent = null;
            }
        }


        private delegate void AppendTextCallback(object sender, DoWorkEventArgs e);
        private void FolderChangeWatcher_Changed(object sender, DoWorkEventArgs e)
        {
            FileChangeItem fci = e.Argument as FileChangeItem;
            if (fci != null)
            {
                if (textBoxWATCHOUTPUT.InvokeRequired)
                {
                    AppendTextCallback d = new AppendTextCallback(FolderChangeWatcher_Changed);
                    textBoxWATCHOUTPUT.BeginInvoke(d, new object[] { sender, e });
                }
                else
                    textBoxWATCHOUTPUT.AppendText(fci.ToString() + Environment.NewLine);

            }
            _dirtyTable = true;
        }

        private void FolderChangeWatcher_Error(object sender, ErrorEventArgs e)
        {
            _dirtyErrorEvent = e;
        }

        private void dateTimePicker1_ValueChanged(object sender, EventArgs e)
        {
            _dirtyTable = true;
            Application_Idle(this, null);
        }

        private void dateTimePicker2_ValueChanged(object sender, EventArgs e)
        {
            _dirtyTable = true;
            Application_Idle(this, null);
        }


        private void _saveSettings()
        {
            if (_loadingSettings) return;
            Properties.Settings.Default.LogFile = fileFieldAndBrowserLOG.FileName;
            Properties.Settings.Default.Path = fileFieldAndBrowserWATCH.FileName;
            Properties.Settings.Default.Location = this.Location;
            Properties.Settings.Default.Size = this.Size;
            Properties.Settings.Default.EsTimePeriod = (int)eventScroller1.TimePeriod;
            Properties.Settings.Default.Save();
            themeController.SaveSettings();
            mainPanel.SaveSettings();

            if (_fcWatcher != null)
                _fcWatcher.AutoWrite();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            _loadingSettings = true;
            fileFieldAndBrowserLOG.FileName = Properties.Settings.Default.LogFile;
            fileFieldAndBrowserWATCH.FileName = Properties.Settings.Default.Path;
            showInfoColumnToolStripMenuItem.Checked = Properties.Settings.Default.ShowInfoColumn;
            this.Location = Properties.Settings.Default.Location;
            this.Size = Properties.Settings.Default.Size;
            themeController.LoadSettings();
            TIMEPERIOD tP = (TIMEPERIOD)Properties.Settings.Default.EsTimePeriod;
            eventScroller1.TimePeriod = tP;
            mainPanel.LoadSettings();
            _loadingSettings = false;

            startCollectionToolStripMenuItem_Click(this, null);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing) e.Cancel = true;
            // to hide from taskbar
            hideToolStripMenuItem_Click(sender, null);
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

#if DEBUG == false
            this.Hide();
#endif
        }

        private void showToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Show();
            this.Activate();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
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
            mainPanel.IncrementDayStart(-1);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            mainPanel.IncrementDayStart(1);
        }

        private void buttonINC_Click(object sender, EventArgs e)
        {
            mainPanel.IncrementRowHeight(1);
        }
        private void buttonDEC_Click(object sender, EventArgs e)
        {
            mainPanel.IncrementRowHeight(-1);
        }

        private void lastWeekToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mainPanel.ShowPresentToLast(TIMEPERIOD.ONEWEEK);
        }

        private void lastMonthToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mainPanel.ShowPresentToLast(TIMEPERIOD.ONEMONTH);
        }

        private void last3MonthsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mainPanel.ShowPresentToLast(TIMEPERIOD.THREEMONTHS);
        }

        private void last6MonthsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mainPanel.ShowPresentToLast(TIMEPERIOD.SIXMONTHS);
        }

        private void lastYearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mainPanel.ShowPresentToLast(TIMEPERIOD.ONEYEAR);
        }

        private void allTimeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mainPanel.ShowPresentToLast(TIMEPERIOD.ALLTIME);
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
            var dR = MessageBox.Show("Clear?", "This will PERMANENTLY clear the record. Are you sure?",
                MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
            if (dR == DialogResult.Yes)
            {
                if (_fcWatcher != null)
                    _fcWatcher.ClearAll();
                mainPanel.ScrollToTop();
                mainPanel.ShowPresentToLast(TIMEPERIOD.ONEWEEK);
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
            ProcessStartInfo pInfo = new ProcessStartInfo();
            //Set the file name member of the process info structure.
            pInfo.FileName = fileFieldAndBrowserLOG.FileName;
            //Start the process.
            Process p = Process.Start(pInfo);
            //Wait for the window to finish loading.
            p.WaitForInputIdle();
            //Wait for the process to end.
            p.WaitForExit();
            startCollectionToolStripMenuItem_Click(sender, e);
            MessageBox.Show("Code continuing...");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            textBoxWATCHOUTPUT.Clear();
        }
        private void button6_Click(object sender, EventArgs e)
        {
            mainPanel.IncrementRow(-1);
        }
        private void button7_Click(object sender, EventArgs e)
        {
            mainPanel.IncrementRow(1);
        }




        private void generateTestLogFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                FileChangeTester myTester = new FileChangeTester(_fcWatcher.WatchPath);
                string testLogPath = Path.Combine(Path.GetDirectoryName(_fcWatcher.LogPath), "testLog.txt");
                myTester.GenerateTestLog(testLogPath, 1000, DateTime.Now.AddYears(-1), DateTime.Now);
            }
            catch
            {

            }
        }

        private void fileFieldAndBrowserWATCH_Changed(object sender, EventArgs e)
        {
            restartCollectionToolStripMenuItem_Click(sender, e);
        }
        private void fileFieldAndBrowserLOG_Changed(object sender, EventArgs e)
        {
            restartCollectionToolStripMenuItem_Click(sender, e);
        }



        private void lightToolStripMenuItem_Click(object sender, EventArgs e)
        {
            themeController.SetLightTheme(Color.MidnightBlue, Color.Orange, Color.Lime);
        }

        private void darkToolStripMenuItem_Click(object sender, EventArgs e)
        {
            themeController.SetDarkTheme(Color.ForestGreen, Color.DodgerBlue, Color.Lime);
        }

        private void specialToolStripMenuItem_Click(object sender, EventArgs e)
        {
            themeController.SetDarkTheme(Color.Orange, Color.Maroon, Color.Lime);
        }

        private void darkRandomToolStripMenuItem_Click(object sender, EventArgs e)
        {
            themeController.RandomDarkTheme();
        }

        private void lightRandomToolStripMenuItem_Click(object sender, EventArgs e)
        {
            themeController.RandomLightTheme();
        }


        ProjectListForm projectListForm;
        private void projectListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (projectListForm == null)
            {
                projectListForm = new ProjectListForm();
                projectListForm.FileChangeWatcher = _fcWatcher;
            }

            projectListForm.Show(this);
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mainPanel.TimeBoxDeleteSelected();
        }

        private void showInfoColumnToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            mainPanel.ShowInfoColumn(showInfoColumnToolStripMenuItem.Checked);
        }

        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mainPanel.TimeBoxSelectAll();
        }

        private void copyWorkSummaryToClipboardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mainPanel.CopyWorkSummaryToClipboard();
        }
    }
}

using ChartLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace CodeWatcher
{
    public partial class Form1 : Form
    {
        double REDZONE = 0.8;
        double ORANGEZONE = 0.6;
        double YELLOWZONE = 0.4;
        double GREENZONE = 0.2;
        int ROWHEIGHTDEF = 25;
        int ROWHEIGHTMAX = 100;
        int ROWHEIGHTINC = 4;

        bool _dirtyTable;

        ErrorEventArgs _dirtyErrorEvent;
        FileChangeWatcher _fcWatcher;
        int _iOffset = 0, _maxIOffset = 100;
        int _jOffset = 0, _maxJOffset = 10;
        int _scrollDays;
        int _rowHeight;
        TooltipContainer ttC;
        public Form1()
        {
            InitializeComponent();

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

            doubleBuffer1.MouseWheel += DoubleBuffer1_MouseWheel;

            this.Icon = notifyIcon1.Icon;
            _rowHeight = ROWHEIGHTDEF;

            dateTimePicker1.Value = dateTimePicker1.MinDate;
            dateTimePicker2.Value = dateTimePicker2.MaxDate;

            ttC = new TooltipContainer(doubleBuffer1);

            checkBox1_CheckedChanged(null, null);

            Application.Idle += Application_Idle;
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


        private void DoubleBuffer1_MouseWheel(object sender, MouseEventArgs e)
        {
            int click = e.Delta / 120;

            if (ChartViewer.IsModifierHeld(Keys.Control))
            {
                _incRowHeight(click * 3);
                doubleBuffer1.Refresh();
            }
            else if (ChartViewer.IsModifierHeld(Keys.Shift))
            {
                _incIoffset(_scrollDays * click);
                doubleBuffer1.Refresh();
            }
            else
            {
                _incJoffset(-click);
                doubleBuffer1.Refresh();
            }
        }


        int BORDER = 10;
        int BORDERHEADER = 60;
        int PROJCOLWIDTH = 100;
        int MINBOXSPACE = 4;
        int MAXBOXSPACE = 20;
        int MINBOXH = 4;

        private void doubleBuffer1_PaintEvent(object sender, PaintEventArgs e)
        {
            ttC.Clear();

            var g = e.Graphics;
            FileChangeTable _table = null;
            if (_fcWatcher == null ||
            _fcWatcher.Table == null ||
            (_table = _fcWatcher.Table) == null ||
           _table.ItemCount == 0)
            {
                g.DrawString("NO DATA!", Font, Brushes.Red, BORDER, BORDERHEADER);
                return;
            }


            // day range
            DateTime dt0 = dateTimePicker1.Value.Date;
            DateTime dt1 = checkBox1.Checked ? DateTime.Now.Date : dateTimePicker2.Value.Date;
            int dayCount = (dt1 - dt0).Days + 1;
            int firstDayIdx = _table.GetDayIndex(dt0);
            int fH = Font.Height;
            int playWidth = (doubleBuffer1.Width - BORDER - BORDER - PROJCOLWIDTH);
            int playHeight = (doubleBuffer1.Height - BORDERHEADER);
            int boxSpace = playWidth / dayCount;
            if (boxSpace < MINBOXSPACE) boxSpace = MINBOXSPACE;
            if (boxSpace > MAXBOXSPACE) boxSpace = MAXBOXSPACE;
            bool showDayOfWeek = (boxSpace > fH);
            bool showDayOfMonth = (boxSpace > fH);
            bool showMondays = (boxSpace * 7 > fH * 2);
            int boxW = (int)(boxSpace * 0.8);
            int boxOff = (boxSpace - boxW) / 2;
            int boxH = _rowHeight - 2;

            // projects with activity in shown time range
            List<FileChangeProject> projWithActivity = _table.GetProjectsWithActivity(dt0, dt1);
            int peakOpsInDay = _table.GetPeakOpsInDay(dt0, dt1);

            _scrollDays = 50 / boxSpace;
            _maxIOffset = dayCount - (int)(0.6 * playWidth / boxSpace);
            if (_maxIOffset < 0) _maxIOffset = 0;
            _maxJOffset = projWithActivity.Count - playHeight / _rowHeight;
            if (_maxJOffset < 0) _maxJOffset = 0;
            Brush br;

            Font smallFont = new Font(Font.FontFamily, Font.SizeInPoints * 0.8f, FontStyle.Italic);

            // draw
            int x, y;
            List<int> verts = new List<int>();
            // row per project
            y = BORDERHEADER;
            int ic = 0;
            for (int j = 0; j < projWithActivity.Count; j++)
            {
                int idxJ = j + _jOffset;
                var proj = (idxJ >= 0 && idxJ < projWithActivity.Count) ? projWithActivity[idxJ] : null;

                if (y > doubleBuffer1.Height) break;

                int count = proj != null ? proj.CountChanges(dt0, dt1) : 0;
                if (count > 0)
                {
                    // don't show ones with no activity in the shown range
                    ic++;
                    x = BORDER;
                    Rectangle rectBG = new Rectangle(0, y, doubleBuffer1.Width, _rowHeight);
                    g.FillRectangle(idxJ % 2 == 1 ? Brushes.Gainsboro : Brushes.White, rectBG);

                    var py = y + (_rowHeight - Font.Height) / 2;
                    g.DrawString(proj.Name, Font, Brushes.Black, x, py);
                    //g.DrawString(count.ToString(), Font, Brushes.Blue, x, py + fH);
                    //g.DrawString(proj.Path, smallFont, Brushes.Blue, x, py + fH);

                    Rectangle pRect = rectBG;
                    pRect.Width = PROJCOLWIDTH;
                    ttC.Add(pRect, proj.Path + " : " + count + " changes", proj);

                    x += PROJCOLWIDTH;
                    int icDay = -1;
                    int year = -999;
                    for (int i = 0; i < dayCount; i++)
                    {
                        int idx = i + _iOffset + firstDayIdx;
                        var projDay = proj.GetDay(idx);

                        if (projDay != null)
                        {
                            icDay++;
                            if (ic == 1)
                            {
                                int tY = y - fH;
                                if (showDayOfWeek) { _drawCenteredString(g, projDay.DateTime.ToString("ddd").Substring(0, 1), Font, Brushes.Black, x + boxW / 2, tY); tY -= fH; }
                                if (showDayOfMonth) { _drawCenteredString(g, projDay.DateTime.Day.ToString(), Font, Brushes.Black, x + boxW / 2, tY); tY -= fH; }
                                else if (showMondays)
                                {
                                    if (projDay.DateTime.DayOfWeek == DayOfWeek.Monday)
                                        _drawCenteredString(g, projDay.DateTime.Day.ToString(), Font, Brushes.Black, x + boxW / 2, tY);
                                    tY -= fH;
                                }
                                if (icDay == 0 || projDay.DateTime.Day == 1) //MONTH
                                {
                                    g.FillRectangle(projDay.DateTime.Month % 2 == 0 ? Brushes.Khaki : Brushes.AliceBlue, x, tY, doubleBuffer1.Width - x, fH);
                                    g.DrawString(projDay.DateTime.ToString("MMMM"), Font, Brushes.Black, x, tY); tY -= fH;
                                }

                                if (year != projDay.DateTime.Year) //YEAR
                                {
                                    year = projDay.DateTime.Year;
                                    g.DrawString(projDay.DateTime.Year.ToString(), Font, Brushes.Black, x, tY); tY -= fH;
                                }
                                // START OF WEEK
                                if (projDay.DateTime.DayOfWeek == DayOfWeek.Monday)
                                    verts.Add(x - boxOff);

                            }

                            if (projDay.Count > 0)
                            {
                                // blue, green, yellow, orange, red
                                //        5+      10+    15+    20+
                                double activity = ((double)projDay.Count) / peakOpsInDay;
                                int bH = (int)(boxH * activity);
                                if (bH < MINBOXH) bH = MINBOXH;
                                Rectangle rect = new Rectangle(x, y + _rowHeight - bH, boxW, bH);
                                if (activity > REDZONE) br = Brushes.Red;
                                else if (activity > ORANGEZONE) br = Brushes.Orange;
                                else if (activity > YELLOWZONE) br = Brushes.Gold;
                                else if (activity > GREENZONE) br = Brushes.ForestGreen;
                                else br = Brushes.CadetBlue;
                                g.FillRectangle(br, rect);
                            }
                        }

                        x += boxSpace;
                    }
                    g.DrawRectangle(Pens.LightGray, rectBG);


                    y += _rowHeight;
                }
            }


            foreach (var vx in verts)
            {
                g.DrawLine(Pens.Gray, vx, BORDERHEADER, vx, doubleBuffer1.Height);
            }


        }

        private void _drawCenteredString(Graphics g, string txt, Font font, Brush br, int x, int y)
        {
            var siz = g.MeasureString(txt, font);
            g.DrawString(txt, font, br, x - siz.Width / 2, y);
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


        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            dateTimePicker2.Enabled = !checkBox1.Checked;
            _dirtyTable = true;
            Application_Idle(this, null);
        }
        private void _saveSettings()
        {
            Properties.Settings.Default.LogFile = textBoxLOGFILE.Text;
            Properties.Settings.Default.Path = textBoxPATH.Text;
            Properties.Settings.Default.MinDateTime = dateTimePicker1.Value;
            Properties.Settings.Default.MaxDateTime = dateTimePicker2.Value;
            Properties.Settings.Default.ToPresent = checkBox1.Checked;
            Properties.Settings.Default.Ioffset = _iOffset;
            Properties.Settings.Default.Joffset = _jOffset;
            Properties.Settings.Default.RowHeight = _rowHeight;
            Properties.Settings.Default.Location = this.Location;
            Properties.Settings.Default.Size = this.Size;
            Properties.Settings.Default.EventScrollPeriod = (int)eventScroller1.TimePeriod;
            Properties.Settings.Default.Save();

            if (_fcWatcher != null)
                _fcWatcher.AutoWrite();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            textBoxLOGFILE.Text = Properties.Settings.Default.LogFile;
            textBoxPATH.Text = Properties.Settings.Default.Path;
            dateTimePicker1.Value = Properties.Settings.Default.MinDateTime;
            dateTimePicker2.Value = Properties.Settings.Default.MaxDateTime;
            checkBox1.Checked = Properties.Settings.Default.ToPresent;
            _iOffset = Properties.Settings.Default.Ioffset;
            _jOffset = Properties.Settings.Default.Joffset;
            _rowHeight = Properties.Settings.Default.RowHeight;
            this.Location = Properties.Settings.Default.Location;
            this.Size = Properties.Settings.Default.Size;
            TIMEPERIOD tP = Properties.Settings.Default.EventScrollPeriod != -1 ?
                (TIMEPERIOD)Properties.Settings.Default.EventScrollPeriod : TIMEPERIOD.ONEHOUR;
            eventScroller1.TimePeriod = tP;

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
        { // hides on startup, no big deal
            this.Hide();
        }

        private void showToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Show();
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
            _incIoffset(-_scrollDays);
            doubleBuffer1.Refresh();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            _incIoffset(_scrollDays);
            doubleBuffer1.Refresh();
        }

        private void buttonINC_Click(object sender, EventArgs e)
        {
            _incRowHeight(ROWHEIGHTINC);
            doubleBuffer1.Refresh();
        }
        private void buttonDEC_Click(object sender, EventArgs e)
        {
            _incRowHeight(-ROWHEIGHTINC);
            doubleBuffer1.Refresh();
        }

        private void lastWeekToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _jOffset = 0;
            _iOffset = 0;
            checkBox1.Checked = true;
            dateTimePicker1.Value = DateTime.Now.AddDays(-7);
        }

        private void lastMonthToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _jOffset = 0;
            _iOffset = 0;
            checkBox1.Checked = true;
            dateTimePicker1.Value = DateTime.Now.AddMonths(-1);
        }

        private void last3MonthsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _jOffset = 0;
            _iOffset = 0;
            checkBox1.Checked = true;
            dateTimePicker1.Value = DateTime.Now.AddMonths(-3);
        }

        private void last6MonthsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _jOffset = 0;
            _iOffset = 0;
            checkBox1.Checked = true;
            dateTimePicker1.Value = DateTime.Now.AddMonths(-6);
        }

        private void lastYearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _jOffset = 0;
            _iOffset = 0;
            checkBox1.Checked = true;
            dateTimePicker1.Value = DateTime.Now.AddYears(-1);
        }

        private void allTimeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _jOffset = 0;
            _iOffset = 0;
            checkBox1.Checked = true;
            dateTimePicker1.Value = dateTimePicker1.MinDate;
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
                _iOffset = 0;
                _jOffset = 0;
                lastWeekToolStripMenuItem_Click(sender, e);
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
            _fcWatcher.LogPath = Path.Combine(textBoxPATH.Text, "changeLog.txt");
            _fcWatcher.WatchPath = textBoxPATH.Text;
            _fcWatcher.Start();
            textBoxWATCHOUTPUT.AppendText("Start Collection\r\n");
            _dirtyTable = true;
            eventScroller1.UpdateAll();
            _saveSettings();
        }

        private void editLogFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Create a new process info structure.
            pauseCollectionToolStripMenuItem_Click(sender, e);
            ProcessStartInfo pInfo = new ProcessStartInfo();
            //Set the file name member of the process info structure.
            pInfo.FileName = textBoxLOGFILE.Text;
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
            _incJoffset(-1);
            doubleBuffer1.Refresh();
        }
        private void button7_Click(object sender, EventArgs e)
        {
            _incJoffset(1);
            doubleBuffer1.Refresh();
        }

        void _incIoffset(int inc)
        {
            _iOffset += inc;
            if (_iOffset < 0) _iOffset = 0;
            if (_iOffset > _maxIOffset) _iOffset = _maxIOffset;
        }

        void _incJoffset(int inc)
        {
            _jOffset += inc;
            if (_jOffset < 0) _jOffset = 0;
            if (_jOffset > _maxJOffset) _jOffset = _maxJOffset;
        }

        void _incRowHeight(int inc)
        {
            _rowHeight += inc;
            if (_rowHeight > ROWHEIGHTMAX) _rowHeight = ROWHEIGHTMAX;
            if (_rowHeight < Font.Height) _rowHeight = Font.Height;
        }
    }
}

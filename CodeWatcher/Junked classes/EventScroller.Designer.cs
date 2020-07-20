namespace CodeWatcher
{
    partial class EventScroller
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.doubleBuffer1 = new ChartLib.DoubleBuffer();
            this.comboBoxEnumControl1 = new IERSInterface.ComboBoxEnumControl();
            this.SuspendLayout();
            // 
            // doubleBuffer1
            // 
            this.doubleBuffer1.BackColor = System.Drawing.Color.White;
            this.doubleBuffer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.doubleBuffer1.Freeze = false;
            this.doubleBuffer1.Location = new System.Drawing.Point(0, 0);
            this.doubleBuffer1.Name = "doubleBuffer1";
            this.doubleBuffer1.Size = new System.Drawing.Size(316, 672);
            this.doubleBuffer1.TabIndex = 0;
            this.doubleBuffer1.Text = "doubleBuffer1";
            this.doubleBuffer1.PaintEvent += new System.Windows.Forms.PaintEventHandler(this.doubleBuffer1_PaintEvent);
            // 
            // comboBoxEnumControl1
            // 
            this.comboBoxEnumControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxEnumControl1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.comboBoxEnumControl1.DefaultEnum = null;
            this.comboBoxEnumControl1.EnumType = null;
            this.comboBoxEnumControl1.ImageList = null;
            this.comboBoxEnumControl1.Location = new System.Drawing.Point(155, -1);
            this.comboBoxEnumControl1.Margin = new System.Windows.Forms.Padding(2);
            this.comboBoxEnumControl1.Name = "comboBoxEnumControl1";
            this.comboBoxEnumControl1.Padding = new System.Windows.Forms.Padding(0, 2, 0, 0);
            this.comboBoxEnumControl1.SelectedItem = null;
            this.comboBoxEnumControl1.ShowTextInItem = true;
            this.comboBoxEnumControl1.Size = new System.Drawing.Size(160, 24);
            this.comboBoxEnumControl1.TabIndex = 2;
            this.comboBoxEnumControl1.Title = "";
            // 
            // EventScroller
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.comboBoxEnumControl1);
            this.Controls.Add(this.doubleBuffer1);
            this.Name = "EventScroller";
            this.Size = new System.Drawing.Size(316, 672);
            this.ResumeLayout(false);

        }

        #endregion

        private ChartLib.DoubleBuffer doubleBuffer1;
        private IERSInterface.ComboBoxEnumControl comboBoxEnumControl1;
    }
}

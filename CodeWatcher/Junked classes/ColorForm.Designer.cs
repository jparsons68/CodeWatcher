namespace CodeWatcher
{
    partial class ColorForm
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            Syncfusion.Windows.Forms.MetroColorTable metroColorTable1 = new Syncfusion.Windows.Forms.MetroColorTable();
            this.colorUIControl1 = new Syncfusion.Windows.Forms.ColorUIControl();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // colorUIControl1
            // 
            this.colorUIControl1.BeforeTouchSize = new System.Drawing.Size(199, 218);
            this.colorUIControl1.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.colorUIControl1.ColorGroups = ((Syncfusion.Windows.Forms.ColorUIGroups)(((Syncfusion.Windows.Forms.ColorUIGroups.StandardColors | Syncfusion.Windows.Forms.ColorUIGroups.SystemColors) 
            | Syncfusion.Windows.Forms.ColorUIGroups.UserColors)));
            this.colorUIControl1.CustomColorsStretchOnResize = true;
            this.colorUIControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.colorUIControl1.EnableTouchMode = true;
            this.colorUIControl1.ForeColor = System.Drawing.SystemColors.ControlText;
            this.colorUIControl1.Location = new System.Drawing.Point(0, 20);
            this.colorUIControl1.Name = "colorUIControl1";
            this.colorUIControl1.ScrollMetroColorTable = metroColorTable1;
            this.colorUIControl1.Size = new System.Drawing.Size(199, 198);
            this.colorUIControl1.TabIndex = 3;
            this.colorUIControl1.Text = "colorUIControl1";
            this.colorUIControl1.ThemeName = "Default";
            this.colorUIControl1.UserColorsStretchOnResize = true;
            // 
            // label1
            // 
            this.label1.Dock = System.Windows.Forms.DockStyle.Top;
            this.label1.Location = new System.Drawing.Point(0, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(199, 20);
            this.label1.TabIndex = 4;
            this.label1.Text = "label1";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // ColorForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.ClientSize = new System.Drawing.Size(199, 247);
            this.Controls.Add(this.colorUIControl1);
            this.Controls.Add(this.label1);
            this.Name = "ColorForm";
            this.ShowClose = true;
            this.ShowOK = true;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Color Editor";
            this.Controls.SetChildIndex(this.label1, 0);
            this.Controls.SetChildIndex(this.colorUIControl1, 0);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Syncfusion.Windows.Forms.ColorUIControl colorUIControl1;
        private System.Windows.Forms.Label label1;
    }
}

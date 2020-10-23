namespace RezaB.Mikrotik.Extentions.Forms
{
    partial class NetMapViewForm
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
            this.NetmapListTextbox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // NetmapListTextbox
            // 
            this.NetmapListTextbox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.NetmapListTextbox.Location = new System.Drawing.Point(0, 0);
            this.NetmapListTextbox.Multiline = true;
            this.NetmapListTextbox.Name = "NetmapListTextbox";
            this.NetmapListTextbox.ReadOnly = true;
            this.NetmapListTextbox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.NetmapListTextbox.Size = new System.Drawing.Size(412, 628);
            this.NetmapListTextbox.TabIndex = 0;
            this.NetmapListTextbox.WordWrap = false;
            // 
            // NetMapViewForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(412, 628);
            this.Controls.Add(this.NetmapListTextbox);
            this.Cursor = System.Windows.Forms.Cursors.Default;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "NetMapViewForm";
            this.Text = "Netmap View";
            this.Load += new System.EventHandler(this.NetMapViewForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox NetmapListTextbox;
    }
}
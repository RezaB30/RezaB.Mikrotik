namespace RezaB.Mikrotik.Extentions.Forms
{
    partial class PoolManagerForm
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
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.LowBoundTextbox = new System.Windows.Forms.TextBox();
            this.HighBoundTextbox = new System.Windows.Forms.TextBox();
            this.AddButton = new System.Windows.Forms.Button();
            this.PoolListbox = new System.Windows.Forms.ListBox();
            this.RemoveButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(72, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Low Boundry:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(13, 40);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(74, 13);
            this.label2.TabIndex = 0;
            this.label2.Text = "High Boundry:";
            // 
            // LowBoundTextbox
            // 
            this.LowBoundTextbox.Location = new System.Drawing.Point(93, 11);
            this.LowBoundTextbox.Name = "LowBoundTextbox";
            this.LowBoundTextbox.Size = new System.Drawing.Size(146, 20);
            this.LowBoundTextbox.TabIndex = 0;
            // 
            // HighBoundTextbox
            // 
            this.HighBoundTextbox.Location = new System.Drawing.Point(93, 37);
            this.HighBoundTextbox.Name = "HighBoundTextbox";
            this.HighBoundTextbox.Size = new System.Drawing.Size(146, 20);
            this.HighBoundTextbox.TabIndex = 1;
            // 
            // AddButton
            // 
            this.AddButton.Location = new System.Drawing.Point(164, 63);
            this.AddButton.Name = "AddButton";
            this.AddButton.Size = new System.Drawing.Size(75, 23);
            this.AddButton.TabIndex = 2;
            this.AddButton.Text = "Add >>";
            this.AddButton.UseVisualStyleBackColor = true;
            this.AddButton.Click += new System.EventHandler(this.AddButton_Click);
            // 
            // PoolListbox
            // 
            this.PoolListbox.FormattingEnabled = true;
            this.PoolListbox.Location = new System.Drawing.Point(245, 11);
            this.PoolListbox.Name = "PoolListbox";
            this.PoolListbox.Size = new System.Drawing.Size(184, 264);
            this.PoolListbox.TabIndex = 4;
            // 
            // RemoveButton
            // 
            this.RemoveButton.Location = new System.Drawing.Point(164, 92);
            this.RemoveButton.Name = "RemoveButton";
            this.RemoveButton.Size = new System.Drawing.Size(75, 23);
            this.RemoveButton.TabIndex = 3;
            this.RemoveButton.Text = "<< Remove";
            this.RemoveButton.UseVisualStyleBackColor = true;
            this.RemoveButton.Click += new System.EventHandler(this.RemoveButton_Click);
            // 
            // PoolManagerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(441, 285);
            this.Controls.Add(this.PoolListbox);
            this.Controls.Add(this.RemoveButton);
            this.Controls.Add(this.AddButton);
            this.Controls.Add(this.HighBoundTextbox);
            this.Controls.Add(this.LowBoundTextbox);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PoolManagerForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Manage IP Pools";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox LowBoundTextbox;
        private System.Windows.Forms.TextBox HighBoundTextbox;
        private System.Windows.Forms.Button AddButton;
        private System.Windows.Forms.ListBox PoolListbox;
        private System.Windows.Forms.Button RemoveButton;
    }
}
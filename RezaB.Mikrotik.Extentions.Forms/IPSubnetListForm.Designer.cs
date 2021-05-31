
namespace RezaB.Mikrotik.Extentions.Forms
{
    partial class IPSubnetListForm
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
            this.SubnetListbox = new System.Windows.Forms.ListBox();
            this.SubnetTextbox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.AddButton = new System.Windows.Forms.Button();
            this.RemoveButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // SubnetListbox
            // 
            this.SubnetListbox.FormattingEnabled = true;
            this.SubnetListbox.Location = new System.Drawing.Point(211, 12);
            this.SubnetListbox.Name = "SubnetListbox";
            this.SubnetListbox.Size = new System.Drawing.Size(175, 368);
            this.SubnetListbox.TabIndex = 0;
            // 
            // SubnetTextbox
            // 
            this.SubnetTextbox.Location = new System.Drawing.Point(12, 28);
            this.SubnetTextbox.Name = "SubnetTextbox";
            this.SubnetTextbox.Size = new System.Drawing.Size(140, 20);
            this.SubnetTextbox.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 12);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(57, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "IP Subnet:";
            // 
            // AddButton
            // 
            this.AddButton.Location = new System.Drawing.Point(158, 26);
            this.AddButton.Name = "AddButton";
            this.AddButton.Size = new System.Drawing.Size(47, 23);
            this.AddButton.TabIndex = 3;
            this.AddButton.Text = "Add >";
            this.AddButton.UseVisualStyleBackColor = true;
            this.AddButton.Click += new System.EventHandler(this.AddButton_Click);
            // 
            // RemoveButton
            // 
            this.RemoveButton.Location = new System.Drawing.Point(130, 55);
            this.RemoveButton.Name = "RemoveButton";
            this.RemoveButton.Size = new System.Drawing.Size(75, 23);
            this.RemoveButton.TabIndex = 4;
            this.RemoveButton.Text = "< Remove";
            this.RemoveButton.UseVisualStyleBackColor = true;
            this.RemoveButton.Click += new System.EventHandler(this.RemoveButton_Click);
            // 
            // IPSubnetListForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(398, 394);
            this.Controls.Add(this.RemoveButton);
            this.Controls.Add(this.AddButton);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.SubnetTextbox);
            this.Controls.Add(this.SubnetListbox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "IPSubnetListForm";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "IP Subnet List";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox SubnetListbox;
        private System.Windows.Forms.TextBox SubnetTextbox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button AddButton;
        private System.Windows.Forms.Button RemoveButton;
    }
}
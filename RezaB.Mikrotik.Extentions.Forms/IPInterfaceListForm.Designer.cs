
namespace RezaB.Mikrotik.Extentions.Forms
{
    partial class IPInterfaceListForm
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
            this.IPListbox = new System.Windows.Forms.ListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.IPTextbox = new System.Windows.Forms.TextBox();
            this.AddButton = new System.Windows.Forms.Button();
            this.RemoveButton = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.InterfaceTextbox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // IPListbox
            // 
            this.IPListbox.FormattingEnabled = true;
            this.IPListbox.Location = new System.Drawing.Point(199, 12);
            this.IPListbox.Name = "IPListbox";
            this.IPListbox.Size = new System.Drawing.Size(162, 381);
            this.IPListbox.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 12);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(20, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "IP:";
            // 
            // IPTextbox
            // 
            this.IPTextbox.Location = new System.Drawing.Point(12, 29);
            this.IPTextbox.Name = "IPTextbox";
            this.IPTextbox.Size = new System.Drawing.Size(127, 20);
            this.IPTextbox.TabIndex = 2;
            // 
            // AddButton
            // 
            this.AddButton.Location = new System.Drawing.Point(147, 70);
            this.AddButton.Name = "AddButton";
            this.AddButton.Size = new System.Drawing.Size(46, 23);
            this.AddButton.TabIndex = 3;
            this.AddButton.Text = "Add >";
            this.AddButton.UseVisualStyleBackColor = true;
            this.AddButton.Click += new System.EventHandler(this.AddButton_Click);
            // 
            // RemoveButton
            // 
            this.RemoveButton.Location = new System.Drawing.Point(118, 98);
            this.RemoveButton.Name = "RemoveButton";
            this.RemoveButton.Size = new System.Drawing.Size(75, 23);
            this.RemoveButton.TabIndex = 4;
            this.RemoveButton.Text = "< Remove";
            this.RemoveButton.UseVisualStyleBackColor = true;
            this.RemoveButton.Click += new System.EventHandler(this.RemoveButton_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 56);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(52, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "Interface:";
            // 
            // InterfaceTextbox
            // 
            this.InterfaceTextbox.Location = new System.Drawing.Point(12, 72);
            this.InterfaceTextbox.Name = "InterfaceTextbox";
            this.InterfaceTextbox.Size = new System.Drawing.Size(127, 20);
            this.InterfaceTextbox.TabIndex = 2;
            // 
            // IPInterfaceListForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(373, 414);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.RemoveButton);
            this.Controls.Add(this.AddButton);
            this.Controls.Add(this.InterfaceTextbox);
            this.Controls.Add(this.IPTextbox);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.IPListbox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MinimizeBox = false;
            this.Name = "IPInterfaceListForm";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "IP Interface List";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox IPListbox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox IPTextbox;
        private System.Windows.Forms.Button AddButton;
        private System.Windows.Forms.Button RemoveButton;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox InterfaceTextbox;
    }
}
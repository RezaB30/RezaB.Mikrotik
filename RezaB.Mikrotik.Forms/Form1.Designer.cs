namespace RezaB.Mikrotik.Forms
{
    partial class MainForm
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
            this.CommandTextBox = new System.Windows.Forms.TextBox();
            this.MainTabControl = new System.Windows.Forms.TabControl();
            this.LoginTab = new System.Windows.Forms.TabPage();
            this.LoginPanel = new System.Windows.Forms.Panel();
            this.LoggedInPanel = new System.Windows.Forms.Panel();
            this.PortTextBox = new System.Windows.Forms.TextBox();
            this.IPTextBox = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.LoginResultLabel = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.LoginButton = new System.Windows.Forms.Button();
            this.UsernameTextBox = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.PasswordTextBox = new System.Windows.Forms.TextBox();
            this.LoggedOutPanel = new System.Windows.Forms.Panel();
            this.LogoutButton = new System.Windows.Forms.Button();
            this.MainTabPage = new System.Windows.Forms.TabPage();
            this.ResponseTextBox = new System.Windows.Forms.TextBox();
            this.ToolboxPanel = new System.Windows.Forms.Panel();
            this.ClearButton = new System.Windows.Forms.Button();
            this.CommandPanel = new System.Windows.Forms.Panel();
            this.ParameterHelpLabel = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.QueryTextBox = new System.Windows.Forms.TextBox();
            this.ParameterTextBox = new System.Windows.Forms.TextBox();
            this.CommandButton = new System.Windows.Forms.Button();
            this.ConditionTypeCheckbox = new System.Windows.Forms.CheckBox();
            this.MainTabControl.SuspendLayout();
            this.LoginTab.SuspendLayout();
            this.LoginPanel.SuspendLayout();
            this.LoggedInPanel.SuspendLayout();
            this.LoggedOutPanel.SuspendLayout();
            this.MainTabPage.SuspendLayout();
            this.ToolboxPanel.SuspendLayout();
            this.CommandPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(0, 10);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(57, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Command:";
            // 
            // CommandTextBox
            // 
            this.CommandTextBox.Location = new System.Drawing.Point(74, 7);
            this.CommandTextBox.Name = "CommandTextBox";
            this.CommandTextBox.Size = new System.Drawing.Size(884, 20);
            this.CommandTextBox.TabIndex = 1;
            this.CommandTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.CommandTextBox_KeyDown);
            // 
            // MainTabControl
            // 
            this.MainTabControl.Controls.Add(this.LoginTab);
            this.MainTabControl.Controls.Add(this.MainTabPage);
            this.MainTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MainTabControl.Location = new System.Drawing.Point(0, 0);
            this.MainTabControl.Name = "MainTabControl";
            this.MainTabControl.SelectedIndex = 0;
            this.MainTabControl.Size = new System.Drawing.Size(1058, 472);
            this.MainTabControl.TabIndex = 3;
            // 
            // LoginTab
            // 
            this.LoginTab.Controls.Add(this.LoginPanel);
            this.LoginTab.Location = new System.Drawing.Point(4, 22);
            this.LoginTab.Name = "LoginTab";
            this.LoginTab.Size = new System.Drawing.Size(1050, 446);
            this.LoginTab.TabIndex = 1;
            this.LoginTab.Text = "Login";
            this.LoginTab.UseVisualStyleBackColor = true;
            this.LoginTab.SizeChanged += new System.EventHandler(this.LoginTab_SizeChanged);
            // 
            // LoginPanel
            // 
            this.LoginPanel.Controls.Add(this.LoggedInPanel);
            this.LoginPanel.Controls.Add(this.LoggedOutPanel);
            this.LoginPanel.Location = new System.Drawing.Point(364, 3);
            this.LoginPanel.Name = "LoginPanel";
            this.LoginPanel.Size = new System.Drawing.Size(325, 167);
            this.LoginPanel.TabIndex = 2;
            // 
            // LoggedInPanel
            // 
            this.LoggedInPanel.Controls.Add(this.PortTextBox);
            this.LoggedInPanel.Controls.Add(this.IPTextBox);
            this.LoggedInPanel.Controls.Add(this.label6);
            this.LoggedInPanel.Controls.Add(this.label5);
            this.LoggedInPanel.Controls.Add(this.LoginResultLabel);
            this.LoggedInPanel.Controls.Add(this.label2);
            this.LoggedInPanel.Controls.Add(this.LoginButton);
            this.LoggedInPanel.Controls.Add(this.UsernameTextBox);
            this.LoggedInPanel.Controls.Add(this.label3);
            this.LoggedInPanel.Controls.Add(this.PasswordTextBox);
            this.LoggedInPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.LoggedInPanel.Location = new System.Drawing.Point(0, 0);
            this.LoggedInPanel.Name = "LoggedInPanel";
            this.LoggedInPanel.Size = new System.Drawing.Size(325, 167);
            this.LoggedInPanel.TabIndex = 3;
            // 
            // PortTextBox
            // 
            this.PortTextBox.Location = new System.Drawing.Point(78, 29);
            this.PortTextBox.Name = "PortTextBox";
            this.PortTextBox.Size = new System.Drawing.Size(195, 20);
            this.PortTextBox.TabIndex = 2;
            this.PortTextBox.Text = "8728";
            this.PortTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.PasswordTextBox_KeyDown);
            // 
            // IPTextBox
            // 
            this.IPTextBox.Location = new System.Drawing.Point(78, 3);
            this.IPTextBox.Name = "IPTextBox";
            this.IPTextBox.Size = new System.Drawing.Size(195, 20);
            this.IPTextBox.TabIndex = 1;
            this.IPTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.PasswordTextBox_KeyDown);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(14, 32);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(29, 13);
            this.label6.TabIndex = 5;
            this.label6.Text = "Port:";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(14, 6);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(20, 13);
            this.label5.TabIndex = 5;
            this.label5.Text = "IP:";
            // 
            // LoginResultLabel
            // 
            this.LoginResultLabel.AutoSize = true;
            this.LoginResultLabel.Location = new System.Drawing.Point(14, 143);
            this.LoginResultLabel.Name = "LoginResultLabel";
            this.LoginResultLabel.Size = new System.Drawing.Size(64, 13);
            this.LoginResultLabel.TabIndex = 4;
            this.LoginResultLabel.Text = "No attempts";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(14, 58);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(58, 13);
            this.label2.TabIndex = 0;
            this.label2.Text = "Username:";
            // 
            // LoginButton
            // 
            this.LoginButton.Location = new System.Drawing.Point(198, 107);
            this.LoginButton.Name = "LoginButton";
            this.LoginButton.Size = new System.Drawing.Size(75, 23);
            this.LoginButton.TabIndex = 5;
            this.LoginButton.Text = "Login";
            this.LoginButton.UseVisualStyleBackColor = true;
            this.LoginButton.Click += new System.EventHandler(this.LoginButton_Click);
            // 
            // UsernameTextBox
            // 
            this.UsernameTextBox.Location = new System.Drawing.Point(78, 55);
            this.UsernameTextBox.Name = "UsernameTextBox";
            this.UsernameTextBox.Size = new System.Drawing.Size(195, 20);
            this.UsernameTextBox.TabIndex = 3;
            this.UsernameTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.PasswordTextBox_KeyDown);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(14, 84);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(56, 13);
            this.label3.TabIndex = 0;
            this.label3.Text = "Password:";
            // 
            // PasswordTextBox
            // 
            this.PasswordTextBox.Location = new System.Drawing.Point(78, 81);
            this.PasswordTextBox.Name = "PasswordTextBox";
            this.PasswordTextBox.Size = new System.Drawing.Size(195, 20);
            this.PasswordTextBox.TabIndex = 4;
            this.PasswordTextBox.UseSystemPasswordChar = true;
            this.PasswordTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.PasswordTextBox_KeyDown);
            // 
            // LoggedOutPanel
            // 
            this.LoggedOutPanel.Controls.Add(this.LogoutButton);
            this.LoggedOutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.LoggedOutPanel.Location = new System.Drawing.Point(0, 0);
            this.LoggedOutPanel.Name = "LoggedOutPanel";
            this.LoggedOutPanel.Size = new System.Drawing.Size(325, 167);
            this.LoggedOutPanel.TabIndex = 3;
            this.LoggedOutPanel.Visible = false;
            // 
            // LogoutButton
            // 
            this.LogoutButton.Location = new System.Drawing.Point(126, 49);
            this.LogoutButton.Name = "LogoutButton";
            this.LogoutButton.Size = new System.Drawing.Size(75, 23);
            this.LogoutButton.TabIndex = 0;
            this.LogoutButton.Text = "Logout";
            this.LogoutButton.UseVisualStyleBackColor = true;
            this.LogoutButton.Click += new System.EventHandler(this.LogoutButton_Click);
            // 
            // MainTabPage
            // 
            this.MainTabPage.Controls.Add(this.ResponseTextBox);
            this.MainTabPage.Controls.Add(this.ToolboxPanel);
            this.MainTabPage.Controls.Add(this.CommandPanel);
            this.MainTabPage.Location = new System.Drawing.Point(4, 22);
            this.MainTabPage.Name = "MainTabPage";
            this.MainTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.MainTabPage.Size = new System.Drawing.Size(1050, 446);
            this.MainTabPage.TabIndex = 0;
            this.MainTabPage.Text = "Commands";
            this.MainTabPage.UseVisualStyleBackColor = true;
            this.MainTabPage.SizeChanged += new System.EventHandler(this.MainTabPage_SizeChanged);
            // 
            // ResponseTextBox
            // 
            this.ResponseTextBox.BackColor = System.Drawing.Color.Linen;
            this.ResponseTextBox.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.ResponseTextBox.Location = new System.Drawing.Point(3, 142);
            this.ResponseTextBox.Multiline = true;
            this.ResponseTextBox.Name = "ResponseTextBox";
            this.ResponseTextBox.ReadOnly = true;
            this.ResponseTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.ResponseTextBox.Size = new System.Drawing.Size(1044, 301);
            this.ResponseTextBox.TabIndex = 6;
            // 
            // ToolboxPanel
            // 
            this.ToolboxPanel.Controls.Add(this.ClearButton);
            this.ToolboxPanel.Location = new System.Drawing.Point(3, 104);
            this.ToolboxPanel.Name = "ToolboxPanel";
            this.ToolboxPanel.Size = new System.Drawing.Size(1044, 32);
            this.ToolboxPanel.TabIndex = 5;
            this.ToolboxPanel.SizeChanged += new System.EventHandler(this.ToolboxPanel_SizeChanged);
            // 
            // ClearButton
            // 
            this.ClearButton.Location = new System.Drawing.Point(484, 4);
            this.ClearButton.Name = "ClearButton";
            this.ClearButton.Size = new System.Drawing.Size(75, 23);
            this.ClearButton.TabIndex = 0;
            this.ClearButton.Text = "Clear";
            this.ClearButton.UseVisualStyleBackColor = true;
            this.ClearButton.Click += new System.EventHandler(this.ClearButton_Click);
            // 
            // CommandPanel
            // 
            this.CommandPanel.Controls.Add(this.ConditionTypeCheckbox);
            this.CommandPanel.Controls.Add(this.ParameterHelpLabel);
            this.CommandPanel.Controls.Add(this.label7);
            this.CommandPanel.Controls.Add(this.label4);
            this.CommandPanel.Controls.Add(this.QueryTextBox);
            this.CommandPanel.Controls.Add(this.ParameterTextBox);
            this.CommandPanel.Controls.Add(this.CommandButton);
            this.CommandPanel.Controls.Add(this.label1);
            this.CommandPanel.Controls.Add(this.CommandTextBox);
            this.CommandPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.CommandPanel.Location = new System.Drawing.Point(3, 3);
            this.CommandPanel.Name = "CommandPanel";
            this.CommandPanel.Size = new System.Drawing.Size(1044, 95);
            this.CommandPanel.TabIndex = 4;
            this.CommandPanel.SizeChanged += new System.EventHandler(this.CommandPanel_SizeChanged);
            // 
            // ParameterHelpLabel
            // 
            this.ParameterHelpLabel.AutoSize = true;
            this.ParameterHelpLabel.Location = new System.Drawing.Point(964, 37);
            this.ParameterHelpLabel.Name = "ParameterHelpLabel";
            this.ParameterHelpLabel.Size = new System.Drawing.Size(68, 13);
            this.ParameterHelpLabel.TabIndex = 5;
            this.ParameterHelpLabel.Text = "p1=x p2=y ...";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(5, 63);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(38, 13);
            this.label7.TabIndex = 4;
            this.label7.Text = "Query:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(5, 37);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(63, 13);
            this.label4.TabIndex = 4;
            this.label4.Text = "Parameters:";
            // 
            // QueryTextBox
            // 
            this.QueryTextBox.Location = new System.Drawing.Point(74, 60);
            this.QueryTextBox.Name = "QueryTextBox";
            this.QueryTextBox.Size = new System.Drawing.Size(884, 20);
            this.QueryTextBox.TabIndex = 3;
            this.QueryTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.CommandTextBox_KeyDown);
            // 
            // ParameterTextBox
            // 
            this.ParameterTextBox.Location = new System.Drawing.Point(74, 34);
            this.ParameterTextBox.Name = "ParameterTextBox";
            this.ParameterTextBox.Size = new System.Drawing.Size(884, 20);
            this.ParameterTextBox.TabIndex = 2;
            this.ParameterTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.CommandTextBox_KeyDown);
            // 
            // CommandButton
            // 
            this.CommandButton.Enabled = false;
            this.CommandButton.Location = new System.Drawing.Point(964, 5);
            this.CommandButton.Name = "CommandButton";
            this.CommandButton.Size = new System.Drawing.Size(75, 23);
            this.CommandButton.TabIndex = 4;
            this.CommandButton.Text = "Send";
            this.CommandButton.UseVisualStyleBackColor = true;
            this.CommandButton.Click += new System.EventHandler(this.CommandButton_Click);
            // 
            // ConditionTypeCheckbox
            // 
            this.ConditionTypeCheckbox.AutoSize = true;
            this.ConditionTypeCheckbox.Location = new System.Drawing.Point(967, 62);
            this.ConditionTypeCheckbox.Name = "ConditionTypeCheckbox";
            this.ConditionTypeCheckbox.Size = new System.Drawing.Size(37, 17);
            this.ConditionTypeCheckbox.TabIndex = 6;
            this.ConditionTypeCheckbox.Text = "Or";
            this.ConditionTypeCheckbox.UseVisualStyleBackColor = true;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1058, 472);
            this.Controls.Add(this.MainTabControl);
            this.MinimumSize = new System.Drawing.Size(500, 400);
            this.Name = "MainForm";
            this.Text = "Mikrotik Connector";
            this.MainTabControl.ResumeLayout(false);
            this.LoginTab.ResumeLayout(false);
            this.LoginPanel.ResumeLayout(false);
            this.LoggedInPanel.ResumeLayout(false);
            this.LoggedInPanel.PerformLayout();
            this.LoggedOutPanel.ResumeLayout(false);
            this.MainTabPage.ResumeLayout(false);
            this.MainTabPage.PerformLayout();
            this.ToolboxPanel.ResumeLayout(false);
            this.CommandPanel.ResumeLayout(false);
            this.CommandPanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox CommandTextBox;
        private System.Windows.Forms.TabControl MainTabControl;
        private System.Windows.Forms.TabPage MainTabPage;
        private System.Windows.Forms.Panel CommandPanel;
        private System.Windows.Forms.Button CommandButton;
        private System.Windows.Forms.Panel ToolboxPanel;
        private System.Windows.Forms.Button ClearButton;
        private System.Windows.Forms.TextBox ResponseTextBox;
        private System.Windows.Forms.TabPage LoginTab;
        private System.Windows.Forms.TextBox PasswordTextBox;
        private System.Windows.Forms.TextBox UsernameTextBox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Panel LoginPanel;
        private System.Windows.Forms.Button LoginButton;
        private System.Windows.Forms.Label LoginResultLabel;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox ParameterTextBox;
        private System.Windows.Forms.Panel LoggedInPanel;
        private System.Windows.Forms.Panel LoggedOutPanel;
        private System.Windows.Forms.Button LogoutButton;
        private System.Windows.Forms.TextBox PortTextBox;
        private System.Windows.Forms.TextBox IPTextBox;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label ParameterHelpLabel;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox QueryTextBox;
        private System.Windows.Forms.CheckBox ConditionTypeCheckbox;
    }
}


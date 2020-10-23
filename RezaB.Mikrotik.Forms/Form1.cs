using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RezaB.Mikrotik.Forms
{
    public partial class MainForm : Form
    {
        MikrotikConnector _connector = new MikrotikConnector();

        public MainForm()
        {
            InitializeComponent();
        }

        private void CommandPanel_SizeChanged(object sender, EventArgs e)
        {
            CommandTextBox.Width = CommandPanel.Width - 160;
            ParameterTextBox.Width = CommandPanel.Width - 160;
            CommandButton.Left = CommandPanel.Width - 80;
        }

        private void MainTabPage_SizeChanged(object sender, EventArgs e)
        {
            ToolboxPanel.Width = MainTabPage.Width - 10;
            ResponseTextBox.Height = MainTabPage.Height - 145;
            ParameterHelpLabel.Left = MainTabPage.Width - 86;
        }

        private void ToolboxPanel_SizeChanged(object sender, EventArgs e)
        {
            ClearButton.Left = ToolboxPanel.Width / 2 - ClearButton.Width / 2;
        }

        private void CommandButton_Click(object sender, EventArgs e)
        {
            // create standard parameters
            var parameterList = ParameterTextBox.Text.Split(' ')
                .Select(part => new MikrotikCommandParameter(part.Split('=').FirstOrDefault(), part.Split('=').LastOrDefault()))
                .Where(par => !string.IsNullOrEmpty(par.Name) && !string.IsNullOrEmpty(par.Value)).ToArray();
            // create query parameters
            var queryArray = QueryTextBox.Text.Split(' ')
                .Select(part => new MikrotikCommandParameter(part.Split('=').FirstOrDefault(), part.Split('=').LastOrDefault(), MikrotikCommandParameter.ParameterType.Query))
                .Where(par => !string.IsNullOrEmpty(par.Name) && !string.IsNullOrEmpty(par.Value)).ToArray();
            if (ConditionTypeCheckbox.Checked)
            {
                var queryParams = new List<MikrotikCommandParameter>();
                for (int i = 0; i < queryArray.Length; i++)
                {
                    queryParams.Add(queryArray[i]);
                    if (i > 0)
                    {
                        queryParams.Add(new MikrotikCommandParameter("#|", null, MikrotikCommandParameter.ParameterType.Query));
                    }
                }
                queryArray = queryParams.ToArray();
            }
            parameterList = parameterList.Concat(queryArray).ToArray();
            ResponseTextBox.AppendText(_connector.ExecuteCommand(CommandTextBox.Text, parameterList).Raw);
            ResponseTextBox.AppendText(Environment.NewLine);
        }

        private void CommandTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Return)
                CommandButton.PerformClick();
        }

        private void LoginTab_SizeChanged(object sender, EventArgs e)
        {
            LoginPanel.Left = LoginTab.Width / 2 - LoginPanel.Width / 2;
        }

        private void LoginButton_Click(object sender, EventArgs e)
        {
            try
            {
                _connector.Open(IPTextBox.Text, int.Parse(PortTextBox.Text));
            }
            catch (Exception ex)
            {
                ShowExceptionError(ex);
            }
            var response = _connector.Login(UsernameTextBox.Text, PasswordTextBox.Text);
            if (response.ErrorCode == 0)
            {
                LoginResultLabel.Text = "Login Successful!";
                LoginResultLabel.ForeColor = Color.Green;
                MainTabControl.SelectedTab = MainTabPage;
                CommandTextBox.Focus();
                CommandButton.Enabled = true;
                LoggedOutPanel.Visible = true;
                LoggedInPanel.Visible = false;
            }
            else
            {
                LoginResultLabel.Text = "Login Failed: " + response.ErrorMessage;
                LoginResultLabel.ForeColor = Color.Red;
            }
        }

        private void PasswordTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Return)
                LoginButton.PerformClick();
        }

        private void ClearButton_Click(object sender, EventArgs e)
        {
            ResponseTextBox.Clear();
        }

        private void LogoutButton_Click(object sender, EventArgs e)
        {
            _connector.ExecuteCommand("/quit");
            _connector.Close();
            //_connector = new MikrotikConnector();
            //_connector.Open("188.119.30.14", 8728);

            LoggedInPanel.Visible = true;
            LoggedOutPanel.Visible = false;
        }

        private void ShowExceptionError(Exception ex)
        {
            while (ex.InnerException != null)
            {
                ex = ex.InnerException;
            }
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

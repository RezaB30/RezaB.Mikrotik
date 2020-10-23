using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using RezaB.Mikrotik.Extentions;
using System.Reflection;
using RezaB.Networking.IP;

namespace RezaB.Mikrotik.Extentions.Forms
{
    public partial class MainForm : Form
    {
        private MikrotikApiCredentials _credentials
        {
            get
            {
                try
                {
                    return new MikrotikApiCredentials(RouterIPTextbox.Text, int.Parse(RouterPortTextbox.Text), RouterUsername.Text, RouterPassword.Text);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Invalid Credentials", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return null;
                }
            }
        }

        private MikrotikRouter _router = null;

        private List<IPPool> localIpPools = new List<IPPool>();
        private List<IPPool> realIPPools = new List<IPPool>();
        private List<NetMapRoutingTable> NetmapTable = null;

        public MainForm()
        {
            InitializeComponent();
            for (int i = 1; i <= 30; i++)
            {
                IPCapacityCombobox.Items.Add(i);
                IPCapacityCombobox2.Items.Add(i);
                IPCapacityCombobox3.Items.Add(i);
                UpdateIPCapacityCombobox.Items.Add(i);
            }
            IPCapacityCombobox.SelectedIndex = 29;
            IPCapacityCombobox2.SelectedIndex = 29;
            IPCapacityCombobox3.SelectedIndex = 29;
            UpdateIPCapacityCombobox.SelectedIndex = 29;
            // version info
            this.Text += " v" + Assembly.GetAssembly(typeof(MikrotikRouter)).GetName().Version.ToString();
        }

        private void GetOnlineUsersButton_Click(object sender, EventArgs e)
        {
            var results = _router.GetOnlineUsers();
            ResultsTextBox.Text = string.Join(Environment.NewLine, results.ToArray());
        }

        private void ExecuteNATRuleButton_Click(object sender, EventArgs e)
        {
            var results = _router.SetFirewallNATFor_TCP_UDP(NATRealIPTextBox.Text, NATLogPrefixTextBox.Text);
            if (results == null)
            {
                ResultsTextBox.Text = _router.ExceptionLog;
            }
            else
            {
                ResultsTextBox.Text = results.ToString();
            }
        }

        private void NATRemovalExecuteButton_Click(object sender, EventArgs e)
        {
            var results = _router.RemoveFirewallNATFor_TCP_UDP(NATRemovalLocalIPTextBox.Text, NATRemovalRealIPTextbox.Text);
            if (results == false)
            {
                ResultsTextBox.Text = _router.ExceptionLog;
            }
            else
            {
                ResultsTextBox.Text = results.ToString();
            }
        }

        private void GetRateLimitsButton_Click(object sender, EventArgs e)
        {
            var results = _router.GetCurrentRateLimits();
            ResultsTextBox.Text = string.Join(Environment.NewLine, results.Select(item => item.Key + ": " + item.Value));
        }

        private void NATUpdateExecuteButton_Click(object sender, EventArgs e)
        {
            var results = _router.UpdateFirewallNATFor_TCP_UDP(NATUpdateLocalIPTextBox.Text, NATUpdateRealIPTextBox.Text, (int)UpdateIPCapacityCombobox.SelectedItem);
            if (results == null)
            {
                ResultsTextBox.Text = _router.ExceptionLog;
            }
            else
            {
                ResultsTextBox.Text = results.ToString();
            }
        }

        private void LocalIPPoolsButton_Click(object sender, EventArgs e)
        {
            var poolManagerForm = new PoolManagerForm();
            var poolControl = (ListBox)poolManagerForm.Controls.Find("PoolListbox", true).FirstOrDefault();
            poolControl.Items.AddRange(localIpPools.Select(pool => pool.LowBoundryIP + "-" + pool.HighBoundryIP).ToArray());
            poolManagerForm.ShowDialog();
            var poolItems = (poolControl).Items;
            localIpPools = new List<IPPool>();
            foreach (var item in poolItems)
            {
                var parts = item.ToString().Split('-');
                localIpPools.Add(new IPPool()
                {
                    LowBoundryIP = parts[0],
                    HighBoundryIP = parts[1]
                });
            }
        }

        private void RealIPPoolsButton_Click(object sender, EventArgs e)
        {
            var poolManagerForm = new PoolManagerForm();
            var poolControl = (ListBox)poolManagerForm.Controls.Find("PoolListbox", true).FirstOrDefault();
            poolControl.Items.AddRange(realIPPools.Select(pool => pool.LowBoundryIP + "-" + pool.HighBoundryIP).ToArray());
            poolManagerForm.ShowDialog();
            var poolItems = (poolControl).Items;
            realIPPools = new List<IPPool>();
            foreach (var item in poolItems)
            {
                var parts = item.ToString().Split('-');
                realIPPools.Add(new IPPool()
                {
                    LowBoundryIP = parts[0],
                    HighBoundryIP = parts[1]
                });
            }
        }

        private void ExecuteAutoNATRuleButton_Click(object sender, EventArgs e)
        {
            var results = _router.SetAutoNATFor_TCP_UDP(localIpPools, realIPPools, LogPrefixTextbox.Text, (int)IPCapacityCombobox.SelectedItem, ICMPCheckbox.Checked);
            if (results == null)
            {
                ResultsTextBox.Text = _router.ExceptionLog;
            }
            else
            {
                ResultsTextBox.Text = results.ToString();
            }
        }

        private void ManualNATRuleExecuteButton_Click(object sender, EventArgs e)
        {
            var results = _router.SetFirewallNATFor_TCP_UDP(localIpPools, ManualNATRealIPTextbox.Text, LogPrefixTextbox2.Text, (int)IPCapacityCombobox2.SelectedItem, ICMPCheckbox2.Checked);
            if (results == null)
            {
                ResultsTextBox.Text = _router.ExceptionLog;
            }
            else
            {
                ResultsTextBox.Text = results.ToString();
            }
        }

        private void StaticNATRuleExecuteButton_Click(object sender, EventArgs e)
        {
            var results = _router.SetStaticNATFor_TCP_UDP(localIpPools, StaticNATRealIPTextbox.Text, LogPrefixTextbox3.Text, (int)IPCapacityCombobox3.SelectedItem, ICMPCheckbox3.Checked);
            if (results == null)
            {
                ResultsTextBox.Text = _router.ExceptionLog;
            }
            else
            {
                ResultsTextBox.Text = results.ToString();
            }
        }

        private void DisconnectUserButton_Click(object sender, EventArgs e)
        {
            var results = _router.DisconnectUser(DisconnectUsernameTextbox.Text.Split(','));
            ResultsTextBox.Text = results.ToString();
        }

        private void LoginButton_Click(object sender, EventArgs e)
        {
            try
            {
                _router = new MikrotikRouter(_credentials, 10000);
                _router.Open(_router.Credentials.IP, _router.Credentials.Port);
                _router.Login(_router.Credentials.ApiUsername, _router.Credentials.ApiPassword);
                if (_router.IsLoggedIn)
                {
                    LoginStatusLabel.Text = string.Format("Logged in {0}:{1} as \"{2}\"", _router.Credentials.IP, _router.Credentials.Port, _router.Credentials.ApiUsername);
                    LoginStatusPanel.Visible = true;
                    CredentialsPanel.Visible = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private void LogoutButton_Click(object sender, EventArgs e)
        {
            _router.Close();
            LoginStatusLabel.Text = "Not logged in.";
            LoginStatusPanel.Visible = false;
            CredentialsPanel.Visible = true;
        }

        private void LoadNetmapTableButton_Click(object sender, EventArgs e)
        {
            NetmapTable = _router.GetNetmapRoutes();
            ViewNetmapTableButton.Enabled = NetmapTable != null;
            if (NetmapTable == null)
                NetmapListCountLabel.Text = "(null)";
            else
                NetmapListCountLabel.Text = "Data Count: " + NetmapTable.Count;
        }

        private void ViewNetmapTableButton_Click(object sender, EventArgs e)
        {
            NetMapViewForm viewForm = new NetMapViewForm();
            viewForm.NetmapTable = NetmapTable;
            viewForm.ShowDialog();
        }

        private void NetmapCalculateRealIPButton_Click(object sender, EventArgs e)
        {
            var localIP = NetmapLocalIPTextbox.Text;
            var results = NetmapTable.Select(nmt=> new SimpleNetmapRule() { SourceAddress = new IPSubnet() { MinBound = nmt.LocalIPLowerBound, Count = nmt.Count }, ToAddresses = new IPSubnet() { MinBound = nmt.RealIPLowerBound, Count = nmt.Count }, ToPorts = nmt.PortLowerBound + "-" + nmt.PortUpperBound }).FindNetmapRealIP(localIP);
            NetmapRealIPResultsLabel.Text = results.RealIP + ":" + results.PortRange;
        }

        private void InsertNetmapClusterInsertButton_Click(object sender, EventArgs e)
        {
            var results = _router.InsertNetmapCluster(InsertNetmapClusterLocalTextbox.Text, InsertNetmapClusterRealTextbox.Text, Convert.ToInt32(InsertNetmapClusterPortCountNumeric.Value), NetmapPreserveLastByteCheckbox.Checked);
            ResultsTextBox.Text = results.ToString();
        }

        private void NetmapConfirmButton_Click(object sender, EventArgs e)
        {
            var results = _router.ConfirmNetmapChanges(true);
            ResultsTextBox.Text = results.ToString();
        }

        private void NetmapReverseButton_Click(object sender, EventArgs e)
        {
            var results = _router.ReverseChanges();
            ResultsTextBox.Text = results.ToString();
        }

        private void NetmapCleanButton_Click(object sender, EventArgs e)
        {
            var results = _router.ClearActiveNetmaps();
            ResultsTextBox.Text = results.ToString();
        }

        private void VerticalNAT_InsertButton_Click(object sender, EventArgs e)
        {
            var results = _router.InsertVerticalNATRuleCluster(new List<VerticalIPMapRule>()
            {
                new VerticalIPMapRule()
                {
                    LocalIPStart = IPTools.GetUIntValue(VerticalNAT_LocalIPStartTextbox.Text),
                    LocalIPEnd = IPTools.GetUIntValue(VerticalNAT_LocalIPEndTextbox.Text),
                    RealIPStart = IPTools.GetUIntValue(VerticalNAT_RealIPStartTextbox.Text),
                    RealIPEnd = IPTools.GetUIntValue(VerticalNAT_RealIPEndTextbox.Text),
                    PortCount = Convert.ToUInt16(VerticalNAT_PortCountNumeric.Value)
                }
            });

            ResultsTextBox.Text = results.ToString();
        }

        private void VerticalNAT_ConfirmButton_Click(object sender, EventArgs e)
        {
            var results = _router.ConfirmVerticalNAT();

            ResultsTextBox.Text = results.ToString();
        }

        private void VerticalNAT_ReverseButton_Click(object sender, EventArgs e)
        {
            var results = _router.ReverseVerticalNATChanges();
            ResultsTextBox.Text = results.ToString();
        }

        private void VerticalNAT_ClearAllButton_Click(object sender, EventArgs e)
        {
            var results = _router.ClearActiveVerticalNATs();
            ResultsTextBox.Text = results.ToString();
        }
    }
}

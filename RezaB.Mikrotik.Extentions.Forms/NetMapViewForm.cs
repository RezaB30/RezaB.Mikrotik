using RezaB.Networking.IP;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RezaB.Mikrotik.Extentions.Forms
{
    public partial class NetMapViewForm : Form
    {
        public List<NetMapRoutingTable> NetmapTable { get; set; }
        public NetMapViewForm()
        {
            InitializeComponent();
        }

        private void NetMapViewForm_Load(object sender, EventArgs e)
        {
            NetmapListTextbox.Text = string.Join(Environment.NewLine, NetmapTable.Select(nmt => new { IPDetails = new List<IPSubnet>() { new IPSubnet() { MinBound = nmt.LocalIPLowerBound, Count = nmt.Count }, new IPSubnet() { MinBound = nmt.RealIPLowerBound, Count = nmt.Count } }, PortDetails = nmt.PortLowerBound + "-" + nmt.PortUpperBound }).Select(ipSubnet => IPTools.GetIPSubnetString(ipSubnet.IPDetails[0]) + " <-> " + IPTools.GetIPSubnetString(ipSubnet.IPDetails[1]) + " : " + ipSubnet.PortDetails));
        }
    }
}

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
    public partial class IPSubnetListForm : Form
    {
        public IPSubnetListForm()
        {
            InitializeComponent();
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            try
            {
                SubnetListbox.Items.Add(IPTools.ParseIPSubnet(SubnetTextbox.Text));

            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void RemoveButton_Click(object sender, EventArgs e)
        {
            if (SubnetListbox.SelectedItem != null)
                SubnetListbox.Items.Remove(SubnetListbox.SelectedItem);
        }
    }
}

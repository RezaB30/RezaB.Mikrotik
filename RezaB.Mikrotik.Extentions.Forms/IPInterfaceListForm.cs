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
    public partial class IPInterfaceListForm : Form
    {
        public IPInterfaceListForm()
        {
            InitializeComponent();
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            try
            {
                IPTools.GetUIntValue(IPTextbox.Text);
                IPListbox.Items.Add(new IPInterfacePair()
                {
                    IP = IPTextbox.Text,
                    InterfaceName = InterfaceTextbox.Text
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void RemoveButton_Click(object sender, EventArgs e)
        {
            if (IPListbox.SelectedItem != null)
                IPListbox.Items.Remove(IPListbox.SelectedItem);
        }
    }
}

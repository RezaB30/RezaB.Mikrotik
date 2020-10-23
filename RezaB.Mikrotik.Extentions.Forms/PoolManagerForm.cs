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
    public partial class PoolManagerForm : Form
    {
        public PoolManagerForm()
        {
            InitializeComponent();
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            PoolListbox.Items.Add(LowBoundTextbox.Text + "-" + HighBoundTextbox.Text);
        }

        private void RemoveButton_Click(object sender, EventArgs e)
        {
            if (PoolListbox.SelectedItem != null)
                PoolListbox.Items.Remove(PoolListbox.SelectedItem);
        }
    }
}

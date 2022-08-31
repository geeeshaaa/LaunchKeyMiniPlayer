using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsAppMusicPad
{
    public partial class Pads : Form
    {
        public static string trackPlay = "";
        public Pads()
        {
            InitializeComponent();
        }

        private void Pads_Load_1(object sender, EventArgs e)
        {
            foreach (var item in Form1.trackNames)
            {
                foreach (Control value in tableLayoutPanelPads.Controls)
                {
                    if ((value as Button).Name == item.ButtonName)
                        value.Text = item.name;
                }
            }
        }

        private void pad1_Click(object sender, EventArgs e)
        {
            Form1.button1_Click(sender, e);
        }

        private void pad1_DragDrop(object sender, DragEventArgs e)
        {
            Form1.button1_DragDrop(sender, e);
        }

        private void pad1_DragEnter(object sender, DragEventArgs e)
        {
            Form1.button1_DragEnter(sender, e);
        }

        private void pad16_Click(object sender, EventArgs e)
        {
            Form1.PlayStop_Click(sender, e);
        }

        private void Pads_FormClosing(object sender, FormClosingEventArgs e)
        {
            Form1.isPadsFormOpen = false;
        }

        private void pad1_MouseUp(object sender, MouseEventArgs e)
        {
            Form1.button6_MouseUp(sender, e);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            Text = trackPlay;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using WMPLib;
using System.Drawing;
using NAudio.Midi;

namespace WindowsFormsAppMusicPad
{
    public partial class Form1 : Form
    {
        //private static WMPLib.WindowsMediaPlayer Player1 = new WMPLib.WindowsMediaPlayer();
        //private static WMPLib.WindowsMediaPlayer Player2 = new WMPLib.WindowsMediaPlayer();
        private static WMPLib.WindowsMediaPlayer Player1;
        private static WMPLib.WindowsMediaPlayer Player2;

        private MidiInMessageEventArgs ChangeEvent;
        private MidiIn midiIn;
        private string midiDeviceName = "None";
        private int midiDeviceIndex = -1;
        private int buttonNumber = 0;
        private int keyStop = 16;
        private int midiController = 100;

        List<TrackName> trackNames = new List<TrackName>();
        string PlayFile = "";
        int trans = 2;
        public Form1()
        {
            InitializeComponent();

            Player1 = new WindowsMediaPlayer();
            Player2 = new WindowsMediaPlayer();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            for (int i = 0; i < MidiIn.NumberOfDevices; i++)
            {
                comboBox1.Items.Add(MidiIn.DeviceInfo(i).ProductName);
            }
            comboBox1.Items.Add(midiDeviceName);
            comboBox1.SelectedText = "None";
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            midiDeviceIndex = comboBox1.SelectedIndex;
            if (midiDeviceIndex == -1) return;
            if (midiIn == null)
            {
                try
                {
                    midiDeviceIndex = comboBox1.SelectedIndex;
                    midiIn = new MidiIn(midiDeviceIndex);
                    comboBox1.Enabled = false;
                    midiIn.MessageReceived += new EventHandler<MidiInMessageEventArgs>(MidiIn_MessageReceived);
                    midiIn.ErrorReceived += new EventHandler<MidiInMessageEventArgs>(MidiIn_ErrorReceived);

                    midiIn.Start();

                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }


        private void MidiIn_MessageReceived(object sender, MidiInMessageEventArgs e)
        {
            if (MidiEvent.IsNoteOn(e.MidiEvent))
            {
                label1.Invoke((MethodInvoker)delegate ()
                {
                    buttonNumber = (((NoteEvent)e.MidiEvent).NoteNumber + 1);
                    label1.Text = "Note " + buttonNumber;
                    if (buttonNumber == keyStop)
                    {
                        PlayStop_Click(sender, new EventArgs());
                        return;
                    }
                    foreach (var item in panel1.Controls)
                    {
                        if ((item as Button).AccessibleName == buttonNumber.ToString())
                        {
                            button1_Click((item as Button), new EventArgs());
                        }
                    }
                });
            }
            else
            {
                //MidiEvent me = e.MidiEvent;
                ControlChangeEvent cce = e.MidiEvent as ControlChangeEvent;
                if (cce != null && cce.Controller == (MidiController)midiController)
                {
                    label3.Invoke((MethodInvoker)delegate ()
                    {
                        Player1.settings.volume = Player2.settings.volume = trackBar1.Value = cce.ControllerValue;
                        label3.Text = cce.ControllerValue.ToString();
                    });
                }
            }
        }

        private void MidiIn_ErrorReceived(object sender, MidiInMessageEventArgs e)
        {
            MessageBox.Show(String.Format("Time {0} Message 0x{1:X8} Event {2}", e.Timestamp, e.RawMessage, e.MidiEvent));
        }


        private Point MouseHook;
        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) MouseHook = e.Location;
            Location = new Point((Size)Location - (Size)MouseHook + (Size)e.Location);
        }

        private void button1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }
        private void button1_DragDrop(object sender, DragEventArgs e)
        {
            string[] file = (string[])e.Data.GetData(DataFormats.FileDrop);
            trackNames.Add(new TrackName(file[0], (sender as Button).Name));
            (sender as Button).Text = Path.GetFileNameWithoutExtension(file[0]);
        }
        private void button1_Click(object sender, EventArgs e)
        {
            if (PlayFile == (sender as Button).Text) return;

            if (Player1.playState == WMPPlayState.wmppsPlaying)
                foreach (var item in trackNames)
                {
                    if (item.name == (sender as Button).Text)
                    {
                        Player2.URL = item.path;
                        Player2.controls.play();
                        PlayFile = item.name;
                        for (int i = 0, j = trackBar1.Value; i < trackBar1.Value; i++, j--)
                        {
                            Player2.settings.volume = i;
                            Player1.settings.volume = j;
                            //Thread.Sleep(trans);
                        }
                        Player1.controls.stop();
                    }
                }
            else
                foreach (var item in trackNames)
                {
                    if (item.name == (sender as Button).Text)
                    {
                        Player1.URL = item.path;
                        Player1.controls.play();
                        PlayFile = item.name;
                        for (int i = 0, j = trackBar1.Value; i < trackBar1.Value; i++, j--)
                        {
                            Player1.settings.volume = i;
                            Player2.settings.volume = j;
                            //Thread.Sleep(trans);
                        }
                        Player2.controls.stop();
                    }
                }
            (sender as Button).Focus();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (Player1.playState == WMPPlayState.wmppsStopped && (Player2.playState == WMPPlayState.wmppsStopped || Player2.playState == WMPPlayState.wmppsUndefined))
            {
                PlayFile = "";
                label2.Text = "00:00";
            }
            else
            {
                Text = PlayFile;
                if (Player1.playState == WMPPlayState.wmppsPlaying)
                {
                    label2.Text = $"{((int)(Player1.currentMedia.duration - Player1.controls.currentPosition)) / 60 % 60:d2}:" +
                    $"{((int)(Player1.currentMedia.duration - Player1.controls.currentPosition)) % 60:d2}";
                }
                if (Player2.playState == WMPPlayState.wmppsPlaying)
                {
                    label2.Text = $"{((int)(Player2.currentMedia.duration - Player2.controls.currentPosition)) / 60 % 60:d2}:" +
                    $"{((int)(Player2.currentMedia.duration - Player2.controls.currentPosition)) % 60:d2}";
                }

            }
        }
        private void PlayStop_Click(object sender, EventArgs e)
        {
            if (Player1.playState == WMPPlayState.wmppsPlaying || Player2.playState == WMPPlayState.wmppsPlaying)
            {
                for (int j = trackBar1.Value; j > 0; j--)
                {
                    Player2.settings.volume = j;
                    Player1.settings.volume = j;
                    Thread.Sleep(trans);
                }
                Player1.controls.stop();
                Player2.controls.stop();
                PlayFile = "";
            }
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            Player1.settings.volume = trackBar1.Value;
            Player2.settings.volume = trackBar1.Value;
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            trackNames.Clear();
            foreach (var item in panel1.Controls)
                if (item is Button)
                {
                    //if ((item as Button).Text == "Stop") continue;
                    (item as Button).Text = "";
                }
        }

        private void seveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.DefaultExt = ".Playjson";
                dialog.Filter = ".Playjson file|*.Playjson";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    SaveLoad saveLoad = new SaveLoad(dialog.FileName);
                    saveLoad.Save(trackNames);
                }
            }
        }

        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.DefaultExt = ".Playjson";
                dialog.Filter = ".Playjson file|*.Playjson";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    SaveLoad saveLoad = new SaveLoad(dialog.FileName);
                    clearToolStripMenuItem_Click(sender, e);
                    trackNames = saveLoad.Load();
                    foreach (var item in trackNames)
                    {
                        foreach (Control value in panel1.Controls)
                        {
                            if (value is Button)
                            {
                                if (value.Name == item.ButtonName)
                                    value.Text = item.name;
                            }
                        }
                    }
                }
            }
        }

        private void topToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!this.TopMost)
            {
                topToolStripMenuItem.Text = "Top";
                this.TopMost = true;
                topToolStripMenuItem.ForeColor = Color.ForestGreen;
            }
            else
            {
                topToolStripMenuItem.Text = "Bot";
                this.TopMost = false;
                topToolStripMenuItem.ForeColor = Color.Red;

            }
        }


        private void button25_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }
        private void button26_Click(object sender, EventArgs e)
        {
            if (midiIn != null)
            {
                midiIn.Stop();
                midiIn.Dispose();
            }
            this.Close();
        }

    }
}
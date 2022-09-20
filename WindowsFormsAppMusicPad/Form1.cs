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
        public static WMPLib.WindowsMediaPlayer Player1;
        public static WMPLib.WindowsMediaPlayer Player2;

        private MidiInMessageEventArgs ChangeEvent;
        private MidiIn midiIn;
        private string midiDeviceName = "None";
        private int midiDeviceIndex = -1;
        private int noteNumber = 0;
        private int keyStop = 127;
        private int midiController = 100; //Volume control
        private static int volume = 100;

        public static List<TrackName> trackNames = new List<TrackName>();
        public static string PlayFile = "";
        private static int trans = 2;
        private static Button playBytton;
        private static Pads padsForm;
        public static bool isPadsFormOpen = false;
        public Form1()
        {
            InitializeComponent();

            foreach (Button btn in panel1.Controls)
            {
                btn.Click += new System.EventHandler(button1_Click);
                btn.DragDrop += new System.Windows.Forms.DragEventHandler(button1_DragDrop);
                btn.DragEnter += new System.Windows.Forms.DragEventHandler(button1_DragEnter);
                btn.MouseUp += new System.Windows.Forms.MouseEventHandler(button6_MouseUp);
            }
            this.PlayStop.Click += new System.EventHandler(PlayStop_Click);

            Player1 = new WindowsMediaPlayer();
            Player2 = new WindowsMediaPlayer();
            playBytton = new Button();
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
                    noteNumber = (((NoteEvent)e.MidiEvent).NoteNumber);
                    label1.Text = "Note " + noteNumber;
                    if (noteNumber == keyStop)
                    {
                        PlayStop_Click(sender, new EventArgs());
                        return;
                    }

                    foreach (var item in trackNames)
                    {
                        if (item.note == noteNumber)
                        {
                            playBytton.Text = item.name;
                            playBytton.Name = item.ButtonName;
                            button1_Click(playBytton, new EventArgs());
                            foreach (Button btn in panel1.Controls)
                            {
                                if (btn.Name == playBytton.Name)
                                {
                                    btn.Focus();
                                    return;
                                }
                            }
                            if (isPadsFormOpen)
                                foreach (Button btn in Pads.tableLayoutPanelPads.Controls)
                                {
                                    if (btn.Name == playBytton.Name)
                                    {
                                        btn.Focus();
                                        return;
                                    }
                                }
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
                        float v = cce.ControllerValue;
                        Player1.settings.volume = Player2.settings.volume = trackBar1.Value = volume = (int)(v / 1.27);
                        label3.Text = (volume).ToString();
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

        public static void button1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        public static void button1_DragDrop(object sender, DragEventArgs e)
        {
            try
            {
                string[] file = (string[])e.Data.GetData(DataFormats.FileDrop);
                int note = int.Parse((sender as Button).AccessibleName);
                //if (file.Length == 1)
                //{
                //    trackNames.Add(new TrackName(file[0], (sender as Button).Name, note));
                //    (sender as Button).Text = Path.GetFileNameWithoutExtension(file[0]);
                //    return;
                //}

                if (note >= 48 && note <= 72)
                {
                    if (note + (file.Length - 1) > 72)
                    {
                        var res = MessageBox.Show((note + (file.Length - 1) - 72) + " tracks did not fit on the keyboard", "Do you want continue?", MessageBoxButtons.YesNo);
                        if (res == DialogResult.No) return;
                    }
                    for (int i = 0; i < file.Length; i++)
                    {
                        var res = trackNames.Find(track => track.note == (note + i));
                        if (res != null)
                            trackNames.Remove(res);
                    }

                    for (int i = 0; i < file.Length; i++)
                    {
                        trackNames.Add(new TrackName(file[i], "button" + (note + i), note + i));
                        foreach (Button btn in panel1.Controls)
                        {
                            if (int.Parse(btn.AccessibleName) == note + i)
                            {
                                btn.Text = Path.GetFileNameWithoutExtension(file[i]);
                                if (btn.BackColor == Color.FromKnownColor(KnownColor.ControlLightLight) || btn.BackColor == Color.FromKnownColor(KnownColor.Control))
                                {
                                    btn.ForeColor = Color.FromKnownColor(KnownColor.ControlText);
                                }
                                else
                                {
                                    btn.ForeColor = Color.FromKnownColor(KnownColor.ControlLightLight);
                                }
                            }
                        }
                    }
                }
                else if (note >= 74 && note <= 88)
                {
                    if ((note + file.Length - 1) > 88)
                    {
                        var res = MessageBox.Show((note + (file.Length - 1) - 88) + " tracks did not fit on the PADS", "Do you want continue?", MessageBoxButtons.YesNo);
                        if (res == DialogResult.No) return;
                    }
                    for (int i = 0; i < file.Length; i++)
                    {
                        var res = trackNames.Find(track => track.note == (note + i));
                        if (res != null)
                            trackNames.Remove(res);
                    }

                    for (int i = 0; i < file.Length; i++)
                    {
                        trackNames.Add(new TrackName(file[i], "button" + (note + i), note + i));
                        foreach (var item in Pads.tableLayoutPanelPads.Controls)
                        {
                            if (int.Parse((item as Button).AccessibleName) == note + i)
                            {
                                (item as Button).Text = Path.GetFileNameWithoutExtension(file[i]);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public static void button1_Click(object sender, EventArgs e)
        {
            try
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
                            for (int i = 0, j = volume; i < volume; i++, j--)
                            {
                                Player2.settings.volume = i;
                                Player1.settings.volume = j;
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
                            for (int i = 0, j = volume; i < volume; i++, j--)
                            {
                                Player1.settings.volume = i;
                                Player2.settings.volume = j;
                            }
                            Player2.controls.stop();
                        }
                    }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                Player1.settings.volume = Player2.settings.volume = volume;
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (Player1.playState == WMPPlayState.wmppsStopped && (Player2.playState == WMPPlayState.wmppsStopped || Player2.playState == WMPPlayState.wmppsUndefined))
            {
                PlayFile = "";
                label2.Text = "00:00";
                labelTrackName.Text = "";
            }
            else
            {
                Text = PlayFile;
                if (Player1.playState == WMPPlayState.wmppsPlaying)
                {
                    label2.Text = $"{((int)(Player1.currentMedia.duration - Player1.controls.currentPosition)) / 60 % 60:d2}:" +
                    $"{((int)(Player1.currentMedia.duration - Player1.controls.currentPosition)) % 60:d2}";
                    labelTrackName.Text = Path.GetFileNameWithoutExtension(Player1.URL);
                }
                if (Player2.playState == WMPPlayState.wmppsPlaying)
                {
                    label2.Text = $"{((int)(Player2.currentMedia.duration - Player2.controls.currentPosition)) / 60 % 60:d2}:" +
                    $"{((int)(Player2.currentMedia.duration - Player2.controls.currentPosition)) % 60:d2}";
                    labelTrackName.Text = Path.GetFileNameWithoutExtension(Player2.URL);
                }
            }
            if (isPadsFormOpen)
            {
                Pads.trackPlay = labelTrackName.Text;
            }
        }
        public static void PlayStop_Click(object sender, EventArgs e)
        {
            if (Player1.playState == WMPPlayState.wmppsPlaying || Player2.playState == WMPPlayState.wmppsPlaying)
            {
                for (int j = volume; j > 0; j--)
                {
                    Player1.settings.volume = Player2.settings.volume = j;
                    Thread.Sleep(trans);
                }
                Player1.controls.stop();
                Player2.controls.stop();
                PlayFile = "";
                Player1.settings.volume = Player2.settings.volume = volume;
            }
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            Player1.settings.volume = Player2.settings.volume = volume = trackBar1.Value;
            label3.Text = trackBar1.Value.ToString();
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
            if (isPadsFormOpen)
            {
                foreach (var item in padsForm.Controls)
                {
                    if (item is TableLayoutPanel)
                        foreach (var btn in (item as TableLayoutPanel).Controls)
                        {
                            (btn as Button).Text = "";
                        }
                }
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
                        if (isPadsFormOpen)
                            foreach (var pad in Pads.tableLayoutPanelPads.Controls)
                            {
                                if ((pad as Button).Name == item.ButtonName)
                                {
                                    (pad as Button).Text = item.name;
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

        private void pads_Click(object sender, EventArgs e)
        {
            if (!isPadsFormOpen)
                padsForm = new Pads();
            padsForm.Show();
            isPadsFormOpen = true;
        }

        public static void button6_MouseUp(object sender, MouseEventArgs e)
        {
            if ((sender as Button).Text == "") return;
            if (e.Button == MouseButtons.Right)
            {
                var relativeClickedPosition = e.Location;
                var screenClickedPosition = (sender as Control).PointToScreen(relativeClickedPosition);
                contextMenuStrip1.AccessibleDescription = (sender as Button).Name;
                contextMenuStrip1.Show(screenClickedPosition);
            }
        }

        private void clearKeyToolStripMenuItem_Click(object sender, EventArgs e)
        {

            foreach (var item in panel1.Controls)
                if (item is Button)
                {
                    var res = trackNames.Find(track => track.ButtonName == (item as Button).Name);
                    trackNames.Remove(res);
                    (item as Button).Text = "";
                }

        }

        private void clearPadsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (isPadsFormOpen)
            {
                foreach (var pad in Pads.tableLayoutPanelPads.Controls)
                {
                    var res = trackNames.Find(track => track.ButtonName == (pad as Button).Name);
                    trackNames.Remove(res);
                    (pad as Button).Text = "";
                }
            }
            else
            {
                for (int i = 74; i < 89; i++)
                {
                    var res = trackNames.Find(track => track.note == i);
                    trackNames.Remove(res);
                }
            }
        }

        private void clearToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            var tr = trackNames.Find(track => track.ButtonName == contextMenuStrip1.AccessibleDescription);
            if (tr != null)
                trackNames.Remove(tr);
            foreach (Button button in panel1.Controls)
            {
                if (button.Name == contextMenuStrip1.AccessibleDescription)
                {
                    button.Text = "";
                    if (button.BackColor == Color.FromKnownColor(KnownColor.ControlLightLight) || button.BackColor == Color.FromKnownColor(KnownColor.Control))
                    {
                        button.ForeColor = Color.FromKnownColor(KnownColor.ControlText);
                    }
                    else
                    {
                        button.ForeColor = Color.FromKnownColor(KnownColor.ControlLightLight);
                    }
                    return;
                }
            }
            foreach (Button button in Pads.tableLayoutPanelPads.Controls)
            {
                if (button.Name == contextMenuStrip1.AccessibleDescription)
                {
                    button.Text = "";
                    button.ForeColor = Color.FromKnownColor(KnownColor.ControlText);
                    return;
                }
            }
        }

        private void colorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var res = int.Parse(contextMenuStrip1.AccessibleDescription.Substring(6, 2));
            if (res > 73) return;
            ColorDialog colorDialog = new ColorDialog();
            colorDialog.ShowDialog();
            foreach (Button button in panel1.Controls)
            {
                if (button.Name == contextMenuStrip1.AccessibleDescription)
                {
                    button.ForeColor = colorDialog.Color;
                }
            }
        }
    }
}
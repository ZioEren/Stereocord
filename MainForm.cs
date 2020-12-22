using System.Windows.Forms;
using System.Diagnostics;
using System;
using System.Threading;
using System.Runtime.InteropServices;
using System.Runtime;
using System.Collections.Generic;
using Discord.Gateway;
using Discord.Media;
using NAudio.Wave;
using Discord;
public partial class MainForm : Form
{
    [DllImport("psapi.dll")]
    static extern int EmptyWorkingSet(IntPtr hwProc);
    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetProcessWorkingSetSize(IntPtr process, UIntPtr minimumWorkingSetSize, UIntPtr maximumWorkingSetSize);
    public static Random rand = new Random();
    public static bool dmSpammerWorking, serverSpammerWorking, typingSpammerWorking, tokenCheckerWorking;
    public static List<DiscordVoiceSession> sessions;
    public static List<StereoClient> novaClients;
    public static WaveIn waveIn = null;
    public static BufferedWaveProvider waveProvider = null;
    public static List<string> sounds = new List<string>();
    public MainForm()
    {
        InitializeComponent();
        CheckForIllegalCrossThreadCalls = false;
        Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;
        openFileDialog1.DefaultExt = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        new Thread(new ThreadStart(clearRam)).Start();
        if (!System.IO.File.Exists("tokens.txt"))
        {
            System.IO.File.WriteAllText("tokens.txt", "");
        }
        else
        {
            textBox1.Text = System.IO.File.ReadAllText("tokens.txt");
        }
        sessions = new List<DiscordVoiceSession>();
        novaClients = new List<StereoClient>();
        for (int waveInDevice = 0; waveInDevice < WaveIn.DeviceCount; waveInDevice++)
        {
            comboBox2.Items.Add(WaveIn.GetCapabilities(waveInDevice).ProductName);
        }
        comboBox2.SelectedIndex = 0;
        if (!System.IO.File.Exists("sounds.txt"))
        {
            System.IO.File.WriteAllText("sounds.txt", "");
        }
        else
        {
            int i = 0;
            foreach (string line in System.IO.File.ReadAllLines("sounds.txt"))
            {
                i++;
                ListViewItem listViewItem = new ListViewItem();
                listViewItem.Text = i.ToString();
                listViewItem.SubItems.Add(line);
                listView1.Items.Add(listViewItem);
                sounds.Add(line);
            }
        }
    }
    public void clearRam()
    {
        while (true)
        {
            Thread.Sleep(1000);
            EmptyWorkingSet(Process.GetCurrentProcess().Handle);
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(GC.MaxGeneration);
            GC.WaitForPendingFinalizers();
            SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, (UIntPtr)0xFFFFFFFF, (UIntPtr)0xFFFFFFFF);
        }
    }
    private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        Process.GetCurrentProcess().Kill();
    }
    private void firefoxButton1_Click(object sender, EventArgs e)
    {
        openFileDialog1.Title = "Load your tokens here...";
        if (openFileDialog1.ShowDialog() == DialogResult.OK)
        {
            textBox1.Text = System.IO.File.ReadAllText(openFileDialog1.FileName);
        }
    }
    private void textBox1_TextChanged(object sender, EventArgs e)
    {
        System.IO.File.WriteAllText("tokens.txt", textBox1.Text);
    }
    private void firefoxButton15_Click(object sender, EventArgs e)
    {
        try
        {
            Thread thread = new Thread(() => LoadTokens());
            thread.Priority = ThreadPriority.Highest;
            thread.Start();
            MessageBox.Show("Succesfully loaded all tokens!", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception)
        {
        }
    }
    public void LoadTokens()
    {
        int i = 0;
        foreach (string token in textBox1.Lines)
        {
            try
            {
                Thread thread = new Thread(() => LoadToken(token, i));
                thread.Priority = ThreadPriority.Highest;
                thread.Start();
            }
            catch (Exception)
            {
            }
            i++;
        }
    }
    public void LoadToken(string token, int proxy)
    {
        try
        {
            DiscordSocketClient discordSocketClient = new DiscordSocketClient(null);
            discordSocketClient.Login(token);
            novaClients.Add(new StereoClient(token, discordSocketClient));
        }
        catch (Exception)
        {
        }
    }
    private void trackBar10_Scroll(object sender, EventArgs e)
    {
        label16.Text = "Delay: " + trackBar10.Value.ToString() + "ms";
    }
    public static List<DiscordVoiceStream> streams;
    private void firefoxButton17_Click(object sender, EventArgs e)
    {
        try
        {
            streams = new List<DiscordVoiceStream>();
            if (firefoxCheckBox8.Checked)
            {
                waveIn = new WaveIn();
                waveIn.DeviceNumber = comboBox2.SelectedIndex;
                waveIn.WaveFormat = new WaveFormat((int)numericUpDown1.Value, (int) numericUpDown2.Value, firefoxRadioButton2.Checked ? 2 : 1);
                waveIn.BufferMilliseconds = (int) numericUpDown3.Value;
                waveIn.DataAvailable += WaveIn_DataAvailable;
                waveProvider = new BufferedWaveProvider(waveIn.WaveFormat);
                waveProvider.DiscardOnBufferOverflow = true;
                waveIn.NumberOfBuffers = (int) numericUpDown4.Value;
                waveIn.StartRecording();
            }    
            Thread thread = new Thread(() => doVoiceJoiner());
            thread.Priority = ThreadPriority.Highest;
            thread.Start();
        }
        catch (Exception)
        {
        }
    }
    private void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
    {
        if (firefoxCheckBox8.Checked)
        {
            new Thread(() => passBytes(e.Buffer)).Start();
        }
    }
    public void passBytes(byte[] buffer)
    {
        foreach (DiscordVoiceStream stream in streams)
        {
            try
            {
                stream.CopyFrom(new System.IO.MemoryStream(buffer));
            }
            catch (Exception)
            {
            }
        }
    }
    public void doVoiceJoiner()
    {
        try
        {
            foreach (StereoClient client in novaClients)
            {
                try
                {
                    Thread.Sleep(trackBar10.Value);
                    Thread thread = new Thread(() => joinVoice(client.GetClient()));
                    thread.Priority = ThreadPriority.Highest;
                    thread.Start();
                }
                catch (Exception)
                {
                }
            }
        }
        catch (Exception)
        {
        }
    }
    public void joinVoice(DiscordSocketClient client)
    {
        try
        {
            DiscordVoiceSession session = client.JoinVoiceChannel(new VoiceStateProperties() { ChannelId = ulong.Parse(textBox14.Text), GuildId = ulong.Parse(textBox15.Text), Muted = firefoxCheckBox1.Checked, Deafened = firefoxCheckBox2.Checked, Video = firefoxCheckBox3.Checked});
            session.ReceivePackets = false;
            session.OnConnected += Session_OnConnected;
            session.Connect();
            sessions.Add(session);
        }
        catch (Exception)
        {
        }
    }
    private void firefoxButton18_Click(object sender, EventArgs e)
    {
        try
        {
            Thread thread = new Thread(() => doVoiceLefter());
            thread.Priority = ThreadPriority.Highest;
            thread.Start();
        }
        catch (Exception)
        {
        }
    }
    private void firefoxButton2_Click(object sender, EventArgs e)
    {
        try
        {
            Thread thread = new Thread(() => doJoin());
            thread.Priority = ThreadPriority.Highest;
            thread.Start();
        }
        catch (Exception)
        {
        }
    }
    public void doJoin()
    {
        try
        {
            foreach (StereoClient client in novaClients)
            {
                try
                {
                    client.GetClient().JoinGuild(textBox2.Text);
                }
                catch (Exception)
                {
                }
            }
        }
        catch (Exception)
        {
        }
    }
    private void firefoxButton3_Click(object sender, EventArgs e)
    {
        if (openFileDialog2.ShowDialog() == DialogResult.OK)
        {
            ListViewItem listViewItem = new ListViewItem();
            listViewItem.Text = (sounds.Count + 1).ToString();
            listViewItem.SubItems.Add(openFileDialog2.FileName);
            sounds.Add(openFileDialog2.FileName);
            string soundsFile = System.IO.File.ReadAllText("sounds.txt");
            if (soundsFile == "" || soundsFile == null)
            {
                soundsFile = openFileDialog2.FileName;
            }
            else
            {
                soundsFile += Environment.NewLine + openFileDialog2.FileName;
            }
            System.IO.File.WriteAllText("sounds.txt", soundsFile);
            listView1.Items.Add(listViewItem);
        }
    }
    private void firefoxButton4_Click(object sender, EventArgs e)
    {
        if (listView1.SelectedItems != null)
        {
            if (listView1.SelectedItems.Count == 1)
            {
                ListViewItem item = listView1.SelectedItems[0];
                string path = item.SubItems[1].Text;
                sounds.Remove(path);
                string soundsFile = "";
                foreach (string line in System.IO.File.ReadAllLines("sounds.txt"))
                {
                    if (line != path)
                    {
                        if (soundsFile == "")
                        {
                            soundsFile = line;
                        }
                        else
                        {
                            soundsFile += Environment.NewLine + line;
                        }
                    }
                }
                System.IO.File.WriteAllText("sounds.txt", soundsFile);
                listView1.Items.Remove(item);
            }
        }
    }
    private void firefoxButton5_Click(object sender, EventArgs e)
    {
        if (listView1.SelectedItems != null)
        {
            if (listView1.SelectedItems.Count == 1)
            {
                string path = listView1.SelectedItems[0].SubItems[1].Text;
                foreach (DiscordVoiceStream stream in streams)
                {
                    try
                    {
                        stream.CopyFrom(DiscordVoiceUtils.GetAudioStream(path));
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }
    }
    public void doVoiceLefter()
    {
        try
        {
            foreach (DiscordVoiceSession session in sessions)
            {
                try
                {
                    Thread.Sleep(trackBar10.Value);
                    Thread thread = new Thread(() => leftVoice(session));
                    thread.Priority = ThreadPriority.Highest;
                    thread.Start();
                }
                catch (Exception)
                {
                }
            }
        }
        catch (Exception)
        {
        }
    }
    public void leftVoice(DiscordVoiceSession session)
    {
        for (int i = 0; i < 3; i++)
        {
            Thread.Sleep(250);
            try
            {
                session.Disconnect();
            }
            catch (Exception)
            {
            }
            try
            {
                sessions.Remove(session);
            }
            catch (Exception)
            {
            }
        }
        streams.Clear();
        sessions.Clear();
        waveIn.Dispose();
        waveIn = null;
        waveProvider.ClearBuffer();
        waveProvider = null;
    }
    private void Session_OnConnected(DiscordVoiceSession session, EventArgs e)
    {
        DiscordVoiceStream discordVoiceStream = session.CreateStream(96000u, AudioApplication.Mixed);
        session.SetSpeakingState(DiscordSpeakingFlags.Soundshare);
        streams.Add(discordVoiceStream);
        if (firefoxCheckBox4.Checked)
        {
            session.GoLive();
        }
    }
}
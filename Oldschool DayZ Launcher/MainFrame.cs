using Microsoft.VisualBasic;
using Oldschool_DayZ_Launcher.Properties;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MetroFramework.Controls;
using SteamQueryNet;
using SteamQueryNet.Models;
using SteamKit2;
using static SteamKit2.Internal.CMsgCellList;
using SteamKit2.Discovery;

namespace Oldschool_DayZ_Launcher
{
    public partial class MainFrame : MetroFramework.Forms.MetroForm
    {    
        public static ArrayList filesToDownload = new ArrayList();
        public static ArrayList missingFiles = new ArrayList();
        public static ArrayList existingFiles = new ArrayList();
        public static ArrayList bytesOfFiles = new ArrayList();
        public static ArrayList needToDownload = new ArrayList();
        public static ArrayList folderPath = new ArrayList();
        public static ArrayList missingFolders = new ArrayList();
        public static string fileName;
        public static int fileCounter = 0;
        public static int downloadedFileCounter = 0;

        public static int playersCountGeneral = 0;

        static WebClient wclient = new WebClient();


        public MainFrame()
        {
            InitializeComponent();

            this.FormClosing += MainFrame_FormClosing;

            Discord.Initialize();
        }

        private void MainFrame_FormClosing(object sender, FormClosingEventArgs e)
        {
            Application.Exit();
        }

        private async void MainFrame_Load(object sender, EventArgs e)
        {
            //if (Settings.Default.version != wclient.DownloadString("http://185.223.31.43/Launcher/version"))
            //{
            //    MessageBox.Show("New update available!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            //    Application.Exit();
            //    Process.Start(Application.StartupPath + "\\updater.exe");
            //    return;
            //}
        

            if (Settings.Default.steamToken == "")
            {
                var content = Interaction.InputBox("Go to this site: https://steamcommunity.com/dev/apikey and copy/paste your steam token down below. If its asking for domain name type in 127.0.0.1", "Steam Token", "Enter your token here", -1, -1);
                if (content != null)
                {
                    Settings.Default.steamToken = content;
                    Settings.Default.Save();
                }
            }
            listView1.MouseClick += listView1_MouseClick;


            if (Settings.Default.gamePath == "")
            {
                using (var fbd = new FolderBrowserDialog())
                {
                    fbd.Description = "Select the path where you want to install the game or the path of already existing files!";
                    DialogResult result = fbd.ShowDialog();


                    if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                    {
                        Settings.Default.gamePath = fbd.SelectedPath;
                        Settings.Default.Save();
                        this.Controls.Remove(settingsTab);
                        settingsState = false;
                      //  MessageBox.Show("Gamepath saved!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                      //  gettingsFiles();
                    }
                }           
            }
            else
            {
              //  gettingsFiles();
            }


            listView1.Visible = true;
            pictureBox2.Visible = true;
            pictureBox3.Visible = true;
            serverLoader();
            //  gettingsFiles();          
        }

      

        private void listView1_MouseClick(object sender, MouseEventArgs e)
        {
            //for (int i = 0; i < listView1.Items.Count; i++)
            //{
            //    ListViewItem item = listView1.Items[i];
            //    Rectangle itemRect = item.GetBounds(ItemBoundsPortion.Label);
            //    if (itemRect.Contains(e.Location))
            //    {
            //        if (getVersionByAddr(item.Name) != "0.62.140099")
            //        {
            //            MessageBox.Show("This version is not supported!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            //            break;
            //        }
            //        else
            //        {                                                               
            //            startGame(item.Name);                      
            //            Discord.changeDiscordRPC(item.Text, "Playing on", "OSD Launcher", "logo");
            //            break;
            //        }                  
            //    }
            //}
        }

        private void T_Tick(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void gettingsFiles()
        {
            metroLabel1.Text = "Status: Getting file list...";
            wclient.DownloadFile(Settings.Default.serverURLfiles, Settings.Default.gamePath + "\\files.txt");
            var table = File.ReadAllLines(Settings.Default.gamePath + "\\files.txt");
            foreach (string fileName2 in table)
            {
                if (fileName2 != "")
                {
                    filesToDownload.Add(fileName2);
                }
            }
            getFolders();
        }

        //step 1.5 get the folders and create the missing ones

        private void getFolders()
        {
            metroLabel1.Text = "Status: Getting folder list...";
            wclient.DownloadFile(Settings.Default.serverURLfolders, Settings.Default.gamePath + "\\folders.txt");
            var folder = File.ReadAllLines(Settings.Default.gamePath + "\\folders.txt");
            foreach (string folderName in folder)
            {
                if (folderName != "")
                {
                    folderPath.Add(folderName);
                }
            }
            getMissingFolder();
        }

        private void getMissingFolder()
        {

            metroLabel1.Text = "Status: Check missing folders...";
            foreach (string folderName in folderPath)
            {
                if (folderName != "")
                {
                    if (!Directory.Exists(Settings.Default.gamePath + "\\" + folderName))
                    {
                        missingFolders.Add(folderName);
                    }
                }
            }
            createMissingFolders();
        }

        private void createMissingFolders()
        {

            metroLabel1.Text = "Status: Create missing folders...";
            foreach (string missingFolderName in missingFolders)
            {
                if (missingFolderName != "")
                {
                    Directory.CreateDirectory(Settings.Default.gamePath + "\\" + missingFolderName);
                }
            }
            checkNotExistsFiles();
        }

        //step 2 check if already some file exists and add the not existing files to a new array

        private void checkNotExistsFiles()
        {

            metroLabel1.Text = "Status: Check missing files...";
            foreach (string file in filesToDownload)
            {
                // MessageBox.Show(Application.StartupPath + "/" + file);
                if (file != "")
                {
                    // MessageBox.Show(Application.StartupPath + "\\" + file);
                    if (!File.Exists(Settings.Default.gamePath + "\\" + file))
                    {
                        //  MessageBox.Show(file);
                        missingFiles.Add(file);
                    }
                }
            }
            FileBytes();
        }



        //step 3 getting file bytes

        private void FileBytes()
        {

            metroLabel1.Text = "Status: Getting file bytes...";
            wclient.DownloadFile(Settings.Default.serverURLbytes, Settings.Default.gamePath + "\\bytes.txt");
            var table = File.ReadAllLines(Settings.Default.gamePath + "\\bytes.txt");
            foreach (string byteS in table)
            {
                if (byteS != "")
                {
                    bytesOfFiles.Add(byteS);
                }
            }
            byteChecker();
        }

        //step 4 check the bytes of the existing files


        private void byteChecker()
        {

            metroLabel1.Text = "Status: Check bytes of existing files...";
            foreach (string fileBytes in filesToDownload)
            {             
                if (File.Exists(Settings.Default.gamePath + "\\" + fileBytes))
                {
                    if (FileSystem.FileLen(Settings.Default.gamePath + "\\" + fileBytes).ToString() != bytesOfFiles[0].ToString())
                    {
                        needToDownload.Add(fileBytes);
                        bytesOfFiles.RemoveAt(0);
                    }
                    else
                    {
                        bytesOfFiles.RemoveAt(0);
                    }
                }
                else
                {
                    needToDownload.Add(fileBytes);
                    bytesOfFiles.RemoveAt(0);
                }
            }
            fileCount();
            // downloader
        }

        //step 4.5 counting files

        private void fileCount()
        {

            foreach (string file in needToDownload)
            {
                // fileCounter++;
                downloadedFileCounter++;
            }
            downloader();
        }

        //step 5 Download the missing Files

        private void downloader()
        {

            if (fileCounter < downloadedFileCounter)
            {
                metroLabel1.Text = "Status: Downloading missing files...";
                using (WebClient wc = new WebClient())
                {                 
                    wc.DownloadProgressChanged += wc_DownloadProgressChanged;
                    wc.DownloadFileCompleted += wc_DownloadFileCompleted;                  
                    metroLabel2.Text = needToDownload[0].ToString();
                    metroLabel3.Text = fileCounter.ToString() + " / " + downloadedFileCounter;
                    wc.DownloadFileAsync(new Uri(Settings.Default.serverURLdownload + needToDownload[0].ToString()), Settings.Default.gamePath + "\\" + needToDownload[0].ToString());
                }
            }
            else if (fileCounter >= downloadedFileCounter)
            {
        
                metroLabel1.Text = "Download Finished!";
                listView1.Visible = true;
                metroLabel2.Visible = false;
                metroLabel1.Visible = false;
                metroLabel3.Visible = false;
                metroProgressBar1.Visible = false;
                metroLabel4.Visible = false;
                serverLoader();
                pictureBox2.Visible = true;
                pictureBox3.Visible = true;


                metroProgressBar1.Value = 100;
                filesToDownload.Clear();
                missingFiles.Clear();
                existingFiles.Clear();
                bytesOfFiles.Clear();
                needToDownload.Clear();
                folderPath.Clear();
                missingFolders.Clear();
            }
            else
            {
                MessageBox.Show("Error!");
                return;
            }
        }

        private void wc_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            needToDownload.RemoveAt(0);
            fileCounter++;          

            if (fileCounter < downloadedFileCounter)
            {
                downloader();
            }
            else
            {
                listView1.Visible = true;
                metroLabel1.Text = "Download finished!";
                metroLabel1.Visible = false;
                metroLabel2.Visible = false;
                metroLabel3.Visible = false;
                metroProgressBar1.Visible = false;
                metroLabel4.Visible = false;
                serverLoader();
                pictureBox2.Visible = true;
                pictureBox3.Visible = true;


                metroProgressBar1.Value = 100;            
                filesToDownload.Clear();
                missingFiles.Clear();
                existingFiles.Clear();
                bytesOfFiles.Clear();
                needToDownload.Clear();
                folderPath.Clear();
                missingFolders.Clear();
            }
        }

        private void wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            metroProgressBar1.Value = e.ProgressPercentage;
            long totalbytes = e.TotalBytesToReceive / 1024 / 1024;
            long totalbytesKB = e.TotalBytesToReceive / 1024;
            long bytes = e.BytesReceived / 1024 / 1024;
            long gbbytes = e.BytesReceived / 1024 / 1024 / 1024;
            long totalbytesGB = e.TotalBytesToReceive / 1024 / 1024 / 1024;
            long bytesKB = e.BytesReceived / 1024;
            if (e.BytesReceived >= 999)
            {
                metroLabel4.Text = bytes.ToString() + " / " + totalbytes.ToString() + " MB ";
            }
            else if (e.BytesReceived < 999)
            {
                metroLabel4.Text = bytesKB.ToString() + " / " + totalbytesKB.ToString() + " KB ";
            }
            else if (e.BytesReceived >= 9999)
            {
                metroLabel4.Text = gbbytes.ToString() + " / " + totalbytesGB.ToString() + " GB ";
            }
        }

       


        private void startGame (string ip)
        {
            stopGame();
            string startParameter = "-connect #ip#";

            if (File.Exists(Settings.Default.gamePath + "\\DayZ_x64.exe"))
            {
                startParameter = startParameter.Replace("#ip#", ip);
                Process.Start(Settings.Default.gamePath + "\\DayZ_x64.exe", startParameter);
            }
            //else
            //{
            //    var result = MessageBox.Show("Cant find DayZ_x64.exe in your selected gamepath! Do you want to download dayz into '" + Settings.Default.gamePath + "'?", " Error", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            //    if (result == DialogResult.Yes)
            //    {
            //        listView1.Visible = false;
            //        metroLabel2.Visible = true;
            //        metroLabel1.Visible = true;
            //        metroLabel3.Visible = true;
            //        metroProgressBar1.Visible = true;
            //        metroLabel4.Visible = true;                 
            //        pictureBox2.Visible = false;
            //        pictureBox3.Visible = false;
            //        gettingsFiles();
            //    }
          //  }
        }


        private string getVersionByAddr(string addr)
        {
            string version = "";
            string response = wclient.DownloadString("https://api.steampowered.com/IGameServersService/GetServerList/v1/?key=" + Settings.Default.steamToken + "&filter=appid\\221100\\version_match\\0.62.140099");       

            if (response != "{'response':{}}")
            {
                var serverlist = JObject.Parse(response);

                foreach (var table in serverlist["response"]["servers"])
                {
                    if (table["addr"].ToString().Split(Convert.ToChar(":"))[0] + ":" + table["gameport"].ToString() == addr)
                    {
                        version = table["version"].ToString();
                        break;
                    }
                }
            }                           
            return version;
        }



        private string getNameByAddr(string addr)
        {
            string name = "";
            string response = wclient.DownloadString("https://api.steampowered.com/IGameServersService/GetServerList/v1/?key=" + Settings.Default.steamToken + "&filter=appid\\221100\\version_match\\0.62.140099");

            if (response != "{'response':{}}")
            {
                var serverlist = JObject.Parse(response);

                foreach (var table in serverlist["response"]["servers"])
                {
                    if (table["addr"].ToString().Split(Convert.ToChar(":"))[0] + ":" + table["gameport"].ToString() == addr)
                    {
                        name = table["name"].ToString();
                        break;
                    }
                }
            }
            return name;
        }



        // Control[] playerlistButtons = {};


        private void serverLoader()
        {
            metroLabel2.Visible = false;
            metroLabel1.Visible = false;
            metroLabel3.Visible = false;
            metroProgressBar1.Visible = false;
            metroLabel4.Visible = false;


            try
            {
                playersCountGeneral = 0;
                string response = wclient.DownloadString("https://api.steampowered.com/IGameServersService/GetServerList/v1/?key=" + Settings.Default.steamToken + "&filter=appid\\221100\\version_match\\0.62.140099");
                var serverlist = JObject.Parse(response);

                if (serverlist["response"].ToString() != "{}")
                {
                    foreach (var table in serverlist["response"]["servers"])
                    {


                        ListViewItem item = new ListViewItem(table["name"].ToString());
                        item.SubItems.Add(table["players"].ToString() + " / " + table["max_players"].ToString());
                        item.SubItems.Add(table["map"].ToString());
                        item.SubItems.Add(table["version"].ToString());
                        item.SubItems.Add("");
                        item.SubItems.Add("");
                        //  item.Name = table["addr"].ToString().Split(Convert.ToChar(":"))[0] + ":" + table["gameport"].ToString();
                        listView1.Items.Add(item);
                        


                        Button testButton = new Button();
                        testButton.Text = "";
                        testButton.BackgroundImage = Oldschool_DayZ_Launcher.Properties.Resources.user1;
                        testButton.BackgroundImageLayout = ImageLayout.Stretch;
                        testButton.BackColor = Color.Transparent;
                       // testButton.Style = MetroFramework.MetroColorStyle.Silver;
                       // testButton.Theme = MetroFramework.MetroThemeStyle.Dark;
                        testButton.Tag = table["addr"].ToString();
                        testButton.Cursor = Cursors.Hand;
                        testButton.FlatStyle = FlatStyle.Flat;
                        testButton.FlatAppearance.BorderSize = 0;
                        testButton.Size = new Size(item.SubItems[4].Bounds.Size.Width, item.SubItems[4].Bounds.Size.Height);
                        testButton.Location = new Point(item.SubItems[4].Bounds.Location.X, item.SubItems[4].Bounds.Location.Y);
                        testButton.Click += PlayerList_Click;



                        Button playerButton = new Button();
                        playerButton.Text = "";
                        playerButton.BackgroundImage = Oldschool_DayZ_Launcher.Properties.Resources.play;
                        playerButton.BackgroundImageLayout = ImageLayout.Stretch;
                        playerButton.BackColor = Color.Transparent;
                        playerButton.FlatStyle = FlatStyle.Flat;
                        playerButton.FlatAppearance.BorderSize =0;
                        //  playerButton.Style = MetroFramework.MetroColorStyle.Silver;
                        //  playerButton.Theme = MetroFramework.MetroThemeStyle.Dark;
                        playerButton.Tag = table["addr"].ToString().Split(Convert.ToChar(":"))[0] + ":" + table["gameport"].ToString(); ;
                        playerButton.Cursor = Cursors.Hand;
                        playerButton.Size = new Size(item.SubItems[5].Bounds.Size.Width, item.SubItems[5].Bounds.Size.Height);
                        playerButton.Location = new Point(item.SubItems[5].Bounds.Location.X, item.SubItems[5].Bounds.Location.Y);                       
                        playerButton.Click += PlayButton_Click;




                        listView1.Controls.Add(testButton);
                        listView1.Controls.Add(playerButton);
                        playersCountGeneral = playersCountGeneral + Convert.ToInt32(table["players"].ToString());

                    }
                }
                this.Text = "Oldschool DayZ - " + playersCountGeneral.ToString() + " players online";
                this.Refresh();
            }
            catch(Exception e)
            {
                MessageBox.Show("Looks like your steam token is invalid! Check your launcher settings", "Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
                  
        }

        private void PlayButton_Click(object sender, EventArgs e)
        {
            Button clickedButton = sender as Button;
            if (clickedButton != null)
            {
                Console.WriteLine(clickedButton.Tag.ToString());


                if (getVersionByAddr(clickedButton.Tag.ToString()) != "0.62.140099")
                {
                    MessageBox.Show("This version is not supported!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);            
                }
                else
                {
                    startGame(clickedButton.Tag.ToString());
                    Discord.changeDiscordRPC(getNameByAddr(clickedButton.Tag.ToString()), "Playing on", "OSD Launcher", "logo");              
                }
            }
        }

        private void PlayerList_Click(object sender, EventArgs e)
        {
            _ = Task.Run(async () =>
            {
                Button clickedButton = sender as Button;
                if (clickedButton != null)
                {
                    StringBuilder sb = new StringBuilder();
                    string ip = clickedButton.Tag.ToString();



                    var playerinfo = new List<Player>();

                    using (var serverConnector = new ServerQuery())
                    {
                        serverConnector.Connect(ip.Split(Convert.ToChar(":"))[0], Convert.ToUInt16(ip.Split(Convert.ToChar(":"))[1]));

                        int timeout = 2000;
                        var task = serverConnector.GetPlayersAsync();
                        if (await Task.WhenAny(task, Task.Delay(timeout)) == task)
                        {
                            playerinfo = task.Result;
                        }
                        else
                        {
                            Player player = new Player();
                            player.Name = "Cant fetch playerlist | Port closed";
                            playerinfo.Add(player);
                        }
                    }


                    foreach (Player player in playerinfo)
                    {
                        sb.AppendLine(player.Name + " | Playtime: " + player.TotalDurationAsString);
                    }



                    sb.AppendLine("");
                    sb.AppendLine("");

                    MessageBox.Show(sb.ToString(), "Playerlist", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            });
          //  return Task.CompletedTask;
        }
            

        //private void metroButton1_Click(object sender, EventArgs e)
        //{
        //    listView1.Items.Clear();
        //    listView1.Controls.Clear();
        //    serverLoader();
        //}

        private void metroListView1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void stopGame()
        {
            Process[] prozess = Process.GetProcessesByName("DayZ_x64");
            foreach (Process pr in prozess)
            {
                pr.Kill();
            }

            Discord.changeDiscordRPC("Running Launcher", "", "OSD Launcher", "logo");
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            listView1.Items.Clear();
            listView1.Controls.Clear();
            serverLoader();
        }

        private void pictureBox3_Click(object sender, EventArgs e)
        {
            createSettingsTabWithAnimation();
        }
         public static MetroPanel settingsTab { get; set; }

         public static bool settingsState = false;
        

        private void createSettingsTabWithAnimation()
        {
            if (settingsState == false)
            {
                //string gamePathDirectory = String.Empty;
                //if (Settings.Default.gamePath != "")
                //{
                //    gamePathDirectory = Settings.Default.gamePath;
                //}
                //else
                //{
                //    gamePathDirectory = Application.StartupPath;
                //}


                Color c = Color.FromArgb(17, 17, 17);
                PictureBox closeButton = new PictureBox();
                MetroPanel settingsPanel = new MetroPanel();
                MetroButton gamePath = new MetroButton();
                MetroLabel gamePathLabel = new MetroLabel();
                MetroButton changeGamePath = new MetroButton();
                MetroTextBox steamToken = new MetroTextBox();
                MetroLabel steamTokenLabel = new MetroLabel();

                MetroButton saveSteamToken = new MetroButton();



                settingsPanel.Size = new Size(487, 626);
                settingsPanel.Location = new Point(685, 25);
                settingsPanel.Theme = MetroFramework.MetroThemeStyle.Dark;
                settingsPanel.Style = MetroFramework.MetroColorStyle.Silver;


                closeButton.Size = new Size(32, 32);
                closeButton.Location = new Point(0, 0);
                closeButton.Image = Oldschool_DayZ_Launcher.Properties.Resources.close;
                closeButton.BackColor = c;
                closeButton.Click += closeSettingsTab;
                closeButton.Cursor = Cursors.Hand;

                gamePath.Location = new Point(40, 18);
                gamePath.Size = new Size(250, 25);
                gamePath.Text = Settings.Default.gamePath;
                gamePath.Enabled = false;
                gamePath.Style = MetroFramework.MetroColorStyle.Silver;
                gamePath.Theme = MetroFramework.MetroThemeStyle.Dark;


                changeGamePath.Location = new Point(40, 47);
                changeGamePath.Size = new Size(250, 25);
                changeGamePath.Text = "Change game path";
                changeGamePath.Enabled = true;
                changeGamePath.Style = MetroFramework.MetroColorStyle.Silver;
                changeGamePath.Theme = MetroFramework.MetroThemeStyle.Dark;
                changeGamePath.Click += ChangeGamePath_Click;
                changeGamePath.Cursor = Cursors.Hand;


                gamePathLabel.Location = new Point(40, 0);
                gamePathLabel.Size = new Size(250, 25);
                gamePathLabel.Text = "Game Path";
                gamePathLabel.Enabled = false;
                gamePathLabel.Style = MetroFramework.MetroColorStyle.Silver;
                gamePathLabel.Theme = MetroFramework.MetroThemeStyle.Dark;



                ///

                steamToken.Location = new Point(40, 120);
                steamToken.Size = new Size(250, 25);
                steamToken.Text = Settings.Default.steamToken;
                steamToken.Enabled = true;
                steamToken.Style = MetroFramework.MetroColorStyle.Silver;
                steamToken.Theme = MetroFramework.MetroThemeStyle.Dark;


                steamTokenLabel.Location = new Point(40, 95);
                steamTokenLabel.Size = new Size(250, 25);
                steamTokenLabel.Text = "Steam API token";
                steamTokenLabel.Enabled = false;
                steamTokenLabel.Style = MetroFramework.MetroColorStyle.Silver;
                steamTokenLabel.Theme = MetroFramework.MetroThemeStyle.Dark;


                saveSteamToken.Location = new Point(40, 150);
                saveSteamToken.Size = new Size(250, 25);
                saveSteamToken.Text = "Save steam token";
                saveSteamToken.Enabled = true;
                saveSteamToken.Style = MetroFramework.MetroColorStyle.Silver;
                saveSteamToken.Theme = MetroFramework.MetroThemeStyle.Dark;
                saveSteamToken.Click += SaveSteamToken_Click; ;
                saveSteamToken.Cursor = Cursors.Hand;



                settingsPanel.Controls.Add(saveSteamToken);
                settingsPanel.Controls.Add(steamTokenLabel);
                settingsPanel.Controls.Add(steamToken);
                settingsPanel.Controls.Add(changeGamePath);
                settingsPanel.Controls.Add(gamePath);
                settingsPanel.Controls.Add(gamePathLabel);
                settingsPanel.Controls.Add(closeButton);

                Controls.Add(settingsPanel);
                settingsPanel.BringToFront();
                settingsTab = settingsPanel;

                settingsState = true;
            }
        }

        private void SaveSteamToken_Click(object sender, EventArgs e)
        {
            foreach (Control ctrl in settingsTab.Controls)
            {
                if (ctrl.ToString() == "MetroFramework.Controls.MetroTextBox")
                {             
                    Settings.Default.steamToken = ctrl.Text;
                    Settings.Default.Save();
                    MessageBox.Show("Steam token saved!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void ChangeGamePath_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    Settings.Default.gamePath = fbd.SelectedPath;
                    Settings.Default.Save();
                    this.Controls.Remove(settingsTab);
                    settingsState = false;
                    MessageBox.Show("Gamepath saved!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }       
        }

        private void closeSettingsTab(object sender, EventArgs e)
        {
            this.Controls.Remove(settingsTab);
            settingsState = false;
        }

        private void getServerNameByIP()
        {

        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}

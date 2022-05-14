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

namespace Oldschool_DayZ_Launcher
{
    public partial class MainFrame : MetroFramework.Forms.MetroForm
    {
        public static string[] ipList = { "158.69.22.190", "185.223.31.43", "193.110.160.36" };
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

        static WebClient wclient = new WebClient();


        public MainFrame()
        {
            InitializeComponent();

            this.FormClosing += MainFrame_FormClosing;
        }

        private void MainFrame_FormClosing(object sender, FormClosingEventArgs e)
        {
            Application.Exit();
        }

        private void MainFrame_Load(object sender, EventArgs e)
        {
            if (Settings.Default.version != wclient.DownloadString("http://185.223.31.43/Launcher/version"))
            {
                MessageBox.Show("New update available!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Application.Exit();
                Process.Start(Application.StartupPath + "\\updater.exe");
                return;
            }



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

           // listView1.Visible = true;
          //  pictureBox2.Visible = true;
          //  pictureBox3.Visible = true; 
          //  serverLoader();
             gettingsFiles();          
        }

        private void listView1_MouseClick(object sender, MouseEventArgs e)
        {
            for (int i = 0; i < listView1.Items.Count; i++)
            {
                ListViewItem item = listView1.Items[i];
                Rectangle itemRect = item.GetBounds(ItemBoundsPortion.Label);
                if (itemRect.Contains(e.Location))
                {
                    if (getVersionByAddr(item.Name) != "0.62.140099")
                    {               
                        MessageBox.Show("This version is not supported!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        break;
                    }
                    else
                    {                      
                        startGame(item.Name);
                        break;
                    }                  
                }
            }
        }
       

        private void gettingsFiles()
        {
            metroLabel1.Text = "Status: Getting file list...";
            wclient.DownloadFile(Settings.Default.serverURLfiles, Application.StartupPath + "\\files.txt");
            var table = File.ReadAllLines(Application.StartupPath + "\\files.txt");
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
            wclient.DownloadFile(Settings.Default.serverURLfolders, Application.StartupPath + "\\folders.txt");
            var folder = File.ReadAllLines(Application.StartupPath + "\\folders.txt");
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
                    if (!Directory.Exists(Application.StartupPath + "\\" + folderName))
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
                    Directory.CreateDirectory(Application.StartupPath + "\\" + missingFolderName);
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
                    if (!File.Exists(Application.StartupPath + "\\" + file))
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
            wclient.DownloadFile(Settings.Default.serverURLbytes, Application.StartupPath + "\\bytes.txt");
            var table = File.ReadAllLines(Application.StartupPath + "\\bytes.txt");
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
                if (File.Exists(Application.StartupPath + "\\" + fileBytes))
                {
                    if (FileSystem.FileLen(fileBytes).ToString() != bytesOfFiles[0].ToString())
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
                    wc.DownloadFileAsync(new Uri(Settings.Default.serverURLdownload + needToDownload[0].ToString()), Application.StartupPath + "\\" + needToDownload[0].ToString());
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

            if (File.Exists(Application.StartupPath + "\\DayZ_x64.exe"))
            {
                startParameter = startParameter.Replace("#ip#", ip);
                Process.Start(Application.StartupPath + "\\DayZ_x64.exe", startParameter);
            }
        }


        private string getVersionByAddr(string addr)
        {
            string version = "";
            string response = wclient.DownloadString("https://api.steampowered.com/IGameServersService/GetServerList/v1/?key=" + Settings.Default.steamToken + "&filter=appid\\221100\\version_match\\0.62.140099");
           // string response = wclient.DownloadString("https://api.steampowered.com/IGameServersService/GetServerList/v1/?key=" + Settings.Default.steamToken + "&filter=addr\\" + addr.Split(Convert.ToChar(":"))[0]);

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

        private void serverLoader()
        {
            try
            {


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
                        item.Name = table["addr"].ToString().Split(Convert.ToChar(":"))[0] + ":" + table["gameport"].ToString();
                        listView1.Items.Add(item);
                    }
                }
            }
            catch(Exception e)
            {
                MessageBox.Show("Looks like your steam token is invalid! Check your launcher settings", "Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
                  
        }

        private void metroButton1_Click(object sender, EventArgs e)
        {
            listView1.Items.Clear();
            serverLoader();
        }

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
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            listView1.Items.Clear();
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
                gamePath.Text = "";
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
            MessageBox.Show("Not implemented yet! I will add it in the next update", "Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void closeSettingsTab(object sender, EventArgs e)
        {
            this.Controls.Remove(settingsTab);
            settingsState = false;
        }
    }
}

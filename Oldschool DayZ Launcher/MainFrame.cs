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
        }

        private void MainFrame_Load(object sender, EventArgs e)
        {
            if (Settings.Default.steamToken == "")
            {              
                var content = Interaction.InputBox("Go to this site: https://steamcommunity.com/dev/apikey and copy/paste your steam token down below", "Steam Token", "Enter your token here", -1, -1);               
                if (content != null)
                {             
                    Settings.Default.steamToken = content;
                    Settings.Default.Save();                 
                }             
            }     
            listView1.MouseClick += listView1_MouseClick;

            //listView1.Visible = true;
           // serverLoader();
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
                metroButton1.Visible = true;


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
                metroButton1.Visible = true;


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
            if (File.Exists(Application.StartupPath + "\\DayZ_x64.exe"))
            {
                Process.Start(Application.StartupPath + "\\DayZ_x64.exe", "-connect " + ip);
            }
        }


        private string getVersionByAddr(string addr)
        {
            string version = "";
            string response = wclient.DownloadString("https://api.steampowered.com/IGameServersService/GetServerList/v1/?key=" + Settings.Default.steamToken + "&filter=addr\\158.69.22.190");
            var serverlist = JObject.Parse(response);

            foreach (var table in serverlist["response"]["servers"])
            {
                if (table["addr"].ToString() == addr)
                {
                    version = table["version"].ToString();
                    break;
                }
            }
            return version;
        }

        private void serverLoader()
        {
            string response = wclient.DownloadString("https://api.steampowered.com/IGameServersService/GetServerList/v1/?key=" + Settings.Default.steamToken  + "&filter=addr\\158.69.22.190");         
            var serverlist = JObject.Parse(response);
            
            foreach (var table in serverlist["response"]["servers"])
            {             
                ListViewItem item = new ListViewItem(table["name"].ToString());
                item.SubItems.Add(table["players"].ToString() + " / " + table["max_players"].ToString());
                item.SubItems.Add(table["map"].ToString());
                item.SubItems.Add(table["version"].ToString());
                item.Name = table["addr"].ToString();       
                listView1.Items.Add(item);
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
    }
}

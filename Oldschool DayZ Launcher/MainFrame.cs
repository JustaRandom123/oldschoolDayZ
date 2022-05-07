﻿using Microsoft.VisualBasic;
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
            gettingsFiles();
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
                metroButton1.Visible = true;
                metroLabel2.Visible = false;
                metroLabel1.Visible = false;
                metroLabel3.Visible = false;
                metroProgressBar1.Visible = false;
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
                metroButton1.Visible = true;
                metroLabel1.Text = "Download finished!";
                metroLabel1.Visible = false;
                metroLabel2.Visible = false;
                metroLabel3.Visible = false;
                metroProgressBar1.Visible = false;
                

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
            //long totalbytes = e.TotalBytesToReceive / 1024 / 1024;
            //long totalbytesKB = e.TotalBytesToReceive / 1024;
            //long bytes = e.BytesReceived / 1024 / 1024;
            //long gbbytes = e.BytesReceived / 1024 / 1024 / 1024;
            //long totalbytesGB = e.TotalBytesToReceive / 1024 / 1024 / 1024;
            //long bytesKB = e.BytesReceived / 1024;
            //if (e.BytesReceived >= 999)
            //{
            //    label5.Text = bytes.ToString() + " / " + totalbytes.ToString() + " MB ";
            //}
            //else if (e.BytesReceived < 999)
            //{
            //    label5.Text = bytesKB.ToString() + " / " + totalbytesKB.ToString() + " KB ";
            //}
            //else if (e.BytesReceived >= 9999)
            //{
            //    label5.Text = gbbytes.ToString() + " / " + totalbytesGB.ToString() + " GB ";
            //}
        }

        private void metroButton1_Click(object sender, EventArgs e)
        {
            if (File.Exists(Application.StartupPath + "\\DayZ_x64.exe"))
            {
                Process.Start(Application.StartupPath + "\\DayZ_x64.exe", "-connect 158.69.22.190:2302");
            }
        }
    }
}

﻿using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace Ship_Game
{
    public partial class ExceptionViewer : Form
    {
        public ExceptionViewer()
        {
            InitializeComponent();
        }

        public string Description
        {
            set => descrLabel.Text = value;
        }

        public string Error
        {
            get => tbError.Text;
            set
            {
                tbError.Text = value.Replace("\n", "\r\n");
                tbError.Select(0, 0);
            }
        }
        [STAThread]
        private void btClip_Click(object sender, EventArgs e)
        {
            string all = tbError.Text + "\n\nUser Comment: " + tbComment.Text;
            Clipboard.SetText(all);
        }

        private void btClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btOpenBugTracker_Click(object sender, EventArgs e)
        {
            if(!ExceptionTracker.Kudos)
            Process.Start(ExceptionTracker.BugtrackerURL);
            else
            {
                ExceptionTracker.Kudos = false;
                Process.Start(ExceptionTracker.KudosURL);
            }
        }

        public static void ShowExceptionDialog(string dialogText, bool autoReport)
        {
            var view = new ExceptionViewer();

            if (autoReport)
            {
                view.Description =
                    "This error was submitted automatically to our exception tracking system. \r\n" +
                    "If this error keeps reoccurring, you can add comments and create a new issue on BitBucket.";
            }
            else
            {
                view.Description = 
                    "Automatic error reporting is disabled. \r\n" +
                    "If this error keep reoccurring, you can add comments and create a new issue on BitBucket.";
            }

            view.Error = dialogText;
            view.ShowDialog();
        }
    }
}

﻿// COPYRIGHT 2014 by the Open Rails project.
// 
// This file is part of Open Rails.
// 
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

using ORTS.Common;
using ORTS.Updater;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace Updater
{
    public partial class UpdaterProgress : Form
    {
        string BasePath;
        string LauncherPath;

        public UpdaterProgress()
        {
            InitializeComponent();

            // Windows 2000 and XP should use 8.25pt Tahoma, while Windows
            // Vista and later should use 9pt "Segoe UI". We'll use the
            // Message Box font to allow for user-customizations, though.
            Font = SystemFonts.MessageBoxFont;

            BasePath = Path.GetDirectoryName(Application.ExecutablePath);
            LauncherPath = Path.Combine(BasePath, "OpenRails.exe");
        }

        void UpdaterProgress_Load(object sender, EventArgs e)
        {
            new Thread(UpdaterThread).Start();
        }

        void UpdaterThread()
        {
            // We wait for any processes identified by /WAITPID=<pid> to exit before starting up so that the updater
            // will not try and apply an update whilst the previous instance is still lingering.
            var waitPids = Environment.GetCommandLineArgs().Where(a => a.StartsWith("/WAITPID="));
            foreach (var waitPid in waitPids)
            {
                try
                {
                    var process = Process.GetProcessById(int.Parse(waitPid.Substring(9)));
                    while (!process.HasExited)
                        process.WaitForExit(100);
                    process.Close();
                }
                catch (ArgumentException)
                {
                    // ArgumentException occurs if we try and GetProcessById with an ID that has already exited.
                }
            }

            // Update manager is needed early to apply any updates before we show UI.
            var updateManager = new UpdateManager(BasePath, Application.ProductName, VersionInfo.VersionOrBuild);
            updateManager.ApplyProgressChanged += (object sender, ProgressChangedEventArgs e) =>
            {
                Invoke((Action)(() =>
                {
                    progressBarUpdater.Value = e.ProgressPercentage;
                }));
            };

            updateManager.Check();
            if (updateManager.LastCheckError != null)
            {
                if (!IsDisposed)
                {
                    Invoke((Action)(() =>
                    {
                        MessageBox.Show("Error: " + updateManager.LastCheckError, Application.ProductName + " " + VersionInfo.VersionOrBuild, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }));
                }
                Application.Exit();
                return;
            }

            updateManager.Apply();
            if (updateManager.LastUpdateError != null)
            {
                if (!IsDisposed)
                {
                    Invoke((Action)(() =>
                    {
                        MessageBox.Show("Error: " + updateManager.LastUpdateError, Application.ProductName + " " + VersionInfo.VersionOrBuild, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }));
                }
                Application.Exit();
                return;
            }

            var appProcess = Process.Start(LauncherPath);
            appProcess.WaitForExit();
            Environment.Exit(0);
        }

        void UpdaterProgress_FormClosed(object sender, FormClosedEventArgs e)
        {
            Process.Start(LauncherPath);
        }
    }
}
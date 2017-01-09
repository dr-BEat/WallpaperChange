﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using Squirrel;
using WallpaperChange.Settings;
using Timer = System.Timers.Timer;

namespace WallpaperChange
{
    internal class SysTrayApplicationContext : ApplicationContext
    {
        private readonly object _syncLock = new object();
        private ToolStripMenuItem _btnAbout;
        private ToolStripMenuItem _btnSettings;
        private ToolStripMenuItem _btnStop;
        private ContextMenuStrip _contextMenuStrip1;
        private FileAtTime _currentFileAtTime;
        private NotifyIcon _notifyIcon1;
        private Timer _timer;
        private WallpaperChanger _wallpaperChanger;

        private IContainer components;

        public SysTrayApplicationContext()
        {
            InitializeComponent();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new Container();
            _notifyIcon1 = new NotifyIcon(components);
            _contextMenuStrip1 = new ContextMenuStrip(components);
            _btnAbout = new ToolStripMenuItem();
            _btnStop = new ToolStripMenuItem();
            _btnSettings = new ToolStripMenuItem();
            _contextMenuStrip1.SuspendLayout();
            _notifyIcon1.ContextMenuStrip = _contextMenuStrip1;
            var imgStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("WallpaperChange.favicon.ico");
            if (imgStream != null)
            {
                var ico = new Icon(imgStream);
                _notifyIcon1.Icon = ico;
            }
            _notifyIcon1.Tag = "WallpaperChange";
            _notifyIcon1.Text = @"WallpaperChange";
            _notifyIcon1.Visible = true;
            _contextMenuStrip1.Items.AddRange(new ToolStripItem[] {_btnAbout, _btnSettings, _btnStop});
            _contextMenuStrip1.Name = "_contextMenuStrip1";
            _contextMenuStrip1.Size = new Size(176, 76);
            _btnAbout.Name = "_btnAbout";
            _btnAbout.AutoSize = true;
            _btnAbout.Text = @"About";
            _btnAbout.Click += btnAbout_Click;
            _btnStop.Name = "_btnStop";
            _btnStop.AutoSize = true;
            _btnStop.Text = @"Stop";
            _btnStop.Click += btnStop_Click;
            _btnSettings.Name = "_btnSettings";
            _btnSettings.Text = @"Settings";
            _btnSettings.AutoSize = true;
            _btnSettings.Click += btnSettings_Click;
            _contextMenuStrip1.ResumeLayout(false);
            _timer = new Timer();
            _timer.AutoReset = false;
            _timer.Elapsed += _timer_Elapsed;
            _timer.Interval = 10000;
            _timer.Start();
        }

        private void btnAbout_Click(object sender, EventArgs e)
        {
            new AboutBox().ShowDialog();
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                lock (_syncLock)
                {
                    if (_wallpaperChanger == null)
                    {
                        _wallpaperChanger = new WallpaperChanger();
                    }
                    else
                    {
                        return;
                    }
                }
                _currentFileAtTime = _wallpaperChanger.Start(_currentFileAtTime);
                _wallpaperChanger = null;
            }
            finally
            {
                _timer.Start();
            }
        }

        private async Task CheckForUpdates()
        {
            if (File.Exists("..\\Update.exe"))
            {
                using (var mgr = new UpdateManager("http://tamarau.com/SybilClient"))
                {
                    var updateInfo = await mgr.CheckForUpdate();
                    if (updateInfo != null)
                    {
                        Trace.WriteLine("Current Version: " + updateInfo.CurrentlyInstalledVersion.Version);
                        Trace.WriteLine("Newest Version: " + updateInfo.FutureReleaseEntry.Version);
                        Trace.WriteLine(updateInfo.ReleasesToApply.Count() + " releases to apply");
                        if (updateInfo.ReleasesToApply.Any())
                        {
                            Trace.WriteLine("Getting Updates");
                            await mgr.UpdateApp();
                            Trace.WriteLine("Updates Applied, Please Restart Sybil Client");
                            MessageBox.Show("Updates Applied, Please Restart Sybil Client");
                        }
                    }
                }
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            _timer.Stop();
            Application.Exit();
        }

        private void btnSettings_Click(object sender, EventArgs e)
        {
            var settingsForm = SettingsForm.GetInstance();
            settingsForm.Show();
            if (!settingsForm.Visible)
            {
                settingsForm.BringToFront();
            }
        }
    }
}
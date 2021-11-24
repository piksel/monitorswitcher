﻿/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using System.IO;
using System.Xml;
using System.Runtime.InteropServices;
using System.Reflection;
using static MonitorSwitcherGUI.Util;

namespace MonitorSwitcherGUI
{
    public class MonitorSwitcherGUI : Form
    {
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private String settingsDirectory;
        private String settingsDirectoryProfiles;
        private List<Hotkey> Hotkeys;
        //private GlobalKeyboardHook KeyHook; 

        public MonitorSwitcherGUI(string CustomSettingsDirectory)
        {
            // Initialize settings directory
            settingsDirectory = GetSettingsDirectory(CustomSettingsDirectory);
            settingsDirectoryProfiles = GetSettingsProfielDirectotry(settingsDirectory);

            if (!Directory.Exists(settingsDirectory))
                Directory.CreateDirectory(settingsDirectory);
            if (!Directory.Exists(settingsDirectoryProfiles))
                Directory.CreateDirectory(settingsDirectoryProfiles);

            // Initialize Hotkey list before loading settings
            Hotkeys = new List<Hotkey>();

            // Load all settings
            LoadSettings();

            // Inizialize globa keyboard hook or hotkeys
            //KeyHook = new GlobalKeyboardHook();
            //KeyHook.KeyDown += new KeyEventHandler(KeyHook_KeyDown);
            //KeyHook.KeyUp += new KeyEventHandler(KeyHook_KeyUp);

            // Refresh Hotkey Hooks
            KeyHooksRefresh();

            // Build up context menu
            trayMenu = new ContextMenuStrip();
            trayMenu.ImageList = new ImageList();
            trayMenu.ImageList.Images.Add(new Icon(GetType(), "MainIcon.ico"));
            trayMenu.ImageList.Images.Add(new Icon(GetType(), "DeleteProfile.ico"));
            trayMenu.ImageList.Images.Add(new Icon(GetType(), "Exit.ico"));
            trayMenu.ImageList.Images.Add(new Icon(GetType(), "Profile.ico"));
            trayMenu.ImageList.Images.Add(new Icon(GetType(), "SaveProfile.ico"));
            trayMenu.ImageList.Images.Add(new Icon(GetType(), "NewProfile.ico"));
            trayMenu.ImageList.Images.Add(new Icon(GetType(), "About.ico"));
            trayMenu.ImageList.Images.Add(new Icon(GetType(), "Hotkey.ico"));

            // add paypal png logo
            Assembly myAssembly = Assembly.GetExecutingAssembly();
            Stream myStream = myAssembly.GetManifestResourceStream("MonitorSwitcherGUI.PayPal.png");
            trayMenu.ImageList.Images.Add(Image.FromStream(myStream));

            // finally build tray menu
            BuildTrayMenu();

            // Create tray icon
            trayIcon = new NotifyIcon();
            trayIcon.Text = "Monitor Profile Switcher";
            trayIcon.Icon = new Icon(GetType(), "MainIcon.ico");
            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.Visible = true;
            trayIcon.MouseUp += OnTrayClick;
        }

        public static String GetSettingsDirectory(string customSettingsDirectory)
        {
            String dir = "";
            if (string.IsNullOrEmpty(customSettingsDirectory))
            {
                dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MonitorSwitcher");
            }
            else
            {
                dir = customSettingsDirectory;
            }
            return dir;
        }

        public static String GetSettingsProfielDirectotry(string settingsDirectory)
        {
            return Path.Combine(settingsDirectory, "Profiles");
        }

        private void KeyHooksRefresh()
        {
            List<Hotkey> removeList = new List<Hotkey>();
            // check which hooks are still valid
            foreach (Hotkey hotkey in Hotkeys)
            {
                if (!File.Exists(ProfileFileFromName(hotkey.profileName)))
                {
                    hotkey.UnregisterHotkey();
                    removeList.Add(hotkey);
                }
            }
            if (removeList.Count > 0)
            {
                foreach (Hotkey hotkey in removeList)
                {
                    Hotkeys.Remove(hotkey);
                }
                removeList.Clear();
                SaveSettings();
            }

            // register the valid hooks
            foreach (Hotkey hotkey in Hotkeys)
            {
                hotkey.UnregisterHotkey();
                hotkey.RegisterHotkey(this);
            }
        }

        public void KeyHook_KeyUp(object sender, HandledEventArgs e)
        {
            HotkeyCtrl hotkeyCtrl = (sender as HotkeyCtrl);
            Hotkey hotkey = FindHotkey(hotkeyCtrl);
            LoadProfile(hotkey.profileName);
            e.Handled = true;
        }

        public void KeyHook_KeyDown(object sender, HandledEventArgs e)
        {
            e.Handled = true;
        }

        public void LoadSettings()
        {
            // Unregister and clear all existing hotkeys
            foreach (Hotkey hotkey in Hotkeys)
            {
                hotkey.UnregisterHotkey();
            }
            Hotkeys.Clear();

            // Loading the xml file
            if (!File.Exists(SettingsFileFromName("Hotkeys")))
                return;

            System.Xml.Serialization.XmlSerializer readerHotkey = new System.Xml.Serialization.XmlSerializer(typeof(Hotkey));

            try
            {
                XmlReader xml = XmlReader.Create(SettingsFileFromName("Hotkeys"));
                xml.Read();
                while (true)
                {
                    if ((xml.Name.CompareTo("Hotkey") == 0) && (xml.IsStartElement()))
                    {
                        Hotkey hotkey = (Hotkey)readerHotkey.Deserialize(xml);
                        Hotkeys.Add(hotkey);
                        continue;
                    }

                    if (!xml.Read())
                    {
                        break;
                    }
                }
                xml.Close();
            }
            catch
            {
            }
        }

        public void SaveSettings()
        {
            System.Xml.Serialization.XmlSerializer writerHotkey = new System.Xml.Serialization.XmlSerializer(typeof(Hotkey));

            XmlWriterSettings xmlSettings = new XmlWriterSettings();
            xmlSettings.CloseOutput = true;

            try
            {
                using (FileStream fileStream = new FileStream(SettingsFileFromName("Hotkeys"), FileMode.Create))
                {
                    XmlWriter xml = XmlWriter.Create(fileStream, xmlSettings);
                    xml.WriteStartDocument();
                    xml.WriteStartElement("hotkeys");
                    foreach (Hotkey hotkey in Hotkeys)
                    {
                        writerHotkey.Serialize(xml, hotkey);
                    }
                    xml.WriteEndElement();
                    xml.WriteEndDocument();
                    xml.Flush();
                    xml.Close();

                    fileStream.Close();
                }
            }
            catch
            {
            }
        }

        public Hotkey FindHotkey(HotkeyCtrl ctrl)
        {
            foreach (Hotkey hotkey in Hotkeys)
            {
                if (hotkey.hotkeyCtrl == ctrl)
                    return hotkey;
            }

            return null;
        }

        public Hotkey FindHotkey(String name)
        {
            foreach (Hotkey hotkey in Hotkeys)
            {
                if (hotkey.profileName.CompareTo(name) == 0)
                    return hotkey;
            }

            return null;
        }

        public void BuildTrayMenu()
        {
            ToolStripItem newMenuItem;

            trayMenu.Items.Clear();

            trayMenu.Items.Add("Load Profile").Enabled = false;
            trayMenu.Items.Add("-");

            // Find all profile files
            string[] profiles = Directory.GetFiles(settingsDirectoryProfiles, "*.xml");

            // Add to load menu
            foreach (string profile in profiles)
            {
                string itemCaption = Path.GetFileNameWithoutExtension(profile);
                newMenuItem = trayMenu.Items.Add(itemCaption);
                newMenuItem.Click += OnMenuLoad;
                newMenuItem.ImageIndex = 3;
            }

            // Menu for saving items
            trayMenu.Items.Add("-");
            ToolStripMenuItem saveMenu = new ToolStripMenuItem("Save Profile");
            saveMenu.ImageIndex = 4;
            saveMenu.DropDown = new ToolStripDropDownMenu();
            saveMenu.DropDown.ImageList = trayMenu.ImageList;
            trayMenu.Items.Add(saveMenu);

            newMenuItem = saveMenu.DropDownItems.Add("New Profile...");
            newMenuItem.Click += OnMenuSaveAs;
            newMenuItem.ImageIndex = 5;
            saveMenu.DropDownItems.Add("-");

            // Menu for deleting items
            ToolStripMenuItem deleteMenu = new ToolStripMenuItem("Delete Profile");
            deleteMenu.ImageIndex = 1;
            deleteMenu.DropDown = new ToolStripDropDownMenu();
            deleteMenu.DropDown.ImageList = trayMenu.ImageList;
            trayMenu.Items.Add(deleteMenu);

            // Menu for hotkeys
            ToolStripMenuItem hotkeyMenu = new ToolStripMenuItem("Set Hotkeys");
            hotkeyMenu.ImageIndex = 7;
            hotkeyMenu.DropDown = new ToolStripDropDownMenu();
            hotkeyMenu.DropDown.ImageList = trayMenu.ImageList;
            trayMenu.Items.Add(hotkeyMenu);

            // Add to delete, save and hotkey menus
            foreach (string profile in profiles)
            {
                string itemCaption = Path.GetFileNameWithoutExtension(profile);
                newMenuItem = saveMenu.DropDownItems.Add(itemCaption);
                newMenuItem.Click += OnMenuSave;
                newMenuItem.ImageIndex = 3;

                newMenuItem = deleteMenu.DropDownItems.Add(itemCaption);
                newMenuItem.Click += OnMenuDelete;
                newMenuItem.ImageIndex = 3;

                string hotkeyString = "(No Hotkey)";
                // check if a hotkey is assigned
                Hotkey hotkey = FindHotkey(Path.GetFileNameWithoutExtension(profile));
                if (hotkey != null)
                {
                    hotkeyString = "(" + hotkey.ToString() + ")";
                }

                newMenuItem = hotkeyMenu.DropDownItems.Add(itemCaption + " " + hotkeyString);
                newMenuItem.Tag = itemCaption;
                newMenuItem.Click += OnHotkeySet;
                newMenuItem.ImageIndex = 3;
            }

            trayMenu.Items.Add("-");
            newMenuItem = trayMenu.Items.Add("Turn Off All Monitors");
            newMenuItem.Click += OnEnergySaving;
            newMenuItem.ImageIndex = 0;

            trayMenu.Items.Add("-");
            newMenuItem = trayMenu.Items.Add("About");
            newMenuItem.Click += OnMenuAbout;
            newMenuItem.ImageIndex = 6;

            newMenuItem = trayMenu.Items.Add("Donate");
            newMenuItem.Click += OnMenuDonate;
            newMenuItem.ImageIndex = 8;

            newMenuItem = trayMenu.Items.Add("Exit");
            newMenuItem.Click += OnMenuExit;
            newMenuItem.ImageIndex = 2;
        }

        public string ProfileFileFromName(string name)
        {
            string fileName = name + ".xml";
            string filePath = Path.Combine(settingsDirectoryProfiles, fileName);

            return filePath;
        }

        public string SettingsFileFromName(string name)
        {
            string fileName = name + ".xml";
            string filePath = Path.Combine(settingsDirectory, fileName);

            return filePath;
        }

        public void OnEnergySaving(object sender, EventArgs e)
        {
            System.Threading.Thread.Sleep(500); // wait for 500 milliseconds to give the user the chance to leave the mouse alone
            SendMessageAPI.PostMessage(new IntPtr(SendMessageAPI.HWND_BROADCAST), SendMessageAPI.WM_SYSCOMMAND, new IntPtr(SendMessageAPI.SC_MONITORPOWER), new IntPtr(SendMessageAPI.MONITOR_OFF));
        }

        public void OnMenuAbout(object sender, EventArgs e)
        {
            MessageBox.Show("Monitor Profile Switcher by Martin Krämer \n(MartinKraemer84@gmail.com)\nVersion 0.8.0.0\nCopyright 2013-2017 \n\nhttps://sourceforge.net/projects/monitorswitcher/", "About Monitor Profile Switcher", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public void OnMenuDonate(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=Y329BPYNKDTLC");
        }

        public void OnMenuSaveAs(object sender, EventArgs e)
        {
            string profileName = "New Profile";
            if (InputBox("Save as new profile", "Enter name of new profile", ref profileName) == DialogResult.OK)
            {
                string invalidChars = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
                foreach (char invalidChar in invalidChars)
                {
                    profileName = profileName.Replace(invalidChar.ToString(), "");
                }

                if (profileName.Trim().Length > 0)
                {
                    if (!MonitorSwitcher.SaveDisplaySettings(ProfileFileFromName(profileName)))
                    {
                        trayIcon.BalloonTipTitle = "Failed to save Multi Monitor profile";
                        trayIcon.BalloonTipText = "MonitorSwitcher was unable to save the current profile to a new profile with name\"" + profileName + "\"";
                        trayIcon.BalloonTipIcon = ToolTipIcon.Error;
                        trayIcon.ShowBalloonTip(5000);
                    }
                }
            }
        }

        public void OnHotkeySet(object sender, EventArgs e)
        {
            string profileName = (((ToolStripMenuItem)sender).Tag as string);
            Hotkey hotkey = FindHotkey(profileName);
            Boolean isNewHotkey = false;
            if (hotkey == null)
                isNewHotkey = true;
            if (HotkeySetting("Set Hotkey for Monitor Profile '" + profileName + "'", "Enter name of new profile", ref hotkey) == DialogResult.OK)
            {
                if ((isNewHotkey) && (hotkey != null))
                {
                    if (!hotkey.RemoveKey)
                    {
                        hotkey.profileName = profileName;
                        Hotkeys.Add(hotkey);
                    }
                }
                else if (hotkey != null)
                {
                    if (hotkey.RemoveKey)
                    {
                        Hotkeys.Remove(hotkey);
                    }
                }

                KeyHooksRefresh();
                SaveSettings();
            }
        }

        public void LoadProfile(string name)
        {
            if (!MonitorSwitcher.LoadDisplaySettings(ProfileFileFromName(name)))
            {
                trayIcon.BalloonTipTitle = "Failed to load Multi Monitor profile";
                trayIcon.BalloonTipText = "MonitorSwitcher was unable to load the previously saved profile \"" + name + "\"";
                trayIcon.BalloonTipIcon = ToolTipIcon.Error;
                trayIcon.ShowBalloonTip(5000);
            }
        }

        public void OnMenuLoad(object sender, EventArgs e)
        {
            LoadProfile(((ToolStripMenuItem)sender).Text);
        }

        public void OnMenuSave(object sender, EventArgs e)
        {
            if (!MonitorSwitcher.SaveDisplaySettings(ProfileFileFromName(((ToolStripMenuItem)sender).Text)))
            {
                trayIcon.BalloonTipTitle = "Failed to save Multi Monitor profile";
                trayIcon.BalloonTipText = "MonitorSwitcher was unable to save the current profile to name\"" + ((ToolStripMenuItem)sender).Text + "\"";
                trayIcon.BalloonTipIcon = ToolTipIcon.Error;
                trayIcon.ShowBalloonTip(5000);
            }
        }

        public void OnMenuDelete(object sender, EventArgs e)
        {
            File.Delete(ProfileFileFromName(((ToolStripMenuItem)sender).Text));
        }

        public void OnTrayClick(object sender, MouseEventArgs e)
        {
            BuildTrayMenu();

            if (e.Button == MouseButtons.Left)
            {
                System.Reflection.MethodInfo mi = typeof(NotifyIcon).GetMethod("ShowContextMenu", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                mi.Invoke(trayIcon, null);
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            KeyHooksRefresh();
        }

        protected override void OnLoad(EventArgs e)
        {
            Visible = false; // Hide form window.
            ShowInTaskbar = false; // Remove from taskbar.

            base.OnLoad(e);
        }

        private void OnMenuExit(object sender, EventArgs e)
        {
            Application.Exit();
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                // Release the icon resource.
                trayIcon.Dispose();
            }

            base.Dispose(isDisposing);
        }
    }
}

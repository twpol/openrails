// COPYRIGHT 2009 - 2024 by the Open Rails project.
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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Resources;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using Newtonsoft.Json;
using ORTS.Common;
using ORTS.Settings;
using ORTS.Updater;
using static ORTS.Common.SystemInfo;
using static Menu.Notifications.NotificationPage;
using Newtonsoft.Json.Serialization;

// Behaviour
// Notifications are read only once as a background task at start into Notifications.
// Every time the notifications page is re-visited, the position is discarded and first page is shown.
// Every time the notifications page is re-visited, the Update Mode option may have changed, so
// the visibility of each notification in NotificationList is re-assessed. Also its date.
// Every time the user selects a different notification page, the panel is re-loaded with items for that page.

namespace Menu.Notifications
{
    public class NotificationManager
    {
        public Notifications Notifications; // An object defined by the JSON schema
        
        // The Page points to the MainForm.Panel and also holds details of the items for the current notification.
        public NotificationPage Page { get; private set; }
        public bool ArePagesVisible = false;
        public int CurrentPageIndex = 0;

        public class PageTracking
        {
            // Notifications are listed in reverse date order, with the newest one at the front.
            public string LastViewDate { get; set; } // This is the date when notifications were last viewed and is saved in yyyy-MM-dd format as a hidden UserSetting.
            public int Count { get; set; }  // New notifications are those with a date after the LastViewDate.
            public int Viewed { get; set; } // Viewing always starts at the first, newest notification. The user sees (Count - Viewed) 
        }

        // Parameters (e.g. {{installed_version}} are saved in the dictionary alongside their value.
        // They are written once and potentially read many times.
        private Dictionary<string, string> ParameterDictionary;
        
        private Exception Error;
        private bool Log = false;
        private const string LogFile = "notifications_trial_log.txt";

        private readonly MainForm MainForm; // Needed so we can add controls to the NotificationPage
        private readonly UpdateManager UpdateManager;
        private readonly UserSettings Settings;
        private readonly Panel Panel;

        public Image PreviousImage { get; private set; }
        public Image NextImage { get; private set; }
        public Image FirstImage { get; private set; }
        public Image LastImage { get; private set; }
        public PageTracking NewPages { get; private set; }
        public double ScreenScaling { get; private set; }
        public bool ScreenAdjusted { get; set; }

        public NotificationManager(MainForm mainForm, ResourceManager resources, UpdateManager updateManager, UserSettings settings, Panel panel)
        {
            MainForm = mainForm;
            this.UpdateManager = updateManager;
            this.Settings = settings;
            Panel = panel;
            NewPages = new PageTracking();
            ScreenScaling = GetScalingFactor();

            // Load images of arrows
            PreviousImage = (Image)resources.GetObject("Notification_previous");
            NextImage = (Image)resources.GetObject("Notification_next");
            FirstImage = (Image)resources.GetObject("Notification_first");
            LastImage = (Image)resources.GetObject("Notification_last");
        }

        public double GetScalingFactor()
        {
#if NET5_0_OR_GREATER
#warning GetScalingFactor() not implemented for NET5+
            return 1;
#else
            return Screen.PrimaryScreen.Bounds.Width / SystemParameters.PrimaryScreenWidth;
#endif
        }

        public void CheckNotifications()
        {
            try
            {
                Error = null;
                ArePagesVisible = false;
                CurrentPageIndex = 0;
                Notifications = GetNotifications();
                ParameterDictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                // To support testing, add any overriding values to the ParameterDictionary
                GetOverrideParameters()?.ParameterValueList.ForEach(i => ParameterDictionary.Add(i.Parameter, i.Value));
                LogOverrideParameters();

                ReplaceParameters();
                LogParameters();

                Notifications.NotificationList = IncludeValid(Notifications.NotificationList);
                Notifications.NotificationList = SortByDate(Notifications.NotificationList);
            }
            catch (Exception ex)
            {
                AppendToLog(ex.ToString());
                Error = ex;
            }
        }

        Notifications GetNotifications()
        {
            string notificationsSerial;

            // To support testing of a new remote notifications.json file before it is published,
            // GetNotifications() tests first for a local file notifications_trial.json
            // and uses that if present, else it uses the remote file.
            var filename = @"notifications_trial.json";
            if (File.Exists(filename))
            {
                // Input from local file into a string
                notificationsSerial = File.ReadAllText(filename);
                
                Log = true; // Turn on logging to LogFile
                if (File.Exists(LogFile)) File.Delete(LogFile);
            }
            else
            {
                // Input from remote file into a string
                notificationsSerial = GetRemoteJson();
            }

            var jsonSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto, SerializationBinder = new NotificationSerializationBinder() };
            var jsonInput = JsonConvert.DeserializeObject<Notifications>(notificationsSerial, jsonSettings);

            NewPages.Count = 0;
            NewPages.Viewed = 0;
            NewPages.LastViewDate = Settings.LastViewNotificationDate;
            if (NewPages.LastViewDate == "") NewPages.LastViewDate = "2024-01-01"; // Date of this code - i.e. before Notifications went public

            return jsonInput;
        }

        /// <summary>
        /// Fetch the Notifications from https://static.openrails.org/api/notifications/menu.json 
        /// This file is copied hourly from Github openrails/notifications/
        /// </summary>
        private string GetRemoteJson()
        {
            var client = new WebClient()
            {
                CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.BypassCache),
                Encoding = Encoding.UTF8,
            };
            // Helpful to supply server with data for its log file.
            client.Headers[HttpRequestHeader.UserAgent] = $"{System.Windows.Forms.Application.ProductName}/{VersionInfo.VersionOrBuild}";

            return client.DownloadString(new Uri("https://static.openrails.org/api/notifications/menu.json"));
        }

        /// <summary>
        /// Returns a list of Notifications excluding any that fail IncludeIf. Sorts Update channel "none" to the end.
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        private List<Notification> IncludeValid(List<Notification> list)
        {
            NewPages.Count = 0;

            var filteredList = new List<Notification>();
            foreach (var n in list)
            {
                if (AreNotificationChecksMet(n))
                {
                    if (n.Date == "none") 
                        n.Date = " none"; // UpdateChannel = "none" found; push this to end of the list
                    else
                    {
                        if (String.Compare(NewPages.LastViewDate, n.Date) == -1) NewPages.Count++;
                    }
                    filteredList.Insert(0, n); // Add to head of list to provide a basic pre-sort.
                }
            }
            return filteredList;
        }

        private List<Notification> SortByDate(List<Notification> list)
        {
            return list.OrderByDescending(n => n.Date).ToList();
        }

        /// <summary>
        /// Adds details of the current notification to the panel
        /// </summary>
        public void PopulatePage()
        {
            Page = new NotificationPage(MainForm, Panel, this); 

            if (UpdateManager.LastCheckError != null || Error != null)
            {
                PopulateRetryPage();
            }
            else if (Notifications.NotificationList.Count == 0)
            {
                PopulateEmptyPage();
            }
            else
            {
                Settings.LastViewNotificationDate = $"{DateTime.Today:yyyy-MM-dd}";
                Settings.Save("LastViewNotificationDate");  // Saves the date on any viewing of notifications

                var list = Notifications.NotificationList;
                var n = list[CurrentPageIndex];
                LogNotification(n);

                Page.NDetailList.Add(new NTitleControl(Panel, CurrentPageIndex + 1, list.Count, n.Date, n.Title));

                // Check criteria for each item and add the successful items to the current page
                foreach (var item in n.ItemList)
                {
                    if (AreItemChecksMet(item)) AddItemToPage(Page, item);
                }
            }

            Page.NDetailList.Add(new NTextControl(Panel, ""));
            Page.NDetailList.Add(new NTextControl(Panel, "(Toggle icon to hide these notifications.)"));
        }

        private void PopulateRetryPage()
        {
            NewPages.Count = 0;

            // Reports notifications are not available.
            var channelName = UpdateManager.ChannelName == "" ? "None" : UpdateManager.ChannelName;
            var today = DateTime.Now.Date;
            Page.NDetailList.Add(new NTitleControl(Panel, 1, 1, $"{today:dd-MMM-yy}", "Notifications are not available"));

            Page.NDetailList.Add(new NRecordControl(Panel, "Update mode", 140, channelName));
            Page.NDetailList.Add(new NRecordControl(Panel, "Installed version", 140, VersionInfo.VersionOrBuild));

            Page.NDetailList.Add(new NHeadingControl(Panel, "Notifications are not available", "red"));
            var message = (UpdateManager.LastCheckError != null)
                ? UpdateManager.LastCheckError.Message
                : Error.Message;
            Page.NDetailList.Add(new NTextControl(Panel, $"Error: {message}"));
            Page.NDetailList.Add(new NTextControl(Panel, "Is your Internet connected?"));

            Page.NDetailList.Add(new NRetryControl(Page, "Retry", 140, "Try again to fetch notifications", MainForm));
        }

        private void PopulateEmptyPage()
        {
            NewPages.Count = 0;

            // Reports no notifications.
            var today = DateTime.Now.Date;
            Page.NDetailList.Add(new NTitleControl(Panel, 1, 1, $"{today:dd-MMM-yy}", "No notifications"));
        }

        #region Process Criteria
        private bool AreNotificationChecksMet(Notification notification)
        {
            if (notification.IncludeIf != null || notification.IncludeIfNot != null)
            {
                AppendToLog($"Title: {notification.Title}");
            }
            if (notification.IncludeIf != null)
            {
                foreach (var checkName in notification.IncludeIf)
                {
                    // Include if A=true AND B=true AND ...
                    if (IsCheckMet(checkName) == false) return false;
                }
            }
            if (notification.IncludeIfNot != null)
            {
                foreach (var checkName in notification.IncludeIfNot)
                {
                    // Include if C=false AND D=false AND ...
                    if (IsCheckMet(checkName) == true) return false;
                }
            }

            return true;
        }

        /// <summary>
        ///  For the checks in the item, compares parameter values with criteria values
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private bool AreItemChecksMet(Item item)
        {
            if (item.IncludeIf != null || item.IncludeIfNot != null)
            {
                AppendToLog($"Label: {item.Label}");
            }
            if (item.IncludeIf != null)
            {
                foreach (var checkName in item.IncludeIf)
                {
                    // Include if A=true AND B=true AND ...
                    if (IsCheckMet(checkName) == false) return false;
                }
            }
            if (item.IncludeIfNot != null)
            {
                foreach (var checkName in item.IncludeIfNot)
                {
                    // Include if C=false AND D=false AND ...
                    if (IsCheckMet(checkName) == true) return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Could cache these checks to improve performance.
        /// </summary>
        /// <param name="checkname"></param>
        /// <returns></returns>
        private bool IsCheckMet(string checkname)
        {
            foreach (var check in Notifications.CheckList)
            {
                if (check.Id == checkname)
                {
                    if (CheckAnyMatch(check.AnyOfList)) return true;
                }
            }
            return false;
        }

        private bool CheckAnyMatch(List<AnyOf> anyOfList)
        {
            foreach (var anyOf in anyOfList)
            {
                if (CheckAllMatch(anyOf.AllOfList)) return true; 
            }
            return false;
        }

        private bool CheckAllMatch(List<Criteria> criteriaList)
        {
            foreach (var c in criteriaList)
            {
                var result = c.IsMatch();
                AppendToLog($"Check: {result}: '{c.Property}' {c.GetType().Name} '{c.Value}'");
                if (!result) return false;
            }
            return true;
        }
        #endregion

        private void AddItemToPage(NotificationPage page, Item item)
        {
            if (item is Record record)
            {
                Page.NDetailList.Add(new NRecordControl(Panel, item.Label, item.Indent, record.Value));
            }
            else if (item is Link link)
            {
                var url = GetUrl(link);
                if (string.IsNullOrEmpty(url) == false)
                {
                    Page.NDetailList.Add(new NLinkControl(Page, item.Label, item.Indent, link.Value, MainForm, url));
                }
            }
            else if (item is Dialog dialog)
            {
                Page.NDetailList.Add(new NDialogControl(Page, item.Label, item.Indent, dialog.Value, MainForm, dialog.Form));
            }
            else if (item is Update update)
            {
                Page.NDetailList.Add(new NUpdateControl(Page, item.Label, item.Indent, update.Value, MainForm));
            }
            else if (item is Heading heading)
            {
                Page.NDetailList.Add(new NHeadingControl(Panel, item.Label, heading.Color));
            }
            else if (item is Text text)
            {
                Page.NDetailList.Add(new NTextControl(Panel, item.Label, text.Color));
            }
            else
            {
                Page.NDetailList.Add(new NTextControl(Panel, item.Label));
            }
        }

        private string GetUrl(Link link)
        {
            var url = link.Url;
            if (string.IsNullOrEmpty(url))
            {
                switch (UpdateManager.ChannelName.ToLower())
                {
                    case "stable":
                    case "": // Channel None
                        url = link.StableUrl;
                        break;
                    case "testing":
                        url = link.TestingUrl;
                        break;
                    case "unstable":
                        url = link.UnstableUrl;
                        break;
                }
            }

            return url;
        }

        void ReplaceParameters()
        {
            Notifications.ReplaceParameters(ReplaceParameterValues);
        }

        string ReplaceParameterValues(string value)
        {
            if (value == null) return value;
            var start = 0;
            while ((start = value.IndexOf("{{", start)) >= 0)
            {
                var end = value.IndexOf("}}", start);
                if (end == -1) break;
                var variable = value.Substring(start + 2, end - start - 2);
                var replacement = GetParameterValue(variable);
                value = value.Substring(0, start) + replacement + value.Substring(end + 2);
                start += replacement.Length;
            }
            return value;
        }

        string GetParameterValue(string parameter)
        {
            string replacement;
            if (ParameterDictionary.ContainsKey(parameter))
            {
                replacement = ParameterDictionary[parameter];
            }
            else
            {
                switch (parameter)
                {
                    // Update parameters
                    // Using "none" instead of "" so that records are readable.
                    case "update_mode":
                        replacement = (UpdateManager.ChannelName == "")
                            ? "none"
                            : UpdateManager.ChannelName;
                        break;
                    case "latest_version":
                        replacement = UpdateManager.LastUpdate == null
                            || UpdateManager.ChannelName == ""
                            ? "none"
                            : UpdateManager.LastUpdate.Version;
                        break;
                    case "release_date":
                        replacement = UpdateManager.LastUpdate == null
                            ? "none"
                            : $"{UpdateManager.LastUpdate.Date:yyyy-MM-dd}";
                        break;

                    // System parameters
                    case "installed_version":
                        replacement = SystemInfo.Application.Version;
                        break;
                    case "runtime":
                        replacement = Runtime.ToString();
                        break;
                    case "system":
                        replacement = SystemInfo.OperatingSystem.ToString();
                        break;
                    case "memory":
                        replacement = Direct3DFeatureLevels.ToString();
                        break;
                    case "cpu":
                        replacement = "";
                        foreach (var cpu in CPUs)
                        {
                            replacement += $", {cpu.Name}";
                        }
                        break;
                    case "gpu":
                        replacement = "";
                        foreach (var gpu in GPUs)
                        {
                            replacement += $", {gpu.Name}";
                        }
                        break;
                    case "direct3d":
                        replacement = string.Join(",", Direct3DFeatureLevels);
                        break;

                    // Routes, User Settings and Not Recognised
                    case "installed_routes":
                        replacement = GetInstalledRoutes();
                        break;
                    default:
                        var propertyValue = GetSetting(parameter);
                        replacement = (propertyValue == "")
                            ? "{{" + parameter + "}}"     // strings that are not recognised are not replaced.
                            : propertyValue.ToLower().Replace("false", "off").Replace("true", "on");
                        break;
                }
                ParameterDictionary.Add(parameter, replacement);
            }
            return replacement;
        }

        /// <summary>
        /// Gets the property value of a UserSetting. The setting must match case as in "Setting.SimpleControlPhysics".
        /// </summary>
        /// <param name="settingText"></param>
        /// <returns></returns>
        string GetSetting(string settingText)
        {
            // 2 elements: "Settings.<property>", e.g. "SimpleControlPhysics" 
            // 3 elements: "Settings.<group>.<property>", e.g. "Telemetry.RandomNumber1000"
            var nameArray = settingText.Split('.');
            if (nameArray.Length < 2 || nameArray[0] != "Settings") return "";

            var value = (object)Settings;
            for (var i = 1; i < nameArray.Length; i++)
            {
                var prop = value.GetType().GetProperty(nameArray[i]);
                if (prop == null) return "";
                value = prop.GetValue(value);
            }
            if (value is DateTime dateTime) return dateTime.ToString("yyyy-MM-dd");
            return value?.ToString() ?? "";
        }

        private string GetInstalledRoutes()
        {
            var installedRouteList = "";
            var contentDictionary = Settings.Folders.Folders;
            foreach (var folder in contentDictionary)
            {
                var path = folder.Value + @"\ROUTES\";
                if (Directory.Exists(path))
                {
                    foreach (var routePath in Directory.GetDirectories(path))
                    {
                        // Extract the last folder in the path - the route folder name, e.g. "SCE"
                        var routeName = Path.GetFileName(routePath).ToLower();
                        installedRouteList += routeName + ",";
                    }
                }
            }
            return installedRouteList;
        }

        OverrideParameterList GetOverrideParameters()
        {
            // To support testing of a new remote notifications.json file before it is published,
            // GetNotifications tests first for a local file notifications_override_values.json
            // and uses that if present to override the current program values, else it extracts these from the program.

            var filename = @"notifications_trial_parameters.json";
            if (File.Exists(filename))
            {
                // Input from local file into a string
                var overrideParametersSerial = File.ReadAllText(filename);

                var jsonSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto, SerializationBinder = new NotificationSerializationBinder() };
                var jsonInput = JsonConvert.DeserializeObject<OverrideParameterList>(overrideParametersSerial, jsonSettings);

                return jsonInput;
            }
            else
                return null;
        }

        public void ChangePage(int step)
        {
            CurrentPageIndex += step;
            if (step > 0
                && CurrentPageIndex > NewPages.Viewed  // and this is a new unviewed page
                && CurrentPageIndex <= NewPages.Count) // and there are still new unviewed pages to be viewed
            {
                NewPages.Viewed++;
            }
            MainForm.ShowNotificationPages();
        }

        #region Logging
        void LogOverrideParameters()
        {
            if (Log == false) return;

            using (StreamWriter sw = File.CreateText(LogFile))
            {
                sw.WriteLine("Parameters overridden:");
                foreach (var p in ParameterDictionary) sw.WriteLine($"{p.Key} = {p.Value}");
                sw.WriteLine();
            }
        }

        void LogParameters()
        {
            if (Log == false) return;

            using (StreamWriter sw = File.AppendText(LogFile))
            {
                sw.WriteLine("Parameters used:");
                foreach (var p in ParameterDictionary) sw.WriteLine($"{p.Key} = {p.Value}");
                sw.WriteLine();
            }
        }

        void LogNotification(Notification n)
        {
            AppendToLog($"\r\nNotification: {n.Title}");
        }

        void AppendToLog(string record)
        {
            if (Log == false) return;

            using (StreamWriter sw = File.AppendText(LogFile)) sw.WriteLine(record);
        }
        #endregion

        class NotificationSerializationBinder : ISerializationBinder
        {
            public void BindToName(Type serializedType, out string assemblyName, out string typeName)
            {
                throw new NotImplementedException();
            }

            public Type BindToType(string assemblyName, string typeName)
            {
                if (assemblyName == "Menu")
                {
                    var ns = typeof(Notifications).Namespace;
                    var name = typeName.Split('.').Last();
                    return typeof(Notifications).Assembly.GetType($"{ns}.{name}");
                }
                return null;
            }
        }
    }
}

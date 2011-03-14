using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Notification;
using Newtonsoft.Json;


namespace GeoScav
{

    public partial class MainPage : PhoneApplicationPage
    {
        bool isGameOn = false;
        bool isRegistered = true;

        private enum SendType : int
        {
            Registration = 0,
            UpdateRegid
        }

        // Constructor
        public MainPage()
        {
            InitializeComponent();
            //GlobalVars.serveraddr = "http://marrow.cs.ucsb.edu:3000/";
            GlobalVars.serveraddr = "http://cs176b.heroku.com/";
            //GlobalVars.serveraddr = "http://cs176b.appspot.com/";
            //GlobalVars.token = "9bb15023550810858bdd6ea364cfbbdb2749159a"; // TJ's server (heroku)
            //GlobalVars.token = "5717a9f6656653f386691c6404ee282718fdb25a"; // Ryan's server (appspot)
            Random randgen = new Random();
            GlobalVars.name = "win-name-final" + randgen.Next(1000);
            NotificationClient.Current.Connect();
            NotificationClient.Current.NotificationReceived += new EventHandler(getNotification);
            NotificationClient.Current.UriUpdated += new EventHandler(setUri);
            DisplayInfoText("Bootstrapping...", 3);
        }

        private void openMap(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/MapPage.xaml", UriKind.Relative));
        }

        private void registerPhone(object sender, RoutedEventArgs e)
        {
            // this is the phone's "name"
            //var phoneID = DeviceExtendedProperties.GetValue("DeviceUniqueId");
            //System.Text.UTF8Encoding enc = new System.Text.UTF8Encoding();
            //string name = enc.GetString((byte[])phoneID, 0, 20);

            send(GlobalVars.serveraddr + "register?name=" + GlobalVars.name + "&registration_id=" + GlobalVars.rid + "&phonetype=windows", SendType.Registration);
            if (isRegistered && isGameOn)
                startButton.IsEnabled = true;
        }

        private void getNotification(object s, EventArgs e)
        {
            // Convert to string
            HttpNotificationEventArgs eargs = (HttpNotificationEventArgs)e;
            StreamReader reader = new StreamReader(eargs.Notification.Body);
            string bodytext = reader.ReadToEnd();
            DisplayInfoText(bodytext, 60);
            // Parse HTTP request
            string[] parameters = bodytext.Split('&');
            foreach (string kv in parameters) 
            {
                string[] temp = kv.Split('=');
                if (temp[0] == "type" && temp[1] == "GAMEON")
                {
                    isGameOn = true;
                    DisplayInfoText("GAME ON!", 5);
                }
            }
            //var decodedUrl = HttpUtility.UrlDecode(bodytext);
            if (isRegistered && isGameOn)
                startButton.IsEnabled = true;
        }

        private void setUri(object s, EventArgs e)
        {
            NotificationChannelUriEventArgs eargs = (NotificationChannelUriEventArgs)e;
            GlobalVars.rid = eargs.ChannelUri.ToString();
            if (GlobalVars.token != null)
                send(GlobalVars.serveraddr + "update_reg_id?token=" + GlobalVars.token + "&registration_id=" + GlobalVars.rid, SendType.UpdateRegid);
            registerButton.IsEnabled = true;
        }

        private void send(string url, SendType type)
        {
            WebClient c = new WebClient();
            switch (type)
            {
                case SendType.Registration:
                    c.DownloadStringCompleted += new DownloadStringCompletedEventHandler(RegistrationCompleted);
                    break;
                case SendType.UpdateRegid:
                    c.DownloadStringCompleted += new DownloadStringCompletedEventHandler(UpdateRegidCompleted);
                    break;
                default: break;
            }
            c.DownloadStringAsync(new Uri(url));
        }

        /* parses the JSON response from the server */
        private void RegistrationCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            var deserializedJSON = JsonConvert.DeserializeObject<item>(e.Result);

            if (deserializedJSON.status.code == 0)
            {
                GlobalVars.token = deserializedJSON.response.token;
                isRegistered = true;
            }
            else if (deserializedJSON.status.code == 99)
            {
                send(GlobalVars.serveraddr + "update_reg_id?GlobalVars.token=" + GlobalVars.token + "&registration_id=" + GlobalVars.rid, SendType.UpdateRegid);
            }
            else
            {
                DisplayInfoText("Error (" + deserializedJSON.status.code + "): " + deserializedJSON.status.message, 5);
            }
            if (isRegistered && isGameOn)
                startButton.IsEnabled = true;
        }

        private void UpdateRegidCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            var deserializedJSON = JsonConvert.DeserializeObject<item>(e.Result);

            if (deserializedJSON.status.code == 0)
            {
                isRegistered = true;
            }
            else
            {
                DisplayInfoText("Error (" + deserializedJSON.status.code + "): " + deserializedJSON.status.message, 5);
            }
            if (isRegistered && isGameOn)
                startButton.IsEnabled = true;
        }

        #region Display Helpers

        // Async display of info text
        private void DisplayInfoText(String text, int durationSec)
        {
            infoText.Text = text;
            infoText.Visibility = System.Windows.Visibility.Visible;
            DispatcherTimer timer = new DispatcherTimer();

            timer.Tick +=
                delegate(object s, EventArgs args)
                {
                    infoText.Visibility = System.Windows.Visibility.Collapsed;
                    timer.Stop();
                };

            timer.Interval = new TimeSpan(0, 0, durationSec); // durationSec * 1sec
            timer.Start();
        }

        #endregion
    }
}
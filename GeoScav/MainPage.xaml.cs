using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Info;
using Microsoft.Phone.Notification;
using Newtonsoft.Json;


namespace GeoScav
{

    public partial class MainPage : PhoneApplicationPage
    {

        private string token;
        static int testnum = 0;
        static string name = "bdwm-win-test-name-test-" + testnum;
        string rid;
        bool isGameOn = false;
        bool isRegistered = false;

        // Constructor
        public MainPage()
        {
            InitializeComponent();
            rid = NotificationClient.Current.Connect();
            NotificationClient.Current.NotificationReceived += new EventHandler(getNotification);
        }

        private void openMap(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri(string.Format("/MapPage.xaml?token={0}", token), UriKind.Relative));
        }

        private void registerPhone(object sender, RoutedEventArgs e)
        {
            // this is the phone's "name"
            //var phoneID = DeviceExtendedProperties.GetValue("DeviceUniqueId");
            //System.Text.UTF8Encoding enc = new System.Text.UTF8Encoding();
            //string name = enc.GetString((byte[])phoneID, 0, 20);

            // should actually wait for "GAMEON" message before enabling the button
            // but for now, just turn it on
            startButton.IsEnabled = true;

            Random randgen = new Random();
            send(App.ServerAddr + "register?name=" + name + "&registration_id=" + rid + "&phonetype=windows");
            //send(App.ServerAddr + "register?name=" + name + "&registration_id=" + rid + "&phonetype=windows");
            if (isRegistered && isGameOn)
                startButton.IsEnabled = true;
        }

        private void getNotification(object s, EventArgs e)
        {
            // Convert to string
            HttpNotificationEventArgs eargs = (HttpNotificationEventArgs)e;
            BinaryReader reader = new BinaryReader(eargs.Notification.Body, System.Text.Encoding.UTF8);
            string bodytext = reader.ReadString();

            // Parse JSON
            var deserializedJSON = JsonConvert.DeserializeObject<item>(bodytext);
            if (deserializedJSON.push.type != null && deserializedJSON.push.type == "GAMEON")
            {
                isGameOn = true;
                DisplayInfoText("GAME ON", 5);
            }

            if (isRegistered && isGameOn)
                startButton.IsEnabled = true;
        }

        public void send(string url)
        {
            WebClient c = new WebClient();
            c.DownloadStringCompleted += new DownloadStringCompletedEventHandler(DownloadStringCompleted);
            c.DownloadStringAsync(new Uri(url));
        }

        /* parses the JSON response from the server */
        public void DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {

            var deserializedJSON = JsonConvert.DeserializeObject<item>(e.Result);

            Code.Text = (deserializedJSON.status.code).ToString();
            Message.Text = deserializedJSON.status.message;
            Token.Text = deserializedJSON.response.token;
            token = deserializedJSON.response.token;
            isRegistered = true;
            if (isRegistered && isGameOn)
                startButton.IsEnabled = true;
        }

        #region Display Helpers

        // Async display of info text
        public void DisplayInfoText(String text, int durationSec)
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
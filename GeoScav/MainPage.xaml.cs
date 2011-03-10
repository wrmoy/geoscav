using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Device.Location;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Info;
using Microsoft.Phone.Tasks;
using Microsoft.Phone.Reactive;
using Newtonsoft.Json;

namespace GeoScav
{



    public partial class MainPage : PhoneApplicationPage
    {

        private string token;
        static int test = 013;
        static string name = "bdwm-win-test-name-test-" + test;
        static string rid = "bdwm-win-test-rid-test-" + test;

        // Constructor
        public MainPage()
        {
            InitializeComponent();
        }

        



        private void registerPhone(object sender, RoutedEventArgs e)
        {
            // this is the phone's "name"
            var phoneID = DeviceExtendedProperties.GetValue("DeviceUniqueId");

            // should actually wait for "GAMEON" message before enabling the button
            // but for now, just turn it on
            startButton.IsEnabled = true;


           send("http://cs176b.heroku.com/register?name=" + test + "&registration_id=" + rid + "&phonetype=windows");

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
        }




        private void openMap(object sender, RoutedEventArgs e)
        {
           NavigationService.Navigate(new Uri(string.Format("/MapPage.xaml?token={0}", token), UriKind.Relative)); 
           // NavigationService.Navigate(new Uri("/MapPage.xaml", UriKind.Relative));
        }




    }


}
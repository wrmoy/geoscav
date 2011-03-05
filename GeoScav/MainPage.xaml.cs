using System;
using System.IO;
using System.Collections.Generic;
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
using Microsoft.Phone.Tasks;
using System.Device.Location;
using Microsoft.Phone.Reactive;

namespace GeoScav
{


    public partial class MainPage : PhoneApplicationPage
    {


        // Constructor
        public MainPage()
        {
            InitializeComponent();
        }


        private void registerPhone(object sender, RoutedEventArgs e)
        {

            var values = new NameValueCollection();
            values.Add("param1", "value1");
            values.Add("param2", "value2");

            new WebClient().UploadValues("http://www.example.com", values);


        }




        private void queryLocation(object sender, RoutedEventArgs e)
        {
            var searchTask = new SearchTask
            {
                SearchQuery = "46 Anglesea Street, Auckland 1011, New Zealand"
            };


        }


        private void checkin(object sender, RoutedEventArgs e)
        {

        }


        private void cancelCheckin(object sender, RoutedEventArgs e)
        {

        }


        private void uploadPhoto(object sender, RoutedEventArgs e)
        {

        }


        private void getPhoto(object sender, RoutedEventArgs e)
        {

        }


    }
}
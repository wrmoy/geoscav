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
using Microsoft.Phone.Tasks;
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

            /*var values = new NameValueCollection();
            values.Add("param1", "value1");
            values.Add("param2", "value2");

            new WebClient().UploadValues("http://www.example.com", values);*/

            startButton.Visibility = System.Windows.Visibility.Visible;
            helpText.Visibility = System.Windows.Visibility.Visible;
            getPhotoButton.Visibility = System.Windows.Visibility.Visible;
        }




        private void openMap(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/MapPage.xaml", UriKind.Relative));
        }


        private void getPhoto(object sender, RoutedEventArgs e)
        {
            // TODO: get and display a photo
        }


    }
}
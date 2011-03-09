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
            // this is the phone's "name"
            var phoneID = DeviceExtendedProperties.GetValue("DeviceUniqueId");

            startButton.Visibility = System.Windows.Visibility.Visible;
            helpText.Visibility = System.Windows.Visibility.Visible;
            getPhotoButton.Visibility = System.Windows.Visibility.Visible;

           // getResults("http://cs176b.heroku.com/register?name=winblowz&registration_id=thisIsGarbage&phonetype=windows");



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
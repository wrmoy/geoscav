using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Maps.MapControl;
using Microsoft.Phone.Controls;
using Microsoft.Phone;

namespace GeoScav
{
    public partial class MapPage : PhoneApplicationPage
    {
        // token parameter to use for server interactions
        string token;

        // listens for coordinate changes
        GeoCoordinateWatcher watcher;

        // current location
        double curr_lat;
        double curr_long;

        // current CID
        int curr_cid = 1;

        // coords of next check-in point
        double cid_lat;
        double cid_long;

        // boolean that holds whether or not the player is within check-in range
        bool isWithinRange = false;

        // boolean that holds whether or not the player has taken a picture
        bool pictureTaken = false;

        public MapPage()
        {
            InitializeComponent();
           
            // Reinitialize the GeoCoordinateWatcher
            watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High);
            watcher.MovementThreshold = 5;//distance in metres

            // Add event handlers for StatusChanged and PositionChanged events
            watcher.StatusChanged += new EventHandler<GeoPositionStatusChangedEventArgs>(watcher_StatusChanged);
            watcher.PositionChanged += new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(watcher_PositionChanged);

            // Ask for next check-in point
            QueryCidLocation();

            // Start data acquisition
            watcher.Start();
        }


        #region Event Handlers

        /// <summary>
        /// Handler for the StatusChanged event. This invokes MyStatusChanged on the UI thread and
        /// passes the GeoPositionStatusChangedEventArgs
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void watcher_StatusChanged(object sender, GeoPositionStatusChangedEventArgs e)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() => MyStatusChanged(e));

        }

        /// <summary>
        /// Handler for the PositionChanged event. This invokes MyStatusChanged on the UI thread and
        /// passes the GeoPositionStatusChangedEventArgs
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void watcher_PositionChanged(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() => MyPositionChanged(e));
        }

        #endregion

        /// <summary>
        /// Custom method called from the PositionChanged event handler
        /// </summary>
        /// <param name="e"></param>
        void MyPositionChanged(GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            // Update last location
            curr_lat = e.Position.Location.Latitude;
            curr_long = e.Position.Location.Longitude;

            // Update the map to show the current location
            Location ppLoc = new Location(e.Position.Location.Latitude, e.Position.Location.Longitude);
            mapMain.SetView(ppLoc, 18);

            //update pushpin location and show
            MapLayer.SetPosition(ppLocation, ppLoc);
            ppLocation.Visibility = System.Windows.Visibility.Visible;

            // Check if the check-in point is in range
            ProximityCheck();
        }

        /// <summary>
        /// Custom method called from the StatusChanged event handler
        /// </summary>
        /// <param name="e"></param>
        void MyStatusChanged(GeoPositionStatusChangedEventArgs e)
        {
            switch (e.Status)
            {
                case GeoPositionStatus.Disabled:
                    // The location service is disabled or unsupported.
                    // Alert the user
                    statusText.Visibility = System.Windows.Visibility.Visible;
                    statusText.Text = "Geolocation disabled: can't find you";
                    break;
                case GeoPositionStatus.Initializing:
                    // The location service is initializing.
                    // Disable the Start Location button
                    statusText.Visibility = System.Windows.Visibility.Visible;
                    statusText.Text = "Initializing geolocation...";
                    break;
                case GeoPositionStatus.NoData:
                    // The location service is working, but it cannot get location data
                    // Alert the user and enable the Stop Location button
                    statusText.Visibility = System.Windows.Visibility.Visible;
                    statusText.Text = "No geolocation data, are you indoors?";
                    ResetMap();

                    break;
                case GeoPositionStatus.Ready:
                    // The location service is working and is receiving location data
                    // Show the current position and enable the Stop Location button
                    statusText.Visibility = System.Windows.Visibility.Collapsed;
                    break;

            }
        }


        protected override void  OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (this.NavigationContext.QueryString.ContainsKey("token"))
            {
                token = this.NavigationContext.QueryString["token"];
            }

        }
    
        void ResetMap()
        {
            Location ppLoc = new Location(0, 0);
            mapMain.SetView(ppLoc, 1);

            //update pushpin location and show
            MapLayer.SetPosition(ppLocation, ppLoc);
            ppLocation.Visibility = System.Windows.Visibility.Collapsed;
        }

        // Only updates check-in info (cid_dist, cid_angle, curr_cid)
        void QueryCidLocation()
        {
            // TODO: update the cid_dist and cid_angle
            double cid_dist = 0;
            double cid_angle = 0;
            AddCheckInPoint(cid_dist, cid_angle);
        }

        // add a new pushpin where the next check-in point is
        void AddCheckInPoint(double cid_dist, double cid_angle)
        {
            // color
            var accentBrush = (Brush)Application.Current.Resources["PhoneAccentBrush"];
            // create new pushpin
            var pin = new Pushpin
            {
                Location = new Location
                {
                    Latitude = curr_lat + cid_dist * Math.Cos(cid_angle),
                    Longitude = curr_long + cid_dist * Math.Sin(cid_angle)
                },
                Background = accentBrush,
                Content = curr_cid,
            };
            // add it to the map
            mapLayer.AddChild(pin, pin.Location);
            // update internal variables
            cid_lat = curr_lat + cid_dist * Math.Cos(cid_angle);
            cid_long = curr_long + cid_dist * Math.Sin(cid_angle);
        }

        // check if the player is within range of the next check-in point
        void ProximityCheck()
        {
            // if we're not within distance, return
            if (Math.Sqrt(Math.Pow(curr_lat - cid_lat, 2) + Math.Pow(curr_long - cid_long, 2)) > 0.0001)
            {
                isWithinRange = false;
                return;
            }
            // if we were not previously in range, open the check-in buttons
            if (!isWithinRange)
            {
                isWithinRange = true;
                // open the check-in buttons
                DisplayInfoText("You're within check-in distance!", 10);
                checkInButton.Visibility = System.Windows.Visibility.Visible;
                checkOutButton.Visibility = System.Windows.Visibility.Collapsed;
                getPicButton.Visibility = System.Windows.Visibility.Visible;
                takePicButton.Visibility = System.Windows.Visibility.Visible;
            }
            // otherwise, we don't want to open the buttons again
        }

        // check-in procedures
        void checkIn(object sender, RoutedEventArgs e)
        {
            checkInButton.Visibility = System.Windows.Visibility.Collapsed;
            checkOutButton.Visibility = System.Windows.Visibility.Visible;
            getPicButton.IsEnabled = true;
            takePicButton.IsEnabled = true;

            // do check-in with the server
        }

        // check-out procedures
        void checkOut(object sender, RoutedEventArgs e)
        {
            // if the player has taken a picture, then get next checkpoint
            if (pictureTaken)
            {
                // update cid and next chkpt
                curr_cid++;
                QueryCidLocation();
                // close and reset the buttons
                checkOutButton.Visibility = System.Windows.Visibility.Collapsed;
                getPicButton.Visibility = System.Windows.Visibility.Collapsed;
                takePicButton.Visibility = System.Windows.Visibility.Collapsed;
                getPicButton.IsEnabled = false;
                takePicButton.IsEnabled = false;
                // reset the bool
                pictureTaken = false;
            }
            else // restart the "asking for check-in" process
            {
                isWithinRange = false;
                ProximityCheck();
            }
        }

        void getPic(object sender, RoutedEventArgs e)
        {
            // get picture URL from server
            // display it in a popup?
            Popup imgdisp = new Popup();
        }

        void takePic(object sender, RoutedEventArgs e)
        {
            // take picture
        }

        // Async display of info text
        void DisplayInfoText(String text, int durationSec)
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
    }
}
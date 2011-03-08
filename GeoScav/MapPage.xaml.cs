using System;
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
using Microsoft.Maps.MapControl;
using Microsoft.Phone.Controls;

namespace GeoScav
{
    public partial class MapPage : PhoneApplicationPage
    {
        // listens for coordinate changes
        GeoCoordinateWatcher watcher;

        // current location
        double curr_lat;
        double curr_long;

        // current CID
        int curr_cid;

        // directions to next CID
        double cid_dist;
        double cid_angle;

        public MapPage()
        {
            InitializeComponent();
            // Reinitialize the GeoCoordinateWatcher
            watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High);
            watcher.MovementThreshold = 5;//distance in metres

            // Add event handlers for StatusChanged and PositionChanged events
            watcher.StatusChanged += new EventHandler<GeoPositionStatusChangedEventArgs>(watcher_StatusChanged);
            watcher.PositionChanged += new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(watcher_PositionChanged);

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

            // redraw check-in point
            // TODO
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
                    if (statusText.Visibility == System.Windows.Visibility.Collapsed)
                        statusText.Visibility = System.Windows.Visibility.Visible;
                    statusText.Text = "Geolocation disabled: can't find you";
                    break;
                case GeoPositionStatus.Initializing:
                    // The location service is initializing.
                    // Disable the Start Location button
                    if (statusText.Visibility == System.Windows.Visibility.Collapsed)
                        statusText.Visibility = System.Windows.Visibility.Visible;
                    statusText.Text = "Initializing geolocation...";
                    break;
                case GeoPositionStatus.NoData:
                    // The location service is working, but it cannot get location data
                    // Alert the user and enable the Stop Location button
                    if (statusText.Visibility == System.Windows.Visibility.Collapsed)
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

        void ResetMap()
        {
            Location ppLoc = new Location(0, 0);
            mapMain.SetView(ppLoc, 1);

            //update pushpin location and show
            MapLayer.SetPosition(ppLocation, ppLoc);
            ppLocation.Visibility = System.Windows.Visibility.Collapsed;
        }

        void QueryCidLocation()
        {
            // TODO: get location data and put it into cid_dist and cid_angle
            // call UpdateCid() when new CID data is received
        }

        void UpdateCid()
        {
            var accentBrush = (Brush)Application.Current.Resources["PhoneAccentBrush"];

            var pin = new Pushpin
            {
                Location = new Location
                {
                    Latitude = (curr_lat + cid_dist * Math.Cos(cid_angle)),
                    Longitude = (curr_long + cid_dist * Math.Sin(cid_angle))
                },
                Background = accentBrush,
                Content = curr_cid,
            };

            mapLayer.AddChild(pin, pin.Location);
        }
    }
}
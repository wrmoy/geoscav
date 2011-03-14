using System;
using System.Device.Location;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Controls.Maps;
using Microsoft.Phone.Notification;
using Microsoft.Phone.Tasks;
using Newtonsoft.Json;

namespace GeoScav
{
    public partial class MapPage : PhoneApplicationPage
    {
        // token parameter to use for server interactions
        private string token;

        // listens for coordinate changes
        private GeoCoordinateWatcher watcher;

        // current location
        private double curr_lat;
        private double curr_long;

        // current and last CIDs
        private int curr_cid = 1;
        private int last_cid = -1;

        // coords of next check-in point
        private double cid_lat;
        private double cid_long;

        // boolean that holds whether or not the player is within check-in range
        private bool isWithinRange = false;

        // boolean that holds whether or not the player has taken a picture
        private bool pictureTaken = false;

        // last picture taken
        private Stream lastPic;

        // Credentials
        private readonly CredentialsProvider _credentialsProvider = new ApplicationIdCredentialsProvider(App.Id);

        private enum SendType : int
        {
            CheckPointQuery = 0,
            CheckIn,
            CheckOut,
            UploadPic,
            GetPic,
            GetN
        }

        public MapPage()
        {
            InitializeComponent();
            // Make sure notification client is connected
            NotificationClient.Current.Connect();
            NotificationClient.Current.NotificationReceived += new EventHandler(getNotification);
            NotificationClient.Current.UriUpdated += new EventHandler(setUri);

            // Reinitialize the GeoCoordinateWatcher
            watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High);
            watcher.MovementThreshold = 5;//distance in metres

            // Add event handlers for StatusChanged and PositionChanged events
            watcher.StatusChanged += new EventHandler<GeoPositionStatusChangedEventArgs>(watcher_StatusChanged);
            watcher.PositionChanged += new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(watcher_PositionChanged);

            // Start data acquisition
            watcher.Start();

            // Ask for next check-in point
            DisplayInfoText("Retrieving first checkpoint", 10);
            QueryCidLocationWithWait(10);
            // Ask for number of checkpoints
            send(App.ServerAddr + "get_n", SendType.GetN);
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
        private void MyPositionChanged(GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            // Update last location
            curr_lat = e.Position.Location.Latitude;
            curr_long = e.Position.Location.Longitude;

            // Update the map to show the current location
            var ppLoc = new GeoCoordinate(e.Position.Location.Latitude, e.Position.Location.Longitude);
            mapMain.SetView(ppLoc, 18);

            //update pushpin location and show
            youLayer.Children.Clear();
            var pin = new Pushpin
            {
                Location = new GeoCoordinate
                {
                    Latitude = curr_lat,
                    Longitude = curr_long
                },
                Content = "You",
            };
            youLayer.AddChild(new Pushpin(), ppLoc);

            // Check if the check-in point is in range
            ProximityCheck();
        }

        /// <summary>
        /// Custom method called from the StatusChanged event handler
        /// </summary>
        /// <param name="e"></param>
        private void MyStatusChanged(GeoPositionStatusChangedEventArgs e)
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

        private void ResetMap()
        {
            mapMain.SetView(new GeoCoordinate(34.41237775, -119.86041103), 15);
            youLayer.Children.Clear();
        }

        // Only updates check-in info (cid_dist, cid_angle, curr_cid)
        private void QueryCidLocation()
        {
            // ask for updates to the cid_dist and cid_angle
            send(App.ServerAddr + "query?cid=" + curr_cid + "&latitude=" + curr_lat + "&longitude=" + curr_long + "&token=" + token, SendType.CheckIn);
        }

        // add a new pushpin where the next check-in point is
        private void AddCheckInPoint(double cid_dist, double cid_angle)
        {
            // color
            var accentBrush = (Brush)Application.Current.Resources["PhoneAccentBrush"];
            // create new pushpin
            var pin = new Pushpin
            {
                Location = new GeoCoordinate
                {
                    Latitude = curr_lat + cid_dist * Math.Cos(cid_angle),
                    Longitude = curr_long + cid_dist * Math.Sin(cid_angle)
                },
                Background = accentBrush,
                Content = curr_cid,
            };
            // add it to the map
            pinLayer.AddChild(pin, pin.Location);
            // update internal variables
            cid_lat = curr_lat + cid_dist * Math.Cos(cid_angle);
            cid_long = curr_long + cid_dist * Math.Sin(cid_angle);
        }

        // check if the player is within range of the next check-in point
        private void ProximityCheck()
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
        private void checkIn(object sender, RoutedEventArgs e)
        {
            // do check-in with the server
            send(App.ServerAddr + "checkin?cid=" + curr_cid + "&latitude=" + curr_lat + "&longitude=" + curr_long + "&token=" + token, SendType.CheckIn);
        }

        // check-out procedures
        private void checkOut(object sender, RoutedEventArgs e)
        {
            // check out
            send(App.ServerAddr + "cancel_checkin?cid=" + curr_cid + "&token=" + token, SendType.CheckOut);
        }

        private void getPic(object sender, RoutedEventArgs e)
        {
            // get picture URL from server
            // display it in a popup?
            // TODO
        }

        private void takePic(object sender, RoutedEventArgs e)
        {
            // take picture
            CameraCaptureTask camera = new CameraCaptureTask();
            camera.Show();
            camera.Completed += cameraTaskComplete;
        }

        private void cameraTaskComplete(object sender, PhotoResult e)
        {
            if (e.TaskResult == TaskResult.OK)
            {
                pictureTaken = true;
                lastPic = e.ChosenPhoto;
                // then upload image to server
                // TODO
            }
        }

        // send a url
        private void send(string url, SendType type)
        {
            WebClient c = new WebClient();
            switch (type)
            {
                case SendType.CheckPointQuery:
                    c.DownloadStringCompleted += new DownloadStringCompletedEventHandler(DownloadCPQCompleted);
                    break;
                case SendType.CheckIn:
                    c.DownloadStringCompleted += new DownloadStringCompletedEventHandler(DownloadCICompleted);
                    break;
                case SendType.CheckOut:
                    c.DownloadStringCompleted += new DownloadStringCompletedEventHandler(DownloadCOCompleted);
                    break;
                case SendType.UploadPic:
                    c.DownloadStringCompleted += new DownloadStringCompletedEventHandler(DownloadUPCompleted);
                    break;
                case SendType.GetPic:
                    c.DownloadStringCompleted += new DownloadStringCompletedEventHandler(DownloadGPCompleted);
                    break;
                case SendType.GetN:
                    c.DownloadStringCompleted += new DownloadStringCompletedEventHandler(DownloadGNCompleted);
                    break;
                default:
                    break;
            }
            c.DownloadStringAsync(new Uri(url));
        }

        #region Notification handling

        private void getNotification(object s, EventArgs e)
        {
            // Convert to string
            HttpNotificationEventArgs eargs = (HttpNotificationEventArgs)e;
            StreamReader reader = new StreamReader(eargs.Notification.Body);
            string bodytext = reader.ReadToEnd();
            DisplayInfoText(bodytext, 30);
            // Parse HTTP request
            //WebClient c = new WebClient();

            string[] parameters = bodytext.Split('&');
            foreach (string kv in parameters)
            {
                string[] temp = kv.Split('=');
            }
            //var decodedUrl = HttpUtility.UrlDecode(bodytext);
        }

        private void setUri(object s, EventArgs e)
        {
            NotificationChannelUriEventArgs eargs = (NotificationChannelUriEventArgs)e;
            rid = eargs.ChannelUri.ToString();
            if (token != null)
                send(App.ServerAddr + "update_reg_id?token=" + token + "&registration_id=" + rid, SendType.UpdateRegid);
            registerButton.IsEnabled = true;
        }

        #endregion

        #region JSON parsing

        /* parses the JSON response from the server */
        // CheckPointQuery
        private void DownloadCPQCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            double cid_dist, cid_angle;
            var deserializedJSON = JsonConvert.DeserializeObject<item>(e.Result);
            if (deserializedJSON.status.code == 0)
            {
                cid_dist = deserializedJSON.response.distance;
                cid_angle = deserializedJSON.response.angle;
                AddCheckInPoint(cid_dist, cid_angle);
            }
            else
                DisplayInfoText("Error: " + deserializedJSON.status.message, 5);
        }

        /* parses the JSON response from the server */
        // CheckIn
        private void DownloadCICompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            var deserializedJSON = JsonConvert.DeserializeObject<item>(e.Result);
            if (deserializedJSON.status.code == 0)
            {
                checkInButton.Visibility = System.Windows.Visibility.Collapsed;
                checkOutButton.Visibility = System.Windows.Visibility.Visible;
                getPicButton.IsEnabled = true;
                takePicButton.IsEnabled = true;

                DisplayInfoText("Hint: " + deserializedJSON.response.hint, 5);
            }
            else
                DisplayInfoText("Error: " + deserializedJSON.status.message, 5);
        }

        /* parses the JSON response from the server */
        // CheckOut
        private void DownloadCOCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            var deserializedJSON = JsonConvert.DeserializeObject<item>(e.Result);
            if (deserializedJSON.status.code == 0)
            {
                DisplayInfoText("Checked out", 3);

                // if the player has taken a picture, then get next checkpoint
                if (pictureTaken)
                {
                    // update cid and next chkpt
                    curr_cid++;
                    if (curr_cid > last_cid)
                    {
                        DisplayInfoText("Congratulations, you finished!", 10);
                        checkOutButton.Visibility = System.Windows.Visibility.Collapsed;
                        checkInButton.Visibility = System.Windows.Visibility.Collapsed;
                        getPicButton.Visibility = System.Windows.Visibility.Collapsed;
                        takePicButton.Visibility = System.Windows.Visibility.Collapsed;
                        return;
                    }
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
                    checkOutButton.Visibility = System.Windows.Visibility.Collapsed;
                    checkInButton.Visibility = System.Windows.Visibility.Visible;
                    getPicButton.IsEnabled = false;
                    takePicButton.IsEnabled = false;
                    ProximityCheck();
                }

            }
            else
                DisplayInfoText("Error: " + deserializedJSON.status.message, 5);
        }

        /* parses the JSON response from the server */
        // UploadPicture
        private void DownloadUPCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            var deserializedJSON = JsonConvert.DeserializeObject<item>(e.Result);
            // TODO
        }

        /* parses the JSON response from the server */
        // GetPic
        private void DownloadGPCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            var deserializedJSON = JsonConvert.DeserializeObject<item>(e.Result);
            // TODO
            DisplayInfoText("Grabbing image from server...", 3);
            DisplayImg("http://www.google.com/images/logos/ps_logo2.png", 5);
        }

        /* parses the JSON response from the server */
        private void DownloadGNCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            var deserializedJSON = JsonConvert.DeserializeObject<item>(e.Result);

            if (deserializedJSON.status.code == 0)
                last_cid = deserializedJSON.response.n;
            else
                DisplayInfoText("Error: " + deserializedJSON.status.message, 5);
        }

        #endregion

        #region Async helpers

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

        // Async checkpoint query
        void QueryCidLocationWithWait(int durationSec)
        {
            DispatcherTimer timer = new DispatcherTimer();

            timer.Tick +=
                delegate(object s, EventArgs args)
                {
                    QueryCidLocation();
                    ProximityCheck();
                    timer.Stop();
                };

            timer.Interval = new TimeSpan(0, 0, durationSec); // durationSec * 1sec
            timer.Start();
        }

        // Async image display
        void DisplayImg(String imgUri, int durationSec)
        {
            BitmapImage tempimg = new BitmapImage(new Uri(imgUri));
            photoPreview.Source = tempimg;
            photoPreview.Visibility = System.Windows.Visibility.Visible;

            DispatcherTimer timer = new DispatcherTimer();
            timer.Tick +=
                delegate(object s, EventArgs args)
                {
                    photoPreview.Visibility = System.Windows.Visibility.Collapsed;
                    timer.Stop();
                };

            timer.Interval = new TimeSpan(0, 0, durationSec); // durationSec * 1sec
            timer.Start();
        }

        #endregion
    }
}
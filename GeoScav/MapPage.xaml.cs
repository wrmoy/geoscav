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
        #region Class variables

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

        // first run?
        private bool firstRun = true;

        // boolean that holds whether or not the player has taken a picture
        private bool pictureTaken = false;

        // last treasure picture (from REPORT)
        private string imgUri = "http://www.google.com/images/logos/ps_logo2.png"; // default to Google logo (ironic...though it should never display it)

        #endregion

        // Credentials
        private readonly CredentialsProvider _credentialsProvider = new ApplicationIdCredentialsProvider(App.Id);

        private enum SendType : int
        {
            CheckPointQuery = 0,
            CheckIn,
            CheckOut,
            GetN,
            UpdateRegid
        }

        public MapPage()
        {
            InitializeComponent();
            // Define event handlers
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

            if (firstRun)
            {
                firstRun = false;
                // Ask for next check-in point
                DisplayInfoText("Retrieving first checkpoint", 10);
                QueryCidLocationWithWait(10);
                // Ask for number of checkpoints
                send(GlobalVars.serveraddr + "get_n?token=" + GlobalVars.token, SendType.GetN);
            }
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

        private void ResetMap()
        {
            mapMain.SetView(new GeoCoordinate(34.41237775, -119.86041103), 15);
            youLayer.Children.Clear();
        }

        // Only updates check-in info (cid_dist, cid_angle, curr_cid)
        private void QueryCidLocation()
        {
            // ask for updates to the cid_dist and cid_angle
            send(GlobalVars.serveraddr + "query?cid=" + curr_cid + "&latitude=" + curr_lat + "&longitude=" + curr_long + "&token=" + GlobalVars.token, SendType.CheckPointQuery);
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
                takePicButton.Visibility = System.Windows.Visibility.Visible;
            }
            // otherwise, we don't want to open the buttons again
        }

        // check-in procedures
        private void checkIn(object sender, RoutedEventArgs e)
        {
            // do check-in with the server
            send(GlobalVars.serveraddr + "checkin?cid=" + curr_cid + "&latitude=" + curr_lat + "&longitude=" + curr_long + "&token=" + GlobalVars.token, SendType.CheckIn);
        }

        // check-out procedures
        private void checkOut(object sender, RoutedEventArgs e)
        {
            // check out
            send(GlobalVars.serveraddr + "cancel_checkin?cid=" + curr_cid + "&token=" + GlobalVars.token, SendType.CheckOut);
        }

        private void showPic(object sender, RoutedEventArgs e)
        {
            DisplayImg(imgUri, 10);
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
                string picname = "Treasure " + curr_cid;
                string filename = "treasure_"+curr_cid+"_"+DateTime.Now.Ticks.ToString("x")+".jpeg";
                // then upload image to server
                MemoryStream memStream = new MemoryStream();
                string boundary = "----------------";

                string url = GlobalVars.serveraddr + "upload?token=" + GlobalVars.token + "&cid=" + curr_cid;
                HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                httpWebRequest.ContentType = "multipart/form-data; boundary=" + boundary;
                httpWebRequest.Method = "POST";
                
                byte[] boundarybytes = System.Text.Encoding.UTF8.GetBytes("\r\n--" + boundary + "\r\n");
                memStream.Write(boundarybytes, 0, boundarybytes.Length);

                string header = string.Format("Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\n Content-Type: image/jpeg\r\n\r\n", picname, filename);
                byte[] headerbytes = System.Text.Encoding.UTF8.GetBytes(header);
                memStream.Write(headerbytes, 0, headerbytes.Length);

                byte[] buffer = new byte[1024];
                int bytesRead = 0;
                while ( (bytesRead = e.ChosenPhoto.Read(buffer, 0, buffer.Length)) != 0 )
                {
                    memStream.Write(buffer, 0, bytesRead);
                }

                memStream.Write(boundarybytes, 0, boundarybytes.Length);
                memStream.Close();
                
                httpWebRequest.BeginGetRequestStream((ar) => { GetReqCallback(ar, memStream.ToArray()); }, httpWebRequest);


                /*memStream.Position = 0;
                byte[] tempBuffer = new byte[memStream.Length];
                memStream.Read(tempBuffer, 0, tempBuffer.Length);
                memStream.Close();
                requestStream.Write(tempBuffer, 0, tempBuffer.Length);
                requestStream.Close();

                try
                {
                    WebResponse webResponse = httpWebRequest.GetResponse();
                    Stream stream = webResponse.GetResponseStream();
                    StreamReader reader = new StreamReader(stream);
                    Console.WriteLine(reader.ReadToEnd());
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
                finally
                {
                }
                httpWebRequest = null;*/
            }
        }

        private void GetReqCallback(IAsyncResult asyncResult, byte[] towrite)
        {
            HttpWebRequest req = (HttpWebRequest)asyncResult.AsyncState;
            Stream postStream = req.EndGetRequestStream(asyncResult);
            postStream.Write(towrite, 0, towrite.Length);
            postStream.Close();
            DisplayInfoText("Sending image...", 3);
            req.BeginGetResponse(new AsyncCallback(UploadPicCompleted), req);
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
                case SendType.GetN:
                    c.DownloadStringCompleted += new DownloadStringCompletedEventHandler(DownloadGNCompleted);
                    break;
                case SendType.UpdateRegid:
                    c.DownloadStringCompleted += new DownloadStringCompletedEventHandler(UpdateRegidCompleted);
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
            string[] parameters = bodytext.Split('&');
            foreach (string kv in parameters)
            {
                string[] temp = kv.Split('=');
                switch (temp[0])
                {
                    case "GAMEOVER":
                        DisplayInfoText("GAME OVER", 60);
                        break;
                    case "content[url]":
                        imgUri = temp[1];
                        getPicButton.Visibility = System.Windows.Visibility.Visible;
                        PicButtonTimeout(30);
                        break;
                    case "content[action]":
                        string decodedUrl = HttpUtility.UrlDecode(bodytext);
                        DisplayInfoText(decodedUrl, 5);
                        break;
                }
            }
        }

        private void setUri(object s, EventArgs e)
        {
            NotificationChannelUriEventArgs eargs = (NotificationChannelUriEventArgs)e;
            GlobalVars.rid = eargs.ChannelUri.ToString();
            if (GlobalVars.token != null)
                send(GlobalVars.serveraddr + "update_reg_id?token=" + GlobalVars.token + "&registration_id=" + GlobalVars.rid, SendType.UpdateRegid);
        }

        private void UpdateRegidCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            var deserializedJSON = JsonConvert.DeserializeObject<item>(e.Result);
            if (deserializedJSON.status.code == 0)
            {
            }
            else
            {
                DisplayInfoText("Error (" + deserializedJSON.status.code + "): " + deserializedJSON.status.message, 5);
            }
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
                //cid_dist = deserializedJSON.response.distance;
                cid_dist = 0;
                cid_angle = deserializedJSON.response.angle;
                AddCheckInPoint(cid_dist, cid_angle);
                ProximityCheck();
            }
            else
                DisplayInfoText("Error (" + deserializedJSON.status.code + "): " + deserializedJSON.status.message, 5);
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
                takePicButton.IsEnabled = true;

                DisplayInfoText("Hint: " + deserializedJSON.response.hint, 5);
            }
            else
                DisplayInfoText("Error (" + deserializedJSON.status.code + "): " + deserializedJSON.status.message, 5);
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
                //if (pictureTaken) 
                /* ^^^^ COMMENTED OUT SO THE USER CAN PROCEED */
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
                    takePicButton.Visibility = System.Windows.Visibility.Collapsed;
                    takePicButton.IsEnabled = false;
                    // reset the bool
                    pictureTaken = false;
                }
                /*else // restart the "asking for check-in" process
                {
                    checkOutButton.Visibility = System.Windows.Visibility.Collapsed;
                    checkInButton.Visibility = System.Windows.Visibility.Visible;
                    takePicButton.IsEnabled = false;
                    ProximityCheck();
                }*/
                /* ^^^^ COMMENTED OUT SO THE USER CAN PROCEED */

            }
            else
                DisplayInfoText("Error (" + deserializedJSON.status.code + "): " + deserializedJSON.status.message, 5);
        }

        /* parses the JSON response from the server */
        private void DownloadGNCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            var deserializedJSON = JsonConvert.DeserializeObject<item>(e.Result);

            if (deserializedJSON.status.code == 0)
                last_cid = deserializedJSON.response.n;
            else
                DisplayInfoText("Error (" + deserializedJSON.status.code + "): " + deserializedJSON.status.message, 5);
        }

        /* parses the JSON response from the server */
        // UploadPicture
        private void UploadPicCompleted(IAsyncResult asyncResult)
        {
            HttpWebRequest r = (HttpWebRequest)asyncResult.AsyncState;
            HttpWebResponse response = (HttpWebResponse)r.EndGetResponse(asyncResult);
            Stream responseStream = response.GetResponseStream();
            var deserializedJSON = JsonConvert.DeserializeObject<item>(responseStream.ToString());
            if (deserializedJSON.status.code == 0)
            {
                DisplayInfoText("Uploaded pic", 3);
            }
            else
            {
                DisplayInfoText("Error (" + deserializedJSON.status.code + "): " + deserializedJSON.status.message, 5);
            }
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

        // getPicButton timeout
        void PicButtonTimeout(int durationSec)
        {
            DispatcherTimer timer = new DispatcherTimer();

            timer.Tick +=
                delegate(object s, EventArgs args)
                {
                    getPicButton.Visibility = System.Windows.Visibility.Collapsed;
                    timer.Stop();
                };

            timer.Interval = new TimeSpan(0, 0, durationSec); // durationSec * 1sec
            timer.Start();
        }

        #endregion
    }
}
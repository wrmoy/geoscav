using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Notification;
using System.Windows.Threading;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.IO;

namespace GeoScav
{
    public sealed class NotificationClient
    {
        #region NotificationReceived

        public event EventHandler NotificationReceived;

        protected void OnNotificationReceived()
        {
            EventHandler handler = this.NotificationReceived;

            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        #endregion

        private HttpNotificationChannel httpChannel;
        const string channelName = "GeoScavChannel";
        const int pushConnectTimeout = 30;

        private static NotificationClient current = new NotificationClient();

        public static NotificationClient Current
        {
            get { return current; }
        }

        public string Connect()
        {
            try
            {
                httpChannel = HttpNotificationChannel.Find(channelName);

                if (null != httpChannel)
                {
                    SubscribeToChannelEvents();
                    SubscribeToNotifications();
                    Deployment.Current.Dispatcher.BeginInvoke(() => this.UpdateStatus("Channel recovered"));
                }
                else
                {
                    httpChannel = new HttpNotificationChannel(channelName, "GeoScavChannel");
                    SubscribeToChannelEvents();
                    httpChannel.Open();
                    Deployment.Current.Dispatcher.BeginInvoke(() => this.UpdateStatus("Channel open requested"));
                }
            }
            catch (Exception ex)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() => UpdateStatus("Channel error: " + ex.Message));
            }

            return httpChannel.ChannelUri.ToString();
        }



        private void SubscribeToChannelEvents()
        {
            httpChannel.ChannelUriUpdated += new EventHandler<NotificationChannelUriEventArgs>(httpChannel_ChannelUriUpdated);
            httpChannel.HttpNotificationReceived += new EventHandler<HttpNotificationEventArgs>(httpChannel_HttpNotificationReceived);
            httpChannel.ShellToastNotificationReceived += new EventHandler<NotificationEventArgs>(httpChannel_ShellToastNotificationReceived);
            httpChannel.ErrorOccurred += new EventHandler<NotificationChannelErrorEventArgs>(httpChannel_ErrorOccurred);
        }

        private void httpChannel_ErrorOccurred(object sender, NotificationChannelErrorEventArgs e)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() => UpdateStatus(e.Message));
        }

        private void httpChannel_ShellToastNotificationReceived(object sender, NotificationEventArgs e)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() => this.OnNotificationReceived());
        }

        private void httpChannel_ChannelUriUpdated(object sender, NotificationChannelUriEventArgs e)
        {
           
            SubscribeToNotifications();
            Deployment.Current.Dispatcher.BeginInvoke(() => UpdateStatus("Channel created successfully"));
        }

        private void httpChannel_HttpNotificationReceived(object sender, HttpNotificationEventArgs e)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() => this.OnNotificationReceived());
        }

        private void SubscribeToNotifications()
        {
            try
            {
                if (httpChannel.IsShellToastBound != true)
                    httpChannel.BindToShellToast();
            }
            catch (Exception)
            {
            }

            try
            {
                if (httpChannel.IsShellTileBound != true)
                {
                    Collection<Uri> uris = new Collection<Uri>();
                    uris.Add(new Uri("http://localhost:8000/EarthquakeService/tile"));
                    httpChannel.BindToShellTile(uris);
                }
            }
            catch (Exception)
            {
            }
        }

        private void UpdateStatus(string message)
        {
            Debug.WriteLine(message);
        }
    }
}
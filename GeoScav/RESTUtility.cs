// Brian Dunlay
// Mar 8, 2011

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
using Newtonsoft.Json;

using System.IO;
using System.Collections.Generic;

namespace GeoScav
{
    public class item
    {
        public status status { get; set; }
        public response response { get; set; }
    }
    public class status
    {
        public int code { get; set; }
        public string message { get; set; }
    }
    public class response
    {
        public string token { get; set; }
        public int distance { get; set; }
        public int angle { get; set; }
    }

    public class RESTUtility
    {
        item deserializedJSON;
        bool isReady;

        RESTUtility()
        {
            deserializedJSON = null;
            isReady = false;
        }

        /* sends the url string to the server */
        public void send(string url)
        {
            WebClient c = new WebClient();
            c.DownloadStringAsync(new Uri(url));
            c.DownloadStringCompleted += new DownloadStringCompletedEventHandler(c_DownloadStringCompleted);
        }
        
        /* parses the JSON response from the server */
        void c_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            lock (this)
            {
                deserializedJSON = JsonConvert.DeserializeObject<item>(e.Result);
                isReady = true;
            }
        }

        /* returns the status object */
        public status status()
        {
            return deserializedJSON.status;
        }

        /* returns the response object */
        public response response()
        {
            return deserializedJSON.response;
        }

        /* Checks if the server response has been received and parsed */
        public bool ready()
        {
            return isReady;
        }
    }
}

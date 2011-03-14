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
        public string hint { get; set; }
        public double distance { get; set; }
        public double angle { get; set; }
        public int n { get; set; }
    }

    public class pushresponse
    {
        public string type { get; set; }
        public class content
        {
            public string action { get; set; }
            public string url { get; set; }
        }
    }

    public class RESTUtility
    {
        public item deserializedJSON;
        bool isReady = false;
        public bool isInitialized;

        public RESTUtility()
        {
            isInitialized = true;
            deserializedJSON = null;
            isReady = false;
        }

        /* sends the url string to the server */
        public void send(string url)
        {
            WebClient c = new WebClient();
            c.DownloadStringCompleted += new DownloadStringCompletedEventHandler(DownloadStringCompleted);
            c.DownloadStringAsync(new Uri(url));
         //   c.UploadStringCompleted += new UploadStringCompletedEventHandler(c_UploadStringCompleted);
        //   c.UploadStringAsync(new Uri(url), "");
        }
        


        /* parses the JSON response from the server */
        public void DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
                isReady = true;
                deserializedJSON = JsonConvert.DeserializeObject<item>(e.Result);
                
        }

        /* returns the status object */
        public status getStatus()
        {
            return deserializedJSON.status;
        }

        /* returns the response object */
        public response getResponse()
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

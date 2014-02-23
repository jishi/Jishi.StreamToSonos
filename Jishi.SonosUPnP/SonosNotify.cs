using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Xml.Linq;
using log4net;
using HttpListener = Mono.Net.HttpListener;

namespace Jishi.SonosUPnP
{
    public class SonosNotify
    {
        private readonly HttpListener listener;
        private string notifyUrl;

        private ILog log = log4net.LogManager.GetLogger(typeof (SonosNotify));
        private readonly IDictionary<string, string> subscriptions = new Dictionary<string, string>();
        private readonly IDictionary<string, Timer> timers = new Dictionary<string, Timer>(); 
        public IPEndPoint LocalEndpoint { get; set; }
        public event NotificationReceivedEventHandler NotificationReceived;
        // Singleton 
        private static readonly SonosNotify instance = new SonosNotify();

        public static SonosNotify Instance
        {
            get { return instance; }
        }

        public string NotifyUrl
        {
            get { return notifyUrl; }
            set { notifyUrl = value; }
        }

        private SonosNotify()
        {
            listener = new HttpListener();
            var port = FindAvailablePort();

            notifyUrl = string.Format("http://*:{0}/notify/", port);
            Debug.WriteLine(notifyUrl);
            listener.Prefixes.Add(notifyUrl);
            listener.Start();
            listener.BeginGetContext(HandleRequest, listener);
        }


        private void HandleRequest(IAsyncResult result)
        {
            var listener = (HttpListener) result.AsyncState;
            // Call EndGetContext to complete the asynchronous operation.
            var context = listener.EndGetContext(result);
            var request = context.Request;
            var reader = XElement.Load(request.InputStream);
            XNamespace ns = "urn:schemas-upnp-org:event-1-0";
            var args = new NotificationReceivedEventHandlerArgs
                           {
                               Properties = reader.Elements(ns + "property").Select(GetProperty).ToList()
                           };

            NotificationReceived.Invoke(this, args);
            // Obtain a response object.
            var response = context.Response;
            response.OutputStream.Close();
            listener.BeginGetContext(HandleRequest, listener);
        }

        private SonosProperty GetProperty(XElement property)
        {
            var valueNode = (XElement) property.FirstNode;
            return new SonosProperty
                       {
                           Name = valueNode.Name.ToString(),
                           Value = (string) valueNode
                       };
        }

        private static int FindAvailablePort()
        {
            var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            var tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();

            var startingPort = 3400;
            bool isTaken;
            do
            {
                startingPort++;
                isTaken = tcpConnInfoArray.Any(tcpi => tcpi.LocalEndPoint.Port == startingPort);
            } while (isTaken);

            return startingPort;
        }

        public void SubscribeToEvent(string subscribeUri)
        {
            var client = new ExtendedWebClient();
            //SUBSCRIBE /MediaServer/ContentDirectory/Event HTTP/1.1
            //HOST: 192.168.1.152:1400
            //USER-AGENT: Linux UPnP/1.0 Sonos/19.3-53220 (WDCR:Microsoft Windows NT 6.1.7601 Service Pack 1)
            //CALLBACK: <http://192.168.1.128:3400/notify>
            //NT: upnp:event
            //TIMEOUT: Second-3600
            // find local IP
            var uri = listener.Prefixes.FirstOrDefault();

            if (uri == null)
                return;

            var localUri = uri.Replace("*", LocalEndpoint.Address.ToString());

            //client.Headers.Add("USER-AGENT", "Linux UPnP/1.0 Sonos/19.3-53220 (Custom:Jishi.SonosUPnP)");
            if (subscriptions.ContainsKey(subscribeUri))
            {
                // Resubscription
                client.Headers.Add("SID", subscriptions[subscribeUri]);
                log.DebugFormat("Resubscribing to {0} with SID {1}", subscribeUri, subscriptions[subscribeUri]);
            }
            else
            {
                client.Headers.Add("CALLBACK", string.Format("<{0}>", localUri));
                client.Headers.Add("NT", "upnp:event");
                log.DebugFormat("New subscribe to {0}", subscribeUri);
            }
            client.Headers.Add("TIMEOUT", "Second-600");

            try
            {
                client.UploadString(subscribeUri, "SUBSCRIBE", string.Empty);
                var sid = client.ResponseHeaders["SID"];
                subscriptions[subscribeUri] = sid;
                timers[subscribeUri] = new Timer(Resubscribe, subscribeUri, TimeSpan.FromSeconds(500), TimeSpan.FromMilliseconds(-1));
            }
            catch (WebException ex)
            {
                log.ErrorFormat("Error subscribing to {0}. {1}", subscribeUri, ex.Message);
                if (subscriptions.ContainsKey(subscribeUri))
                {
                    subscriptions.Remove(subscribeUri);
                    timers[subscribeUri] = new Timer(Resubscribe, subscribeUri, TimeSpan.FromSeconds(10), TimeSpan.FromMilliseconds(-1));
                }
            }
        }

        private void Resubscribe(object state)
        {
            var subscribeUri = (string) state;
            SubscribeToEvent(subscribeUri);
        }
    }

    public struct SonosProperty
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public delegate void NotificationReceivedEventHandler(object sender, NotificationReceivedEventHandlerArgs args);

    public class NotificationReceivedEventHandlerArgs
    {
        public XElement Reader { get; set; }
        public string Sid { get; set; }
        public string Uuid { get; set; }

        public List<SonosProperty> Properties { get; set; }
    }
}
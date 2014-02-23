using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Xml.Linq;
using HttpListener = Mono.Net.HttpListener;
using HttpListenerContext = Mono.Net.HttpListenerContext;
using HttpListenerRequest = Mono.Net.HttpListenerRequest;
using HttpListenerResponse = Mono.Net.HttpListenerResponse;

namespace Jishi.SonosUPnP
{
    public class SonosNotify
    {
        private HttpListener listener;
        private string notifyUrl;

        // Singleton 
        private static SonosNotify instance = new SonosNotify();

        public static SonosNotify Instance
        {
            get { return instance; }
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

        public string NotifyUrl
        {
            get { return notifyUrl; }
            set { notifyUrl = value; }
        }

        public IPEndPoint LocalEndpoint { get; set; }

        public event NotificationReceivedEventHandler NotificationReceived;


        private void HandleRequest(IAsyncResult result)
        {
            HttpListener listener = (HttpListener) result.AsyncState;
            // Call EndGetContext to complete the asynchronous operation.
            HttpListenerContext context = listener.EndGetContext(result);
            HttpListenerRequest request = context.Request;
            Console.WriteLine(request.Headers);
            var reader = XElement.Load(request.InputStream);
            XNamespace ns = "urn:schemas-upnp-org:event-1-0";
            var args = new NotificationReceivedEventHandlerArgs
                           {
                               Properties = reader.Elements(ns + "property").Select(x => GetProperty(x)).ToList()
                           };

            NotificationReceived.Invoke(this, args);
            // Obtain a response object.
            HttpListenerResponse response = context.Response;
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

        private int FindAvailablePort()
        {
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            TcpConnectionInformation[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();

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
            var client = new WebClient();
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

            client.Headers.Add("USER-AGENT", "Linux UPnP/1.0 Sonos/19.3-53220 (WDCR:Jishi.SonosPartyMode)");
            client.Headers.Add("CALLBACK", string.Format("<{0}>", localUri));
            client.Headers.Add("NT", "upnp:event");
            client.Headers.Add("TIMEOUT", "Second-600");

            Console.WriteLine("Subscribing to {0} with url {1}", subscribeUri, localUri);

            client.UploadString(subscribeUri, "SUBSCRIBE", string.Empty);
        }

        private IPAddress FindIpAddress(string subscribeUri)
        {
            //var interfaces = new Dictionary<int, IPInterfaceProperties>();

            //foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            //{
            //    IPInterfaceProperties ipProps = nic.GetIPProperties();
            //    var ipv4Props = ipProps.GetIPv4Properties();
            //    if (ipv4Props == null) continue;
            //    interfaces.Add(ipv4Props.Index, ipProps);
            //}

            //var firstInterface = interfaces.OrderBy(i => i.Key).FirstOrDefault();

            //var ipAddress = firstInterface.Value.UnicastAddresses.First(ip => !ip.Address.IsIPv6LinkLocal);

            //return ipAddress.Address;

            // Find ip and port to use
            var uri = new Uri(subscribeUri);
            var host = uri.Host;
            var port = uri.Port;
            IPEndPoint endpoint;
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.IPv4))
            {
                socket.Connect(host, port);
                // We can now find local IP
                endpoint = (IPEndPoint) socket.LocalEndPoint;
                socket.Close();
                socket.Dispose();
            }

            return endpoint.Address;
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
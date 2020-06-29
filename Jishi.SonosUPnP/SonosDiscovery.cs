using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Jishi.SonosUPnP
{
	public class SonosDiscovery : IDisposable
	{
		private IPEndPoint groupEndpoint = new IPEndPoint(IPAddress.Parse("239.255.255.250"), 1900);
		private IList<SonosZone> zones = new List<SonosZone>();
		private IList<SonosPlayer> players = new List<SonosPlayer>();
		private HashSet<string> knownLocations = new HashSet<string>();

		public SonosDiscovery()
		{
			var foundAvailablePort = false;
			// 1900 is the default ssdp port, we try to find the first available after that, since windows and sonos usually occupies it
			var port = 1902;
		    var addresses = GetLocalEndpoints();
            

			foreach (var ip in addresses)
			{
				try
				{
				    var localEndpoint = new IPEndPoint(ip, port);
					var udpClient = new UdpClient(localEndpoint);
                    // okay, join multicast
                    udpClient.JoinMulticastGroup(IPAddress.Parse("239.255.255.250"));
				    StartMSearchSequence(udpClient, localEndpoint);
				}
				catch (SocketException ex)
				{
					// We couldn't bind, increase port number
					Console.WriteLine("Port {0} was taken, trying {1}", port, port + 1);
					port++;
				}
			} 

			

            //int tries = 5;
            //while (tries-- > 0)
            //{
            //    new Task(StartMSearchSequence).Start();
            //}

			SonosNotify.Instance.NotificationReceived += NotificationHandler;
		}

		private void NotificationHandler(object sender, NotificationReceivedEventHandlerArgs args)
		{
			foreach (var property in args.Properties)
			{
				if (property.Name != "ZoneGroupState") continue;

				UpdateTopology(property.Value);
			}
		}

		private void UpdateTopology(string value)
		{          
			var xml = XElement.Parse(value);
			Console.WriteLine(value);

			Zones.Clear();
			foreach (var groups in xml.Elements("ZoneGroups"))
			foreach (var zoneNode in groups.Elements("ZoneGroup"))
			{
				var zone = new SonosZone();
				foreach (var playerNode in zoneNode.Elements("ZoneGroupMember"))
				{
                    // We ignore invisible units (bonded, bridges, stereopairs)
                    if (String.Equals((string)playerNode.Attribute("Invisible"), "1")) continue;
				    
                    var url = new Uri( (string)playerNode.Attribute( "Location" ) );
					var baseUrl = string.Format( "{0}://{1}", url.Scheme, url.Authority );
					var player = new SonosPlayer( (string)playerNode.Attribute( "UUID" ), (string)playerNode.Attribute( "ZoneName" ), baseUrl );
					if ((string)zoneNode.Attribute("Coordinator") == player.Uuid)
					{
						zone.Coordinator = player;
					}

					zone.Members.Add(player);
				}
                if (zone.Members.Count > 0)
				    Zones.Add( zone );
			}

			TopologyChanged.Invoke(null, new TopologyChangedEventHandlerArgs {});
		}

		private void StartMSearchSequence(UdpClient client, IPEndPoint endpoint)
		{
			Console.WriteLine("Starting M-Search sequence from {0}", endpoint);
			const string mSearch =
				@"M-SEARCH * HTTP/1.1
HOST: 239.255.255.250:1900
MAN: ""ssdp:discover""
MX: 1
ST: urn:schemas-upnp-org:device:ZonePlayer:1

";
			var mSearchBytes = Encoding.ASCII.GetBytes(mSearch);
			client.BeginReceive( ReceivedPlayerNotification, new MSearchState { Client = client, Endpoint = endpoint } );
			client.Send( mSearchBytes, mSearchBytes.Length, groupEndpoint );
		}

		private void ReceivedPlayerNotification(IAsyncResult ar)
		{
			var endpoint = new IPEndPoint(IPAddress.Any, 0);
		    var state = (MSearchState)ar.AsyncState;
			var client = state.Client;
			string response;
			if (ar.IsCompleted)
			{
				var byteResult = client.EndReceive( ar, ref endpoint );
				response = Encoding.ASCII.GetString(byteResult);
				client.Close();
				var topologyUri = string.Format("http://{0}:1400/ZoneGroupTopology/Event", endpoint.Address);
			    SonosNotify.Instance.LocalEndpoint = state.Endpoint;
				SonosNotify.Instance.SubscribeToEvent( topologyUri );
			}
			
			
		}

        private IList<IPAddress> GetLocalEndpoints()
        {
            //var interfaces = new Dictionary<int, IPInterfaceProperties>();
            var addresses = new List<IPAddress>();

            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus != OperationalStatus.Up) continue;
                if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                IPInterfaceProperties ipProps = nic.GetIPProperties();
	            try
	            {
		            var ipv4Props = ipProps.GetIPv4Properties();
		            if ( ipv4Props == null )
			            continue;
		            //interfaces.Add(ipv4Props.Index, ipProps);
		            addresses.AddRange(
			            ipProps.UnicastAddresses.Where( u => u.Address.AddressFamily.Equals( AddressFamily.InterNetwork ) )
				            .Select( x => x.Address ) );
	            }
	            catch ( NetworkInformationException ex )
	            {
		            Console.WriteLine( "Interface doesn't have any IPv4 properties {0}", nic.Name );
	            }
            }

            return addresses;

            //var firstInterface = interfaces.OrderBy(i => i.Key).FirstOrDefault();

            //var ipAddress = firstInterface.Value.UnicastAddresses.First(ip => !ip.Address.IsIPv6LinkLocal);

            //return ipAddress.Address;
        }

		private void FetchPropertiesAndCreatePlayer(string response)
		{
			var headers = ParseResponse(response);
			// Check for duplicates
			var location = headers["LOCATION"];
			if (knownLocations.Contains(location))
			{
				Console.WriteLine("skipping " + location);
				return;
			}

			knownLocations.Add(location);
			var url = new Uri(location);
			//var discoveryTask = new Task(GetProperties, url);
			//discoveryTask.Start();
		}

		//private void GetProperties(object state)
		//{
		//	Uri url = (Uri) state;
		//	var properties = new SonosProperties();
		//	var sonosBaseUrl = string.Format("{0}://{1}", url.Scheme, url.Authority);
		//	var webClient = new ExtendedWebClient();
			
			


		//	bool successfulRequest = false;
		//	string upnpDescriptorXml = null;
		//	int tries = 5;
		//	do
		//	{
		//		try
		//		{
		//			upnpDescriptorXml = webClient.DownloadString(url);
		//			successfulRequest = true;
		//		}
		//		catch (WebException ex)
		//		{
		//			Console.WriteLine("failed getting url {0}", url.AbsoluteUri);
		//		}
		//	} while (!successfulRequest && tries-- > 0);

		//	Console.WriteLine("got url " + url);
			
		//	if (upnpDescriptorXml == null) return;

		//	var doc = XElement.Parse(upnpDescriptorXml);
		//	XNamespace ns = doc.Attribute("xmlns").Value;
		//	var device = doc.Element(ns + "device");
		//	properties.UDN = device.Element(ns + "UDN").Value;
		//	properties.RoomName = device.Element(ns + "roomName").Value;
		//	properties.ModelName = device.Element(ns + "modelName").Value;

		//	properties.SubscribeUrls = device.Descendants(ns + "eventSubURL").Select(x => x.Value).ToList();

		//	ZoneType type;
		//	if (ZoneType.TryParse(device.Element(ns + "zoneType").Value, out type))
		//	{
		//		properties.ZoneType = type;
		//	}

		//	if (properties.ZoneType == ZoneType.Bridge) return;

		//	Console.WriteLine("Creating player " + properties.RoomName);
		//	var player = new SonosPlayer(properties, sonosBaseUrl);
		//	Players.Add(player);
		//	TopologyChanged.Invoke(player, new TopologyChangedEventHandlerArgs {Player = player});

		//	// Subscribe to events
			
		//	foreach (var eventUrl in properties.SubscribeUrls)
		//	{
		//		if ( eventUrl.StartsWith( "/ZoneGroupTopology/" ) )
		//			notify.SubscribeToEvent(sonosBaseUrl + eventUrl);
		//	}

		//}

		private Dictionary<string, string> ParseResponse(string response)
		{
			var headers = new Dictionary<string, string>();
			var rows = response.Split('\n');
			foreach (var row in rows)
			{
				if (string.IsNullOrEmpty(row))
					continue;
				var split = row.Split(new[] {':'}, 2);
				if (split.Length < 2)
					continue;
				headers.Add(split[0].Trim(), split[1].Trim());
			}

			return headers;
		}

		public IList<SonosPlayer> Players
		{
			get { return players; }
			set { players = value; }
		}

		public IList<SonosZone> Zones
		{
			get { return zones; }
			set { zones = value; }
		}

		public event TopologyChangedEventHandler TopologyChanged;
	    
        public void Dispose()
	    {
	        
	    }
	}

    internal class MSearchState
    {
        public UdpClient Client { get; set; }
        public IPEndPoint Endpoint { get; set; }
    }

    public delegate void TopologyChangedEventHandler(object sender, TopologyChangedEventHandlerArgs args);

	public class TopologyChangedEventHandlerArgs
	{
		public SonosPlayer Player;
	}
}

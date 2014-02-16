using System;
using System.Collections.Generic;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Jishi.SonosUPnP
{
	public class SonosPlayer
	{
		private Task discoveryTask;
		private Uri url;
		//private string sonosBaseUrl;
		private SonosProperties properties;
		private SonosStatus status;
		private SimpleSoapClient soapClient;

		public SonosPlayer Coordinator { get; set; }
		public string RoomName { get; set; }
		public string Uuid { get; set; }

		public SonosPlayer(string uuid, string roomName, string sonosBaseUrl)
		{
			Uuid = uuid;
			RoomName = roomName;
			soapClient = new SimpleSoapClient(sonosBaseUrl + "/MediaRenderer/AVTransport/Control");
		}

		public void SetAvTransportUri(string uri, string metaData = "")
		{
			soapClient.SetAVTransportURI(0, uri, metaData);
		}

		public void Pause()
		{
			soapClient.Pause(0);
		}

		public PositionInfoResponse GetPositionInfo()
		{
			return soapClient.GetPositionInfo(0);
		}

		public SonosStatus Status
		{
			get
			{
				VerifySonosStatus();
				return status;
			}
		}

		public bool IsPlayer
		{
			get { return properties.ZoneType == ZoneType.Player; }
		}

		public SonosProperties Properties
		{
			get { return properties; }
			set { properties = value; }
		}

		private void VerifySonosStatus()
		{
			if (status != null)
			{
				return;
			}

			// Ask the player of it's current status
		}


		public void EnqueueTrack(string uri, string metadata)
		{
			soapClient.AddURIToQueue(0, uri, metadata, 0, false);
		}

		public void Play()
		{
			soapClient.Play(0);
		}
	}

	public class PositionInfo
	{
		public string Track { get; set; }

		public override string ToString()
		{
			return string.Format("{0}", Track);
		}
	}

	public class SimpleSoapClient
	{
		private ChannelFactory<IAVTransportService> factory;

		public SimpleSoapClient(string baseAddress)
		{
			//ServiceHost host = new ServiceHost( typeof( SimpleSoapClient ), new Uri( baseAddress ) );
			//host.AddServiceEndpoint( typeof( IAVTransportService ), GetBinding(), "" );
			//host.Description.Behaviors.Add( new ServiceMetadataBehavior { HttpGetEnabled = true } );
			//host.Open();
			//Console.WriteLine( "Host opened" );

			factory = new ChannelFactory<IAVTransportService>(GetBinding(), new EndpointAddress(baseAddress));
		}

		static Binding GetBinding()
		{
			return new BasicHttpBinding();
		}


		public void Invoke()
		{
		}

		public PositionInfoResponse GetPositionInfo(int InstanceID)
		{
			IAVTransportService service = factory.CreateChannel();
			var response = service.GetPositionInfo(new PositionInfoRequest {InstanceID = 0});
			((IClientChannel) service).Close();

			return response;
		}

		public async void SetAVTransportURI(int instanceID, string currentURI, string currentURIMetaData)
		{
			IAVTransportService service = factory.CreateChannel();
			await service.SetAVTransportURIAsync(new SetAvTransportUriRequest
				                          {
					                          InstanceID = instanceID,
					                          CurrentURI = currentURI,
					                          CurrentURIMetaData = currentURIMetaData
				                          });
			((IClientChannel) service).Close();
		}

		public void Pause(int instanceID)
		{
			IAVTransportService service = factory.CreateChannel();
			var response = service.Pause(new PauseRequest {InstanceID = instanceID});
			((IClientChannel) service).Close();
		}

		public EnqueueResponse AddURIToQueue(int instanceID, string enqueuedURI, string enqueuedURIMetaData,
		                                     int desiredFirstTrackNumberEnqueued, bool enqueueAsNext)
		{
			IAVTransportService service = factory.CreateChannel();
			var response =
				service.AddURIToQueue(new EnqueueRequest
					                      {
						                      InstanceID = instanceID,
						                      EnqueuedURI = enqueuedURI,
						                      DesiredFirstTrackNumberEnqueued = desiredFirstTrackNumberEnqueued,
						                      EnqueueAsNext = enqueueAsNext,
						                      EnqueuedURIMetaData = enqueuedURIMetaData
					                      });
			((IClientChannel) service).Close();
			return response;
		}

		public void Play(int instanceID)
		{
			IAVTransportService service = factory.CreateChannel();
			service.Play(new PlayRequest
				             {
					             InstanceID = instanceID,
                                 Speed = 1
				             });
			((IClientChannel) service).Close();
		}
	}

	public enum SonosStatus
	{
		Paused
	}

	public class SonosProperties
	{
		public string UDN { get; set; }
		public string ModelName { get; set; }
		public ZoneType ZoneType { get; set; }
		public string RoomName { get; set; }
		public List<string> SubscribeUrls { get; set; }

		public SonosStatus Status
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}
	}

	[ServiceContract(Namespace = "urn:schemas-upnp-org:service:AVTransport:1")]
	internal interface IAVTransportService
	{
		[OperationContract(Action = "urn:schemas-upnp-org:service:AVTransport:1#SetAVTransportURI")]
		Task SetAVTransportURIAsync(SetAvTransportUriRequest req);

		[OperationContract(Action = "urn:schemas-upnp-org:service:AVTransport:1#GetPositionInfo")]
		PositionInfoResponse GetPositionInfo(PositionInfoRequest req);

		[OperationContract(Action = "urn:schemas-upnp-org:service:AVTransport:1#Pause")]
		PauseResponse Pause(PauseRequest args);

		[OperationContract(Action = "urn:schemas-upnp-org:service:AVTransport:1#AddURIToQueue")]
		EnqueueResponse AddURIToQueue(EnqueueRequest args);

		[OperationContract(Action = "urn:schemas-upnp-org:service:AVTransport:1#Play")]
		void Play(PlayRequest playRequest);
	}

	[MessageContract(WrapperName = "SetAVTransportURI", WrapperNamespace = "urn:schemas-upnp-org:service:AVTransport:1",
		IsWrapped = true)]
	internal class SetAvTransportUriRequest
	{
		[MessageBodyMember(Namespace = "")]
		public int InstanceID { get; set; }

		[MessageBodyMember(Namespace = "")]
		public string CurrentURI { get; set; }

		[MessageBodyMember(Namespace = "")]
		public string CurrentURIMetaData { get; set; }
	}

	[MessageContract(WrapperName = "Play", WrapperNamespace = "urn:schemas-upnp-org:service:AVTransport:1",
		IsWrapped = true)]
	internal class PlayRequest
	{
		[MessageBodyMember(Namespace = "")]
		public int InstanceID { get; set; }
        [MessageBodyMember(Namespace = "")]
        public int Speed { get; set; }
	}

	[MessageContract(WrapperName = "AddURIToQueue", WrapperNamespace = "urn:schemas-upnp-org:service:AVTransport:1",
		IsWrapped = true)]
	internal class EnqueueRequest
	{
		[MessageBodyMember(Namespace = "")]
		public int InstanceID { get; set; }

		[MessageBodyMember(Namespace = "")]
		public string EnqueuedURI { get; set; }

		[MessageBodyMember(Namespace = "")]
		public string EnqueuedURIMetaData { get; set; }

		[MessageBodyMember(Namespace = "")]
		public int DesiredFirstTrackNumberEnqueued { get; set; }

		[MessageBodyMember(Namespace = "")]
		public bool EnqueueAsNext { get; set; }
	}

	[MessageContract(WrapperName = "AddURIToQueueResponse",
		WrapperNamespace = "urn:schemas-upnp-org:service:AVTransport:1",
		IsWrapped = true)]
	public class EnqueueResponse
	{
		[MessageBodyMember(Namespace = "")]
		public int FirstTrackNumberEnqueued { get; set; }

		[MessageBodyMember(Namespace = "")]
		public int NumTracksAdded { get; set; }

		[MessageBodyMember(Namespace = "")]
		public int NewQueueLength { get; set; }
	}

	[MessageContract(WrapperName = "GetPositionInfoResponse",
		WrapperNamespace = "urn:schemas-upnp-org:service:AVTransport:1",
		IsWrapped = true)]
	public class PositionInfoResponse
	{
		[MessageBodyMember(Namespace = "")]
		public string Track { get; set; }

		[MessageBodyMember(Namespace = "")]
		public string TrackDuration { get; set; }

		[MessageBodyMember(Namespace = "")]
		public string TrackMetaData { get; set; }

		[MessageBodyMember(Namespace = "")]
		public string TrackURI { get; set; }

		[MessageBodyMember(Namespace = "")]
		public string RelTime { get; set; }

		[MessageBodyMember(Namespace = "")]
		public string AbsTime { get; set; }

		[MessageBodyMember(Namespace = "")]
		public string RelCount { get; set; }

		[MessageBodyMember(Namespace = "")]
		public string AbsCount { get; set; }

		public override string ToString()
		{
			return string.Format("Track: {0}\nDuration: {1}\nTrackURI: {2}\nAbsCount: {3}\n", Track, TrackDuration, TrackURI,
			                     AbsCount);
		}
	}

	[MessageContract(WrapperName = "GetPositionInfo", WrapperNamespace = "urn:schemas-upnp-org:service:AVTransport:1",
		IsWrapped = true)]
	internal class PositionInfoRequest
	{
		[MessageBodyMember(Namespace = "")]
		public int InstanceID { get; set; }
	}

	[MessageContract(WrapperName = "PauseResponse", WrapperNamespace = "urn:schemas-upnp-org:service:AVTransport:1",
		IsWrapped = true)]
	internal class PauseResponse
	{
	}

	[MessageContract(WrapperName = "Pause", WrapperNamespace = "urn:schemas-upnp-org:service:AVTransport:1",
		IsWrapped = true)]
	internal class PauseRequest
	{
		[MessageBodyMember(Namespace = "")]
		public int InstanceID { get; set; }
	}
}
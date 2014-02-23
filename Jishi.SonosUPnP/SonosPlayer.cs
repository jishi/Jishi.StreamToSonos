using System;
using System.Collections.Generic;
using System.Net;
using System.ServiceModel.Description;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Jishi.SonosUPnP.MessageContracts;

namespace Jishi.SonosUPnP
{
	public class SonosPlayer
	{
		private Task discoveryTask;
		private Uri url;
		//private string sonosBaseUrl;
		private SonosProperties properties;
		private SonosStatus status;
		private AVTransportService avTransportService;

		public SonosPlayer Coordinator { get; set; }
		public string RoomName { get; set; }
		public string Uuid { get; set; }

		public SonosPlayer(string uuid, string roomName, string sonosBaseUrl)
		{
			Uuid = uuid;
			RoomName = roomName;
            avTransportService = new AVTransportService(sonosBaseUrl + "/MediaRenderer/AVTransport/Control");
		}

		public void SetAvTransportUri(string uri, string metaData = "")
		{
            avTransportService.SetAVTransportURI(0, uri, metaData);
		}

		public void Pause()
		{
            avTransportService.Pause(0);
		}

		public PositionInfoResponse GetPositionInfo()
		{
            return avTransportService.GetPositionInfo(0);
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
            avTransportService.AddURIToQueue(0, uri, metadata, 0, false);
		}

		public void Play()
		{
            avTransportService.Play(0);
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
}
using System.ServiceModel;

namespace Jishi.SonosUPnP.MessageContracts
{
    [MessageContract(WrapperName = "AddURIToQueue", WrapperNamespace = "urn:schemas-upnp-org:service:AVTransport:1",
        IsWrapped = true)]
    public class EnqueueRequest
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
}
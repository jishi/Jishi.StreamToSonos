using System.ServiceModel;

namespace Jishi.SonosUPnP.MessageContracts
{
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
}
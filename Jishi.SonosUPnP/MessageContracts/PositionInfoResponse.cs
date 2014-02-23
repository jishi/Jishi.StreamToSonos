using System.ServiceModel;

namespace Jishi.SonosUPnP.MessageContracts
{
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
}
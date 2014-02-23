using System.ServiceModel;

namespace Jishi.SonosUPnP.MessageContracts
{
    [MessageContract(WrapperName = "SetAVTransportURI", WrapperNamespace = "urn:schemas-upnp-org:service:AVTransport:1",
        IsWrapped = true)]
    public class SetAvTransportUriRequest
    {
        [MessageBodyMember(Namespace = "")]
        public int InstanceID { get; set; }

        [MessageBodyMember(Namespace = "")]
        public string CurrentURI { get; set; }

        [MessageBodyMember(Namespace = "")]
        public string CurrentURIMetaData { get; set; }
    }
}
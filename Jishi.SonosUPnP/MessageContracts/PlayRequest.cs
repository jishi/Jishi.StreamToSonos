using System.ServiceModel;

namespace Jishi.SonosUPnP.MessageContracts
{
    [MessageContract(WrapperName = "Play", WrapperNamespace = "urn:schemas-upnp-org:service:AVTransport:1",
        IsWrapped = true)]
    public class PlayRequest
    {
        [MessageBodyMember(Namespace = "")]
        public int InstanceID { get; set; }
        [MessageBodyMember(Namespace = "")]
        public int Speed { get; set; }
    }
}
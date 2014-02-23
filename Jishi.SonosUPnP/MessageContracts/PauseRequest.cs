using System.ServiceModel;

namespace Jishi.SonosUPnP.MessageContracts
{
    [MessageContract(WrapperName = "Pause", WrapperNamespace = "urn:schemas-upnp-org:service:AVTransport:1",
        IsWrapped = true)]
    public class PauseRequest
    {
        [MessageBodyMember(Namespace = "")]
        public int InstanceID { get; set; }
    }
}
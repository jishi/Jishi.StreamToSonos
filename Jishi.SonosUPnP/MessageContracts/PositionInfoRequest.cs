using System.ServiceModel;

namespace Jishi.SonosUPnP.MessageContracts
{
    [MessageContract(WrapperName = "GetPositionInfo", WrapperNamespace = "urn:schemas-upnp-org:service:AVTransport:1",
        IsWrapped = true)]
    public class PositionInfoRequest
    {
        [MessageBodyMember(Namespace = "")]
        public int InstanceID { get; set; }
    }
}
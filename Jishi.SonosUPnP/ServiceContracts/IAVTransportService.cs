using System.ServiceModel;
using System.Threading.Tasks;
using Jishi.SonosUPnP.MessageContracts;

namespace Jishi.SonosUPnP.ServiceContracts
{
    [ServiceContract(Namespace = "urn:schemas-upnp-org:service:AVTransport:1")]
    public interface IAVTransportService
    {
        [OperationContract(Action = "urn:schemas-upnp-org:service:AVTransport:1#SetAVTransportURI")]
        Task SetAVTransportURIAsync(SetAvTransportUriRequest req);

        [OperationContract(Action = "urn:schemas-upnp-org:service:AVTransport:1#GetPositionInfo")]
        PositionInfoResponse GetPositionInfo(PositionInfoRequest req);

        [OperationContract(Action = "urn:schemas-upnp-org:service:AVTransport:1#Pause")]
        Task PauseAsync(PauseRequest args);

        [OperationContract(Action = "urn:schemas-upnp-org:service:AVTransport:1#AddURIToQueue")]
        EnqueueResponse AddURIToQueue(EnqueueRequest args);

        [OperationContract(Action = "urn:schemas-upnp-org:service:AVTransport:1#Play")]
        Task PlayAsync(PlayRequest playRequest);
    }
}
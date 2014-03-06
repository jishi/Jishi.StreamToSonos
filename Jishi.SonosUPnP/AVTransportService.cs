using System.ServiceModel;
using System.Threading.Tasks;
using Jishi.SonosUPnP.MessageContracts;
using Jishi.SonosUPnP.ServiceContracts;

namespace Jishi.SonosUPnP
{
    public class AVTransportService : SimpleSoapClient<IAVTransportService>
    {
        public AVTransportService(string baseAddress) : base(baseAddress)
        {
        }

        public PositionInfoResponse GetPositionInfo(int InstanceID)
        {
            IAVTransportService service = Factory.CreateChannel();
            var response = service.GetPositionInfo(new PositionInfoRequest {InstanceID = 0});
            ((IClientChannel) service).Close();

            return response;
        }

        public Task SetAVTransportURI(int instanceID, string currentURI, string currentURIMetaData)
        {
            IAVTransportService service = Factory.CreateChannel();
            var task = service.SetAVTransportURIAsync(new SetAvTransportUriRequest
                                                     {
                                                         InstanceID = instanceID,
                                                         CurrentURI = currentURI,
                                                         CurrentURIMetaData = currentURIMetaData
                                                     });
            ((IClientChannel) service).Close();
            return task;
        }

        public async void Pause(int instanceID)
        {
            IAVTransportService service = Factory.CreateChannel();
            await service.PauseAsync(new PauseRequest {InstanceID = instanceID});
            ((IClientChannel) service).Close();
        }

        public EnqueueResponse AddURIToQueue(int instanceID, string enqueuedURI, string enqueuedURIMetaData,
                                             int desiredFirstTrackNumberEnqueued, bool enqueueAsNext)
        {
            IAVTransportService service = Factory.CreateChannel();
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

        public Task Play(int instanceID)
        {
            IAVTransportService service = Factory.CreateChannel();
            var task = service.PlayAsync(new PlayRequest
                                  {
                                      InstanceID = instanceID,
                                      Speed = 1
                                  });
            ((IClientChannel) service).Close();
            return task;
        }
    }
}
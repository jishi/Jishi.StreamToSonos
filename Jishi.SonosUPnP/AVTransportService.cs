using System.ServiceModel;
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

        public async void SetAVTransportURI(int instanceID, string currentURI, string currentURIMetaData)
        {
            IAVTransportService service = Factory.CreateChannel();
            await service.SetAVTransportURIAsync(new SetAvTransportUriRequest
                                                     {
                                                         InstanceID = instanceID,
                                                         CurrentURI = currentURI,
                                                         CurrentURIMetaData = currentURIMetaData
                                                     });
            ((IClientChannel) service).Close();
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

        public async void Play(int instanceID)
        {
            IAVTransportService service = Factory.CreateChannel();
            await service.PlayAsync(new PlayRequest
                                  {
                                      InstanceID = instanceID,
                                      Speed = 1
                                  });
            ((IClientChannel) service).Close();
        }
    }
}
using System.ServiceModel;
using System.ServiceModel.Channels;
using Jishi.SonosUPnP.MessageContracts;
using Jishi.SonosUPnP.ServiceContracts;

namespace Jishi.SonosUPnP
{
    public class SimpleSoapClient<T>
    {
        protected ChannelFactory<T> Factory { get; private set; }
        protected SimpleSoapClient(string baseAddress)
        {
            //ServiceHost host = new ServiceHost( typeof( SimpleSoapClient ), new Uri( baseAddress ) );
            //host.AddServiceEndpoint( typeof( IAVTransportService ), GetBinding(), "" );
            //host.Description.Behaviors.Add( new ServiceMetadataBehavior { HttpGetEnabled = true } );
            //host.Open();
            //Console.WriteLine( "Host opened" );

            Factory = new ChannelFactory<T>(GetBinding(), new EndpointAddress(baseAddress));
        }

        static Binding GetBinding()
        {
            return new BasicHttpBinding();
        }
    }
}
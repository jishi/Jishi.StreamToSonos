using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Jishi.SonosUPnP;
using Jishi.StreamToSonos.Services;

namespace Jishi.StreamToSonos
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		public static HttpServer server;
		public static SonosDiscovery discovery;

		protected override void OnStartup( StartupEventArgs e )
		{
			base.OnStartup( e );
			discovery = new SonosDiscovery();
			discovery.TopologyChanged += TopologyChanged;
			server = new HttpServer();
		}

		protected override void OnExit( ExitEventArgs e )
		{
			server.Dispose();
			discovery.Dispose();
			base.OnExit( e );
		}

		private async void TopologyChanged( object sender, TopologyChangedEventHandlerArgs args )
		{
			await Dispatcher.InvokeAsync( delegate { ((MainWindow) MainWindow).UpdateZoneList( discovery.Zones ); } );
		}
	}
}
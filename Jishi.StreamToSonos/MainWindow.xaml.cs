using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Jishi.SonosUPnP;
using Jishi.StreamToSonos.Services;

namespace Jishi.StreamToSonos
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private SonosDiscovery discovery;
        HttpServer server;

		public MainWindow()
		{
			discovery = new SonosDiscovery();
			discovery.TopologyChanged += TopologyChanged;
		    server = new HttpServer();
		    //server.BufferSize = 20000;
            InitializeComponent();
		}

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            server.Dispose();
            discovery.Dispose();
            base.OnClosing(e);
        }

		private async void TopologyChanged(object sender, TopologyChangedEventHandlerArgs args)
		{
			await Dispatcher.InvokeAsync(delegate
				{
					ZoneList.Items.Clear();

					foreach ( var zone in discovery.Zones )
					{
						var item = new ComboBoxItem
						{
							Content = zone.Name,
							DataContext = zone.Coordinator
						};
						ZoneList.Items.Add( item );
					}
				});
		}

		private void StreamAction_Click( object sender, RoutedEventArgs e )
		{
			var selectedItem = (ComboBoxItem)ZoneList.Items[ZoneList.SelectedIndex];
			var player = (SonosPlayer)selectedItem.DataContext;
			Console.WriteLine(player.RoomName);
            // Find local endpoint
		    var localIp = SonosNotify.Instance.LocalEndpoint.Address.ToString();
		    var streamUrl = string.Format("http://{0}:9283/stream.wav", localIp);
			player.SetAvTransportUri(streamUrl);
		    player.Play();

		}

        private void Buffer_TextChanged(object sender, TextChangedEventArgs e)
        {
            var box = (TextBox)sender;
            int bufferSize;
            Int32.TryParse(box.Text, out bufferSize);

            server.BufferSize = bufferSize;
        }
	}
}

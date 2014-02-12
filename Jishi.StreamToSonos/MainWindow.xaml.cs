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

namespace Jishi.StreamToSonos
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private SonosDiscovery discovery;

		public MainWindow()
		{
			InitializeComponent();
			discovery = new SonosDiscovery();
			discovery.TopologyChanged += TopologyChanged;
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
			player.SetAvTransportUri("http://192.168.10.101:3333/stream.wav");
		}
	}
}

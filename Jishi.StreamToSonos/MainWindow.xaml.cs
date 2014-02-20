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
		public MainWindow()
		{
			//server.BufferSize = 20000;
			InitializeComponent();
		    KeyDown += OnKeyDown;
		}

		public void UpdateZoneList( IList<SonosZone> zones )
		{
			ZoneList.Items.Clear();

			foreach ( var zone in zones )
			{
				var item = new ComboBoxItem
				{
					Content = zone.Name,
					DataContext = zone.Coordinator
				};
				ZoneList.Items.Add( item );
			}
		}

        void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F9)
            {
                throw new Exception("Exception triggered");
            }
        }

		private void StreamAction_Click( object sender, RoutedEventArgs e )
		{
		    if (ZoneList.SelectedIndex == -1) return;
            var selectedItem = (ComboBoxItem) ZoneList.Items[ZoneList.SelectedIndex];
			var player = (SonosPlayer) selectedItem.DataContext;
			Console.WriteLine( player.RoomName );
			// Find local endpoint
			var localIp = SonosNotify.Instance.LocalEndpoint.Address.ToString();
			var streamUrl = string.Format( "http://{0}:9283/stream.wav", localIp );
			player.SetAvTransportUri( streamUrl );
			player.Play();
		}

		private void Buffer_TextChanged( object sender, TextChangedEventArgs e )
		{
			var box = (TextBox) sender;
			int bufferSize;
			Int32.TryParse( box.Text, out bufferSize );

			App.server.BufferSize = bufferSize;
		}
	}
}
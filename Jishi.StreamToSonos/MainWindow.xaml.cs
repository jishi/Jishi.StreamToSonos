using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Jishi.SonosUPnP;
using Jishi.StreamToSonos.Services;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using TextBox = System.Windows.Controls.TextBox;

namespace Jishi.StreamToSonos
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		Dictionary<string, SonosPlayer> dslist = new Dictionary<string, SonosPlayer>();
		public List<Task> Tasks = new List<Task>();

		public MainWindow()
		{
			//server.BufferSize = 20000;
			InitializeComponent();
		    KeyDown += OnKeyDown;
		    NotifyIcon ni = new NotifyIcon();
		    ni.Icon = Properties.Resources.assets_sonos;
            ni.Visible = true;
            ni.DoubleClick +=
                delegate(object sender, EventArgs args)
                {
                    this.Show();
                    this.WindowState = WindowState.Normal;
                };
            var menu = new System.Windows.Forms.ContextMenu();

			var ExitItem = menu.MenuItems.Add("Exit");
			ExitItem.Click += delegate (object sender, EventArgs args) {
				this.Close();
			};


			ni.ContextMenu = menu;

			//  DispatcherTimer setup
			DispatcherTimer dispatcherTimer = new DispatcherTimer();
			dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
			dispatcherTimer.Interval = new TimeSpan(0, 0, 2);
			dispatcherTimer.Start();
		}

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
			List<Task> toDel = new List<Task>();
			foreach (Task task in Tasks)
			{
				task.Start();
				task.Wait();
				toDel.Add(task);
			}

			foreach(Task task in toDel)
            {
				Tasks.Remove(task);
            }
			ZoneList.ItemsSource = dslist;
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
                this.Hide();

            base.OnStateChanged(e);
        }

		public void UpdateZoneList(IList<SonosZone> zones)
		{
			
				foreach (var zone in zones)
				{
					Task task = new Task(() =>
					{
						try
						{
							dslist.Add(zone.Name, zone.Coordinator);
						}
						catch { }
					});
					Tasks.Add(task);
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
			StreamAction.IsEnabled = false;
		    if (ZoneList.SelectedIndex == -1) return;
            var selectedItem = (KeyValuePair<string,SonosPlayer>) ZoneList.Items[ZoneList.SelectedIndex];
			var player = selectedItem.Value;
			Console.WriteLine( player.RoomName );
			// Find local endpoint
			var localIp = SonosNotify.Instance.LocalEndpoint.Address.ToString();
			var streamUrl = string.Format( "http://{0}:9283/stream.wav", localIp );

            Task.Factory.StartNew(() => { StartPlayer(player, streamUrl); });
			
		}

	    private async void StartPlayer(SonosPlayer player, string streamUrl)
	    {
			try
			{

				await player.SetAvTransportUri(streamUrl);
				await player.Play();
			}
            catch
            {
				StreamAction.IsEnabled = true;
            }
	    }

        private void ZoneList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
			StreamAction.IsEnabled = true;
		}
    }
}
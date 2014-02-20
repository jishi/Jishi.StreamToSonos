using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Jishi.SonosUPnP;
using Jishi.StreamToSonos.Services;
using log4net;

namespace Jishi.StreamToSonos
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		public static HttpServer server;
		public static SonosDiscovery discovery;
	    private ILog log;

	    protected override void OnStartup( StartupEventArgs e )
		{
            log = Logger.GetLogger(GetType());
            AppDomain.CurrentDomain.UnhandledException += HandleException;
            DispatcherUnhandledException += HandleDispatcherException;
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

        private void HandleDispatcherException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            CreateCrashDump(e.Exception);
        }

        private void HandleException(object sender, UnhandledExceptionEventArgs e)
        {
            CreateCrashDump(e.ExceptionObject as Exception);
        }

        private void CreateCrashDump(Exception e)
        {
            log.Error(e);
            var time = DateTime.Now;
            var filename = string.Format("craschdump-{0}.txt", time.ToString("yyyyMMdd HHmmss"));
            var file = File.CreateText(filename);
            file.Write(e.Message);
            if (e.InnerException != null)
                file.Write(e.InnerException.Message);
            file.Write(e.StackTrace);
            file.Close();
        }

		private async void TopologyChanged( object sender, TopologyChangedEventHandlerArgs args )
		{
			await Dispatcher.InvokeAsync( delegate { ((MainWindow) MainWindow).UpdateZoneList( discovery.Zones ); } );
		}
	}
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Jishi.StreamToSonos.Services
{
	public class HttpServer : IDisposable
	{
		AudioStreamHandler audioStreamHandler = new AudioStreamHandler();
	    Stream currentStream = null;
	    readonly MemoryStream initialBuffer = new MemoryStream();
	    public int BufferSize { get; set; }


	    private bool isDisposed = false;
	    private bool isBuffering = true;
	    private HttpListener Listener { get; set; }

		public HttpServer()
		{
			Listener = new HttpListener();
			Listener.Prefixes.Add("http://+:9283/");
			Listener.Start();
			StartListening();
			audioStreamHandler.SampleAvailable += SampleAvailable;
		}

		public void StartListening()
		{
		    if (isDisposed) return;
            Listener.BeginGetContext(HandleConnection, null);
            // Clear out buffer if possible
		    initialBuffer.SetLength(0);
		    isBuffering = true;
		}

		private void HandleConnection(IAsyncResult ar)
		{
			var context = Listener.EndGetContext(ar);
            
			audioStreamHandler.StartRecording();
            
			Console.WriteLine("Client connected {0}, {1} {2},\n{3}", context.Request.RemoteEndPoint, context.Request.HttpMethod, context.Request.RawUrl,
			                  context.Request.Headers);
            
			context.Response.SendChunked = true;
            context.Response.ContentType = "audio/x-wave";
            Console.WriteLine("{0}, {1}", context.Response.StatusCode, context.Response.StatusDescription);
            Console.WriteLine("{0}, {1}", context.Response.Headers, context.Response.ContentType);
		    SendWaveHeader(context.Response.OutputStream);
		    currentStream = context.Response.OutputStream;
		    
		}

        private void SendWaveHeader(Stream outputStream)
        {
            using (
                var headerStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Jishi.StreamToSonos.Resources.wav_header.bin")
                )
            {
                if (headerStream != null)
                    headerStream.CopyTo(outputStream);
            }
        }

        private void SampleAvailable(byte[] buffer)
        {
            if (currentStream == null) return;
            try {
            lock (initialBuffer)
            {
                if (isBuffering)
                {
                    initialBuffer.Write(buffer, 0, buffer.Length);
                    if (initialBuffer.Length < BufferSize) return;

                    Console.WriteLine("Writing BUFFER {0} bytes to player", initialBuffer.Length);
                    currentStream.Write(initialBuffer.ToArray(), 0, (int) initialBuffer.Length);
                    initialBuffer.SetLength(0);
                    isBuffering = false;
                }
            }
           
                Console.WriteLine("Writing {0} bytes to player", buffer.Length);
                currentStream.Write(buffer, 0, buffer.Length);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.StackTrace, e);
                currentStream = null;
                audioStreamHandler.StopRecording();
                StartListening();
            }
        }

	    public void Dispose()
	    {
	        Listener.Close();
	        isDisposed = true;
	    }
	}
}
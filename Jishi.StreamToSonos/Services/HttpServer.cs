using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Jishi.StreamToSonos.Services
{
	public class HttpServer
	{
		AudioStreamHandler audioStreamHandler = new AudioStreamHandler();
	    Stream currentStream = null;
	    MemoryStream initialBuffer = new MemoryStream();
	    int bufferSize = 20000;
	    private bool isBuffering = true;
	    protected HttpListener Listener { get; set; }

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
			Listener.BeginGetContext(HandleConnection, null);
            // Clear out buffer if possible
		    initialBuffer.SetLength(0);
		    isBuffering = true;
		}

		private void HandleConnection(IAsyncResult ar)
		{
			var context = Listener.EndGetContext(ar);
			audioStreamHandler.StartRecording();
			Console.WriteLine("Client connected {0}, {1},\n{2}", context.Request.RemoteEndPoint, context.Request.RawUrl,
			                  context.Request.Headers);
			context.Response.SendChunked = true;
		    SendWaveHeader(context.Response.OutputStream);
		    currentStream = context.Response.OutputStream;
		    
		}

        private void SendWaveHeader(Stream outputStream)
        {
            var fileStream = File.OpenRead("c:/temp/wav_header.bin");
            fileStream.CopyTo(outputStream);
        }

        private void SampleAvailable(byte[] buffer)
        {
            if (currentStream == null) return;
            if (isBuffering && initialBuffer.Length < bufferSize)
            {
                initialBuffer.Write(buffer, 0, buffer.Length);
                return;
            }
            try
            {
                
                if (initialBuffer.Length > 0)
                {
                    Console.WriteLine("Writing BUFFER {0} bytes to player", initialBuffer.Length);
                    currentStream.Write(initialBuffer.ToArray(), 0, (int)initialBuffer.Length);
                    initialBuffer.SetLength(0);
                    isBuffering = false;

                }
                Console.WriteLine("Writing {0} bytes to player", buffer.Length);
                currentStream.Write(buffer, 0, buffer.Length);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.StackTrace, e);
                currentStream = null;
                StartListening();
            }
        }
	}
}
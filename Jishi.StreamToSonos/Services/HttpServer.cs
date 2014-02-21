using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Mono.Net;
using log4net;

namespace Jishi.StreamToSonos.Services
{
    public class HttpServer : IDisposable
    {
        AudioStreamHandler audioStreamHandler = new AudioStreamHandler();
        readonly MemoryStream initialBuffer = new MemoryStream();
        ConcurrentQueue<byte[]> flowBuffer = new ConcurrentQueue<byte[]>();
        readonly ManualResetEvent resetEvent = new ManualResetEvent(false);
        public int BufferSize { get; set; }
		byte[] header;


        private bool isDisposed = false;
        private bool isBuffering = true;
        private ILog log;
        private HttpListener Listener { get; set; }

        public HttpServer()
        {
            log = Logger.GetLogger(GetType());
            log.Debug("Starting Listener");
            Listener = new HttpListener();
            Listener.Prefixes.Add("http://+:9283/");
            Listener.Start();
            StartListening();
            audioStreamHandler.SampleAvailable += SampleAvailable;
        }

        public void StartListening()
        {
            if (isDisposed) return;
            log.Debug("Started listening for requests");
            Listener.BeginGetContext(HandleConnection, null);
            // Clear out buffer if possible
            isBuffering = true;
            resetEvent.Reset();
        }

        private void HandleConnection(IAsyncResult ar)
        {

            if (isDisposed) return;

            var context = Listener.EndGetContext(ar);
            log.DebugFormat("Received connection from {0}", context.Request.RemoteEndPoint);

            log.DebugFormat("Client connected {0}, {1} {2},\n{3} {4}", context.Request.RemoteEndPoint,
                              context.Request.HttpMethod, context.Request.RawUrl,
                              context.Request.Headers, DateTime.Now);

            context.Response.SendChunked = true;
            context.Response.ContentType = "audio/x-wave";
            log.DebugFormat("{0}, {1}", context.Response.StatusCode, context.Response.StatusDescription);
            log.DebugFormat("{0}, {1}", context.Response.Headers, context.Response.ContentType);
            var currentStream = context.Response.OutputStream;
            log.DebugFormat("StartRecording");
            audioStreamHandler.StartRecording();
           
            try
            {
                SendWaveHeader(currentStream);
                log.Debug("Entering send loop");
                while (!isDisposed)
                {
                    resetEvent.WaitOne(5000);
                    if (!Listener.IsListening) break;
                    byte[] chunk;
                    while (flowBuffer.TryDequeue(out chunk))
                    {
                        currentStream.Write(chunk, 0, chunk.Length);
                        currentStream.Flush();
                    }
                    resetEvent.Reset();

                }

                log.Debug("Disposed called, loop exited");
                currentStream.Dispose();
                Cleanup();
            }
            catch (IOException e)
            {
                log.Error("Error when writing response", e);
                log.DebugFormat("Stopped recording");
                audioStreamHandler.StopRecording();
                // Clear buffer as well.
                flowBuffer = new ConcurrentQueue<byte[]>();

            }
            finally
            {
				StartListening();
            }
        }

        private void SendWaveHeader(Stream outputStream)
        {
	        
	        if ( header == null )
	        {
		        using ( var headerStream = Assembly.GetExecutingAssembly()
			        .GetManifestResourceStream( "Jishi.StreamToSonos.Resources.wav_header.bin" ) )
		        {
			        header = new byte[44];
					if ( headerStream != null )
					{
						headerStream.Read( header, 0, 44 );
					}
		        }
	        }
			
			// Fix sample rate
	        if ( audioStreamHandler.SampleRate != 44100 )
	        {
		        var sampleRateInBytes = BitConverter.GetBytes((short)audioStreamHandler.SampleRate);
		        header[24] = sampleRateInBytes[0];
				header[25] = sampleRateInBytes[1];
	        }
			log.DebugFormat("Writing header {0}", BitConverter.ToString( header ) );
            outputStream.Write(header, 0, header.Length);
        }

	    private void SampleAvailable(byte[] buffer)
        {
            flowBuffer.Enqueue(buffer);
            if (isBuffering && flowBuffer.Sum(x => x.Length) < BufferSize) return;
            if (isBuffering) log.DebugFormat("Buffer was {0}", flowBuffer.Sum(x => x.Length));
            
            isBuffering = false;
            
            resetEvent.Set();
        }

        public void Dispose()
        {
            log.Info("Disposing HTTP Server");
            isDisposed = true;
        }

        private void Cleanup()
        {
            log.Info("Closing all sockets in HTTP Server");
            Listener.Close();
        }
    }
}
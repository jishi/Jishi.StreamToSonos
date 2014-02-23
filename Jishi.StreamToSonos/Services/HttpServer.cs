using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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
        private bool isConnected;
        private HttpListener Listener { get; set; }

        public HttpServer()
        {
            log = Logger.GetLogger(GetType());
            log.Debug("Starting Listener");
            Listener = new HttpListener();
            Listener.Prefixes.Add("http://*:9283/");
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
            isConnected = true;
            var context = Listener.EndGetContext(ar);
            log.DebugFormat("Received connection from {0}", context.Request.RemoteEndPoint);

            log.DebugFormat("Client connected {0}, {1} {2},\n{3} {4}", context.Request.RemoteEndPoint,
                              context.Request.HttpMethod, context.Request.RawUrl,
                              context.Request.Headers, DateTime.Now);

            context.Response.SendChunked = false;
            context.Response.ContentType = "audio/x-wave";
            log.DebugFormat("{0}, {1}", context.Response.StatusCode, context.Response.StatusDescription);
            log.DebugFormat("{0}, {1}", context.Response.Headers, context.Response.ContentType);
            var currentStream = context.Response.OutputStream;
            log.DebugFormat("StartRecording");
            audioStreamHandler.StartRecording();
            try
            {
                SendWaveHeader(currentStream);
                SendSilent(currentStream, BufferSize);
                log.Debug("Entering send loop");
                while (!isDisposed && isConnected)
                {
                    resetEvent.WaitOne(5000);
                    if (!audioStreamHandler.IsRecording) break;
                    byte[] chunk;
                    while (isConnected && flowBuffer.TryDequeue(out chunk))
                    {
                        WriteToStream(currentStream, chunk);
                    }
                    resetEvent.Reset();
                }

                log.Debug("Disconnected called, loop exited");
                if (isDisposed)
                {
                    currentStream.Dispose();
                    Cleanup();
                }

                log.DebugFormat("Stopped recording");
                audioStreamHandler.StopRecording();
                // Clear buffer as well.
                flowBuffer = new ConcurrentQueue<byte[]>();
            }
            catch (IOException e)
            {
                

            }
            finally
            {
				StartListening();
            }
        }

        private async void SendSilent(Stream stream, int bufferSize)
        {
            var buffer = new byte[bufferSize];
            Array.Clear(buffer, 0, bufferSize);
            try
            {
                stream.Write(buffer, 0, buffer.Length);
                stream.Flush();
            }
            catch (IOException ex)
            {
                log.Debug(ex);
            }
        }

        private async void WriteToStream(Stream currentStream, byte[] chunk)
        {
            try
            {
                currentStream.Write(chunk, 0, chunk.Length);
                currentStream.Flush();
            }
            catch (IOException ex)
            {
                log.Debug(ex.Message);
                isConnected = false;
            }
            catch (ObjectDisposedException ex)
            {
                isConnected = false;
            }
        }

        private async void SendWaveHeader(Stream outputStream)
        {
	        
	        if ( header == null )
	        {
		        using ( var headerStream = Assembly.GetExecutingAssembly()
			        .GetManifestResourceStream( "Jishi.StreamToSonos.Resources.wav_header.bin" ) )
		        {
			        header = new byte[44];
					if ( headerStream != null )
					{
						await headerStream.ReadAsync( header, 0, 44 );
					    await headerStream.FlushAsync();
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
            try
            {
                outputStream.Write(header, 0, header.Length);
                outputStream.Flush();
            }
            catch (IOException ex)
            {
                log.Debug(ex);
                isConnected = false;
            }
        }

	    private void SampleAvailable(byte[] buffer)
        {
            if (buffer.Length == 0)
            {
                log.DebugFormat("sample was zero length!");
            }
            flowBuffer.Enqueue(buffer);
            resetEvent.Set();
        }

        public void Dispose()
        {
            log.Info("Disposing HTTP Server");
            isDisposed = true;
            audioStreamHandler.Dispose();
        }

        private void Cleanup()
        {
            log.Info("Closing all sockets in HTTP Server");
            Listener.Close();
        }
    }
}
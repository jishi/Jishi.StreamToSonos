using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Jishi.StreamToSonos.Services
{
    public class HttpServer : IDisposable
    {
        AudioStreamHandler audioStreamHandler = new AudioStreamHandler();
        Stream currentStream = null;
        readonly MemoryStream initialBuffer = new MemoryStream();
        ConcurrentQueue<byte[]> flowBuffer = new ConcurrentQueue<byte[]>();
        readonly ManualResetEvent resetEvent = new ManualResetEvent(false);
        public int BufferSize { get; set; }
		byte[] header;


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
            isBuffering = true;
            resetEvent.Reset();
        }

        private void HandleConnection(IAsyncResult ar)
        {

            if (isDisposed) return;
            var context = Listener.EndGetContext(ar);
            
            Console.WriteLine("Client connected {0}, {1} {2},\n{3} {4}", context.Request.RemoteEndPoint,
                              context.Request.HttpMethod, context.Request.RawUrl,
                              context.Request.Headers, DateTime.Now);

            context.Response.SendChunked = true;
            context.Response.ContentType = "audio/x-wave";
            Console.WriteLine("{0}, {1}", context.Response.StatusCode, context.Response.StatusDescription);
            Console.WriteLine("{0}, {1}", context.Response.Headers, context.Response.ContentType);
            currentStream = context.Response.OutputStream;
            Console.WriteLine("StartRecording");
            audioStreamHandler.StartRecording();
           
            try
            {
                SendWaveHeader(currentStream);
                while (!isDisposed)
                {
                    resetEvent.WaitOne();
                    byte[] chunk;
                    while (flowBuffer.TryDequeue(out chunk))
                    {
                        //Console.WriteLine("Writing {0} bytes to player", chunk.Length);
                        currentStream.BeginWrite(chunk, 0, chunk.Length, result =>
                            {
                                currentStream.Flush();
                            }, currentStream);
                        
                        //Console.WriteLine("Flushed");
                    }
                    resetEvent.Reset();
                    
                }
            }
            catch (HttpListenerException e)
            {
                Console.Error.WriteLine(e.StackTrace, e);
                Console.WriteLine("Stopped recording");
                audioStreamHandler.StopRecording();
                currentStream.Close();
                // Clear buffer as well.
                flowBuffer = new ConcurrentQueue<byte[]>();
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
			Console.WriteLine( BitConverter.ToString( header ) );
	        currentStream.Write( header, 0, header.Length );
        }

	    private void SampleAvailable(byte[] buffer)
        {
            flowBuffer.Enqueue(buffer);
            if (isBuffering && flowBuffer.Sum(x => x.Length) < BufferSize) return;
            if (isBuffering) Console.WriteLine("Buffer was {0}", flowBuffer.Sum(x => x.Length));
            
            isBuffering = false;
            
            resetEvent.Set();
        }

        public void Dispose()
        {
            Listener.Close();
            isDisposed = true;
        }
    }
}
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Mono.Net;
using log4net;
using System.Net;

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
        private TcpListener Listener { get; set; }

        public HttpServer()
        {
            log = Logger.GetLogger(GetType());
            log.Debug("Starting Listener");

            Listener = new TcpListener(IPAddress.Any,9283);

            //Listener = new HttpListener();
            //Listener.Prefixes.Add("http://*:9283/");
            Listener.Start();
            StartListening();
            audioStreamHandler.SampleAvailable += SampleAvailable;
        }

        public void StartListening()
        {
            if (isDisposed) return;
            log.Debug("Started listening for requests");
            Listener.BeginAcceptTcpClient(HandleConnection, null);
            // Clear out buffer if possible
            isBuffering = true;
            resetEvent.Reset();
        }

        private void HandleConnection(IAsyncResult ar)
        {
            if (isDisposed) return;
            isConnected = true;
            var client = Listener.EndAcceptTcpClient(ar);

            var headers = ParseHeaders(client);

            log.DebugFormat("Client connected {0}, {1} {2}", client.Client.RemoteEndPoint,
                            headers, DateTime.Now);

            var stream = client.GetStream();

            var responseHeaders = new NameValueCollection
                                      {
                                          {"Transfer-Encoding", "Chunked"},
                                          {"Content-Type", "audio/x-wave"}
                                      };

            SendHeaders(responseHeaders, stream);

            log.DebugFormat("StartRecording");
            audioStreamHandler.StartRecording();
            try
            {
                SendWaveHeader(stream);
                SendSilent(stream, BufferSize);
                log.Debug("Entering send loop");
                while (!isDisposed && isConnected)
                {
                    resetEvent.WaitOne(5000);
                    if (!audioStreamHandler.IsRecording) break;
                    byte[] chunk;
                    while (isConnected && flowBuffer.TryDequeue(out chunk))
                    {
                        WriteChunkedToStream(stream, chunk);
                    }
                    resetEvent.Reset();
                }

                log.Debug("Disconnected called, loop exited");
                if (isDisposed)
                {
                    stream.Dispose();
                    Cleanup();
                }

                log.DebugFormat("Stopped recording");
                audioStreamHandler.StopRecording();
                // Clear buffer as well.
                flowBuffer = new ConcurrentQueue<byte[]>();
            }
            catch (IOException e)
            {
                log.Error("socket disconnected?", e);
            }
            finally
            {
                StartListening();
            }
        }

        private void SendHeaders(NameValueCollection responseHeaders, NetworkStream stream)
        {
            var httpResponse = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\n");
            stream.Write(httpResponse, 0, httpResponse.Length);
            try
            {
                foreach (string header in responseHeaders)
                {
                    var headerBytes =
                        Encoding.ASCII.GetBytes(string.Format("{0}: {1}\r\n", header, responseHeaders[header]));
                    stream.Write(headerBytes, 0, headerBytes.Length);
                }
                var end = Encoding.ASCII.GetBytes("\r\n");
                stream.Write(end, 0, end.Length);
            }
            catch (IOException ex)
            {
                log.Debug("Error when writing response headers", ex);
                isConnected = false;
            }
        }

        private NameValueCollection ParseHeaders(TcpClient client)
        {
            var stream = client.GetStream();
            var reader = new StreamReader(stream, Encoding.ASCII);
            string line;
            var headers = new NameValueCollection();
            do
            {
                line = reader.ReadLine();
                if (string.IsNullOrEmpty(line)) break;
                var chunks = line.Split(':');
                if (chunks.Length < 2) continue;

                headers.Add(chunks[0], chunks[1]);
            } while (true);

            return headers;
        }

        private void SendSilent(Stream stream, int bufferSize)
        {
            if (bufferSize < 1) return;
            var buffer = new byte[bufferSize*4];
            Array.Clear(buffer, 0, bufferSize);
            try
            {
                WriteChunkedToStream(stream, buffer);
            }
            catch (IOException ex)
            {
                log.Debug(ex);
            }
        }

        //private async void WriteToStream(Stream currentStream, byte[] chunk)
        //{
        //    if (chunk.Length == 0)
        //    {
        //        log.Error("Received empty chunk!");
        //        return;
        //    }
        //    try
        //    {
        //        currentStream.Write(chunk, 0, chunk.Length);
        //        currentStream.Flush();
        //    }
        //    catch (IOException ex)
        //    {
        //        log.Debug(ex.Message);
        //        isConnected = false;
        //    }
        //    catch (ObjectDisposedException ex)
        //    {
        //        isConnected = false;
        //    }
        //}

        private async void SendWaveHeader(Stream outputStream)
        {
            if (header == null)
            {
                using (var headerStream = Assembly.GetExecutingAssembly()
                                                  .GetManifestResourceStream(
                                                      "Jishi.StreamToSonos.Resources.wav_header.bin"))
                {
                    header = new byte[44];
                    if (headerStream != null)
                    {
                        await headerStream.ReadAsync(header, 0, 44);
                        await headerStream.FlushAsync();
                    }
                }
            }

            // Fix sample rate
            if (audioStreamHandler.SampleRate != 44100)
            {
                var sampleRateInBytes = BitConverter.GetBytes((short) audioStreamHandler.SampleRate);
                header[24] = sampleRateInBytes[0];
                header[25] = sampleRateInBytes[1];
            }
            log.DebugFormat("Writing header {0}", BitConverter.ToString(header));
            // first ouput length headers
            WriteChunkedToStream(outputStream, header);
        }

        private void WriteChunkedToStream(Stream outputStream, byte[] chunk)
        {
            var lineEnd = new byte[] {0x0d, 0x0a};
            var length = Encoding.ASCII.GetBytes(chunk.Length.ToString("X") + "\r\n");
            try
            {
                outputStream.Write(length, 0, length.Length);
                outputStream.Write(chunk, 0, chunk.Length);
                outputStream.Write(lineEnd, 0, lineEnd.Length);
                outputStream.Flush();
            }
            catch
            {
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
            Listener.Stop();
        }
    }
}
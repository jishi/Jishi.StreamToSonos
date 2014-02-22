using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Utils;
using NAudio.Wave;
using log4net;

namespace Jishi.StreamToSonos.Services
{
	public class AudioStreamHandler
	{
	    private bool isStopping;
	    private bool resumeRecording;
	    private ILog log;
	    WasapiLoopbackCapture WaveIn { get; set; }

		public int SampleRate
		{
			get
			{
				return WaveIn.WaveFormat.SampleRate;
			}
		}

		public AudioStreamHandler()
		{
		    log = Logger.GetLogger(GetType());
            WaveIn = new WasapiLoopbackCapture();
			WaveIn.DataAvailable += DataAvailable;
		    WaveIn.RecordingStopped += RecordingStopped;
		}

	    public event SampleAvailableEventHandler SampleAvailable;

        public bool IsRecording { get; private set; }

		public void StartRecording()
		{
		    if (isStopping)
		    {
		        resumeRecording = true;
		        return;
		    }
            WaveIn.StartRecording();
		    IsRecording = true;

		}

		public void StopRecording()
		{
			WaveIn.StopRecording();
		    isStopping = true;
		}

        private void RecordingStopped(object sender, StoppedEventArgs e)
        {
            isStopping = false;
            IsRecording = false;
            if (resumeRecording)
            {
                WaveIn.StartRecording();
                resumeRecording = false;
                IsRecording = true;
            }
        }

		private void DataAvailable(object sender, WaveInEventArgs e)
		{
			if (e.BytesRecorded == 0)
			{
			    log.DebugFormat("Sample was zero length, but buffer was {0}", e.Buffer.Length);
			}

            log.DebugFormat("buffer size {0} recorded size {1}", e.Buffer.Length, e.BytesRecorded);
            var bytesRecorded = e.BytesRecorded == 0 ? e.Buffer.Length / 2 + 1560: e.BytesRecorded;
			// Convert to 16 bit
			if ( bytesRecorded % 8 > 0 )
			{
				Console.WriteLine("float sample not multiple of 8");
			}


			byte[] destBuffer = new byte[bytesRecorded / 2];
			var readBuffer = e.Buffer;
			WaveBuffer sourceWaveBuffer = new WaveBuffer(readBuffer);
			WaveBuffer destWaveBuffer = new WaveBuffer(destBuffer);

			int sourceSamples = bytesRecorded / 4;
			int destOffset = 0;
			for (int sample = 0; sample < sourceSamples; sample++)
			{
				// adjust volume
				float sample32 = sourceWaveBuffer.FloatBuffer[sample]*1.0f;
				// clip
				if ( sample32 > 1.0f )
				{
					sample32 = 1.0f;
				}
				if ( sample32 < -1.0f )
				{
					sample32 = -1.0f;
				}
				destWaveBuffer.ShortBuffer[destOffset++] = (short) (sample32*32767);
			}

			if (SampleAvailable != null)
				SampleAvailable.BeginInvoke(destWaveBuffer.ByteBuffer, ar =>
					{
						
					}, null);
		}
	}

	public delegate void SampleAvailableEventHandler(byte[] buffer);
}
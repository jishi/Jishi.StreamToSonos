using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Utils;
using NAudio.Wave;

namespace Jishi.StreamToSonos.Services
{
	public class AudioStreamHandler
	{
		WasapiLoopbackCapture WaveIn { get; set; }

		public AudioStreamHandler()
		{
			WaveIn = new WasapiLoopbackCapture();
			WaveIn.DataAvailable += DataAvailable;
			//WaveIn.RecordingStopped += RecordingStopped;
		}

		public event SampleAvailableEventHandler SampleAvailable;

		public void StartRecording()
		{
			try
			{
				WaveIn.StartRecording();
			} catch (Exception e)
			{
			}
		}

		public void StopRecording()
		{
			WaveIn.StopRecording();
		}

		private void DataAvailable(object sender, WaveInEventArgs e)
		{
			// Convert to 16 bit
			if (e.BytesRecorded%8 > 0)
			{
				Console.WriteLine("float sample not multiple of 8");
			}
			byte[] destBuffer = new byte[e.BytesRecorded/2];
			var readBuffer = e.Buffer;
			WaveBuffer sourceWaveBuffer = new WaveBuffer(readBuffer);
			WaveBuffer destWaveBuffer = new WaveBuffer(destBuffer);

			int sourceSamples = e.BytesRecorded/4;
			int destOffset = 0;
			for (int sample = 0; sample < sourceSamples; sample++)
			{
				// adjust volume
				float sample32 = sourceWaveBuffer.FloatBuffer[sample]*1.0f;
				// clip
				if (sample32 > 1.0f)
					sample32 = 1.0f;
				if (sample32 < -1.0f)
					sample32 = -1.0f;
				destWaveBuffer.ShortBuffer[destOffset++] = (short) (sample32*32767);
			}

			if (SampleAvailable != null)
				SampleAvailable.BeginInvoke(destWaveBuffer.ByteBuffer, ar =>
					{
						
					}, null);
		}
	}

	public delegate void SampleAvailableEventHandler(byte[] buffer);

	public class SampleAvailableEventHandlerArgs
	{
		public byte[] Data { get; set; }
	}
}
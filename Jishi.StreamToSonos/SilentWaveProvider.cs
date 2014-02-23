using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;

namespace Jishi.StreamToSonos
{
    public class SilentWaveProvider : IWaveProvider
    {
        public WaveFormat WaveFormat
        {
            get
            {
                return new WaveFormat(44100, 2);
            }
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            //Array.Clear(buffer, 0, buffer.Length);
            return buffer.Length;
        }
    }
}
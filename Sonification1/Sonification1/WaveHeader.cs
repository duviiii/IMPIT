using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sonification1
{
    public class WaveHeader
    {
        public string sGroupID;
        public uint dwFileLength;
        public string sRiffType;

        public WaveHeader() {
            dwFileLength = 0;
            sGroupID = "RIFF";
            sRiffType = "WAVE";
        }
    }

    public class WaveFormatChunk 
    {
        public string sChunkID;
        public uint dwChunkSize;
        public ushort wFormatTag;
        public ushort wChannels;
        public uint dwSamplesPerSec;
        public uint dwAvgBytesPerSec;
        public ushort wBlockAlign;
        public ushort wBitsPerSample;

        // Initial value: 
        // Sample Rate : 44000 Hz
        // Channels : Stereo
        // Bite depth : 16-bit
        public WaveFormatChunk() 
        {
            sChunkID = "fmt ";
            dwChunkSize = 16;
            wFormatTag = 1;
            wChannels = 2;
            dwSamplesPerSec = 44000;
            wBitsPerSample = 16;
            wBlockAlign = (ushort)(wChannels * (wBitsPerSample / 8));
            dwAvgBytesPerSec = dwSamplesPerSec * wBlockAlign;
        }
    }

    public class WaveDataChunk 
    {
        public string sChunkID;
        public uint dwChunkSize;
        public short[] shortArray;

        public WaveDataChunk()
        {
            shortArray = new short[0];
            dwChunkSize = 0;
            sChunkID = "data";
        }
    }
}

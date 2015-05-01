using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Sonification1
{
    public enum WaveExampleType
    { 
        ExampleSineWave = 0
    }

    public class WaveGenerator
    {
        WaveHeader header;
        WaveFormatChunk format;
        WaveDataChunk data;

        public WaveGenerator(WaveExampleType type)
        {
            header = new WaveHeader();
            format = new WaveFormatChunk();
            data = new WaveDataChunk();

            switch(type)
            {
                case WaveExampleType.ExampleSineWave:
                    uint numSamples = format.dwSamplesPerSec * format.wChannels;

                    data.shortArray = new short[numSamples];

                    int amplitute = 32760; // Maximum amplitues for 16-bit audio
                    double freq = 440.0f;  // Consert A: 440Hz

                    double t = (Math.PI * 2 * freq) / (format.dwSamplesPerSec * format.wChannels);

                    for (uint i=0; i<numSamples - 1; i++){
                        for (int channel = 0; channel <format.wChannels; channel++)
                        {
                            data.shortArray[i+channel] = Convert.ToInt16(amplitute * Math.Sin(t * i));
                        }
                    }
                    break;
            }
        }

        public void Save(string filePath)
        {
            FileStream fileStream = new FileStream(filePath, FileMode.Create);

            BinaryWriter writer = new BinaryWriter(fileStream);

            /*writer.Write(Encoding.ASCII.GetBytes(header.sGroupID.ToCharArray()));
            writer.Write(header.dwFileLength);
            writer.Write(Encoding.ASCII.GetBytes(header.sRiffType.ToCharArray()));

            writer.Write(Encoding.ASCII.GetBytes(format.sChunkID.ToCharArray()));
            writer.Write(format.dwChunkSize);
            writer.Write(format.wFormatTag);
            writer.Write(format.wChannels);
            writer.Write(format.dwSamplesPerSec);
            writer.Write(format.dwAvgBytesPerSec);
            writer.Write(format.wBlockAlign);
            writer.Write(format.wBitsPerSample);

            writer.Write(Encoding.ASCII.GetBytes(data.sChunkID.ToCharArray()));
            writer.Write(data.dwChunkSize);
             */
            byte[] RIFF_HEADER = new byte[] { 0x52, 0x49, 0x46, 0x46 };
            byte[] FORMAT_WAVE = new byte[] { 0x57, 0x41, 0x56, 0x45 };
            byte[] FORMAT_TAG  = new byte[] { 0x66, 0x6d, 0x74, 0x20 };
            byte[] AUDIO_FORMAT = new byte[] {0x01, 0x00};
            byte[] SUBCHUNK_ID  = new byte[] { 0x64, 0x61, 0x74, 0x61 };
            int BYTES_PER_SAMPLE = 2;
            uint numsamples = 44100;
            ushort numchannels = 1;
            ushort samplelength = 1; // in bytes
            uint samplerate = 22050;
            writer.Write(RIFF_HEADER);
            writer.Write(0);
            writer.Write(FORMAT_WAVE);
            writer.Write(FORMAT_TAG);
            writer.Write(18 + (int)(numsamples * samplelength));
            writer.Write((short)1); // Encoding
            writer.Write((short)numchannels); // Channels
            writer.Write((int)(samplerate)); // Sample rate
            writer.Write((int)(samplerate * samplelength * numchannels)); // Average bytes per second
            writer.Write((short)(samplelength * numchannels)); // block align
            writer.Write((short)(8 * samplelength)); // bits per sample
            //writer.Write((short)(numsamples * samplelength)); // Extra size
            writer.Write(SUBCHUNK_ID);
            foreach (short dataPoint in data.shortArray)
            {
                writer.Write(dataPoint);
            }

            //writer.Seek(4, SeekOrigin.Begin);
            //uint filesize = (uint)writer.BaseStream.Length;
            //writer.Write(filesize - 8);

            writer.Close();
            fileStream.Close();
        }
    }
}

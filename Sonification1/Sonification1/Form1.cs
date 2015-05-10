using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Media;
using System.IO;

namespace Sonification1
{
    public partial class Form1 : Form
    {
        static byte[] RIFF_HEADER = new byte[] { 0x52, 0x49, 0x46, 0x46 };
        static byte[] FORMAT_WAVE = new byte[] { 0x57, 0x41, 0x56, 0x45 };
        static byte[] FORMAT_TAG = new byte[] { 0x66, 0x6d, 0x74, 0x20 };
        static byte[] AUDIO_FORMAT = new byte[] { 0x01, 0x00 };
        static byte[] SUBCHUNK_ID = new byte[] { 0x64, 0x61, 0x74, 0x61 };
        private const int BYTES_PER_SAMPLE = 2;
        private bool isCropping = false;
        private Point startPoint;
        private Point endPoint;
        private Rectangle theRectangle = new Rectangle(new Point(0, 0), new Size(0, 0));

        public Form1()
        {
            InitializeComponent();
        }

        
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                //ControlPaint.DrawReversibleFrame(theRectangle, Color.OrangeRed, FrameStyle.Thick);
                theRectangle = new Rectangle(0, 0, 0, 0);
                isCropping = true;
            }
            startPoint.X = e.X+10;
            startPoint.Y = e.Y+30;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            // Initialize
            base.OnMouseUp(e);
            isCropping = false;
            string filepath_1 = @"E:\sample1.wav";
            string filepath_2 = @"E:\sample2.wav";
            string filepath_3 = @"E:\sample3.wav";

            int sampleRate = 44000;
            // int low_frequency = 4; // lower frequency of later use
            int high_frequency = 300;
            int divide_size = 4;
            double sound_length = 2; // on second

            // Get the background image and crop out the selected area
            Image img = this.BackgroundImage;
            Rectangle cropRect = new Rectangle(startPoint.X-10, startPoint.Y-30, endPoint.X - startPoint.X, endPoint.Y - startPoint.Y);
            Bitmap clone = new Bitmap(img);
            Bitmap cropped = clone.Clone(cropRect, clone.PixelFormat);
            
            // Option 1

            var tmp = CreateSinWave(cropped, sampleRate, high_frequency, TimeSpan.FromSeconds(sound_length), 1d);
            short[] soundDataShort = tmp;
            byte[] soundDataByte = convertShorttoByte(soundDataShort);

            // Save sound data into output .wav file
            using (FileStream fs = new FileStream(filepath_1, FileMode.Create))
            {
                WriteHeader(fs, soundDataByte.Length, 1, sampleRate);
                fs.Write(soundDataByte, 0, soundDataByte.Length);
                fs.Close();
            }

            // Option 2
            // Split selected area into 4x4 field    
            Bitmap[][] cropped_matrix = select_subimage(cropped, divide_size);

            List<short> divided_sound_data = new List<short>();
            TimeSpan divided_sound_length = TimeSpan.FromMilliseconds(sound_length*1000 / (divide_size * divide_size));

            // create a single sin wave
            var initial_sound = CreateSimpleSinWave(sampleRate, high_frequency, TimeSpan.FromSeconds(sound_length), 1d);

            // get magnitude value for each subimage
            short[][] mag_value = getMagnitude(cropped_matrix, divide_size);

            // modify the amplitude (0~100) value of each subimage

            int step = initial_sound.Length / (divide_size * divide_size);

            // Row by row
            for (int j = 0; j < divide_size; j++) {
                for (int i = 0; i < divide_size; i++) {
                    // Generate Sin wave for each area
                    //Bitmap current = cropped_matrix[i][j];

                    //var tmp = CreateSinWave(current, sampleRate, high_frequency, divided_sound_length, 1d);
                    // Add sound information into data list
                    //divided_sound_data.AddRange(tmp);
                    int start = step*(j*divide_size + i);
                    int end = start+step;
                    adjustMagnitude(initial_sound, start, end, mag_value[i][j]);
                }
            }

            // convert data list into data array
            short[] soundDataShort1 = initial_sound;
            byte[] soundDataByte1 = convertShorttoByte(soundDataShort1);

            // Save sound data into output .wav file
            using (FileStream fs = new FileStream(filepath_2, FileMode.Create))
            {
                WriteHeader(fs, soundDataByte1.Length, 1, sampleRate);
                fs.Write(soundDataByte1, 0, soundDataByte1.Length);
                fs.Close();
            }

            // Option 3
            // Genereate 16 sin waves with increased frequency
            short[][] initial_waves = new short[divide_size * divide_size][];
            double initial_frequency = 50;
            for (int i = 0; i < divide_size * divide_size; i++) {
                initial_waves[i] = CreateSimpleSinWave(sampleRate, initial_frequency*(i+1), TimeSpan.FromSeconds(sound_length), 1d);
            }

            //Adjust amplitude of each sin waves
            for (int j = 0; j < divide_size; j++)
            {
                for (int i = 0; i < divide_size; i++)
                {
                    int index = j * divide_size + i;
                    //short current_mag = (short)(mag_value[i][j] / (index+1));
                    short current_mag = (short)(mag_value[i][j]);
                    adjustMagnitude(initial_waves[index], 0, initial_waves[index].Length, mag_value[i][j]);
                }
            }

            //Combine 16 sin waves into one final sinwave
            short[] final_wave = new short[initial_waves[0].Length];
            for (int i = 0; i< final_wave.Length; i++)  {
                final_wave[i] = 0;
                for (int j = 0; j < divide_size * divide_size; j++) {
                    final_wave[i] = (short)((final_wave[i] + initial_waves[j][i])/2);
                }
            }



            // convert data list into data array
            short[] soundDataShort2 = final_wave;
            byte[] soundDataByte2 = convertShorttoByte(soundDataShort2);

            // Save sound data into output .wav file
            using (FileStream fs = new FileStream(filepath_3, FileMode.Create))
            {
                WriteHeader(fs, soundDataByte2.Length, 1, sampleRate);
                fs.Write(soundDataByte2, 0, soundDataByte2.Length);   
                fs.Close();
            }

            // replay the file
            SoundPlayer player = new SoundPlayer(filepath_3);
            player.Play();

            // Dispay selected area in a new window
            PictureBox pb = new PictureBox();
            pb.Dock = DockStyle.Fill;
            pb.Image = cropped;
            pb.Width = cropped.Width;
            pb.Height = cropped.Height;

            Form2 popup = new Form2();
            popup.Width = pb.Width;
            popup.Height = pb.Height;
            popup.Controls.Add(pb);
            popup.ShowDialog();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (isCropping)
            {
                ControlPaint.DrawReversibleFrame(theRectangle, Color.OrangeRed, FrameStyle.Thick);
                endPoint.X = e.X+10;
                endPoint.Y = e.Y+30;
                int width = endPoint.X - startPoint.X;
                int height = endPoint.Y - startPoint.Y;
                Point winLoc = this.Location;
                theRectangle = new Rectangle(startPoint.X+winLoc.X, startPoint.Y+winLoc.Y, width, height);
                ControlPaint.DrawReversibleFrame(theRectangle, Color.OrangeRed, FrameStyle.Thick);
            }
        }

        private byte[] BitmaptoByteArray(Bitmap img){
            ImageConverter converter = new ImageConverter();
            return (byte[])converter.ConvertTo(img, typeof(byte[]));
        }

        public static int[] ByteArrayToIntArray(byte[] source,int begin) {
            int[] rval = new int[source.Length/4];
            for (int i = begin; i < source.Length / 4; i++) {
                rval[i] = BitConverter.ToInt32(source,i*4);    
            }
            return rval;
        }

        public static double arrayAverage(int[] a) {
            double sum = 0;
            for (int i = 0; i < a.Length; i++) {
                sum += a[i];
            }
            return (double)sum/ a.Length;
        }

        public static short[] CreateSinWave(
            Bitmap img,
            int sampleRate,
            double frequency,
            TimeSpan length,
            double magnitude
        )
        {
            MemoryStream ms = new MemoryStream();
            img.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            byte[] img_byte = ms.ToArray();
            int[] img_int = ByteArrayToIntArray(img_byte,54);
            double avg = arrayAverage(img_int);

            int sampleCount = (int)(((double)sampleRate) * length.TotalSeconds);
            short[] retVal = new short[sampleCount];
            double new_frequency = frequency*(1 + avg/int.MaxValue);
            double step = Math.PI * 2.0d * new_frequency/sampleRate;
            double current = 0;

            for (int i = 0; i < retVal.Length; ++i)
            {
                short tmp = (short)(Math.Sin(current) * magnitude * (short.MaxValue));
                retVal[i] = tmp;
                current += step;
            }

            return retVal;
        }

        public static short[] CreateSimpleSinWave(
            int sampleRate,
            double frequency,
            TimeSpan length,
            double magnitude) 
        {
            int sampleCount = (int)(((double)sampleRate) * length.TotalSeconds);
            short[] retVal = new short[sampleCount];
            double step = Math.PI * 2.0d * frequency / sampleRate;
            double current = 0;

            for (int i = 0; i < retVal.Length; ++i)
            {
                short tmp = (short)(Math.Sin(current) * magnitude * (short.MaxValue));
                retVal[i] = tmp;
                current += step;
            }

            return retVal;
        }

        public static void adjustMagnitude(short[] source, int start_index, int end_index, short magnitude) {
            for (int i = start_index; i < end_index; i++)
            {
                source[i] = (short)(source[i]*magnitude/100);
            }
        }

        public static short[][] getMagnitude(Bitmap[][] source, int divide_size) {
            short[][] retVal = new short[divide_size][];
            double[][] avgs = new double[divide_size][];

            double max_value = 4*Math.Pow(10,7);
            //short avg_magnitude = 50;

            for (int i = 0; i < divide_size; i++) {
                retVal[i] = new short[divide_size];
                //avgs[i] = new double[divide_size];
            }

            for (int i = 0; i < divide_size; i++)
            {
                for (int j = 0; j < divide_size; j++)
                {
                    MemoryStream ms = new MemoryStream();
                    Bitmap img = source[i][j];
                    img.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                    byte[] img_byte = ms.ToArray();
                    int[] img_int = ByteArrayToIntArray(img_byte, 54);
                    double avg = arrayAverage(img_int);

                    //if (avg > max_value) {
                    //    max_value = avg;
                    //}

                    //avgs[i][j] = avg;
                    short adjust = (short)(avg / max_value);
                    retVal[i][j] = (short)(50 + avg / max_value);
                }
            }

            //for (int i = 0; i < divide_size; i++)
            //{
            //    for (int j = 0; j < divide_size; j++)
            //    {
            //        retVal[i][j] = (short)(avg_magnitude * (1 + avgs[i][j] / max_value));
            //    }
            //}

            return retVal;
        }

        static byte[] PackageInt(int source, int length = 2)
        {
            var retVal = new byte[length];
            retVal[0] = (byte)(source & 0xFF);
            retVal[1] = (byte)((source >> 8) & 0xFF);
            if (length == 4)
            {
                retVal[2] = (byte)((source >> 0x10) & 0xFF);
                retVal[3] = (byte)((source >> 0x18) & 0xFF);
            }
            return retVal;
        }

        public static void WriteHeader(
            System.IO.Stream targetStream,
            int byteStreamSize,
            int channelCount,
            int sampleRate)
        {

            int byteRate = sampleRate * channelCount * BYTES_PER_SAMPLE;
            int blockAlign = channelCount * BYTES_PER_SAMPLE;

            targetStream.Write(RIFF_HEADER, 0, RIFF_HEADER.Length);
            targetStream.Write(PackageInt(byteStreamSize + 42, 4), 0, 4);

            targetStream.Write(FORMAT_WAVE, 0, FORMAT_WAVE.Length);
            targetStream.Write(FORMAT_TAG, 0, FORMAT_TAG.Length);
            targetStream.Write(PackageInt(16, 4), 0, 4);//Subchunk1Size    

            targetStream.Write(AUDIO_FORMAT, 0, AUDIO_FORMAT.Length);//AudioFormat   
            targetStream.Write(PackageInt(channelCount, 2), 0, 2);
            targetStream.Write(PackageInt(sampleRate, 4), 0, 4);
            targetStream.Write(PackageInt(byteRate, 4), 0, 4);
            targetStream.Write(PackageInt(blockAlign, 2), 0, 2);
            targetStream.Write(PackageInt(BYTES_PER_SAMPLE * 8), 0, 2);
            //targetStream.Write(PackageInt(0,2), 0, 2);//Extra param size
            targetStream.Write(SUBCHUNK_ID, 0, SUBCHUNK_ID.Length);
            targetStream.Write(PackageInt(byteStreamSize, 4), 0, 4);
        }

        private Bitmap[][] select_subimage(Bitmap source, int divide_size) {

            Bitmap[][] reval = new Bitmap[divide_size][];
            
            for (int i=0; i<divide_size; i++){
                reval[i] = new Bitmap[divide_size];
            }

            int sub_start_x = 0;
            int sub_start_y = 0;
            int sub_width = 0;
            int sub_height = 0;

            sub_width = source.Width/divide_size-1;
            sub_height = source.Height/divide_size-1;

            for (int i = 0; i < divide_size; i++) {
                for (int j = 0; j < divide_size; j++) {
                    sub_start_x = i * sub_width;
                    sub_start_y = j * sub_height;
                    reval[i][j] = source.Clone(
                        new Rectangle(sub_start_x, sub_start_y, sub_width, sub_height), 
                        source.PixelFormat);
                }
            }

            return reval;
        }

        private byte[] convertShorttoByte(short[] source) {
            byte[] retVal = new byte[source.Length * 2];
            Buffer.BlockCopy(source, 0, retVal, 0, retVal.Length);
            return retVal;
        }
    }
}

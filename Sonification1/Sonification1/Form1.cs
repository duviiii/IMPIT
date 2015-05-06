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
            string filepath = @"E:\sample.wav";
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

            // Split selected area into 4x4 field    
            Bitmap[][] cropped_matrix = select_subimage(cropped, divide_size);

            // TODO: Generate Sin wave for each area, merged them into one sound data
            List<byte> divided_sound_data = new List<byte>();
            TimeSpan divided_sound_length = TimeSpan.FromMilliseconds(sound_length*1000 / (divide_size * divide_size));

            for (int i = 0; i < divide_size; i++) {
                for (int j = 0; j < divide_size; j++) {
                    // Generate Sin wave
                    Bitmap current = cropped_matrix[i][j];

                    var tmp = CreateSinWave(current, sampleRate, high_frequency, divided_sound_length, 1d);
                    // Add sound information into data list
                    divided_sound_data.AddRange(tmp);
                }
            }
            
            // convert data list into data array
            var soundData = divided_sound_data.ToArray();

            // Save sound data into output .wav file
            using (FileStream fs = new FileStream(filepath, FileMode.Create))
            {
                WriteHeader(fs, soundData.Length, 1, sampleRate);
                fs.Write(soundData, 0, soundData.Length);
                fs.Close();
            }
            
            // replay the file
            SoundPlayer player = new SoundPlayer(filepath);               
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

        public static byte[] CreateSinWave(
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
            short[] tempBuffer = new short[sampleCount];
            byte[] retVal = new byte[sampleCount*2];
            double new_frequency = frequency*(1 + avg/int.MaxValue);
            double step = Math.PI * 2.0d * new_frequency/sampleRate;
            double current = 0;

            for (int i = 0; i < tempBuffer.Length; ++i)
            {
                short tmp = (short)(Math.Sin(current) * magnitude * (short.MaxValue));
                tempBuffer[i] = tmp;
                current += step;
            }

            Buffer.BlockCopy(tempBuffer, 0, retVal, 0, retVal.Length);
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
                    sub_start_y = i * sub_height;
                    reval[i][j] = source.Clone(
                        new Rectangle(sub_start_x, sub_start_y, sub_width, sub_height), 
                        source.PixelFormat);
                }
            }

            return reval;
        }
    }
}

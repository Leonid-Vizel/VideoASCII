using Accord.Video.FFMPEG;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace VideoASCII
{
    public partial class Form1 : Form
    {
        private readonly int threadNum;
        private ThreadResults[] results;
        private int currentFrame;
        private int currentResult;
        private Label formDisplay;
        private static string[] _AsciiChars = { "#", "#", "@", "%", "=", "+", "*", ":", "-", ".", "&nbsp;" };
        public Form1()
        {
            formDisplay = new Label();
            Form frm = new Form();
            frm.Controls.Add(formDisplay);
            frm.Show();

            threadNum = Environment.ProcessorCount / 2;
            results = new ThreadResults[threadNum];
            InitializeComponent();
            for (int i = 0; i < threadNum; i++)
            {
                BackgroundWorker worker = new BackgroundWorker();
                worker.DoWork += ProcessFrames;
                worker.RunWorkerAsync(("videoplay.mp4",i));
            }
            Thread.Sleep(2000);
            frameTimer.Start();
        }

        public void ProcessFrames(object sender, DoWorkEventArgs e)
        {
            (string, int) tuple = ((string, int))e.Argument;
            int frame = tuple.Item2;
            ThreadResults currentResult = new ThreadResults(tuple.Item1);
            results[frame] = currentResult;
            for (int i = frame; i < currentResult.frameReader.FrameCount; i += threadNum)
            {
                currentResult.frames.Add(GetFinalDcumentText(currentResult.frameReader.ReadVideoFrame(i)));
            }
        }

        public static string GetASCIIString(Bitmap initial)
        {
            using (Graphics graphics = Graphics.FromImage(initial))
            {
                using (ImageAttributes attributes = new ImageAttributes())
                {
                    attributes.SetColorMatrix(new ColorMatrix(
                               new float[][]
                               {
                                 new float[] {.3f, .3f, .3f, 0, 0},
                                 new float[] {.59f, .59f, .59f, 0, 0},
                                 new float[] {.11f, .11f, .11f, 0, 0},
                                 new float[] {0, 0, 0, 1, 0},
                                 new float[] {0, 0, 0, 0, 1}
                               }));
                    graphics.DrawImage(initial, new Rectangle(0, 0, initial.Width, initial.Height), 0, 0, initial.Width, initial.Height, GraphicsUnit.Pixel, attributes);
                }
                graphics.Flush();
            }
            StringBuilder builder = new StringBuilder();
            for (int y = 0; y < initial.Height; y++)
            {
                for (int x = 0; x < initial.Width; x++)
                {
                    builder.Append(_AsciiChars[initial.GetPixel(x, y).R * 10 / 255]);
                }
                builder.Append("<BR>");
            }
            return builder.ToString();
        }

        public static string GetFinalDcumentText(Bitmap initial)
            => $"<body style = \"background-color:black; zoom: 50%;\"><p style = \"color:white\" style=\"font-family:'Courier New'\">{GetASCIIString(initial)}</p></body>";

        public static Bitmap ResizeBitmap(Bitmap initial, Size size)
        {
            Bitmap newMap = new Bitmap(size.Width, size.Height);
            newMap.SetResolution(initial.HorizontalResolution, initial.VerticalResolution);

            using (Graphics graphics = Graphics.FromImage(newMap))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(initial, new Rectangle(0, 0, size.Width, size.Height), 0, 0, initial.Width, initial.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }
            return newMap;
        }

        
        private void OnTimerTick(object sender, EventArgs e)
        {
            if (currentResult == threadNum)
            {
                currentResult = 0;
                currentFrame++;
            }
            webBrowser1.DocumentText = results[currentResult].frames[currentFrame];
            currentResult++;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            formDisplay.Text = results.Sum(x => x.frames.Count).ToString();
        }
    }

    public class ThreadResults : IDisposable
    {
        public List<string> frames;
        public VideoFileReader frameReader;

        public ThreadResults(string file)
        {
            frameReader = new VideoFileReader();
            frameReader.Open(file);
            frames = new List<string>();
        }

        public void Dispose()
        {
            frames.Clear();
            frameReader.Close();
            frameReader.Dispose();
        }
    }
}

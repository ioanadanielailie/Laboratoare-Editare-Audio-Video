using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Lab6
{
    public partial class Form1 : Form
    {
        Image<Bgr, byte> myImage;
        Image<Bgr, byte> resizedImage;
        Image<Bgr, byte> rotatedImage;
        Image<Gray, byte> grayImage;
        Image<Gray, byte> imgOutput;
        Image<Bgr, byte> gammaOutput;
        Rectangle rectangle;
        Point startROI;
        bool mouseDown;

        int TotalFrame, FrameNo;
        double Fps;
        bool IsReadingFrame;
        VideoCapture capture;
        private static VideoCapture cameraCapture;
        private Image<Bgr, Byte> newBackgroundImage;
        private static IBackgroundSubtractor fgDetector;

        public Form1()
        {
            InitializeComponent();
        }

        public void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog OpenFile = new OpenFileDialog ();
            if(OpenFile.ShowDialog() == DialogResult.OK)
            {
                myImage = new Image<Bgr, Byte>(OpenFile.FileName);
                pictureBox1.Image = myImage.ToBitmap();
            }
        }
        
        private void button2_Click(object sender, EventArgs e)
        {
            grayImage = myImage.Convert<Gray,Byte>();
            pictureBox2.Image = grayImage.AsBitmap();
            grayImage[0,0] = new Gray(200);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            HistogramViewer v = new HistogramViewer();
            v.HistogramCtrl.GenerateHistograms(myImage, 255);
            v.Show();
        }

        
        private void button4_Click(object sender, EventArgs e)
        {
            float alpha = float.Parse(textBox1.Text);
            float beta = float.Parse(textBox2.Text);
            imgOutput = new Image<Gray, Byte>(grayImage.Width, grayImage.Height);

            for (int i = 0; i < grayImage.Height; i++)
                for (int j = 0; j < grayImage.Width; j++)
                {
                    var formula = grayImage[i, j].Intensity * alpha + beta; 
                    imgOutput[i,j] = new Gray(formula);
                }
            pictureBox2.Image = imgOutput.AsBitmap();
        }

        
        private void button5_Click(object sender, EventArgs e)
        {
            gammaOutput = myImage;
            double gamma = double.Parse(textBox3.Text);
            gammaOutput._GammaCorrect(gamma);
            pictureBox2.Image = gammaOutput.AsBitmap();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            resizedImage = new Image<Bgr, byte>(myImage.Width, myImage.Height);
            resizedImage = myImage.Resize(256,128,Emgu.CV.CvEnum.Inter.Cubic);
            pictureBox2.Image = resizedImage.AsBitmap();
        }
   
        private void button7_Click(object sender, EventArgs e)
        {
            rotatedImage = new Image<Bgr, byte>(myImage.Width, myImage.Height);
            rotatedImage = myImage.Rotate(45,new Bgr(255,255,255));
            pictureBox2.Image = rotatedImage.AsBitmap();
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            mouseDown = true;
            startROI = e.Location;
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (pictureBox1.Image == null)
            {
                return;
            }
            int width = Math.Max(startROI.X, e.X) - Math.Min(startROI.X, e.X);
            int height = Math.Max(startROI.Y, e.Y) - Math.Min(startROI.Y, e.Y);
            rectangle = new Rectangle(
                    Math.Min(startROI.X, e.X),
                    Math.Min(startROI.Y, e.Y),
                    width,
                    height
                );
            int mouseX = e.X;
            int mouseY = e.Y;
            Refresh();
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            mouseDown = false;
            if (pictureBox1.Image == null || rectangle == Rectangle.Empty)
                return;

            var img = new Bitmap(pictureBox1.Image).ToImage<Bgr, byte>();
            img.ROI = rectangle;
            var imgROI = img.Copy();
            pictureBox2.Image = imgROI.ToBitmap();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                capture = new VideoCapture(ofd.FileName);
                Mat m = new Mat();
                capture.Read(m);
                pictureBox1.Image = m.ToBitmap();

                TotalFrame = (int)capture.Get(CapProp.FrameCount);
                Fps = capture.Get(CapProp.Fps);
                FrameNo = 1;
                numericUpDown1.Value = FrameNo;
                numericUpDown1.Minimum = 0;
                numericUpDown1.Maximum = TotalFrame;

            }

        }

        private async void ReadAllFrames()
        {

            Mat m = new Mat();
            while (IsReadingFrame == true && FrameNo < TotalFrame)
            {
                FrameNo += 1;
                var mat = capture.QueryFrame();
                pictureBox1.Image = mat.ToBitmap();
                await Task.Delay(1000 / Convert.ToInt16(Fps));
                label4.Text = FrameNo.ToString() + "/" + TotalFrame.ToString();
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            //try
            //{
            //    cameraCapture = new VideoCapture(@"D:\an3sem2\Editare audio-video\Do. Or do not. There is no try.mp4");
            //    newBackgroundImage = new Image<Bgr, byte>(@"D:\an3sem2\Editare audio-video\lab-editare\lab-editare\Lab6\Untitled.jpg");
            //    fgDetector = new BackgroundSubtractorMOG2();
            //    Application.Idle += ProcessFrames;
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show(ex.Message);
            //    return;
            //}
            if (capture == null)
            {
                return;
            }
            IsReadingFrame = true;
            ReadAllFrames();

        }

        private async void ProcessFrames(object sender, EventArgs e)
        {
            Mat frame = cameraCapture.QueryFrame();
            Image<Bgr, byte> frameImage = frame.ToImage<Bgr, Byte>();

            Mat foregroundMask = new Mat();
            fgDetector.Apply(frame, foregroundMask);
            var foregroundMaskImage = foregroundMask.ToImage<Gray, Byte>();
            foregroundMaskImage = foregroundMaskImage.Not();

            var copyOfNewBackgroundImage = newBackgroundImage.Resize(foregroundMaskImage.Width, foregroundMaskImage.Height, Inter.Lanczos4);
            copyOfNewBackgroundImage = copyOfNewBackgroundImage.Copy(foregroundMaskImage);

            foregroundMaskImage = foregroundMaskImage.Not();
            frameImage = frameImage.Copy(foregroundMaskImage);
            frameImage = frameImage.Or(copyOfNewBackgroundImage);
        }

        private async void button10_Click(object sender, EventArgs e)
        {
            string[] FileNames = Directory.GetFiles(@"C:\Users\ioana\Documents\GitHub\Laboratoare-Editare-Audio-Video\Lab6", "*.jpg");
            List<Image<Bgr, byte>> listImages = new List<Image<Bgr, byte>>();
            foreach (var file in FileNames)
            {
                listImages.Add(new Image<Bgr, byte>(file));
            }
            for (int i = 0; i < listImages.Count - 1; i++)
            {
                for (double alpha = 0.0; alpha <= 1.0; alpha += 0.01)
                {
                    pictureBox1.Image = listImages[i + 1].AddWeighted(listImages[i], alpha, 1 - alpha, 0).AsBitmap();
                    await Task.Delay(25);
                }
            }


        }

        private void button11_Click(object sender, EventArgs e)
        {
            VideoCapture capture = new VideoCapture(@"C:\Users\ioana\Documents\GitHub\Laboratoare-Editare-Audio-Video\Lab6\video.mpg");

            int Fourcc = Convert.ToInt32(capture.Get(CapProp.FourCC));
            int Width = Convert.ToInt32(capture.Get(CapProp.FrameWidth));
            int Height = Convert.ToInt32(capture.Get(CapProp.FrameHeight));
            var Fps = capture.Get(CapProp.Fps);
            var TotalFrame = capture.Get(CapProp.FrameCount);


            string destionpath = @"C:\Users\ioana\Documents\GitHub\Laboratoare-Editare-Audio-Video\Lab6\ouput.mpg";
            using (VideoWriter writer = new VideoWriter(destionpath, Fourcc, Fps, new Size(Width, Height), true))
            {
                Image<Bgr, byte> logo = new Image<Bgr, byte>(@"C:\Users\ioana\Documents\GitHub\Laboratoare-Editare-Audio-Video\Lab6\logo.jpg");
                Mat m = new Mat();

                var FrameNo = 1;
                while (FrameNo < TotalFrame)
                {
                    capture.Read(m);
                    Image<Bgr, byte> img = m.ToImage<Bgr, byte>();
                    img.ROI = new Rectangle(Width - logo.Width - 30, 10, logo.Width, logo.Height);
                    logo.CopyTo(img);

                    img.ROI = Rectangle.Empty;

                    writer.Write(img.Mat);
                    FrameNo++;
                }
            }

        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            if (mouseDown)
            {
                using (Pen pen = new Pen(Color.Red, 1))
                {
                    e.Graphics.DrawRectangle(pen, rectangle);
                }
            }
        }
    }
}

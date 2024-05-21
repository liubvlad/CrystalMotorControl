namespace CrystalMotorControl
{
    using System;
    using System.Drawing;
    using System.IO;
    using System.Threading;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media.Imaging;
    using DirectShowLib;
    using Emgu.CV;

    public partial class CameraUserControl : UserControl
    {
        private Thread _thread;

        private VideoCapture _capture = null;
        private DsDevice[] webCams = null;
        private int selectedCameraId = 0;

        public CameraUserControl()
        {
            InitializeComponent();

            webCams = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
            selectedCameraId = webCams.Length - 1;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
        }

        public void StartCameraThread()
        {
            _thread = new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                InitCameraCapture();
            });
            _thread?.Start();
        }

        public void StopCameraThread()
        {
            _capture?.Release();
            _capture?.Stop();
        }

        private void InitCameraCapture()
        {
            try
            {
                if (webCams.Length > 0)
                {
                    _capture?.Stop();

                    _capture = new VideoCapture(selectedCameraId);
                    _capture.ImageGrabbed += _capture_ImageGrabbed;

                    _capture.Start();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Проблемы подключения к камере!{Environment.NewLine}{ex.Message}", "Ошибка");
            }
        }

        private void _capture_ImageGrabbed(object sender, EventArgs e)
        {
            try
            {
                Mat m = new Mat();
                _capture.Retrieve(m);

                Dispatcher.Invoke(new Action(() =>
                    cameraBox.Source = ConvertBitmap(m.ToBitmap())
                ));
            }
            catch { }
        }

        private void cameraBox_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                webCams = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
                if (webCams.Length > 0)
                {
                    selectedCameraId--;
                    if (selectedCameraId < 0)
                    {
                        selectedCameraId = webCams.Length - 1;
                    }

                    InitCameraCapture();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Проблемы подключения к камере!{Environment.NewLine}{ex.Message}", "Ошибка");
            }
        }

        private BitmapImage ConvertBitmap(Bitmap bitmap)
        {
            MemoryStream ms = new MemoryStream();
            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            ms.Seek(0, SeekOrigin.Begin);
            image.StreamSource = ms;
            image.EndInit();

            return image;
        }
    }
}

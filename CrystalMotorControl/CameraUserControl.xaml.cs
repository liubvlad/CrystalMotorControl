namespace CrystalMotorControl
{
    using System;
    using System.Collections.ObjectModel;
    using System.Drawing;
    using System.IO;
    using System.Linq;
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

        public int SelectedCameraId { get; private set; } = 0;
        public ObservableCollection<string> CamerasNames { get; private set; } = new ObservableCollection<string>();

        public CameraUserControl()
        {
            InitializeComponent();
            RefreshCameras();
        }

        public void RefreshCameras()
        {
            // Если необходимо по ТЗ
            _capture?.Stop();

            webCams = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
            SelectedCameraId = webCams.Length - 1;

            CamerasNames.Clear();
            foreach (var item in webCams.Select(x => x.Name)) 
            {
                CamerasNames.Add(item);
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
        }

        public void SetCameraDevice(string cameraName)
        {
            try
            {
                if (webCams.Length > 0)
                {
                    int newIndex = -1;
                    foreach (var webCam in webCams)
                    {
                        newIndex++;
                        if (webCam.Name == cameraName)
                        {
                            SelectedCameraId = newIndex;
                            InitCameraCapture();
                            return;
                        }
                    }

                    // Если не нашел камеру
                    _capture?.Stop();
                }
            }
            catch
            {
                RefreshCameras();
            }
        }

        public void StartCameraThread(bool initCapture = false)
        {
            _thread = new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                if (initCapture) InitCameraCapture();
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

                    _capture = new VideoCapture(SelectedCameraId);
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
            return;

            try
            {
                RefreshCameras();

                if (webCams.Length > 0)
                {
                    SelectedCameraId--;
                    if (SelectedCameraId < 0)
                    {
                        SelectedCameraId = webCams.Length - 1;
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

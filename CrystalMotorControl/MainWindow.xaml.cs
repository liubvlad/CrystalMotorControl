namespace CrystalMotorControl
{
    using DirectShowLib;
    using Emgu.CV;
    using Emgu.CV.Structure;
    using System;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.IO.Ports;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Interop;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Threading;
    using static Emgu.CV.ML.KNearest;

    
    public enum Directions
    {
        Left = 1,
        Right = 2,
    }

    public enum MoveState
    {
        Stop = 0,
        Moving = 1,
    }

    // !!! Убраться в коде :)

    // добавить стиль для нажатых кнопок
    // добавить изменение цвета при наведении на TOP кнопки
    // Понять какой цвет менять если мы дома
    // размытие фона за камерой (убрать синий)


    // алгоритм поиска и обновления камеры


    // каждый раз когда прошел, запоминать дом
    // если уже дома - не искать дом
    // оптимизировать алгоритм запоминания дома (в разные стороны ближе от середины) (подумать)

    // добавить картинки


    public partial class MainWindow : Window
    {
        private readonly CircularControl CircularControl;

        private readonly DispatcherTimer _positionTimer;
        private readonly DispatcherTimer _loopMovingTimer;

        private SerialPort _serialPort;
        private bool _isConnected = false;

        private readonly Style _defaultButtonStyle;
        public string Position { get; set; }
        public double MoveValue { get; set; }

        public MoveState State { get; private set; } = MoveState.Stop;
        public Directions Direction { get;  private set; } = Directions.Right;

        public MainWindow()
        {
            InitializeComponent();
            _defaultButtonStyle = buttonRightDir.Style;

            CircularControl = new CircularControl(this);
            sliderControl.Content = CircularControl;

            _positionTimer = new DispatcherTimer();
            _positionTimer.Interval = TimeSpan.FromMilliseconds(50);
            _positionTimer.Tick += PositionTimer_Tick;

            _loopMovingTimer = new DispatcherTimer();
            _loopMovingTimer.Interval = TimeSpan.FromMilliseconds(5000);
            _loopMovingTimer.Tick += LoopMovingTimer_Tick;

            SearchArduinoAndConnect();
            VisualMovingButtons();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            /*
#if DEBUG
            // TODO DEBUG
            for (double val = 60; val > -60; val -= 1.1)
            {
                var angle = 360 - (360 + val) % 360;
                CircularControl.SetDegreeAngleForCircle(angle);

                tbPos.Text = val.ToString();

                await Task.Delay(1);
            }
#endif
*/
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                /* run your code here */
                SimpleCamera();
            }).Start();
            
        }

        public void CallbackHandlerFromCircular(double value)
        {
            // Fix value by redirection
            value = 360 - value;

            // TODO del this
            //Title = value.ToString();

            try
            {
                if (_isConnected && !string.IsNullOrEmpty(MoveValue.ToString()))
                {
                    // Отправляем команду "влево" с указанным значением
                    //var sign = Direction == Directions.Right ? "-" : "";
                    // TODO fix textBoxInput
                    _serialPort.WriteLine($"moveDegTo {value}");
                }
            }
            catch { }
        }


        private VideoCapture _capture = null;
        private DsDevice[] webCams = null;

        private int selectedCameraId = 0;

        private bool _captureInProgress;
        private Mat _frame;

        private void SimpleCamera()
        {

            webCams = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);

            var sb = new StringBuilder();

            for (int i = 0; i < webCams.Length; i++)
            {
                sb.AppendLine($"{i}: {webCams[i].Name}");
            }

            MessageBox.Show(sb.ToString());

            if (webCams.Length > 0)
            {
                _capture = new VideoCapture(webCams.Length - 1);
                _capture.ImageGrabbed += _capture_ImageGrabbed;

                _capture.Start();
            }

            /*
            CvInvoke.UseOpenCL = false;
            try
            {
                _capture = new VideoCapture();
                // Подписываемся на событие
                _capture.ImageGrabbed += ProcessFrame;
            }
            catch (NullReferenceException excpt)
            {
                MessageBox.Show(excpt.Message);
            }


            _frame = new Mat();
            _capture.Start();*/
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

        public BitmapImage ConvertBitmap(System.Drawing.Bitmap bitmap)
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

        private void ProcessFrame(object sender, EventArgs arg)
        {
            if (_capture != null && _capture.Ptr != IntPtr.Zero)
            {
                _capture.Retrieve(_frame, 0);
                ///Dispatcher.Invoke(new Action(() => imageBox.Source;
                ///BitmapSourceConvert.ToBitmapSource(_frame as IImage)));
            }
        }


        [Obsolete("delete")]
        private void LoopMovingTimer_Tick(object sender, EventArgs e)
        {
            return;

            if (!_isConnected) return;

            while (State == MoveState.Moving)
            {
                var value = 1;
                var sign = Direction == Directions.Right ? "-" : "";
                _serialPort.WriteLine($"moveDeg {sign}{value}");
            }
        }

        private void SearchArduinoAndConnect()
        {
            var ports = SerialPort.GetPortNames();

            if (ports.Length < 0)
            {
                MessageBox.Show("Нет устройств для подключения!");
            }


            foreach (var port in ports)
            {
                try
                {
                    // TODO автопоиск
                    ConnectToArduino(port);

                    Title = "Подключено к " + port;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"TEMP {ex.Message}");
                }
            }



        }


        private void ConnectToArduino(string port)
        {
            _serialPort = new SerialPort(port, 9600);
            _serialPort.Open();
            _serialPort.DataReceived += SerialPort_DataReceived;

            _isConnected = true;

            _loopMovingTimer.Start();
            _positionTimer.Start();

            connectedIndicator.Fill = Brushes.Green;
        }

        private void DisconnectFromArduino()
        {
            _positionTimer.Stop();
            _loopMovingTimer.Stop();
            _serialPort.Close();

            _isConnected = false;

            connectedIndicator.Fill = Brushes.Red;
        }

        [Obsolete]
        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            var SelectedPort = "stub";

            if (!_isConnected)
            {
                if (string.IsNullOrEmpty(SelectedPort))
                {
                    MessageBox.Show("Выберите COM порт", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                try
                {
                    ConnectToArduino(SelectedPort);
                    //MessageBox.Show("Подключено к " + SelectedPort, "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при подключении: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                DisconnectFromArduino();
                //MessageBox.Show("Отключено от " + SelectedPort, "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                Title = "Отключено от " + SelectedPort;

            }
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (_isConnected)
                {
                    string receivedData = _serialPort.ReadExisting().Trim();

                    if (receivedData.Contains("optical"))
                    {
                        Dispatcher.Invoke(() => OpticalIndicatorDo(true));
                    }
                    if (receivedData.Contains("deoptical"))
                    {
                        Dispatcher.Invoke(() => OpticalIndicatorDo(false));
                    }

                    if (receivedData.StartsWith("currentDeg="))
                    {
                        var value = receivedData.Replace("currentDeg=", string.Empty);
                        value = value.Split('\r')[0];

                        Position = value.Trim();
                        try
                        {
                            Dispatcher.Invoke(() =>
                            {
                                tbPos.Text = Position;
                                var angle = Convert.ToDouble(Position.Replace('.', ','));

                                // TODO памагити (отрицательные работаю некорректно)
                                angle = 360 - Math.Abs(angle % 360);

                                CircularControl.SetDegreeAngleForCircle(angle);
                            });
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при чтении данных: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PositionTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (_isConnected)
                {
                    _serialPort.WriteLine("currentDeg"); // Отправляем команду для запроса текущего положения
                }
            }
            catch { }
        }

        private void HomeReset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_isConnected)
                {
                    // Отправляем команду "установку домой"
                    _serialPort.WriteLine("sethome");
                }
            }
            catch { }
        }

        private void OpticalIndicatorDo(bool enabled)
        {
            sliderControl.Background = enabled ? Brushes.Green : (Brush)Brushes.IndianRed;
        }

        private void buttonStartStop_Click(object sender, RoutedEventArgs e)
        {
            State = State == MoveState.Stop ? MoveState.Moving : MoveState.Stop;
            
            /**/
            if (State == MoveState.Moving)
            {
                // START
                var value = 1;
                var sign = Direction == Directions.Right ? "-" : "";
                _serialPort.WriteLine($"running {sign}{value}");
            }
            else if (State == MoveState.Stop)
            {
                // STOP
                _serialPort.WriteLine($"stop");
            }

            VisualMovingButtons();
        }

        private void ButtonLeftDir_Click(object sender, RoutedEventArgs e)
        {
            Direction = Directions.Left;
            VisualMovingButtons();
        }

        private void ButtonRightDir_Click(object sender, RoutedEventArgs e)
        {
            Direction = Directions.Right;
            VisualMovingButtons();
        }

        private void VisualMovingButtons()
        {
            // Меняем названия (картинку)
            switch (State)
            {   
                case MoveState.Stop:
                    /*buttonStartStop.Template.FindName("Image", playSequence)
            .SetValue(Image.SourceProperty,
                      new BitmapImage(new Uri(@"Pause.png", UriKind.Relative)));*/
                    //new Uri(@"pack://application:,,,/Images/1.png")
                    buttonStartStop.Content = "ПУСК";
                    break;
                case MoveState.Moving:
                    buttonStartStop.Content = "СТОП";
                    break;
                default:
                    break;
            }

            if (Direction == Directions.Right)
            {
                buttonRightDir.Style = this.FindResource("ActiveButtonStyle") as Style;
                buttonLeftDir.Style = _defaultButtonStyle;
            }
            else if (Direction == Directions.Left)
            {
                buttonLeftDir.Style = this.FindResource("ActiveButtonStyle") as Style;
                buttonRightDir.Style = _defaultButtonStyle;
            }
        }

        private void ButtonDegMove_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_isConnected)
                {
                    var senderButton = sender as Button;
                    var value = senderButton.Content.ToString().TrimEnd('°');

                    // Отправляем команду "влево" с указанным значением
                    var sign = Direction == Directions.Right ? "-" : "";
                    ///tbLog.Text += $"{DateTime.Now.Millisecond}: moveDeg {sign}{value}\n";
                    _serialPort.WriteLine($"moveDeg {sign}{value}");
                }
            }
            catch { }

        }

        private void GoButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_isConnected && !string.IsNullOrEmpty(MoveValue.ToString()))
                {
                    // Отправляем команду "влево" с указанным значением
                    var sign = Direction == Directions.Right ? "-" : "";
                    // TODO fix textBoxInput
                    _serialPort.WriteLine($"moveDeg {sign}{textBoxInput.Text}");
                }
            }
            catch { }
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            if (!_isConnected)
            {
                SearchArduinoAndConnect();
            }
        }

    }
}
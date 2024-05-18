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
    


    // !!! Убраться в коде :)

    // добавить стиль для нажатых кнопок+
    // добавить изменение цвета при наведении на TOP кнопки+
    // Понять какой цвет менять если мы дома+
    // размытие фона за камерой (убрать синий)


    // алгоритм поиска и обновления камеры


    // каждый раз когда прошел, запоминать дом
    // если уже дома - не искать дом
    // оптимизировать алгоритм запоминания дома (в разные стороны ближе от середины) (подумать)

    // добавить картинки


    public partial class MainWindow : Window
    {
        private readonly CameraUserControl CameraUserControl;
        private readonly CircularControl CircularControl;

        private readonly DispatcherTimer _positionTimer;

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

            CameraUserControl = new CameraUserControl();
            cameraBoxControl.Content = CameraUserControl;

            _positionTimer = new DispatcherTimer();
            _positionTimer.Interval = TimeSpan.FromMilliseconds(50);
            _positionTimer.Tick += PositionTimer_Tick;

            SearchArduinoAndConnect();
            VisualMovingButtons();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CameraUserControl.StartCameraThread();
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            if (!_isConnected)
            {
                SearchArduinoAndConnect();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_isConnected)
            {
                DisconnectFromArduino();
            }
        }

        public void CallbackHandlerFromCircular(double value)
        {
            // Fix value by reverse direction
            value = 360 - value;

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

                    var serialPort = new SerialPort(port, 9600);
                    serialPort.Open();
                    serialPort.WriteTimeout = 100;
                    serialPort.ReadTimeout = 100;

                    serialPort.WriteLine("ATZ");

                    Task.Delay(100);

                    var line = serialPort.ReadLine();
                    serialPort.Close();

                    if (line.StartsWith("Crystal Motor"))
                    {
                        ConnectToArduino(port);
                        Title = "Подключено к " + port;

                        return;
                    }
                }
                catch (Exception ex)
                {
                    //MessageBox.Show($"TEMP {ex.Message}");
                }
            }
        }

        private void ConnectToArduino(string port)
        {
            _serialPort = new SerialPort(port, 9600);
            _serialPort.Open();
            _serialPort.DataReceived += SerialPort_DataReceived;

            _isConnected = true;

            _positionTimer.Start();

            connectedIndicator.Fill = Brushes.Green;
        }

        private void DisconnectFromArduino()
        {
            try
            {
                _positionTimer.Stop();
                _serialPort.Close();

                _isConnected = false;
            }
            catch { }
            finally
            {
                connectedIndicator.Fill = Brushes.Red;
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
                        Dispatcher.Invoke(() => OpticalIndicatorHandler(true));
                    }
                    if (receivedData.Contains("deoptical"))
                    {
                        Dispatcher.Invoke(() => OpticalIndicatorHandler(false));
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

        private void OpticalIndicatorHandler(bool enabled)
        {
            sliderControl.Background = enabled ? Brushes.Green : (Brush)Brushes.White;
        }

        private void buttonStartStop_Click(object sender, RoutedEventArgs e)
        {
            State = State == MoveState.Stop ? MoveState.Moving : MoveState.Stop;
            
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
            /*
            buttonStartStop.Template.FindName("Image", playSequence)
                .SetValue(Image.SourceProperty,
                new BitmapImage(new Uri(@"Pause.png", UriKind.Relative)));
            new Uri(@"pack://application:,,,/Images/1.png")
            */
            switch (State)
            {   
                case MoveState.Stop:
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
    }
}

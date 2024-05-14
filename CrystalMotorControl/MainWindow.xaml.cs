namespace CrystalMotorControl
{
    using Emgu.CV;
    using System;
    using System.Collections.ObjectModel;
    using System.IO.Ports;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Threading;

    
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

    // не менять фон когда дома или нет, или менять зеленый -- серый
    // если уже дома - не искать дом
    // каждый раз когда прошел, запоминать дом

    // оптимизировать алгоритм запоминания дома (в разные стороны ближе от середины) (подумать)

    // добавить картинки
    // вывести программные ручки для слайдера (задавать угол из вне)


    public partial class MainWindow : Window
    {
        private SerialPort _serialPort;
        private readonly CircularControl CircularControl = new CircularControl();
        private bool _isConnected = false;
        private readonly DispatcherTimer _positionTimer;
        private readonly DispatcherTimer _loopMovingTimer;

        public string Position { get; set; }
        public double MoveValue { get; set; }

        public MoveState State { get; private set; } = MoveState.Stop;
        public Directions Direction { get;  private set; } = Directions.Right;

        public MainWindow()
        {
            InitializeComponent();
            //DataContext = this;

            sliderControl.Content = CircularControl;

            _positionTimer = new DispatcherTimer();
            _positionTimer.Interval = TimeSpan.FromMilliseconds(50);
            _positionTimer.Tick += PositionTimer_Tick;

            _loopMovingTimer = new DispatcherTimer();
            _loopMovingTimer.Interval = TimeSpan.FromMilliseconds(5000);
            _loopMovingTimer.Tick += LoopMovingTimer_Tick;

            SearchArduinoAndConnect();
            VisualMovingButtons();

            SimpleCamera();
        }






        private VideoCapture _capture = null;
        private bool _captureInProgress;
        private Mat _frame;
        private void SimpleCamera()
        {
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
            _capture.Start();
        }
        private void ProcessFrame(object sender, EventArgs arg)
        {
            if (_capture != null && _capture.Ptr != IntPtr.Zero)
            {
                _capture.Retrieve(_frame, 0);
                Dispatcher.Invoke(new Action(() => imageBox.Source =
                BitmapSourceConvert.ToBitmapSource(_frame as IImage)));
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

                    if (receivedData.StartsWith("currentDeg="))
                    {
                        var value = receivedData.Replace("currentDeg=", string.Empty);
                        Position = value.Trim();
                        try
                        {
                            Dispatcher.Invoke(() =>
                            {
                                tbPos.Text = Position;
                                CircularControl.SetDegreeAngleForCircle(Convert.ToDouble(Position.Replace('.', ',')));
                            });
                        }
                        catch { }
                    }

                    if (receivedData.Contains("optical"))
                    {
                        Dispatcher.Invoke(() => OpticalIndicatorDo(true));
                    }
                    if (receivedData.Contains("deoptical"))
                    {
                        Dispatcher.Invoke(() => OpticalIndicatorDo(false));
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
                    buttonStartStop.Content = "СТАРТ";
                    break;
                case MoveState.Moving:
                    buttonStartStop.Content = "СТОП";
                    break;
                default:
                    break;
            }

            buttonRightDir.Background = Direction == Directions.Right ? Brushes.Wheat : Brushes.White;
            buttonLeftDir.Background = Direction == Directions.Left ? Brushes.Wheat : Brushes.White;
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
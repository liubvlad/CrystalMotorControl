﻿namespace CrystalMotorControl
{
    using System;
    using System.IO.Ports;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Threading;

    // размытие фона за камерой (убрать синий)

    // каждый раз когда прошел, запоминать дом
    // если уже дома - не искать дом
    // оптимизировать алгоритм запоминания дома (в разные стороны ближе от середины) (подумать)

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
            camerasComboBox.ItemsSource = CameraUserControl.CamerasNames;

            _positionTimer = new DispatcherTimer();
            _positionTimer.Interval = TimeSpan.FromMilliseconds(50);
            _positionTimer.Tick += PositionTimer_Tick;

            SearchArduinoAndConnect();
            VisualMovingButtons();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                CameraUserControl?.StartCameraThread();
                // Может долго грузиться окно
                //camerasComboBox.SelectedIndex = CameraUserControl.SelectedCameraId;
            }
            catch { }
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            if (!_isConnected)
            {
                SearchArduinoAndConnect(showDialogMessages: false);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

            try
            {
                CameraUserControl?.StopCameraThread();
                CameraUserControl.IsEnabled = false;

                if (_serialPort.IsOpen && _isConnected)
                {
                    _serialPort.WriteLine($"stop");

                    // TODO зависает (проблема по потокам??)
                    //DisconnectFromArduino();
                }
            }
            catch { }

            Application.Current.Shutdown();
        }

        public void CallbackHandlerFromCircular(double angleAbsolute)
        {
            // Fix value by reverse direction
            var value = 360 - angleAbsolute;
            var currentAngle = Convert.ToDouble(Position.Replace('.', ','));

            // TODO испр кратчайший путь (см. Егор)
            value = value - currentAngle;

            try
            {
                if (_isConnected && !string.IsNullOrEmpty(MoveValue.ToString()))
                {
                    _serialPort.WriteLine($"moveDeg {value}");
                }
            }
            catch { }
        }

        private void SearchArduinoAndConnect(bool showDialogMessages = true)
        {
            var ports = SerialPort.GetPortNames();

            if (ports.Length < 0)
            {
                if (showDialogMessages) MessageBox.Show("Нет устройств для подключения!");
            }

            foreach (var port in ports)
            {
                try
                {
                    // TODO автопоиск

                    var serialPort = new SerialPort(port, 9600);
                    serialPort.Open();
                    serialPort.WriteTimeout = 200;
                    serialPort.ReadTimeout = 200;

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
                catch (UnauthorizedAccessException ex)
                {
                    if (showDialogMessages) MessageBox.Show($"{ex.Message}");
                }

                catch (Exception ex)
                {
                    //MessageBox.Show($"TEMP {ex.Message}");
                }
            }

            Title = "Устройство не найдено";
            if (showDialogMessages) MessageBox.Show($"Устройство не найдено");
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
                if (_serialPort.IsOpen)
                {
                    _serialPort.Close();
                }

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
                                if (angle >= 0)
                                {
                                    angle = 360 - Math.Abs(angle % 360);
                                }
                                else
                                {
                                    angle = Math.Abs(angle % 360);
                                }

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
            try
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
            catch { }
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
                    _serialPort.WriteLine($"moveDeg {sign}{textBoxInput.Text.Replace(',', ',')}");
                }
            }
            catch { }
        }

        private bool _isCameraFullScreen = false;
        private void buttonFullScreen_Click(object sender, RoutedEventArgs e)
        {
            if (!_isCameraFullScreen)
            {
                Grid.SetRow(cameraBorder, 0);
                Grid.SetColumn(cameraBorder, 0);

                Grid.SetRowSpan(cameraBorder, 3);
                Grid.SetColumnSpan(cameraBorder, 2);

                cameraBorder.Margin = new Thickness(0);

                _isCameraFullScreen = true;
            }
        }

        private void cameraBoxControl_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_isCameraFullScreen)
            {
                Grid.SetRow(cameraBorder, 1);
                Grid.SetColumn(cameraBorder, 0);

                Grid.SetRowSpan(cameraBorder, 1);
                Grid.SetColumnSpan(cameraBorder, 1);

                cameraBorder.Margin = new Thickness(15, 0, 25, 25);

                _isCameraFullScreen = false;
            }
        }

        private void camerasComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (camerasComboBox.SelectedIndex != -1)
                {
                    CameraUserControl.SetCameraDevice(camerasComboBox.SelectedItem.ToString());
                }
            }
            catch { }
        }

        private void buttonRefreshCameras_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                camerasComboBox.SelectedIndex = -1;
                CameraUserControl.RefreshCameras();
            }
            catch { }
        }
    }
}

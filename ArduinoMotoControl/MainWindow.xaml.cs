namespace ArduinoMotoControl
{
    using System;
    using System.Collections.ObjectModel;
    using System.IO.Ports;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Threading;

    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SerialPort _serialPort;
        private bool _isConnected = false;
        private readonly DispatcherTimer _positionTimer;

        public ObservableCollection<string> AvailablePorts { get; set; } = new ObservableCollection<string>();
        public string SelectedPort { get; set; }
        public string Position { get; set; }
        public string MoveValue { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            RefreshPortList();


            _positionTimer = new DispatcherTimer();
            _positionTimer.Interval = TimeSpan.FromMilliseconds(250); // Период запроса текущего положения (5 секунд)
            _positionTimer.Tick += PositionTimer_Tick;
        }

        private void RefreshPortList()
        {
            AvailablePorts.Clear();
            foreach (var port in SerialPort.GetPortNames())
            {
                AvailablePorts.Add(port);
            }
        }

        private void RefreshPortsButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshPortList();
        }

        private void OpticalIndicatorDo(bool enabled)
        {
            gridOpEnd.Background = enabled ? Brushes.Green : (Brush)Brushes.IndianRed;
        }

        private void ConnectToArduino()
        {
            _serialPort = new SerialPort(SelectedPort, 9600);
            _serialPort.Open();
            _serialPort.DataReceived += SerialPort_DataReceived;

            _isConnected = true;
            ConnectButton.Content = "Отключить";

            _positionTimer.Start();

            connectedIndicator.Fill = Brushes.Green;
        }

        private void DisconnectFromArduino()
        {
            _positionTimer.Stop();
            _serialPort.Close();

            _isConnected = false;
            ConnectButton.Content = "Подключить";

            connectedIndicator.Fill = Brushes.Red;
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isConnected)
            {
                if (string.IsNullOrEmpty(SelectedPort))
                {
                    MessageBox.Show("Выберите COM порт", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                try
                {
                    ConnectToArduino();
                    //MessageBox.Show("Подключено к " + SelectedPort, "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    Title = "Подключено к " + SelectedPort;
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

        private async void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (_isConnected)
                {
                    string receivedData = _serialPort.ReadExisting().Trim();

                    if (receivedData.StartsWith("current="))
                    {
                        var value = receivedData.Replace("current=", string.Empty);
                        Position = value.Trim();
                        Dispatcher.Invoke(() => tbPos.Text = Position);
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
                    _serialPort.WriteLine("current"); // Отправляем команду для запроса текущего положения
                }
            }
            catch { }
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_isConnected)
                {
                    // Отправляем команду "домой"
                    _serialPort.WriteLine("home");
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

        private void LeftButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_isConnected && !string.IsNullOrEmpty(MoveValue))
                {
                    // Отправляем команду "влево" с указанным значением
                    _serialPort.WriteLine($"move {MoveValue}");
                }
            }
            catch { }
        }

        private void RightButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_isConnected && !string.IsNullOrEmpty(MoveValue))
                {
                    // Отправляем команду "вправо" с указанным значением
                    _serialPort.WriteLine($"move -{MoveValue}");
                }
            }
            catch { }
        }


    }
}
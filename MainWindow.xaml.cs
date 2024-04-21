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
        public string LeftValue { get; set; }
        public string RightValue { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            RefreshPortList();


            _positionTimer = new DispatcherTimer();
            _positionTimer.Interval = TimeSpan.FromSeconds(1); // Период запроса текущего положения (5 секунд)
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

        private async Task OpticalIndicatorDoAsync()
        {
            var defaultColor = gridOpEnd.Background;

            gridOpEnd.Background = Brushes.Green;
            await Task.Delay(500);

            gridOpEnd.Background = defaultColor;
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
                    string receivedData = _serialPort.ReadExisting();

                    if (receivedData.StartsWith("curTarg="))
                    {
                        var value = receivedData.Replace("curTarg=", string.Empty);

                        Position = receivedData.Trim();
                    }

                    if (receivedData.StartsWith("optical"))
                    {
                        await OpticalIndicatorDoAsync();
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
                    _serialPort.WriteLine("targ"); // Отправляем команду для запроса текущего положения
                }
            }
            catch { }
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            OpticalIndicatorDoAsync();

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

        private void LeftButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_isConnected && !string.IsNullOrEmpty(LeftValue))
                {
                    // Отправляем команду "влево" с указанным значением
                    _serialPort.WriteLine($"left {LeftValue}");
                }
            }
            catch { }
        }

        private void RightButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_isConnected && !string.IsNullOrEmpty(RightValue))
                {
                    // Отправляем команду "вправо" с указанным значением
                    _serialPort.WriteLine($"right {RightValue}");
                }
            }
            catch { }
        }
    }
}
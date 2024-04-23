namespace CrystalMotorControl
{
    using System;
    using System.Collections.ObjectModel;
    using System.IO.Ports;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Threading;

    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    /// 


    public partial class MainWindow : Window
    {
        private SerialPort _serialPort;
        private bool _isConnected = false;
        private readonly DispatcherTimer _positionTimer;

        public string Position { get; set; }
        public double MoveValue { get; set; }

        private bool Direction = true;

        public MainWindow()
        {
            InitializeComponent();
            //DataContext = this;


            _positionTimer = new DispatcherTimer();
            _positionTimer.Interval = TimeSpan.FromMilliseconds(200);
            _positionTimer.Tick += PositionTimer_Tick;

            SearchArduinoAndConnect();



            ColorDirectionButtons();
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

            _positionTimer.Start();

            connectedIndicator.Fill = Brushes.Green;
        }

        private void DisconnectFromArduino()
        {
            _positionTimer.Stop();
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

        private async void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
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
            clock.Background = enabled ? Brushes.Green : (Brush)Brushes.IndianRed;
        }

        private void LeftDirectionButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void buttonLeftDir_Click(object sender, RoutedEventArgs e)
        {
            Direction = false;
            ColorDirectionButtons();
        }

        private void buttonRightDir_Click(object sender, RoutedEventArgs e)
        {
            Direction = true;
            ColorDirectionButtons();
        }

        private void ColorDirectionButtons()
        {
            if (Direction)
            {
                buttonRightDir.Background = Brushes.Wheat;
                buttonLeftDir.Background = Brushes.White;
            }
            else
            {
                buttonRightDir.Background = Brushes.White;
                buttonLeftDir.Background = Brushes.Wheat;
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
                    var sign = Direction ? "-" : "";
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
                    var sign = Direction ? "-" : "";
                    // TODO fix textBoxInput
                    _serialPort.WriteLine($"moveDeg {sign}{textBoxInput.Text.ToString()}");
                }
            }
            catch { }
        }
    }
}
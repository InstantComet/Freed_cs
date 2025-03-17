using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using FreeDSender.Models;
using FreeDSender.Services;

namespace FreeDSender.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private string _packetData;
        private readonly UdpService _udpService;
        private bool _isRunning;
        private string _status;
        private FreeDData _freeDData;
        private double _maxPosX = 10000;
        private double _maxPosY = 10000;
        private double _maxPosZ = 10000;

        public double MinPosX => -MaxPosX;
        public double MinPosY => -MaxPosY;
        public double MinPosZ => -MaxPosZ;

        private int _frameRate = 50;
        public int FrameRate
        {
            get => _frameRate;
            set
            {
                if (value < 1) value = 1;
                if (value > 240) value = 240;
                _frameRate = value;
                OnPropertyChanged();
            }
        }

        private void UpdateFreeDDataMaxPos()
        {
            if (_freeDData != null)
            {
                _freeDData.MaxPosX = _maxPosX;
                _freeDData.MaxPosY = _maxPosY;
                _freeDData.MaxPosZ = _maxPosZ;
            }
        }

        public double MaxPosX
        {
            get => _maxPosX;
            set
            {
                var newValue = Math.Min(value, 131072);
                if (_maxPosX != newValue)
                {
                    _maxPosX = newValue;
                    UpdateFreeDDataMaxPos();
                    OnPropertyChanged(nameof(MaxPosX));
                    OnPropertyChanged(nameof(MinPosX));
                }
            }
        }

        public double MaxPosY
        {
            get => _maxPosY;
            set
            {
                var newValue = Math.Min(value, 131072);
                if (_maxPosY != newValue)
                {
                    _maxPosY = newValue;
                    UpdateFreeDDataMaxPos();
                    OnPropertyChanged(nameof(MaxPosY));
                    OnPropertyChanged(nameof(MinPosY));
                }
            }
        }

        public double MaxPosZ
        {
            get => _maxPosZ;
            set
            {
                var newValue = Math.Min(value, 131072);
                if (_maxPosZ != newValue)
                {
                    _maxPosZ = newValue;
                    UpdateFreeDDataMaxPos();
                    OnPropertyChanged(nameof(MaxPosZ));
                    OnPropertyChanged(nameof(MinPosZ));
                }
            }
        }

        public MainViewModel()
        {
            _udpService = new UdpService();
            _udpService.PacketSent += OnPacketSent;
            FreeDData = new FreeDData();
            _freeDData = FreeDData;
            StartCommand = new RelayCommand(Start, CanStart);
            StopCommand = new RelayCommand(Stop, CanStop);
            Status = "Ready";
            _status = Status;
            PacketData = string.Empty;
            _packetData = PacketData;
            IpAddress = "";
            Port = "";
            FrameRate = 50;
        }

        public string PacketData
        {
            get => _packetData;
            private set
            {
                _packetData = value;
                OnPropertyChanged();
            }
        }

        private void OnPacketSent(byte[] data)
        {
            string packetInfo = $"Packet size: {data.Length} bytes\n";
            packetInfo += $"Pan: {FreeDData.Pan:F2}\n";
            packetInfo += $"Tilt: {FreeDData.Tilt:F2}\n";
            packetInfo += $"Roll: {FreeDData.Roll:F2}\n";
            packetInfo += $"X: {FreeDData.PosX:F2}\n";
            packetInfo += $"Y: {FreeDData.PosY:F2}\n";
            packetInfo += $"Z: {FreeDData.PosZ:F2}\n";
            packetInfo += $"Zoom: {FreeDData.Zoom}\n";
            packetInfo += $"Focus: {FreeDData.Focus}\n";
            packetInfo += $"Raw data: {BitConverter.ToString(data)}";

            PacketData = packetInfo;
        }
        public FreeDData FreeDData
        {
            get => _freeDData;
            set
            {
                _freeDData = value;
                OnPropertyChanged();
            }
        }

        public string IpAddress { get; set; } = "";
        public string Port { get; set; } = "";

        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged();
            }
        }

        public ICommand StartCommand { get; }
        public ICommand StopCommand { get; }

        private bool CanStart() => !_isRunning;
        private bool CanStop() => _isRunning;

        private async void Start()
        {
            try
            {
                if (!int.TryParse(Port, out int port))
                    throw new ArgumentException("Invalid port number");

                _udpService.Connect(IpAddress, port);
                _isRunning = true;
                Status = $"Sending data to {IpAddress}:{Port}...";

                var stopwatch = new System.Diagnostics.Stopwatch();
                var frameCount = 0;
                var lastFpsUpdate = DateTime.Now;
                var targetFrameTime = TimeSpan.FromMilliseconds(1000.0 / FrameRate);
                var nextFrameTime = 0L;

                await Task.Factory.StartNew(() =>
                {
                    stopwatch.Start();
                    while (_isRunning)
                    {
                        _udpService.Send(FreeDData);
                        frameCount++;

                        var now = DateTime.Now;
                        if ((now - lastFpsUpdate).TotalSeconds >= 1)
                        {
                            Status = $"Sending data to {IpAddress}:{Port} at {frameCount}fps";
                            frameCount = 0;
                            lastFpsUpdate = now;
                        }

                        nextFrameTime += targetFrameTime.Ticks;
                        var currentTime = stopwatch.ElapsedTicks;
                        if (nextFrameTime > currentTime)
                        {
                            System.Threading.SpinWait.SpinUntil(() => stopwatch.ElapsedTicks >= nextFrameTime);
                        }
                    }
                }, TaskCreationOptions.LongRunning);
            }
            catch (Exception ex)
            {
                Status = $"Error: {ex.Message}";
                Stop();
            }
        }

        private void Stop()
        {            _isRunning = false;
            _udpService.Close();
            Status = "Stopped";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RelayCommand : ICommand
    {        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object parameter) => _execute();
    }
}
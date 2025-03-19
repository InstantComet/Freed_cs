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
        private bool _isXAnimationRunning;
        private bool _isYAnimationRunning;
        private bool _isZAnimationRunning;
        private double _animationSpeed = 1.0;
        private System.Windows.Threading.DispatcherTimer _animationTimer;
        private double _animationPhaseX;
        private double _animationPhaseY;
        private double _animationPhaseZ;
        private Controls.AnimationCurveControl _animationCurveX;
        private Controls.AnimationCurveControl _animationCurveY;
        private Controls.AnimationCurveControl _animationCurveZ;
        public ICommand StopAllAnimationsCommand { get; private set; }

        public double AnimationSpeed
        {
            get => _animationSpeed * 10;
            set
            {
                var newValue = Math.Max(1, Math.Min(10, value));
                var scaledValue = newValue / 10.0;
                if (_animationSpeed != scaledValue)
                {
                    _animationSpeed = scaledValue;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsXAnimationRunning
        {
            get => _isXAnimationRunning;
            private set
            {
                _isXAnimationRunning = value;
                OnPropertyChanged();
            }
        }

        public bool IsYAnimationRunning
        {
            get => _isYAnimationRunning;
            private set
            {
                _isYAnimationRunning = value;
                OnPropertyChanged();
            }
        }

        public bool IsZAnimationRunning
        {
            get => _isZAnimationRunning;
            private set
            {
                _isZAnimationRunning = value;
                OnPropertyChanged();
            }
        }

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

        public ICommand ResetPanCommand { get; }
        public ICommand ResetTiltCommand { get; }
        public ICommand ResetRollCommand { get; }
        public ICommand ResetPosXCommand { get; }
        public ICommand ToggleXAnimationCommand { get; }
        public ICommand ToggleYAnimationCommand { get; }
        public ICommand ToggleZAnimationCommand { get; }
        public ICommand ResetPosYCommand { get; }
        public ICommand ResetPosZCommand { get; }

        private void ResetPan()
        {
            FreeDData.Pan = 0;
            OnPropertyChanged(nameof(FreeDData));
        }

        private void ResetTilt()
        {
            FreeDData.Tilt = 0;
            OnPropertyChanged(nameof(FreeDData));
        }

        private void ResetRoll()
        {
            FreeDData.Roll = 0;
            OnPropertyChanged(nameof(FreeDData));
        }

        private void ResetPosX()
        {
            FreeDData.PosX = 0;
            OnPropertyChanged(nameof(FreeDData));
        }

        private void ResetPosY()
        {
            FreeDData.PosY = 0;
            OnPropertyChanged(nameof(FreeDData));
        }

        private void ResetPosZ()
        {
            FreeDData.PosZ = 0;
            OnPropertyChanged(nameof(FreeDData));
        }

        public MainViewModel()
        {
            _udpService = new UdpService();
            _udpService.PacketSent += OnPacketSent;
            FreeDData = new FreeDData();
            _freeDData = FreeDData;
            StartCommand = new RelayCommand(Start, CanStart);
            StopCommand = new RelayCommand(Stop, CanStop);
            ResetPanCommand = new RelayCommand(() => ResetPan());
            ResetTiltCommand = new RelayCommand(() => ResetTilt());
            ResetRollCommand = new RelayCommand(() => ResetRoll());
            ResetPosXCommand = new RelayCommand(() => ResetPosX());
            ResetPosYCommand = new RelayCommand(() => ResetPosY());
            ResetPosZCommand = new RelayCommand(() => ResetPosZ());
            ToggleXAnimationCommand = new RelayCommand(ToggleXAnimation);
            ToggleYAnimationCommand = new RelayCommand(ToggleYAnimation);
            ToggleZAnimationCommand = new RelayCommand(ToggleZAnimation);
            StopAllAnimationsCommand = new RelayCommand(StopAllAnimations);
            SetBroadcastCommand = new RelayCommand(SetBroadcast);
            System.Windows.Application.Current.MainWindow.Loaded += (s, e) =>
            {
                _animationCurveX = System.Windows.LogicalTreeHelper
                    .FindLogicalNode(System.Windows.Application.Current.MainWindow, "AnimationCurveX") 
                    as Controls.AnimationCurveControl;
                _animationCurveY = System.Windows.LogicalTreeHelper
                    .FindLogicalNode(System.Windows.Application.Current.MainWindow, "AnimationCurveY") 
                    as Controls.AnimationCurveControl;
                _animationCurveZ = System.Windows.LogicalTreeHelper
                    .FindLogicalNode(System.Windows.Application.Current.MainWindow, "AnimationCurveZ") 
                    as Controls.AnimationCurveControl;
            };

            _animationTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };
            _animationTimer.Tick += OnAnimationTick;
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
        public ICommand SetBroadcastCommand { get; }

        private void SetBroadcast()
        {
            try
            {
                var ipParts = IpAddress.Split('.');
                if (ipParts.Length == 4)
                {
                    IpAddress = $"{ipParts[0]}.{ipParts[1]}.{ipParts[2]}.255";
                    OnPropertyChanged(nameof(IpAddress));
                }
            }
            catch
            {
                Status = "Invalid IP address format";
            }
        }

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
                Status = $"Sending to {IpAddress}:{Port}...";

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
                            Status = $"Sending to {IpAddress}:{Port} at {frameCount}fps";
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

        private void StopAllAnimations()
        {
            if (IsXAnimationRunning || IsYAnimationRunning || IsZAnimationRunning)
            {
                IsXAnimationRunning = false;
                IsYAnimationRunning = false;
                IsZAnimationRunning = false;
                _animationTimer.Stop();
                _animationPhaseX = 0;
                _animationPhaseY = 0;
                _animationPhaseZ = 0;
                _animationCurveX?.Clear();
                _animationCurveY?.Clear();
                _animationCurveZ?.Clear();
            }
        }

        private void ToggleXAnimation()
        {
            if (IsXAnimationRunning)
            {
                IsXAnimationRunning = false;
                _animationCurveX?.Clear();
                if (!IsYAnimationRunning && !IsZAnimationRunning)
                    _animationTimer.Stop();
            }
            else
            {
                double normalizedValue = (FreeDData.PosX - MinPosX) / (MaxPosX - MinPosX);
                _animationPhaseX = Math.Asin(normalizedValue * 2 - 1);
                if (!_animationTimer.IsEnabled)
                    _animationTimer.Start();
                IsXAnimationRunning = true;
            }
        }

        private void ToggleYAnimation()
        {
            if (IsYAnimationRunning)
            {
                IsYAnimationRunning = false;
                _animationCurveY?.Clear();
                if (!IsXAnimationRunning && !IsZAnimationRunning)
                    _animationTimer.Stop();
            }
            else
            {
                double normalizedValue = (FreeDData.PosY - MinPosY) / (MaxPosY - MinPosY);
                _animationPhaseY = Math.Asin(normalizedValue * 2 - 1);
                if (!_animationTimer.IsEnabled)
                    _animationTimer.Start();
                IsYAnimationRunning = true;
            }
        }

        private void ToggleZAnimation()
        {
            if (IsZAnimationRunning)
            {
                IsZAnimationRunning = false;
                _animationCurveZ?.Clear();
                if (!IsXAnimationRunning && !IsYAnimationRunning)
                    _animationTimer.Stop();
            }
            else
            {
                double normalizedValue = (FreeDData.PosZ - MinPosZ) / (MaxPosZ - MinPosZ);
                _animationPhaseZ = Math.Asin(normalizedValue * 2 - 1);
                if (!_animationTimer.IsEnabled)
                    _animationTimer.Start();
                IsZAnimationRunning = true;
            }
        }

        private void OnAnimationTick(object sender, EventArgs e)
        {
            if (IsXAnimationRunning)
            {
                _animationPhaseX += _animationSpeed * 0.02;
                if (_animationPhaseX > Math.PI * 2)
                    _animationPhaseX -= Math.PI * 2;

                double normalizedValueX = (Math.Sin(_animationPhaseX) + 1) / 2;
                FreeDData.PosX = MinPosX + (MaxPosX - MinPosX) * normalizedValueX;
                _animationCurveX?.AddPoint(normalizedValueX);
            }

            if (IsYAnimationRunning)
            {
                _animationPhaseY += _animationSpeed * 0.02;
                if (_animationPhaseY > Math.PI * 2)
                    _animationPhaseY -= Math.PI * 2;

                double normalizedValueY = (Math.Sin(_animationPhaseY) + 1) / 2;
                FreeDData.PosY = MinPosY + (MaxPosY - MinPosY) * normalizedValueY;
                _animationCurveY?.AddPoint(normalizedValueY);
            }

            if (IsZAnimationRunning)
            {
                _animationPhaseZ += _animationSpeed * 0.02;
                if (_animationPhaseZ > Math.PI * 2)
                    _animationPhaseZ -= Math.PI * 2;

                double normalizedValueZ = (Math.Sin(_animationPhaseZ) + 1) / 2;
                FreeDData.PosZ = MinPosZ + (MaxPosZ - MinPosZ) * normalizedValueZ;
                _animationCurveZ?.AddPoint(normalizedValueZ);
            }

            OnPropertyChanged(nameof(FreeDData));
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
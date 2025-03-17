using System;

namespace FreeDSender.Models
{
    public class FreeDData
    {
        private double _pan = 0;
        private double _tilt = 0;
        private double _roll = 0;
        private double _posX = 0;
        private double _posY = 0;
        private double _posZ = 0;
        private double _maxPosX = 10000;
        private double _maxPosY = 10000;
        private double _maxPosZ = 10000;
        private const double MAX_POS_LIMIT = 131072;

        public FreeDData(double maxPosX = 10000, double maxPosY = 10000, double maxPosZ = 10000)
        {
            MaxPosX = maxPosX;
            MaxPosY = maxPosY;
            MaxPosZ = maxPosZ;
        }

        public double MaxPosX
        {
            get => _maxPosX;
            set => _maxPosX = Math.Max(0, Math.Min(MAX_POS_LIMIT, value));
        }

        public double MaxPosY
        {
            get => _maxPosY;
            set => _maxPosY = Math.Max(0, Math.Min(MAX_POS_LIMIT, value));
        }

        public double MaxPosZ
        {
            get => _maxPosZ;
            set => _maxPosZ = Math.Max(0, Math.Min(MAX_POS_LIMIT, value));
        }

        public double Pan
        {
            get => _pan;
            set => _pan = Math.Max(-180, Math.Min(180, value));
        }

        public double Tilt
        {
            get => _tilt;
            set => _tilt = Math.Max(-180, Math.Min(180, value));
        }

        public double Roll
        {
            get => _roll;
            set => _roll = Math.Max(-180, Math.Min(180, value));
        }

        public double PosX
        {
            get => _posX;
            set => _posX = Math.Max(-_maxPosX, Math.Min(_maxPosX, value));
        }

        public double PosY
        {
            get => _posY;
            set => _posY = Math.Max(-_maxPosY, Math.Min(_maxPosY, value));
        }

        public double PosZ
        {
            get => _posZ;
            set => _posZ = Math.Max(-_maxPosZ, Math.Min(_maxPosZ, value));
        }

        private int _zoom = 0;
        private int _focus = 0;
        private const int MAX_ZOOM_FOCUS = 4095;

        public int Zoom
        {
            get => _zoom;
            set => _zoom = Math.Max(0, Math.Min(MAX_ZOOM_FOCUS, value));
        }

        public int Focus
        {
            get => _focus;
            set => _focus = Math.Max(0, Math.Min(MAX_ZOOM_FOCUS, value));
        }
    }    
}
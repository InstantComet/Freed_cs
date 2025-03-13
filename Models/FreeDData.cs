namespace FreeDSender.Models
{
    public class FreeDData
    {
        public double Pan { get; set; } = 0;      // Pan angle
        public double Tilt { get; set; } = 0;     // Tilt angle
        public double Roll { get; set; } = 0;     // Roll angle
        public double PosZ { get; set; } = 0;     // Z position
        public double PosX { get; set; } = 0;     // X position
        public double PosY { get; set; } = 0;     // Y position
        public int Zoom { get; set; } = 0;        // Zoom value
        public int Focus { get; set; } = 0;       // Focus value
    }
}
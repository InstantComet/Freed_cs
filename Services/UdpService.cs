using System;
using System.Net;
using System.Net.Sockets;
using FreeDSender.Models;

namespace FreeDSender.Services
{
    public class UdpService
    {
        private UdpClient udpClient = new UdpClient();
        private IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);

        public event Action<byte[]> PacketSent = delegate { };

        public void Connect(string ipAddress, int port)
        {
            try
            {
                udpClient?.Close();
                udpClient = new UdpClient();
                endPoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void Send(FreeDData data)
        {
            if (udpClient == null || endPoint == null)
                throw new InvalidOperationException("UDP client not initialized");

            byte[] bytes = ConvertToFreeDBytes(data);
            udpClient.Send(bytes, bytes.Length, endPoint);
            PacketSent?.Invoke(bytes);
        }

        public void Close()
        {
            udpClient?.Close();
            udpClient = new UdpClient();
        }

        private byte[] ConvertToFreeDBytes(FreeDData data)
        {
            byte[] bytes = new byte[29];
            bytes[0] = 0xD1;  // Identifier
            bytes[1] = 0x01;  // Camera ID

            // Convert angles
            ConvertToRotation(data.Pan, -180, 180, 2, bytes);
            ConvertToRotation(data.Tilt, -180, 180, 5, bytes);
            ConvertToRotation(data.Roll, -180, 180, 8, bytes);

            // Convert positions
            ConvertToPosition(data.PosX, 11, bytes);  // X axis maps to FreeD Z position
            ConvertToPosition(data.PosZ, 14, bytes);  // Z axis maps to FreeD X position
            ConvertToPosition(data.PosY, 17, bytes);  // Y axis remains unchanged

            // Convert encoder values
            ConvertToEncoder(data.Zoom, 20, bytes);
            ConvertToEncoder(data.Focus, 23, bytes);

            // Reserved bytes
            bytes[26] = 0x00;
            bytes[27] = 0x00;

            // Calculate checksum
            byte checksum = 64;
            for (int i = 0; i < 28; i++)
            {
                checksum = (byte)(checksum - bytes[i]);
            }
            bytes[28] = checksum;

            return bytes;
        }

        private void ConvertToRotation(double angle, double minAngle, double maxAngle, int offset, byte[] bytes)
        {
            double clampedAngle = Math.Max(minAngle, Math.Min(maxAngle, angle));
            int value = (int)(clampedAngle * 32768.0);
            byte[] valueBytes = BitConverter.GetBytes(value);
            bytes[offset] = valueBytes[2];
            bytes[offset + 1] = valueBytes[1];
            bytes[offset + 2] = valueBytes[0];
        }

        private void ConvertToPosition(double pos, int offset, byte[] bytes)
        {
            int value = (int)(pos * 64.0);
            value = Math.Max(-8388608, Math.Min(8388607, value));
            byte[] valueBytes = BitConverter.GetBytes(value);
            bytes[offset] = valueBytes[2];
            bytes[offset + 1] = valueBytes[1];
            bytes[offset + 2] = valueBytes[0];
        }

        private void ConvertToEncoder(int value, int offset, byte[] bytes)
        {
            int clampedValue = Math.Max(0, Math.Min(4095, value));
            byte[] valueBytes = BitConverter.GetBytes(clampedValue);
            bytes[offset] = valueBytes[2];
            bytes[offset + 1] = valueBytes[1];
            bytes[offset + 2] = valueBytes[0];
        }
    }
}
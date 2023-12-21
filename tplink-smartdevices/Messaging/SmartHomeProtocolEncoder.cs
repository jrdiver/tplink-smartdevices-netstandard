using System;
using System.Linq;
using System.Text;

namespace TPLinkSmartDevices.Messaging
{
    internal static class SmartHomeProtocolEncoder
    {
        const byte INITIALIZATION_VECTOR = 171;

        internal static byte[] Encrypt(string data)
        {
            byte[] encryptedMessage = Encrypt(Encoding.ASCII.GetBytes(data));

            byte[] lengthBytes = BitConverter.GetBytes((UInt32)encryptedMessage.Length);
            if (BitConverter.IsLittleEndian) // this value needs to be in big-endian
                lengthBytes = lengthBytes.Reverse().ToArray();

            return lengthBytes.Concat(encryptedMessage).ToArray();
        }

        internal static byte[] Encrypt(byte[] data)
        {
            byte[] result = new byte[data.Length];
            byte key = INITIALIZATION_VECTOR; // TP-Link Constant
            for (int i = 0; i < data.Length; i++)
            {
                result[i] = (byte)(key ^ data[i]);
                key = result[i];
            }

            return result;
        }

        internal static byte[] Decrypt(byte[] data)
        {
            byte[] buf = (byte[])data.Clone();
            byte key = INITIALIZATION_VECTOR; // TP-Link Constant
            for (int i = 0; i < data.Length; i++)
            {
                byte nextKey = buf[i];
                buf[i] = (byte)(key ^ buf[i]);
                key = nextKey;
            }
            return buf;
        }
    }
}

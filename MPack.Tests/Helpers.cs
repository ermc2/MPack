using System.Security.Cryptography;
using System.Text;

namespace MPack.MPackTests
{
    public class Helpers
    {
        public static string GetString(int size)
        {
            char[] chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray();
            byte[] data = GetBytes(size);
            StringBuilder result = new(size);
            foreach (byte b in data)
            {
                result.Append(chars[b % chars.Length]);
            }
            return result.ToString();
        }

        public static byte[] GetBytes(int size)
        {
            byte[] data = new byte[1];
            RNGCryptoServiceProvider crypto = new();
            crypto.GetNonZeroBytes(data);
            data = new byte[size];
            crypto.GetNonZeroBytes(data);
            return data;
        }
    }
}
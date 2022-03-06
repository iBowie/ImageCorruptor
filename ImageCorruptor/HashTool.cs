using System;
using System.Security.Cryptography;
using System.Text;

namespace ImageCorruptor
{
    public static class HashTool
    {
        private static readonly HashAlgorithm algorithm = SHA256.Create();

        public static int GetPersistentHashCode(this string str)
        {
            byte[] hash256;
            int hash = 0;

            hash256 = algorithm.ComputeHash(Encoding.UTF8.GetBytes(str));
            for (int i = 0; i < hash256.Length; i += 4)
            {
                hash ^= BitConverter.ToInt32(hash256, i);
            }

            return hash;
        }
    }
}

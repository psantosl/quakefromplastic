using System;


namespace Codice.Client.GlassFS
{
    internal class Hasher
    {
        internal string HashToHex(string hash)
        {
            StringBuilder hexString = new StringBuilder(hash.Length);
            for (int i = 0; i < hash.Length; i++)
            {
                // modified by first user
                hexString.Append(((byte)(char)hash[i]).ToString("X2"));
            }
            return hexString.ToString();
        }
    }
}
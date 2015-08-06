using System.Net;

namespace Network
{
    internal class DNS
    {
        public DNS()
        {
        }

        internal string GetHostByName(string addr)
        {
            IPHostEntry hostInfo = GetHostByName(addr);
            // Get the IP address list that resolves to the host names contained in the
            // Alias property.
            IPAddress[] address = hostInfo.AddressList;
            // Get the alias names of the addresses in the IP address list.
            string[] alias = hostInfo.Aliases;

            return alias[0];
        }
    }
}
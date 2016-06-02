using System;
using System.Collections.Generic;
using System.Text;

namespace NannarFtp
{
    public static class Network
    {
        public static System.Net.IPAddress GetLocalIPv4(string hostNameOrAddress)
        {
            System.Net.IPAddress IPv4 = null;
            System.Net.IPAddress[] ipList = System.Net.Dns.GetHostAddresses(hostNameOrAddress);
            foreach (System.Net.IPAddress ip in ipList)
            {
                //获得IPv4
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    IPv4 = ip;
                    break;
                }
            }
            return IPv4;
        }

        public static System.Net.IPAddress[] GetLocalIPv4s(string hostNameOrAddress)
        {
            List<System.Net.IPAddress> IPv4s = new List<System.Net.IPAddress>();
            System.Net.IPAddress[] ipList = System.Net.Dns.GetHostAddresses(hostNameOrAddress);
            foreach (System.Net.IPAddress ip in ipList)
            {
                //获得IPv4
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    IPv4s.Add(ip);
                }
            }
            return IPv4s.ToArray();
        }
    }
}

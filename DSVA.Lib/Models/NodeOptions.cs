using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace DSVA.Lib.Models
{
    public class NodeOptions
    {
        private string address;
        private Lazy<string> IpAddress = new Lazy<string>(() => $"https://{GetLocalIPAddress()}:5001");
        private static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            return host.AddressList
                .FirstOrDefault(x => !x.ToString().Contains("127.0.0.1") && x.AddressFamily == AddressFamily.InterNetwork)
                ?.ToString()
                ?? throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        public string Address
        {
            get => string.IsNullOrEmpty(address) ? IpAddress.Value : address;
            set => address = value;
        }

        public string Next { get; set; }
        public int Id { get; set; }
    }
}

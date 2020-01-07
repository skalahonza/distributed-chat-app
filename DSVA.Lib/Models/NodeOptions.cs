using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace DSVA.Lib.Models
{
    public class NodeOptions
    {
        private string address;
        private Lazy<string> IpAddress = new Lazy<string>(() => $"https://{GetLocalIPAddress()}:5001");
        private static string GetLocalIPAddress()
        {
            var firstUpInterface = NetworkInterface.GetAllNetworkInterfaces()
                .OrderByDescending(c => c.Speed)
                .FirstOrDefault(c => c.NetworkInterfaceType != NetworkInterfaceType.Loopback);

            if (firstUpInterface != null)
            {
                var props = firstUpInterface.GetIPProperties();
                // get first IPV4 address assigned to this interface
                var firstIpV4Address = props.UnicastAddresses
                    .Where(c => c.Address.AddressFamily == AddressFamily.InterNetwork)
                    .Select(c => c.Address)
                    .FirstOrDefault();
                return firstIpV4Address.ToString();
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
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

using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace DropLAN;

public static class NetworkHelper
{
    public static string GetLocalIPv4()
    {
        var candidates = NetworkInterface
            .GetAllNetworkInterfaces()
            .Where(network =>
                network.OperationalStatus == OperationalStatus.Up &&
                network.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                network.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
            .Select(network => new
            {
                Network = network,
                Properties = network.GetIPProperties()
            })
            .Where(x =>
                x.Properties.GatewayAddresses.Any(gateway =>
                    gateway.Address.AddressFamily == AddressFamily.InterNetwork &&
                    !gateway.Address.Equals(IPAddress.Any) &&
                    !gateway.Address.Equals(IPAddress.None)))
            .OrderByDescending(x =>
                x.Network.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
            .ThenByDescending(x =>
                x.Network.NetworkInterfaceType == NetworkInterfaceType.Wireless80211);

        foreach (var candidate in candidates)
        {
            var address = candidate.Properties.UnicastAddresses
                .FirstOrDefault(x =>
                    x.Address.AddressFamily == AddressFamily.InterNetwork &&
                    !IPAddress.IsLoopback(x.Address));

            if (address != null)
                return address.Address.ToString();
        }

        return "127.0.0.1";
    }
}

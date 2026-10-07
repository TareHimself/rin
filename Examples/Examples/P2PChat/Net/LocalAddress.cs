using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Examples.P2PChat.Net;

public static class LocalAddress
{
    public static string Find()
    {
        foreach (var network in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (network.OperationalStatus != OperationalStatus.Up ||
                network.NetworkInterfaceType is NetworkInterfaceType.Loopback) continue;

            foreach (var address in network.GetIPProperties().UnicastAddresses)
                if (address.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(address.Address))
                    return address.Address.ToString();
        }

        return IPAddress.Loopback.ToString();
    }
}

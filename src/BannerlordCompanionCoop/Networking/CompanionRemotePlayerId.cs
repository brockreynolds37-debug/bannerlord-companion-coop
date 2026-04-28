using System;
using TaleWorlds.MountAndBlade;

namespace BannerlordCompanionCoop.Networking;

public static class CompanionRemotePlayerId
{
    public static string FromNetworkPeer(NetworkCommunicator networkPeer)
    {
        if (networkPeer is null)
        {
            throw new ArgumentNullException(nameof(networkPeer));
        }

        if (networkPeer.VirtualPlayer is not null && networkPeer.VirtualPlayer.Id.IsValid)
        {
            return networkPeer.VirtualPlayer.Id.ToString();
        }

        if (!string.IsNullOrWhiteSpace(networkPeer.UserName))
        {
            return networkPeer.UserName;
        }

        return $"peer_{networkPeer.Index}";
    }
}

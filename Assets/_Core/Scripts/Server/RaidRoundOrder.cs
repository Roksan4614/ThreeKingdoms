using System.Numerics;

namespace ThreeKingdoms.Client.Server
{
    public static class RaidRoundOrder
    {
        public static bool Allows(string currentRaidId, string incomingRaidId, string joinedRaidId)
        {
            if (!string.IsNullOrEmpty(joinedRaidId)) return incomingRaidId == joinedRaidId;
            return string.IsNullOrEmpty(currentRaidId)
                || BigInteger.Parse(incomingRaidId) >= BigInteger.Parse(currentRaidId);
        }
    }
}

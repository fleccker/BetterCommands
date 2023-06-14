using helpers.Extensions;
using helpers.Results;

using System;
using System.Linq;
using System.Net;

namespace BetterCommands.Parsing.Parsers
{
    public class ReferenceHubParser : ICommandArgumentParser
    {
        public IResult<object> Parse(string value, Type type)
        {
            ReferenceHub result = null;

            var players = ReferenceHub.AllHubs.Where(player => player.Mode is ClientInstanceMode.ReadyClient 
                                    && !string.IsNullOrWhiteSpace(player.characterClassManager.UserId) 
                                    && !string.IsNullOrWhiteSpace(player.nicknameSync.Network_myNickSync) 
                                    && player.connectionToClient != null);

            if (int.TryParse(value, out var playerId))
            {
                if (players.TryGetFirst(player => player.PlayerId == playerId, out result))
                    return new SuccessResult(result);
            }

            if (value.StartsWith("nid"))
            {
                value = value.Remove("nid", "nid:");

                if (uint.TryParse(value, out var netId))
                {
                    if (players.TryGetFirst(player => player.netId == netId, out result))
                        return new SuccessResult(result);
                }
            }

            if (IPAddress.TryParse(value, out var ip))
            {
                if (players.TryGetFirst(player => player.connectionToClient.address == ip.ToString(), out result))
                    return new SuccessResult(result);
            }

            if (players.TryGetFirst(player => player.characterClassManager.UserId == value || player.characterClassManager.UserId.StartsWith(value), out result))
                return new SuccessResult(result);

            players = players.Where(player => player.nicknameSync.Network_myNickSync.GetSimilarity(value) > 0.0);
            players = players.OrderByDescending(player => player.nicknameSync.Network_myNickSync.GetSimilarity(value));

            result = players.FirstOrDefault();

            if (result is null)
                return new ErrorResult($"Failed to find a player by string: {value}");
            else
                return new SuccessResult(result);
        }
    }
}
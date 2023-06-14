using helpers.Extensions;
using helpers.Results;

using PluginAPI.Core;
using PluginAPI.Core.Interfaces;

using System;
using System.Linq;
using System.Net;

namespace BetterCommands.Parsing.Parsers
{
    public class PlayerParser : ICommandArgumentParser
    {
        public IResult<object> Parse(string value, Type type)
        {
            IResult<object> ToResult(Player player)
            {
                if (player is null)
                    return new ErrorResult($"Failed to find a target player by string: {value}");

                if (type == typeof(Player) || type == typeof(IPlayer))
                    return new SuccessResult(player);

                if (!FactoryManager.FactoryTypes.TryGetValue(type, out var factoryType))
                    return new ErrorResult($"Failed to find a player factory for player type: {type.FullName}");

                if (!FactoryManager.PlayerFactories.TryGetValue(factoryType, out var factory))
                    return new ErrorResult($"Failed to find a player factory by type: {factoryType.FullName}");

                var factoryResult = factory.GetOrAdd(player.ReferenceHub);

                if (factoryResult is null)
                    return new ErrorResult($"Failed to fetch a player from factory: {factoryType.FullName}");

                return new SuccessResult(factoryResult);
            }

            Player result = null;

            var players = Player.GetPlayers().Where(player => !player.IsServer);

            if (int.TryParse(value, out var playerId))
            {
                if (players.TryGetFirst(player => player.PlayerId == playerId, out result))
                    return ToResult(result);
            }

            if (value.StartsWith("nid"))
            {
                value = value.Remove("nid", "nid:");

                if (uint.TryParse(value, out var netId))
                {
                    if (players.TryGetFirst(player => player.NetworkId == netId, out result))
                        return ToResult(result);
                }
            }

            if (IPAddress.TryParse(value, out var ip))
            {
                if (players.TryGetFirst(player => player.IpAddress == ip.ToString(), out result))
                    return ToResult(result);
            }

            if (players.TryGetFirst(player => player.UserId == value || player.UserId.StartsWith(value), out result))
                return ToResult(result);

            players = players.Where(player => player.Nickname.GetSimilarity(value) > 0.0);
            players = players.OrderByDescending(player => player.Nickname.GetSimilarity(value));

            result = players.FirstOrDefault();

            if (result is null)
                return new ErrorResult($"Failed to find a player by string: {value}");
            else
                return ToResult(result);
        }
    }
}
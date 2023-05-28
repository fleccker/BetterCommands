using BetterCommands.Results;
using BetterCommands.Support.Compendium;

using PluginAPI.Core;
using PluginAPI.Core.Interfaces;

using System;
using System.Linq;
using System.Net;

namespace BetterCommands.Parsing.Parsers
{
    public class PlayerParser : ICommandArgumentParser
    {
        public IResult Parse(string value, Type type)
        {
            var loweredValue = value.ToLower();

            if (type == typeof(IPlayer)) type = typeof(Player);

            Player player = null;
            
            if (int.TryParse(value, out var pId))
            {
                if (!Player.TryGet(pId, out player)) return new ErrorResult($"Failed to fetch a valid player by ID: {pId}");
                else return new SuccessResult(MakeCorrectType(player, type));
            }

            if (value.StartsWith("nid:") || value.StartsWith("netid:"))
            {
                var clearValue = value
                    .Replace("nid:", "")
                    .Replace("netid:", "")
                    .Trim();
                
                if (uint.TryParse(clearValue, out var netId))
                {
                    if (!Player.TryGet(netId, out player)) return new ErrorResult($"Failed to fetch a valid player by Network ID: {netId}");
                    else return new SuccessResult(MakeCorrectType(player, type));
                }
            }

            if (IPAddress.TryParse(value, out var ip))
            {
                foreach (var hub in ReferenceHub.AllHubs)
                {
                    if (hub.Mode != ClientInstanceMode.ReadyClient) continue;
                    if (hub.connectionToClient.address.StartsWith(ip.ToString())) player = Player.Get(hub);
                    if (player != null) break;
                }

                if (player != null) return new SuccessResult(MakeCorrectType(player, type));
                else return new ErrorResult($"Failed to fetch a valid player by IP: {ip}");
            }

            if (value.Where(x => x == '@').Count() == 1)
            {
                var splitId = value.Split('@');
                var clearId = splitId[0];

                if (!ulong.TryParse(clearId, out var clearIdNumber)) return new ErrorResult($"Failed to fetch a valid player by User ID: {clearId} (failed to parse)");
                if (!Player.TryGet($"{clearId}@{splitId[1].ToLower()}", out player)) return new ErrorResult($"Failed to fetch a valid player by User ID: {clearId}");
                else return new SuccessResult(MakeCorrectType(player, type));
            }
            else if (ulong.TryParse(value, out var clearId))
            {
                var hub = ReferenceHub.AllHubs.Where(x => x.Mode is ClientInstanceMode.ReadyClient).FirstOrDefault(y => y.characterClassManager.UserId.StartsWith(value));
                if (hub != null)
                {
                    if (!Player.TryGet(hub, out player)) return new ErrorResult($"Failed to fetch a valid player by User ID: {clearId} (invalid player object)");
                    else return new SuccessResult(MakeCorrectType(player, type));
                }
            }

            if (CompendiumSupport.IsAvailable)
            {
                if (CompendiumSupport.TryGetIpById(value, out var ipStr))
                {
                    var hub = ReferenceHub.AllHubs.Where(x => x.Mode is ClientInstanceMode.ReadyClient).FirstOrDefault(y => y.connectionToClient.address == ipStr);
                    if (hub != null)
                    {
                        if (!Player.TryGet(hub, out player)) return new ErrorResult($"Failed to fetch a valid player by unique ID: {value} (invalid player object)");
                        else return new SuccessResult(MakeCorrectType(player, type));
                    }
                }
            }


            var nicknames = ReferenceHub.AllHubs.Where(x => x.Mode is ClientInstanceMode.ReadyClient).Select(x => x.nicknameSync.Network_myNickSync.ToLower()).ToList();
            var orderedNicknames = ParsingUtils.OrderByScore(nicknames, loweredValue);

            if (!orderedNicknames.Any()) return new ErrorResult($"Failed to fetch a valid player by nickname: {value}");
            else return new SuccessResult(MakeCorrectType(Player.Get(ReferenceHub.AllHubs.First(x => x.nicknameSync.Network_myNickSync.ToLower() == orderedNicknames.First())), type));
        }

        private IPlayer MakeCorrectType(Player player, Type type)
        {
            if (type == typeof(Player)) return player;

            if (!FactoryManager.FactoryTypes.TryGetValue(type, out var factoryType)) return player;
            if (!FactoryManager.PlayerFactories.TryGetValue(type, out var factory)) return player;
            if (factory is null) return player;
                 
            return factory.GetOrAdd(player.ReferenceHub);
        }
    }
}
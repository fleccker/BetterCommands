using HarmonyLib;

using RemoteAdmin;
using RemoteAdmin.Communication;

using System.Linq;
using System;

using Utils.NonAllocLINQ;

using PluginAPI.Events;

using BetterCommands.Management;

namespace BetterCommands.Patches
{
    [HarmonyPatch(typeof(CommandProcessor), nameof(CommandProcessor.ProcessQuery))]
    public static class ExecuteCommandsPatch
    {
        public static bool Prefix(string q, CommandSender sender, ref string __result)
        {
            if (q.StartsWith("$"))
            {
                var split = q.Remove(0, 1).Split(' ');
                if (split.Length <= 0)
                {
                    __result = null;
                    return false;
                }

                if (!int.TryParse(split[0], out var actionKey))
                {
                    __result = null;
                    return false;
                }

                if (CommunicationProcessor.ServerCommunication.TryGetValue(actionKey, out var comms)) comms.ReceiveData(sender, string.Join(" ", split.Skip(1)));

                __result = null;
                return false;
            }

            var player = sender as PlayerCommandSender;
            if (q.StartsWith("@"))
            {
                if (!CommandProcessor.CheckPermissions(sender, "Admin Chat", PlayerPermissions.AdminChat))
                {
                    player?.ReferenceHub?.queryProcessor?.TargetAdminChatAccessDenied(player?.ReferenceHub?.connectionToClient);

                    __result = "Your current permissions do not allow you to access Admin Chat!";
                    return false;
                }

                q += $"~{sender.Nickname}";

                ReferenceHub.AllHubs.ForEach(x =>
                {
                    if (x.Mode != ClientInstanceMode.ReadyClient) return;
                    if (!(x.serverRoles.AdminChatPerms || x.serverRoles.RaEverywhere)) return;

                    x.queryProcessor?.TargetReply(x.queryProcessor?.connectionToClient, q, true, false, string.Empty);
                });

                __result = null;
                return false;
            }
            else
            {
                var split = q.Trim().Split(QueryProcessor.SpaceArray, 512, StringSplitOptions.RemoveEmptyEntries);

                if (!EventManager.ExecuteEvent(PluginAPI.Enums.ServerEventType.RemoteAdminCommand, sender, split[0], split.Skip(1).ToArray()))
                {
                    __result = null;
                    return false;
                }

                if (CommandManager.TryExecute(string.Join(" ", split), player.ReferenceHub, CommandType.RemoteAdmin, out __result))
                {
                    sender.RaReply(__result, !__result.Contains("Command execution failed"), true, string.Empty);
                    return false;
                }

                if (CommandProcessor.RemoteAdminCommandHandler.TryGetCommand(split[0], out var command))
                {
                    try
                    {
                        var success = command.Execute(split.Segment(1), sender, out var response);
                        if (!EventManager.ExecuteEvent(PluginAPI.Enums.ServerEventType.RemoteAdminCommandExecuted, sender, split[0], split.Skip(1).ToArray(), success, response))
                        {
                            __result = null;
                            return false;
                        }

                        if (!string.IsNullOrWhiteSpace(response)) sender.RaReply($"{split[0].ToUpper()}#{response}", success, true, string.Empty);

                        __result = response;
                        return false;
                    }
                    catch (Exception ex)
                    {
                        __result = $"Command execution failed!\n{ex}";
                        return false;
                    }
                }
                else
                {
                    if (!EventManager.ExecuteEvent(PluginAPI.Enums.ServerEventType.RemoteAdminCommandExecuted, sender, split[0], split.Skip(1).ToArray(), false, "Unknown command!"))
                    {
                        __result = null;
                        return false;
                    }

                    sender.RaReply($"SYS#Unknown command!", false, true, string.Empty);

                    __result = "Unknown command!";
                    return false;
                }
            }
        }
    }
}

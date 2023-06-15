using HarmonyLib;

using RemoteAdmin;
using RemoteAdmin.Communication;

using System.Linq;
using System;

using Utils.NonAllocLINQ;

using PluginAPI.Events;

using BetterCommands.Management;

using helpers.Extensions;

using UnityEngine;

namespace BetterCommands.Patches
{
    [HarmonyPatch(typeof(GameCore.Console), nameof(GameCore.Console.TypeCommand))]
    public static class TypeCommandPatch
    {
        public static bool Prefix(GameCore.Console __instance, string cmd, ref string __result, CommandSender sender = null)
        {
            if (sender is null)
                sender = ServerConsole.Scs;

            var flag = cmd.StartsWith("@");

            if ((cmd.StartsWith("/") || flag) && cmd.Length > 1)
            {
                var str = flag ? cmd : cmd.Substring(1);

                if (!flag)
                {
                    str = str.TrimStart('$');

                    if (string.IsNullOrWhiteSpace(str))
                    {
                        if (sender != null)
                        {
                            sender.Print("Command can't be empty!", ConsoleColor.Red);
                        }

                        __result = "Command can't be empty!";
                        return false;
                    }
                }

                __result = CommandProcessor.ProcessQuery(str, sender);
                return false;
            }

            var array = cmd.Trim().Split(QueryProcessor.SpaceArray, 512, StringSplitOptions.RemoveEmptyEntries);

            if (!EventManager.ExecuteEvent(PluginAPI.Enums.ServerEventType.ConsoleCommand, sender, array[0], array.Skip(1).ToArray()))
            {
                __result = null;
                return false;
            }

            if (CommandManager.TryExecute(string.Join(" ", array), ReferenceHub.HostHub, CommandType.GameConsole, out var response1))
            {
                __result = response1.RemoveHtmlTags();
                return false;
            }

            cmd = array[0];

            if (__instance.ConsoleCommandHandler.TryGetCommand(cmd, out var command))
            {
                var result = "";
                var success = false;

                try
                {
                    success = command.Execute(array.Segment(1), sender, out result);

                    if (!EventManager.ExecuteEvent(PluginAPI.Enums.ServerEventType.ConsoleCommandExecuted, sender, array[0], array.Skip(1).ToArray(), success, result))
                    {
                        result = null;
                        return false;
                    }

                    if (sender != null)
                    {
                        sender.Print(result, success ? ConsoleColor.Green : ConsoleColor.Red);
                    }
                }
                catch (Exception ex)
                {
                    result = $"Command execution failed! Error:\n{ex}";

                    if (!EventManager.ExecuteEvent(PluginAPI.Enums.ServerEventType.ConsoleCommandExecuted, sender, array[0], array.Skip(1).ToArray(), false, result))
                    {
                        __result = null;
                        return false;
                    }

                    if (sender != null)
                    {
                        sender.Print(result, ConsoleColor.Red);
                    }
                }

                __result = result;
                return false;
            }

            var response = $"Command {cmd} does not exist!";

            if (!EventManager.ExecuteEvent(PluginAPI.Enums.ServerEventType.ConsoleCommandExecuted, sender, array[0], array.Skip(1).ToArray(), false, response))
            {
                __result = null;
                return false;
            }

            if (sender != null)
            {
                sender.Print(response, ConsoleColor.DarkYellow, new Color(255, 180, 0, 255));
            }

            __result = response;
            return false;
        }
    }

    [HarmonyPatch(typeof(QueryProcessor), nameof(QueryProcessor.ProcessGameConsoleQuery))]
    public static class ProcessGameConsoleQueryPatch
    {
        public static bool Prefix(QueryProcessor __instance, string query)
        {
            var array = query.Trim().Split(QueryProcessor.SpaceArray, 512, StringSplitOptions.RemoveEmptyEntries);

            if (!EventManager.ExecuteEvent(PluginAPI.Enums.ServerEventType.PlayerGameConsoleCommand, __instance._hub, array[0], array.Skip(1).ToArray()))
                return false;

            if (CommandManager.TryExecute(string.Join(" ", array), __instance._hub, CommandType.PlayerConsole, out var response1))
            {
                __instance.GCT.SendToClient(__instance.connectionToClient, $"SYSTEM#{response1.RemoveHtmlTags()}", "yellow");
                return false;
            }

            if (QueryProcessor.DotCommandHandler.TryGetCommand(array[0], out var command))
            {
                try
                {
                    var success = command.Execute(array.Segment(1), __instance._sender, out var response);

                    if (!EventManager.ExecuteEvent(PluginAPI.Enums.ServerEventType.PlayerGameConsoleCommandExecuted, __instance._hub, array[0], array.Skip(1).ToArray(), success, response))
                        return false;

                    __instance.GCT.SendToClient(__instance.connectionToClient, $"{array[0].ToUpper()}#{response.RemoveHtmlTags()}", "");
                }
                catch (Exception ex)
                {
                    var response = $"Command execution failed! Error:\n{ex}";

                    if (!EventManager.ExecuteEvent(PluginAPI.Enums.ServerEventType.PlayerGameConsoleCommandExecuted, __instance._hub, array[0], array.Skip(1).ToArray(), false, response))
                        return false;

                    __instance.GCT.SendToClient(__instance.connectionToClient, $"{array[0].ToUpper()}#{response}", "");
                }

                return false;
            }

            var response2 = "Command not found.";

            if (!EventManager.ExecuteEvent(PluginAPI.Enums.ServerEventType.PlayerGameConsoleCommandExecuted, __instance._hub, array[0], array.Skip(1).ToArray(), false, response2))
                return false;

            __instance.GCT.SendToClient(__instance.connectionToClient, $"SYSTEM#{response2}", "red");
            return false;
        }
    }

    [HarmonyPatch(typeof(CommandProcessor), nameof(CommandProcessor.ProcessQuery))]
    public static class ProcessQueryPatch
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

                if (player is null)
                {
                    if (CommandManager.TryExecute(string.Join(" ", split), ReferenceHub.HostHub, CommandType.RemoteAdmin, out __result))
                    {
                        __result = __result.RemoveHtmlTags();
                        sender.RaReply(__result, true, true, string.Empty);
                        return false;
                    }
                }
                else
                {
                    if (CommandManager.TryExecute(string.Join(" ", split), player.ReferenceHub, CommandType.RemoteAdmin, out __result))
                    {
                        sender.RaReply(__result, true, true, string.Empty);
                        return false;
                    }
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

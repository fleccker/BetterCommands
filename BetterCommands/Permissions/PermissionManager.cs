using helpers.Extensions;

using System.Collections.Generic;
using System.Linq;

using PluginAPI.Core.Interfaces;

namespace BetterCommands.Permissions
{
    public static class PermissionManager
    {
        public static PermissionConfig Config => Loader.Config.Permissions;

        public static bool TryGetLevel(ReferenceHub hub, out PermissionLevel permission)
        {
            if (Config.LevelsByPlayer.TryGetValue(hub.characterClassManager.UserId, out permission)) 
                return true;

            if (Config.LevelsByPlayer.TryGetValue(hub.connectionToClient.address, out permission)) 
                return true;
            if (PermissionUtils.TryGetGroupKey(hub, out var key) && Config.LevelsByPlayer.TryGetValue(key, out permission)) 
                return true;

            if (PermissionUtils.TryGetClearId(hub, out var clear) && Config.LevelsByPlayer.TryGetValue(clear, out permission)) 
                return true;

            return false;
        }

        public static bool TryGetNodes(ReferenceHub hub, out string[] nodes)
        {
            if (Config.NodesByPlayer.TryGetValue(hub.characterClassManager.UserId, out nodes)) 
                return true;

            if (Config.NodesByPlayer.TryGetValue(hub.connectionToClient.address, out nodes)) 
                return true;
            if (PermissionUtils.TryGetGroupKey(hub, out var key) && Config.NodesByPlayer.TryGetValue(key, out nodes)) 
                return true;

            if (PermissionUtils.TryGetClearId(hub, out var clear) && Config.NodesByPlayer.TryGetValue(clear, out nodes)) 
                return true;

            if (!TryGetLevel(hub, out var level)) 
                return false;

            if (Config.NodesByLevel.TryGetValue(level, out nodes)) 
                return true;

            return false;
        }

        public static void AssignLevel(string target, PermissionLevel level)
        {
            Config.LevelsByPlayer[target] = level;
            Loader.SaveConfig();
        }

        public static void RemoveLevel(string target)
        {
            if (Config.LevelsByPlayer.Remove(target))
            {
                Loader.SaveConfig();
            }
        }

        public static void AddNodes(string target, params string[] nodes)
        {
            var nodeList = new List<string>();

            if (Config.NodesByPlayer.TryGetValue(target, out var activeNodes))
            {
                nodeList.AddRange(nodes);
            }

            nodeList.AddRange(nodes.Where(node => !nodeList.Contains(node)));
            nodeList = nodeList.OrderByDescending(node => node).ToList();

            Config.NodesByPlayer[target] = nodeList.ToArray();

            Loader.SaveConfig();
        }

        public static void AddNodes(PermissionLevel level, params string[] nodes)
        {
            var nodeList = new List<string>();

            if (Config.NodesByLevel.TryGetValue(level, out var activeNodes))
            {
                nodeList.AddRange(nodes);
            }

            nodeList.AddRange(nodes.Where(node => !nodeList.Contains(node)));
            nodeList = nodeList.OrderByDescending(node => node).ToList();

            Config.NodesByLevel[level] = nodeList.ToArray();

            Loader.SaveConfig();
        }

        public static void RemoveNodes(string target, params string[] nodes)
        {
            var nodeList = new List<string>();

            if (Config.NodesByPlayer.TryGetValue(target, out var activeNodes))
            {
                nodeList.AddRange(nodes);
            }

            nodes.ForEach(node => nodeList.Remove(node));
            nodeList = nodeList.OrderByDescending(node => node).ToList();

            Config.NodesByPlayer[target] = nodeList.ToArray();

            Loader.SaveConfig();
        }

        public static void RemoveNodes(PermissionLevel level, params string[] nodes)
        {
            var nodeList = new List<string>();

            if (Config.NodesByLevel.TryGetValue(level, out var activeNodes))
            {
                nodeList.AddRange(nodes);
            }

            nodes.ForEach(node => nodeList.Remove(node));
            nodeList = nodeList.OrderByDescending(node => node).ToList();

            Config.NodesByLevel[level] = nodeList.ToArray();

            Loader.SaveConfig();
        }

        [Command("perms_add_nodes", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [Permission(PermissionLevel.Administrator)]
        private static string AddNodesCommand(IPlayer sender, PermissionLevel level, string[] nodes)
        {
            AddNodes(level, nodes);
            return $"Added nodes to {level}: {string.Join(",", nodes)}";
        }

        [Command("perms_remove_nodes", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [Permission(PermissionLevel.Administrator)]
        private static string RemoveNodesCommand(IPlayer sender, PermissionLevel level, string[] nodes)
        {
            RemoveNodes(level, nodes);
            return $"Removed nodes from {level}: {string.Join(",", nodes)}";
        }

        [Command("perms_assign_level", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [Permission(PermissionLevel.Administrator)]
        private static string AssignLevelCommand(IPlayer sender, PermissionLevel level, string target)
        {
            AssignLevel(target, level);
            return $"Added level {level} to target: {target}";
        }

        [Command("perms_remove_level", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [Permission(PermissionLevel.Administrator)]
        private static string RemoveLevelCommand(IPlayer sender, string target)
        {
            RemoveLevel(target);
            return $"Removed level from target: {target}";
        }
    }
}
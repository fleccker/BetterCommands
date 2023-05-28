using BetterCommands.Support.Compendium;

namespace BetterCommands.Permissions
{
    public static class PermissionManager
    {
        public static PermissionConfig Config => Loader.Config.Permissions;

        public static bool TryGetLevel(ReferenceHub hub, out PermissionLevel permission)
        {
            if (Config.LevelsByPlayer.TryGetValue(hub.characterClassManager.UserId, out permission)) return true;
            if (Config.LevelsByPlayer.TryGetValue(hub.connectionToClient.address, out permission)) return true;
            if (PermissionUtils.TryGetGroupKey(hub, out var key) && Config.LevelsByPlayer.TryGetValue(key, out permission)) return true;
            if (PermissionUtils.TryGetClearId(hub, out var clear) && Config.LevelsByPlayer.TryGetValue(clear, out permission)) return true;
            if (CompendiumSupport.TryGetUniqueId(hub, out var id) && Config.LevelsByPlayer.TryGetValue(id, out permission)) return true;

            return false;
        }

        public static bool TryGetNodes(ReferenceHub hub, out string[] nodes)
        {
            if (Config.NodesByPlayer.TryGetValue(hub.characterClassManager.UserId, out nodes)) return true;
            if (Config.NodesByPlayer.TryGetValue(hub.connectionToClient.address, out nodes)) return true;
            if (PermissionUtils.TryGetGroupKey(hub, out var key) && Config.NodesByPlayer.TryGetValue(key, out nodes)) return true;
            if (PermissionUtils.TryGetClearId(hub, out var clear) && Config.NodesByPlayer.TryGetValue(clear, out nodes)) return true;
            if (CompendiumSupport.TryGetUniqueId(hub, out var id) && Config.NodesByPlayer.TryGetValue(id, out nodes)) return true;

            if (!TryGetLevel(hub, out var level)) return false;

            if (Config.NodesByLevel.TryGetValue(level, out nodes)) return true;

            return false;
        }
    }
}
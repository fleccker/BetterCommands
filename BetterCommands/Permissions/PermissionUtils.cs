using System.Linq;

namespace BetterCommands.Permissions
{
    public static class PermissionUtils
    {
        public static bool TryGetGroupKey(ReferenceHub hub, out string groupKey)
        {
            groupKey = null;

            if (ServerStatic.PermissionsHandler is null) return false;
            if (ServerStatic.PermissionsHandler._members.TryGetValue(hub.characterClassManager.UserId, out groupKey)) return true;

            return false;
        }

        public static bool TryGetClearId(ReferenceHub hub, out string clearId)
        {
            var split = hub.characterClassManager.UserId.Split('@');

            if (split.Length > 1) clearId = split[0];
            else clearId = null;

            return !string.IsNullOrWhiteSpace(clearId);
        }

        public static bool TryValidateNodes(string[] nodes, string[] availableNodes, PermissionNodeMode nodeMode)
        {
            if (nodeMode is PermissionNodeMode.AllOf)
            {
                if (nodes.Length != availableNodes.Length) return false;

                for (int i = 0; i < nodes.Length; i++)
                {
                    if (!ContainsNode(nodes[i], availableNodes)) return false;
                }

                return true;
            }
            else
            {
                if (nodes.Any(x => ContainsNode(x, availableNodes))) return true;
                return false;
            }
        }

        public static bool ContainsNode(string node, string[] availableNodes)
        {
            if (availableNodes.Any(x => x == "*")) return true;
            if (availableNodes.Any(x => x == node)) return true;

            var split = node.Split('.');
            if (split.Length > 1)
            {
                for (int i = 0; i < split.Length; i++)
                {
                    var curNode = split[i];
                    if (availableNodes.Any(x => x == $"{curNode}.*")) return true;
                    if (availableNodes.Any(x => x == $"*.{curNode}")) return true;
                }
            }

            return false;
        }
    }
}

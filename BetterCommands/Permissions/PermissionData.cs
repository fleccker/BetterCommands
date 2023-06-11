using helpers.Results;

namespace BetterCommands.Permissions
{
    public class PermissionData
    {
        public string[] RequiredNodes { get; }

        public PermissionNodeMode NodeMode { get; }
        public PermissionLevel? RequiredLevel { get; }

        public IResult<object> Validate(ReferenceHub player)
        {
            if (RequiredNodes != null && RequiredNodes.Length > 0)
            {
                if (PermissionManager.TryGetNodes(player, out var nodes))
                {
                    if (PermissionUtils.TryValidateNodes(RequiredNodes, nodes, NodeMode)) 
                        return new SuccessResult(null);
                    else 
                        return new ErrorResult($"Missing permissions!\n{ToString()}");
                }
                else 
                    return new ErrorResult($"Missing permissions!\n{ToString()}");
            }
            else
            {
                if (RequiredLevel.HasValue)
                {
                    if (RequiredLevel.Value != PermissionLevel.None)
                    {
                        if (PermissionManager.TryGetLevel(player, out var permissionLevel))
                        {
                            if (!permissionLevel.HasFlag(RequiredLevel.Value)) 
                                return new ErrorResult($"Missing permissions!\n{ToString()}");
                        }
                        else 
                            return new ErrorResult($"Missing permissions!\n{ToString()}");
                    }
                }
            }

            return new SuccessResult(null);
        }

        public PermissionData(string[] reqNodes, PermissionNodeMode permissionNodeMode, PermissionLevel? permissionLevel = null)
        {
            RequiredNodes = reqNodes;
            NodeMode = permissionNodeMode;
            RequiredLevel = permissionLevel;
        }

        public override string ToString()
        {
            var str = "";

            if (RequiredNodes != null && RequiredNodes.Length > 0)
            {
                str += $"Nodes ({(NodeMode is PermissionNodeMode.AllOf ? "all of" : "any of")}): ";
                str += string.Join(", ", RequiredNodes);
            }

            if (RequiredLevel.HasValue)
            {
                if (str != "") 
                    str += $"\n";

                str += $"Level: {RequiredLevel.Value}";
            }

            return str;
        }
    }
}
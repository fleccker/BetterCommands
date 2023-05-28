using System;

namespace BetterCommands.Permissions
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class PermissionAttribute : Attribute
    {
        public string[] RequiredNodes { get; }
        public PermissionLevel? RequiredLevel { get; }
        public PermissionNodeMode NodeMode { get; }

        public PermissionAttribute(PermissionNodeMode nodeMode, params string[] requiredNodes)
        {
            RequiredNodes = requiredNodes;
            NodeMode = nodeMode;
        }

        public PermissionAttribute(PermissionLevel level)
        {
            RequiredLevel = level;
        }
    }
}
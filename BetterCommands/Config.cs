using BetterCommands.Permissions;

namespace BetterCommands
{
    public class Config
    {
        [System.ComponentModel.Description("Whether or not to show debug messages.")]
        public bool IsDebugEnabled { get; set; }

        [System.ComponentModel.Description("Default permissions.")]
        public PermissionConfig Permissions { get; set; } = new PermissionConfig();
    }
}
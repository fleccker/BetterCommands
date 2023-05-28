using System;

namespace BetterCommands.Management
{
    [Flags]
    public enum CommandType
    {
        RemoteAdmin,
        PlayerConsole,
        GameConsole
    }
}
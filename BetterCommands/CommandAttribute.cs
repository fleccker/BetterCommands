using BetterCommands.Management;

using PluginAPI.Core;

using System;

namespace BetterCommands
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class CommandAttribute : Attribute
    {
        public string Name { get; }
        public bool IsHidden { get; }
        public CommandType Types { get; }

        public CommandAttribute(object name, params CommandType[] types)
        {
            Name = name?.ToString() ?? null;
            IsHidden = false;
            Types = default;

            foreach (var type in types) Types &= type;
            Log.Debug($"Compiled attribute: {Name};{IsHidden};{Types}", "Command Manager");
        }

        public CommandAttribute(object name, bool hidden, params CommandType[] types)
        {
            Name = name?.ToString() ?? null;
            IsHidden = hidden;
            Types = default;

            foreach (var type in types) Types &= type;
            Log.Debug($"Compiled attribute: {Name};{IsHidden};{Types}", "Command Manager");
        }
    }
}
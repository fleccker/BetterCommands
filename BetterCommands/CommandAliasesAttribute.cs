using System;
using System.Linq;

namespace BetterCommands
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class CommandAliasesAttribute : Attribute
    {
        public string[] Aliases { get; }

        public CommandAliasesAttribute(params object[] aliases) => Aliases = aliases?.Select(x => x?.ToString() ?? null)?.ToArray() ?? Array.Empty<string>();
    }
}
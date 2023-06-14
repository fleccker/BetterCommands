using BetterCommands.Management;

using helpers.Values;

using System;

namespace BetterCommands
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class CommandAttribute : Attribute
    {
        private FlagEnumValue<CommandType> m_Type = new FlagEnumValue<CommandType>();

        public string Name { get; }
        public bool IsHidden { get; }
        public CommandType Types => m_Type.Flags;

        public CommandAttribute(object name, params CommandType[] types)
        {
            Name = name?.ToString() ?? null;
            IsHidden = false;

            foreach (var type in types)
                m_Type.WithFlag(type);

            if (m_Type.HasFlag(CommandType.RemoteAdmin) && !types.Contains(CommandType.RemoteAdmin))
                m_Type.WithoutFlag(CommandType.RemoteAdmin);
        }

        public CommandAttribute(object name, bool hidden, params CommandType[] types)
        {
            Name = name?.ToString() ?? null;
            IsHidden = hidden;

            foreach (var type in types)
                m_Type.WithFlag(type);

            if (m_Type.HasFlag(CommandType.RemoteAdmin) && !types.Contains(CommandType.RemoteAdmin))
                m_Type.WithoutFlag(CommandType.RemoteAdmin);
        }
    }
}
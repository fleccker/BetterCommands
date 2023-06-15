using BetterCommands.Management;

using System;

namespace BetterCommands
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class CommandAttribute : Attribute
    {
        private CommandType[] m_Types;

        public string Name { get; }

        public bool IsHidden { get; }

        public CommandType[] Types => m_Types;

        public CommandAttribute(object name, params CommandType[] types)
        {
            Name = name?.ToString() ?? null;
            IsHidden = false;

            m_Types = types;

            if (m_Types is null)
                m_Types = Array.Empty<CommandType>();
        }

        public CommandAttribute(object name, bool hidden, params CommandType[] types)
        {
            Name = name?.ToString() ?? null;
            IsHidden = hidden;

            m_Types = types;

            if (m_Types is null)
                m_Types = Array.Empty<CommandType>();
        }
    }
}
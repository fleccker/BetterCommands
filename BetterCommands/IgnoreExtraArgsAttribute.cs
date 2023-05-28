using System;

namespace BetterCommands
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class IgnoreExtraArgsAttribute : Attribute { }
}
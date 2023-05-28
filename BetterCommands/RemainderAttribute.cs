using System;

namespace BetterCommands
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = true, Inherited = false)]
    public class RemainderAttribute : Attribute { }
}
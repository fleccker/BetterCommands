using System;

namespace BetterCommands.Conditions
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public class ConditionAttribute : Attribute
    {
        public ConditionFlag Flags { get; }
        public object ConditionObject { get; }

        public ConditionAttribute(object conditionObject, params ConditionFlag[] flags)
        {
            ConditionObject = conditionObject;
            Flags = default;

            foreach (var flag in flags) 
                Flags &= flag;
        }

        public ConditionAttribute(params ConditionFlag[] flags)
        {
            Flags = default;
            foreach (var flag in flags) 
                Flags &= flag;
        }
    }
}
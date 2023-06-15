using System;

namespace BetterCommands
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class ValueRestrictionAttribute : Attribute
    {
        private ValueRestrictionMode m_Mode;
        private object[] m_Values;

        public ValueRestrictionAttribute(ValueRestrictionMode mode, params object[] values)
        {
            m_Mode = mode;
            m_Values = values;
        }

        public ValueRestrictionMode GetMode() => m_Mode;
        public object[] GetValues() => m_Values ?? Array.Empty<object>();
    }
}
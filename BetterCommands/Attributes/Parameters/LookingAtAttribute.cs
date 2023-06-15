using System;

using UnityEngine;

namespace BetterCommands
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = true, Inherited = false)]
    public class LookingAtAttribute : Attribute 
    {
        private float m_Distance;
        private int m_Mask;

        public LookingAtAttribute(float distance, int mask)
        {
            m_Distance = distance;
            m_Mask = mask;
        }

        public LookingAtAttribute(float distance, params string[] mask)
        {
            m_Distance = distance;
            m_Mask = LayerMask.GetMask(mask);
        }

        public float GetDistance() => m_Distance;
        public int GetMask() => m_Mask;
    }
}
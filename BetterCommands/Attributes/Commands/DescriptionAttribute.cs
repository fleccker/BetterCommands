using System;

namespace BetterCommands
{ 
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class DescriptionAttribute : Attribute
    {
        public string Description { get; }

        public DescriptionAttribute(object description)
        {
            Description = description?.ToString() ?? "Invalid Description";

            if (Description.Length > 80) 
                Description = Description.Substring(0, 80) + "...";
        }
    }
}
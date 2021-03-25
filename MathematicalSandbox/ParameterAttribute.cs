using System;

namespace MathematicalSandbox
{
    [AttributeUsage(AttributeTargets.Parameter)]
    class ParameterAttribute : Attribute
    {
        private string description;

        public ParameterAttribute(string description)
        {
            this.description = description;
        }

        public override string ToString()
        {
            return description;
        }
    }
}
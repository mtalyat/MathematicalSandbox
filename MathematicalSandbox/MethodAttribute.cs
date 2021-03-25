using System;

namespace MathematicalSandbox
{
    [AttributeUsage(AttributeTargets.Method)]
    class MethodAttribute : Attribute
    {
        private string description;

        public MethodAttribute(string description)
        {
            this.description = description;
        }

        public override string ToString()
        {
            return description;
        }
    }
}
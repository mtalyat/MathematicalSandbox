using System;

namespace MathematicalSandbox
{
    [AttributeUsage(AttributeTargets.ReturnValue)]
    class ReturnAttribute : Attribute
    {
        private string description;

        public ReturnAttribute(string description)
        {
            this.description = description;
        }

        public override string ToString()
        {
            return description;
        }
    }
}
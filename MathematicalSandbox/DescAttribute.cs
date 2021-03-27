using System;
using System.Collections.Generic;
using System.Text;

namespace MathematicalSandbox
{
    class DescAttribute : Attribute
    {
        string description;

        public DescAttribute(string description)
        {
            this.description = description;
        }

        public override string ToString()
        {
            return description;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace MathematicalSandbox
{
    public enum CategoryType : int
    {
        All = 0,
        Math,
        Array,
        Algebra,
        Geometry,
        Calculus,
        Random,
        Miscellaneous
    }

    class CategoryAttribute : Attribute
    {
        public CategoryType CatType { get; private set; }

        public CategoryAttribute(CategoryType type)
        {
            CatType = type;
        }

        public override string ToString()
        {
            return Enum.GetName(typeof(CategoryType), CatType);
        }
    }
}

using System;
using System.Reflection;
using System.Collections.Generic;
using System.Text;

namespace MathematicalSandbox
{
    static class Output
    {
        private const char OUT_CHAR = '<';
        private static int INDENTATION_AMOUNT = 2;

        #region Printing

        public static void Clear()
        {
            Console.Clear();
        }

        public static void Print(string text, int indentation = 0)
        {
            Console.WriteLine(new string(' ', indentation * INDENTATION_AMOUNT) + text);
        }

        public static void PrintError(string message)
        {
            Console.WriteLine($"[{message}]");
        }

        public static void PrintLine()
        {
            Console.WriteLine();
        }
        public static void PrintLine(int amount)
        {
            for (int i = 0; i < amount; i++)
            {
                PrintLine();
            }
        }

        public static void PrintVariable(string name, object value)
        {
            Console.WriteLine($"{name} = {value}");
        }

        public static void PrintTitle(string text, int tabs = 2)
        {
            Console.WriteLine(new string('\t', tabs) + text);
        }

        public static void PrintParagraph(string text, int wrapWidth = 100)
        {
            Console.WriteLine(LooseTextWrap(text, wrapWidth));
        }

        public static void PrintMethod(string name) => PrintMethod(Function.GetFunction(name));
        public static void PrintMethod(MethodInfo mi)
        {
            Console.WriteLine(MethodToString(mi));
        }

        public static void PrintMethodInfo(MethodInfo mi)
        {
            //print the header
            Print(MethodToString(mi), 2);

            PrintLine();

            Print("Description:", 1);

            //write the description, if there is one
            MethodAttribute ma = (MethodAttribute)mi.GetCustomAttribute(typeof(MethodAttribute));
            if (ma != null)
            {
                Print(ma.ToString());
            }

            PrintLine();

            Print("Category:", 1);

            //write the category, if there is one
            CategoryAttribute ca = (CategoryAttribute)mi.GetCustomAttribute(typeof(CategoryAttribute));
            if(ca != null)
            {
                Print(ca.ToString());
            }

            PrintLine();

            //only print if there are parameters
            if(mi.GetParameters().Length > 0)
            {
                Print("Parameters:", 1);

                //write all of the parameters
                foreach (ParameterInfo pi in mi.GetParameters())
                {
                    Console.Write($"{pi.Name} - ");

                    //print the description, if there is one
                    ParameterAttribute pa = (ParameterAttribute)pi.GetCustomAttribute(typeof(ParameterAttribute));
                    if (pa != null)
                    {
                        Print(pa.ToString());
                    }
                    else
                    {
                        PrintLine();
                    }
                }

                PrintLine();
            }

            //only print if there is a return type
            if(mi.ReturnType != typeof(void))
            {
                //write the return type
                Print("Returns:", 1);

                //print the description, if applicable
                ReturnAttribute ra = (ReturnAttribute)mi.ReturnParameter.GetCustomAttribute(typeof(ReturnAttribute));
                if (ra != null)
                {
                    Print(ra.ToString());
                }
            }
        }

        public static string MethodToString(MethodInfo mi)
        {
            StringBuilder sb = new StringBuilder();

            ParameterInfo[] parameters = mi.GetParameters();

            //write the return type and name
            sb.Append($"{mi.ReturnType.Name.ToLower()} {mi.Name}(");

            //write all of the parameters
            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo pi = parameters[i];

                //print the type and name
                sb.Append($"{pi.ParameterType.Name.ToLower()} {pi.Name}");

                if (i < parameters.Length - 1)
                {
                    sb.Append(", ");
                }
            }
            sb.Append(")");

            return sb.ToString();
        }

        public static string ArrayToString(double[] array)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append('[');

            for (int i = 0; i < array.Length; i++)
            {
                sb.Append(array[i]);

                if (i < array.Length - 1)
                {
                    sb.Append(", ");
                }
            }

            sb.Append(']');

            return sb.ToString();
        }

        public static void PrintArray(double[] array)
        {
            Console.WriteLine(ArrayToString(array));
        }

        private static string LooseTextWrap(string text, int width)
        {
            StringBuilder sb = new StringBuilder();

            int w = 0;
            for (int i = 0; i < text.Length; i++, w++)
            {
                char c = text[i];

                if (w >= width && char.IsWhiteSpace(c))
                {
                    sb.Append(Environment.NewLine);
                    w = 0;
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        public static void Display(object o)
        {
            //check if it is an array, those are handled differently
            if (o.GetType().IsArray)
            {
                Console.Write(OUT_CHAR);
                PrintArray((double[])o);
            } else
            {
                Console.WriteLine(OUT_CHAR + o.ToString());
            }
        }

        #endregion
    }
}

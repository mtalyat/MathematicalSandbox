using System;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using MathNet.Numerics;
using MathNet.Numerics.Integration;
using MathNet.Symbolics;
using Expr = MathNet.Symbolics.SymbolicExpression;
using static MathematicalSandbox.Input;
using static MathematicalSandbox.Output;

namespace MathematicalSandbox
{
    public static class Function
    {
        #region Initialization and Reflection

        private static MethodInfo[] methods;
        private static FieldInfo[] fields;
        private static Random rng = new Random();

        static Function()
        {
            LoadMethods();
            LoadFields();
        }

        private static void LoadMethods()
        {
            //get all of the methods
            MethodInfo[] allMethods = typeof(Function).GetMethods(BindingFlags.Public | BindingFlags.Static);

            //then eliminate all of them with the Hidden attribute
            List<MethodInfo> nonHidden = new List<MethodInfo>();

            foreach(MethodInfo mi in allMethods)
            {
                if (mi.GetCustomAttributes(typeof(HiddenAttribute), false).Length == 0)
                {
                    nonHidden.Add(mi);
                }
            }

            //then set that to the array
            methods = nonHidden.ToArray();
        }

        private static void LoadFields()
        {
            //get all of the constant fields
            FieldInfo[] allFields = typeof(Function).GetFields(BindingFlags.Public | BindingFlags.Static);

            //then eliminate all of them with the Hidden attribute
            List<FieldInfo> nonHidden = new List<FieldInfo>();

            foreach(FieldInfo fi in allFields)
            {
                if(fi.IsLiteral && !fi.IsInitOnly && fi.GetCustomAttributes(typeof(HiddenAttribute), false).Length == 0)
                {
                    nonHidden.Add(fi);
                }
            }

            fields = nonHidden.ToArray();
        }

        [Hidden]
        public static MethodInfo GetFunction(string name)
        {
            foreach (MethodInfo mi in methods)
            {
                if (mi.Name == name) return mi;
            }

            return null;
        }

        public static MethodInfo GetFunctionFromFullName(string fullName)
        {
            foreach(MethodInfo mi in methods)
            {
                if (Output.MethodToString(mi).CompareTo(fullName) == 0) return mi;
            }

            return null;
        }
        
        [Hidden]
        public static MethodInfo GetFunction(int index)
        {
            if (index < 0 || index >= methods.Length) return null;

            return methods[index];
        }

        [Hidden]
        public static string[] GetFunctionNames(CategoryType category)
        {
            List<string> names = new List<string>();

            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo mi = GetFunction(i);

                //only add if category is all, or the category matches
                if (category == CategoryType.All)
                {
                    names.Add(MethodToString(mi));
                } else
                {
                    CategoryAttribute ca = (CategoryAttribute)mi.GetCustomAttribute(typeof(CategoryAttribute));
                    if(ca != null && ca.CatType == category)
                    {
                        names.Add(MethodToString(mi));
                    }
                }
            }

            return names.ToArray();
        }

        [Hidden]
        public static string ExtractFunctionName(string methodString)
        {
            int start = methodString.IndexOf(' ') + 1;
            int end = methodString.IndexOf('(', start);

            return methodString.Substring(start, end - start);
        }

        #endregion

        #region Parsing

        private static int FindNextClosing(string str, int startIndex, char opening, char closing)
        {
            int depth = 0;

            for (int i = startIndex; i < str.Length; i++)
            {
                char c = str[i];

                if (c == opening)
                {
                    depth++;
                } else if (c == closing)
                {
                    //ignore if inside of another set of brackets
                    if (depth > 0)
                    {
                        depth--;
                    } else
                    {
                        return i;
                    }
                }
            }

            //not found
            return -1;
        }

        [Hidden]
        public static string ReplaceFunctions(string input)
        {
            foreach (MethodInfo mi in methods)
            {
                string funcName = mi.Name;

                int funcIndex = input.IndexOf(funcName);

                //replace every occurance of this function
                while (funcIndex >= 0)
                {
                    //found the name, make sure there is a ( after the name
                    //this will verify that it is this function, not another one with a slightly different name
                    if(funcIndex + funcName.Length > input.Length || input[funcIndex + funcName.Length] != '(')
                    {
                        funcIndex = input.IndexOf(funcName, funcIndex + 1);
                        continue;
                    }

                    //get the arguments
                    int startIndex = funcIndex + funcName.Length + 1;
                    int endIndex = FindNextClosing(input, startIndex, '(', ')') - 1;
                    //int endIndex = input.IndexOf(')', funcIndex) - 1;

                    ParameterInfo[] pis = mi.GetParameters();

                    //start should be <= end, if there are parameters
                    if (startIndex > endIndex && pis.Length > 0) break;

                    //arguments
                    string[] args = new string[0];

                    if(startIndex <= endIndex)
                    {
                        string argsStr = input.Substring(startIndex, endIndex - startIndex + 1);
                        args = ParseArgs(argsStr);
                    }

                    object result;

                    if (args.Length > 0)
                    {
                        //get the values from all of the arguments
                        object[] values = new object[args.Length];

                        //make sure the amount of arguments is valid
                        if (args.Length > pis.Length || args.Length < pis.Length)
                        {
                            //just go check the next function
                            funcIndex = input.IndexOf(funcName, funcIndex + 1);
                            continue;
                        }

                        for (int i = 0; i < values.Length; i++)
                        {
                            ParameterInfo pi = pis[i];

                            string arg = args[i];//ParseInput(args[i]);

                            if (pi.ParameterType == typeof(bool))
                            {
                                values[i] = ParseBool(arg);
                            }
                            else if (pi.ParameterType == typeof(int))
                            {
                                values[i] = ParseInt(arg);
                            }
                            else if (pi.ParameterType == typeof(double[]))
                            {
                                values[i] = ParseDoubleArray(arg);
                            }
                            else if (pi.ParameterType == typeof(double))
                            {
                                values[i] = ParseDouble(arg);
                            }
                            else if (pi.ParameterType == typeof(string))
                            {
                                values[i] = ParseString(arg);
                            }
                            else if (pi.ParameterType == typeof(MathNet.Symbolics.SymbolicExpression))
                            {
                                values[i] = ParseExpression(arg);
                            }
                            else
                            {
                                values[i] = arg;//type??
                            }
                        }

                        //now that all of the values for the parameters have been gathered, pass them into the function and get the value
                        result = mi.Invoke(null, values);
                    } else
                    {
                        //no parameters, just run it
                        result = mi.Invoke(null, null);
                    }

                    //now take the updated custom function and replace it in the actual function
                    startIndex = funcIndex;
                    endIndex = FindNextClosing(input, funcIndex + funcName.Length + 1, '(', ')');
                    //endIndex = input.IndexOf(')', funcIndex);

                    //remove the function
                    input = input.Remove(startIndex, endIndex - startIndex + 1);

                    //if it has a return value, insert it
                    if (result != null)
                    {
                        //handle arrays differently...
                        if (result.GetType().IsArray)
                        {
                            input = input.Insert(startIndex, ArrayToString((double[])result));
                        } else
                        {
                            input = input.Insert(startIndex, result.ToString());
                        }
                    }

                    //find the index of the function again to keep going
                    funcIndex = input.IndexOf(funcName);
                }
            }

            return input;
        }

        [Hidden]
        public static string ReplaceConstants(string input)
        {
            foreach (FieldInfo fi in fields)
            {
                int varIndex = input.IndexOf(fi.Name);

                //replace every occurance of this constant
                while (varIndex >= 0)
                {
                    //make sure that this belongs to this constant only
                    if ((varIndex == 0 || !char.IsLetterOrDigit(input[varIndex - 1])) && (varIndex + fi.Name.Length >= input.Length || !char.IsLetterOrDigit(input[varIndex + fi.Name.Length])))
                    {
                        //if it is, replace it
                        input = input.Remove(varIndex, fi.Name.Length).Insert(varIndex, fi.GetRawConstantValue().ToString());
                    }
                    else
                    {
                        //if not a valid instance, set the index to the next one in the string
                        varIndex = input.IndexOf(fi.Name, varIndex + 1);
                        continue;
                    }

                    varIndex = input.IndexOf(fi.Name);
                }
            }

            return input;
        }

        #endregion

        #region Constants

        //non-hidden
        public const double PI = Math.PI;
        public const double E = Math.E;
        public const double G = 9.81;
        public const double DEG2RAD = PI / 180.0;
        public const double RAD2DEG = 180.0 / PI;

        //hidden
        [Hidden] public const double PRECISION = 0.000001;

        #endregion

        #region Math

        [Category(CategoryType.Math)]
        [method: Method("Returns the absolute value of the number d.")]
        [return: Return("The distance from 0, using d.")]
        public static double abs(
            [Parameter("The value to find the absolute value of.")] double d
            ) => Math.Abs(d);

        [Category(CategoryType.Math)]
        [method: Method("")]
        [return: Return("")]
        public static double acos(double theta) => Math.Acos(theta);

        [Category(CategoryType.Math)]
        [method: Method("")]
        [return: Return("")]
        public static double atan(double theta) => Math.Atan(theta);

        [Category(CategoryType.Math)]
        [method: Method("")]
        [return: Return("")]
        public static double asin(double theta) => Math.Asin(theta);

        [Category(CategoryType.Math)]
        [method: Method("")]
        [return: Return("")]
        public static double cos(double theta) => Math.Cos(theta);

        [Category(CategoryType.Math)]
        [method: Method("")]
        [return: Return("")]
        public static double tan(double theta) => Math.Tan(theta);

        [Category(CategoryType.Math)]
        [method: Method("")]
        [return: Return("")]
        public static double sin(double theta) => Math.Sin(theta);


        [Category(CategoryType.Math)]
        [method: Method("Returns d, rounded to the nearest whole number.")]
        [return: Return("")]
        public static double round(double d) => round(d, 1);

        [Category(CategoryType.Math)]
        [method: Method("Returns d, rounded to precision p.")]
        [return: Return("")]
        public static double round(double d, double p)
        {
            if (p == 0) return d;

            return Math.Round(d / p) * p;
        }

        [Category(CategoryType.Math)]
        [method: Method("Returns d, rounded up.")]
        [return: Return("")]
        public static double ceiling(double d) => Math.Ceiling(d);

        [Category(CategoryType.Math)]
        [method: Method("Returns d, rounded down.")]
        [return: Return("")]
        public static double floor(double d) => Math.Floor(d);


        [Category(CategoryType.Math)]
        [method: Method("Returns the minimum between two numbers.")]
        [return: Return("")]
        public static double min(double v1, double v2) => Math.Min(v1, v2);

        [Category(CategoryType.Math)]
        [method: Method("Returns the maximum between two numbers.")]
        [return: Return("")]
        public static double max(double v1, double v2) => Math.Max(v1, v2);

        [Category(CategoryType.Math)]
        [method: Method("Returns a value within the inclusive range of min and max.")]
        [return: Return("")]
        public static double clamp(double value, double min, double max) => Math.Clamp(value, min, max);


        [Category(CategoryType.Math)]
        [method: Method("Returns e ^ p.")]
        [return: Return("The value of e raised to the power of p.")]
        public static double exp(double p) => Math.Exp(p);


        [Category(CategoryType.Math)]
        [method: Method("")]
        [return: Return("")]
        public static double log(double d) => Math.Log(d);

        [Category(CategoryType.Math)]
        [method: Method("")]
        [return: Return("")]
        public static double log10(double d) => Math.Log10(d);

        [Category(CategoryType.Math)]
        [method: Method("")]
        [return: Return("")]
        public static double log2(double d) => Math.Log2(d);


        [Category(CategoryType.Math)]
        [method: Method("Returns d ^ p.")]
        [return: Return("The value of d raised to the power of p.")]
        public static double pow(double d, double p) => Math.Pow(d, p);

        [Category(CategoryType.Math)]
        [method: Method("Returns d ^ (1/2).")]
        [return: Return("The square root value of d.")]
        public static double sqrt(double d) => Math.Sqrt(d);


        [Category(CategoryType.Math)]
        [method: Method("Returns a number that represents the sign of d.")]
        [return: Return("-1 if the d is negative, 0 if d is 0, or 1 if d is positive.")]
        public static int sign(double d) => Math.Sign(d);


        [Category(CategoryType.Math)]
        [method: Method("Returns the comparison between two objects.")]
        [return: Return("True if the objects are the same value, false otherwise.")]
        public static bool compare(object o1, object o2)
        {
            return o1.ToString().CompareTo(o2.ToString()) == 0;
        }

        [Category(CategoryType.Math)]
        [method: Method("Returns the comparison between an array of numbers.")]
        [return: Return("True if the numbers are the same within 0.000001, false otherwise.")]
        public static bool compare(double[] values)
        {
            if (values.Length < 2) return true;

            for (int i = 0; i < values.Length - 1; i++)
            {
                if (!compare(values[i], values[i + 1]))
                {
                    return false;
                }
            }

            return true;
        }


        [Category(CategoryType.Math)]
        [method: Method("Returns the X component with distance d and angle theta.")]
        [return: Return("")]
        public static double extractX(double d, double theta)
        {
            return d * cos(theta);
        }

        [Category(CategoryType.Math)]
        [method: Method("Returns the Y component with distance d and angle theta.")]
        [return: Return("")]
        public static double extractY(double d, double theta)
        {
            return d * sin(theta);
        }


        [Category(CategoryType.Math)]
        [method: Method("Returns d % m.")]
        [return: Return("The value of d modulated by the value of m.")]
        public static double mod(double d, double m)
        {
            return d % m;
        }

        #endregion

        #region Random

        [Category(CategoryType.Random)]
        [method: Method("Sets the seed of the random number generator.")]
        public static void seed(int seed)
        {
            rng = new Random(seed);
        }

        [Category(CategoryType.Random)]
        [method: Method("Returns a random number between 0 (inclusive) and 1 (exclusive).")]
        [return: Return("A value in the range 0 <= x < 1.")]
        public static double random()
        {
            return rng.NextDouble();
        }

        [Category(CategoryType.Random)]
        [method: Method("Returns a random number between min (inclusive) and max (exclusive).")]
        [return: Return("A value in the range min <= x < max.")]
        public static double random(double min, double max)
        {
            return random() * (max - min) + min;
        }

        [Category(CategoryType.Random)]
        [method: Method("Returns an array of size size of random numbers between 0 (inclusive) and 1 (exclusive).")]
        [return: Return("An array whose values are in the range 0 <= x < 1.")]
        public static double[] randomArr(int size)
        {
            double[] arr = new double[size];

            for (int i = 0; i < size; i++)
            {
                arr[i] = random();
            }

            return arr;
        }

        [Category(CategoryType.Random)]
        [method: Method("Returns an array of size size of random numbers between min (inclusive) and max (exclusive).")]
        [return: Return("An array whose values are in the range min <= x < max.")]
        public static double[] randomArr(int size, double min, double max)
        {
            double[] arr = new double[size];

            for (int i = 0; i < size; i++)
            {
                arr[i] = random(min, max);
            }

            return arr;
        }

        [Category(CategoryType.Random)]
        [method: Method("Returns a random whole number that is greater than or equal to 0.")]
        [return: Return("")]
        public static int randomInt()
        {
            return rng.Next();
        }

        [Category(CategoryType.Random)]
        [method: Method("Returns a random whole number between min (inclusive) and max (exclusive).")]
        [return: Return("")]
        public static int randomInt(int min, int max)
        {
            return rng.Next(min, max);
        }

        [Category(CategoryType.Random)]
        [method: Method("Returns an array of size size of random whole numbers greater than or equal to 0.")]
        [return: Return("")]
        public static double[] randomArrInt(int size)
        {
            double[] arr = new double[size];

            for (int i = 0; i < size; i++)
            {
                arr[i] = randomInt();
            }

            return arr;
        }

        [Category(CategoryType.Random)]
        [method: Method("Returns an array of size size of random whole numbers between min (inclusive) and max (exclusive).")]
        [return: Return("")]
        public static double[] randomArrInt(int size, int min, int max)
        {
            double[] arr = new double[size];

            for (int i = 0; i < size; i++)
            {
                arr[i] = randomInt(min, max);
            }

            return arr;
        }

        #endregion

        #region Arrays

        [Category(CategoryType.Array)]
        [method: Method("Returns the sum of an array.")]
        [return: Return("The sum of all of the elements of the given array.")]
        public static double sum(double[] values)
        {
            double total = 0.0;

            foreach (double d in values) total += d;

            return total;
        }

        [Category(CategoryType.Array)]
        [method: Method("")]
        [return: Return("The average of all of the elements of the given array.")]
        public static double average(double[] values)
        {
            return sum(values) / values.Length;
        }


        [Category(CategoryType.Array)]
        [method: Method("Returns the number of elements of the array.")]
        [return: Return("The number of elements in the array.")]
        public static int count(double[] values)
        {
            return values.Length;
        }

        [Category(CategoryType.Array)]
        [method: Method("Returns the size of the array.")]
        [return: Return("The size of the array in memory, in bytes..")]
        public static int sizeOf(double[] values)
        {
            return sizeof(double) * values.Length;
        }

        [Category(CategoryType.Array)]
        [method: Method("Returns the minimum value in an array.")]
        [return: Return("The lowest value in the given array.")]
        public static double min(double[] values)
        {
            double min = double.MaxValue;

            foreach (double d in values)
            {
                if (d < min)
                {
                    min = d;
                }
            }

            return min;
        }

        [Category(CategoryType.Array)]
        [method: Method("Returns the maximum value in an array.")]
        [return: Return("The higest value in the given array.")]
        public static double max(double[] values)
        {
            double max = double.MinValue;

            foreach (double d in values)
            {
                if (d > max)
                {
                    max = d;
                }
            }

            return max;
        }

        [Category(CategoryType.Array)]
        [method: Method("Returns the range of the values in an array.")]
        [return: Return("The (highest value - lowest value) in the given array.")]
        public static double range(double[] values)
        {
            return max(values) - min(values);
        }


        [Category(CategoryType.Array)]
        [method: Method("")]
        [return: Return("")]
        public static double index(double[] values, int index)
        {
            if (index < 0 || index >= values.Length) return 0;

            return values[index];
        }

        #endregion

        #region Algebra

        [Category(CategoryType.Algebra)]
        [method: Method("Returns the value of the expression after plugging in the variable a.")]
        [return: Return("The value of the expression when solving for a.")]
        public static double evaluate(
            [Parameter("The equation to evaluate.")] Expr expr,
            [Parameter("The name of the variable in the equation.")] string aName,
            [Parameter("The value of the variable.")] double a)
        {
            var f = expr.Compile(aName);

            return f(a);
        }

        [Category(CategoryType.Algebra)]
        [method: Method("Returns the value of the expression after plugging in the variables a and b.")]
        [return: Return("The value of the expression when solving for a and b.")]
        public static double evaluate(
            [Parameter("The equation to evaluate.")] Expr expr,
            [Parameter("The name of the first variable in the equation.")] string aName, [Parameter("The value of the first variable.")] double a,
            [Parameter("The name of the second variable in the equation.")] string bName, [Parameter("The value of the second variable.")] double b)
        {
            var f = expr.Compile(aName, bName);

            return f(a, b);
        }

        [Category(CategoryType.Algebra)]
        [method: Method("Returns the value of the expression after plugging in the variables a, b and c.")]
        [return: Return("The value of the expression when solving for a, b and c.")]
        public static double evaluate(
            [Parameter("The equation to evaluate.")] Expr expr,
            [Parameter("The name of the first variable in the equation.")] string aName, [Parameter("The value of the first variable.")] double a,
            [Parameter("The name of the second variable in the equation.")] string bName, [Parameter("The value of the second variable.")] double b,
            [Parameter("The name of the third variable in the equation.")] string cName, [Parameter("The value of the third variable.")] double c)
        {
            var f = expr.Compile(aName, bName, cName);

            return f(a, b, c);
        }

        [Category(CategoryType.Algebra)]
        [method: Method("Returns the value of the expression after plugging in the variables a, b, c and d.")]
        [return: Return("The value of the expression when solving for a, b, c and d.")]
        public static double evaluate(
            [Parameter("The equation to evaluate.")] Expr expr,
            [Parameter("The name of the first variable in the equation.")] string aName, [Parameter("The value of the first variable.")] double a,
            [Parameter("The name of the second variable in the equation.")] string bName, [Parameter("The value of the second variable.")] double b,
            [Parameter("The name of the third variable in the equation.")] string cName, [Parameter("The value of the third variable.")] double c,
            [Parameter("The name of the fourth variable in the equation.")] string dName, [Parameter("The value of the fourth variable.")] double d)
        {
            var f = expr.Compile(aName, bName, cName, dName);

            return f(a, b, c, d);
        }

        [Category(CategoryType.Algebra)]
        [method: Method("Returns the distance of x and y from the origin (0, 0).")]
        [return: Return("")]
        public static double distance(double x, double y)
        {
            return sqrt(pow(x, 2) + pow(y, 2));
        }

        #endregion

        #region Geometry

        [Category(CategoryType.Geometry)]
        [method: Method("Returns the area of a circle, with given radius r.")]
        [return: Return("The area of the circle.")]
        public static double areaCircle(
            [Parameter("The radius of the circle.")] double r)
        {
            return PI * (r * r);
        }

        #endregion

        #region Calculus

        [Category(CategoryType.Calculus)]
        [method: Method("Returns the value of the first derivative of x.")]
        [return: Return("")]
        public static double firstDerivative(Expr expr, string varName, double x)
        {
            var f = expr.Compile(varName);

            return Differentiate.FirstDerivative(f, x);
        }

        [Category(CategoryType.Calculus)]
        [method: Method("Returns the value of the second derivative of x.")]
        [return: Return("")]
        public static double secondDerivative(Expr expr, string varName, double x)
        {
            var f = expr.Compile(varName);

            return Differentiate.SecondDerivative(f, x);
        }

        [Category(CategoryType.Calculus)]
        [method: Method("Returns the value of the third derivative of x.")]
        [return: Return("")]
        public static double thirdDerivative(Expr expr, string varName, double x)
        {
            var f = expr.Compile(varName);

            return Differentiate.FirstDerivative(Differentiate.SecondDerivativeFunc(f), x);
        }

        [Category(CategoryType.Calculus)]
        [method: Method("Returns the value of the fourth derivative of x.")]
        [return: Return("")]
        public static double fourthDerivative(Expr expr, string varName, double x)
        {
            var f = expr.Compile(varName);

            return Differentiate.SecondDerivative(Differentiate.SecondDerivativeFunc(f), x);
        }

        [Category(CategoryType.Calculus)]
        [method: Method("Returns the value of the closed integral.")]
        [return: Return("")]
        public static double integral(Expr expr, string varName, double a, double b)
        {
            var f = expr.Compile(varName);

            return Integrate.OnClosedInterval(f, a, b);
        }

        #region Trapezoidal

        [Category(CategoryType.Calculus)]
        [method: Method("Returns the integrated value using the Trapezoidal Rule.")]
        [return: Return("")]
        public static double trapezoidalRule(Expr expr, string varName, double a, double b, int n)
        {
            var f = expr.Compile(varName);

            return NewtonCotesTrapeziumRule.IntegrateComposite(f, a, b, n);
        }

        [Category(CategoryType.Calculus)]
        [method: Method("Returns the integrated value using the Trapezoidal Rule, with the given values and intervals.")]
        [return: Return("")]
        public static double trapezoidalRule(double[] values, double[] intervals)
        {
            //find the total area from all of the values and intervals
            double totalArea = 0.0;

            for (int i = 0; i < values.Length - 1; i++)
            {
                totalArea += intervals[i] / 2.0 * (values[i] + values[i + 1]);
            }

            //return the total area
            return totalArea;
        }

        [Category(CategoryType.Calculus)]
        [method: Method("Returns the integrated value using the Trapezoidal Rule, given the Et instead of n.")]
        [return: Return("")]
        public static double trapezoidalRuleError(Expr expr, string varName, double a, double b, double Et)
        {
            var f = expr.Compile(varName);

            return NewtonCotesTrapeziumRule.IntegrateAdaptive(f, a, b, Et);
        }

        [Category(CategoryType.Calculus)]
        [method: Method("Returns the value of M, using the Trapezoidal Rule.")]
        [return: Return("")]
        public static double trapezoidalRuleM(Expr expr, string varName, double a, double b)
        {
            var f = expr.Compile(varName);

            f = Differentiate.SecondDerivativeFunc(f);

            return max(f(a), f(b));
        }

        [Category(CategoryType.Calculus)]
        [method: Method("Returns the amount of intervals, n, that can be used with the given error, Et.")]
        [return: Return("The number of interals.")]
        public static double trapezoidalRuleN(double m, double a, double b, double Et)
        {
            return ceiling(sqrt((m * pow(b - a, 3)) / (12 * abs(Et))));
        }

        [Category(CategoryType.Calculus)]
        [method: Method("Returns the maximum amount of error, using the Simpson Rule.")]
        [return: Return("")]
        public static double trapezoidalRuleUpper(double m, double a, double b, int n)
        {
            return abs((m * pow(b - a, 3)) / (12 * pow(n, 2)));
        }

        [Category(CategoryType.Calculus)]
        [method: Method("Returns the amount of error, using the Trapezoidal Rule.")]
        [return: Return("The difference of the estimated integrated value versus the true integrated value.")]
        public static double trapezoidalRuleEt(Expr expr, string varName, double a, double b, int n)
        {
            var f = expr.Compile(varName);

            double trValue = NewtonCotesTrapeziumRule.IntegrateComposite(f, a, b, n);

            double trueValue = Integrate.OnClosedInterval(f, a, b);

            return abs(trueValue - trValue);
        }

        #endregion

        #region Simpson

        [Category(CategoryType.Calculus)]
        [method: Method("Returns the integrated value using the Simpson Rule.")]
        [return: Return("")]
        public static double simpsonRule(Expr expr, string varName, double a, double b, int n)
        {
            var f = expr.Compile(varName);

            return SimpsonRule.IntegrateComposite(f, a, b, n);
        }

        [Category(CategoryType.Calculus)]
        [method: Method("Returns the integrated value using the Simpson Rule, with the given values and interval.")]
        [return: Return("")]
        public static double simpsonRule(double[] values, double interval)
        {
            //first do the inside of the parenthesis
            //add the first and last numbers first since they are not doubled
            double s = values[0] + values[values.Length - 1];

            //then avoid the first and last in the loop
            for (int i = 1; i < values.Length - 1; i++)
            {
                if (i % 2 == 1)
                {
                    s += values[i] * 4.0;
                }
                else
                {
                    s += values[i] * 2.0;
                }
            }

            //finally, multiply the value of T by the interval divided by 3
            s *= interval / 3.0;

            return s;
        }

        [Category(CategoryType.Calculus)]
        [method: Method("Returns the value of M, using the Simpson Rule.")]
        [return: Return("")]
        public static double simpsonRuleM(Expr expr, string varName, double a, double b)
        {
            var f = expr.Compile(varName);

            f = Differentiate.SecondDerivativeFunc(Differentiate.SecondDerivativeFunc(f));

            return max(f(a), f(b));
        }

        [Category(CategoryType.Calculus)]
        [method: Method("Returns the amount of intervals, n, that can be used with the given error, Es.")]
        [return: Return("The number of intervals.")]
        public static double simpsonRuleN(double m, double a, double b, double Es)
        {
            int n = (int)ceiling(pow((m * pow(b - a, 5)) / (180 * abs(Es)), 1.0 / 4));

            //n must be an even number and > 0
            if (n <= 0) n = 2;
            if (n % 2 == 1) n++;

            return n;
        }

        [Category(CategoryType.Calculus)]
        [method: Method("Returns the maximum amount of error, using the Simpson Rule.")]
        [return: Return("")]
        public static double simpsonRuleUpper(double m, double a, double b, int n)
        {
            return (m * pow(b - a, 5)) / (180 * pow(n, 4));
        }

        [Category(CategoryType.Calculus)]
        [method: Method("Returns the amount of error, using the Simpson Rule.")]
        [return: Return("The difference of the estimated integrated value versus the true integrated value.")]
        public static double simpsonRuleEs(Expr expr, string varName, double a, double b, int n)
        {
            var f = expr.Compile(varName);

            double srValue = SimpsonRule.IntegrateComposite(f, a, b, n);

            double trueValue = Integrate.OnClosedInterval(f, a, b);

            return abs(trueValue - srValue);
        }

        [Category(CategoryType.Calculus)]
        [method: Method("")]
        [return: Return("")]
        public static double percentOfTrueValue(double EsEt, double trueValue)
        {
            return (abs(EsEt) / trueValue) * 100.0;
        }

        #endregion

        #endregion

        #region Miscellaneous

        [Category(CategoryType.Miscellaneous)]
        [method: Method("Clears the screen of all previous calculations.")]
        public static void clear()
        {
            Sandbox.RedrawScreen();
        }

        #endregion
    }
}
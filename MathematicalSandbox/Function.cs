using System;
using System.Linq;
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
        public static object GetConstant(string name)
        {
            //check the constants
            FieldInfo fi = fields.First((v) =>
            {
                return v.Name == name;
            });

            if (fi != null) return fi;

            //found nothing, return default
            return 0.0;
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

        [Hidden]
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

        [Hidden]
        public static string[] ReplaceConstants(string[] input)
        {
            string[] output = new string[input.Length];

            for (int i = 0; i < input.Length; i++)
            {
                String s = input[i];

                bool found = false;
                foreach(FieldInfo fi in fields)
                {
                    if(fi.Name.CompareTo(s) == 0)
                    {
                        output[i] = fi.GetRawConstantValue().ToString();
                        found = true;
                        break;
                    }
                }

                if (!found)
                    output[i] = s;
            }

            return output;
        }

        [Hidden]
        public static object EvaluateFunction(string name, object[] args)
        {
            return GetFunction(name).Invoke(null, args);
        }

        #endregion

        #region Parsing



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
        [method: Desc("Returns the absolute value of the number d.")]
        [return: Desc("The distance from 0, using d.")]
        public static double abs(
            [Desc("The value to find the absolute value of.")] double d
            ) => Math.Abs(d);

        [Category(CategoryType.Math)]
        [method: Desc("")]
        [return: Desc("")]
        public static double acos(double theta) => Math.Acos(theta);

        [Category(CategoryType.Math)]
        [method: Desc("")]
        [return: Desc("")]
        public static double atan(double theta) => Math.Atan(theta);

        [Category(CategoryType.Math)]
        [method: Desc("")]
        [return: Desc("")]
        public static double asin(double theta) => Math.Asin(theta);

        [Category(CategoryType.Math)]
        [method: Desc("")]
        [return: Desc("")]
        public static double cos(double theta) => Math.Cos(theta);

        [Category(CategoryType.Math)]
        [method: Desc("")]
        [return: Desc("")]
        public static double tan(double theta) => Math.Tan(theta);

        [Category(CategoryType.Math)]
        [method: Desc("")]
        [return: Desc("")]
        public static double sin(double theta) => Math.Sin(theta);


        [Category(CategoryType.Math)]
        [method: Desc("Returns d, rounded to the nearest whole number.")]
        [return: Desc("")]
        public static double round(double d) => roundPrecise(d, 1);

        [Category(CategoryType.Math)]
        [method: Desc("Returns d, rounded to precision p.")]
        [return: Desc("")]
        public static double roundPrecise(double d, double p)
        {
            if (p == 0) return d;

            return Math.Round(d / p) * p;
        }

        [Category(CategoryType.Math)]
        [method: Desc("Returns d, rounded up.")]
        [return: Desc("")]
        public static double ceiling(double d) => Math.Ceiling(d);

        [Category(CategoryType.Math)]
        [method: Desc("Returns d, rounded down.")]
        [return: Desc("")]
        public static double floor(double d) => Math.Floor(d);


        [Category(CategoryType.Math)]
        [method: Desc("Returns the minimum between two numbers.")]
        [return: Desc("")]
        public static double min(double v1, double v2) => Math.Min(v1, v2);

        [Category(CategoryType.Math)]
        [method: Desc("Returns the maximum between two numbers.")]
        [return: Desc("")]
        public static double max(double v1, double v2) => Math.Max(v1, v2);

        [Category(CategoryType.Math)]
        [method: Desc("Returns a value within the inclusive range of min and max.")]
        [return: Desc("")]
        public static double clamp(double value, double min, double max) => Math.Clamp(value, min, max);


        [Category(CategoryType.Math)]
        [method: Desc("Returns e ^ p.")]
        [return: Desc("The value of e raised to the power of p.")]
        public static double exp(double p) => Math.Exp(p);


        [Category(CategoryType.Math)]
        [method: Desc("")]
        [return: Desc("")]
        public static double log(double d) => Math.Log(d);

        [Category(CategoryType.Math)]
        [method: Desc("")]
        [return: Desc("")]
        public static double log10(double d) => Math.Log10(d);

        [Category(CategoryType.Math)]
        [method: Desc("")]
        [return: Desc("")]
        public static double log2(double d) => Math.Log2(d);


        [Category(CategoryType.Math)]
        [method: Desc("Returns d ^ p.")]
        [return: Desc("The value of d raised to the power of p.")]
        public static double pow(double d, double p) => Math.Pow(d, p);

        [Category(CategoryType.Math)]
        [method: Desc("Returns d ^ (1/2).")]
        [return: Desc("The square root value of d.")]
        public static double sqrt(double d) => Math.Sqrt(d);


        [Category(CategoryType.Math)]
        [method: Desc("Returns a number that represents the sign of d.")]
        [return: Desc("-1 if the d is negative, 0 if d is 0, or 1 if d is positive.")]
        public static int sign(double d) => Math.Sign(d);


        [Category(CategoryType.Math)]
        [method: Desc("Returns the comparison between two objects.")]
        [return: Desc("True if the objects are the same value, false otherwise.")]
        public static bool compare(object o1, object o2)
        {
            return o1.ToString().CompareTo(o2.ToString()) == 0;
        }

        [Category(CategoryType.Math)]
        [method: Desc("Returns the comparison between an array of numbers.")]
        [return: Desc("True if the numbers are the same within 0.000001, false otherwise.")]
        public static bool compareArr(double[] values)
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
        [method: Desc("Returns the X component with distance d and angle theta.")]
        [return: Desc("")]
        public static double extractX(double d, double theta)
        {
            return d * cos(theta);
        }

        [Category(CategoryType.Math)]
        [method: Desc("Returns the Y component with distance d and angle theta.")]
        [return: Desc("")]
        public static double extractY(double d, double theta)
        {
            return d * sin(theta);
        }


        [Category(CategoryType.Math)]
        [method: Desc("Returns d % m.")]
        [return: Desc("The value of d modulated by the value of m.")]
        public static double mod(double d, double m)
        {
            return d % m;
        }

        [Category(CategoryType.Array)]
        [method: Desc("Returns the size of object.")]
        [return: Desc("The size of the array in memory, in bytes, or -1 if the size cannot be determined.")]
        public static int sizeOf(object o)
        {
            Type t = o.GetType();

            if(t == typeof(int))
            {
                return sizeof(int);
            } else if (t == typeof(double))
            {
                return sizeof(double);
            } else if (t == typeof(string))
            {
                return sizeof(char) * o.ToString().Length;
            } else if (t == typeof(bool))
            {
                return sizeof(bool);
            } else
            {
                //anything else cannot be determined
                return -1;
            }
        }

        #endregion

        #region Random

        [Category(CategoryType.Random)]
        [method: Desc("Sets the seed of the random number generator.")]
        public static void seed(int seed)
        {
            rng = new Random(seed);
        }

        [Category(CategoryType.Random)]
        [method: Desc("Returns a random number between 0 (inclusive) and 1 (exclusive).")]
        [return: Desc("A value in the range 0 <= x < 1.")]
        public static double random()
        {
            return rng.NextDouble();
        }

        [Category(CategoryType.Random)]
        [method: Desc("Returns a random number between min (inclusive) and max (exclusive).")]
        [return: Desc("A value in the range min <= x < max.")]
        public static double randomRange(double min, double max)
        {
            return random() * (max - min) + min;
        }

        [Category(CategoryType.Random)]
        [method: Desc("Returns an array of size size of random numbers between 0 (inclusive) and 1 (exclusive).")]
        [return: Desc("An array whose values are in the range 0 <= x < 1.")]
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
        [method: Desc("Returns an array of size size of random numbers between min (inclusive) and max (exclusive).")]
        [return: Desc("An array whose values are in the range min <= x < max.")]
        public static double[] randomRangeArr(int size, double min, double max)
        {
            double[] arr = new double[size];

            for (int i = 0; i < size; i++)
            {
                arr[i] = randomRange(min, max);
            }

            return arr;
        }

        [Category(CategoryType.Random)]
        [method: Desc("Returns a random whole number that is greater than or equal to 0.")]
        [return: Desc("")]
        public static int randomInt()
        {
            return rng.Next();
        }

        [Category(CategoryType.Random)]
        [method: Desc("Returns a random whole number between min (inclusive) and max (exclusive).")]
        [return: Desc("")]
        public static int randomRangeInt(int min, int max)
        {
            return rng.Next(min, max);
        }

        [Category(CategoryType.Random)]
        [method: Desc("Returns an array of size size of random whole numbers greater than or equal to 0.")]
        [return: Desc("")]
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
        [method: Desc("Returns an array of size size of random whole numbers between min (inclusive) and max (exclusive).")]
        [return: Desc("")]
        public static double[] randomRangeArrInt(int size, int min, int max)
        {
            double[] arr = new double[size];

            for (int i = 0; i < size; i++)
            {
                arr[i] = randomRangeInt(min, max);
            }

            return arr;
        }

        #endregion

        #region Arrays

        [Category(CategoryType.Array)]
        [method: Desc("Returns the sum of an array.")]
        [return: Desc("The sum of all of the elements of the given array.")]
        public static double sumArr(double[] values)
        {
            double total = 0.0;

            foreach (double d in values) total += d;

            return total;
        }

        [Category(CategoryType.Array)]
        [method: Desc("Returns the average of the values in an array.")]
        [return: Desc("The average of all of the elements of the given array.")]
        public static double averageArr(double[] values)
        {
            return sumArr(values) / values.Length;
        }

        [Category(CategoryType.Array)]
        [method: Desc("Returns the median of the values in an array.")]
        [return: Desc("The middle value of an array.")]
        public static double medianArr(double[] values)
        {
            if (values.Length == 0) return 0;

            double[] sorted = sortArr(values);

            return sorted[sorted.Length / 2];
        }


        [Category(CategoryType.Array)]
        [method: Desc("Returns the number of elements of the array.")]
        [return: Desc("The number of elements in the array.")]
        public static int countArr(double[] values)
        {
            return values.Length;
        }

        [Category(CategoryType.Array)]
        [method: Desc("Returns the size of the array.")]
        [return: Desc("The size of the array in memory, in bytes.")]
        public static int sizeOfArr(double[] values)
        {
            return sizeof(double) * values.Length;
        }

        [Category(CategoryType.Array)]
        [method: Desc("Returns the minimum value in an array.")]
        [return: Desc("The lowest value in the given array.")]
        public static double minArr(double[] values)
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
        [method: Desc("Returns the maximum value in an array.")]
        [return: Desc("The higest value in the given array.")]
        public static double maxArr(double[] values)
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
        [method: Desc("Returns the range of the values in an array.")]
        [return: Desc("The (highest value - lowest value) in the given array.")]
        public static double rangeArr(double[] values)
        {
            return maxArr(values) - minArr(values);
        }


        [Category(CategoryType.Array)]
        [method: Desc("Returns the value at a given index from an array.")]
        [return: Desc("The value at the index, or 0 if the index is out of range.")]
        public static double indexArr(double[] values, int index)
        {
            if (index < 0 || index >= values.Length) return 0;

            return values[index];
        }

        [Category(CategoryType.Array)]
        [method: Desc("Returns a copy of the given array, sorted.")]
        [return: Desc("A copy of the array, sorted.")]
        public static double[] sortArr(double[] values)
        {
            List<double> list = new List<double>(values);

            list.Sort();

            return list.ToArray();
        }

        #endregion

        #region Algebra

        [Category(CategoryType.Algebra)]
        [method: Desc("Returns the value of the expression after plugging in the variable a.")]
        [return: Desc("The value of the expression when solving for a.")]
        public static double evaluate1(
            [Desc("The equation to evaluate.")] Expr expr,
            [Desc("The name of the variable in the equation.")] string aName,
            [Desc("The value of the variable.")] double a)
        {
            var f = expr.Compile(aName);

            return f(a);
        }

        [Category(CategoryType.Algebra)]
        [method: Desc("Returns the value of the expression after plugging in the variables a and b.")]
        [return: Desc("The value of the expression when solving for a and b.")]
        public static double evaluate2(
            [Desc("The equation to evaluate.")] Expr expr,
            [Desc("The name of the first variable in the equation.")] string aName, [Desc("The value of the first variable.")] double a,
            [Desc("The name of the second variable in the equation.")] string bName, [Desc("The value of the second variable.")] double b)
        {
            var f = expr.Compile(aName, bName);

            return f(a, b);
        }

        [Category(CategoryType.Algebra)]
        [method: Desc("Returns the value of the expression after plugging in the variables a, b and c.")]
        [return: Desc("The value of the expression when solving for a, b and c.")]
        public static double evaluate3(
            [Desc("The equation to evaluate.")] Expr expr,
            [Desc("The name of the first variable in the equation.")] string aName, [Desc("The value of the first variable.")] double a,
            [Desc("The name of the second variable in the equation.")] string bName, [Desc("The value of the second variable.")] double b,
            [Desc("The name of the third variable in the equation.")] string cName, [Desc("The value of the third variable.")] double c)
        {
            var f = expr.Compile(aName, bName, cName);

            return f(a, b, c);
        }

        [Category(CategoryType.Algebra)]
        [method: Desc("Returns the value of the expression after plugging in the variables a, b, c and d.")]
        [return: Desc("The value of the expression when solving for a, b, c and d.")]
        public static double evaluate4(
            [Desc("The equation to evaluate.")] Expr expr,
            [Desc("The name of the first variable in the equation.")] string aName, [Desc("The value of the first variable.")] double a,
            [Desc("The name of the second variable in the equation.")] string bName, [Desc("The value of the second variable.")] double b,
            [Desc("The name of the third variable in the equation.")] string cName, [Desc("The value of the third variable.")] double c,
            [Desc("The name of the fourth variable in the equation.")] string dName, [Desc("The value of the fourth variable.")] double d)
        {
            var f = expr.Compile(aName, bName, cName, dName);

            return f(a, b, c, d);
        }

        [Category(CategoryType.Algebra)]
        [method: Desc("Returns the distance of x and y from the origin (0, 0).")]
        [return: Desc("")]
        public static double distance(double x, double y)
        {
            return sqrt(pow(x, 2) + pow(y, 2));
        }

        [Category(CategoryType.Algebra)]
        [method: Desc("Returns an expression containing an approximate fraction based on d.")]
        [return: Desc("")]
        public static Expr approximate(double d)
        {
            double denominator = pow(10, d.ToString().Length);

            return Expr.Parse(d + "/" + denominator);
        }

        [Category(CategoryType.Algebra)]
        [method: Desc("Returns an array with the two possible answers to the quadratic formula.")]
        [return: Desc("A double array with the two values from the formula, the first being the + one and the second being the - one. If a is 0, both return values will be 0.")]
        public static double[] quadratic(double a, double b, double c)
        {
            double[] output = new double[2];

            if(a != 0)
            {
                output[0] = (-b + sqrt(pow(b, 2) - 4 * a * c)) / (2 * a);
                output[1] = (-b - sqrt(pow(b, 2) - 4 * a * c)) / (2 * a);
            }

            return output;
        }

        [Category(CategoryType.Algebra)]
        [method: Desc("Returns a simplified version of the given expression using the variable varName.")]
        [return: Desc("")]
        public static Expr simplify(Expr expr, string varName)
        {
            return expr.RationalSimplify(Expr.Variable(varName));
        }

        [Category(CategoryType.Algebra)]
        [method: Desc("Returns an expanded version of the given expression using the variable varName.")]
        [return: Desc("")]
        public static Expr expand(Expr expr)
        {
            return expr.Expand();
        }

        #endregion

        #region Geometry

        [Category(CategoryType.Geometry)]
        [method: Desc("Returns the area of a circle, with given radius r.")]
        [return: Desc("The area of the circle.")]
        public static double areaCircle(
            [Desc("The radius of the circle.")] double r)
        {
            return PI * (r * r);
        }

        #endregion

        #region Calculus

        [Category(CategoryType.Calculus)]
        [method: Desc("Returns the value of the first derivative of x.")]
        [return: Desc("")]
        public static double derivative1(Expr expr, string varName, double x)
        {
            var f = expr.Compile(varName);

            return Differentiate.FirstDerivative(f, x);
        }

        [Category(CategoryType.Calculus)]
        [method: Desc("Returns the value of the second derivative of x.")]
        [return: Desc("")]
        public static double derivative2(Expr expr, string varName, double x)
        {
            var f = expr.Compile(varName);

            return Differentiate.SecondDerivative(f, x);
        }

        [Category(CategoryType.Calculus)]
        [method: Desc("Returns the value of the third derivative of x.")]
        [return: Desc("")]
        public static double derivative3(Expr expr, string varName, double x)
        {
            var f = expr.Compile(varName);

            return Differentiate.FirstDerivative(Differentiate.SecondDerivativeFunc(f), x);
        }

        [Category(CategoryType.Calculus)]
        [method: Desc("Returns the value of the fourth derivative of x.")]
        [return: Desc("")]
        public static double derivative4(Expr expr, string varName, double x)
        {
            var f = expr.Compile(varName);

            return Differentiate.SecondDerivative(Differentiate.SecondDerivativeFunc(f), x);
        }

        [Category(CategoryType.Calculus)]
        [method: Desc("Returns the value of the closed integral.")]
        [return: Desc("")]
        public static double integral(Expr expr, string varName, double a, double b)
        {
            var f = expr.Compile(varName);

            return Integrate.OnClosedInterval(f, a, b);
        }

        #region Trapezoidal

        [Category(CategoryType.Calculus)]
        [method: Desc("Returns the integrated value using the Trapezoidal Rule.")]
        [return: Desc("")]
        public static double trapezoidalRule(Expr expr, string varName, double a, double b, int n)
        {
            var f = expr.Compile(varName);

            return NewtonCotesTrapeziumRule.IntegrateComposite(f, a, b, n);
        }

        [Category(CategoryType.Calculus)]
        [method: Desc("Returns the integrated value using the Trapezoidal Rule, with the given values and intervals.")]
        [return: Desc("")]
        public static double trapezoidalRuleInterval(double[] values, double[] intervals)
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
        [method: Desc("Returns the integrated value using the Trapezoidal Rule, given the Et instead of n.")]
        [return: Desc("")]
        public static double trapezoidalRuleError(Expr expr, string varName, double a, double b, double Et)
        {
            var f = expr.Compile(varName);

            return NewtonCotesTrapeziumRule.IntegrateAdaptive(f, a, b, Et);
        }

        [Category(CategoryType.Calculus)]
        [method: Desc("Returns the value of M, using the Trapezoidal Rule.")]
        [return: Desc("")]
        public static double trapezoidalRuleM(Expr expr, string varName, double a, double b)
        {
            var f = expr.Compile(varName);

            f = Differentiate.SecondDerivativeFunc(f);

            return max(f(a), f(b));
        }

        [Category(CategoryType.Calculus)]
        [method: Desc("Returns the amount of intervals, n, that can be used with the given error, Et.")]
        [return: Desc("The number of interals.")]
        public static double trapezoidalRuleN(double m, double a, double b, double Et)
        {
            return ceiling(sqrt((m * pow(b - a, 3)) / (12 * abs(Et))));
        }

        [Category(CategoryType.Calculus)]
        [method: Desc("Returns the maximum amount of error, using the Simpson Rule.")]
        [return: Desc("")]
        public static double trapezoidalRuleUpper(double m, double a, double b, int n)
        {
            return abs((m * pow(b - a, 3)) / (12 * pow(n, 2)));
        }

        [Category(CategoryType.Calculus)]
        [method: Desc("Returns the amount of error, using the Trapezoidal Rule.")]
        [return: Desc("The difference of the estimated integrated value versus the true integrated value.")]
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
        [method: Desc("Returns the integrated value using the Simpson Rule.")]
        [return: Desc("")]
        public static double simpsonRule(Expr expr, string varName, double a, double b, int n)
        {
            var f = expr.Compile(varName);

            return SimpsonRule.IntegrateComposite(f, a, b, n);
        }

        [Category(CategoryType.Calculus)]
        [method: Desc("Returns the integrated value using the Simpson Rule, with the given values and interval.")]
        [return: Desc("")]
        public static double simpsonRuleInterval(double[] values, double interval)
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
        [method: Desc("Returns the value of M, using the Simpson Rule.")]
        [return: Desc("")]
        public static double simpsonRuleM(Expr expr, string varName, double a, double b)
        {
            var f = expr.Compile(varName);

            f = Differentiate.SecondDerivativeFunc(Differentiate.SecondDerivativeFunc(f));

            return max(f(a), f(b));
        }

        [Category(CategoryType.Calculus)]
        [method: Desc("Returns the amount of intervals, n, that can be used with the given error, Es.")]
        [return: Desc("The number of intervals.")]
        public static int simpsonRuleN(double m, double a, double b, double Es)
        {
            int n = (int)ceiling(pow((m * pow(b - a, 5)) / (180 * abs(Es)), 1.0 / 4));

            //n must be an even number and > 0
            if (n <= 0) n = 2;
            if (n % 2 == 1) n++;

            return n;
        }

        [Category(CategoryType.Calculus)]
        [method: Desc("Returns the maximum amount of error, using the Simpson Rule.")]
        [return: Desc("")]
        public static double simpsonRuleUpper(double m, double a, double b, int n)
        {
            return (m * pow(b - a, 5)) / (180 * pow(n, 4));
        }

        [Category(CategoryType.Calculus)]
        [method: Desc("Returns the amount of error, using the Simpson Rule.")]
        [return: Desc("The difference of the estimated integrated value versus the true integrated value.")]
        public static double simpsonRuleEs(Expr expr, string varName, double a, double b, int n)
        {
            var f = expr.Compile(varName);

            double srValue = SimpsonRule.IntegrateComposite(f, a, b, n);

            double trueValue = Integrate.OnClosedInterval(f, a, b);

            return abs(trueValue - srValue);
        }

        [Category(CategoryType.Calculus)]
        [method: Desc("")]
        [return: Desc("")]
        public static double percentOfTrueValue(double EsEt, double trueValue)
        {
            return (abs(EsEt) / trueValue) * 100.0;
        }

        #endregion

        #endregion

        #region Miscellaneous

        [Category(CategoryType.Miscellaneous)]
        [method: Desc("Clears the screen of all previous calculations.")]
        public static void clear()
        {
            Sandbox.ClearScreen();
        }

        [Category(CategoryType.Miscellaneous)]
        [method: Desc("Returns string version of o.")]
        [return: Desc("")]
        public static string toString(object o)
        {
            return o.ToString();
        }

        [Category(CategoryType.Miscellaneous)]
        [method: Desc("Returns an integer representing the amount of significant figures in str.")]
        [return: Desc("")]
        public static int sigfig(string str)
        {
            //remove the 0's on the left and the decimal point and you have your sigfigs
            return str.TrimStart('0').Replace(".", "").Length;
        }

        #endregion
    }
}
using System;
using System.Collections.Generic;
using System.Text;
using MathNet.Symbolics;
using Expr = MathNet.Symbolics.SymbolicExpression;
using static MathematicalSandbox.Output;

namespace MathematicalSandbox
{
    static class Input
    {
        private const char IN_CHAR = '>';

        #region Reading

        public static object ReadAnything()
        {
            return ParseAnything(ReadString());
        }

        public static Expr ReadExpression()
        {
            return ParseExpression(Console.ReadLine());
        }

        public static string ReadString()
        {
            return Console.ReadLine();
        }

        public static int ReadInt()
        {
            return (int)ReadDouble();
        }

        public static double ReadDouble()
        {
            return ReadExpression().Compile("")(0);
        }

        public static ConsoleKey ReadKey()
        {
            return Console.ReadKey(true).Key;
        }

        public static ConsoleKeyInfo ReadInfo()
        {
            return Console.ReadKey(true);
        }

        public static bool ReadBool()
        {
            return ParseBool(ReadString());
        }

        public static void WaitForEnter()
        {
            Console.WriteLine("Press Enter to continue.");

            ConsoleKey key;

            do
            {
                key = ReadKey();
            } while (key != ConsoleKey.Enter);
        }

        #endregion

        #region Conversions

        public static object ParseAnything(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return null;
            }

            if (input.Contains("=="))//evaluation
            {
                input = ParseInput(input);

                string[] parts = input.Split("==");

                bool equal = true;

                for (int i = 0; i < parts.Length - 1; i++)
                {
                    if (!Function.compare(ParseAnything(parts[i]), ParseAnything(parts[i + 1])))
                    {
                        equal = false;
                    }
                }

                return equal;
            }
            else if (input.Contains("="))//assignment
            {
                string[] parts = input.Split('=');

                if (parts.Length > 2)
                {
                    PrintError("You cannot have more than one assignment in one line.");
                    return null;
                }

                Sandbox.SetVariable(parts[0], ParseAnything(parts[1]));
                return Sandbox.GetVariable(parts[0]);
            }
            else//expression, or otherwise
            {
                if (input.Contains('"'))//string
                {
                    return ParseString(input);
                }

                //any math thing doesn't need spaces, and you can replace functions and all that now
                input = ParseInput(input.Replace(" ", ""));

                //if it is an expression, handle that
                if (input.Contains('{') && input.Contains('}'))//expression
                {
                    Expr expr = ParseExpression(input.Replace("{", "").Replace("}", ""));

                    return expr;
                }

                //should be only numbers at this point, so feel free to remove spaces
                input = ParseInput(input.Replace(" ", ""));

                if (input.Contains('[') && input.Contains(']'))//array
                {
                    return ParseDoubleArray(input);
                }

                //must be a number or bool, otherwise
                bool boolInput;
                if (bool.TryParse(input, out boolInput))
                {
                    return boolInput;
                }

                double doubleValue = ParseDouble(input);
                int intValue = (int)doubleValue;

                //if the int values is equal to the double value, then they must be whole numbers
                if(intValue == doubleValue)
                {
                    return intValue;
                } else
                {
                    return doubleValue;
                }
            }
        }

        public static string ParseInput(string input)
        {
            return Function.ReplaceFunctions(Function.ReplaceConstants(Sandbox.ReplaceVariables(input)));
        }

        public static Expr ParseExpression(string str)
        {
            try
            {
                return Expr.Parse(str);
            }
            catch
            {
                return Expr.Zero;
            }
        }

        public static string ParseString(string str)
        {
            if (str.Length > 0)
            {
                if (str[0] == '"' && str[str.Length - 1] == '"') str = str.Remove(0, 1).Remove(str.Length - 1, 1);
                else str = string.Empty;
            }

            return str;
        }

        public static double ParseDouble(string str)
        {
            bool outBool;
            if(bool.TryParse(str, out outBool))
            {
                return outBool == true ? 1 : 0;
            }

            try
            {
                return ParseExpression(str).Compile("")(0);
            } catch
            {
                return 0.0;
            }
        }

        public static string[] ParseArgs(string str)
        {
            if (str.Length == 0) return new string[0];

            List<string> args = new List<string>();

            StringBuilder current = new StringBuilder();

            int arrayDepth = 0;
            int stringDepth = 0;
            int exprDepth = 0;
            int parenDepth = 0;

            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];

                if (c == '"')
                {
                    stringDepth = (stringDepth + 1) % 2;
                }
                else if (c == '{')
                {
                    exprDepth++;
                } else if (c == '}')
                {
                    exprDepth--;
                }
                else if (c == '(')
                {
                    parenDepth++;
                    current.Append(c);
                }
                else if (c == ')')
                {
                    parenDepth--;
                    current.Append(c);
                }
                else if (stringDepth == 0 && exprDepth == 0 && parenDepth == 0)
                {
                    if (c == '[')
                    {
                        arrayDepth++;
                        current.Append(c);
                    }
                    else if (c == ']')
                    {
                        arrayDepth--;
                        current.Append(c);
                    }
                    else if (c == ',' && arrayDepth == 0)
                    {
                        //not in array, so split
                        args.Add(current.ToString());
                        current.Clear();
                    }
                    else
                    {
                        //either in an array with a comma, OR in the middle of an arg
                        current.Append(c);
                    }
                }
                else
                {
                    //in a string or expression, so append it all baby
                    current.Append(c);
                }
                
            }

            //add the last argument
            if(current.Length > 0)
            {
                args.Add(current.ToString());
            }

            return args.ToArray();
        }

        public static double[] ParseDoubleArray(string str)
        {
            //first, get the values as strings
            string[] strValues = str.Replace("[", "").Replace("]", "").Split(',');

            //then, put them in a double array
            double[] values = new double[strValues.Length];

            for (int i = 0; i < values.Length; i++)
            {
                values[i] = ParseDouble(strValues[i]);
            }

            return values;
        }

        public static int ParseInt(string str)
        {
            return (int)ParseDouble(str);
        }

        public static bool ParseBool(string str)
        {
            bool output;

            if(bool.TryParse(str, out output))
            {
                return output;
            }

            return false;
        }

        public static bool IsEquation(Expr expression)
        {
            try
            {
                expression.Compile();

                //no errors thrown, must be valid
                return true;
            }
            catch
            {
                //error thrown, there must be a variable
                return false;
            }
        }

        #endregion

        #region Input

        public static object EnterAnything()
        {
            Console.Write(IN_CHAR);
            return ReadAnything();
        }

        public static Expr EnterExpression()
        {
            Console.Write(IN_CHAR);
            return ReadExpression();
        }

        public static string EnterString()
        {
            Console.Write(IN_CHAR);
            return ReadString();
        }

        public static int EnterInt()
        {
            Console.Write(IN_CHAR);
            return ReadInt();
        }

        public static double EnterDouble()
        {
            Console.Write(IN_CHAR);
            return ReadDouble();
        }

        public static bool EnterBool()
        {
            Console.Write(IN_CHAR);
            return ReadBool();
        }

        public static bool EnterYesNo(string prompt)
        {
            return EnterChoiceIndex(new string[] { "Yes", "No" }, prompt, 0) == 0;//if canceled or No, this will return false
        }

        public static T EnterChoice<T>(T[] options, int startIndex = 0)
        {
            int index = EnterChoiceIndex(options, startIndex);

            if(index >= 0)
            {
                return options[index];
            } else
            {
                return default;
            }
        }

        public static int EnterChoiceIndex<T>(T[] options, int startIndex = 0) => EnterChoiceIndex(options, "", startIndex);
        public static int EnterChoiceIndex<T>(T[] options, string prompt, int startIndex)
        {
            //cannot pick from an empty list
            if (options.Length == 0) return -1;

            int selectedIndex = startIndex;
            int optionCount = options.Length;

            ConsoleKey key;

            Console.Clear();

            if (!string.IsNullOrWhiteSpace(prompt))
            {
                Print(prompt);
            }

            PrintLine();

            int offset = Console.CursorTop;

            //first, print all of the options to the screen
            for (int i = 0; i < options.Length; i++)
            {
                Console.WriteLine("  " + options[i].ToString());
            }

            PrintLine();
            Print("Use the arrow keys to select an option.", 1);
            Print("Press Enter when you are done, or Escape to cancel.", 1);

            //print the initial >
            Console.SetCursorPosition(0, selectedIndex + offset);
            Console.Write(">");

            do
            {
                key = ReadKey();

                //handle if up or down is pressed
                if (key == ConsoleKey.UpArrow || key == ConsoleKey.DownArrow ||
                    key == ConsoleKey.PageUp || key == ConsoleKey.PageDown)
                {
                    //clear current position
                    Console.SetCursorPosition(0, selectedIndex + offset);
                    Console.Write(" ");

                    switch (key)
                    {
                        case ConsoleKey.UpArrow:
                            selectedIndex = (selectedIndex + optionCount - 1) % optionCount;
                            break;
                        case ConsoleKey.DownArrow:
                            selectedIndex = (selectedIndex + 1) % optionCount;
                            break;
                        case ConsoleKey.PageUp:
                            if (selectedIndex == 0)
                                selectedIndex = optionCount - 1;
                            else
                                selectedIndex = Math.Max(0, selectedIndex - 10);
                            break;
                        case ConsoleKey.PageDown:
                            if (selectedIndex == optionCount - 1)
                                selectedIndex = 0;
                            else
                                selectedIndex = Math.Min(optionCount - 1, selectedIndex + 10);
                            break;
                    }

                    //write new position
                    //clear current position
                    Console.SetCursorPosition(0, selectedIndex + offset);
                    Console.Write(">");
                } else if (key == ConsoleKey.Escape)
                {
                    return -1;
                }

            } while (key != ConsoleKey.Enter);

            Console.Clear();

            return selectedIndex;
        }

        public static double[] EnterDoubleArray(int length)
        {
            Console.Clear();

            //print the empty array at the top of the screen
            Console.WriteLine("[" + new string(',', length - 1) + "]");

            double[] values = new double[length];

            int numberEntered = 0;

            do
            {
                double input = EnterDouble();

                values[numberEntered++] = input;

                //rewrite array
                Console.SetCursorPosition(0, 0);
                Console.Write("[");
                for (int i = 0; i < length; i++)
                {
                    if (i < numberEntered)
                    {
                        Console.Write(values[i]);
                    }

                    if (i < length - 1)
                    {
                        if (i < numberEntered - 1)
                        {
                            Console.Write(", ");
                        }
                        else
                        {
                            Console.Write(",");
                        }
                    }
                }
                Console.Write("]");

                //clear written text
                Console.SetCursorPosition(0, 1);
                Console.Write(new string(' ', input.ToString().Length + 1));

                Console.SetCursorPosition(0, 1);
            } while (numberEntered < length);

            return values;
        }

        #endregion
    }
}

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

        public static Expr ReadExpression()
        {
            return Parse.ParseExpression(Console.ReadLine());
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
            return Parse.EvaluateExpression(ReadString());
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
            return Parse.ParseBool(ReadString());
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

        public static object EvaluateInput(string input)
        {
            if (input.Contains("=="))//comparison operator
            {
                string[] parts = input.Split("==");

                object original = Parse.ParseAnything(parts[0]);

                //compare all of the following to the original object
                for (int i = 1; i < parts.Length; i++)
                {
                    //if not equal, return now
                    if (!original.Equals(Parse.ParseAnything(parts[i])))
                    {
                        return false;
                    }
                }

                //if it made it this far, it is equal
                return true;
            } else if (input.Contains('='))//assignment operator
            {
                string[] parts = input.Split('=');

                if(parts.Length > 2)
                {
                    PrintError("You can only use one assignment operator (=) in one line. (Input.EvaluateInput)");
                    return null;
                }

                string name = parts[0].Trim();

                object value = Parse.ParseAnything(parts[1]);

                Sandbox.SetVariable(name, value);

                return value;
            } else
            {
                return Parse.ParseAnything(input);
            }
        }

        public static void FakeInput(string output)
        {
            Console.WriteLine(IN_CHAR + output);
        }

        #region Input

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

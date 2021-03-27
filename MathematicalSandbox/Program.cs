using System;
using System.IO;

using static MathematicalSandbox.Input;
using static MathematicalSandbox.Output;
using static MathematicalSandbox.Parse;

namespace MathematicalSandbox
{
    class Program
    {
        private static SaveData saveData;

        private static void Main(string[] args)
        {
            Console.Title = "Mathematical Sandbox";
            Console.CursorVisible = false;
            Console.SetWindowSize(150, 45);
            Console.TreatControlCAsInput = false;

            saveData = SaveData.Load();
            saveData.Update();
            Sandbox.LoadVariables(saveData);

            //if the background has a color, this will color it all that color
            Output.Clear();
            
            Intro();

            MainLoop();

            Output.Clear();

            Sandbox.SaveVariables(saveData);
            saveData.Save();
        }

        private static void Intro()
        {
            Console.WriteLine(File.ReadAllText("Intro.txt"));

            Output.PrintLine();

            Input.WaitForEnter();

            Output.Clear();
        }

        private static void Help()
        {
            Console.WriteLine(File.ReadAllText("Help.txt"));

            Output.PrintLine();

            Input.WaitForEnter();

            Output.Clear();
        }

        private static void Documentation()
        {
            Clear();

            int categoryChoice = EnterChoiceIndex(Enum.GetNames(typeof(CategoryType)));

            while (categoryChoice >= 0)
            {
                CategoryType category = (CategoryType)(categoryChoice);

                string[] funcs = Function.GetFunctionNames(category);

                int funcChoiceIndex = EnterChoiceIndex(funcs);

                while (funcChoiceIndex >= 0)
                {
                    string funcChoice = funcs[funcChoiceIndex];

                    Variable(funcChoice);

                    funcChoiceIndex = EnterChoiceIndex(funcs, funcChoiceIndex);
                }

                Output.Clear();

                categoryChoice = EnterChoiceIndex(Enum.GetNames(typeof(CategoryType)), categoryChoice);
            }
        }

        private static void Variable(string funcChoice)
        {
            Clear();

            System.Reflection.MethodInfo mi = Function.GetFunctionFromFullName(funcChoice);

            PrintFunctionInfo(mi);

            PrintLine(2);

            string choice = EnterChoice(new string[] { "Use", "Back" }, 1);

            switch (choice)
            {
                case "Use":
                    UseFunction(funcChoice);
                    break;
                case "Back":
                    return;
            }
        }

        private static void UseFunction(string funcChoice)
        {
            //get the function itself
            System.Reflection.MethodInfo mi = Function.GetFunctionFromFullName(funcChoice);

            string funcName = mi.Name;

            //get the parameters so we know what to ask
            System.Reflection.ParameterInfo[] pis = mi.GetParameters();

            object[] args = new object[pis.Length];

            Clear();

            //LINE 1: Preview of function
            //LINE 2: Instructions
            //LINE 3: Input

            int argsEntered = 0;

            while(argsEntered < args.Length)
            {
                Type t = pis[argsEntered].ParameterType;

                Console.SetCursorPosition(0, 0);
                PrintPartialFunction(funcName, args);

                Console.SetCursorPosition(0, 1);
                Console.Write(string.Format("Enter a {0}.{1}", t.Name.ToLower(), new string(' ', 30)));//write the instructions

                Console.SetCursorPosition(0, 2);
                
                object input = null;

                if(t == typeof(string))
                {
                    input = EnterString();
                } else if (t == typeof(MathNet.Symbolics.SymbolicExpression))
                {
                    input = EnterExpression();
                } else if (t.IsArray)
                {
                    input = ParseDoubleArray(EnterString());
                } else if (t == typeof(int))
                {
                    input = EnterInt();
                } else if (t == typeof(double))
                {
                    input = EnterDouble();
                } else if (t == typeof(bool))
                {
                    input = EnterBool();
                } else
                {
                    input = EnterString();
                }

                args[argsEntered++] = input;

                Console.SetCursorPosition(0, 2);
                Console.Write(new string(' ', input.ToString().Length + 1));
            }

            //input is done, print the output info and then go into sanbox mode
            Clear();

            object output = Function.EvaluateFunction(funcName, args);

            Sandbox.SetVariable("ans", output);

            PrintPartialFunction(funcName, args);
            PrintLine();
            Print(string.Format("ans = {0}", output));
            Print("Use ans to refer to your answer.");
            PrintLine(2);

            Sandbox.SandboxLoop(false);
        }

        private static void Settings()
        {
            Clear();

            string[] options = new string[] { "Text Color", "Background Color", "Debug Mode", "Delete All Variables", "Reset Settings", "Back" };

            int optionsChoice = EnterChoiceIndex(options);

            while (optionsChoice >= 0)
            {
                string choice = options[optionsChoice];

                switch (choice)
                {
                    case "Text Color":
                    case "Background Color":
                        bool foreColor = choice.CompareTo("Text Color") == 0;

                        string[] colors = Enum.GetNames(typeof(ConsoleColor));

                        int colorIndex = 0;
                        string currentColor = Enum.GetName(typeof(ConsoleColor), foreColor ? saveData.ForeColor : saveData.BackColor);
                        for (int i = 0; i < colors.Length; i++)
                        {
                            if (colors[i].CompareTo(currentColor) == 0) colorIndex = i;
                        }

                        colorIndex = EnterChoiceIndex(colors, colorIndex);

                        if(colorIndex >= 0)
                        {
                            ConsoleColor cc = (ConsoleColor)Enum.Parse(typeof(ConsoleColor), colors[colorIndex]);

                            //set based on whichever one the user is setting, and do not set it if it is the same color as the opposite
                            //otherwise you cannot see anything
                            if (foreColor && saveData.BackColor != cc)
                            {
                                saveData.ForeColor = cc;
                            } else if (!foreColor && saveData.ForeColor != cc)
                            {
                                saveData.BackColor = cc;
                            }
                            saveData.Update();
                        }
                        break;
                    case "Debug Mode":
                        saveData.DebugMode = EnterChoiceIndex(new string[] { "On", "Off" }, "Debug Mode", saveData.DebugMode ? 0 : 1) == 0;
                        break;
                    case "Delete All Variables":
                        if(EnterYesNo("Are you sure you want to delete all saved variables?"))
                        {
                            Sandbox.ClearVariables();
                        }
                        break;
                    case "Reset Settings":
                        if (EnterYesNo("Are you sure you want to reset all settings?"))
                        {
                            saveData.ResetSettings();
                            saveData.Update();
                        }
                        break;
                    case "Back":
                        return;
                }

                optionsChoice = EnterChoiceIndex(options, optionsChoice);
            }
        }

        private static void MainLoop()
        {
            int choiceIndex = 0;
            string[] choices = new string[] { "Sandbox", "Variables", "Help", "Documentation", "Settings", "Quit" };

            choiceIndex = EnterChoiceIndex(choices, choiceIndex);

            while (choiceIndex >= 0)
            {
                Console.CursorVisible = false;

                string choice = choices[choiceIndex];

                switch (choice)
                {
                    case "Sandbox":
                        Sandbox.SandboxLoop(true);
                        break;
                    case "Variables":
                        Sandbox.VariableLoop();
                        break;
                    case "Help":
                        Help();
                        break;
                    case "Documentation":
                        Documentation();
                        break;
                    case "Settings":
                        Settings();
                        break;
                    case "Quit":
                        return;
                    default://null choice
                        return;
                }

                Console.Clear();

                choiceIndex = EnterChoiceIndex(choices, choiceIndex);
            }
        }
    }
}

using System;
using System.IO;

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
            Output.Clear();

            int categoryChoice = Input.EnterChoiceIndex(Enum.GetNames(typeof(CategoryType)));

            while (categoryChoice >= 0)
            {
                CategoryType category = (CategoryType)(categoryChoice);

                string[] funcs = Function.GetFunctionNames(category);

                int funcChoiceIndex = Input.EnterChoiceIndex(funcs);

                while (funcChoiceIndex >= 0)
                {
                    string funcChoice = funcs[funcChoiceIndex];

                    Output.Clear();

                    Output.PrintMethodInfo(Function.GetFunctionFromFullName(funcChoice));

                    Output.PrintLine(2);

                    Input.WaitForEnter();

                    funcChoiceIndex = Input.EnterChoiceIndex(funcs, funcChoiceIndex);
                }

                Output.Clear();

                categoryChoice = Input.EnterChoiceIndex(Enum.GetNames(typeof(CategoryType)), categoryChoice);
            }
        }

        private static void Settings()
        {
            Output.Clear();

            string[] options = new string[] { "Text Color", "Background Color", "Delete All Variables", "Reset Settings" };

            int optionsChoice = Input.EnterChoiceIndex(options);

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

                        colorIndex = Input.EnterChoiceIndex(colors, colorIndex);

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
                    case "Delete All Variables":
                        if(Input.EnterYesNo("Are you sure you want to delete all saved variables?"))
                        {
                            Sandbox.ClearVariables();
                        }
                        break;
                    case "Reset Settings":
                        if (Input.EnterYesNo("Are you sure you want to reset all settings?"))
                        {
                            saveData.ResetSettings();
                            saveData.Update();
                        }
                        break;
                }

                optionsChoice = Input.EnterChoiceIndex(options, optionsChoice);
            }
        }

        private static void MainLoop()
        {
            int choiceIndex = 0;
            string[] choices = new string[] { "Sandbox", "Variables", "Help", "Documentation", "Settings", "Quit" };

            choiceIndex = Input.EnterChoiceIndex(choices, choiceIndex);

            while (choiceIndex >= 0)
            {
                string choice = choices[choiceIndex];

                switch (choice)
                {
                    case "Sandbox":
                        Sandbox.SandboxLoop();
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

                choiceIndex = Input.EnterChoiceIndex(choices, choiceIndex);
            }
        }
    }
}

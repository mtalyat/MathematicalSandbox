using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Symbolics;
using MathNet.Numerics;
using Expr = MathNet.Symbolics.SymbolicExpression;
using static MathematicalSandbox.Input;
using static MathematicalSandbox.Output;
using static MathematicalSandbox.Function;

namespace MathematicalSandbox
{
    static class Sandbox
    {
        #region Variables

        private static Dictionary<string, object> variables = new Dictionary<string, object>();

        public static void LoadVariables(SaveData sd)
        {
            variables = new Dictionary<string, object>(sd.Variables);
        }

        public static void SaveVariables(SaveData sd)
        {
            sd.Variables = new Dictionary<string, object>(variables);
        }

        public static void SetVariable(string name, object value)
        {
            if (variables.ContainsKey(name))
            {
                variables[name] = value;
            }
            else
            {
                variables.Add(name, value);
            }
        }

        public static object GetVariable(string name)
        {
            if (variables.ContainsKey(name))
            {
                return variables[name];
            }

            return null;
        }

        private static string[] GetVariables()
        {
            string[] vars = new string[variables.Count];

            for (int i = 0; i < vars.Length; i++)
            {
                var pair = variables.ElementAt(i);

                vars[i] = GetVariableName(pair);
            }

            return vars;
        }

        private static string GetVariableName(KeyValuePair<string, object> pair)
        {
            return $"{pair.Key} = {pair.Value}";
        }

        public static void VariableLoop()
        {
            Clear();

            string[] vars = GetVariables();

            if(vars.Length == 0)
            {
                Print("You have no variables saved.");

                PrintLine();

                WaitForEnter();
            } else
            {
                int varChoice = EnterChoiceIndex(vars);

                while (varChoice >= 0)
                {
                    var variable = variables.ElementAt(varChoice);

                    EditVariableLoop(variable);

                    vars = GetVariables();

                    varChoice = EnterChoiceIndex(vars, Math.Min(vars.Length - 1, varChoice));
                }
            }
        }

        private static void EditVariableLoop(KeyValuePair<string, object> variable)
        {
            Clear();

            string[] options = new string[] { "Rename", "Change Value", "Delete" };

            int optionsChoice = EnterChoiceIndex(options, GetVariableName(variable), 0);

            while (optionsChoice >= 0)
            {
                switch (options[optionsChoice])
                {
                    case "Rename":
                        string newName = EnterString();
                        variables.Remove(variable.Key);
                        SetVariable(newName, variable.Value);
                        variable = new KeyValuePair<string, object>(newName, variable.Value);
                        break;
                    case "Change Value":
                        SetVariable(variable.Key, EnterAnything());
                        variable = new KeyValuePair<string, object>(variable.Key, GetVariable(variable.Key));
                        break;
                    case "Delete":
                        if (EnterYesNo($"Are you sure you want to delete {variable.Key}?"))
                        {
                            variables.Remove(variable.Key);

                            return;
                        }
                        break;
                }

                optionsChoice = EnterChoiceIndex(options, GetVariableName(variable), optionsChoice);
            }
        }

        public static void ClearVariables()
        {
            variables.Clear();
        }

        #endregion

        #region Replacing

        public static string ReplaceVariables(string input)
        {
            foreach (var pair in variables)
            {
                int varIndex = input.IndexOf(pair.Key);

                //replace every occurance of this variable
                while (varIndex >= 0)
                {
                    //make sure that this belongs to this variable only
                    if ((varIndex == 0 || !char.IsLetterOrDigit(input[varIndex - 1])) && (varIndex + pair.Key.Length >= input.Length || !char.IsLetterOrDigit(input[varIndex + pair.Key.Length])))
                    {
                        //if it is, replace it
                        //handle differently if it is an array
                        if (pair.Value.GetType().IsArray)
                        {
                            input = input.Remove(varIndex, pair.Key.Length).Insert(varIndex, ArrayToString((double[])pair.Value));
                        } else
                        {
                            input = input.Remove(varIndex, pair.Key.Length).Insert(varIndex, pair.Value.ToString());
                        }
                        
                    }
                    else
                    {
                        //if not a valid instance, set the index to the next one in the string
                        varIndex = input.IndexOf(pair.Key, varIndex + 1);
                        continue;
                    }

                    varIndex = input.IndexOf(pair.Key);
                }
            }

            return input;
        }

        #endregion

        #region Sandboxing

        public static void SandboxLoop()
        {
            //show cursor, only for sandbox mode
            Console.CursorVisible = true;

            RedrawScreen();

            while (true)
            {
                object input = EnterAnything();

                if (input == null) break;

                Display(input);
                PrintLine();
            }

            Clear();

            Console.CursorVisible = false;
        }

        public static void RedrawScreen()
        {
            Clear();

            Console.SetCursorPosition(0, 0);

            PrintTitle("Sandbox Mode");

            Print("Welcome to sandbox mode. Visit the help page to learn how to use Sandbox Mode. Enter a blank line to quit.");
        }

        #endregion
    }
}

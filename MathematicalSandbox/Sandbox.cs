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
        private static List<string> history = new List<string>();

        private static bool recordNextValue = true;

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
            if (SaveData.Instance.DebugMode) Output.Print(string.Format("Setting {0} to {1}.", name, value));

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
                        SetVariable(variable.Key, EvaluateInput(EnterString()));
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

        public static string[] ReplaceVariables(string[] input)
        {
            string[] output = new string[input.Length];

            for (int i = 0; i < input.Length; i++)
            {
                String s = input[i];

                if (variables.ContainsKey(s))
                {
                    //determine what to "wrap" the variable with, if needed
                    object value = variables[s];
                    Type t = value.GetType();

                    string wrap = "";

                    if(t == typeof(string))
                    {
                        wrap = Parse.STR_OPEN_CLOSE.ToString() + Parse.STR_OPEN_CLOSE.ToString();
                    } else if (t == typeof(Expr))
                    {
                        wrap = Parse.EXPR_OPEN.ToString() + Parse.EXPR_CLOSE.ToString();
                    }

                    //something entirely different if it is an array
                    if (t.IsArray)
                    {
                        output[i] = Output.ArrayToString((double[])variables[s]);
                    } else
                    {
                        if (wrap.Length > 0)
                        {
                            output[i] = string.Format("{1}{0}{2}", variables[s].ToString(), wrap[0], wrap[1]);
                        }
                        else
                        {
                            output[i] = variables[s].ToString();
                        }
                    }
                } else
                {
                    output[i] = s;
                }
            }

            return output;
        }

        #endregion

        #region Sandboxing

        public static void SandboxLoop(bool recordHistory, string startingInput = "")
        {
            //show cursor, only for sandbox mode
            Console.CursorVisible = true;

            DrawScreen(recordHistory);

            while (true)
            {
                string input = EnterString(startingInput);
                object output = EvaluateInput(input);

                if (output == null) break;

                if (recordHistory)
                {
                    if (recordNextValue)
                    {
                        Display(output);
                        PrintLine();

                        history.Add(input);
                        history.Add(output.ToString());
                    }
                    else
                    {
                        recordNextValue = true;
                    }
                } else {
                    Display(output);
                    PrintLine();
                }
            }

            Clear();

            Console.CursorVisible = false;
        }

        public static void ClearScreen()
        {
            history.Clear();

            recordNextValue = false;

            RedrawScreen(true);
        }

        private static void DrawScreen(bool showHistory)
        {
            PrintTitle("Sandbox Mode");

            Print("Welcome to sandbox mode. Visit the help page to learn how to use Sandbox Mode. Enter a blank line to quit.");

            if (showHistory)
            {
                bool input = true;
                foreach (string line in history)
                {
                    if (input)
                    {
                        FakeInput(line);
                    }
                    else
                    {
                        Display(line);
                        PrintLine();
                    }

                    input = !input;
                }
            }
        }

        public static void RedrawScreen(bool showHistory)
        {
            Clear();

            DrawScreen(showHistory);
        }

        #endregion
    }
}

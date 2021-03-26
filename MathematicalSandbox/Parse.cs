using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Reflection;
using MathNet.Symbolics;
using Expr = MathNet.Symbolics.SymbolicExpression;

namespace MathematicalSandbox
{
    static class Parse
    {
        #region Constants

        //expression open and close
        public const char EXPR_OPEN = '{';
        public const char EXPR_CLOSE = '}';

        //string open/close
        public const char STR_OPEN_CLOSE = '"';

        //array open and close
        public const char ARR_OPEN = '[';
        public const char ARR_CLOSE = ']';

        //argument partition
        public const char ARG_PAR = ',';

        //function open and close
        public const char FUNC_OPEN = '(';
        public const char FUNC_CLOSE = ')';

        public const char ADD = '+';
        public const char SUB = '-';
        public const char DIV = '/';
        public const char MUL = '*';

        #endregion

        private static int FindNextCloseIndex(string str, int startIndex, char open, char close)
        {
            int depth = 0;

            for (int i = startIndex; i < str.Length; i++)
            {
                char c = str[i];

                if(c == open)
                {
                    depth++;
                } else if(c == close)
                {
                    if(depth <= 0)
                    {
                        return i;
                    } else
                    {
                        depth--;
                    }
                }
            }

            return str.Length - 1;
        }

        private static int FindNextSTRChar(string input, int startIndex)
        {
            for (int i = startIndex; i < input.Length; i++)
            {
                char c = input[i];

                if(c == STR_OPEN_CLOSE)
                {
                    //account for escape characters
                    if (i > 0 && input[i - 1] != '\\')
                    {
                        return i;
                    }
                }
            }

            return input.Length - 1;
        }

        //example: sin ( max ( 2 , 3 ) / 3 * pi )
        //output: 2 3 max 3 / pi * sin

        private static string[] GetTokens(string input)
        {
            List<string> tokens = new List<string>();

            StringBuilder current = new StringBuilder();

            char lastChar = '\n';

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];

                if(c == STR_OPEN_CLOSE)
                {
                    int end = FindNextSTRChar(input, i + 1);
                    tokens.Add(input.Substring(i, end - i + 1));
                    i = end;
                    continue;
                }

                if (c == ARR_OPEN)
                {
                    int end = FindNextCloseIndex(input, i + 1, ARR_OPEN, ARR_CLOSE);
                    tokens.Add(input.Substring(i, end - i + 1));
                    i = end;
                    continue;
                }

                if(c == EXPR_OPEN)
                {
                    int end = FindNextCloseIndex(input, i + 1, EXPR_OPEN, EXPR_CLOSE);
                    tokens.Add(input.Substring(i, end - i + 1));
                    i = end;
                    continue;
                }

                //ignore whitespace
                if (char.IsWhiteSpace(c)) continue;

                if (char.IsLetterOrDigit(c) || c == '.' || (c == '-' && IsOperator(lastChar)))//letter, number or decimal point
                {
                    current.Append(c);
                } else
                {
                    //must be an operator or something if it got this far
                    if(current.Length > 0)
                    {
                        tokens.Add(current.ToString());
                        current.Clear();
                    }

                    //add commas
                    tokens.Add(c.ToString());

                    //don't add commas
                    //if (c != ARG_PAR)
                    //{
                        
                    //}
                }

                lastChar = c;
            }

            //get the last part
            if (current.Length > 0)
            {
                tokens.Add(current.ToString());
            }

            return tokens.ToArray();
        }

        private static bool IsOperator(char c)
        {
            return c == ADD || c == SUB || c == DIV || c == MUL;
        }

        private static int GetOperatorPrecidence(char c)
        {
            switch (c)
            {
                case FUNC_OPEN:
                case FUNC_CLOSE:
                    return 5;
                case ARG_PAR:
                    return 4;
                //this is where exponent would be (3)
                case DIV:
                case MUL:
                    return 2;
                case ADD:
                case SUB:
                    return 1;
                default:
                    return 0;
            }
        }

        //https://en.wikipedia.org/wiki/Shunting-yard_algorithm
        private static string[] PreformShuntingYard(string[] tokensArr)
        {
            Queue<string> tokens = new Queue<string>(tokensArr);

            //then split it into an output stack and an operator stack
            Stack<string> outputs = new Stack<string>();
            Stack<string> operators = new Stack<string>();

            while (tokens.Count > 0)
            {
                string token = tokens.Dequeue();

                char c = token[0];

                if (char.IsDigit(c) || (c == '-' && token.Length > 1))//if it is a char, or a negative number (not SUB)
                {
                    outputs.Push(token);
                }
                else if (char.IsLetter(c))//functions
                {
                    operators.Push(token);
                }
                else if (IsOperator(c))//operators
                {
                    if (operators.Any())
                    {
                        char op = operators.Peek()[0];

                        //precedence difference
                        int presDiff = GetOperatorPrecidence(op) - GetOperatorPrecidence(c);

                        //true refers to "token is left associative"
                        //we're not using the ^ operator so we don't need it
                        //that's what pow() is for
                        while (IsOperator(op) && (presDiff > 0 || (presDiff == 0 && true)) && op != FUNC_OPEN)
                        {
                            outputs.Push(operators.Pop());

                            if (!operators.Any())
                            {
                                //if no operators left, break out of this loop
                                break;
                            } else
                            {
                                op = operators.Peek()[0];
                                presDiff = GetOperatorPrecidence(op) - GetOperatorPrecidence(c);
                            }
                            
                        }
                    }

                    operators.Push(token);
                }
                else if (c == ARG_PAR)
                {
                    if (operators.Any())
                    {
                        char op = operators.Peek()[0];

                        //precidence does not matter with commas. Just keep going until the next open parenthesis

                        while (IsOperator(op) && op != FUNC_OPEN)
                        {
                            outputs.Push(operators.Pop());

                            if (!operators.Any())
                            {
                                //if no operators left, break out of this loop
                                break;
                            }
                            else
                            {
                                op = operators.Peek()[0];
                            }

                        }
                    }

                    //no adding commas here
                }
                else if (c == FUNC_OPEN)
                {
                    operators.Push(token);
                }
                else if (c == FUNC_CLOSE)
                {
                    while (operators.Any() && operators.Peek()[0] != FUNC_OPEN)
                    {
                        outputs.Push(operators.Pop());
                    }

                    //operators runs out, that means there is mismatched parenthesis
                    if (!operators.Any())
                    {
                        Output.PrintError("Mismatch parenthesis. Missing an opening parenthesis. (Parse.PreformShuntingYard)");
                        return null;
                    }
                    
                    if (operators.Peek()[0] == FUNC_OPEN)
                    {
                        //discard opening parenthesis
                        operators.Pop();
                    }

                    if (operators.Any() && char.IsLetter(operators.Peek()[0]))
                    {
                        outputs.Push(operators.Pop());
                    }
                }
                else
                {
                    //something else
                    if (c == ARR_OPEN || c == EXPR_OPEN || c == STR_OPEN_CLOSE)
                    {
                        outputs.Push(token);
                    }
                }
            }

            if (tokens.Count == 0)
            {
                while (operators.Count > 0)
                {
                    //if an operator is a parenthesis, that means that there are mismatched parenthesis
                    string op = operators.Pop();

                    if (op[0] != FUNC_OPEN)
                    {
                        outputs.Push(op);
                    } else
                    {
                        Output.PrintError("Mismatch parenthesis. Missing a closing parenthesis. (Parse.PreformShuntingYard)");
                        return null;
                    }
                }
            }

            return outputs.ToArray();
        }

        private static dynamic EvaluateOperator(char oper, dynamic v1, dynamic v2)
        {
            switch (oper)
            {
                case ADD:
                    return v1 + v2;
                case SUB:
                    return v1 - v2;
                case MUL:
                    return v1 * v2;
                case DIV:
                    if(v2 != 0)
                        return v1 / v2;
                    else
                        return 0;


                default://unknown operator
                    return 0;
            }
        }

        //https://stevenpcurtis.medium.com/evaluate-reverse-polish-notation-using-a-stack-7c618c9f80c0
        private static object EvaluateShuntingYard(string[] tokensArr)
        {
            Stack<string> tokens = new Stack<string>(tokensArr);

            //have somewhere to store variables when evaluating the outputs from the shunting yard algorithm
            //using dynamic so we can convert variables as they are processed from tokens
            Stack<dynamic> operands = new Stack<dynamic>();

            while(tokens.Count > 0)
            {
                string token = tokens.Pop();

                char c = token[0];

                if(c == STR_OPEN_CLOSE)
                {
                    operands.Push(ParseString(token));
                } else if (c == EXPR_OPEN)
                {
                    operands.Push(ParseExpression(token));
                } else if (c == ARR_OPEN)
                {
                    operands.Push(ParseDoubleArray(token));
                } else if (char.IsDigit(c) || (c == '-' && token.Length > 1))
                {
                    //coult be an integer or a double
                    if (token.Contains('.'))
                    {
                        //must be a double if it has a decimal point
                        operands.Push(ParseDouble(token));
                    } else
                    {
                        //no decimal point, must be an integer
                        operands.Push(ParseInt(token));
                    }
                } else if (IsOperator(c))
                {
                    //all operators need two operands, so grab two
                    //they're backwards in the tokens, so flip them
                    dynamic v2 = operands.Pop();
                    dynamic v1 = operands.Pop();

                    operands.Push(EvaluateOperator(c, v1, v2));
                } else
                {
                    //coult be a boolean or a function, so check if it is a function first
                    MethodInfo mi = Function.GetFunction(token);

                    if(mi != null)
                    {
                        //function
                        //determine how many arguments it needs
                        int paramCount = mi.GetParameters().Length;

                        //get those arguments from the operands
                        object[] args = new object[paramCount];

                        //go backwards in the array, since they are backwards in the tokens
                        for (int i = paramCount - 1; i >= 0; i--)
                        {
                            args[i] = (object)operands.Pop();
                        }

                        //now run the function and convert the output from an object, if it has one
                        object returnValue = mi.Invoke(null, args);

                        dynamic output = 0;

                        if (returnValue != null)
                            output = Convert.ChangeType(mi.Invoke(null, args), mi.ReturnType);

                        //then add it to the operands
                        operands.Push(output);
                    } else
                    {
                        //must be a boolean
                        operands.Push(ParseBool(token));
                    }
                }
            }

            if(operands.Count > 1 || operands.Count == 0)
            {
                Output.PrintError("Parsing incomplete. (Parse.EvaluateShuntingYard)");
                return null;
            }

            return (object)operands.Pop();
        }

        
        public static object ParseAnything(string input)
        {
            //split the input into tokens
            //replace all variables with their numbers
            //then put it in an array to use with the shunting-yard algorithm
            object output = EvaluateShuntingYard(PreformShuntingYard(Function.ReplaceConstants(Sandbox.ReplaceVariables(GetTokens(input)))));

            return output;
        }

        private static string[] SplitArguments(string input)
        {
            List<string> args = new List<string>();

            int depth = 0;
            char waitFor = ' ';
            StringBuilder current = new StringBuilder();

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];

                switch (c)
                {
                    case STR_OPEN_CLOSE:
                        waitFor = STR_OPEN_CLOSE;
                        depth++;
                        break;
                    case EXPR_OPEN:
                        waitFor = EXPR_CLOSE;
                        depth++;
                        break;
                    case ARR_OPEN:
                        waitFor = ARR_CLOSE;
                        depth++;
                        break;
                    case FUNC_OPEN:
                        waitFor = FUNC_CLOSE;
                        depth++;
                        break;
                }

                if(depth > 0)
                {
                    if(c == waitFor)
                        depth--;
                } else
                {
                    //if not deep in anything, break it up here
                    if(c == ARG_PAR)
                    {
                        args.Add(current.ToString());
                        current.Clear();
                    } else
                    {
                        current.Append(c);
                    }
                }
            }

            return args.ToArray();
        }

        public static string ParseString(string input)
        {
            return input.Trim(STR_OPEN_CLOSE);
        }

        public static Expr ParseExpression(string input)
        {
            return Expr.Parse(input.TrimStart(EXPR_OPEN).TrimEnd(EXPR_CLOSE));
        }

        public static bool ParseBool(string input)
        {
            switch (input.ToLower())
            {
                case "yes":
                case "on":
                case "true":
                case "affirmative":
                case "1":
                    return true;
                default:
                    return false;
            }
        }

        public static double ParseDouble(string input)
        {
            double d;
            if (double.TryParse(input, out d))
                return d;

            return 0.0;
        }

        public static double[] ParseDoubleArray(string input)
        {
            throw new NotImplementedException();
        }

        public static int ParseInt(string input)
        {
            int i;
            if (int.TryParse(input, out i))
                return i;

            return 0;
        }

        public static double EvaluateExpression(string input)
        {
            if (input.Any(char.IsLetter))
            {
                //there is a letter (variable) that has not been dealt with, so the expression will fail
                return 0.0;
            } else
            {
                //try this, might need to do .Compile("")(0);
                return Expr.Parse(input).RealNumberValue;
            }
        }
    }
}

using System;
using System.Data; // for DataTable.Compute (simple expressions)
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace ScientificCalculator
{
    public partial class MainWindow : Window
    {
        private string input = "";
        private bool isResult = false;

        public MainWindow()
        {
            InitializeComponent();
            AttachButtonHandlers();
        }

        private void AttachButtonHandlers()
        {
            foreach (var child in LogicalTreeHelper.GetChildren(this))
            {
                if (child is Grid grid)
                {
                    foreach (var item in grid.Children)
                    {
                        if (item is UniformGrid ug)
                        {
                            foreach (var btn in ug.Children)
                            {
                                if (btn is Button b)
                                {
                                    string content = b.Content.ToString();
                                    if (content == "=")
                                        b.Click += Equals_Click;
                                    else
                                        b.Click += Button_Click;
                                }
                            }
                        }
                    }
                }
            }
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string value = (string)((Button)sender).Content;

            // Only clear input if we're starting a new calculation after getting a result
            if (isResult && (char.IsDigit(value[0]) || value == "." || value == "(" || value == "π" || value == "e"))
            {
                input = "";
                isResult = false;
            }
            // Reset isResult flag for any button press (except when starting new number)
            else if (isResult)
            {
                isResult = false;
            }

            switch (value)
            {
                case "CE":
                    input = "";
                    break;
                case "⌫":
                    if (input.Length > 0)
                        input = input.Substring(0, input.Length - 1);
                    break;
                case "±":
                    if (!string.IsNullOrWhiteSpace(input))
                    {
                        if (input.StartsWith("-"))
                            input = input.Substring(1);
                        else
                            input = "-" + input;
                    }
                    break;
                case "π":
                    input += Math.PI.ToString(CultureInfo.InvariantCulture);
                    break;
                case "e":
                    input += Math.E.ToString(CultureInfo.InvariantCulture);
                    break;
                case "x²":
                    input += "^2";
                    break;
                case "√x":
                    input += "sqrt(";
                    break;
                case "∛x":
                    input += "cbrt(";
                    break;
                case "1/x":
                    input = $"1/({input})";
                    break;
                case "xʸ":
                    input += "^";
                    break;
                case "10ˣ":
                    input += "10^";
                    break;
                case "log":
                case "ln":
                case "sin":
                case "cos":
                case "tan":
                case "asin":
                case "acos":
                case "atan":
                case "exp":
                case "abs":
                    input += value + "(";
                    break;
                case "n!":
                    input += "!";
                    break;
                case "|x|":
                    input += "abs(";
                    break;
                // Fixed: Handle the Unicode symbols from XAML
                case "÷":
                    input += "/";
                    break;
                case "×":
                    input += "*";
                    break;
                case "−":  // Unicode minus sign
                    input += "-";
                    break;
                case "mod":
                    input += "%";
                    break;
                case "2ⁿᵈ":
                    // Handle 2nd function button - you might want to implement mode switching here
                    // For now, just ignore it
                    break;
                default:
                    input += value;
                    break;
            }

            Display.Text = input;
        }

        private void Equals_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                double result = EvaluateExpression(input);
                Display.Text = result.ToString(CultureInfo.InvariantCulture);
                input = result.ToString(CultureInfo.InvariantCulture);
                isResult = true;
            }
            catch
            {
                Display.Text = "Error";
                input = "";
            }
        }

        private double EvaluateExpression(string expr)
        {
            return new ExpressionParser().Parse(expr);
        }
    }

    class ExpressionParser
    {
        private int pos = -1, ch;
        private string expr;

        public double Parse(string expression)
        {
            expr = expression;
            NextChar();
            double x = ParseExpression();
            if (pos < expr.Length)
                throw new Exception("Unexpected character: " + (char)ch);
            return x;
        }

        void NextChar() => ch = (++pos < expr.Length) ? expr[pos] : -1;
        bool Eat(int charToEat)
        {
            while (ch == ' ') NextChar();
            if (ch == charToEat)
            {
                NextChar();
                return true;
            }
            return false;
        }

        double ParseExpression()
        {
            double x = ParseTerm();
            while (true)
            {
                if (Eat('+')) x += ParseTerm();
                else if (Eat('-')) x -= ParseTerm();
                else return x;
            }
        }

        double ParseTerm()
        {
            double x = ParseFactor();
            while (true)
            {
                if (Eat('*')) x *= ParseFactor();
                else if (Eat('/')) x /= ParseFactor();
                else if (Eat('%')) x = x % ParseFactor(); // Added modulo support
                else return x;
            }
        }

        double ParseFactor()
        {
            if (Eat('+')) return ParseFactor();
            if (Eat('-')) return -ParseFactor();

            double x = 0;
            int startPos = this.pos;

            if (Eat('('))
            {
                x = ParseExpression();
                Eat(')');
            }
            else if ((ch >= '0' && ch <= '9') || ch == '.')
            {
                while ((ch >= '0' && ch <= '9') || ch == '.') NextChar();
                x = double.Parse(expr.Substring(startPos, pos - startPos), CultureInfo.InvariantCulture);
            }
            else if (ch >= 'a' && ch <= 'z')
            {
                while (ch >= 'a' && ch <= 'z') NextChar();
                string func = expr.Substring(startPos, pos - startPos);
                x = ParseFactor();

                switch (func)
                {
                    case "sqrt": x = Math.Sqrt(x); break;
                    case "cbrt": x = Math.Pow(x, 1.0 / 3.0); break;
                    case "sin": x = Math.Sin(DegToRad(x)); break;
                    case "cos": x = Math.Cos(DegToRad(x)); break;
                    case "tan": x = Math.Tan(DegToRad(x)); break;
                    case "asin": x = RadToDeg(Math.Asin(x)); break;
                    case "acos": x = RadToDeg(Math.Acos(x)); break;
                    case "atan": x = RadToDeg(Math.Atan(x)); break;
                    case "log": x = Math.Log10(x); break;
                    case "ln": x = Math.Log(x); break;
                    case "exp": x = Math.Exp(x); break;
                    case "abs": x = Math.Abs(x); break;
                    default: throw new Exception("Unknown function: " + func);
                }
            }
            else
            {
                throw new Exception("Unexpected: " + (char)ch);
            }

            if (Eat('^')) x = Math.Pow(x, ParseFactor());
            if (Eat('!')) x = Factorial((int)x);

            return x;
        }

        private double DegToRad(double deg) => deg * Math.PI / 180;
        private double RadToDeg(double rad) => rad * 180 / Math.PI;

        private int Factorial(int n)
        {
            if (n < 0) throw new Exception("Negative factorial");
            int result = 1;
            for (int i = 2; i <= n; i++) result *= i;
            return result;
        }
    }
}
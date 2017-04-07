namespace InMemoreCompilation
{
    public static class CodeGenerator
    {
        public static string GenerateCalculator()
        {
            var calculator = @"namespace Calculator
{
    public class Calculator
    {
        public int AddIntegers(int x, int y)
        {
            return x + y;
        }
    }
}";
            return calculator;
        }
    }
}

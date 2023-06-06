using System.Diagnostics.CodeAnalysis;

namespace TokiwadaiPride.Types
{
    public class Expense
    {
        public DateTime Date = DateTime.Now;

        [DisallowNull]
        public string Name = "";

        public double Cost;

        public override string ToString()
        {
            return $"{Date.ToString("D")} {Name}, {Cost}";
        }
    }
}

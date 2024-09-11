using TokiwadaiPride.Contract.Types;

namespace TokiwadaiPride.Bot.Helpers;

internal class ExpenseFormatter
{
    private static readonly string DateColumn = "Date";

    private static readonly string NameColumn = "Name";

    private static readonly string CostColumn = "Cost";

    public static string Format(List<Expense> expenses)
    {
        if (expenses == null || expenses.Count == 0)
            return "";
        
        var unformatted = expenses.Select(x => (x.Date.ToString("dd MMM"), x.Name, $"{x.Cost:0.00}"));

        int dateColumnLength = Math.Max(unformatted.Max(x => x.Item1.Length), 6);
        int nameColumnLength = Math.Max(unformatted.Max(x => x.Name.Length), 18);
        int costColumnLength = Math.Max(unformatted.Max(x => x.Item3.Length), 10);

        var result = $"<pre>\n|{DateColumn.PadRight(dateColumnLength)}|{NameColumn.PadRight(nameColumnLength)}|{CostColumn.PadRight(costColumnLength)}|\n";
        result += "\n".PadLeft(result.Length * 9 / 10, '-');

        return result + string.Join("\n", unformatted.Select(x => $"|{x.Item1.PadRight(dateColumnLength).Substring(0, 6)}|{x.Name.PadRight(nameColumnLength).Substring(0, 18)}|{x.Item3.PadRight(costColumnLength).Substring(0, 10)}|")) + "\n</pre>";
    }
    
    public static string Format(ExpensesStatistics expensesStatistics)
    {
        string dateFormat = "dd MMMM HH:mm";
        return $"Всего с {expensesStatistics.From.ToString(dateFormat)} по {expensesStatistics.To.ToString(dateFormat)} потрачено {expensesStatistics.Total:0.00}.\n" +
            $"Исключая выше {expensesStatistics.BigFilter}: {expensesStatistics.TotalWithoutBig:0.00}.\n\n" +
            $"Топ 10 трат:\n{Format(expensesStatistics.Top10)}";
        }
}

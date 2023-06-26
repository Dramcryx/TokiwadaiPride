using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace TokiwadaiPride.Types;
public class ExpensesStatistics
{
    public double Total { get; private set; }

    public double TotalWithoutBig { get; private set; }

    public DateTime From { get; private set; }

    public DateTime To { get; private set; }

    public List<Expense> Top10 { get; private set; }

    private double _bigFilter = 0.0;

    private ExpensesStatistics()
    {
        Total = 0;
        TotalWithoutBig = 0;
        From = DateTime.MaxValue;
        To = DateTime.MinValue;
        Top10 = new List<Expense>();
    }

    public static Task<ExpensesStatistics> CalculateAsync(IEnumerable<Expense> expenses, double bigFilter)
    {
        return Task.Run(
            () => {
                var result = new ExpensesStatistics();

                result._bigFilter = bigFilter;

                var top10Expenses = new PriorityQueue<Expense, double>(10, new ExpenseDescendingComparer());

                foreach (var expense in expenses)
                {
                    top10Expenses.Enqueue(expense, expense.Cost);

                    if (top10Expenses.Count > 10)
                    {
                        top10Expenses.Dequeue();
                    }

                    if (expense.Date < result.From)
                    {
                        result.From = expense.Date;
                    }

                    if (expense.Date >= result.To)
                    {
                        result.To = expense.Date;
                    }

                    result.Total += expense.Cost;

                    if (expense.Cost - bigFilter < double.Epsilon)
                    {
                        result.TotalWithoutBig += expense.Cost;
                    }
                }

                while (top10Expenses.Count > 0)
                {
                    result.Top10.Add(top10Expenses.Dequeue());
                }

                result.Top10.Reverse();

                return result;
            });
    }

    public override string ToString()
    {
        string dateFormat = "dd MMMM HH:mm";
        return
            $"Всего с {From.ToString(dateFormat)} по {To.ToString(dateFormat)} потрачено {Total}.\n" +
            $"Исключая выше {_bigFilter}: {TotalWithoutBig}.\n\n" +
            $"Топ 10 трат:\n{string.Join("\n", Top10.Select(x => x.ToString()))}";
    }
}

internal class ExpenseDescendingComparer : IComparer<double>
{
    public int Compare(double x, double y)
    {
        return x >= y ? 1 : -1;
    }
}

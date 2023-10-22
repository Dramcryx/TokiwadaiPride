namespace TokiwadaiPride.Contract.Requests;

public class AddExpenseRequest
{
    public DateTime Date { get; set; }

    public string Name { get; set; } = "";

    public double Expense { get; set; }
}

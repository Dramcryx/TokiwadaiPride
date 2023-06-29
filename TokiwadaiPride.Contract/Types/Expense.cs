﻿namespace TokiwadaiPride.Contract.Types;

public class Expense
{
    public DateTime Date { get; set; } = DateTime.Now;

    public string Name { get; set; } = string.Empty;

    public double Cost { get; set; } = 0.0;

    public override string ToString()
    {
        return $"{Date.ToString("D")} {Name}, {Cost.ToString("0.00")}";
    }
}

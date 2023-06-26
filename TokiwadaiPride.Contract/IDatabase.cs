using Microsoft.Data.Sqlite;

namespace TokiwadaiPride.Contract;

public interface IDatabase
{
    Task<SqliteDataReader> SelectAllExpensesAsync();

    Task<SqliteDataReader> SelectExpensesForDatesAsync(DateTime from, DateTime to);

    Task<bool> InsertExpenseAsync(DateTime date, string name, double expense);

    Task<SqliteDataReader?> DeleteLastAsync();
}

using Microsoft.Data.Sqlite;

namespace TokiwadaiPride.Database.Services;

public class DatabaseAdapterService
{
    private readonly ILogger _logger;
    private Dictionary<long, Database> _databasesForChats = new Dictionary<long, Database>();

    public DatabaseAdapterService(ILogger<DatabaseAdapterService> logger)
    {
        _logger = logger;
    }

    public async Task<SqliteDataReader?> DeleteLastAsync(long chatId)
    {
        _logger.LogInformation($"Querying deletion of last entry for {chatId}");
        return await GetForUser(chatId).DeleteLastAsync();
    }

    public async Task<bool> InsertExpenseAsync(long chatId, DateTime date, string name, double expense)
    {
        _logger.LogInformation($"Querying insertion of new entry for {chatId}");
        return await GetForUser(chatId).InsertExpenseAsync(date, name, expense);
    }

    public async Task<SqliteDataReader> SelectAllExpensesAsync(long chatId)
    {
        _logger.LogInformation($"Querying all entries for {chatId}");
        return await GetForUser(chatId).SelectAllExpensesAsync();
    }

    public async Task<SqliteDataReader> SelectExpensesForDatesAsync(long chatId, DateTime from, DateTime to)
    {
        _logger.LogInformation($"Querying entries for {chatId} between {from} and {to}");
        return await GetForUser(chatId).SelectExpensesForDatesAsync(from, to);
    }

    public async Task<SqliteDataReader?> SearchExpensesAsync(long chatId, string text, DateTime? from, DateTime? to)
    {
        if ((from == null) != (to == null))
            throw new ArgumentException("Both dates must be null or not null at the same time");

        _logger.LogInformation(from == null
            ? $"Querying entries for {chatId} with text {text}"
            : $"Querying entries for {chatId} between {from} and {to} with text {text}");
        return await GetForUser(chatId).SearchExpensesAsync(text, from, to);
    }

    private Database GetForUser(long chatId)
    {
        _logger.LogInformation($"Get database for {chatId}");
        if (!_databasesForChats.ContainsKey(chatId))
        {
            _logger.LogInformation($"Database object does not exist for {chatId}, creating one");
            _databasesForChats.Add(chatId, new Database(chatId));
        }
        return _databasesForChats[chatId];
    }
}

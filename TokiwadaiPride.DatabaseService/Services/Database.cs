using Microsoft.Data.Sqlite;

namespace TokiwadaiPride.DatabaseService.Services;

internal class Database
{
    private static readonly string MainTable = "entries";
    private static readonly string IdColumn = "id";
    private static readonly string DateColumn = "date";
    private static readonly string NameColumn = "name";
    private static readonly string CostColumn = "cost";

    private static readonly int Limit = 1000;

    private readonly ILogger<Database> _logger;
    private long _chatId;
    private SqliteConnection _connection;

    public Database(long chatId)
    {
        _logger = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
        }).CreateLogger<Database>();

        _chatId = chatId;

        var databasesDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".tokiwadai-pride");
        if (!Directory.Exists(databasesDir))
        {
            Directory.CreateDirectory(databasesDir);
        }

        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".tokiwadai-pride", $"{_chatId}.db");

        _connection = new SqliteConnection($"Data Source={path}");
        _connection.Open();

        var createCommand = _connection.CreateCommand();
        createCommand.CommandText =
            $@"
                CREATE TABLE IF NOT EXISTS {MainTable} (
                    {IdColumn} INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                    {DateColumn} TEXT NOT NULL,
                    {NameColumn} TEXT NOT NULL,
                    {CostColumn} DOUBLE NOT NULL
                );
            ";
        createCommand.ExecuteNonQuery();

        _logger.LogInformation($"Создал базу для пользователя {_chatId}");
    }

    public async Task<SqliteDataReader> SelectAllExpensesAsync()
    {
        _logger.LogInformation($"Запрос всех записей для {_chatId}");
        var getExpensesCommand = _connection.CreateCommand();
        getExpensesCommand.CommandText =
             $@"
                SELECT {DateColumn}, {NameColumn}, {CostColumn}
                FROM {MainTable} LIMIT {Limit};
             ";
        return await getExpensesCommand.ExecuteReaderAsync();
    }

    public async Task<SqliteDataReader> SelectExpensesForDatesAsync(DateTime from, DateTime to)
    {
        _logger.LogInformation($"Запрос записей для {_chatId} c {from} по {to}");
        var getExpensesCommand = _connection.CreateCommand();
        getExpensesCommand.CommandText =
            $@"
                SELECT {DateColumn}, {NameColumn}, {CostColumn}
                FROM {MainTable}
                WHERE {DateColumn} BETWEEN '{ToSQLiteDate(from)}' AND '{ToSQLiteDate(to)}'
                LIMIT {Limit};
            ";

        return await getExpensesCommand.ExecuteReaderAsync();
    }

    public async Task<bool> InsertExpenseAsync(DateTime date, string name, double expense)
    {
        _logger.LogInformation($"Добавить расход для {_chatId}: {date}; {name}; {expense}");

        var addExpenseCommand = _connection.CreateCommand();
        addExpenseCommand.CommandText =
            $@"
                INSERT INTO {MainTable} ({DateColumn}, {NameColumn}, {CostColumn})
                VALUES ('{ToSQLiteDate(date)}', @NAME, {expense.ToString().Replace(',', '.')});
            ";
        addExpenseCommand.Parameters.AddWithValue("@NAME", name);

        return 1 == await addExpenseCommand.ExecuteNonQueryAsync();
    }

    public async Task<SqliteDataReader?> DeleteLastAsync()
    {
        _logger.LogInformation($"Удалить последнюю запись пользователя {_chatId}");

        var getLastIdCommand = _connection.CreateCommand();
        getLastIdCommand.CommandText =
            $@"
                SELECT MAX({IdColumn}) FROM {MainTable};
            ";

        try
        {
            using (var maxIdReader = await getLastIdCommand.ExecuteReaderAsync())
            {
                maxIdReader.Read();
                int maxId = maxIdReader.GetInt32(0);

                var getLastExpense = _connection.CreateCommand();
                getLastExpense.CommandText =
                     $@"
                        SELECT {DateColumn}, {NameColumn}, {CostColumn}
                        FROM {MainTable}
                        WHERE {IdColumn} == {maxId};
                     ";
                var returnValue = await getLastExpense.ExecuteReaderAsync();

                var deleteLastExpenseCommand = _connection.CreateCommand();
                deleteLastExpenseCommand.CommandText =
                    $@"
                        DELETE FROM {MainTable} WHERE {IdColumn} == {maxId};
                    ";

                await deleteLastExpenseCommand.ExecuteNonQueryAsync();

                return returnValue;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex.ToString());
            return null;
        }
    }

    private static string ToSQLiteDate(DateTime date)
        => date.ToUniversalTime().ToString("u").Replace("Z", "");
}

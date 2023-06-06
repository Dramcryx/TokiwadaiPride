using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using TokiwadaiPride.Types;

namespace TokiwadaiPride.Database
{
    public class DatabaseClient
    {
        private readonly ILogger<DatabaseClient> _logger;

        public DatabaseClient()
        {

            _logger = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
            }).CreateLogger<DatabaseClient>();
        }

        public async Task AddExpenseAsync(long chatId, DateTime date, string name, double expense)
        {
            _logger.LogInformation($"Добавить расход пользователю {chatId}: {date}; {name}; {expense}");

            if (!(await Database.GetForUser(chatId).InsertExpenseAsync(date, name, expense))) {
                throw new ArgumentException($"Не удалось добавить запись {date};{name};{expense} для пользователя {chatId}");
            }
        }

        public async Task<IEnumerable<Expense>> GetExpensesAsync(long chatId, DateTime? date = null)
        {
            _logger.LogInformation($"Получить список расходов: {date ?? DateTime.MinValue}");

            SqliteDataReader? reader = null;
            List<Expense> expenses = new List<Expense>();
            try
            {
                if (!date.HasValue)
                {
                    reader = await Database.GetForUser(chatId).SelectAllExpensesAsync();
                }
                else
                {
                    var (dayStart, dayEnd) = GetDayRange(date.Value);
                    reader = await Database.GetForUser(chatId).SelectExpensesForDatesAsync(dayStart, dayEnd);
                }

                while (reader.Read())
                {
                    expenses.Add(new Expense
                    {
                        Date = reader.GetDateTime(0).ToLocalTime(),
                        Name = reader.GetString(1),
                        Cost = reader.GetDouble(2)
                    });
                }
            }
            finally
            {
                if (reader != null)
                {
                    reader.Dispose();
                }
            }

            return expenses;
        }

        public async Task<ExpensesStatistics> GetExpensesStatisticsForAsync(long chatId, DateTime from, DateTime to)
        {
            _logger.LogInformation($"Получить для пользователя {chatId} статистику расходов с {from} по {to}");

            var (dayStart, dayEnd) = (from.Date, GetDayRange(to).Item2);

            List<Expense> expenses = new List<Expense>();
            using (var reader = await Database.GetForUser(chatId).SelectExpensesForDatesAsync(dayStart, dayEnd))
            {
                while (reader.Read())
                {
                    expenses.Add(new Expense
                    {
                        Date = reader.GetDateTime(0).ToLocalTime(),
                        Name = reader.GetString(1),
                        Cost = reader.GetDouble(2)
                    });
                }
            }

            return await ExpensesStatistics.CalculateAsync(expenses, 20000.0);
        }

        public async Task<Expense?> DeleteLastExpenseAsync(long chatId)
        {
            _logger.LogInformation($"Удалить для пользователя {chatId} последнюю запись");

            var reader = await Database.GetForUser(chatId).DeleteLastAsync();
            if (reader != null)
            {
                using (reader)
                {
                    while (reader.Read())
                    {
                        return new Expense
                        {
                            Date = reader.GetDateTime(0).ToLocalTime(),
                            Name = reader.GetString(1),
                            Cost = reader.GetDouble(2)
                        };
                    }
                }
            }
            return null;
        }

        private static (DateTime, DateTime) GetDayRange(DateTime date)
        {
            return (date.Date, date.Date.AddDays(1).AddTicks(-1));
        }
    }
}

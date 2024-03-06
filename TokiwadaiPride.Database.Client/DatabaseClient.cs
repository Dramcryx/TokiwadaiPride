using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Net.Http.Json;
using TokiwadaiPride.Contract.Requests;
using TokiwadaiPride.Contract.Types;

namespace TokiwadaiPride.Database.Client;

public class DatabaseClient
{
    private readonly ILogger<DatabaseClient> _logger;
    private readonly HttpClient _httpClient;

    public DatabaseClient(ILogger<DatabaseClient> logger, IOptions<BotConfiguration> configuration, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;

        var hostUrl = configuration.Value.DatabaseClientUrl ?? throw new ArgumentException(nameof(configuration));

        var client = httpClientFactory.CreateClient("DatabaseClient.HttpClient");
        client.BaseAddress = new Uri(hostUrl);

        _httpClient = client;
    }

    public async Task AddExpenseAsync(long chatId, DateTime date, string name, double expense)
    {
        _logger.LogInformation($"Добавить расход пользователю {chatId}: {date}; {name}; {expense}");

        var json = JsonContent.Create(new AddExpenseRequest()
        {
            Date = date,
            Name = name,
            Expense = expense
        });

        using var response = await _httpClient.PutAsync($"{chatId}/add", json);

        if (!response.IsSuccessStatusCode) {
            throw new ArgumentException($"Не удалось добавить запись {date};{name};{expense} для пользователя {chatId}");
        }
    }

    public async Task<List<Expense>> GetExpensesAsync(long chatId, DateTime? date = null)
    {
        _logger.LogInformation($"Получить список расходов: {date ?? DateTime.MinValue}");

        using var response = await _httpClient.GetAsync($"{chatId}/all?date={date?.ToString("yyyy-MM-ddTHH:mm:ss")}");

        return await response.Content.ReadFromJsonAsync<List<Expense>>()
            ?? throw new JsonSerializationException("Нарушен контракт!");
    }

    public async Task<List<Expense>> GetExpensesForRangeAsync(long chatId, DateTime start, DateTime end)
    {
        _logger.LogInformation($"Получить список расходов: {start} - {end}");

        var uri = $"{chatId}/expenses-for-dates?start={start:yyyy-MM-ddTHH:mm:ss}&end={end:yyyy-MM-ddTHH:mm:ss}";
        using var response = await _httpClient.GetAsync(uri);

        return await response.Content.ReadFromJsonAsync<List<Expense>>()
            ?? throw new JsonSerializationException("Нарушен контракт!");
    }

    public async Task<(ExpensesStatistics, List<Expense>)> GetExpensesStatisticsForAsync(long chatId, DateTime from, DateTime to)
    {
        _logger.LogInformation($"Получить для пользователя {chatId} статистику расходов с {from} по {to}");

        var (dayStart, dayEnd) = (from.Date, GetDayRange(to).Item2);

        var uri = $"{chatId}/expenses-for-dates?start={dayStart:yyyy-MM-ddTHH:mm:ss}&end={dayEnd:yyyy-MM-ddTHH:mm:ss}";
        using var response = await _httpClient.GetAsync(uri);

        var expenses = await response.Content.ReadFromJsonAsync<List<Expense>>()
            ?? throw new JsonSerializationException("Нарушен контракт!");

        return (await ExpensesStatistics.CalculateAsync(expenses, 20000.0), expenses);
    }

    public async Task<Expense?> DeleteLastExpenseAsync(long chatId)
    {
        _logger.LogInformation($"Удалить для пользователя {chatId} последнюю запись");

        var resposne = await _httpClient.PostAsync($"{chatId}/pop", null);

        switch (resposne.StatusCode)
        {
            case System.Net.HttpStatusCode.OK:
                return await resposne.Content.ReadFromJsonAsync<Expense>()
                    ?? throw new JsonSerializationException("Нарушен контракт");
            case System.Net.HttpStatusCode.NotFound:
                return null;
            default:
                throw new HttpRequestException("Что-то упало на сервисе базы");
        }
    }

    private static (DateTime, DateTime) GetDayRange(DateTime date)
    {
        return (date.Date, date.Date.AddDays(1).AddTicks(-1));
    }
}

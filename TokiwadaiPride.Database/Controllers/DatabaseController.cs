using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using TokiwadaiPride.Contract.Requests;
using TokiwadaiPride.Contract.Types;
using TokiwadaiPride.Database.Services;

namespace TokiwadaiPride.Database.Controllers;

[ApiController]
public class DatabaseController : ControllerBase
{
    private readonly DatabaseAdapterService _databaseService;
    private readonly ILogger<DatabaseController> _logger;

    public DatabaseController(DatabaseAdapterService database, ILogger<DatabaseController> logger)
    {
        _databaseService = database ?? throw new ArgumentNullException(nameof(database));
        _logger = logger;
    }

    [HttpPut("{chatId}/add")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddExpenseAsync([FromRoute] long chatId, AddExpenseRequest request)
    {
        _logger.LogInformation($"AddExpenseAsync: {chatId}, {request}");
        return await _databaseService.InsertExpenseAsync(chatId, request.Date, request.Name, request.Expense)
            ? Ok() : BadRequest();
    }

    [HttpGet("{chatId}/all")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<Expense>))]
    public async Task<IActionResult> SelectAllExpensesAsync([FromRoute] long chatId, [FromQuery] DateTime? date)
    {
        SqliteDataReader? reader = null;
        List<Expense> expenses = new();
        try
        {
            if (!date.HasValue)
            {
                reader = await _databaseService.SelectAllExpensesAsync(chatId);
            }
            else
            {
                reader = await _databaseService.SelectExpensesForDatesAsync(chatId,
                    date.Value.Date,
                    date.Value.Date.AddDays(1).AddTicks(-1));
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

        return Ok(expenses);
    }

    [HttpGet("{chatId}/expenses-for-dates")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<Expense>))]
    public async Task<IActionResult> SelectExpensesForDatesAsync([FromRoute] long chatId, [FromQuery] DateTime start, [FromQuery] DateTime end)
    {
        SqliteDataReader? reader = null;
        List<Expense> expenses = new List<Expense>();
        try
        {
            reader = await _databaseService.SelectExpensesForDatesAsync(chatId, start, end);

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

        return Ok(expenses);
    }

    [HttpPost("{chatId}/pop")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Expense))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteLastExpenseAsync([FromRoute] long chatId)
    {
        var reader = await _databaseService.DeleteLastAsync(chatId);
        if (reader != null)
        {
            using (reader)
            {
                while (reader.Read())
                {
                    return Ok(new Expense
                    {
                        Date = reader.GetDateTime(0).ToLocalTime(),
                        Name = reader.GetString(1),
                        Cost = reader.GetDouble(2)
                    });
                }
            }
        }
        return NotFound();
    }
}

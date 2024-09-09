using Microsoft.AspNetCore.Mvc;
using TokiwadaiPride.Contract.Types;
using TokiwadaiPride.Database.Client;
using TokiwadaiPride.Redis;

namespace TokiwadaiPride.Database.Controllers;

[ApiController]
[Route("[controller]")]
public class ExpensesController : ControllerBase
{
    private readonly DatabaseClient _databaseClient;

    private readonly ILogger<ExpensesController> _logger;

    private readonly ISessionDatabase _sessionDatabase;

    public ExpensesController(DatabaseClient database, ILogger<ExpensesController> logger, ISessionDatabase sessionDatabase)
    {
        _databaseClient = database ?? throw new ArgumentNullException(nameof(database));
        _logger = logger;
        _sessionDatabase = sessionDatabase ?? throw new ArgumentNullException(nameof(sessionDatabase));
    }

    [HttpGet("{sessionId}/all")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<Expense>))]
    public async Task<IActionResult> GetAsync([FromRoute] string sessionId, [FromQuery] DateTime? date)
    {
        var chatId = await _sessionDatabase.GetChatIdAsync(sessionId);

        if (chatId == null)
        {
            return NotFound();
        }

        var expenses = await _databaseClient.GetExpensesAsync(chatId.Value, date);

        return Ok(expenses);
    }

    [HttpGet("{chatId}/expenses-for-dates")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<Expense>))]
    public async Task<IActionResult> SelectExpensesForDatesAsync([FromRoute] string sessionId, [FromQuery] DateTime start, [FromQuery] DateTime end)
    {
        var chatId = await _sessionDatabase.GetChatIdAsync(sessionId);

        if (chatId == null)
        {
            return NotFound();
        }

        return Ok(await _databaseClient.GetExpensesForRangeAsync(chatId.Value, start, end));
    }

    [HttpGet("{chatId}/search")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<Expense>))]
    public async Task<IActionResult> SearchExpensesAsync(
        [FromRoute] long chatId,
        [FromQuery] string text,
        [FromQuery] DateTime? start,
        [FromQuery] DateTime? end)
    {
        
        return Ok(await _databaseClient.SearchExpensesAsync(chatId, text, start, end));
    }
}

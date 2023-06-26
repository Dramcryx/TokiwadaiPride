using System.Collections.Generic;
using Telegram.Bot.Types;
using Telegram.Bot.Polling;

namespace TokiwadaiPride.Contract;

public interface IExpenseHandler : IUpdateHandler
{
    (BotCommandScope, IEnumerable<BotCommand>) GetCommandsConfiguration();
}

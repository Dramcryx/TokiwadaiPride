using System.Data;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using TokiwadaiPride.Bot;
using TokiwadaiPride.Bot.Database;
using TokiwadaiPride.Contract;
using TokiwadaiPride.Types;

namespace TokiwadaiPride
{
    public class ExpenseHandler : IExpenseHandler
    {
        public static readonly string StartCommand = "/start";
        public static readonly string AddExpenseCommand = "/add";
        public static readonly string AddExpenseForDateCommand = "/addon";
        public static readonly string ListExpensesCommand = "/list";
        public static readonly string GetStatisticsForCommand = "/statsfor";
        public static readonly string DeleteLastCommand = "/pop";
        public static readonly string TodayCommand = "/today";
        public static readonly string YesterdayCommand = "/yesterday";
        public static readonly string WeekCommand = "/week";
        public static readonly string MonthCommand = "/month";

        private readonly ITelegramBotClient _botClient;
        private readonly ILogger<ExpenseHandler> _logger;

        private Dictionary<string, Func<UpdateContext, CancellationToken, Task>> _commandHandlers =
            new Dictionary<string, Func<UpdateContext, CancellationToken, Task>>();

        private DatabaseClient _databaseClient = new DatabaseClient();

        public static readonly BotCommand[] Commands = new[]
            {
                new BotCommand { Command = StartCommand, Description = "Показать описание бота" },
                new BotCommand { Command = AddExpenseCommand, Description = "Добавить расход в текущий день в формате 'название с пробелами 12345*0.67'" },
                new BotCommand { Command = AddExpenseForDateCommand, Description = "Добавить расход в конкретный день в формате 'ДД.ММ.ГГГГ название с пробелами 12345*0.67'" },
                new BotCommand { Command = ListExpensesCommand, Description = "Показать расходы за дату в формате ДД.ММ.ГГГГ или за всё время, если не указано" },
                new BotCommand { Command = GetStatisticsForCommand, Description = "Показать расходы за две даты в формате ДД.ММ.ГГГГ" },
                new BotCommand { Command = DeleteLastCommand, Description = "Удалить последнюю запись" },
                new BotCommand { Command = TodayCommand, Description = "Показать расходы за сегодня" },
                new BotCommand { Command = YesterdayCommand, Description = "Показать расходы за вчера" },
                new BotCommand { Command = WeekCommand, Description = "Показать расходы c понедельника" },
                new BotCommand { Command = MonthCommand, Description = "Показать расходы с первого числа месяца" }

            };

        public ExpenseHandler(ITelegramBotClient botClient, ILogger<ExpenseHandler> logger)
        {
            _botClient = botClient;
            _logger = logger;

            _commandHandlers.Add(StartCommand, HandleStartAsync);
            _commandHandlers.Add(AddExpenseCommand, HandleAddExpenseAsync);
            _commandHandlers.Add(AddExpenseForDateCommand, HandleAddExpenseForDateAsync);
            _commandHandlers.Add(ListExpensesCommand, HandleListExpensesAsync);
            _commandHandlers.Add(GetStatisticsForCommand, HandleGetExpensesStatisticsAsync);
            _commandHandlers.Add(DeleteLastCommand, HandleDeleteLastAsync);
            _commandHandlers.Add(TodayCommand, HandleTodayAsync);
            _commandHandlers.Add(YesterdayCommand, HandleYesterayAsync);
            _commandHandlers.Add(WeekCommand, HandleWeekAsync);
            _commandHandlers.Add(MonthCommand, HandleMonthAsync);
        }

        public async Task HandleUpdateAsync(ITelegramBotClient _, Update update, CancellationToken cancellationToken)
        {
            UpdateContext handlerContext = update switch
            {
                { Message: {} message } =>
                    new UpdateContext()
                    {
                        ChatId = message.Chat.Id,
                        When = message.Date,
                        CommandArgs = message.Text
                    },
                { EditedMessage: {} message } =>
                    new UpdateContext()
                    {
                        ChatId = message.Chat.Id,
                        When = message.Date,
                        CommandArgs = message.Text
                    },
                _ => throw new ArgumentException("Как ты это вообще мне прислал?")
            };

            if (handlerContext.CommandArgs == null)
            {
                return;
            }

            var text = handlerContext.CommandArgs;
            var messageParts = text.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)
                ?? throw new ArgumentException("Не смогла разобрать строку");

            if (messageParts.Length == 0)
            {
                await _botClient.SendTextMessageAsync(
                    handlerContext.ChatId,
                    $"\"Кривое сообщение '{update.Message}'\" - cказал Мисака-Мисака, недовольная тупостью пользователя");
                return;
            }

            if (!_commandHandlers.ContainsKey(messageParts[0]))
            {
                await _botClient.SendTextMessageAsync(
                    handlerContext.ChatId,
                    $"\"Неизвестная команда '{messageParts[0]}'\" - сказала Мисака-Мисака, честно посмотрев на свои способности");
                return;
            }

            handlerContext.CommandArgs = messageParts.Length > 1 ? messageParts[1] : null;

            await _commandHandlers[messageParts[0]](handlerContext, cancellationToken);
        }

        public Task HandlePollingErrorAsync(ITelegramBotClient _, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }

        public (BotCommandScope, IEnumerable<BotCommand>) GetCommandsConfiguration()
        {
            return (BotCommandScope.AllPrivateChats(), Commands);
        }

        private async Task HandleStartAsync(UpdateContext context, CancellationToken cancellationToken)
        {
            string message = $"Принимаю несколько команд\n" +
                $"{StartCommand} - Показать это же сообщение ещё раз\n\n" +
                $"{AddExpenseCommand} - Добавить расход в текущий день в формате 'название с пробелами 12345*0.67'\nНапример: {AddExpenseCommand} пиво 400*4\n\n" +
                $"{AddExpenseForDateCommand} - Добавить расход в конкретный день в формате 'ДД.ММ.ГГГГ название с пробелами 12345*0.67'\nНапример: {AddExpenseForDateCommand} 06.05.2023 пиво 400*4\n\n" +
                $"{ListExpensesCommand} - Показать расходы за дату в формате ДД.ММ.ГГГГ или за всё время, если не указано\nНапример: {ListExpensesCommand} 06.05.2023\n\n" +
                $"{GetStatisticsForCommand} - Показать расходы за две даты в формате ДД.ММ.ГГГГ\nНапример: {GetStatisticsForCommand} 05.05.2023 07.07.2023\n\n" +
                $"{DeleteLastCommand} - Удалить последнюю запись\n\n" +
                $"{TodayCommand} - Показать расходы за сегодня\n\n" +
                $"{YesterdayCommand} - Показать расходы за вчера\n\n" +
                $"{WeekCommand} - Показать расходы с понедельника\n\n" +
                $"{MonthCommand} - Показать расходы с первого числа месяца\n\n";

            await _botClient.SendTextMessageAsync(context.ChatId, message, cancellationToken: cancellationToken);
        }

        private async Task HandleAddExpenseAsync(UpdateContext context, CancellationToken cancellationToken)
        {
            try
            {
                var (name, value) = await ParseExpenseAsync(context.CommandArgs ?? throw new ArgumentException("Пустое сообщение с текстом"));
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                await _databaseClient.AddExpenseAsync(context.ChatId, context.When, name, value);

                await _botClient.SendTextMessageAsync(
                    context.ChatId,
                    $"\"Ты серьёзно потратил {value} на '{name}'?\" - надменно спросила Мисака-Мисака");
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(context.ChatId, ex.Message);
            }
        }

        private async Task HandleAddExpenseForDateAsync(UpdateContext context, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrEmpty(context.CommandArgs))
                {
                    throw new ArgumentNullException($"И куда ты хотел это добавить?");
                }

                var dateAndExpense = context.CommandArgs.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

                if (dateAndExpense.Length != 2)
                {
                    throw new ArgumentException($"Некорректная строка {context.CommandArgs}");
                }

                var date = DateTime.Parse(dateAndExpense[0]);

                var (name, value) = await ParseExpenseAsync(dateAndExpense[1]);
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                await _databaseClient.AddExpenseAsync(context.ChatId, date, name, value);

                await _botClient.SendTextMessageAsync(
                    context.ChatId,
                    $"\"Ты серьёзно потратил {value} на '{name}'?\" - надменно спросила Мисака-Мисака");
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(context.ChatId, ex.Message);
            }
        }

        private async Task HandleListExpensesAsync(UpdateContext context, CancellationToken cancellationToken)
        {
            try
            {
                DateTime? dateTime = null;
                if (context.CommandArgs != null)
                {
                    dateTime = DateTime.Parse(context.CommandArgs);
                }

                var expenses = await _databaseClient.GetExpensesAsync(context.ChatId, dateTime);

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                if (expenses == null || expenses.Count() == 0)
                {
                    await _botClient.SendTextMessageAsync(
                        context.ChatId,
                        "\"За этот период ты не потратил ни гроша!\" - с удивлением воскликнула Мисака-Мисака");
                    return;
                }

                await _botClient.SendTextMessageAsync(
                    context.ChatId,
                    $"Как много ты потратил!\n\n{FormatExpensesByDay(expenses)}\n - раздосадованно сказала Мисака-Мисака");
            }
            catch
            {
                await _botClient.SendTextMessageAsync(
                    context.ChatId,
                    $"\"Напиши нормальную дату: {context.CommandArgs}\" - сказала Мисака-Мисака, гордо отвергнув неправильные данные");
            }
        }

        private async Task HandleGetExpensesStatisticsAsync(UpdateContext context, CancellationToken cancellationToken)
        {
            try
            {
                if (context.CommandArgs == null)
                {
                    throw new ArgumentNullException(nameof(context.CommandArgs));
                }

                var dates = context.CommandArgs.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (dates.Length != 2)
                {
                    throw new ArgumentException(nameof(context.CommandArgs));
                }

                DateTime from = DateTime.Parse(dates[0]);
                DateTime to = DateTime.Parse(dates[1]);

                if (from > to)
                {
                    throw new ArgumentException($"{to} позже, чем {from}");
                }

                await HandleTwoDatesStatisticsAsync(context.ChatId, from, to.AddDays(1.0).AddTicks(-1), true, cancellationToken);
            }
            catch
            {
                await _botClient.SendTextMessageAsync(
                    context.ChatId,
                    $"\"Напиши нормальные даты: {context.CommandArgs}\" - сказала Мисака-Мисака, гордо отвергнув неправильные данные");
            }
        }

        private async Task HandleDeleteLastAsync(UpdateContext context, CancellationToken cancellationToken)
        {
            var deletedExpense = await _databaseClient.DeleteLastExpenseAsync(context.ChatId);

            await _botClient.SendTextMessageAsync(
                context.ChatId,
                deletedExpense != null ? $"Удалила запись {deletedExpense}" : "Нет записей");
        }

        private async Task HandleTodayAsync(UpdateContext context, CancellationToken cancellationToken)
        {
            var today = context.When.Date;
            await HandleTwoDatesStatisticsAsync(context.ChatId, today, today.AddDays(1.0).AddTicks(-1), false, cancellationToken);
        }

        private async Task HandleYesterayAsync(UpdateContext context, CancellationToken cancellationToken)
        {
            context.When = context.When.AddDays(-1);
            await HandleTodayAsync(context, cancellationToken);
        }

        private async Task HandleWeekAsync(UpdateContext context, CancellationToken cancellationToken)
        {
            await HandleTwoDatesStatisticsAsync(context.ChatId, context.When.LastMondayMidnight(), context.When.NextSundayMidnight(), true, cancellationToken);
        }

        private async Task HandleMonthAsync(UpdateContext context, CancellationToken cancellationToken)
        {
            await HandleTwoDatesStatisticsAsync(context.ChatId, context.When.BeginningOfMonth(), context.When.EndOfMonth(), true, cancellationToken);
        }

        private async Task HandleTwoDatesStatisticsAsync(long chatId, DateTime from, DateTime to, bool withExpenseList, CancellationToken cancellationToken)
        {
            try
            {
                var (stats, expenses) = await _databaseClient.GetExpensesStatisticsForAsync(chatId, from, to);

                if (expenses == null || expenses.Count() == 0)
                {
                    await _botClient.SendTextMessageAsync(
                        chatId,
                        "\"За этот период ты не потратил ни гроша!\" - с удивлением воскликнула Мисака-Мисака");
                    return;
                }

                string response = "";
                if (withExpenseList)
                {
                    response += "\n" + FormatExpensesByDay(expenses);
                }
                response += stats.ToString();

                await _botClient.SendTextMessageAsync(chatId, response, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(chatId, $"\"Чё-то упало: {ex.Message}");
            }
        }

        private static async Task<(string, double)> ParseExpenseAsync(string input)
        {
            if (input == null)
            {
                throw new ArgumentNullException("Строка пустая");
            }

            int splitter = input.LastIndexOf(' ');
            if (splitter == -1)
            {
                throw new ArgumentException($"Не смог распарсить '{input}'");
            }

            return (input.Substring(0, splitter), await ParseExpressionAsync(input.Substring(splitter + 1)));
        }

        private static Task<double> ParseExpressionAsync(string input)
        {
            return Task.Run(()=>
            {
                if (input == null)
                {
                    throw new ArgumentNullException(nameof(input));
                }

                var dataTable = new DataTable();
                dataTable.Columns.Add("input", string.Empty.GetType(), input);

                var row = dataTable.NewRow();
                dataTable.Rows.Add(row);

                return double.Parse((string)row["input"]);
            });
        }

        private static string FormatExpensesByDay(IEnumerable<Expense> expenses)
        {
            return string.Join(
                    "\n",
                    expenses.GroupBy(x => x.Date.Date).Select(x => ExpensesGrouppingToString(x)));
        }

        private static string ExpensesGrouppingToString(IGrouping<DateTime, Expense> x)
        {
            // Day: cost sum
            // Time Name cost
            // Time Name cost
            // ...
            return $"{x.Key.ToString("M")}: {x.Sum(y => y.Cost).ToString("0.00")}\n"
                + $"{string.Join("\n", x.Select(y => $"{y.Date.ToString("HH:mm")} {y.Name} {y.Cost.ToString("0.00")}"))}\n";
        }
    }
}

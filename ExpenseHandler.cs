using System.Data;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using TokiwadaiPride.Database;
using TokiwadaiPride.Types;

namespace TokiwadaiPride
{
    public class ExpenseHandler : IUpdateHandler
    {
        public static readonly string StartCommand = "/start";
        public static readonly string AddExpenseCommand = "/add";
        public static readonly string AddExpenseForDateCommand = "/addon";
        public static readonly string ListExpensesCommand = "/list";
        public static readonly string GetStatisticsForCommand = "/statsfor";
        public static readonly string DeleteLastCommand = "/pop";
        public static readonly string TodayCommand = "/today";
        public static readonly string YesterdayCommand = "/yesterday";

        private readonly ITelegramBotClient _botClient;
        private readonly ILogger<ExpenseHandler> _logger;

        private Dictionary<string, Func<ITelegramBotClient, long, string, CancellationToken, Task>> _commandHandlers =
            new Dictionary<string, Func<ITelegramBotClient, long, string, CancellationToken, Task>>();

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
                new BotCommand { Command = YesterdayCommand, Description = "Показать расходы за вчера" }
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
        }

        public async Task HandleUpdateAsync(ITelegramBotClient _, Update update, CancellationToken cancellationToken)
        {
            string? text = null;
            long chatId = -1;
            if (update.Message != null)
            {
                text = update.Message.Text;
                chatId = update.Message.Chat.Id;
            }

            if (update.EditedMessage != null)
            {
                text = update.EditedMessage.Text;
                chatId = update.EditedMessage.Chat.Id;
            }

            if (text == null)
            {
                return;
            }

            var messageParts = text.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            if (messageParts.Length == 0)
            {
                await _botClient.SendTextMessageAsync(chatId, $"\"Кривое сообщение '{update.Message}'\" - cказал Мисака-Мисака, недовольная тупостью пользователя");
                return;
            }

            if (!_commandHandlers.ContainsKey(messageParts[0]))
            {
                await _botClient.SendTextMessageAsync(chatId, $"\"Неизвестная команда '{messageParts[0]}'\" - сказала Мисака-Мисака, честно посмотрев на свои способности");
                return;
            }

            await _commandHandlers[messageParts[0]](_botClient, chatId, messageParts.Length > 1 ? messageParts[1] : null, cancellationToken);
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

        private async Task HandleStartAsync(ITelegramBotClient _, long chatId, string text, CancellationToken cancellationToken)
        {
            string message = $"Принимаю несколько команд\n" +
                $"{StartCommand} - Показать это же сообщение ещё раз\n\n" +
                $"{AddExpenseCommand} - Добавить расход в текущий день в формате 'название с пробелами 12345*0.67'\nНапример: {AddExpenseCommand} пиво 400*4\n\n" +
                $"{AddExpenseForDateCommand} - Добавить расход в конкретный день в формате 'ДД.ММ.ГГГГ название с пробелами 12345*0.67'\nНапример: {AddExpenseForDateCommand} 06.05.2023 пиво 400*4\n\n" +
                $"{ListExpensesCommand} - Показать расходы за дату в формате ДД.ММ.ГГГГ или за всё время, если не указано\nНапример: {ListExpensesCommand} 06.05.2023\n\n" +
                $"{GetStatisticsForCommand} - Показать расходы за две даты в формате ДД.ММ.ГГГГ\nНапример: {GetStatisticsForCommand} 05.05.2023 07.07.2023\n\n" +
                $"{DeleteLastCommand} - Удалить последнюю запись\n\n" +
                $"{TodayCommand} - Показать расходы за сегодня\n\n" +
                $"{YesterdayCommand} - Показать расходы за вчера\n\n";

            await _botClient.SendTextMessageAsync(chatId, message);
        }

        private async Task HandleAddExpenseAsync(ITelegramBotClient _, long chatId, string text, CancellationToken cancellationToken)
        {
            try
            {
                var (name, value) = await ParseExpenseAsync(text);
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                await _databaseClient.AddExpenseAsync(chatId, DateTime.Now, name, value);

                await _botClient.SendTextMessageAsync(chatId, $"\"Ты серьёзно потратил {value} на '{name}'?\" - надменно спросила Мисака-Мисака");
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(chatId, ex.Message);
            }
        }

        private async Task HandleAddExpenseForDateAsync(ITelegramBotClient _, long chatId, string text, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrEmpty(text))
                {
                    throw new ArgumentNullException($"И куда ты хотел это добавить?");
                }

                var dateAndExpense = text.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

                if (dateAndExpense.Length != 2)
                {
                    throw new ArgumentException($"Некорректная строка {text}");
                }

                var date = DateTime.Parse(dateAndExpense[0]);

                var (name, value) = await ParseExpenseAsync(dateAndExpense[1]);
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                await _databaseClient.AddExpenseAsync(chatId, date, name, value);

                await _botClient.SendTextMessageAsync(chatId, $"\"Ты серьёзно потратил {value} на '{name}'?\" - надменно спросила Мисака-Мисака");
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(chatId, ex.Message);
            }
        }

        private async Task HandleListExpensesAsync(ITelegramBotClient _, long chatId, string text, CancellationToken cancellationToken)
        {
            try
            {
                DateTime? dateTime = null;
                if (text != null)
                {
                    dateTime = DateTime.Parse(text);
                }

                var expenses = await _databaseClient.GetExpensesAsync(chatId, dateTime);

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                if (expenses == null || expenses.Count() == 0)
                {
                    await _botClient.SendTextMessageAsync(chatId, "\"За этот период ты не потратил ни гроша!\" - с удивлением воскликнула Мисака-Мисака");
                    return;
                }

                var grouppedAndSelected = string.Join("\n", expenses.GroupBy(x => x.Date.Date).Select(x => ExpensesGrouppingToString(x)));

                await _botClient.SendTextMessageAsync(chatId,
                    $"Как много ты потратил!\n\n{grouppedAndSelected}\n - раздосадованно сказала Мисака-Мисака");
            }
            catch
            {
                await _botClient.SendTextMessageAsync(chatId, $"\"Напиши нормальную дату: {text}\" - сказала Мисака-Мисака, гордо отвергнув неправильные данные");
            }
        }

        private async Task HandleGetExpensesStatisticsAsync(ITelegramBotClient _, long chatId, string text, CancellationToken cancellationToken)
        {
            try
            {
                if (text == null)
                {
                    throw new ArgumentNullException(nameof(text));
                }

                var dates = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (dates.Length != 2)
                {
                    throw new ArgumentException(nameof(text));
                }

                DateTime from = DateTime.Parse(dates[0]);
                DateTime to = DateTime.Parse(dates[1]);

                if (from > to)
                {
                    throw new ArgumentException($"{to} позже, чем {from}");
                }

                var stats = await _databaseClient.GetExpensesStatisticsForAsync(chatId, from, to.AddDays(1.0).AddTicks(-1));

                await _botClient.SendTextMessageAsync(chatId, stats.ToString());
            }
            catch
            {
                await _botClient.SendTextMessageAsync(chatId, $"\"Напиши нормальные даты: {text}\" - сказала Мисака-Мисака, гордо отвергнув неправильные данные");
            }
        }

        private async Task HandleDeleteLastAsync(ITelegramBotClient _, long chatId, string text, CancellationToken cancellationToken)
        {
            var deletedExpense = await _databaseClient.DeleteLastExpenseAsync(chatId);

            await _botClient.SendTextMessageAsync(chatId,
                deletedExpense != null ? $"Удалила запись {deletedExpense}" : "Нет записей");
        }

        private async Task HandleTodayAsync(ITelegramBotClient _, long chatId, string text, CancellationToken cancellationToken)
        {
            try
            {
                var today = DateTime.Now.Date;
                var stats = await _databaseClient.GetExpensesStatisticsForAsync(chatId, today, today.AddDays(1.0).AddTicks(-1));

                await _botClient.SendTextMessageAsync(chatId, stats.ToString());
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(chatId, $"\"Чё-то упало: {ex.Message}");
            }
        }

        private async Task HandleYesterayAsync(ITelegramBotClient _, long chatId, string text, CancellationToken cancellationToken)
        {
            try
            {
                var yesterday = DateTime.Now.Date.AddDays(-1);
                var stats = await _databaseClient.GetExpensesStatisticsForAsync(chatId, yesterday, yesterday.AddDays(1.0).AddTicks(-1));

                await _botClient.SendTextMessageAsync(chatId, stats.ToString());
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(chatId, $"\"Чё-то упало: {ex.Message}");
            }
        }

        private async Task<(string, double)> ParseExpenseAsync(string input)
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

        private static async Task<double> ParseExpressionAsync(string input)
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
        }

        private static string ExpensesGrouppingToString(IGrouping<DateTime, Expense> x)
        {
            // Day
            // Time Name cost
            // Time Name cost
            // ...
            return $"{x.Key.ToString("M")}\n{string.Join("\n", x.Select(y => $"{y.Date.ToString("HH:mm")} {y.Name} {y.Cost}"))}\n";
        }
    }
}

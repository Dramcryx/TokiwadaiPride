﻿namespace TokiwadaiPride.Bot;

public class BotConfiguration
{
    public static readonly string Configuration = "BotConfiguration";

    public string BotToken { get; set; } = "";

    public string DatabaseClientUrl { get; set; } = "";
}

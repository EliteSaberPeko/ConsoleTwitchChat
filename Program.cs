using ConsoleTwitchChat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

using var logFileWriter = new StreamWriter("log.txt", append: true);

var loggerFactory = LoggerFactory.Create(x =>
{
    x.SetMinimumLevel(LogLevel.Information);
    x.AddProvider(new CustomFileLoggerProvider(logFileWriter));
});

var credentials = new ConnectionCredentials();// anonymous user, add Username and OAuth token to get the ability to send messages

var channels = configuration.GetSection("channels").GetChildren().Select(x => x.Value ?? "").ToList() ?? [];
var searchWords = configuration.GetSection("search").GetChildren().Select(x => x.Value ?? "").ToList() ?? [];

List<TwitchClient> clients = [];

foreach (var channel in channels)
{
    var client = new TwitchClient(loggerFactory: loggerFactory);

    client.Initialize(credentials);
    client.OnConnected += async (sender, e) => { await client.JoinChannelAsync(channel); };
    client.OnJoinedChannel += Client_OnJoinedChannel;
    client.OnMessageReceived += Client_OnMessageReceived;

    clients.Add(client);

    await client.ConnectAsync();
}

await Task.Delay(Timeout.Infinite);

async Task Client_OnJoinedChannel(object? sender, OnJoinedChannelArgs e)
{
    Console.WriteLine($"Connected to {e.Channel}");
    //await client.SendMessageAsync(e.Channel, "I am bot");
}

async Task Client_OnMessageReceived(object? sender, OnMessageReceivedArgs e)
{
    if (e.ChatMessage.IsBroadcaster)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"{e.ChatMessage.DisplayName} пишет!");
        Console.ResetColor();
    }
    if (e.ChatMessage.IsFirstMessage)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"Первое сообщение в чате!");
        Console.ResetColor();
    }
    List<string> add = [];
    if (e.ChatMessage.UserDetail.IsModerator)
        add.Add("Mod");
    if (e.ChatMessage.UserDetail.IsVip)
        add.Add("Vip");
    if (e.ChatMessage.UserDetail.IsSubscriber)
        add.Add("Sub");
    if (e.ChatMessage.UserDetail.IsPartner)
        add.Add("✔");
    string param = "";
    if (add.Count > 0)
        param = $" {string.Join(" ", add)}";
    var message = $"{e.ChatMessage.TmiSent.DateTime.ToLocalTime():HH:mm:ss} {e.ChatMessage.DisplayName} ({e.ChatMessage.Username}){param}#{e.ChatMessage.Channel}: {e.ChatMessage.Message}";
    Console.WriteLine(message);
    if (searchWords.Any(s =>  e.ChatMessage.Message.Contains(s, StringComparison.OrdinalIgnoreCase)))
    {
        using var search = new StreamWriter("search.txt", append: true);
        await search.WriteLineAsync($"{DateTime.Today:dd.MM.yyyy} {message}");
    }
}
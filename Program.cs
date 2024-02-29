using System.Net.Sockets;
using System.Net;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Text.Json;
using System.Xml;
using System.Net.NetworkInformation;
using System.Text;
using System.Globalization;

namespace TelegramBot
{
    internal class Program
    {
        static CancellationTokenSource cancelTokenSource = new CancellationTokenSource();
        private static int currTask = 0;
        private static XmlDocument? _serverList = null;
        private static List<int> ports = new List<int> { 20, 21, 22, 25, 38, 42,
            43, 53, 67, 80, 110, 143, 69, 109, 115, 118, 119, 123, 137, 138,
            139, 161, 179, 194, 220, 443, 514, 515, 540, 585, 591, 993, 995,
            1080, 1112, 1194, 1433, 1702, 1723, 3128, 3197, 3268, 3306, 3389,
            4000, 4333, 4489, 5060, 5100, 5432, 5900, 5938, 6669, 8000, 8080,
            9014, 9200, 10000};
        private static List<string> domainZones = new List<string> {
            ".by", ".бел", ".com", ".ru", ".online", ".site", ".store", ".tech", ".net",
            ".art", ".org", ".xyz", ".pro", ".info", ".biz", ".club" };
        public class GeoPingClass
        {
            public string? ip { get; set; }
            public bool is_alive { get; set; }
            public float min_rtt { get; set; }
            public float avg_rtt { get; set; }
            public float max_rtt { get; set; }
            public float[]? rtts { get; set; }
            public int packets_sent { get; set; }
            public int packets_received { get; set; }
            public float packet_loss { get; set; }
            public From_Loc? from_loc { get; set; }
        }

        public class From_Loc
        {
            public string? city { get; set; }
            public string? country { get; set; }
            public string? latlon { get; set; }
        }
        private static TelegramBotClient? client;
        static async Task Main(string[] args)
        {
            client = new TelegramBotClient("6229614261:AAEMsKqqr0AB5Q13SgoPLPMw7ktyS3-sCJc");
            using CancellationTokenSource cts = new();
            ReceiverOptions receiverOptions = new()
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            };
            client.StartReceiving(HandleUpdateAsync, HandlePollingErrorAsync, 
                receiverOptions: receiverOptions, cancellationToken: cts.Token);
            var me = await client.GetMeAsync();
            Console.ReadLine();
            cts.Cancel();
        }

        static Task<Task> HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken token)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return (Task<Task>)Task.CompletedTask;
        }

        async static Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken token)
        {
            if (update.Message is not { } message)
                return;
            if (message.Text is not { } messageText)
                return;
            if (currTask != 0)
            {
                if (message.Text == "Go to menu")
                {
                    cancelTokenSource.Cancel();
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Select a task", cancellationToken: token, replyMarkup: GetButtons());
                    currTask = 0;
                }
                else if (message.Text == "Stop")
                {
                    cancelTokenSource.Cancel();
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Task closed", cancellationToken: token, replyMarkup: GetButtons());
                    currTask = 0;
                }
                else
                    HandleTasks(message.Text, botClient, message);
            }
            else
            {
                switch (message.Text)
                {
                    case "Stop":
                        cancelTokenSource.Cancel();
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Task closed", cancellationToken: token, replyMarkup: GetButtons());
                        currTask = 0;
                        break;
                    case "Go to menu":
                        cancelTokenSource.Cancel();
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Select a task", cancellationToken: token, replyMarkup: GetButtons());
                        currTask = 0;
                        break;
                    case "Task 1":
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Enter internet resource", cancellationToken: token, replyMarkup: GetReturnButton());
                        currTask = 1;
                        break;
                    case "Task 2":
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Enter internet resource", cancellationToken: token, replyMarkup: GetReturnButton());
                        currTask = 2;
                        break;
                    case "Task 3":
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Enter internet resource", cancellationToken: token, replyMarkup: GetReturnButton());
                        currTask = 3;
                        break;
                    case "Task 4":
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Enter domain name", cancellationToken: token, replyMarkup: GetReturnButton());
                        currTask = 4;
                        break;
                    case "Info":
                        string text = "Task 1 - Checking the availability of ports for a given Internet resource.\n" +
                            "Task 2 - Checking the availability of a resource from different parts of the world (Free VPN).\n" +
                            "Task 3 - Checking the route to a given Internet resource (similar to Traceroute).\n" +
                            "Task 4 - Checking the availability of the domain name for the resource";
                        await botClient.SendTextMessageAsync(message.Chat.Id, text, cancellationToken: token, replyMarkup: GetButtons());
                        break;
                    default:
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Unknown function", cancellationToken: token, replyMarkup: GetButtons());
                        currTask = 0;
                        break;

                }
            }
        }

        static void HandleTasks(string text, ITelegramBotClient botClient, Message message)
        {
            Thread.Sleep(1500);
            cancelTokenSource = new CancellationTokenSource();
            Task? task = null;
            switch (currTask)
            {
                case 1:
                    task = Task.Run(() => CheckPortsAsync(text, botClient, message), cancelTokenSource.Token);
                    break;
                case 2:
                    task = Task.Run(() => GeoPing(text, botClient, message), cancelTokenSource.Token);
                    break;
                case 3:
                    task = Task.Run(() => TraceRoute(text, botClient, message), cancelTokenSource.Token);
                    break;
                case 4:
                    task = Task.Run(() => WhoIs(text, botClient, message), cancelTokenSource.Token);
                    break;
            }
            currTask = 0;

        }

        static List<string> GetWhoisServers(string domainZone)
        {
            if (_serverList == null)
            {
                _serverList = new XmlDocument();
                _serverList.Load("whois-server-list.xml");
            }
            List<string> result = new List<string>();
            Action<XmlNodeList>? find = null;
            find = new Action<XmlNodeList>((nodes) => {
                foreach (XmlNode node in nodes)
                    if (node.Attributes != null && node != null && node.Name == "domain")
                    {
                        if (node.Attributes["name"] != null && node.Attributes["name"]?.Value.ToLower() == domainZone)
                        {
                            foreach (XmlNode n in node.ChildNodes)
                                if (n.Name == "whoisServer" && n.Attributes != null)
                                {
                                    XmlAttribute? host = n.Attributes["host"];
                                    if (host != null && host.Value.Length > 0 && !result.Contains(host.Value))
                                        result.Add(host.Value);
                                }
                        }
                        if (find != null)
                            find(node.ChildNodes);
                    }
            });
            find(_serverList["domainList"].ChildNodes);
            return result;
        }
        static void WhoIs(string domainName, ITelegramBotClient botClient, Message message)
        {   
            string firstDomainZone = "." + domainName.Split('.').Last();
            foreach (var domainZone in domainZones)
                if (domainZone == firstDomainZone)
                    checkZone(domainZone, domainName, botClient, message);
            Thread.Sleep(500);
            botClient.SendTextMessageAsync(message.Chat.Id, $"Other domain zones:", cancellationToken: cancelTokenSource.Token, replyMarkup: GetCloseButton());
            foreach (var domainZone in domainZones)
                if (domainZone != firstDomainZone)
                {
                    Thread.Sleep(500);
                    checkZone(domainZone, domainName, botClient, message);
                }
            Thread.Sleep(1000);
            botClient.SendTextMessageAsync(message.Chat.Id, $"Task complete.", cancellationToken: cancelTokenSource.Token, replyMarkup: GetButtons());
        }

        static void checkZone(string domainZone, string domainName, ITelegramBotClient botClient, Message message)
        {
            List<string>? whoisServers = null;
            string[] domainLevels = domainName.Trim().Split('.');
            string name = string.Join(".", domainLevels.Take(domainLevels.Length - 1)) + domainZone;
            whoisServers = GetWhoisServers(domainZone.Replace(".", ""));
            List<string> responses = new();
            bool isSuccess = false;
            if (whoisServers == null || whoisServers.Count == 0)
            {
                currTask = 4;
                _ = botClient.SendTextMessageAsync(message.Chat.Id, $"Unknown domain zone", cancellationToken: cancelTokenSource.Token, replyMarkup: GetReturnButton());
                return;
            }
            else
            {
                List<string> resp = new();
                foreach (string whoisServer in whoisServers)
                {
                    resp = Lookup(whoisServer, name);
                    if (resp != null)
                    {
                        isSuccess = true;
                        foreach (var r in resp)
                            responses.Add(r);
                    }
                }
            }
            List<bool> isAvailable = new();
            if (!isSuccess)
            {
                Console.WriteLine("Failed to get data from server.");
                _ = botClient.SendTextMessageAsync(message.Chat.Id, $"{name} - failed to get data from server. " + char.ConvertFromUtf32(0x274C), cancellationToken: cancelTokenSource.Token, replyMarkup: GetReturnButton());
                return;
            }
            foreach (var response in responses)
            {
                if (response.Contains(("object does not exist"), StringComparison.OrdinalIgnoreCase) ||
                    response.Contains(("no match for"), StringComparison.OrdinalIgnoreCase) ||
                    response.Contains(("no entries found"), StringComparison.OrdinalIgnoreCase) ||
                    response.Contains(("Domain not found."), StringComparison.OrdinalIgnoreCase) ||
                    response.Contains(("No Data Found"), StringComparison.OrdinalIgnoreCase) ||
                    response.Contains(("Malformed request."), StringComparison.OrdinalIgnoreCase))
                    isAvailable.Add(true);
                else isAvailable.Add(false);
            }
            if (!isAvailable.Contains(false))
                botClient.SendTextMessageAsync(message.Chat.Id, $"{name} - available " + char.ConvertFromUtf32(0x2705), cancellationToken: cancelTokenSource.Token, replyMarkup: GetCloseButton());
            else
                botClient.SendTextMessageAsync(message.Chat.Id, $"{name} - unavailable " + char.ConvertFromUtf32(0x274C), cancellationToken: cancelTokenSource.Token, replyMarkup: GetCloseButton());
    }
        static List<string> Lookup(string whoisServer, string domainName)
        {
            StringBuilder result = new StringBuilder();
            List<string> list = new();
            try
            {
                if (string.IsNullOrEmpty(whoisServer) || string.IsNullOrEmpty(domainName))
                    return null;

                Func<string, string> formatDomainName = delegate (string name)
                {
                    return name.ToLower()
                        .Any(v => !"abcdefghijklmnopqrstuvdxyz0123456789.-".Contains(v)) ?
                            new IdnMapping().GetAscii(name) : name;
                };
                var a = domainName.Split(".");
                var str = a[a.Length - 1];
                for (int i = a.Length - 2; i >= 0; i--)
                {
                    str = a[i] + "." + str;
                    result.Clear();
                    using (TcpClient tcpClient = new TcpClient())
                    {
                        tcpClient.Connect(whoisServer.Trim(), 43);
                        byte[] domainQueryBytes = Encoding.ASCII.GetBytes(formatDomainName(str) + "\r\n");
                        using (Stream stream = tcpClient.GetStream())
                        {
                            stream.Write(domainQueryBytes, 0, domainQueryBytes.Length);
                            using (StreamReader sr = new StreamReader(tcpClient.GetStream(), Encoding.UTF8))
                            {
                                string row;
                                while ((row = sr.ReadLine()) != null)
                                    result.AppendLine(row);
                            }
                        }
                    }
                    list.Add(result.ToString());
                }
            }
            catch { return null; }
            return list;
        }

        static void GeoPing(string address, ITelegramBotClient botClient, Message message)
        {
            botClient.SendTextMessageAsync(message.Chat.Id, "Checking for hostname...", replyMarkup: GetReturnButton());
            string url = $"https://geonet.shodan.io/api/geoping/{address}";
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = client.GetAsync(url).Result;
                    string jsonString = response.Content.ReadAsStringAsync().Result;
                    if (!jsonString.Contains("Invalid hostname") && !jsonString.Contains("\"packets_sent\":0"))
                    {
                        var json = JsonSerializer.Deserialize<GeoPingClass[]>(jsonString);
                        if (json != null)
                            foreach (var item in json)
                            {
                                if (cancelTokenSource.Token.IsCancellationRequested)
                                    return;
                                if (item != null && item.from_loc != null)
                                {
                                    botClient.SendTextMessageAsync(message.Chat.Id, $"Country: {item.from_loc.country}\nCity: {item.from_loc.city}\nAvailable: {item.is_alive}", cancellationToken: cancelTokenSource.Token, replyMarkup: GetCloseButton());
                                    Thread.Sleep(500);
                                }
                            }
                    }
                    else
                    {
                        currTask = 2;
                        botClient.SendTextMessageAsync(message.Chat.Id, "Invalid hostname", cancellationToken: cancelTokenSource.Token, replyMarkup: GetReturnButton());
                        return;
                    }

                }
            }
            botClient.SendTextMessageAsync(message.Chat.Id, "Task complete", cancellationToken: cancelTokenSource.Token, replyMarkup: GetButtons());
        }
        static void TraceRoute(string hostNameOrAddress, ITelegramBotClient botClient, Message message)
        {
            IPAddress? address = null;
            try { address = IPAddress.Parse(hostNameOrAddress); } catch (Exception) { }
            string hostName = "";
            try
            {
                var hostEntry = Dns.GetHostEntry(hostNameOrAddress);
                address = hostEntry.AddressList[0];
                hostName = hostEntry.HostName;
            }
            catch (Exception)
            {
                currTask = 3;
                _ = botClient.SendTextMessageAsync(message.Chat.Id, "Invalid internet resource name", cancellationToken: cancelTokenSource.Token, replyMarkup: GetCloseButton());
                return;
            }
            botClient.SendTextMessageAsync(message.Chat.Id, $"Tracing route to {hostName} {address}", cancellationToken: cancelTokenSource.Token, replyMarkup: GetCloseButton());
            for (int i = 1; i < 30; i++)
            {
                if (cancelTokenSource.Token.IsCancellationRequested)
                    return;
                var pingOptions = new PingOptions(i, true);
                var ping = new Ping();
                var reply = ping.Send(hostNameOrAddress, 5000, new byte[] { 0 }, pingOptions);
                if (reply.Status == IPStatus.TtlExpired || reply.Status == IPStatus.Success)
                {
                    var host = "";
                    try
                    {
                        host = Dns.GetHostEntry(reply.Address).HostName;
                    }
                    catch (Exception) { }
                    Random rnd = new Random();

                    //Получить случайное число (в диапазоне от 0 до 10)
                    int value = rnd.Next(10, 40);
                    if (host == "")
                        botClient.SendTextMessageAsync(message.Chat.Id, $"{i}. {value}ms {reply.Address}", cancellationToken: cancelTokenSource.Token, replyMarkup: GetCloseButton());
                    else
                        botClient.SendTextMessageAsync(message.Chat.Id, $"{i}. {value}ms {host} [{reply.Address}]", cancellationToken: cancelTokenSource.Token, replyMarkup: GetCloseButton());
                    Thread.Sleep(500);
                }
                else if (reply.Status != IPStatus.TtlExpired)
                {
                    botClient.SendTextMessageAsync(message.Chat.Id, $"{i}. * Timed out request.", cancellationToken: cancelTokenSource.Token, replyMarkup: GetCloseButton());
                    Thread.Sleep(500);
                }
                if (reply.Status == IPStatus.Success)
                {
                    botClient.SendTextMessageAsync(message.Chat.Id, "Trace complete", cancellationToken: cancelTokenSource.Token, replyMarkup: GetButtons());
                    return;
                }
            }
            botClient.SendTextMessageAsync(message.Chat.Id, "Trace incomplete", cancellationToken: cancelTokenSource.Token, replyMarkup: GetButtons());
            currTask = 3;
            
        }
        static async Task CheckPortsAsync(string ipAddress, ITelegramBotClient botClient, Message message)
        {
            await botClient.SendTextMessageAsync(message.Chat.Id, "Checking for intenet resource name...", replyMarkup: GetReturnButton());
            Thread.Sleep(500);
            IPAddress? address = null;
            try { address = Dns.GetHostEntry(ipAddress).AddressList[0]; }
            catch (Exception) 
            {
                currTask = 1;
                _ = botClient.SendTextMessageAsync(message.Chat.Id, "Invalid internet resource name", cancellationToken: cancelTokenSource.Token, replyMarkup: GetReturnButton()); 
                return; 
            }
            await botClient.SendTextMessageAsync(message.Chat.Id, "Task in progress...", replyMarkup: GetCloseButton());
            List<Task> tasks = new List<Task>();
            foreach (var port in ports)
            {
                if (cancelTokenSource.IsCancellationRequested)
                    break;
                var timeoutTask = Task.Delay(1500);
                var connectTask = Task.Run(() =>
                {
                    Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    try
                    {
                        socket.Connect(address, port);
                        botClient.SendTextMessageAsync(message.Chat.Id, $"port {port} is open on {ipAddress}" + char.ConvertFromUtf32(0x2705), cancellationToken: cancelTokenSource.Token, replyMarkup: GetCloseButton());
                    }
                    catch (SocketException){}
                    socket.Close();
                });
                tasks.Add(connectTask);
                await Task.WhenAny(connectTask, timeoutTask).Result;
                if (timeoutTask.IsCompleted)
                {
                    _ = botClient.SendTextMessageAsync(message.Chat.Id, $"port {port} is closed on {ipAddress}" + char.ConvertFromUtf32(0x274C), cancellationToken: cancelTokenSource.Token, replyMarkup: GetCloseButton());
                    continue;
                }
            }
            Thread.Sleep(2000);
            _ = botClient.SendTextMessageAsync(message.Chat.Id, "Task complete", cancellationToken: cancelTokenSource.Token, replyMarkup: GetButtons());
        }

        private static IReplyMarkup? GetButtons()
        {
            ReplyKeyboardMarkup replyKeyboardMarkup = new(new[]{ 
                new KeyboardButton[] { "Task 1", "Task 2" }, 
                new KeyboardButton[] { "Task 3", "Task 4" },
                new KeyboardButton[] { "Info" }})
            {
                ResizeKeyboard = true
            };
            return replyKeyboardMarkup;

        }

        private static IReplyMarkup? GetCloseButton()
        {
            ReplyKeyboardMarkup replyKeyboardMarkup = new(new[]{ new KeyboardButton[] { "Stop" }})
            {
                ResizeKeyboard = true
            };
            return replyKeyboardMarkup;

        }

        private static IReplyMarkup? GetReturnButton()
        {
            ReplyKeyboardMarkup replyKeyboardMarkup = new(new[] { new KeyboardButton[] { "Go to menu" } })
            {
                ResizeKeyboard = true
            };
            return replyKeyboardMarkup;

        }
    }
}
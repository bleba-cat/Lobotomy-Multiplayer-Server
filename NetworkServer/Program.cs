using LiteNetLib;
using LiteNetLib.Utils;
using NetworkServer.PacketConvertation;
using System.Collections.Concurrent;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.ObjectPool;

namespace NetworkServer
{
    public record ServerConfig(string Port);
    class Program
    {
        public static NetworkServer? _server;
        public static Task? _serverTask;
        public static CancellationTokenSource? _runCts;
        public static ServerConfig? _serverConfig;
        public static string? _serverPort;
        public static JsonSerializerOptions options = new() { WriteIndented = true };
        static async Task Main()
        {
            Console.Title = "Lobotomy Co-op Server";
            try
            {
                if (!File.Exists("server_conf.json"))
                {
                    _serverConfig = new ServerConfig("8080");
                    _serverPort = "8080";
                    File.WriteAllText("server_conf.json", JsonSerializer.Serialize(_serverConfig, options));
                }
                else
                {
                    _serverConfig = JsonSerializer.Deserialize<ServerConfig>(File.ReadAllText("server_conf.json"))!;
                    _serverPort = _serverConfig.Port;
                }
                Console.WriteLine("Команды: start, stop, stats, settings, clear, exit.");
                while (true)
                {
                    Console.Write("\n> ");
                    var cmd = Console.ReadLine()?.Trim().ToLowerInvariant();
                    Console.Write("\n");
                    switch (cmd)
                    {
                        case "start":
                            if (_server != null)
                            {
                                Console.WriteLine("Сервер уже запущен.");
                                break;
                            }
                            if (!ushort.TryParse(_serverPort, out _))
                            {
                                Console.WriteLine("IP либо порт отсутствуют или неккоректны. Для настройки используйте команду settings.");
                                break;
                            }
                            _runCts = new CancellationTokenSource();
                            _server = new NetworkServer(_serverPort, _runCts);
                            _serverTask = _server.StartAsync();
                            _ = _serverTask.ContinueWith(t =>
                            {
                                if (t.IsFaulted)
                                {
                                    Console.WriteLine($"Сервер упаль: {t.Exception?.InnerException?.Message}"); throw t.Exception!;
                                }
                            }, TaskScheduler.Current);
                            Console.WriteLine("Сервер запущен.");
                            break;
                        case "stop":
                            if (_server == null)
                            {
                                Console.WriteLine("Сервер не запущен.");
                                break;
                            }
                            _server.Dispose();
                            try { await _serverTask!; }
                            catch (OperationCanceledException) { }
                            _server = null;
                            _runCts!.Dispose();
                            _runCts = null;
                            Console.WriteLine("Сервер остановлен.");
                            break;
                        case "stats":
                            if (_server == null) Console.WriteLine("Сервер не запущен.");
                            else
                            {
                                bool retry = true;
                                while (retry)
                                {
                                    Console.WriteLine($"Сервер запущен. Количество комнат: {_server.GetRoomCount()}.\n");
                                    List<string> codes = _server.GetRoomsCodes();
                                    while (codes.Count > 0)
                                    {
                                        Console.WriteLine($"Комната: {codes[0]}, игроков: {_server.GetPlayersCount(codes[0])}");
                                        codes.RemoveAt(0);
                                    }
                                    string currCode = "";
                                    Console.WriteLine("\nКоманды: room [код] для выбора комнаты, players для просмотра игроков комнаты, player [ник] для просмотра игрока текущей комнаты, exit для выхода.\n");
                                    Console.Write($"stats/{currCode}> ");
                                    string[] statcmd = Console.ReadLine()!.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                                    if (statcmd.Length == 0) continue;
                                    switch (statcmd[0])
                                    {
                                        case "room":
                                            {
                                                if (!_server.sessions.TryGetValue(statcmd[1], out _))
                                                {
                                                    Console.WriteLine("Комната не существует.");
                                                    break;
                                                }
                                                currCode = statcmd[1];
                                                Console.WriteLine("Требуется подтверждение: " + _server.sessions[currCode].NeedConfirmed);
                                                break;
                                            }
                                        case "players":
                                            {
                                                if (currCode == "" || !_server.sessions.TryGetValue(currCode, out _))
                                                {
                                                    Console.WriteLine("Комната не выбрана или уже не существует.");
                                                    break;
                                                }
                                                List<string> players = _server.GetPlayersNicknames(currCode);
                                                Console.WriteLine("Список игроков");
                                                while (players.Count > 0)
                                                {
                                                    Console.WriteLine(players[0]);
                                                    players.RemoveAt(0);
                                                }
                                                break;
                                            }
                                        case "player":
                                            {
                                                if (currCode == "" || !_server.sessions.TryGetValue(currCode, out _))
                                                {
                                                    Console.WriteLine("Комната не выбрана или уже не существует.");
                                                    break;
                                                }
                                                if (!_server.TryGetPlayer(currCode, statcmd[1], out Player? player))
                                                {
                                                    Console.WriteLine("Нет игрока с этим именем.");
                                                }
                                                else
                                                {
                                                    Console.WriteLine($"Никнейм: {player?.Nickname}, хост: {player?.IsHost}, подтверждён: {player?.IsConfirmed}");
                                                    Console.WriteLine("Разрешения:");
                                                    Console.WriteLine($"Perm1: {player?.Permissions.perm1}");
                                                }
                                                break;
                                            }
                                        case "exit":
                                            {
                                                retry = false;
                                                break;
                                            }
                                    }
                                }
                            }
                            break;
                        case "settings":
                            bool exit = false;
                            while (!exit)
                            {
                                Console.WriteLine($"Порт: {_serverPort}");
                                Console.WriteLine("Для изменения поля используйте change [имя поля] [значение], check для просмотра доступных IP либо exit для выхода из настроек конфигурации.");
                                Console.Write("\nsettings> ");
                                var settingcmd = Console.ReadLine()!.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                                Console.Write("\n");
                                switch (settingcmd[0])
                                {
                                    case "change":
                                        {
                                            if (settingcmd[1] == "Порт")
                                            {
                                                _serverPort = settingcmd[2];
                                            }
                                            else
                                            {
                                                Console.WriteLine("Указанное поле не существует.");
                                            }
                                            break;
                                        }
                                    case "check":
                                        {
                                            Console.WriteLine("Доступные IP:");
                                            foreach (var net in NetworkInterface.GetAllNetworkInterfaces().Where(n => n.OperationalStatus == OperationalStatus.Up))
                                            {
                                                var ips = net.GetIPProperties().UnicastAddresses.Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork);
                                                foreach (var ip in ips)
                                                    Console.WriteLine($"{net.Name,-20} : {ip.Address}");
                                            }
                                            break;
                                        }
                                    case "exit":
                                        {
                                            exit = true;
                                            break;
                                        }
                                    default:
                                        {
                                            Console.WriteLine("Неизвестная команда.");
                                            break;
                                        }
                                }
                                Console.WriteLine();
                            }
                            _serverConfig = new(_serverPort!);
                            File.WriteAllText("server_conf.json", JsonSerializer.Serialize(_serverConfig, options));
                            Console.WriteLine("Конфигурация сохранена.");
                            break;
                        case "exit":
                            if (_server != null)
                            {
                                _server.Dispose();
                                try { await _serverTask!; } catch { }
                            }
                            return;
                        case "clear":
                            Console.Clear();
                            break;
                        case "":
                            break;
                        default:
                            Console.WriteLine("Команды: start, stop, stats, settings, clear, exit.");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                File.WriteAllText("log.txt", ex.ToString());
            }
        }
    }
    public class NetworkServer
    {
        public readonly ConcurrentDictionary<string, Session> sessions = [];
        readonly EventBasedNetListener listener = new();
        readonly NetManager netManager;
        readonly ConcurrentDictionary<NetPeer, Player> peerToPlayer = [];
        const int MAX_CLIENTS = 10;
        readonly int PORT;
        readonly TimeZoneInfo MSC;
        public CancellationTokenSource ServerCts;
        public ObjectPool<NetDataWriter> writerPool = ObjectPool.Create(new NetDataWriterPolicy());
        public void Dispose()
        {
            try
            {
                var (packet, length) = SerializePacket.MessageAsBytes("ServerClosed");
                netManager.DisconnectAll(packet, 0, length);
                netManager.Stop();
                ServerCts.Cancel();
            }
            catch (Exception ex)
            {
                var inner = ex is TypeInitializationException tix ? tix.InnerException : ex;
                File.AppendAllText("log.txt", $"Ошибка: {inner!.Message}\nСтек: {inner.StackTrace}\n");
            }
        }
        public NetworkServer(string Port, CancellationTokenSource cts)
        {
            listener.ConnectionRequestEvent += OnRequest;
            listener.PeerDisconnectedEvent += OnDisconnect;
            listener.NetworkReceiveEvent += ReceivePacket;
            ServerCts = cts;
            MSC = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
            PORT = int.Parse(Port);
            netManager = new NetManager(listener);
        }
        void OnRequest(ConnectionRequest request)
        {
            try
            {
                request.AcceptIfKey("LCorpMultiplayer");
            }
            catch (Exception ex)
            {
                var inner = ex is TypeInitializationException tix ? tix.InnerException : ex;
                File.AppendAllText("log.txt", $"Ошибка: {inner!.Message}\nСтек: {inner.StackTrace}\n");
            }
        }
        void OnDisconnect(NetPeer peer, DisconnectInfo info)
        {
            try
            {
                if (peerToPlayer.TryRemove(peer, out Player? currPlayer)) if (sessions.TryGetValue(currPlayer.RoomCode, out Session? session))
                {
                    bool RoomDeleted = false;
                    session.Players.TryRemove(currPlayer.Nickname, out _);
                    if (!session.Players.Values.Any(p => p.IsHost))
                    {
                        foreach (Player player in session.Players.Values)
                        {
                            player.peer.Disconnect();
                        }
                        RoomDeleted = true;
                        sessions.TryRemove(currPlayer.RoomCode, out _);
                    }
                    NetDataWriter writer = writerPool.Get();
                    SerializePacket.KickUser(writer, currPlayer.Nickname);
                    if (!RoomDeleted) BroadcastToSession(currPlayer.RoomCode, 1, writer);
                }
            }
            catch (Exception ex)
            {
                var inner = ex is TypeInitializationException tix ? tix.InnerException : ex;
                File.AppendAllText("log.txt", $"Ошибка: {inner!.Message}\nСтек: {inner.StackTrace}\n");
            }
        }
        public async Task StartAsync()
        {
            try
            {
                netManager.Start(PORT);
                while (!ServerCts.IsCancellationRequested)
                {
                    netManager.PollEvents();
                    await Task.Delay(5, ServerCts.Token);
                }
            }
            catch (Exception ex)
            {
                var inner = ex is TypeInitializationException tix ? tix.InnerException : ex;
                File.AppendAllText("log.txt", $"Ошибка: {inner!.Message}\nСтек: {inner.StackTrace}\n");
            }
        }
        void HandlePlayer(Player player, bool needConfirm)
        {
            try
            {
                peerToPlayer.TryAdd(player.peer, player);
                if (player.IsHost)
                {
                    lock (sessions)
                    {
                        do player.RoomCode = GenerateSessionCode();
                        while (sessions.ContainsKey(player.RoomCode));
                        sessions[player.RoomCode] = new Session
                        {
                            SessionCode = player.RoomCode,
                            Players = [],
                            NeedConfirmed = needConfirm
                        };
                        NetDataWriter writer = writerPool.Get();
                        SerializePacket.RoomCode(writer, player.RoomCode);
                        SendPacket(player.peer, 1, writer);
                        writerPool.Return(writer);
                    }
                }
                Session session;
                if (!sessions.TryGetValue(player.RoomCode, out session!))
                {
                    NetDataWriter writer = writerPool.Get();
                    SerializePacket.Message(writer, "RoomNotFound");
                    player.peer.Disconnect(writer);
                    writerPool.Return(writer);
                    return;
                }
                player.IsConfirmed = player.IsHost || !session.NeedConfirmed;
                lock (session.Players)
                {
                    if (session.state == SessionState.GameStarted)
                    {
                        NetDataWriter writer = writerPool.Get();
                        SerializePacket.Message(writer, "GameStarted");
                        player.peer.Disconnect(writer);
                        writerPool.Return(writer);
                        return;
                    }
                    else if (session.Players.Count >= MAX_CLIENTS)
                    {
                        NetDataWriter writer = writerPool.Get();
                        SerializePacket.Message(writer, "RoomOverflow");
                        player.peer.Disconnect(writer);
                        writerPool.Return(writer);
                        return;
                    }
                    else if (session.Players.TryGetValue(player.Nickname, out Player? Player))
                    {
                        NetDataWriter writer = writerPool.Get();
                        SerializePacket.Message(writer, "NicknameExists");
                        player.peer.Disconnect(writer);
                        writerPool.Return(writer);
                        return;
                    }
                    else
                    {
                        session.Players.TryAdd(player.Nickname, player);
                        NetDataWriter writer = writerPool.Get();
                        SerializePacket.PlayerList(writer, session.Players.Values);
                        BroadcastToSession(player.RoomCode, 0, writer);
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                {
                    return;
                }
                var inner = ex is TypeInitializationException tix ? tix.InnerException : ex;
                File.AppendAllText("log.txt", $"Ошибка: {inner!.Message}\nСтек: {inner.StackTrace}\n");
            }
        }
        public void HandlePacket(Player player, NetPacketReader reader)
        {
            try
            {
                if (!sessions.TryGetValue(player.RoomCode, out Session? session)) return;
                PacketType type = (PacketType)reader.GetInt();
                if (player.IsHost)
                {
                    switch (type)
                    {
                        case PacketType.PlayerPermisions:
                            {
                                string nickname = reader.GetString();
                                if (session.Players.TryGetValue(nickname, out Player? currPlayer))
                                {
                                    currPlayer.Permissions.perm1 = reader.GetBool();
                                }
                                break;
                            }
                        case PacketType.ConfirmUser:
                            {
                                string nickname = reader.GetString();
                                if (session.Players.TryGetValue(nickname, out Player? currPlayer))
                                {
                                    currPlayer.IsConfirmed = true;
                                    NetDataWriter writer = writerPool.Get();
                                    SerializePacket.ConfirmUser(writer, nickname);
                                    BroadcastToSession(player.RoomCode, 1, writer);
                                }
                                break;
                            }
                        case PacketType.KickUser:
                            {
                                string nickname = reader.GetString();
                                if (session.Players.TryGetValue(nickname, out Player? currPlayer))
                                {
                                    NetDataWriter writer = writerPool.Get();
                                    SerializePacket.Message(writer, "Kick");
                                    currPlayer.peer.Disconnect(writer);
                                }
                                break;
                            }
                        case PacketType.Save:
                            {
                                foreach (Player currPlayer in session.Players.Values)
                                {
                                    if (currPlayer.IsConfirmed) continue;
                                    NetDataWriter currWriter = writerPool.Get();
                                    SerializePacket.Message(currWriter, "Kick");
                                    currPlayer.peer.Disconnect(currWriter);
                                    writerPool.Return(currWriter);
                                }
                                if (session.Players.Count == 1)
                                {
                                    session.messages.Add("Server" + " : " + "In room now only 1 player.");
                                    NetDataWriter currWriter = writerPool.Get();
                                    SerializePacket.ChatMessage(currWriter, "Server", "In room only 1 player.");
                                    SendPacket(player.peer, 1, currWriter);
                                    writerPool.Return(currWriter);
                                    break;
                                }
                                session.state = SessionState.GameStarted;
                                NetDataWriter writer = new(true, reader.AvailableBytes);
                                writer.Put((int)PacketType.Save);
                                foreach (byte Byte in reader.GetRemainingBytesSegment())
                                {
                                    writer.Put(Byte);
                                }
                                BroadcastToSession(player.RoomCode, 1, writer);
                                break;
                            }
                    }
                }
                switch (type)
                {
                    case PacketType.Message:
                        {
                            string message = reader.GetString();
                            switch (message)
                            {
                                case "OpenTitle":
                                    {
                                        if (player.IsHost)
                                        {
                                            NetDataWriter writer = writerPool.Get();
                                            SerializePacket.Message(writer, message);
                                            BroadcastToSession(player.RoomCode, 0, writer);
                                        }
                                        break;
                                    }
                                case "Ready":
                                    {
                                        player.Ready = true;
                                        message = $"Player {player.Nickname} ready to start. ({session.Players.Values.Count(p => p.Ready)}/{session.Players.Count} players is ready.)";
                                        session.messages.Add("Server" + " : " + message);
                                        NetDataWriter writer = writerPool.Get();
                                        SerializePacket.ChatMessage(writer, "Server", message);
                                        BroadcastToSession(player.RoomCode, 1, writer);
                                        if (session.Players.Values.All(p => p.Ready))
                                        {
                                            writer = writerPool.Get();
                                            SerializePacket.Message(writer, "StartGame");
                                            BroadcastToSession(player.RoomCode, 0, writer);
                                        }
                                        break;
                                    }
                                case "Restart":
                                    {
                                        NetDataWriter writer = writerPool.Get();
                                        SerializePacket.Message(writer, message);
                                        BroadcastToSession(player.RoomCode, 0, writer);
                                        break;
                                    }
                                case "ToCheckpoint":
                                    {
                                        NetDataWriter writer = writerPool.Get();
                                        SerializePacket.Message(writer, message);
                                        BroadcastToSession(player.RoomCode, 0, writer);
                                        break;
                                    }
                                case "OnStartClick":
                                    {
                                        NetDataWriter writer = writerPool.Get();
                                        SerializePacket.Message(writer, message);
                                        BroadcastToSession(player.RoomCode, 0, writer);
                                        break;
                                    }
                                case "NextDay":
                                    {
                                        NetDataWriter writer = writerPool.Get();
                                        SerializePacket.Message(writer, message);
                                        BroadcastToSession(player.RoomCode, 0, writer);
                                        break;
                                    }
                                case "NextStage":
                                    {
                                        NetDataWriter writer = writerPool.Get();
                                        SerializePacket.Message(writer, message);
                                        BroadcastToSession(player.RoomCode, 0, writer);
                                        break;
                                    }
                                case "esc":
                                    {
                                        NetDataWriter writer = writerPool.Get();
                                        SerializePacket.Message(writer, message);
                                        BroadcastToSession(player.RoomCode, 0, writer);
                                        break;
                                    }
                                case "outesc":
                                    {
                                        NetDataWriter writer = writerPool.Get();
                                        SerializePacket.Message(writer, message);
                                        BroadcastToSession(player.RoomCode, 0, writer);
                                        break;
                                    }
                                case "DiscardResearch":
                                    {
                                        NetDataWriter writer = writerPool.Get();
                                        SerializePacket.Message(writer, message);
                                        BroadcastToSession(player.RoomCode, 0, writer);
                                        break;
                                    }
                                case "Expand":
                                    {
                                        NetDataWriter writer = writerPool.Get();
                                        SerializePacket.Message(writer, message);
                                        BroadcastToSession(player.RoomCode, 0, writer);
                                        break;
                                    }
                                case "CancelExpand":
                                    {
                                        NetDataWriter writer = writerPool.Get();
                                        SerializePacket.Message(writer, message);
                                        BroadcastToSession(player.RoomCode, 0, writer);
                                        break;
                                    }
                            }
                            break;
                        }
                    case PacketType.ChatMessage:
                        {
                            string sender = reader.GetString();
                            string message = reader.GetString();
                            session.messages.Add(sender + " : " + message);
                            NetDataWriter writer = writerPool.Get();
                            SerializePacket.ChatMessage(writer, sender, message);
                            BroadcastToSession(player.RoomCode, 0, writer);
                            break;
                        }
                    default:
                        {
                            NetDataWriter writer = writerPool.Get();
                            writer.Put((int)type);
                            foreach (byte Byte in reader.GetRemainingBytesSegment())
                            {
                                writer.Put(Byte);
                            }
                            BroadcastToSession(player.RoomCode, 0,  writer, player);
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                var inner = ex is TypeInitializationException tix ? tix.InnerException : ex;
                Task.Run(() => File.AppendAllText("log.txt", $"Ошибка: {inner!.Message}\nСтек: {inner.StackTrace}\n"));
            }
            finally
            {
                reader.Recycle();
            }
        }
        public void BroadcastToSession(string sessionCode, byte channel, NetDataWriter writer)
        {
            try
            {
                if (sessions.TryGetValue(sessionCode, out Session? session))
                {
                    foreach (KeyValuePair<string, Player> kvp in session.Players) SendPacket(kvp.Value.peer, channel, writer);
                }
            }
            catch (Exception ex)
            {
                var inner = ex is TypeInitializationException tix ? tix.InnerException : ex;
                File.AppendAllText("log.txt", $"Ошибка: {inner!.Message}\nСтек: {inner.StackTrace}\n");
            }
            finally
            {
                writerPool.Return(writer);
            }
        }
        public void BroadcastToSession(string sessionCode, byte channel, NetDataWriter writer, Player ignore)
        {
            try
            {
                if (sessions.TryGetValue(sessionCode, out Session? session))
                {
                    foreach (KeyValuePair<string, Player> kvp in session.Players) if (kvp.Value != ignore) SendPacket(kvp.Value.peer, channel, writer);
                }
            }
            finally
            {
                writerPool.Return(writer);
            }
        }
        static string GenerateSessionCode()
        {
            Span<byte> buf = stackalloc byte[16];
            RandomNumberGenerator.Fill(buf);
            const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            return string.Create(16, buf, (code, indexes) =>
            {
                for (int i = 0; i < 16; i++)
                {
                    code[i] = chars[indexes[i] % chars.Length];
                }
            });
        }
        public void SendPacket(NetPeer peer, byte DeliveryType, NetDataWriter writer)
        {
            try
            {
                if (peer.ConnectionState != ConnectionState.Connected) return;
                if (DeliveryType == 0) peer.Send(writer, 0, DeliveryMethod.ReliableOrdered);
                if (DeliveryType == 1) peer.Send(writer, 0, DeliveryMethod.ReliableUnordered);
                if (DeliveryType == 2) peer.Send(writer, 0, DeliveryMethod.ReliableSequenced);
            }
            catch (Exception ex)
            {
                var inner = ex is TypeInitializationException tix ? tix.InnerException : ex;
                File.AppendAllText("log.txt", $"Ошибка: {inner!.Message}\nСтек: {inner.StackTrace}\n");
            }
        }
        void ReceivePacket(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            try
            {
                if (peerToPlayer.TryGetValue(peer, out Player? player))
                {
                    if (deliveryMethod is DeliveryMethod.ReliableOrdered or DeliveryMethod.ReliableSequenced or DeliveryMethod.Sequenced) HandlePacket(player, reader);
                    else Task.Run(() => HandlePacket(player, reader));
                }
                else
                {
                    if (reader.GetInt() == (int)PacketType.InitPlayer) HandlePlayer(new Player(peer, reader.GetString(), reader.GetString(), reader.GetBool()), reader.GetBool());
                    else peer.Disconnect();
                }
            }
            catch (Exception ex)
            {
                var inner = ex is TypeInitializationException tix ? tix.InnerException : ex;
                File.AppendAllText("log.txt", $"Ошибка: {inner!.Message}\nСтек: {inner.StackTrace}\n");
            }
        }
        public class Session
        {
            public string SessionCode = "";
            public ConcurrentDictionary<string, Player> Players = [];
            public bool NeedConfirmed = false;
            public SessionState state = SessionState.Waiting;
            public List<string> messages = [];
        }
        public enum SessionState
        {
            Waiting,
            GameStarted
        }
        public int GetRoomCount()
        {
            return sessions.Count;
        }
        public List<string> GetRoomsCodes()
        {
            return [.. sessions.Keys];
        }
        public int GetPlayersCount(string code)
        {
            return sessions[code].Players.Count;
        }
        public List<string> GetPlayersNicknames(string code)
        {
            List<string> res = [];
            if (sessions.TryGetValue(code, out Session? session)) foreach (string key in session.Players.Keys) res.Add(key);
            return res;
        }
        public bool TryGetPlayer(string code, string nickname, out Player? result)
        {
            if (sessions.TryGetValue(code, out Session? session)) if (session.Players.TryGetValue(nickname, out Player? player))
            {
                result = player;
                return true;
            }
            result = null;
            return false;
        }
        public class NetDataWriterPolicy : IPooledObjectPolicy<NetDataWriter>
        {
            public NetDataWriter Create()
            {
                return new NetDataWriter(true, 100);
            }
            public bool Return(NetDataWriter obj)
            {
                if (obj.Length > 4096) return false;
                obj.Reset();
                return true;
            }
        }
    }
    public enum PacketType
    {
        None,
        InitPlayer,
        RoomCode,
        Message,
        PlayerPermisions,
        ConfirmUser,
        KickUser,
        PlayerList,
        ChatMessage,
        Save,
        NewAgent,
        ChangeAgent,
        EquipEGO,
        AddAgent,
        InitAgentAdd,
        RemoveAgent,
        DeployAgent,
        InitOfficers,
        WorkerMoveToMovable,
        WorkerMoveById,
        WorkerMoveByLink,
        CancelWork,
        CancelSuppress,
        StartWorkCreature,
        StartWorkKit,
        Suppress,
        ProgressWorkResult,
        Pause,
        Resume,
        Speed,
        SelectResearch,
        ConfirmResearch,
        WorkerDamageNoEffect,
        WorkerDamage,
        CreatureObserve,
        CreateEquipment,
        GetEGO,
        Overload,
        CreatureMoveToNode,
        CreatureMoveToMovable,
        WaitingPassage,
        SelectAno,
        InitSelectUI,
        SelectToExpand,
        BestRwbp,
        NewTitles,
        CreatureTakeDamage,
        CreatureChangeQliphoth,
    }
    public class Player(NetPeer peer, string roomCode, string Nickname, bool IsHost)
    {
        public NetPeer peer = peer;
        public string Nickname = Nickname;
        public string RoomCode = roomCode;
        public bool IsHost = IsHost;
        public bool IsConfirmed;
        public bool Ready = IsHost;
        public PlayerPermissions Permissions = new();
        public string EndConnectionState = "";
    }
    public struct PlayerPermissions()
    {
        public bool perm1;
    }
}

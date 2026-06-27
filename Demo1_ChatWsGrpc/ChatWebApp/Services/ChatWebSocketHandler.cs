using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Grpc.Net.Client;

namespace ChatWebApp.Services;

public record WsMessage(string Type, string Sender, string Content, string Room, string SentAt = "");

public class ChatWebSocketHandler
{
    private static readonly ConcurrentDictionary<string, List<WebSocket>> _rooms = new();
    private static readonly object _roomLock = new();
    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ILogger<ChatWebSocketHandler> _logger;
    private readonly IConfiguration _config;

    public ChatWebSocketHandler(ILogger<ChatWebSocketHandler> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    public async Task HandleAsync(WebSocket socket, string room, string sender)
    {
        lock (_roomLock)
        {
            if (!_rooms.ContainsKey(room))
                _rooms[room] = new List<WebSocket>();
            _rooms[room].Add(socket);
        }

        _logger.LogInformation("{Sender} se unió a la sala [{Room}]", sender, room);

        await SendHistoryAsync(socket, room);

        await BroadcastAsync(room, new WsMessage("system", "Sistema",
            $"{sender} se unió al chat", room,
            DateTime.UtcNow.ToString("HH:mm")), exclude: null);

        var buffer = new byte[4096];

        try
        {
            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                    break;

                var raw = Encoding.UTF8.GetString(buffer, 0, result.Count);
                var incoming = JsonSerializer.Deserialize<WsMessage>(raw,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (incoming == null) continue;

                await PersistToGrpcAsync(incoming);

                var outgoing = incoming with { SentAt = DateTime.UtcNow.ToString("HH:mm") };
                await BroadcastAsync(room, outgoing, exclude: null);
            }
        }
        catch (WebSocketException ex)
        {
            _logger.LogWarning("WebSocket cerrado inesperadamente: {Msg}", ex.Message);
        }
        finally
        {
            lock (_roomLock)
            {
                _rooms[room].Remove(socket);
            }

            await BroadcastAsync(room, new WsMessage("system", "Sistema",
                $"{sender} abandonó el chat", room,
                DateTime.UtcNow.ToString("HH:mm")), exclude: null);

            if (socket.State == WebSocketState.Open)
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Desconectado", CancellationToken.None);
        }
    }

    private async Task SendHistoryAsync(WebSocket socket, string room)
    {
        try
        {
            var grpcUrl = _config["GrpcService:Url"] ?? "http://localhost:5001";
            using var channel = GrpcChannel.ForAddress(grpcUrl);
            var client = new ChatGrpcService.ChatService.ChatServiceClient(channel);

            var stream = client.GetHistory(new ChatGrpcService.HistoryRequest { Room = room, Limit = 20 });

            while (await stream.ResponseStream.MoveNext(CancellationToken.None))
            {
                var msg = stream.ResponseStream.Current;
                var wsMsg = new WsMessage("history", msg.Sender, msg.Content, msg.Room,
                    DateTime.Parse(msg.SentAt).ToString("HH:mm"));
                await SendAsync(socket, wsMsg);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("No se pudo obtener historial gRPC: {Msg}", ex.Message);
        }
    }

    private async Task PersistToGrpcAsync(WsMessage msg)
    {
        try
        {
            var grpcUrl = _config["GrpcService:Url"] ?? "http://localhost:5001";
            using var channel = GrpcChannel.ForAddress(grpcUrl);
            var client = new ChatGrpcService.ChatService.ChatServiceClient(channel);

            await client.SendMessageAsync(new ChatGrpcService.ChatMessage
            {
                Sender  = msg.Sender,
                Content = msg.Content,
                Room    = msg.Room,
                SentAt  = DateTime.UtcNow.ToString("o")
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning("No se pudo persistir en gRPC: {Msg}", ex.Message);
        }
    }

    private static async Task BroadcastAsync(string room, WsMessage msg, WebSocket? exclude)
    {
        List<WebSocket> targets;
        lock (_roomLock)
        {
            if (!_rooms.TryGetValue(room, out var list)) return;
            targets = list.Where(s => s != exclude && s.State == WebSocketState.Open).ToList();
        }

        var json  = JsonSerializer.Serialize(msg, _jsonOpts);
        var bytes = Encoding.UTF8.GetBytes(json);

        foreach (var ws in targets)
        {
            try
            {
                await ws.SendAsync(new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch { /* socket ya cerrado */ }
        }
    }

    private static async Task SendAsync(WebSocket socket, WsMessage msg)
    {
        var json  = JsonSerializer.Serialize(msg, _jsonOpts);
        var bytes = Encoding.UTF8.GetBytes(json);
        await socket.SendAsync(new ArraySegment<byte>(bytes),
            WebSocketMessageType.Text, true, CancellationToken.None);
    }
}

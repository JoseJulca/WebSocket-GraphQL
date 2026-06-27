using Grpc.Core;

namespace ChatGrpcService.Services;

public class ChatServiceImpl : ChatService.ChatServiceBase
{
    private static readonly List<ChatMessage> _history = new();
    private static readonly object _lock = new();
    private readonly ILogger<ChatServiceImpl> _logger;

    public ChatServiceImpl(ILogger<ChatServiceImpl> logger)
    {
        _logger = logger;
    }

    public override Task<ChatResponse> SendMessage(ChatMessage request, ServerCallContext context)
    {
        var msg = new ChatMessage
        {
            Sender  = request.Sender,
            Content = request.Content,
            Room    = string.IsNullOrEmpty(request.Room) ? "general" : request.Room,
            SentAt  = DateTime.UtcNow.ToString("o")
        };

        lock (_lock)
        {
            _history.Add(msg);
            // Conservar máximo 100 mensajes en memoria
            if (_history.Count > 100)
                _history.RemoveAt(0);
        }

        _logger.LogInformation("[{Room}] {Sender}: {Content}", msg.Room, msg.Sender, msg.Content);

        return Task.FromResult(new ChatResponse
        {
            Success   = true,
            MessageId = Guid.NewGuid().ToString()
        });
    }

    public override async Task GetHistory(HistoryRequest request,
        IServerStreamWriter<ChatMessage> responseStream,
        ServerCallContext context)
    {
        List<ChatMessage> snapshot;
        lock (_lock)
        {
            var room = string.IsNullOrEmpty(request.Room) ? "general" : request.Room;
            var limit = request.Limit > 0 ? request.Limit : 20;
            snapshot = _history
                .Where(m => m.Room == room)
                .TakeLast(limit)
                .ToList();
        }

        foreach (var msg in snapshot)
        {
            await responseStream.WriteAsync(msg);
        }
    }
}

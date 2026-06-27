using ChatGrpcService.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGrpc();

var app = builder.Build();
app.MapGrpcService<ChatServiceImpl>();
app.MapGet("/", () => "ChatGrpcService activo. Usa un cliente gRPC para conectarte.");

app.Run();

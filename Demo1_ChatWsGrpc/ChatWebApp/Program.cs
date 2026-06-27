using ChatWebApp.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddSingleton<ChatWebSocketHandler>();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
});

// Endpoint WebSocket: /ws?room=general&sender=Juan
app.Map("/ws", async context =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = 400;
        return;
    }

    var room   = context.Request.Query["room"].FirstOrDefault()   ?? "general";
    var sender = context.Request.Query["sender"].FirstOrDefault() ?? "Anónimo";

    var socket  = await context.WebSockets.AcceptWebSocketAsync();
    var handler = context.RequestServices.GetRequiredService<ChatWebSocketHandler>();

    await handler.HandleAsync(socket, room, sender);
});

app.MapRazorPages();

app.Run();

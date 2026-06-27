using BookStoreApi.Data;
using BookStoreApi.GraphQL.Mutations;
using BookStoreApi.GraphQL.Queries;
using BookStoreApi.GraphQL.Subscriptions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<BookRepository>();

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddSubscriptionType<Subscription>()
    .AddFiltering()
    .AddSorting()
    .AddInMemorySubscriptions();

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseCors();
app.UseWebSockets();   // necesario para subscriptions de Hot Chocolate
app.MapGraphQL();

app.Run();

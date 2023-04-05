using Sparc.Blossom.Server.Example.TodoItem;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGrpcService<TodoItem>();

app.Run();

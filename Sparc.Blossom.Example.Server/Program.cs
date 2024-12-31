using Sparc.Blossom;
using Sparc.Blossom.Example.Single;
using TodoItems;

var builder = BlossomApplication.CreateBuilder(args);
var app = builder.Build();

var items = app.Services.CreateScope().ServiceProvider.GetRequiredService<IRepository<TodoItem>>();
await items.AddAsync(new TodoItem("First", "First one"));
await items.AddAsync(new TodoItem("Second", "Second one"));
await items.AddAsync(new TodoItem("Third", "Third one"));

await app.RunAsync<App>();

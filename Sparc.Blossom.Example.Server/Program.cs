using Sparc.Blossom;
using Sparc.Blossom.Data;
using Sparc.Blossom.Example.Single;
using Sparc.Blossom.Example.Single.TodoItem;

BlossomApplication.Run<App>(args, app: async x =>
{
    var items = x.Services.CreateScope().ServiceProvider.GetRequiredService<IRepository<TodoItem>>();
    await items.AddAsync(new TodoItem("First", "First one"));
    await items.AddAsync(new TodoItem("Second", "Second one"));
    await items.AddAsync(new TodoItem("Third", "Third one"));
});

using Sparc.Blossom;
using Sparc.Blossom.Data;

var builder = BlossomApplication.CreateBuilder(args);
// builder.Services.AddCosmos(builder.Configuration);
await builder.Build().RunAsync();
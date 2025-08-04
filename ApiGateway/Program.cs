using Orleans.Configuration;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOrleansClient(client =>
{
    client.UseLocalhostClustering(); // Connect to the local silo
    client.Configure<ClusterOptions>(options =>
    {
        options.ClusterId = "dev";
        options.ServiceId = "PokerPlatform";
    });
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers(); 

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.MapControllers();

app.Run();
using CCC.Api.Keys;
using CCC.Api.Projects;
using CCC.Api.SubscriptionModels;
using CCC.Api.Tenants;
using CCC.Api.Users;
using CCC.EventStore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("EventStore")
    ?? throw new InvalidOperationException("Connection string 'EventStore' is not configured.");

builder.Services.AddEventStore(connectionString);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

await EventStoreSchema.CreateAsync(connectionString);

app.MapKeyEndpoints();
app.MapTenantEndpoints();
app.MapProjectEndpoints();
app.MapSubscriptionModelEndpoints();
app.MapUserEndpoints();

app.Run();

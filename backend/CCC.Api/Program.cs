using CCC.Api;
using CCC.Api.Keys;
using CCC.Api.Projects;
using CCC.Api.SubscriptionModels;
using CCC.Api.Tenants;
using CCC.Api.Users;
using CCC.Commands;
using CCC.EventStore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("EventStore")
    ?? throw new InvalidOperationException("Connection string 'EventStore' is not configured.");

builder.Services.AddEventStore(connectionString);
builder.Services.AddCommandHandlers();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

await EventStoreSchema.CreateAsync(connectionString);

var api = app.MapGroup("").AddEndpointFilter<InvalidValueFilter>();
api.MapKeyEndpoints();
api.MapTenantEndpoints();
api.MapProjectEndpoints();
api.MapSubscriptionModelEndpoints();
api.MapUserEndpoints();

app.Run();

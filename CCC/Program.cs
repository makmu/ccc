using CCC.Infrastructure.EventStore;
using CCC.Organizations;
using CCC.Projects;
using CCC.SubscriptionModels;
using CCC.Users;
using Dapper;

SqlMapper.AddTypeHandler(new DateTimeOffsetTypeHandler());

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("EventStore")
    ?? throw new InvalidOperationException("Connection string 'EventStore' is not configured.");

builder.Services.AddSingleton(new EventStore(connectionString));
builder.Services.AddTransient<CommandContextBuilder>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

await EventStoreSchema.CreateAsync(connectionString);

app.MapOrganizationEndpoints();
app.MapProjectEndpoints();
app.MapSubscriptionModelEndpoints();
app.MapUserEndpoints();

app.Run();

using CCC.Infrastructure.EventStore;
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

app.MapPost("/organizations", (CCC.Organizations.AddOrganizationRequest request) =>
{
    return Results.Ok();
});

app.MapPost("/users", async (CCC.Users.AddUserRequest request, CommandContextBuilder contextBuilder) =>
{
    var context = await contextBuilder
        .Where<CCC.Users.UserAdded>(w => w
            .Or(e => e.Email, request.Email)
            .Or(e => e.Id, request.Id))
        .LoadAsync();

    if (context.Events.Count != 0)
        return Results.Conflict("A user with this email address or id already exists.");

    try
    {
        await context.AppendAsync(new CCC.Users.UserAdded(request.Id, request.Name, request.Email));
    }
    catch (ConcurrencyException)
    {
        return Results.Conflict("A concurrent operation modified the event store. Please retry.");
    }

    return Results.Ok();
});

app.Run();

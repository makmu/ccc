using CCC.Infrastructure.EventStore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("EventStore")
    ?? throw new InvalidOperationException("Connection string 'EventStore' is not configured.");

builder.Services.AddSingleton(new EventStore(connectionString));

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

app.MapPost("/users", async (CCC.Users.AddUserRequest request, EventStore eventStore) =>
{
    await eventStore.AppendAsync("UserAdded", new CCC.Users.UserAdded(request.Id, request.Name, request.Email), Guid.Empty);
    return Results.Ok();
});

app.Run();

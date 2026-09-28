using CCC.Api;
using CCC.Api.Keys;
using CCC.Api.Projects;
using CCC.Api.SubscriptionModels;
using CCC.Api.PublicTranslations;
using CCC.Api.Tenants;
using CCC.Api.Users;
using CCC.Commands;
using CCC.EventStore;
using CCC.ReadModels.PublicTranslations;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("EventStore")
    ?? throw new InvalidOperationException("Connection string 'EventStore' is not configured.");

var readModelsConnectionString = builder.Configuration.GetConnectionString("ReadModels")
    ?? throw new InvalidOperationException("Connection string 'ReadModels' is not configured.");

builder.Services.AddEventStore(connectionString);
builder.Services.AddCommandHandlers();
builder.Services.AddReadModelDatabase(readModelsConnectionString);
builder.Services.AddPublicTranslations();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

await EventStoreSchema.CreateAsync(connectionString);

var api = app.MapGroup("").AddEndpointFilter<InvalidValueFilter>();
api.MapAddSubscriptionModel();
api.MapAddUser();
api.MapRegisterTenant();
api.MapAddTeam();
api.MapAssignTenantOwner();
api.MapRenameTenant();
api.MapAddProject();
api.MapAddLanguage();
api.MapAddKey();
api.MapProvideReferenceText();
api.MapProvideTranslation();
api.MapRenameKey();
api.MapTranslationsOfProject();

app.Run();

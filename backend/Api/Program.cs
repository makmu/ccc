using CCC.Api;
using CCC.Api.AppAdministration;
using CCC.Api.TeamAdministration;
using CCC.Api.TenantAdministration;
using CCC.Api.TranslationDelivery;
using CCC.Api.TranslationEditor;
using CCC.EventStore;
using CCC.Modules.AppAdministration;
using CCC.Modules.TeamAdministration;
using CCC.Modules.TenantAdministration;
using CCC.Modules.TranslationDelivery;
using CCC.Modules.TranslationEditor;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("EventStore")
    ?? throw new InvalidOperationException("Connection string 'EventStore' is not configured.");

var readModelsConnectionString = builder.Configuration.GetConnectionString("ReadModels")
    ?? throw new InvalidOperationException("Connection string 'ReadModels' is not configured.");

builder.Services.AddEventStore(connectionString);
builder.Services.AddAppAdministration();
builder.Services.AddTenantAdministration();
builder.Services.AddTeamAdministration();
builder.Services.AddTranslationEditor();
builder.Services.AddReadModelDatabase(readModelsConnectionString);
builder.Services.AddTranslationDelivery();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // In development the Vite dev server proxies plain HTTP, and a redirect would send the browser past the proxy.
    app.UseHttpsRedirection();
}

await EventStoreSchema.CreateAsync(connectionString);

var api = app.MapGroup("").AddEndpointFilter<InvalidValueFilter>();
api.MapAddSubscriptionModel();
api.MapAddUser();
api.MapRegisterTenant();
api.MapSubscriptionModels();
api.MapTenants();
api.MapUsers();
api.MapAddTeam();
api.MapAssignTenantOwner();
api.MapRenameTenant();
api.MapDetailsOfTenant();
api.MapTeamsOfTenant();
api.MapAddProject();
api.MapAddLanguage();
api.MapProjectsOfTeam();
api.MapAddKey();
api.MapProvideReferenceText();
api.MapProvideTranslation();
api.MapRenameKey();
api.MapKeysOfProject();
api.MapTranslationsOfProject();
api.MapLanguagesOfProject();

app.Run();

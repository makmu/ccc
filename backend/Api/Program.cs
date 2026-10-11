using CCC.Api.AppAdministration;
using CCC.Api.TeamAdministration;
using CCC.Api.TenantAdministration;
using CCC.Api.TranslationDelivery;
using CCC.Api.TranslationEditor;
using CCC.Api;
using CCC.EventStore;
using CCC.Modules.AppAdministration;
using CCC.Modules.TeamAdministration;
using CCC.Modules.TenantAdministration;
using CCC.Modules.TranslationDelivery;
using CCC.Modules.TranslationEditor;
using CCC.ProjectionBase;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.RespectNullableAnnotations = true;
    options.SerializerOptions.RespectRequiredConstructorParameters = true;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SupportNonNullableReferenceTypes();
    options.SchemaFilter<ConstructorParametersAsRequiredSchemaFilter>();
});

var connectionString = builder.Configuration.GetConnectionString("EventStore")
    ?? throw new InvalidOperationException("Connection string 'EventStore' is not configured.");

var projectionsConnectionString = builder.Configuration.GetConnectionString("Projections")
    ?? throw new InvalidOperationException("Connection string 'Projections' is not configured.");

builder.Services.AddEventStore(connectionString);
builder.Services.AddAppAdministration();
builder.Services.AddTenantAdministration();
builder.Services.AddTeamAdministration();
builder.Services.AddTranslationEditor();
builder.Services.AddProjectionDatabase(projectionsConnectionString);
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

var api = app.MapGroup("")
    .AddEndpointFilter<RequestAbortedFilter>()
    .AddEndpointFilter<InvalidValueFilter>();
api.MapAppAdministration();
api.MapTenantAdministration();
api.MapTeamAdministration();
api.MapTranslationEditor();
api.MapTranslationDelivery();

app.Run();

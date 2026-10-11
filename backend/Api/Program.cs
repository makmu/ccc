using CCC.Api.AppAdministration;
using CCC.Api.TeamAdministration;
using CCC.Api.TenantAdministration;
using CCC.Api.TranslationDelivery;
using CCC.Api.TranslationEditor;
using CCC.Api;
using CCC.Base.Projections;
using CCC.EventStore;
using CCC.Modules.AppAdministration;
using CCC.Modules.TeamAdministration;
using CCC.Modules.TenantAdministration;
using CCC.Modules.TranslationDelivery;
using CCC.Modules.TranslationEditor;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.RespectNullableAnnotations = true;
    options.SerializerOptions.RespectRequiredConstructorParameters = true;
});
builder.Services.AddProblemDetails();
// In Development, minimal APIs throw on binding failures instead of returning 400.
builder.Services.AddExceptionHandler(options => options.StatusCodeSelector = exception =>
    exception is BadHttpRequestException badRequest ? badRequest.StatusCode : StatusCodes.Status500InternalServerError);
builder.Services.AddOpenApi();

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

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "v1"));
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

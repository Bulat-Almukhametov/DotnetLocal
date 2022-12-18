using System.Globalization;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.Configure<JsonOptions>(opt =>
{
    opt.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(opt =>
{
    opt.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var supportedCultures = Enum.GetNames(typeof(AllowedCultures));
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures.First())
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

app.UseRequestLocalization(localizationOptions);

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", (IStringLocalizer<SharedResource> sharedLocalizer) =>
    {
        var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    sharedLocalizer[summaries[Random.Shared.Next(summaries.Length)]]
                ))
            .ToArray();
        return forecast;
    })
    .WithName("GetWeatherForecast")
    .WithOpenApi();

app.MapPost("/culture", (
        [FromBody] SetCultureRequest body,
        HttpResponse response,
        [FromServices] IStringLocalizer<SharedResource> sharedLocalizer
    ) =>
    {
        var cultureName = Enum.GetName(body.Culture) ?? throw new Exception(sharedLocalizer["Unsupported language"]);
        RequestCulture requestCulture = new(cultureName);
        var cookieValue = CookieRequestCultureProvider.MakeCookieValue(requestCulture);

        response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            cookieValue
        );

        return requestCulture.Culture.DisplayName;
    })
    .WithName("SetCulture")
    .WithOpenApi();

app.MapGet("/culture", () => CultureInfo.CurrentCulture.DisplayName)
    .WithName("GetCulture")
    .WithOpenApi();

app.Run();

internal enum AllowedCultures
{
    EN,
    RU,
    TAT
}

internal class SetCultureRequest
{
    public AllowedCultures Culture { get; set; }
}

internal class SharedResource
{
}

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
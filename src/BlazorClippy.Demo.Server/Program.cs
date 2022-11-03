using BlazorClippy;
using BlazorClippy.AI;
using BlazorClippy.Demo.Server;
using BlazorClippy.Demo.Server.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<WeatherForecastService>();

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://localhost:7008/api") });
builder.Services.AddScoped<ClippyService>();
builder.Services.AddMvc(options => options.EnableEndpointRouting = false).SetCompatibilityVersion(CompatibilityVersion.Version_3_0);
builder.Services.AddSwaggerGen();

var config = builder.Configuration.GetSection("WatsonConfig").Get<WatsonConfigDto>();
if (config != null)
{
    MainDataContext.WatsonConfig = config;
    MainDataContext.TextToSpeech = new WatsonTextToSpeech(config.SpeechToTextApiKey, 
                                                          config.SpeechToTextUrl, 
                                                          config.SpeechToTextVoice);

    MainDataContext.Translator = new WatsonTranslator(config.TranslatorApiKey,
                                                      config.TranslatorUrl);
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Blazor Clippy Server API");
});

app.UseCors(c => c.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
app.UseMvcWithDefaultRoute();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();

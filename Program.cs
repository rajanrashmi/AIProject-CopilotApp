using CopilotApp3.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using MudBlazor.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped(sp =>
    new HttpClient
    {
        BaseAddress = new Uri(builder.Configuration["FrontendUrl"] ??
            "https://localhost:7197")
    });

builder.Services.AddMudServices();
builder.Services.AddHttpClient();
 


// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapPost("/api/copilot",   async (HttpContext httpContext,[FromBody] PromptRequest req) =>
{
    // Retrieve IConfiguration from the HttpContext's request services
    var config = httpContext.RequestServices.GetRequiredService<IConfiguration>();

    // Initialize SK (cache or inject for production)
    var builder = Kernel.CreateBuilder();
    string? apikey = config["OpenAI:ApiKey"];
    builder.AddOpenAIChatCompletion("gpt-4", apikey!);
    var kernel = builder.Build();
    var agent = new ChatCompletionAgent
    {
        Name = "Copilot",
        Instructions = " Ask Copilot.",
        Kernel = kernel
    };

    var chat = new ChatHistory();
    chat.AddUserMessage(req.Prompt);

    // Invoke and return the first response
    await foreach (var msg in agent.InvokeAsync(chat))
    {
        return Results.Ok(new { text = msg.Message.Content });
    }
    return Results.BadRequest("No response.");
});

app.Run();
public record PromptRequest(string Prompt);

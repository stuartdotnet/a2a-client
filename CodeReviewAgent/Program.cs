
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.AI;

var builder = WebApplication.CreateBuilder(args);

IChatClient chatClient = new AzureOpenAIClient(
        new Uri(builder.Configuration["AZURE_OPENAI_ENDPOINT"]),
        new DefaultAzureCredential())
    .GetChatClient(builder.Configuration["AZURE_OPENAI_DEPLOYMENT_NAME"])
    .AsIChatClient();

builder.Services.AddSingleton(chatClient);

var reviewAgent = builder.AddAIAgent(
    "code-review",
    instructions: "You are a .NET code review specialist. Review code for quality, " +
                  "correctness, and best practices. Be specific and actionable.");

var app = builder.Build();

app.Use(async (context, next) =>                          
{
    if (context.Request.Path.StartsWithSegments("/a2a/code-review"))
        app.Logger.LogInformation("Code Review Agent: review request received");
    await next();
});

app.MapA2A(reviewAgent, path: "/a2a/code-review", agentCard: new()
{
    Name = "Code Review Agent",
    Description = "Reviews C# .NET code for quality, correctness, and best practices.",
    Version = "1.0.0",
    Capabilities = new() { Streaming = true }
});

app.Run();
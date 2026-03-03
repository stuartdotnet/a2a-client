using System.Text;

var builder = WebApplication.CreateBuilder(args);                                                                                                                                           
                                                                                                                                                                                              
  builder.Services.AddSingleton<AgentRegistry>();                                                                                                                                          
  builder.Services.AddHttpClient<A2AClient>();

  var app = builder.Build();

  var registry = app.Services.GetRequiredService<AgentRegistry>();
  await registry.RegisterAsync("http://localhost:5001/a2a/code-review/v1/card");

  app.MapPost("/review", async (
      ReviewRequest req,
      AgentRegistry registry,
      A2AClient client,
      CancellationToken cancellationToken) =>
  {
      var specialist = registry.FindBySkill("code-review").FirstOrDefault()
          ?? registry.Find("Code Review Agent");

      if (specialist is null)
          return Results.Problem("No code review agent available.");

      var streamUrl = $"{specialist.Card.Url}/v1/message:stream";
      Console.WriteLine($"Calling: {streamUrl}");

      var result = new StringBuilder();

      await foreach (var chunk in client.StreamAsync(streamUrl, req.Code, cancellationToken))
          result.Append(chunk);

      // Easy-to-read result
      return Results.Text(result.ToString());
  });

  app.Run();

  record ReviewRequest(string Code);

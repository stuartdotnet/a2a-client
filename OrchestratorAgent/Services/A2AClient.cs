using System.Runtime.CompilerServices;
using System.Text.Json;

public class A2AClient
{
    private readonly HttpClient _httpClient;

    public A2AClient(HttpClient httpClient) => _httpClient = httpClient;

    public async IAsyncEnumerable<string> StreamAsync(
        string streamUrl,
        string userMessage,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            message = new
            {
                kind = "message",
                role = "user",
                parts = new[] { new { kind = "text", text = userMessage } },
                messageId = Guid.NewGuid().ToString(),
                contextId = Guid.NewGuid().ToString()
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, streamUrl)
        {
            Content = JsonContent.Create(payload)
        };

        // ResponseHeadersRead streams the body instead of buffering the full response
        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line?.StartsWith("data: ") != true) continue;

            var json = line["data: ".Length..];
            if (json == "[DONE]") break;

            // Useful for debugging raw SSE events from the agent — uncomment to see each chunk as it arrives
            // Console.WriteLine($"SSE event: {json}"); 

            // Each SSE event is a JSON-RPC response object — parse for artifact updates
            var text = ExtractTextFromEvent(json);
            if (text is not null)
                yield return text;
        }
    }

    private static string? ExtractTextFromEvent(string json)                                                                                                                                    
  {                                                                                                                                                                                           
      try
      {
          using var doc = JsonDocument.Parse(json);
          var root = doc.RootElement;

          if (root.TryGetProperty("parts", out var parts) &&
              parts.GetArrayLength() > 0)
          {
              var part = parts[0];
              if (part.TryGetProperty("text", out var text))
                  return text.GetString();
          }

          return null;
      }
      catch (JsonException)
      {
          return null;
      }
  }
}
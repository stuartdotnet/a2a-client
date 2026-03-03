public class AgentRegistry
{
    private readonly HttpClient _httpClient;
    private readonly List<RegisteredAgent> _agents = new();

    public AgentRegistry(IHttpClientFactory httpClientFactory)
    => _httpClient = httpClientFactory.CreateClient();


    public async Task RegisterAsync(string cardUrl)
    {
        try
        {
            var card = await _httpClient.GetFromJsonAsync<AgentCard>(cardUrl);
            if (card is not null)
            {
                _agents.Add(new RegisteredAgent(card));
                Console.WriteLine($"Registered: {card.Name} — {card.Description}");
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Warning: could not reach {cardUrl}. {ex.Message}");
            // Non-fatal — orchestrator starts without this specialist
        }
    }

    // Find by name — extend this to match by skill tag or capability
    public RegisteredAgent? Find(string name) =>
        _agents.FirstOrDefault(a => a.Card.Name == name);

    // Find agents that support a given skill tag
    public IEnumerable<RegisteredAgent> FindBySkill(string tag) =>
        _agents.Where(a =>
            a.Card.Skills?.Any(s => s.Tags?.Contains(tag) == true) == true);
}

public record RegisteredAgent(AgentCard Card);
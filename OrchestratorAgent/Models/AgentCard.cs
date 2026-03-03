public record AgentCard(
    string Name,
    string Description,
    string Version,
    string Url,
    AgentCapabilities? Capabilities,
    AgentSkill[]? Skills);

public record AgentCapabilities(bool Streaming, bool PushNotifications);

public record AgentSkill(
    string Id,
    string Name,
    string Description,
    string[]? Tags);
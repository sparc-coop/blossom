namespace Sparc.Core.Chat;

public record AllowCondition(string Type, string? RoomId = null);
public record JoinRules(string JoinRule, List<AllowCondition>? Allow = null);
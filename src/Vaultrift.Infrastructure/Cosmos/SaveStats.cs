using System.Text.Json;

namespace Vaultrift.Infrastructure.Cosmos;

public sealed record SaveStats(int Deaths, long Coins, int Turns, bool Conquered, long PlaySeconds)
{
    public static SaveStats Empty { get; } = new(0, 0, 0, false, 0);

    public static SaveStats From(string data)
    {
        try
        {
            using var document = JsonDocument.Parse(data);
            var root = document.RootElement;
            var game = Property(root, "Game");
            var player = game is { } found ? Property(found, "Player") : null;

            return new SaveStats(
                (int)Number(game, "Deaths"),
                Number(player, "Coins"),
                (int)Number(game, "TurnNumber"),
                game is { } state && Property(state, "IsConquered") is { ValueKind: JsonValueKind.True },
                Number(root, "PlaySeconds"));
        }
        catch (JsonException)
        {
            return Empty;
        }
    }

    private static JsonElement? Property(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) ? value : null;

    private static long Number(JsonElement? element, string name) =>
        element is { } found && Property(found, name) is { ValueKind: JsonValueKind.Number } value && value.TryGetInt64(out var number) ? number : 0;
}

using Habbo.Infrastructure;

namespace Habbo.Silo.Pets;

/// <summary>What pets say and how often they refuse a command, read from seed/pet_speech.json once.</summary>
public sealed class PetSpeech(SeedFiles seedFiles)
{
    private const string SeedFile = "pet_speech.json";

    public const string Speak = "speak";
    public const string Refuse = "refuse";
    public const string Hungry = "hungry";
    public const string Eat = "eat";
    public const string Drink = "drink";
    public const string Thirsty = "thirsty";
    public const string Sleep = "sleep";
    public const string Play = "play";
    public const string LevelUp = "levelUp";
    public const string Greet = "greet";
    public const string Wag = "wag";

    /// <summary>The postures of the Play (pet.command.11) and Wag Tail (pet.command.44) commands.</summary>
    private const string PlayPosture = "pla";
    private const string WagPosture = "wag";

    /// <summary>Where a line names the pet's owner.</summary>
    private const string OwnerPlaceholder = "%owner%";

    private SpeechFile? _speech;
    private readonly SemaphoreSlim _loading = new(1, 1);

    /// <summary>A line of the kind, picked at random, naming the owner where it does; null for a kind with none.</summary>
    public async Task<string?> LineAsync(string kind, Random random, string ownerName = "")
    {
        var lines = (await LoadAsync()).Lines?.GetValueOrDefault(kind);
        return lines is not { Count: > 0 } ? null : lines[random.Next(lines.Count)].Replace(OwnerPlaceholder, ownerName, StringComparison.Ordinal);
    }

    /// <summary>The kind of line a pet says as it takes up a posture it was told to, if any: playing, "*is playing*" or
    /// "*bounces around*"; wagging its tail, "*wags tail at %owner%*". The lines say what the pet is doing, as habbo.com's
    /// eat and drink lines do at the bowl; the other postures go unsaid.</summary>
    public static string? ForPosture(string posture) => posture switch
    {
        PlayPosture => Play,
        WagPosture => Wag,
        _ => null,
    };

    /// <summary>The chance, between 0 and 1, that a pet refuses a command.</summary>
    public async Task<double> RefuseChanceAsync() => (await LoadAsync()).RefuseChance;

    private async Task<SpeechFile> LoadAsync()
    {
        if (_speech is not null)
            return _speech;

        await _loading.WaitAsync();
        try
        {
            return _speech ??= await seedFiles.ReadJsonAsync<SpeechFile>(SeedFile) ?? new SpeechFile(0, []);
        }
        finally
        {
            _loading.Release();
        }
    }

    private sealed record SpeechFile(double RefuseChance, Dictionary<string, List<string>>? Lines);
}

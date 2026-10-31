using Habbo.Domain.Pets;
using Habbo.Infrastructure;
using Habbo.Silo.Pets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Habbo.Silo.Tests;

public class PetBehaviourTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);

    /// <summary>The hotel's pet rules (appsettings): 120 energy and 100 nutrition at level 1, hungry below 25%, tired
    /// below 20%.</summary>
    private static PetRules Rules() => new(Options.Create(new PetOptions
    {
        EnergyBase = 120, EnergyPerLevel = 20, NutritionBase = 100, NutritionPerLevel = 0,
        EnergyRegainedPerMinute = 2, NutritionLostPerMinute = 1, HungryBelowPercent = 25, TiredBelowPercent = 20,
    }));

    private static Pet PetWith(int energy, int nutrition) => new() { Level = 1, Energy = energy, Nutrition = nutrition, VitalsAt = Now };

    private static SeedFiles Seed() => new(new ConfigurationBuilder().Build());

    [Fact]
    public void AWellPetNeedsNothing() => Assert.Empty(Rules().Needs(PetWith(energy: 120, nutrition: 100), Now));

    [Fact]
    public void AHungryPetEats() => Assert.Equal([PetCare.Eat], Rules().Needs(PetWith(energy: 120, nutrition: 24), Now));

    [Fact]
    public void ATiredPetRestsOrFailingANestDrinks() =>
        Assert.Equal([PetCare.Rest, PetCare.Drink], Rules().Needs(PetWith(energy: 23, nutrition: 100), Now));

    [Fact]
    public void AHungryTiredPetEatsFirst() =>
        Assert.Equal([PetCare.Eat, PetCare.Rest, PetCare.Drink], Rules().Needs(PetWith(energy: 0, nutrition: 0), Now));

    [Fact]
    public void TimeMovesThePetsNeedsOn() =>
        // ten minutes on, 20 energy back takes it past tired, and 10 nutrition gone makes it hungry
        Assert.Equal([PetCare.Eat], Rules().Needs(PetWith(energy: 10, nutrition: 30), Now.AddMinutes(10)));

    [Theory]
    [InlineData("pla", PetSpeech.Play)]
    [InlineData("wag", PetSpeech.Wag)]
    [InlineData("sit", null)]
    [InlineData("jmp", null)]
    public void PlayingAndWaggingAreSaid(string posture, string? kind) => Assert.Equal(kind, PetSpeech.ForPosture(posture));

    [Fact]
    public async Task TheThirstyPlayAndWagLinesAreSeeded()
    {
        var speech = new PetSpeech(Seed());
        var random = new Random(1);

        Assert.Contains("*looking for water*", await AllLinesAsync(speech, PetSpeech.Thirsty, random));
        Assert.Contains("*is playing*", await AllLinesAsync(speech, PetSpeech.Play, random));
        Assert.Equal("*wags tail at Bobba*", await speech.LineAsync(PetSpeech.Wag, random, "Bobba"));
    }

    [Fact]
    public async Task PlayFootballIsTheClientsCommandSeventeen()
    {
        var commands = await Seed().ReadJsonAsync<SeedCommands>("pet_commands.json");

        var football = Assert.Single(commands!.Commands, c => c.Id == 17);
        Assert.Equal(PetCommandAction.Football, football.Action);
        Assert.Null(football.Posture);
        Assert.True(football.Experience > 0 && football.Energy > 0);
    }

    private static async Task<HashSet<string?>> AllLinesAsync(PetSpeech speech, string kind, Random random)
    {
        var lines = new HashSet<string?>();
        for (var i = 0; i < 100; i++)
            lines.Add(await speech.LineAsync(kind, random));
        return lines;
    }

    private sealed record SeedCommands(List<SeedCommand> Commands);

    private sealed record SeedCommand(int Id, PetCommandAction Action, int Level, int Experience, int Energy, string? Posture);
}

using Habbo.Silo.Abstractions.Chat;
using Habbo.Silo.Rooms;

namespace Habbo.Silo.Tests;

public class EmoticonGestureTests
{
    /// <summary>A stand-in for the seeded chat_gestures rows.</summary>
    private static readonly (string Emoticon, Gesture Gesture)[] Map =
    [
        (">:(", Gesture.Aggravated),
        (":@", Gesture.Aggravated),
        (":D", Gesture.Joy),
        (":-D", Gesture.Joy),
        (":)", Gesture.Smile),
        (":-)", Gesture.Smile),
        ("=)", Gesture.Smile),
        (":(", Gesture.Sad),
        (":-(", Gesture.Sad),
        (":o", Gesture.Surprised),
        (":O", Gesture.Surprised),
        (":p", Gesture.Tongue),
        (":P", Gesture.Tongue),
        (";)", Gesture.Eyebrows),
    ];

    [Theory]
    [InlineData("hello :)", Gesture.Smile)]
    [InlineData("haha :D", Gesture.Joy)]
    [InlineData("no >:(", Gesture.Aggravated)]
    [InlineData("oh :(", Gesture.Sad)]
    [InlineData("wow :o", Gesture.Surprised)]
    [InlineData("hehe :p", Gesture.Tongue)]
    [InlineData("hey ;)", Gesture.Eyebrows)]
    [InlineData("plain text", Gesture.None)]
    public void DetectsTheGestureForAnEmoticon(string text, Gesture expected) =>
        Assert.Equal(expected, GestureMatcher.Detect(text, Map));

    [Fact]
    public void TheFirstEmoticonWins() =>
        Assert.Equal(Gesture.Sad, GestureMatcher.Detect(":( then :)", Map));

    [Fact]
    public void AngryIsNotMistakenForSad() =>
        Assert.Equal(Gesture.Aggravated, GestureMatcher.Detect(">:(", Map));

    [Fact]
    public void TheLongerEmoticonWinsAtTheSamePosition() =>
        Assert.Equal(Gesture.Joy, GestureMatcher.Detect(":-D", Map));
}

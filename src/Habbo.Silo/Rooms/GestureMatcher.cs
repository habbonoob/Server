using Habbo.Silo.Abstractions.Chat;

namespace Habbo.Silo.Rooms;

/// <summary>Picks the gesture for a chat message: whichever known emoticon appears earliest
/// in the text wins. When two start at the same place the longer one is the more specific
/// match, so "&gt;:(" reads as angry rather than sad.</summary>
public static class GestureMatcher
{
    public static Gesture Detect(string text, IReadOnlyList<(string Emoticon, Gesture Gesture)> map)
    {
        var earliest = int.MaxValue;
        var matchedLength = 0;
        var gesture = Gesture.None;

        foreach (var (emoticon, candidate) in map)
        {
            var index = text.IndexOf(emoticon, StringComparison.Ordinal);
            if (index < 0)
                continue;

            var isEarlier = index < earliest;
            var isMoreSpecific = index == earliest && emoticon.Length > matchedLength;
            if (!isEarlier && !isMoreSpecific)
                continue;

            earliest = index;
            matchedLength = emoticon.Length;
            gesture = candidate;
        }

        return gesture;
    }
}

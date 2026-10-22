namespace Habbo.Protocol.Messages.Outgoing.FriendList;

/// <summary>What a console message holds, as September 2026 reads it: text, or a Habbicon (its id in place of text).</summary>
public enum ConsoleMessageType
{
    Text = 0,
    Habbicon = 1,
}

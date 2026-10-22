using Habbo.Domain.Users;
using Habbo.Protocol.Messages.Outgoing.FriendList;
using Habbo.Silo.Abstractions.Friends;
using Microsoft.Extensions.Options;

namespace Habbo.Gateway.Messages;

/// <summary>Turns a friend list into what the client reads. The client asks for it once and is
/// then only told about changes, so the same shapes serve both and live in one place.</summary>
public sealed class FriendListEvents(IOptions<HotelOptions> hotel)
{
    public MessengerInitComposer Init(FriendListView friendList)
    {
        return new MessengerInitComposer(
            friendList.FriendLimit,
            friendList.NormalFriendLimit,
            friendList.ExtendedFriendLimit,
            friendList.Categories
                .Select(category => new FriendCategoryData(category.Id, category.Name))
                .ToList());
    }

    /// <summary>The friend list in fragments the client can stitch back together. A list that
    /// fits in one is still one fragment of one: the client counts them, so sending none would
    /// leave it waiting.</summary>
    public IEnumerable<FriendListFragmentMessageComposer> Fragments(FriendListView friendList)
    {
        var size = Math.Max(1, hotel.Value.FriendListFragmentSize);
        var total = Math.Max(1, (friendList.Friends.Count + size - 1) / size);

        for (var fragment = 0; fragment < total; fragment++)
        {
            yield return new FriendListFragmentMessageComposer(
                total,
                fragment,
                friendList.Friends.Skip(fragment * size).Take(size).Select(ToWire).ToList());
        }
    }

    public FriendData ToWire(FriendView friend) => FriendData.From(friend);
}

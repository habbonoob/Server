# Builders Club

The warehouse is Habbo's `BUILDERS_CLUB` catalog, captured on 2026-10-22 through the relay (173 pages,
4,799 offers) into `seed/catalog_builders_club.json`. It is seeded beside the shop with `catalog_pages.BuildersClub`
set, and the index and page requests answer in whichever mode the client asks in.

## Measured on Habbo (2026-10-22)

- An account without membership gets status `[0, 0, 500, 0]` (seconds left, limit, max limit, seconds with grace),
  so it can't place anything. The free trial with a limit no longer exists.
- Membership is sold on the website (`builders_club.buy_membership_page`), at 85 credits + 85 diamonds a month.
  Nothing in the client buys it.
- Every warehouse offer has `rentofferid -1`, so renting and buying out are gone.

## From the client (October 2026), not measured

Placement can't be tried on Habbo without a membership. These rules come from the client's own checks
(`HabboCatalog.getBuilderFurniPlaceableStatus`) and the external texts:

- Only the owner, or a group's admin in its room, may place. The membership that counts is the room owner's
  (one of Habbo's texts for this check reads "The owner of this room has no Builders Club"), so each piece is the
  owner's and counts against their limit (`builders_club_limits.json`). Picking a piece up sends it back to the
  warehouse.
- With no membership time left (the "trial", or a lapsed member in grace), no one but staff may be in the room.
  The first piece asks `room.confirm.hide_room` (`BuildersClubPlacementWarning`, which the client sends again
  confirmed) and hides the room from the navigator.
- A room holding Builders Club furni takes no visitors while its owner has no membership time left. The visitor
  gets `builders_club.visit_denied_for_visitor` and the owner `builders_club.visit_denied_for_owner`.

## Guessed
- After the grace period the limit is 0, as for an account that never joined, so a lapsed member can no longer
  build. Through the grace period the tier's limit still holds.
- Borrowed furni counts toward none of the room-decoration achievements (furni count, kinds, credit value, holes).

- The visitor is sent back with `CloseConnection` after the notification. What Habbo sends is unknown.
- The hidden rooms are unhidden, with `builders_club.membership_renewed`, at the owner's first login with
  membership time. Habbo learns of a renewal from its website; when it tells the player is unknown.
- `builders_club.room_locked`, `membership_made` and `membership_extended` aren't sent.

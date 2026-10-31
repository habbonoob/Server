# Builders Club

The warehouse is Habbo's `BUILDERS_CLUB` catalog, captured on 2026-10-22 through the relay (173 pages,
4,799 offers) into `seed/catalog_builders_club.json`. It is seeded beside the shop with `catalog_pages.BuildersClub`
set, and the index and page requests answer in whichever mode the client asks in.

## Measured on Habbo (2026-10-22)

- An account without membership gets status `[0, 0, 500, 0]` (seconds left, limit, max limit, seconds with grace),
  so it can't place anything. The free trial with a limit no longer exists.
- Membership is sold on the website (`builders_club.buy_membership_page`), at 85 credits + 85 diamonds a month. The
  shop's `builders_club_frontpage` page lists `builders_club_14_days` and `builders_club_31_days` (10,000 credits plus
  10,000 shells or duckets), but its widget only links to the website.
- In the client it is bought on `loyalty_bc` (`builders_club_loyalty`): `loyalty_bc_31_days`, 105 credits + 105 diamonds,
  through the ordinary `PurchaseFromCatalog`. `builders_upgrades` (`builders_club_addons`) sells +45, +250 and +600 furni
  for credits + diamonds, buyable only with membership time left. Habbo sends all of these offers with no products;
  the client knows them by a code starting `builders_club` or `loyalty_bc`.
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
- `builders_club.room_locked` isn't sent.
- A membership bought in the catalog (`seed/builders_club_memberships.json` names each offer's days and tier) goes on
  top of the time left, never lowers the tier, unhides the rooms, and is answered with the new status and
  `membership_made` (no time left before) or `membership_extended`. Habbo's answer is not captured.
- The furni add-ons are not sold yet: a limit bought on top of the tier needs a place to keep it.

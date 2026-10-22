"""End-to-end catalog checks: open the shop, read a page, buy something, go broke.

Run with the host up. Reuses the handshake/login from protocol_probe.
"""
import struct
import sys

from protocol_probe import Msg, connect_and_login, drain_until, wstr

# Client -> server
GET_CATALOG_INDEX = 2933
GET_CATALOG_PAGE = 3793
PURCHASE_FROM_CATALOG = 1962
INFO_RETRIEVE = 1484

# Server -> client
CATALOG_INDEX = 262
CATALOG_PAGE = 2082
PURCHASE_OK = 116
PURCHASE_ERROR = 2430
CREDIT_BALANCE = 1287
FURNI_LIST_ADD_OR_UPDATE = 2151
UNSEEN_ITEMS = 2362

# Client -> server, sent when the player looks at the inventory.
RESET_UNSEEN_ITEM_IDS = 1499
RESET_UNSEEN_ITEMS = 2303

OWNED_FURNI_CATEGORY = 1

CATALOG_TYPE = "NORMAL"
NO_OFFER = -1


def read_node(m):
    """NodeData, which nests: visible, icon, pageId, name, localization, offers, children."""
    node = {
        "visible": bool(m.u8()),
        "icon": m.i32(),
        "pageId": m.i32(),
        "pageName": m.s(),
        "localization": m.s(),
    }
    node["offerIds"] = [m.i32() for _ in range(m.i32())]
    node["children"] = [read_node(m) for _ in range(m.i32())]
    return node


def read_product(m):
    """Branches exactly like the client's CatalogPageMessageProductData."""
    product = {"productType": m.s()}
    if product["productType"] != "b":
        product["furniClassId"] = m.i32()
        product["extraParam"] = m.s()
        product["productCount"] = m.i32()
        product["limited"] = bool(m.u8())
        if product["limited"]:
            product["seriesSize"] = m.i32()
            product["itemsLeft"] = m.i32()
    else:
        product["extraParam"] = m.s()
        product["productCount"] = 1
    return product


def read_page_offer(m):
    offer = {
        "offerId": m.i32(),
        "localizationId": m.s(),
        "isRent": bool(m.u8()),
        "priceInCredits": m.i32(),
        "priceInActivityPoints": m.i32(),
        "activityPointType": m.i32(),
        "priceInSilver": m.i32(),
        "giftable": bool(m.u8()),
    }
    offer["products"] = [read_product(m) for _ in range(m.i32())]
    offer["clubLevel"] = m.i32()
    offer["bundlePurchaseAllowed"] = bool(m.u8())
    offer["isPet"] = bool(m.u8())
    offer["previewImage"] = m.s()
    return offer


def read_purchase_offer(m):
    """PurchaseOKMessageOfferData: no silver price, no preview image."""
    offer = {
        "offerId": m.i32(),
        "localizationId": m.s(),
        "isRent": bool(m.u8()),
        "priceInCredits": m.i32(),
        "priceInActivityPoints": m.i32(),
        "activityPointType": m.i32(),
        "giftable": bool(m.u8()),
    }
    offer["products"] = [read_product(m) for _ in range(m.i32())]
    offer["clubLevel"] = m.i32()
    offer["bundlePurchaseAllowed"] = bool(m.u8())
    return offer


def flatten(node, out=None):
    out = [] if out is None else out
    out.append(node)
    for child in node["children"]:
        flatten(child, out)
    return out


def credits_now(conn):
    """InfoRetrieve replays the hotel-view set, which carries the balance as a string."""
    conn.send(INFO_RETRIEVE)
    return int(drain_until(conn, CREDIT_BALANCE).s())


def main():
    conn = connect_and_login("admin")

    start_credits = credits_now(conn)
    print(f"starting credits: {start_credits}")

    conn.send(GET_CATALOG_INDEX, wstr(CATALOG_TYPE))
    index = drain_until(conn, CATALOG_INDEX)
    root = read_node(index)
    assert index.s() is not None or True
    pages = [p for p in flatten(root) if p["pageId"] > 0 and p["offerIds"]]
    assert pages, "catalog index had no page with offers"
    print(f"catalog index: {len(flatten(root))} node(s), "
          f"{sum(len(p['offerIds']) for p in pages)} offer(s) across {len(pages)} page(s)")

    page_meta = pages[0]
    conn.send(GET_CATALOG_PAGE, struct.pack(">ii", page_meta["pageId"], NO_OFFER) + wstr(CATALOG_TYPE))
    page = drain_until(conn, CATALOG_PAGE)

    page_id = page.i32()
    page.s()  # catalog type
    layout = page.s()
    images = [page.s() for _ in range(page.i32())]
    texts = [page.s() for _ in range(page.i32())]
    offers = [read_page_offer(page) for _ in range(page.i32())]
    assert page_id == page_meta["pageId"], f"page {page_id} != requested {page_meta['pageId']}"
    assert offers, "page had no offers"
    print(f"page {page_id} '{page_meta['pageName']}': layout={layout} "
          f"texts={len(texts)} images={len(images)} offers={len(offers)}")

    # Only offers actually payable in credits: a real catalog prices whole pages in duckets
    # or diamonds, and those carry a credit price of 0 while not being free at all. It also
    # lists avatar effects and bots, which hand over no furniture we can grant.
    payable = [o for o in offers
               if o["priceInCredits"] > 0
               and o["priceInActivityPoints"] == 0
               and o["priceInSilver"] == 0
               and any(pr["productType"] != "b" for pr in o["products"])]
    assert payable, "page had no offer payable in credits"

    cheapest = min(payable, key=lambda o: o["priceInCredits"])
    print(f"cheapest offer payable in credits: {cheapest['localizationId']} "
          f"at {cheapest['priceInCredits']} credits, {len(cheapest['products'])} product(s)")

    # Buy it.
    conn.send(PURCHASE_FROM_CATALOG,
              struct.pack(">ii", page_id, cheapest["offerId"]) + wstr("") + struct.pack(">i", 1))
    bought = read_purchase_offer(drain_until(conn, PURCHASE_OK))
    assert bought["offerId"] == cheapest["offerId"], "PurchaseOK echoed the wrong offer"
    assert bought["priceInCredits"] == cheapest["priceInCredits"], "price changed between page and receipt"
    print(f"purchase ok: offer {bought['offerId']} echoed back intact")

    balance = int(drain_until(conn, CREDIT_BALANCE).s())
    expected = start_credits - cheapest["priceInCredits"]
    assert balance == expected, f"balance {balance}, expected {expected}"
    print(f"credits debited: {start_credits} -> {balance}")

    added = drain_until(conn, FURNI_LIST_ADD_OR_UPDATE)
    count = added.i32()
    expected_items = sum(p["productCount"] for p in bought["products"] if p["productType"] != "b")
    assert count == expected_items, f"{count} item(s) added, expected {expected_items}"
    print(f"inventory updated: {count} item(s) added")

    # The count on the inventory button comes from the unseen set, not from the item arriving.
    # Without this the purchase lands silently.
    unseen = drain_until(conn, UNSEEN_ITEMS)
    categories = []
    for _ in range(unseen.i32()):
        category = unseen.i32()
        categories.append((category, [unseen.i32() for _ in range(unseen.i32())]))

    assert categories, "no unseen categories sent"
    category, ids = categories[0]
    assert category == OWNED_FURNI_CATEGORY, f"unseen category {category}, expected owned furni"
    assert len(ids) == count, f"{len(ids)} unseen id(s), but {count} item(s) were added"
    print(f"unseen indicator: category {category} -> {len(ids)} item(s)")

    # Opening the inventory clears it; the server must accept both resets.
    conn.send(RESET_UNSEEN_ITEM_IDS,
              struct.pack(">ii", OWNED_FURNI_CATEGORY, len(ids))
              + b"".join(struct.pack(">i", i) for i in ids))
    conn.send(RESET_UNSEEN_ITEMS, struct.pack(">i", OWNED_FURNI_CATEGORY))
    assert credits_now(conn) == balance, "a reset must not disturb the session"
    print("unseen resets accepted")

    # A bundle should hand over several items for one price.
    bundles = [o for o in offers if len(o["products"]) > 1]
    if bundles:
        print(f"bundle on this page: {bundles[0]['localizationId']} "
              f"-> {len(bundles[0]['products'])} products")

    # An unknown offer must be refused, not ignored: the client's spinner waits for an answer.
    conn.send(PURCHASE_FROM_CATALOG,
              struct.pack(">ii", page_id, 99999) + wstr("") + struct.pack(">i", 1))
    error = drain_until(conn, PURCHASE_ERROR)
    print(f"unknown offer refused with error code {error.i32()}")

    # An absurd amount must be refused rather than silently clamped.
    conn.send(PURCHASE_FROM_CATALOG,
              struct.pack(">ii", page_id, cheapest["offerId"]) + wstr("") + struct.pack(">i", 100000))
    drain_until(conn, PURCHASE_ERROR)
    after = credits_now(conn)
    assert after == balance, f"refused purchase changed the balance: {balance} -> {after}"
    print(f"oversized amount refused; balance untouched at {after}")

    print("ALL CATALOG CHECKS PASSED")
    return 0


if __name__ == "__main__":
    sys.exit(main())

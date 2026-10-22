# Dev tasks. `make` starts the database, then the hotel and the relay side by side; Ctrl+C stops both.

HOST  := src/Habbo.Host
RELAY := tools/Habbo.Relay
TOOLS := src/Habbo.Tools

.PHONY: run db db-down psql build host relay test gamedata catalog

run: db build
	@$(MAKE) --no-print-directory -j2 host relay

db:
	docker compose up -d

db-down:
	docker compose down

psql:
	docker compose exec postgres psql -U habbo -d habbo

# one after the other: both build Habbo.Domain, and two builds at once trip over its obj/
build:
	dotnet build $(HOST)
	dotnet build $(RELAY)

host:
	dotnet run --no-build --project $(HOST)

relay:
	dotnet run --no-build --project $(RELAY)

test:
	dotnet test Habbo.slnx

# Habbo's newest gamedata into the mirror, and its furnidata into the seed (imported on the hotel's next start)
gamedata:
	dotnet run --project $(TOOLS) -- mirror --only gamedata
	cp habbo-assets/www.habbo.com/gamedata/furnidata.xml seed/furnidata.xml

# Habbo's catalog into the seed (seed/catalog.json), through G-Earth: connect it to Habbo, then press the extension's play.
# Furnidata first, so the newest furni are in it
catalog:
	dotnet run --project tools/Habbo.CatalogDumper

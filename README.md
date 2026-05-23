# AuctionInstagramService

A .NET 10 / Aspire mockup of an Instagram-style auction app — built as an interview design exercise. Each piece is the smallest thing that demonstrates the architectural concept, not a production implementation.

## What it does

- Create auctions (title, description, starting price, status, dates)
- Upload multiple images per auction (stored in Azure Blob Storage / Azurite locally)
- Place bids — ACID-safe under concurrent bidders via SERIALIZABLE isolation + EF Core retrying execution strategy
- Stream new bids in real-time over Server-Sent Events (built on .NET 10's `TypedResults.ServerSentEvents` + `SseParser`)
- Mock cookie-based sign-in (any username works)

## Architecture

```
                            ┌─────────────────────────────────────────────────┐
                            │  AppHost (Aspire orchestrator)                  │
                            │                                                 │
[Browser/WASM]              │  ┌──────────┐    ┌────────────────┐             │
      │                     │  │ Postgres │    │ Azure Storage  │             │
      │                     │  │ (auctions│    │ (auction-      │             │
      │ /                   │  │  bids,   │    │  images blob)  │             │
      │ /auctions/...       │  │  images) │    └───────┬────────┘             │
      │ /api/...            │  └────┬─────┘            │                      │
      ▼                     │       │                  │                      │
[Web (BFF)]                 │   ┌───┴──────────────────┴────┐                 │
  - Blazor SSR + WASM       │   │      ApiService           │                 │
  - Cookie auth             │◄──┤  - CRUD auctions/images   │                 │
  - YARP reverse proxy      │   │  - Place bid (SERIALIZABLE│                 │
  - X-User-Id forwarding    │   │    + ExecuteUpdate)       │                 │
  - Login/logout pages      │   │  - Publish bid → Redis    │                 │
       │                    │   │  - EF Core migrations on  │                 │
       │ /api/auctions/...  │   │    startup                │                 │
       │ /api/images/...    │   └───────────────────────────┘                 │
       │ /api/auctions/{id}/bids/stream                                       │
       ▼                    │   ┌───────────────────────────┐    ┌─────────┐  │
[StreamingService]          │   │   StreamingService        │    │  Redis  │  │
  - Subscribes to Redis     │◄──┤  - Subscribes to          │◄───┤ pub/sub │  │
  - Streams bids as SSE     │   │    bids:{auctionId}       │    └─────────┘  │
  - Scales horizontally     │   │  - Yields each msg as SSE │                 │
                            │   └───────────────────────────┘                 │
                            └─────────────────────────────────────────────────┘
```

YARP routes (BFF):
- `/api/auctions/{auctionId}/bids/stream` → `streamingservice` (Order=1)
- `/api/{**catch-all}` → `apiservice` (Order=2)

Both routes require authentication and inject the `X-User-Id` header from the signed-in user's claims.

## Projects

| Project | What it does |
|---|---|
| `AppHost` | Aspire orchestrator — wires Postgres, Redis, Azure Storage, and the three .NET services |
| `ServiceDefaults` | Shared Aspire/OpenTelemetry defaults + auth schemes (`AddCookieAuth`, `AddMockAuth`) |
| `Contracts` | DTOs (`AuctionDto`, `BidDto`, `AuctionImageDto`), `UserContext`, `ICurrentUser`, error types (`AuctionNotFound`, `BidTooLow`, ...), `BidChannels.For(auctionId)` |
| `Database` | EF Core entities + `AuctionDbContext` + migrations + `AddAuctionDatabase` (registers Aspire-managed Npgsql DbContext with `NoTracking` default + retrying execution strategy) |
| `DataAccess` | `AuctionService`, `AuctionImageService`, `BidService` — use DbContext directly, return DTOs via `.Select()` projections (no `.Include`), bid placement is ACID via execution-strategy-wrapped SERIALIZABLE tx |
| `ApiService` | Minimal API endpoints for auctions / images / bids. Owns writes + non-streaming reads. Stamps bid identity from `X-User-Id` |
| `StreamingService` | SSE-only service. Subscribes to Redis `bids:{auctionId}` channels and forwards as SSE events. Separated from ApiService so connection-heavy traffic scales independently |
| `Web` | Blazor Web App acting as BFF. Cookie auth, login/logout pages, YARP gateway, NavMenu. Serves the WASM client. |
| `Web.Client` | Blazor WebAssembly. `Auctions.razor` and `AuctionDetail.razor` pages call the BFF via the typed clients |
| `Application.Client` | Typed HTTP clients (`AuctionsClient`, `AuctionImagesClient`, `BidsClient`) — shareable between any caller; SSE consumed with `System.Net.ServerSentEvents.SseParser` |

## Identity flow

1. User signs in at `/login` → BFF sets `auction.auth` cookie containing username claim
2. WASM page calls `api/auctions/...` → browser sends cookie → BFF authenticates → YARP request transform adds `X-User-Id` header → downstream service reads it via `DevAuthHandler`
3. Server-side `ICurrentUser.Get()` reads `ClaimTypes.NameIdentifier` from `HttpContext.User`
4. Swap mock auth for real OIDC/JWT: replace `AddCookieAuth()` in BFF and `AddMockAuth()` in downstream services — every other line stays put

## ACID bid placement

`BidService.PlaceAsync`:
1. Wrap whole operation in `DbContext.Database.CreateExecutionStrategy().ExecuteAsync(...)` so transient retries work
2. Open `SERIALIZABLE` transaction
3. Project a snapshot from `Auctions` (StartingPrice, Status, EndsAt, CurrentHighestBid) in a single query
4. Validate — return `AuctionNotFound` / `AuctionNotOpen` / `AuctionEnded` / `BidTooLow` via OneOf<>
5. Insert `Bid`, `ExecuteUpdate` the denormalized `CurrentHighestBid`, commit
6. After commit, publish `BidDto` JSON to `bids:{auctionId}` Redis channel (fire-once-on-success, kept outside the strategy)

If two clients race a bid at the same amount, Postgres aborts one with serialization failure (SQLSTATE 40001), the strategy retries it, and the retry sees the now-committed first bid and likely returns `BidTooLow` to the loser.

## Running it

```bash
dotnet run --project AuctionInstagramService.AppHost
```

The Aspire dashboard URL is printed on startup. Postgres, Redis, Azurite, pgAdmin, and RedisInsight all run as containers. The `Web` (BFF) externally exposes the only public endpoint; everything else is service-discovered internally.

First boot creates the Postgres schema via `MigrateAsync` on ApiService startup.

## What's intentionally not built

- Real authentication (OIDC, password store, etc.) — see "Identity flow" above for the swap point
- Image CDN — currently every image read flows through ApiService. At scale put Azure Front Door in front of the blob container (We might change this)
- Bid history pagination (Might add this later)
- Auction state machine transitions (Draft → Open → Closed) (Needs refactor) 
- Outbox pattern for Redis publish — if Redis is down right after commit, subscribers miss that bid

See `SCALING.md` for the reading list to fix each of these properly.

## Tech

.NET 10 · Aspire 13 · Blazor Web App (Server bff + WASM) · YARP  · Npgsql · StackExchange.Redis · Azure.Storage.Blobs 

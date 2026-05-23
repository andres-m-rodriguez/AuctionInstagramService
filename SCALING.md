# Scaling reading list

Talks to watch, grouped by the component of this system they sharpen your thinking on. No links on purpose — titles + speakers are stable; URLs rot. Search the title and speaker on YouTube / InfoQ / your conference of choice.

## Foundations (watch these first)

- **"Turning the database inside out with Apache Samza"** — Martin Kleppmann. The cleanest explanation of why event logs make systems composable. If you only watch one talk on this list, watch this.
- **"What Came First, the Truth or the Lie?"** — Pat Helland. State, lies, immutability. Foundational for why we keep an append-only `Bids` table and derive `CurrentHighestBid`.
- **"Don't Build A Distributed System"** — Sam Newman. The cost framing for everything below.
- **"Stop Rate Limiting! Capacity Management Done Right"** — John Allspaw. Useful for SSE fan-out and bid throughput discussions.

## Postgres at scale (relevant to the write path bottleneck)

- **"Postgres at any scale"** — Citus / Microsoft folks (Marco Slot, Ozgun Erdogan). How sharding actually looks for OLTP.
- **"Lock, stock and two smoking barrels"** — Bruce Momjian's locking talks. Read once before you write your second SERIALIZABLE transaction.
- **"How discord stores billions of messages"** — Discord engineering. Cassandra-flavored but the partitioning lessons port directly to a partitioned Postgres or DynamoDB-shaped bid history.
- **"What's New in PostgreSQL 16 / 17"** — annual talks, usually by Magnus Hagander or Bruce Momjian. Pay attention to logical replication + parallel query improvements.

## Redis + pub/sub + streaming

- **"Redis Streams: The Big Picture"** — Salvatore Sanfilippo (antirez). Pub/sub vs Streams; explains why Streams beat pub/sub once you need replay or consumer groups. Our SSE service would graduate to Streams under real load.
- **"Architecting for Massive Scale at LinkedIn"** — Jay Kreps. Kafka talk, but the "log-as-backbone" mental model applies directly to swapping our Redis pub/sub for Kafka if we want replay/multi-tenant.
- **"Real-time data processing with Redis"** — Itamar Haber or Brian Sam-Bodden. Practical patterns for high-fanout reads.

## SSE + long-lived connections

- **"Server-Sent Events: the unsung hero of real-time web"** — search recent talks at NDC / GOTO. SSE has fewer talks than WebSockets but the connection-management lessons overlap.
- **"How Slack scaled WebSockets to millions of users"** — Slack engineering. Same connection-economics problem we'd hit at 10M users on Streamingservice.
- **"Real-time at scale at Discord"** — Discord engineering. They run gateway services that look architecturally similar to our StreamingService.

## .NET / Aspire / minimal API specific

- **"David Fowler on building cloud-native apps with .NET Aspire"** — David Fowler. Pick the most recent one each year.
- **".NET Conf — Aspire deep dive"** — annual. The deployment / azd integration story matters more than the dev-loop story for production.
- **"YARP: building a reverse proxy in .NET"** — Sam Spencer / Chris Ross. Useful for the gateway-side scaling (sticky sessions, header transforms, destination resolvers).
- **"What's new in EF Core (each release)"** — Shay Rojansky / Arthur Vickers. NoTracking defaults, execution strategies, `ExecuteUpdate` — we're using all of these.

## Distributed transactions / consistency

- **Jepsen analyses (Kyle Kingsbury / aphyr)** — read the writeups, watch the conference talks. The Postgres analyses are the most relevant to us.
- **"Consistency models for distributed systems"** — Peter Bailis. Pre-reading before you argue with anyone about isolation levels.
- **"Saga pattern for microservices"** — Caitie McCaffrey. Once we have an Auction service + a Payment service we'll want this.

## Generalists worth subscribing to

- **InfoQ engineering tracks** — search by company (Discord, Stripe, Cloudflare, Shopify, Airbnb).
- **Marc Brooker's blog** (AWS principal engineer) — not videos but the same density of insight per minute. Also gives great talks (search his name).
- **High Scalability blog** — case-study format, every other post is a system worth thinking about.
- **Brendan Gregg's perf talks** — useful when you go from "is it scalable" to "what is the actual hot path."

## How to use this list

For each component you want to harden, pick *one* talk from the relevant section and one foundational talk. Take notes on the failure modes they describe — those are usually the next things to break in your system, in order.

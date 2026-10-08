# Shitcoin trader — crypto-intelligence system on FocusTemplate

*First outline: 2026-09-01. Status: design sketch, nothing built.*

An automated crypto-intelligence system: detect new ERC-20 deployments on Ethereum/Base in
real time, analyze deployer/funding graphs, attribute wallets to known teams/devs/projects,
combine on-chain signals with external sources (X, GitHub, DEX data, optionally Discord), and
score candidates into **early-warning research alerts with traceable evidence**.

**Scope decision:** the output is research evidence, not trade execution. No order routing, no
automated buying, no investment advice — alerts carry claims + evidence references, a human
decides. This keeps the system on the analytics side of the regulatory line and matches the
evidence-first architecture below.

**Division of labor (fixed):** detection and scoring are deterministic/statistical; the LLM
(Claude) only does entity resolution, narrative extraction, classification, and evidence
summarization — asynchronously, never in the hot path, never directly writing scores.

## Mapping onto the template

The two audience verticals get real jobs, the new work is a third processing layer:

| Layer | Role |
|---|---|
| `src/intel/` **(new)** | Headless pipeline: ingestion, collectors, enrichment, scoring. No UI, no exposure. |
| Admin vertical | Analyst dashboard (Blazor WASM behind BFF): alert review, evidence drill-down, attribution graph, watchlists. |
| Public vertical | Alert delivery: `Public.Api` serves alert feeds, the MAUI app is the early-warning client on the phone. |
| `FocusTemplate.Data` | Stays the single domain: one model, one Postgres, one `Migrations/` set. |

New projects (all wired as Aspire resources via `AddProject`):

- **`Intel.Ingest`** — `BackgroundService` chain listeners, one resource instance per chain
  (Ethereum, Base; config-driven, same code). Nethereum.
- **`Intel.Collectors`** — X/Twitter, GitHub, DEX (DexScreener/GeckoTerminal or on-chain pool
  events), Discord (optional). One module per source, each behind a feature flag
  (`Features:Collectors:X` etc., **default off** — the Mobile-flag pattern: plain `aspire start`
  needs no API keys; keys via user secrets/Aspire parameters).
- **`Intel.Enrichment`** — LLM job worker (Claude API), consumes an enrichment queue.
- **`Intel.Scoring`** — deterministic scoring library + recompute worker.

## Domain model (rides the §2 DDD wave)

Aggregates in `FocusTemplate.Data`, feature-foldered (§2.1: partial DbContext +
`IEntityTypeConfiguration<T>`, explicit registration):

- `TokenDeployment` — chain, block, tx, deployer, factory?, CREATE/CREATE2 (+ salt).
- `ContractArtifact` — normalized bytecode hash (metadata/swarm hash stripped) + SimHash
  fingerprint for similarity queries (Hamming distance in SQL).
- `WalletEntity` + `AttributionEdge` — the deployer/funding graph as plain tables; recursive
  CTEs carry this far. Graph DB/Apache AGE only if it demonstrably breaks down.
- `LiquidityEvent`, `SocialSignal`, `ScoreSnapshot`, `EvidenceItem`.
- **Evidence is first-class:** every sub-score and every LLM claim references `EvidenceItem`s
  (tx hashes, tweet ids, repo URLs). "Nachvollziehbare Evidenz" is a schema property, not prose.
- §2 features land here for real: Vogen IDs (2.3), Postgres enums (2.5: `chain`,
  `signal_source`, `alert_status`), complex types (2.4: e.g. `BytecodeFingerprint`).

## Pipeline (this is §2.6, for real)

Event chain: deployment detected → graph enriched → signals collected → score computed →
alert raised. Domain events on the aggregates, dispatched post-commit — and the **outbox
upgrade is required early** here, not a nice-to-have: a lost event is a missed alert. This
system is the first real consumer of the §2.6 mediator decision.

Ingest specifics:

- `eth_subscribe` (WebSocket) on newHeads/logs per chain via an RPC provider (❓ which one).
- Plain CREATE via `receipt.contractAddress`; factory/CREATE2 via known factory events +
  `trace_filter`/debug traces.
- **Reorg handling:** facts become final after N confirmations (block-hash chain checked);
  a backfill job closes gaps after downtime.
- Funding graph: walk the deployer EOA backwards with bounded depth; seed labels from known
  CEX hot wallets; bytecode similarity links deployments to prior artifacts/teams.

## Scoring — deterministic, versioned, evidence-backed

One pure service per dimension → `SubScore` with evidence refs → weighted aggregate:

Provenance/entity attribution, contract risk, liquidity & holder quality, social momentum,
developer activity, cross-source confirmation.

- `ScoreSnapshot` carries the **scoring version** — alerts stay auditable when weights change.
- Recompute is event-driven (new signal → affected scores recompute).
- This is the repo's first pure logic → the **first unit-test project** (per dev-loop rule).

## LLM layer (Claude, official C# SDK)

- Async enrichment jobs only. Model split: `claude-opus-5` for hard entity resolution,
  `claude-haiku-4-5` for bulk classification. **Structured outputs** (`output_config.format`):
  the LLM returns typed claims (`{entity, confidence, evidenceRefs[]}`) — validated
  deterministically, never free text into the score.
- Cost levers from day 1: **Batch API** (−50% for everything non-latency-critical), **prompt
  caching** (stable system prompt + label catalog first), token spend as an OTel metric.
- **Hard rule — prompt injection:** tweets, READMEs, and token metadata are
  **attacker-controlled** (a deployer can write content addressed at our LLM). Social content
  is always framed as data; LLM claims are one dimension among several and require
  deterministic cross-source confirmation before they move a score.
- **Dev-time access:** prompts/schemas are developed via fixtures + Claude Code (subscription-
  covered); optionally an MCP server over the intel DB for interactive analysis in Claude
  Desktop. The production worker needs Console API credits — there is no legitimate way to
  tunnel a claude.ai subscription into headless API calls.

## Testing (template pyramid)

- **Unit** (new project): scoring, bytecode normalization/SimHash, graph walk.
- **Integration** (Testcontainers): Postgres (exists) + **Anvil (Foundry) container** — deploy
  a real ERC-20 in the test, assert detection end-to-end against a real chain. Collectors
  against WireMock. Same one-container/db-per-class/Respawn contract as the Admin suite.
- **E2E**: Playwright for the analyst dashboard; Appium for the mobile alert list
  (`AutomationId`s decided up front, as usual).
- **LLM**: recorded fixtures for integration; a small offline eval set for entity resolution
  (precision on a hand-labeled sample) before any prompt change lands.

## Observability & ops

- The money shot: one distributed trace block → detection → enrichment → alert in the Aspire
  dashboard.
- Custom metrics (§1.4): `intel.ingest.lag_blocks`, `intel.deployments.detected`,
  `intel.alerts.raised`, LLM token spend. Health check = RPC subscription lag.
- FusionCache (§3.1) for RPC/DEX responses; resilience via ServiceDefaults (rate limits!).

## Backlog interactions (prerequisites)

- **§2.6 domain events + outbox** — the pipeline backbone; forces the mediator decision.
- **§3.2 auth (Keycloak)** — mandatory before any exposure; this data is valuable.
- **§1.2 ProblemDetails**, **§4.4 pagination** (alert feeds), **§3.1 FusionCache**.

## Walking skeleton (sequencing)

1. `Intel.Ingest` (Ethereum only) → `TokenDeployment` rows + a dashboard list. **First red
   test:** Anvil testcontainer — deploy an ERC-20, assert the `TokenDeployment` row. This
   replaces Weather as the demo domain over time.
2. Funding-graph walk + attribution store.
3. Scoring v1 (2–3 deterministic dimensions) + alerts through `Public.Api` into the MAUI app.
4. Collectors: DEX first (on-chain, no API-key zoo), then GitHub, then X.
5. LLM enrichment (entity resolution + narratives) with the eval set.
6. Auth (§3.2), then any external access.

## Open decisions ❓

- RPC provider (Alchemy/QuickNode/…) vs. self-hosted node; trace API availability matters for
  CREATE2/factory detection.
- Queue tech: start with Postgres outbox + in-proc channels; Redis/Kafka only on demonstrated
  need (matches the §2.6/§3.1 Redis trajectory).
- X/Twitter API tier (pricing!) and whether Discord is worth the ToS/complexity at all.
- Alert push channel for mobile (poll vs. push notifications) — poll first.
- Where attribution label data comes from (public label sets, manual curation in the admin UI).

## Risks

- RPC/WebSocket reliability → reorg + backfill logic is core, not an edge case.
- Social APIs are expensive and rate-limited; collectors must degrade gracefully (flags off =
  system still works).
- LLM claims look authoritative — without the deterministic cross-source gate the scoring
  quietly becomes an LLM opinion. The gate is architecture, not a prompt.
- Adversarial environment: expect deliberate spoofing (fake teams, wash trading, bought
  followers) — every dimension needs a "can this be cheaply faked?" note in its design.

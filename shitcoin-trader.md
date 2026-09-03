# Shitcoin trader — crypto-intelligence system on FocusTemplate

*First outline: 2026-09-01. Revised: 2026-09-02. Status: design sketch, nothing built.*

*2026-09-03: the codebase was renamed — the product is **Argus** (`Argus.slnx`). The former Admin
vertical is now `Argus.Api`/`Argus.Web`/`Argus.Web.Bff`/`Argus.Shared`, the intel worker is
`Argus.Worker` (flat `src/`, no vertical folders), and the Public/MAUI vertical was removed
entirely. `Intel.Worker`/`src/intel/` mentions below refer to today's `Argus.Worker`; planned
modules (`Intel.Enrichment`, `Intel.Execution`, the Intel MCP server) will land as `Argus.*`
projects. Config vocabulary is unchanged: `Features:Intel`, `Intel:Ingest:*`.*

A **personal, single-user** crypto-intelligence system. Two primary use cases:

1. **Entity Research** — search *anything*: wallet address, contract, token, person, fund
   (e.g. Ribbit Capital), X account, GitHub org/repo, Discord server, domain, project. Connect
   the dots: who is behind it, what is it, categorize it (legit / shady / unknown / …), show
   relationships — linked people, portfolio, prior projects, funding paths.
2. **Early Discovery & Notification** — the investable use case: a new contract/token is
   deployed, a new crypto-related repo appears from a known dev, a tweet announces a new
   system → an alert that **already carries the dots** (who is behind it, funding path, track
   record, risk signals), early enough to act on.

**Capabilities** (all views over one entity graph):

- **Early Discovery** — detect new projects early via deployments, wallet funding, GitHub
  activity, and social signals.
- **Entity Research** — search arbitrary wallets, people, funds, X accounts, projects, or
  domains and make relationships visible.
- **Wallet Attribution** — figure out who is probably behind a wallet or a deploy.
- **Investment Due Diligence** — assess team, history, contract risk, holder structure,
  funding, and social momentum together.
- **Smart-Money Tracking** — identify wallets/people that repeatedly show up early in good
  projects.
- **Watchlists & Alerts** — watch entities; get notified on new deployments, funding events,
  or unusual activity.

**Scope decision (revised):** v1 output is research evidence and alerts — no order routing, no
automated buying. **Trading is a future phase, not a never** — see *Future: trading module*.
Until then a human decides; alerts carry claims + evidence references.

**Division of labor (fixed):** detection and scoring are deterministic/statistical; the LLM
(Claude) only does entity resolution, narrative extraction, classification, and evidence
summarization — asynchronously, never in the hot path, never directly writing scores.

## Architecture — simplified for single-user

The Public vertical and the MAUI app are **out of scope** (parked, not deleted — the template
keeps them for its template job). No third-party exposure, no public API surface.

| Piece | Role |
|---|---|
| `src/intel/` **(new)** | Headless pipeline: ingestion, collectors, enrichment, scoring. |
| Admin vertical (as-is) | The one UI: universal search, entity page + relationship graph, alert review, watchlists, manual curation. Blazor WASM behind the BFF — already built, zero new plumbing. **Decided.** |
| Notifications | **No mobile app.** **Web Push API** (decided): the dashboard becomes a PWA (manifest + service worker), installed to the iPhone Home Screen; the BFF sends standard Web Push (VAPID) — works on iOS since 16.4, **Home-Screen install is mandatory**, no silent/data-only pushes. Notification deep-links to the alert/entity page. Fallback if iOS delivery proves unreliable: ntfy or a Telegram bot — the alert-dispatch side stays channel-agnostic. |
| `FocusTemplate.Data` | Stays the single domain: one model, one Postgres, one `Migrations/` set. |

Intel projects, simplified from the v1 sketch: start with **one** worker project,
**`Intel.Worker`**, hosting ingestion/collectors/enrichment/scoring as `BackgroundService`
modules behind feature flags (`Features:Ingest:<chain>`, `Features:Collectors:GitHub`, … —
default off, the Mobile-flag pattern: plain `aspire start` needs no API keys). Split into
separate Aspire resources only when a module demonstrably needs independent scaling or
isolation (chain listeners are the first candidates).

## Prior art & buy-vs-build (2026-09 survey)

This space is a real industry — the point of building is **fusion + ownership**, not
reinventing data feeds. Collectors are **API-first**: call an existing service before
building one.

| Capability | Who does it today | Verdict |
|---|---|---|
| Entity research / attribution | **Arkham** (free UI, AI entity clustering; enterprise API gated, premium intel via ARKM token), **Nansen** (~$49–150+/mo, 500M+ labeled addresses, API), MetaSleuth/BlockSec | Use the UIs during manual research; APIs too gated/pricey as a backbone. **Build the graph**, seed labels from open sets (Dune label tables, eth-labels dumps, GoPlus AML) — with provenance, since imported labels can be wrong. |
| Contract risk | **GoPlus Security** — free APIs: token_security (30+ checks incl. honeypot, mint, tax), address_security/AML; ships its own **MCP server** (fits the Claude Desktop layer). Also RugCheck, TokenSniffer. | **Buy (call GoPlus).** No v1 in-house risk analysis; our SimHash stays for *attribution* (linking deployers via bytecode), which GoPlus doesn't do. |
| New pools / market data / trajectories | DexScreener, GeckoTerminal, Birdeye APIs (free tiers) | **Buy** for price/liquidity trajectories + screening. **Keep the own chain listener** — the APIs show pools, not the deployer/funding link, and our alert latency + evidence chain depends on seeing the deployment itself. |
| Smart-money lists | Nansen Smart Money (enterprise), GMGN, **Cielo** (wallet tracking + alerts), WalletFinder | Their lists are their moat and rented. **Build own** from HyperSync outcome data (that's the §Scoring smart-money dimension); optionally cross-check against GMGN/Cielo free tiers. |
| Watchlists / wallet alerts | Cielo, GMGN Telegram bots | Off-the-shelf covers *per-wallet* alerts. Ours differ: rule = graph neighborhood (N funding-hops) + fused evidence → **build**, it's thin on top of the graph anyway. |
| Social signal | TweetScout (account credibility), LunarCrush | Optional buy, later, when the X collector lands. |

**How the degen/influencer scene actually works** (for calibration): a rented stack of
DexScreener/Birdeye discovery + GoPlus/RugCheck safety checks + GMGN/Cielo smart-money
alerts + Telegram execution bots (Photon, BullX, Banana Gun, Trojan), glued together by
Telegram notifications and private alpha groups. Nobody in that stack fuses deployer
graphs + GitHub + social with owned, evidence-backed scoring — that fusion (and the fact
that many influencer "calls" are paid promos our attribution layer would flag) is exactly
this system's niche. Also: that scene is Solana-first; our EVM+Robinhood focus is
underserved by it.

### Wallet matching — how it's actually done

Arkham (ML clustering), Chainalysis/TRM (enterprise compliance) run this at scale; the
underlying heuristics are public knowledge and map 1:1 onto our proposal/evidence model
(heuristic proposes an edge with confidence + evidence → review queue):

- **Same EOA address on multiple EVM chains** = same key. Deterministic, free — instant
  cross-chain identity (Ethereum ↔ Base ↔ Robinhood Chain).
- **First-funder**: who sent the first gas to a fresh wallet; recursive → funding trees.
- **CEX deposit-address clustering**: two wallets sweeping to the same exchange deposit
  address ⇒ same exchange account. The classic strong signal; needs CEX hot-wallet labels.
- **Bytecode/CREATE2 reuse**: SimHash-similar contracts or reused salts/factories link
  deployers across projects (ours already).
- **Off-chain anchors**: ENS, addresses in X bios/GitHub commits/donation links — collector
  + LLM extraction territory, always evidence-referenced.
- **Behavioral fingerprints** (timing, gas habits, nonce patterns): weak, supporting-only —
  never sufficient for a `same_owner` edge on its own.

## Chains — config-driven EVM registry

One EVM ingestion codebase (Nethereum), one listener instance per chain, chains defined in
config: RPC endpoints, confirmation depth, known factories, trace API flavor. Launch set:

- **Ethereum** — the base case.
- **Base** — degen/memecoin volume.
- **Robinhood Chain** — live since 2026-07-01: Arbitrum Orbit L2, chain ID 4663, ETH gas,
  blob DA, **permissionless**. Same pipeline, same collectors, same scoring as Base — the only
  special case is the official tokenized-stock/RWA issuance: Robinhood's deployer wallets are
  a **known-issuer entity**, and deployments attributed to it route to a separate alert type
  ("new listing/tokenization") and skip degen risk scoring (attribution/rug-risk questions are
  meaningless for an NVDA stock token). One label, one routing rule — not a separate pipeline.
  Nitro-style tracing, same as Arbitrum One.
- **Arbitrum One** — near-free to add once Robinhood Chain works (same Nitro tracing).

`chain` becomes a **reference table**, not a Postgres enum — adding a chain must be a config
entry + row, never a migration. (§2.5 enums stay for genuinely closed sets: `alert_status`,
`signal_source`.)

**Solana: out for now** (decided 2026-09-02). Most memecoin action lives there (pump.fun),
but it is a second ingestion stack — different RPC model, different token/program model, no
EVM tracing. Revisit once the EVM pipeline is proven; the entity graph is chain-agnostic, so
Solana later means a new ingest module, not a schema change.

## History & node strategy

What each capability actually needs — three different data classes, often conflated:

| Data class | Needs | Used by |
|---|---|---|
| Live tip (heads, logs, receipts, new CREATEs) | **Full node** / standard RPC + WebSocket | Early Discovery, watchlist alerts |
| Historical **event logs** (`eth_getLogs`: ERC-20 transfers, pool swaps/mints) | Full node (receipts are kept; providers rate-limit hard) | Funding walks over *token* transfers, DEX price/liquidity history |
| Historical **state** (`balanceOf`/`eth_call`/storage at old blocks) | **Archive node** | Holder structure at launch, smart-money entry sizes, pool reserves at time T, proxy impl history |
| Historical **traces** (`trace_filter`, internal txs, CREATE/CREATE2 backfill) | **Archive + trace API** | Funding walks over *plain ETH* transfers (no logs exist for those!), "all contracts this deployer ever created" |
| **Address → transaction list** | **Indexer** — this is not an RPC method at all | Cold-entity research, wallet history pages |

Key trap: plain ETH transfers emit no logs — the funding graph *requires* traces (internal
txs) plus an indexer (normal txs by address). Archive alone doesn't give address history.

**Self-hosting reality check (one node ≠ three chains — one instance per chain, and three
different stacks):**

- **Ethereum** — Reth archive: ~3 TB, very manageable.
- **Base** — op-reth (Reth's OP-stack build + op-node): archive snapshot ~**14 TB**, growing
  ~3.5 TB per 6 months as the gas limit ramps. This is the deal-breaker for hobbyist
  self-hosting.
- **Robinhood Chain** — **Nitro** (geth fork), not Reth; Reth-for-Arbitrum
  (`arbitrum-reth`) is experimental, Sepolia-only. Chain launched 2026-07 → full history is
  tiny; running a Nitro archive node is cheap *if* the chain config/snapshots are published.

**Decision: no self-hosted nodes in v1.** Paid RPC with archive + trace access
(Alchemy/QuickNode/dRPC) plus indexer APIs (Etherscan V2 multi-chain, Alchemy Transfers) for
address history. A self-hosted Reth (ETH) is a sensible later cost optimization; Base archive
almost certainly stays rented.

Backfill scope ❓: forward-only ingestion from day 1 is cheap; smart-money labeling and
cold-entity research pull toward historical backfill — decide depth per use case (e.g. logs
backfill 12–24 months for outcome labeling, trace backfill only on-demand per research job,
never bulk).

**Backfill cost estimate (2026-09, 24 months ETH+Base, logs-only):** the workload is
(a) factory/pool-creation logs — a few thousand range queries, negligible; (b) early-window
swap/transfer logs + sampled price/liquidity trajectories for the ~100–300k pools that ever
crossed a liquidity threshold (the >90% filter is what keeps this sane; Base's raw token
count is in the millions). Via general RPC (Alchemy-class, ~$0.40–0.45/M CU) that lands
around **$250–500 one-time and days of rate-limited crawling**; via a bulk-history service
(**Envio HyperSync** — purpose-built log/trace streaming for ETH/Base/Arbitrum, ~2000× faster
than RPC, free dev tier + usage-based) the same pull is **≈$0–50 and hours**. Local footprint:
~20–100 GB raw parquet, a few GB of feature tables in Postgres. Robinhood Chain: ~2 months of
history, rounding error. **The anti-pattern is bulk trace backfill** (4-figure territory on
Base) — traces stay on-demand per research job. Plan: HyperSync for backfill, standard RPC
provider for tip-following + on-demand traces.

## Domain model — the entity graph is the center

Revised from v1: `TokenDeployment` is no longer the hub — **`Entity` + `Edge` are**. Chain
events, social posts, and repo activity are *signals* that create or strengthen nodes and
edges. Aggregates in `FocusTemplate.Data`, feature-foldered (§2.1).

- **`Entity`** — kind: `wallet | contract | token | person | org (fund/company) | x_account |
  github_repo | github_org | discord_server | domain | project`. One table + kind-specific
  detail tables where warranted.
- **`Edge`** — typed (`funded`, `deployed`, `member_of`, `invested_in`, `same_owner`,
  `mentions`, …), **confidence-scored and evidence-backed**. Attribution is an edge with a
  confidence, never a destructive merge — merges must be reversible when evidence changes.
- **`Tag`** (revised from v1's `Categorization`) — "legit vs. shady" is not stored, it is the
  *output of the risk score*. What we store are **objective, detector-backed tags**
  (multi-tag per entity, each with evidence refs + provenance `rule | label_set | llm |
  manual`), because each one has a concrete way to determine it:
  - *Deterministic on-chain*: `cex_hot_wallet`, `bridge`, `mixer_funded` (Tornado & co.),
    `mev_bot` (tx patterns), `fresh_wallet`, `serial_deployer`, `rug_history` (a prior token
    of this deployer had >90% LP pulled / honeypot bytecode), `contract_type:*`
    (token/pool/proxy/multisig), `known_issuer` (e.g. Robinhood's deployers).
  - *Label sets / manual curation*: `vc_fund`, `known_person`, `project_official`.
  - *LLM-proposed, needs deterministic confirmation*: `claims_team:<x>`, narrative tags.
- **Signal/fact tables** (feed the graph): `TokenDeployment` (chain, block, tx, deployer,
  factory?, CREATE/CREATE2 + salt), `ContractArtifact` (normalized bytecode hash + SimHash
  fingerprint, Hamming distance in SQL), `FundingTransfer`, `LiquidityEvent`, `SocialSignal`,
  `RepoEvent`, `ScoreSnapshot`, `EvidenceItem`.
- **Evidence stays first-class:** every edge, tag, sub-score, and LLM claim
  references `EvidenceItem`s (tx hashes, tweet ids, repo URLs). Traceable evidence is a schema
  property, not prose.
- **`Watchlist` + `AlertRule`** — watch any entity; rule types: new deployment by a watched
  entity (or anything within N funding-hops of it), funding event, unusual activity, new repo,
  new social signal.
- §2 features land here for real: Vogen IDs (2.3), complex types (2.4, e.g.
  `BytecodeFingerprint`), Postgres enums for closed sets (2.5).
- Graph storage: plain tables + recursive CTEs; Apache AGE only if that demonstrably breaks
  down.

## Pipeline (§2.6, for real)

Event chain: signal detected → graph enriched → score recomputed → alert rule matched → push
notification. Domain events on the aggregates, dispatched post-commit — the **outbox upgrade
is required early**: a lost event is a missed alert. First real consumer of the §2.6 mediator
decision.

Ingest specifics (unchanged from v1):

- `eth_subscribe` (WebSocket) on newHeads/logs per chain via an RPC provider (❓ which, per
  chain — Robinhood Chain endpoint availability is newer/thinner than Ethereum's).
- Plain CREATE via `receipt.contractAddress`; factory/CREATE2 via known factory events +
  traces.
- **Reorg handling:** facts final after N confirmations (block-hash chain checked); backfill
  job closes gaps after downtime.
- Funding graph: walk the deployer EOA backwards with bounded depth; seed labels from known
  CEX hot wallets; bytecode similarity links deployments to prior artifacts/teams.

**Universal search** (the research use case): one search endpoint over the entity graph.
Input is classified first (address / ENS / tx hash / handle / domain / free text) → exact
matches resolve directly into the graph; free text ("Ribbit Capital") goes through
LLM-assisted entity resolution against the graph + collector lookup APIs. A cold entity
triggers a **research job**: collectors fan out on demand, the graph populates, the entity
page fills in as results land — research is a write path, not just a query.

Collectors (feature-flagged per source, default off): **GitHub** (new crypto-related repos by
known/watched devs — cheap API, high signal), **DEX** (DexScreener/GeckoTerminal or on-chain
pool events), **X/Twitter**, **Discord** (optional, ToS ❓).

### Case study — Karma on Robinhood Chain (2026-09-02, manual dry run)

A live launch-and-dump ("Reddit Founder Cat"/Karma, Pons launchpad, paired against RDDT) was
dissected by hand in ~an hour; it validates the pipeline and calibrates what the alert can
contain at which moment:

| At | Automatically derivable | Source |
|---|---|---|
| T+0s | Token metadata, launchpad attribution, **insider whitelist** (`snipeTaxExemptions` decoded from calldata → `Edge` per address), creator's own launch buy % | launch tx alone |
| T+~10s | Deployer freshness (wallet age, tx count), first-funder walk (funded minutes earlier from a CEX-pattern hot wallet), **sniper cluster history** (same five wallets sniped 4 prior Pons launches) | own chain index |
| T+~60s | Live updates: insiders sold X% within Ns, creator exited, fee recipient moved | streaming swaps/events |
| T+minutes | Social/web footprint (or absence), contract-risk API verdicts, LLM narrative | collectors, async |

Design consequences (all deterministic, none need an LLM):

- **Launchpad adapters are a first-class ingest concept.** The token was created *by* the
  launchpad contract (internal CREATE inside `launchAndBuy`) — plain
  `receipt.contractAddress` detection misses it entirely. On launchpad-dominated chains the
  factory-event/calldata path is the *main* detection path, not the refinement. Per-launchpad
  adapter = factory address + event signatures + calldata decoding (whitelists, fee flags).
- **Alerts are living objects for their first minutes**: initial alert at T+~10s with priors
  (fresh deployer, insider whitelist %), then event-driven updates ("insiders exited") — the
  §recompute path, surfaced to the notification channel as an update, not a new alert.
- **New deterministic tags**: `serial_sniper` (same wallet early in ≥N launches),
  `insider_whitelisted`, `launchpad:<name>`, `fee_recipient_moved`.
- **Young chains get a full local index**: Robinhood Chain is two months old — ingesting its
  *entire* history is trivial and removes the indexer dependency there completely (wallet
  age/history queries answered from our own Postgres).

### Field study — ten launches traced (2026-09-03, manual)

All four Pons `launchAndBuy` tokens traced (Karma, KITSU, GOLDINU, BIGLY) ended −46% to −100%;
all six runners (STRATTON, ASS, GMERALD, DINO, BLOKKS, AIAIAI) were plain Uniswap v4 pools quoted
in genuine Robinhood stock tokens. Findings, each mapping to a deterministic signal:

1. **Venue predicts outcome.** Pons' guaranteed creator first-buy plus snipe-tax whitelist selects
   for extractors — `launchpad:pons` is itself a strong negative prior.
2. **Creator sells in the first minute ⇒ dump, every time.** Karma T+11s/20s, KITSU T+7–11s,
   GOLDINU T+34s — 100% of the creator bag each time, before any human buyer arrives.
3. **Whitelisted wallets are the creator's own cluster.** Every `snipeTaxExemptions` entry bought
   in the first second and dumped; the wallets shared a funder (Relay solver, or a top-up seconds
   before launch).
4. **Bot-template metadata ⇒ no sponsor.** "Created with Beast" descriptions, empty social links,
   same name launched twice in 11 s → zero organic trades.
5. **A clean launch can still be a trap via its quote asset.** JINQIAN: no creator bag, LP burned,
   10k holders — but priced in a self-issued "FAMI" controlled by the same team. The quote asset
   (`pairToken`) must itself be classified (official Robinhood stock token / canonical / self-issued).
6. **Post-launch churn signals.** Volume/liquidity > 20× per day = bot churn, not accumulation;
   entries after a >10× six-hour move ended like RETA/FAP.

**Derived detector rules** (v1 of the alert/score semantics; venue + calldata rules are pure
ingest-time checks, the rest need the streaming/entity layers):

- **Reject at T+0** on: Pons hook + non-empty insider whitelist, template description/empty
  socials, serial launcher, or a quote asset that is not an official Robinhood stock token or
  canonical quote. *(Storage semantics confirmed against live launches 2026-09-03:
  `PairTokenAddress` = zero address means natively quoted and is canonical - only an
  unrecognized **token** quote is a flag; null means no launch calldata was decoded. One launch
  writes several `TokenDeployment` rows sharing a transaction hash - token plus curve - so an
  alert must fire on the ERC-20 row, i.e. the one with a symbol, or it notifies twice. The curve
  row is deliberately kept: its address is what the 120 s watcher and churn rules monitor.)*
- **Watch 120 s, kill on**: any creator or whitelisted-wallet sell, or a shared-funding cluster
  among the whitelist (first-funder walk). Waiting costs nothing — the runners moved over hours,
  not seconds.
- **Enter only if**: the creator still holds, ≥20 distinct EOA buyers with none above 2%, and the
  pool is not Pons-hooked.
- New deterministic tags this implies: `template_metadata`, `self_issued_quote`, plus the earlier
  `serial_sniper`, `insider_whitelisted`, `launchpad:<name>`, `fee_recipient_moved`.

## Scoring — deterministic, versioned, evidence-backed

One pure service per dimension → `SubScore` with evidence refs → weighted aggregate:
provenance/attribution, contract risk, liquidity & holder quality, social momentum, developer
activity, cross-source confirmation, **smart-money presence** (new: watched/known-good wallets
appearing early).

- `ScoreSnapshot` carries the scoring version — alerts stay auditable when weights change.
- Recompute is event-driven.
- Smart-money tracking needs **outcome data** (post-launch price/liquidity trajectories from
  DEX sources) to label "good projects" retrospectively — this is the main pull toward
  historical backfill (❓ below).
- First pure logic in the repo → the **first unit-test project** (per dev-loop rule).

## LLM layer (Claude, official C# SDK)

Unchanged from v1:

- Async enrichment jobs only. `claude-opus-5` for hard entity resolution, `claude-haiku-4-5`
  for bulk classification. Structured outputs — typed claims
  (`{entity, confidence, evidenceRefs[]}`), validated deterministically, never free text into
  a score.
- Cost levers day 1: Batch API (−50%), prompt caching (stable system prompt + label catalog),
  token spend as an OTel metric.
- **Hard rule — prompt injection:** tweets, READMEs, token metadata are attacker-controlled.
  Social content is always framed as data; LLM claims are one dimension among several and need
  deterministic cross-source confirmation before they move a score. The gate is architecture,
  not a prompt.
- Dev-time via fixtures + Claude Code; production worker needs Console API credits.

### Interactive layer: Intel MCP server (Claude Desktop / mobile as a front-end)

Second LLM entry point besides the batch worker: an **MCP server over the intel domain**,
used from Claude Desktop (and, as a remote connector, from the Claude app on the phone).
Subscription-covered — no API credits for interactive work.

- **Tools are domain-shaped, never raw SQL**: `search_entities`, `get_entity` (+ graph
  neighborhood), `list_alerts`, `start_research_job`, `propose_edge`, `propose_tag`,
  `attach_evidence`.
- **Read side** — conversational research front-end: "who is behind 0xabc…?", "summarize the
  funding path of this deployer", alert triage from the phone.
- **Write side** — gap-filling and categorization, but **only through the same typed-claim
  contract as `Intel.Enrichment`**: `{claim, confidence, evidenceRefs[]}` with provenance
  `assistant`. No direct tag/edge/score writes, no silent merges. **Review queue (decided):**
  proposals land as `pending` — invisible to graph walks, scoring, and alert rules until
  accepted with one click in the dashboard; only acceptance raises the domain events. Wrong-
  but-pending costs a glance; wrong-and-live can cost a bad trade. Possible later relaxation,
  tiered: auto-apply low-stakes claims (descriptive tags, `mentions`), while attribution-class
  claims (`same_owner`, `invested_in`, anything feeding provenance scoring) always stay behind
  the click.
- **Trust is unchanged by the origin**: what Claude Desktop reads on the web mid-session
  (token sites, X, READMEs) is exactly as attacker-controlled as collector input — the
  cross-source gate applies to `assistant` claims too.
- **Exposure**: stdio/localhost first; remote access for the phone only behind OAuth — this
  is the scenario that pulls §3.2 auth forward.
- Nice side use: hand-labeling the entity-resolution eval set conversationally.

## Testing (template pyramid, minus mobile)

- **Unit** (new project): scoring, bytecode normalization/SimHash, graph walk, input
  classification for universal search.
- **Integration** (Testcontainers): Postgres + **Anvil (Foundry) container** — deploy a real
  ERC-20 in the test, assert detection end-to-end. Collectors against WireMock. Same
  one-container/db-per-class/Respawn contract as the Admin suite.
- **E2E**: Playwright for the dashboard (search → entity page → watchlist → alert). No Appium
  — no mobile client anymore.
- **LLM**: recorded fixtures for integration; a small offline eval set for entity resolution
  (precision on a hand-labeled sample) before any prompt change lands.

## Observability & ops

- The money shot: one distributed trace — chain event → detection → enrichment → alert → push.
- Custom metrics (§1.4): `intel.ingest.lag_blocks` (per chain), `intel.deployments.detected`,
  `intel.alerts.raised`, LLM token spend. Health check = RPC subscription lag per chain.
- FusionCache (§3.1) for RPC/DEX responses; resilience via ServiceDefaults (rate limits!).

## Future: trading module

Not designed now, but the seam is fixed: `Alert` carries **machine-readable action context**
(chain id, token address, pool address, liquidity depth at alert time), so a later
`Intel.Execution` module can subscribe to the same alert stream behind its own feature flag.
Key management, custody, risk limits, and the regulatory picture are deliberately unspecified
until that phase opens.

## Backlog interactions (prerequisites)

- **§2.6 domain events + outbox** — the pipeline backbone; forces the mediator decision.
- **§3.2 auth (Keycloak)** — *downgraded*: single user, nothing public. Still required the
  moment the dashboard is reachable beyond localhost/tailnet — the data is valuable.
- **§1.2 ProblemDetails**, **§4.4 pagination** (alert/signal feeds), **§3.1 FusionCache**.

## Walking skeleton (resequenced for the two use cases)

1. `Intel.Worker` ingest (Ethereum only) → `TokenDeployment` + entity/edge rows from the
   funding walk → dashboard list. **First red test:** Anvil testcontainer — deploy an ERC-20,
   assert the `TokenDeployment` row. Replaces Weather as the demo domain over time.
   *(Done 2026-09-02 on branch `trading`: plain-CREATE detection, polling listener, red→green.
   Dashboard list also done: `/tokendeployments` API endpoint + `/launches` Blazor page - live
   feed with tokens-only filter, launchpad badges, explorer links, 10s auto-refresh. Note: the
   Playwright E2E suite is currently broken machine-wide (pre-existing at the clean baseline,
   likely the Aspire 13.5 CLI update) - `LaunchesTests` is written and pending that fix.)*
   **1b. Launchpad adapters** (factory events + calldata decode, red test: deploy a factory in
   Anvil that CREATEs the token inside a call) — per the case study, this is the main
   detection path on launchpad-dominated chains; without it the Karma launch is invisible.
   *(Detection half done 2026-09-02: generic internal-CREATE detection via block logs +
   code-appeared-this-block check, `FactoryAddress` recorded; unique key is now
   (chain, contract). Still open: per-launchpad calldata decoding — whitelists, fee flags —
   and trace-based detection for log-less internal creations.)*
**Resequenced 2026-09-03** (the notification use case is pulled forward — the system should
ping the phone before it can search; a full ordering of the remaining steps):

1. **Pons calldata decoder** (rest of 1b): decode `launchAndBuy` calldata —
   `snipeTaxExemptions` insider whitelist (first `TokenDeploymentInsider`/edge rows) + creator
   launch-buy size. Red test: crafted calldata against Anvil. This is the first scoring-grade
   signal and what the first alert will lead with.
   *(Done 2026-09-03: `PonsLaunchAndBuyFunction` mirrors the verified `PonsV2LaunchAndBuy` ABI
   (full tuple incl. description/socials — rule-4 inputs decodable later without rework);
   detection stores `TokenDeploymentInsider` rows, `CreatorBuyQuote` (uint256 → numeric), and
   `PairTokenAddress` (rule-5 input). Red→green via crafted calldata against the Anvil test
   factory, plus a wrong-selector guard test.)*
2. **First alert end-to-end (Web Push)**: PWA-ify the dashboard (manifest + service worker),
   VAPID push from the BFF, rule v0 = "new token via known launchpad" (later: insider count ≥ N),
   deep link to `/launches`. Deliberately before scoring — usefulness beats completeness.
3. **Deployer context** (first bite of the entity graph): wallet age, tx count, first-funder
   walk per deployer — answered from our own archive where possible; stored as the first
   `Entity`/`Edge` rows. Turns the alert into "fresh wallet, funded minutes ago from X".
4. More chains: Base, Ethereum — config + registry row; Base is the volume stress test.
5. Universal search v1 (addresses/tx hashes only) + entity page with edges and evidence.
6. Watchlists + graph-aware alert rules (N funding-hops around watched entities).
   *(Robinhood Chain live 2026-09-02: `intel-worker` Aspire resource behind `Features:Intel`
   (default off), cursor persistence + bounded catch-up, RPC failover
   (Alchemy → publicnode → official; keyed URL in gitignored local config), Logs ingest mode
   for rate-limited endpoints (one getLogs per 10-block chunk + lookups for new addresses
   only; BlockReceipts mode remains for tests/friendly RPCs), 429 backoff. Real detections
   flowing into `TokenDeployments`. Same-day follow-up: ERC-20 metadata capture
   (name/symbol/decimals via eth_call, null = not a token) and launchpad attribution
   (`Launchpads` config map factory→name; Pons = `0xe33E9E479dF8802cb0866d5d05258bEc4cF62948`,
   verified against the Karma launch tx) - live Pons launches now land labeled, e.g.
   "TICKER (launchpad: pons)".)*
7. Collectors: DEX first (on-chain, no API-key zoo), then GitHub, then X.
8. Scoring v1 + LLM enrichment (entity resolution for free-text search, narratives) with the
   eval set.

**Operational note (open ❓):** detection only runs while the dev machine runs the AppHost —
every shutdown is an archive gap (bounded catch-up covers minutes, not nights). Eventually
this wants an always-on home (mini-PC / VPS / `aspire publish` to a container host); the
archive's value compounds with uptime. Also pending: the machine-wide Playwright E2E breakage
(tracked as its own task).

## Decided (2026-09-02)

- Solana: **out** for v1 (revisit after the EVM pipeline is proven).
- UI: keep the **Admin vertical (Blazor WASM + BFF)** as-is; new work item: PWA-ify it
  (manifest + service worker) for Web Push.
- Push channel: **Web Push API** (PWA on the iPhone Home Screen, VAPID from the BFF);
  channel-agnostic dispatch so ntfy/Telegram can replace it if iOS delivery disappoints.
- Nodes: **no self-hosting in v1** — paid archive+trace RPC and indexer APIs.
- Robinhood Chain: same pipeline as Base + known-issuer routing for official tokenizations.
- Assistant (MCP) proposals: **review queue** — pending until accepted in the dashboard,
  never live on write; tiered auto-apply is a possible later relaxation.

## Open decisions ❓

- **RPC/indexer providers per chain** (Alchemy/QuickNode/dRPC + Etherscan V2/Alchemy
  Transfers); trace API availability matters for CREATE2/factory detection; Robinhood Chain
  provider coverage is still young.
- **Backfill depth**: how far back for logs (outcome labeling) — traces stay on-demand per
  research job.
- Queue tech: start with Postgres outbox + in-proc channels; Redis/Kafka only on demonstrated
  need.
- X/Twitter API tier (pricing!) and whether Discord is worth the ToS/complexity at all.
- Attribution label sources (public label sets, manual curation in the dashboard).

## Risks

- RPC/WebSocket reliability → reorg + backfill logic is core, not an edge case.
- Social APIs are expensive and rate-limited; collectors must degrade gracefully (flags off =
  system still works).
- LLM claims look authoritative — without the deterministic cross-source gate the scoring
  quietly becomes an LLM opinion. The gate is architecture, not a prompt.
- Adversarial environment: expect deliberate spoofing (fake teams, wash trading, bought
  followers) — every dimension needs a "can this be cheaply faked?" note in its design.
- Entity merging is the classic trap: over-eager attribution poisons the graph. Confidence
  edges + reversible merges, never silent identity collapse.
- iOS Web Push requires the Home-Screen install and allows no silent pushes; if delivery
  proves laggy or throttled, swap the channel (dispatch is channel-agnostic by design).
- Indexer/RPC vendor lock-in: address-history and trace access are rented — abstract behind
  an interface per data class so a provider swap (or later self-hosted Reth) is contained.

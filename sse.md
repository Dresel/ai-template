# SSE draft on Groups: state and next steps

*Written 2026-10-08. Hand-over for the next session: where the hand-written server-sent event demo stands. Everything
below is uncommitted in the working tree.*

## Update 2026-10-09: generated from the spec

The emitter now generates server-sent events (`emitter.md`, section 5), and the draft runs on it. `groups.tsp` declares
`watch(): SSEStream<GroupEvents>` on `:watch`, with `GroupChange`, `GroupChanged` and a one-variant `@events` union, so
the contract is in `openapi.yaml`. Generated now, and deleted by hand: `GroupsWatchQuery`, the endpoint (in
`GroupsEndpoints`, whose route group lost its `/groups` prefix to the colon route), `ServerSentEventStreams` (now the
generated `ServerEventSupport`, keepalive 15 s), `GroupChanged` and `GroupChange` with the `AdminJsonContext` partial,
and the `GroupsClient` partial (now `WatchAsync` returning the generated `ServerEventReader<T>`, which closes the
connection when disposed and ends after 30 s of silence). The browser options went with it: .NET 10 streams responses
by default, and `ServerSentEventsResult` sends `Cache-Control: no-cache,no-store`. `GroupsClientTests` and
`TestWatchHandler` went too, the emitter tests the generated client. By hand still: `SignalHub`, the `Publish` calls,
`GroupsWatchHandler` (now a one-liner on `WatchHandler<TQuery, TSignal>`, which ends at the token's `exp`, read through
`ClaimsPrincipal.AccessTokenExpiry`), and `ChangeFeed`, renamed `LiveUpdates<T>`, which disposes each stream it reads.
`X-Accel-Buffering` comes from `UseUnbufferedEventStreams` for every `text/event-stream` response. Next items 4 and 5
below are done. The draft's own files were backed up in the session scratchpad before the deletion.

Later the same day: the hub ends a stream 15 minutes after the connect at the latest and caps a token without `exp`.
`WatchHandler` is authorized at the connect only: `MatchesAsync(query, subscriber, ct)` runs once per stream in the
subscriber's request and returns the synchronous filter `Publish` runs. The hub subscribes while that filter loads (it
takes a `Task<Func<T, bool>>`) and filters what arrived meanwhile on read. `LocalStreams` (now part of `SignalHub`) keeps every hub's streams by
subscriber, and `EndFor(users)` (now `SignalBus.CloseStreamsFor`) ends them: called after `SaveChangesAsync` by `GroupsAddMember`, `GroupsRemoveMember`,
`GroupsSetPermissions`, `GroupsDelete` (members read before the cascade), `UsersActivate` and `UsersDeactivate`. No
query per signal. An intermediate version that checked the permissions again before each signal was replaced.

Then Redis: the AppHost adds `redis` (`Aspire.Hosting.Redis`, image `redis:8.6`), the API `AddRedisClient("redis")`.
Command handlers inject `SignalBus` only: `Publish<T>` and `CloseStreamsFor(users)` send fire and forget (synchronous, no wait
for Redis, a command never fails because of it). The bus subscribes at startup (hosted service) to `signals:<Type>` per
`AddSignal<T>()` and to `streams:close`, through `ChannelMessageQueue` for ordered handling, and hands each signal to
the process's one `SignalHub` (`Deliver<T>`, `Close`, `CloseAll`), which holds the streams of every signal type as
private `Subscription<T>` objects. Every process receives its own
messages too, so there is one path. A reconnect of the subscription connection ends every local stream
(`ConnectionRestored`), since what was published meanwhile is lost. JSON through the API's options, so the generated
`AdminJsonContext` serializes. The filter now runs on the Redis message loop, outside any request. Integration tests:
`RedisFixture` (one container), each `ApiFixture` with its own `channelPrefix`. Deploying needs a Redis per environment
and `ConnectionStrings__redis` for the Admin API in SpatialFocus/Infrastructure.

Client: `LiveUpdates<T>` is 1:1 now, scoped, one stream per page's view model, closed with the page's scope. The
listener list, the sharing across pages and the race between stream generations went with it, since HTTP/1.1 is not a
target and one signal type is rarely watched twice at once. Its API is two events, `Received` (each signal) and
`Connected` (after every connect, the first included, so the page's read after it covers everything before the
subscription, without versions), and it starts in its constructor. A throwing handler is logged, and the other
handlers and the stream keep running.

Tests still to write: `AStreamEndsAtTheNextSignalOnceItsPermissionIsWithdrawn` deletes the membership in the database
directly and fails now, to be rewritten through `RemoveMember` asserting the stream ends without a signal. Then one
"open stream ends" test per handler above, a `WatchHandler` test for a filter that loads after a publish, and a test
of two API hosts on one Redis (a signal published on
one reaches a stream on the other, `CloseStreamsFor` too).

Then split by side under `Realtime/`: `Streams/` (local SSE: `WatchHandler`, `EventStreams<T>` per signal type with `IEventStreams` for
closing across types, `EventSubscription<T>` per open stream) and `Backplane/` (Redis: `SignalBackplane`, which command
handlers inject, and `BackplaneSubscription<T>` per type, a hosted service). `AddSignalBackplane()` and
`AddSignal<T>()`. The hub no longer filters: `WatchHandler.Handle` subscribes first, then awaits `MatchesAsync`, then
reads and filters in the stream's own request, so the filter may read `ICurrentUser` and the planned architecture test
is unnecessary. Every signal of a type now wakes every stream of that type. Channels are `signals:<FullName>`.

## What it does

The groups list updates live: a group created, changed or deleted in one tab appears in every other tab showing the list,
without a reload. `GET /groups:watch` sends signals (`groupChanged` with the group's id and `created`, `updated` or
`deleted`), and the list reads itself again on each one and after each reconnect.

## Files

| Where | Files |
|---|---|
| Admin.Api | `Streams/SignalHub.cs` (in-memory fan-out, one bounded channel per connection with its audience, ends at the token's `exp`), `Streams/ServerSentEventStreams.cs` (subscribes before the response opens, keepalive every 20 s, `X-Accel-Buffering` in the endpoint), `Features/UserManagement/Groups/GroupsWatchEndpoint.cs`, `GroupsWatchQuery.cs` (`IStreamQuery`), `GroupsWatchHandler.cs`, the six group command handlers publish after `SaveChangesAsync`, `Program.cs` registers the hub and maps the endpoint |
| Admin.Shared | `UserManagement/GroupChanged.cs`, `UserManagement/GroupChange.cs`, `AdminJsonContext.cs` (a partial with the event) |
| Admin.Client | `UserManagement/Groups/GroupsClient.cs`, a partial with `WatchAsync`: returns once the stream is open, browser response streaming, `cache: 'no-store'`, `./groups:watch` because of the colon |
| Admin.Web | `Foundation/ChangeFeed.cs` (singleton, one stream per tab, open only while a page listens, reconnects with 1–30 s and reports `Reconnected`, stops on 401/403), `Features/UserManagement/ServiceCollectionExtensions.cs`, `Features/UserManagement/Groups/ListPageViewModel.cs` |
| Tests | `Admin.Api.IntegrationTests/GroupsWatchTests.cs` (3), `Admin.Web.UnitTests/Foundation/ChangeFeedTests.cs` (3), `Admin.Web.UnitTests/Features/UserManagement/Groups/GroupsClientTests.cs` with `TestWatchHandler.cs` (1) |
| Docs | `AGENTS.md` (the exception to hand-written calls), `docs/spec-first.md` (the draft, the connection limit), `docs/web-pages.md` (`IViewModel.Changed` for feed reloads) |

**Status:** build green. Web unit tests 89, integration tests 63 (three full runs), architecture tests 5. The E2E suite
was not run. Checked by hand in the built-in Chromium and in Playwright Firefox 151, headed and headless.

## Found on the way

- **Subscription race:** the response opened before the handler had subscribed, so a signal published right away was
  lost (seen only in full integration runs). The endpoint now starts the source before it returns the result, and a
  first keepalive opens the response.
- **`NotSupportedException`** when a stream ended while waiting: disposing an async iterator with a pending
  `MoveNextAsync`. The keepalive helper awaits the pending read first.
- **CA1711** forbids the suffix `Stream` on a type, hence `ChangeFeed<T>`.
- **The browser's connection limit:** six HTTP/1.1 connections per host across all tabs. Six tabs with the groups list
  on `http://localhost:5770` hold all six, and a seventh tab does not even load the app (reproduced in Firefox). Through
  `https://localhost:7770` (HTTP/2, `h2` checked) eight tabs work. The feed used to keep its stream after leaving the
  list, which filled the six quickly. It now closes with the last listener.
- **`cache: 'no-store'`** on the watch request came from a theory about Firefox's HTTP cache holding a second request for
  the same URL. It did not fix the hang on its own, and stays because a stream must not be cached anyway.
- **Streams end at the access token's `exp`** by design, then reconnect and reload. A stream opened shortly before the
  token expires lasts only seconds to a minute.
- **The resilience handler's 10 s attempt timeout** aborts a watch request that waits in the browser for a connection,
  which shows as `NS_BINDING_ABORTED` without timings in Firefox.

## Next

1. **Streams only in visible tabs** (proposed, not decided): `ChangeFeed` closes on `visibilitychange` to hidden and
   reconnects and reloads when visible, through a small JS module. Then a window holds one stream, and
   `http://localhost:5770` no longer runs out of connections.
2. **Detail page:** not wired, since a reload discards unsaved permission edits.
3. **Permission changes:** done through `SignalBackplane.CloseStreamsFor` in the user management handlers. A change outside them
   (SQL by hand, `AdministratorBootstrap`) reaches a stream at the token's `exp` or after 15 minutes.
4. **Contract:** missing from `openapi.yaml`, the emitter generates nothing yet (emitter.md, task 5, which still shows the
   old `/events` sketch: switch it to `:watch` per feature with one event type, no union).
5. **Backlog §6 row:** add per-feature `:watch` instead of `GET /events?topics=…`, the connection findings above, and that
   the draft exists.
6. **Scale-out:** done on Redis Pub/Sub (see the Redis paragraph at the top). The `observer` publishes through the same bus
   once it exists.
7. **E2E:** run the suite, and consider a test with two pages on the list.
8. **Decide** whether the draft stays in the template (commit) or moves to a branch.

## Try it

```bash
aspire start
```

Open `https://localhost:7770/user-management/groups` in two tabs (`developer` / `developer`), create, rename or delete a
group in one, and watch the other. Afterwards `aspire stop`. Playwright's Firefox (`firefox-1533`) is installed for
`playwright-cli open --browser=firefox`.
# SSE draft on Groups: state and next steps

*Written 2026-10-08. Hand-over for the next session: where the hand-written server-sent event demo stands. Everything
below is uncommitted in the working tree.*

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
3. **Permission changes** do not end the user's streams yet (`EndFor(user)` in the hub, from the user management
   handlers).
4. **Contract:** missing from `openapi.yaml`, the emitter generates nothing yet (emitter.md, task 5, which still shows the
   old `/events` sketch: switch it to `:watch` per feature with one event type, no union).
5. **Backlog §6 row:** add per-feature `:watch` instead of `GET /events?topics=…`, the connection findings above, and that
   the draft exists.
6. **Scale-out:** in memory only. Redis Pub/Sub comes with a second API process or the `observer`.
7. **E2E:** run the suite, and consider a test with two pages on the list.
8. **Decide** whether the draft stays in the template (commit) or moves to a branch.

## Try it

```bash
aspire start
```

Open `https://localhost:7770/user-management/groups` in two tabs (`developer` / `developer`), create, rename or delete a
group in one, and watch the other. Afterwards `aspire stop`. Playwright's Firefox (`firefox-1533`) is installed for
`playwright-cli open --browser=firefox`.

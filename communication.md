# Communication feature: proposal

*Written 2026-10-13, nothing started. A feature package that delivers notifications to users through several sinks:
the bell in the header, e-mail, desktop notifications and Web Push. Sources: the weather feature as the packaging
reference (`features.md`), the server-sent events draft (`sse.md`), the backlog (§2.1 events, §6 outbox), and
`lamama.md`, whose module list already names "Mitteilungen". Verified on the way: the Community Toolkit's Mailpit
integration (`aspire.dev`), MailKit 4.18 and MimeKit are `IsAotCompatible` and `IsTrimmable` (dotnet-inspect), the two
established .NET Web Push libraries carry no trim or AOT annotations (NuGet, 2026-10-13), and Aspire 13.6.1 has
`AddParameter(name, ParameterDefault, secret, persist)` for a generated key.*

## Goal

Any handler can tell a user something: "your station was renamed", "you were added to a group", "a release finished".
The user sees it in the app (a bell with an unread count, a list, a toast while they are there), gets it by e-mail
when they are not, and as a desktop notification when they asked for one. The user decides per topic and sink. The
feature drops into a host the way weather does, and Lamama takes it as its "Mitteilungen" module.

Two meanings of "communication" are kept apart. A **notification** is what a user receives, one message to one user,
stored as the record. A **sink** is where a copy of it goes: in-app, e-mail, Web Push, later a chat webhook or mobile
push. The feature owns the storage, the preferences and the delivery. Whoever has something to say is a **producer**:
a handler in the host, in another feature, or in the feature itself (announcements).

## Decisions

| # | Question | Decision |
|---|---|---|
| 1 | Slice or feature package | A feature package, `src/features/communication/`, built like weather: own spec, schema, migrations, pages. It has its own tables, its own worker and a page or three, and it is the module Lamama's shell already lists. A slice would tie it to this host. |
| 2 | Name | `communication` for the package (`FocusTemplate.Communication.*`, schema `communication`, service `FocusTemplate.Communication`), since e-mail and push are more than the bell. `Notification` for the thing a user gets. `Sink` for the destination: `Channel` is taken twice in the same assemblies, by `System.Threading.Channels` in the stream code and by the Redis channels of the backplane. |
| 3 | How a producer raises one | Through an outbox the spine promises, written in the producer's own transaction: `OutgoingNotification` in `Data.Foundation`, table `public.notification_outbox`, created by the spine's migration and mapped with `ExcludeFromMigrations()` in every feature context, as `UserReference` is. The feature's dispatcher takes the rows and deletes them. This is the outbox `features.md` planned as the spine's second promised table, narrowed to one job. Rejected: an `INotifier` service writing to the feature's context (a second transaction, so a crash between the two loses the notification, and a failing write either turns a committed change into a 500 or is swallowed) and a Mediator `INotification` (the same, in process). |
| 4 | Texts | The producer renders title and body in every supported culture when it raises, from its own resx, and the row stores them as data. The feature picks the recipient's language per sink. No topic registry, no cross-feature texts, and the text is frozen as the record of what happened. A registry that renders at delivery time comes only if templates should become editable at run time. |
| 5 | Recipient's language | The feature stores it per user (`user_settings.language`), default `Cultures.Default`, chosen on the settings page. The server knows nothing else: the app keeps the culture in local storage. |
| 6 | Where the dispatcher runs | A hosted service in the Admin API, registered by `AddCommunication()`. Replicas are safe through `SELECT … FOR UPDATE SKIP LOCKED` on the outbox and the deliveries. Moving it into a worker host (the `observer` of backlog §10 is the model) is one registration line, `AddCommunicationDispatcher()` in the worker's `Program.cs`. |
| 7 | In-app is the record | Every notification is stored and listed. The bell shows the unread count, the other sinks send copies. In-app cannot be muted, e-mail and Web Push can, per topic. |
| 8 | E-mail library | MailKit (MIT, .NET Foundation, 4.18.1, both it and MimeKit marked AOT-compatible and trimmable). `System.Net.Mail.SmtpClient` is the one Microsoft does not recommend for new code. |
| 9 | Web Push library | None by default. RFC 8291 (encryption: ECDH P-256, HKDF, AES-128-GCM) and RFC 8292 (VAPID: an ES256 JWT) on `System.Security.Cryptography`, about 150 lines, checked against the worked example in RFC 8291 Appendix A. The landscape, checked 2026-10-13: `WebPush` (7.4 M downloads, MPL-2.0) got its first release in five years in April 2026 with net8 to net10 targets, and still runs on `Portable.BouncyCastle` 1.9.0 from 2021, without symbols or trim annotations. `Lib.Net.Http.WebPush` 3.3.1 (2.2 M downloads, MIT, March 2025) targets net6.0, uses the framework's crypto there and BouncyCastle only on netstandard2.0, and carries no trim or AOT annotations either. Everything else on NuGet is tiny or part of another framework. The protocol is frozen since 2017, so an unchanged library is a finished one, and `Lib.Net.Http.WebPush` is the alternative worth half an hour in the spike: a trimmed publish of a scratch host without warnings makes it the pick. |
| 10 | Dev mail | Mailpit from `CommunityToolkit.Aspire.Hosting.MailPit` behind `Features:LocalMail` (on). Off, and always in publish mode, an SMTP server from parameters, the shape of the external Keycloak realm. |
| 11 | Push keys | One secret parameter `vapid-private-key`, generated by a `ParameterDefault` of the AppHost's own on the first start and persisted to user secrets (`persist: true`), so subscriptions survive restarts. The public key is derived at run time. A deployment supplies the parameter. |
| 12 | Permissions | Reading one's own notifications and settings needs a signed-in user only. `Communication.SendAnnouncements` gates the announcement page, `Communication.ViewDeliveries` the delivery log (later). |
| 13 | Service worker | Push only: no fetch handler, no precaching. It ships as the feature's static web asset and is registered when the user turns Web Push on. Offline caching is a different feature with its own failure modes. |

## Target layout

```
src/
  FocusTemplate.Primitives            + Topic (a string typed id like Permission), + Cultures (moved from Admin.Shared,
                                        see Spine changes)
  FocusTemplate.Data.Foundation       + Notifications/: OutgoingNotification, OutgoingNotificationText, the configuration
                                        (public.notification_outbox, ExcludeFromMigrations in feature contexts),
                                        DbContextExtensions.Notify(...), NotificationTexts.Render(...)
  FocusTemplate.Data                  maps the outbox for real, one migration
  features/communication/
    spec/                             api.tsp (@service FocusTemplate.Communication), primitives.tsp (NotificationId,
                                      PushSubscriptionId), permissions.tsp, notifications.tsp, notification-settings.tsp,
                                      push-subscriptions.tsp, announcements.tsp
    FocusTemplate.Communication.Shared  the wire contract (generated), CommunicationTopics (the feature's own topics)
    FocusTemplate.Communication.Client  the typed clients (generated)
    FocusTemplate.Communication.Data    Notification, NotificationText, Delivery, Sink, DeliveryState, UserSettings,
                                        TopicSetting, PushSubscription; CommunicationDbContext and its read-only twin,
                                        schema communication, migrations
    FocusTemplate.Communication.Api     handlers; NotificationDispatcher (hosted); Sinks/: IDeliverySink, EmailSink
                                        (MailKit, Mail/Layout.razor rendered with HtmlRenderer), WebPushSink
                                        (WebPushEncryption, Vapid); AddCommunication(), MapCommunicationEndpoints()
    FocusTemplate.Communication.Web     NotificationBell (+ view model), Notifications/ListPage, Settings/Page,
                                        Announcements/Page, wwwroot/notifications.js (desktop notifications, push
                                        registration), wwwroot/push-worker.js; AddCommunicationPages()
```

References as weather's: the feature references `Primitives`, `Http`, `Data.Foundation`, `Api.Foundation`,
`Admin.Web.Foundation`, `Admin.Web.ClientServiceDefaults`, and no host project and no other feature. The host
references everything.

## Host composition

```csharp
// Admin.Api/Program.cs
builder.AddCommunication(ConnectionNames.Database, ConnectionNames.ReadOnlyDatabase);   // contexts, endpoints, the
                                                                                         // dispatcher, the sinks the
                                                                                         // configuration enables
builder.Services.AddSignalBackplane().AddSignal<GroupChanged>().AddSignal<NotificationChanged>();
endpoints.MapCommunicationEndpoints();

// Admin.Web/Program.cs
builder.Services.AddCommunicationPages(builder.HostEnvironment, ResourceNames.AdminApi,
    topics: [.. WeatherTopics.All, .. UserManagementTopics.All, .. CommunicationTopics.All]);

// Admin.Web/Shell/MainLayout.razor: the bell in the header's right-hand stack, the menu entry nav-notifications
<NotificationBell />

// AppHost
IResourceBuilder<EFMigrationResource> communicationMigrations = api.AddMigrations<CommunicationDbContext>(
        "communication-migrations", "../features/communication/FocusTemplate.Communication.Data/….csproj", focusDb)
    .WaitForCompletion(migrations);
api.WaitForCompletion(communicationMigrations);

// The shape of KeycloakRealm: Mailpit behind Features:LocalMail, the SMTP parameters otherwise
MailServer mail = addLocalMail ? builder.AddLocalMailServer() : builder.AddExternalMailServer();
api.WithMailServer(mail)
    .WithVapidKeys(builder)                                              // the generated secret parameter
    .WithEnvironment("Communication__AppUrl", web.GetEndpoint("http"));  // absolute links in e-mails

// src/spec/primitives.tsp
import "../features/communication/spec/primitives.tsp";
```

The topics a host hands to `AddCommunicationPages` are what the settings page lists (a user can mute a topic before
its first notification). Their labels sit in the host's resx keyed by topic, as `PermissionLabels` labels permissions,
and a topic the resx lacks reads off its name. The API needs no topic list: a topic is a string the producer owns.

## The pipeline

1. **Raise.** A producer calls, inside its own handler and transaction:

   ```csharp
   dbContext.Notify(
       recipient: station.OwnerId,
       topic: WeatherTopics.StationRenamed,
       texts: NotificationTexts.Render(() => new NotificationText(
           localizations.StationRenamedTitle,
           localizations.StationRenamed(station.Name, actor.DisplayName))),
       link: WeatherRoutes.StationsPath);
   await dbContext.SaveChangesAsync(cancellationToken);
   ```

   `Render` switches `CultureInfo.CurrentUICulture` through `Cultures.Supported` and collects one text per culture,
   so the ResXGenerator classes the handler already holds do the work. The row lands in `public.notification_outbox`
   with its texts in `notification_outbox_texts`, committed with the change it reports. The texts are child rows,
   two per notification today, which keeps EF, the precompiler and AOT away from JSON serialization.

2. **Delayed.** `Notify` takes an optional `deliverAt` and an optional `key`. A row with a future `deliverAt` waits in
   the outbox, since intake claims due rows only (`deliver_at <= now`), so nothing of it exists in the feature before
   its time, and a reminder that became moot is withdrawn with `CancelNotification(recipient, topic, key)`, one
   `ExecuteDelete` on the outbox. The key is unique per recipient and topic, so a producer that raises the same thing
   again (an observation over the threshold every ten minutes) replaces the waiting row through an upsert and never
   stacks them. "Remind me in three days unless done" is this pair: raise with `deliverAt`, cancel when done. The
   digest is the same idea one level down: an e-mail delivery of a user in digest mode is written with
   `next_attempt_at` at the next digest time, and the e-mail sink sends every due delivery of one user as one mail.

3. **Intake.** The dispatcher polls the outbox every two seconds (`PeriodicTimer` on `TimeProvider`), claims a batch
   with `FOR UPDATE SKIP LOCKED`, and per row: reads the recipient's settings, writes the `Notification` with its
   texts, one `Delivery` per sink the recipient has not muted and the host has configured (e-mail, Web Push), deletes
   the outbox row, commits. Then it publishes `NotificationChanged { id, recipientId, created }` on the backplane, so
   the recipient's bell reloads in every tab and process. Two seconds of latency is the price of no wake-up signal.
   A `Publish` from the producer's side to wake the dispatcher is an optimization for later and needs nothing in the
   spine.

   `FOR UPDATE SKIP LOCKED` is Postgres's queue primitive, there since 9.5: `SELECT … FOR UPDATE` locks the rows a
   query returns until its transaction ends, and `SKIP LOCKED` makes a second worker's query pass over those rows
   and return the next ones in place of waiting. So every replica runs the same loop against the same table, each
   row goes to exactly one of them, and nothing coordinates. A worker that dies mid-batch rolls its transaction back
   and the rows reappear for the others, which is the retry. The job frameworks run on the same clause underneath
   (Hangfire's Postgres storage, Quartz's clustered store, pg-boss, Oban, Solid Queue). EF Core has no API for a
   locking clause, so the claim is one `FromSql` with the snake_case names, composable with the rest of the query.

4. **Delivery.** A second loop claims due deliveries (`state = pending and next_attempt_at <= now`, again
   `SKIP LOCKED`), hands each to its sink, and records the outcome: `sent` with `sent_at`, or `attempts + 1` with a
   growing `next_attempt_at` (1, 5, 30 minutes, 2 hours), `failed` with `last_error` after five tries. A sink that
   reports the address as gone (a 404 or 410 from a push service) also removes the subscription.

   Delivery holds no transaction across an SMTP or push call. Its claim is an update, `state = sending` and
   `claimed_until = now + 5 minutes` on the rows a `SKIP LOCKED` subquery picked, committed at once, then the send,
   then the outcome. A worker that dies mid-send leaves a row whose lease expires, and the next claim takes `sending`
   rows past their `claimed_until` as well. A sink is therefore at-least-once: a mail can go out twice after a crash
   between the send and the outcome, the accepted trade against holding database connections open through external
   calls.

5. **Read.** `markRead` and `markAllRead` set `read_at` and publish `NotificationChanged { read }`, so the badge
   clears in the other tabs too. A desktop or push notification clicked opens the link and marks it read.

6. **Retention.** A janitor in the same hosted service, on one replica at a time (below), deletes notifications
   older than a configured age (`Communication:Retention`, 90 days) once a day. Later item, cheap.

### Background work without a job framework

Every replica runs the dispatcher and the database hands out the work, which makes the loops above a distributed
background service already: delayed rows, retries with backoff, a digest, and two replicas never doing one row twice.
The one job that must run once per schedule, the janitor, takes a Postgres advisory lock (`pg_try_advisory_lock`, the
way `lamama.md` plans one release runner per environment) and skips its turn while another replica holds it. Wake-up
latency is the polling interval, two seconds, and the cost is one indexed query per interval per replica, with
`LISTEN/NOTIFY` or a backplane signal as the upgrade if two seconds ever matter.

A job framework earns its place when a third unrelated job family appears (reports, imports, cleanups across
features) and someone wants cron expressions and a dashboard for them. The choice is then between a generic claim
loop in the spine, the same two hundred lines once more over a `jobs` table, and a library:

| Library | Notes |
|---|---|
| Quartz.NET | Apache-2.0, mature, clustered through its ADO job store on Postgres, cron triggers, misfire handling. Instantiates jobs and serializes job data by reflection, so nothing for an AOT host |
| Hangfire | The dashboard everyone knows. `Hangfire.Core` is LGPL-3.0, the Pro features are commercial, job invocations are serialized method expressions, reflection throughout |
| TickerQ | Source-generated, EF Core store, a free dashboard, dual MIT and Apache-2.0 per its repository, .NET 8+. Young, worth a look once the need is real |
| Wolverine | Durable messaging with scheduled messages over a Postgres transport, a bigger commitment than a scheduler. Package scanners list the core as MIT and its CritterWatch add-on under a source-available license, to be verified per package |
| Coravel, NCronJob | In-process only, no persistence, so nothing for replicas |

None of them is needed for notifications.

## Spec

A service of its own, `@useAuth(BearerAuth)`, four interfaces, one file each. Every operation scopes to the caller:
another user's notification id is a 404.

- **`Notifications`** at `/notifications`: `search` (`@httpQuery` over `PageQuery` plus `unreadOnly`, newest first),
  `summary` at `:summary` (the unread count and the latest ten, what the bell shows), `markRead` at `/{id}:read`,
  `markAllRead` at `:read-all`, `watch(): SSEStream<NotificationEvents>` at `:watch` with the one variant
  `notificationChanged: NotificationChanged { id, recipientId, change: created | read }`. The handler's `MatchesAsync`
  returns `signal => signal.RecipientId == subscriber`, so a stream only ever carries its own user's signals. The
  recipient travels in the event because the stream's type is the signal's type; the filter runs on the server.
- **`NotificationSettings`** at `/notification-settings`: `get` (language, the sinks the host has configured, this
  user's push subscriptions with labels, the topic overrides) and `update` (`ValidationProblem` behind the form).
- **`PushSubscriptions`** at `/push-subscriptions`: `key` at `:key` (the VAPID public key), `subscribe` (`PUT`,
  upsert by endpoint: endpoint, `p256dh`, `auth`, a label such as the browser's name), `unsubscribe` (`DELETE /{id}`,
  204 for a missing one).
- **`Announcements`** at `/announcements`, `@requiresPermission(Permissions.Communication.SendAnnouncements)`: `send`
  with title, body and an optional link, to every active user. One outbox row per recipient, in one transaction,
  which is fine at the template's scale and worth a note at ten thousand users. "To a group" waits for the spine to
  promise group membership as a second reference shape (`GroupMemberReference`, the `UserReference` pattern), since a
  feature reads only the spine tables it is promised.

Typed ids: `NotificationId` (uuid v7, self-assigned) and `PushSubscriptionId` (uuid) in the feature's
`primitives.tsp`, as weather keeps its own. `Topic` (`extends string`) in the spine's `primitives.tsp`, because the
spine's outbox carries it. Deliveries keep an `int64` identity key inside the feature and never reach the wire.

## Data

Schema `communication`, `CommunicationDbContextBase.Schema`, a history table of its own, native enums `sink` and
`delivery_state` in the schema (as weather's `station_status`).

| Table | Columns | Notes |
|---|---|---|
| `notifications` | id, recipient_id → `public.users`, topic, link?, created_at, read_at? | indexes `(recipient_id, read_at)`, `(recipient_id, created_at desc)`; no `IAuditable`, the dispatcher writes them as `WellKnownUsers.System` |
| `notification_texts` | notification_id, culture, title, body | owned rows, one per supported culture |
| `deliveries` | id, notification_id, sink, state (pending, sending, sent, failed), attempts, next_attempt_at, claimed_until?, last_error?, sent_at? | index `(state, next_attempt_at)` |
| `user_settings` | user_id PK → `public.users`, language | `IAuditable`, the user edits it |
| `topic_settings` | user_id, topic, sink, enabled | PK over the three; absence means the default, enabled |
| `push_subscriptions` | id, user_id, endpoint (unique), p256dh, auth, label, created_at, last_used_at, failures | `IAuditable` |

Spine, `public`: `notification_outbox` (id identity, recipient_id → `users`, topic, link?, deliver_at, key?,
created_at, a unique index on `(recipient_id, topic, key)` where the key is set) and `notification_outbox_texts`.
Every string has its `[MaxLength]` (an endpoint 2000, a title 200, a body 4000, a key 200).

## In-app

- **`NotificationBell`**, a component with a DI scope of its own, since it lives in the layout for the whole session: a
  `ViewModelComponent<TViewModel>` in `Admin.Web.Foundation`, `OwningComponentBase` plus `IViewModel.Changed`, which
  `ViewModelPage<T>` then builds on. Its view model takes the generated `NotificationsClient`, a
  `LiveUpdates<NotificationChanged>` and the usual helpers, loads the summary on `Connected` and on every `Received`.
  `RadzenBadge` shows the count, `RadzenPopup` the latest ten (both exist in Radzen 11.5.1), each row a link that
  marks read. A `created` signal while the page is visible raises a toast through Radzen's `NotificationService`,
  called from the component, so the view model stays free of UI services as the rule says. Test ids:
  `notifications-bell`, `notifications-badge`, `notification-item`, `notifications-all`, `mark-all-read`.
- **`/notifications`**: the full list, paged, an unread filter, mark read per row and for all, the usual
  `ViewModelPage`. **`/notifications/settings`**: language, the topic toggles (one `data-testid` per topic and sink),
  the desktop notification button, the Web Push toggle with this browser's subscription marked and the others
  removable. **`/notifications/announce`**: the permission-gated form.
- **Connections**: the bell's stream is open for the life of the tab. Through the ingress (HTTP/2) that costs nothing.
  On plain `http://localhost:5770` every tab holds one of the browser's six HTTP/1.1 connections from the start, and a
  groups list adds a second, so three tabs are the comfortable limit. "Streams only in visible tabs" (`sse.md`, next
  item 1) moves from proposed to needed with this feature, and goes in with stage 1 or right after.

## E-mail

- **AppHost**: `AddMailPit(ResourceNames.Mailpit)` behind `Features:LocalMail` (on, run mode only): SMTP on 1025, the
  web inspector on 8025 as a dashboard link, health checks built in. The API gets it as
  `WithReference(mailpit, ConnectionNames.Mail)`, which injects `ConnectionStrings__mail=Endpoint=smtp://host:port`.
  With the flag off, and always when publishing, the parameters `smtp-host`, `smtp-port`, `smtp-username`,
  `smtp-password` build the same connection string through `AddConnectionString(ConnectionNames.Mail, expression)`,
  with `smtps://` for implicit TLS and STARTTLS the default otherwise. `mail-from` is a parameter either way (a fixed
  default locally), and the stage's values live in its Secret in `SpatialFocus/Infrastructure`.
- **Sender**: `EmailSink` on MailKit's `SmtpClient`, one connection per delivery batch. The message is a
  multipart/alternative of a plain text part and an HTML part.
- **Layout**: `Mail/Layout.razor` in `Communication.Api` (the project takes the Razor SDK), rendered with
  `HtmlRenderer` from `Microsoft.AspNetCore.Components.Web`, the renderer the unit tests already drive. The layout
  frames title, body and a button for the link, absolute through `Communication:AppUrl`, with a footer line linking
  the settings page. Its own texts come from a resx and render in the recipient's language, `CurrentUICulture` set
  around the render. Plain string templates would be less machinery, and a `.razor` file is what a fork will want to
  edit.
- **Modes**: immediately, per delivery. A daily digest (one mail per user per day) is a later mode of this sink, a
  flag on `user_settings` and a different claim query, no new tables.

## Desktop notifications

Two different things, two stages:

- **From the open tab** (stage 5, small): the bell already receives every signal. When the tab is hidden and the user
  allowed it, the JS module shows an OS notification through the Notifications API (`new Notification(title, { body,
  tag: id })`), clicking it focuses the tab and navigates to the link. The permission is asked from the settings page,
  on a click, the only place a browser grants it. It needs no service worker, no push service and no server change.
- **Web Push** (stage 6): works with no tab open. `push-worker.js` handles `push` (parse the JSON, `showNotification`
  with `tag` and `data.link`) and `notificationclick` (focus a client of the origin or open one at the link, and the
  page marks the notification read when it loads with `?notification=<id>`). The settings page registers the worker
  from `_content/FocusTemplate.Communication.Web/push-worker.js`, subscribes with the VAPID public key from
  `:key` (`userVisibleOnly: true`), and `PUT`s the subscription with a label. The server encrypts the payload per RFC
  8291 for the subscription's `p256dh` and `auth`, signs a VAPID JWT for the push service's origin (cached per origin
  for an hour), sends with `TTL` and `Urgency: normal`, and keeps the payload small (title, body, link, id; the limit
  is 4 KB). A 404 or 410 deletes the subscription, a 429 or 5xx schedules the retry, `Retry-After` respected.
- **Browser support**: Chromium, Firefox and desktop Safari. iOS Safari delivers Web Push only to a site installed to
  the home screen, which needs a web app manifest the Admin app does not have. Out of scope, noted on the settings
  page's help text.
- **CSP** (backlog 3.3): the worker needs `worker-src 'self'`, and `connect-src 'self'` stays enough since the
  subscription goes through `/_api`.

## Other sinks

| Sink | Fit | When |
|---|---|---|
| Chat webhook (Teams, Slack) | Addressed to a channel, so it is a topic-to-URL mapping in configuration, one HTTP POST with a card, no user settings. Lamama needs Teams for releases | after e-mail, small |
| Mobile push (FCM, APNs) | The MAUI app registers a device token through the Public spec, the sink sends through the platform's HTTP API. Needs a signed-in mobile user, so it waits for the Public API's PKCE (backlog 8.1) | later, medium to large |
| SMS | A provider, cost per message, and no use in the template's domains | not planned |
| Digest | A mode of e-mail, see above | later, small |

## Testing

As the development loop wants it: each behaviour starts red at the smallest level that fits.

**Architecture** (`FocusTemplate.ArchitectureTests`)
- `FeatureTests.AFeatureReferencesNoHostProject` gets the five new assemblies. New with the first host that holds two
  features: `AFeatureReferencesNoOtherFeature`, so communication and weather stay apart (backlog 2.3 had it as a
  candidate, now it has a reason).
- `HandlerDbContextTests`, `ModelSentinelTests` and `APooledContextTakesItsOptionsAlone` cover the new handlers, ids
  and contexts once they are listed.
- `UserReferenceTests` gets a sibling for the outbox shape: the spine's `OutgoingNotification` mapping and the one a
  feature context sees agree on the columns.

**Integration** (`FocusTemplate.Admin.Api.IntegrationTests`, through the generated clients)
- `ApiFixture` migrates the communication context after the spine's and exposes `CreateCommunicationDbContext()`.
  `NotificationDispatcher.RunOnceAsync()` is public, so a test drives intake and delivery deterministically and the
  timer loop gets one test of its own on `FakeTimeProvider`.
- `NotificationsTests`: a row written through `Notify` on a test context becomes a notification with both texts;
  the summary counts unread; `markRead` and `markAllRead`; a foreign id is 404; search pages newest first.
- `NotificationsWatchTests`: a `created` signal reaches the recipient's stream and never another user's, across a
  replica too (`CreateReplica`, the `GroupsWatchTests` shape).
- `AnnouncementsTests`: one notification per active user, none for a deactivated one, 403 without the permission,
  400 on an empty title.
- `DelayedNotificationsTests`: intake leaves a row with a future `deliverAt` alone until `FakeTimeProvider` passes
  it, `CancelNotification` removes it, a second `Notify` with the same key replaces the waiting row, a delivery in
  `sending` past its lease is claimed again, and the janitor runs on one of two hosts only.
- `NotificationSettingsTests`: the language and a muted topic change what intake writes (no delivery for a muted
  sink), a sink the host has not configured never gets a delivery.
- `EmailDeliveryTests` with a `MailpitFixture` (assembly fixture, Testcontainers generic container on
  `axllent/mailpit`, `ConnectionStrings:mail` set by `ApiFixture`): the mail arrives with the subject in the
  recipient's language, the link is absolute, a recipient without an e-mail address gets a `failed` delivery with a
  reason, a refused connection schedules a retry. Assertions read Mailpit's HTTP API
  (`GET /api/v1/search?query=to:<address>`), each test with an address of its own, so no reset is needed.
- `WebPushDeliveryTests`: the push `HttpClient`'s primary handler is a `PushServiceStub` the test owns. It records
  the request, checks the VAPID `Authorization` header with the public key, decrypts the body with the subscription's
  private key the test generated, and asserts the JSON. A 410 deletes the subscription, a 429 with `Retry-After`
  schedules the retry, five failures end in `failed`.
- `WebPushEncryptionTests`: RFC 8291 Appendix A's keys, salt and plaintext give its ciphertext, byte for byte, and
  the VAPID token verifies. A test of one type alone, as `EventStreamsTests` is.
- `DispatcherReplicaTests`: two hosts on one database run intake and delivery at once over fifty outbox rows, and
  every notification has exactly one delivery per sink. The `SKIP LOCKED` proof.
- Outbox: a producer's `Notify` and its `SaveChangesAsync` fail together (a bad recipient id rolls back the business
  change too), proven on the weather context once stage 2 adds the producer.

**Unit** (`FocusTemplate.Admin.Web.UnitTests`)
- `NotificationBellViewModelTests` against a fake handler: the count, the reload on `Connected` and `Received`, a
  failure message. `SettingsFormTests` for the validator. `DesktopNotificationsTests` for the decision "show when
  hidden and allowed", kept in C# so the JS module only shows.
- The e-mail layout renders through `HtmlRenderer` with a German culture and contains the translated button text.
  This one sits in the integration test project, next to the sink it serves, since the Web unit tests cannot reference
  the API.

**E2E** (`FocusTemplate.Admin.Web.E2E`)
- `BlazorAppFixture` pins `Features:LocalMail=true` and exposes the Mailpit endpoint.
- `NotificationsTests`: the developer sends an announcement; in a second page of the same context the badge goes to
  one without a reload (the `LiveGroupsList` pattern, waiting for `:watch` to open first); the popup lists it; mark
  read clears the badge in both pages; the list page shows it read. The menu entry leads to `/notifications`.
- `NotificationSettingsTests`: the language and a topic toggle round-trip through a reload.
- `EmailTests`: an announcement arrives in Mailpit for the developer's address, polled through the API, with the
  German subject after the language was switched.
- `WebPushTests` (Chromium only): `Context.GrantPermissionsAsync(["notifications"])`, `PushManager.subscribe` stubbed
  by `AddInitScriptAsync` to return a canned subscription, since Playwright cannot reach a push service; turning the
  toggle on registers the worker (`navigator.serviceWorker.getRegistration()` names the script) and the subscription
  shows on the settings page. A stretch, Chromium's DevTools protocol `ServiceWorker.deliverPushMessage` delivers a
  payload to the worker, which also posts it to its clients, so the page can assert the parse. The weakest tier; the
  encryption and the dispatch are proven below it.
- Desktop notifications: the permission button and its state only. An OS notification is not observable from
  Playwright, and the decision is unit-tested.

**Mobile**: nothing in this plan.

**Observability**: one telemetry class on `IMeterFactory` (backlog 1.4's pattern): `focustemplate.communication.deliveries`
by sink and state, `focustemplate.communication.outbox.age` as the oldest unprocessed row, an activity per dispatcher
batch, the notification id in every log line. The dashboard shows a stuck outbox as a rising gauge.

## Spine changes

1. **`Topic`** in `src/spec/primitives.tsp`, a `Permission`-like string id.
2. **`Cultures`** moves from `Admin.Shared/Localization/` into `FocusTemplate.Primitives` (hand-written, no package),
   so `Data.Foundation`'s `Render` and a feature's handler can enumerate the supported cultures. Backlog 8.2 wanted
   the same move for the Public API. If `Primitives` should stay generated plus partials, a small
   `FocusTemplate.Foundation` project is the alternative.
3. **The outbox** in `Data.Foundation` (`Notifications/`), mapped by `AppDbContextBase` for real and promised to
   feature contexts through `ApplyFoundationReferences()` next to `UserReference`, one spine migration.
4. **Routes a producer links**: a page whose address a server-side producer puts into a link keeps that address in
   the feature's `Shared` project (`WeatherRoutes.StationsPath`), and its `ListPage.Path` reads it from there. The
   web-pages rule gets that sentence.
5. **`ViewModelComponent<TViewModel>`** in `Admin.Web.Foundation`, the scope-owning base `ViewModelPage<T>` derives
   from.
6. **`ConnectionNames.Mail`**, **`ResourceNames.Mailpit`**, the flag `Features:LocalMail` pinned in both E2E fixtures.
7. **Producers**: weather's `StationsRenameHandler` notifies the owner when somebody else renames their station (one
   query for the actor's `UserReference`), user management's `GroupsAddMemberHandler` notifies the new member. Their
   topics are constants in `Weather.Shared` and `Admin.Shared`.

## Order of work

One commit per stage, each leaving the suite green and the feature usable.

| Stage | What | Size |
|---|---|---|
| 0 | Spine: `Topic`, `Cultures`, the outbox and `Notify`, `ViewModelComponent`, the no-other-feature architecture test | S |
| 1 | In-app: the five projects, spec and generated code, schema and migrations resource, intake and the signal, the bell, the list page, announcements, tests. First producer: announcements, so nothing outside the feature changes yet | M/L, 3 to 4 days |
| 2 | Producers in weather and user management, the outbox tests on a feature context | S, a day |
| 3 | Settings: language and topic toggles, `topics` in the host's composition and the labels | S/M |
| 4 | E-mail: Mailpit and the parameters, MailKit, the layout, the delivery loop with retries, `MailpitFixture`, E2E through the API | M, 2 days |
| 5 | Desktop notifications from the open tab | S |
| 6 | Web Push: the worker, subscriptions, the VAPID parameter, RFC 8291 and 8292, the stub, E2E with the stubbed subscribe | M/L, 2 to 3 days |
| 7 | Docs: `docs/communication.md` and `.claude/rules/communication.md`, the AGENTS.md index and project list, `architecture.md` (the second feature, the promised outbox), `database.md` (the second reference shape), `apphost.md` (flag, parameters, Mailpit), `web-pages.md` (`ViewModelComponent`, routes in `Shared`), `web-e2e.md` (Mailpit), `localization.md` (`Cultures`), the `feature-slice` skill (a producer's `Notify`), `lamama.md` (Mitteilungen is this feature) | with each stage |

Later: the delivery log page with a retry button (`ViewDeliveries`), the retention janitor, the digest mode, the chat
webhook sink, mobile push, editable templates, quiet hours, `List-Unsubscribe` if announcements ever go to people
who did not sign up for them.

Stop criteria, visible by the end of stage 1: `FOR UPDATE SKIP LOCKED` through EF needs raw SQL that the precompiler
cannot follow (acceptable, two queries, documented), or `HtmlRenderer` inside an API project fights the Razor SDK (then
string templates).

## Risks

- **The permanent stream per tab** on HTTP/1.1, see In-app. Visible-tab streams are the fix.
- **A host without the feature** keeps the spine's outbox and its producers' `Notify` calls, so rows pile up unread.
  A fork that drops the feature drops the producers' calls with it, and a weekly purge in the spine is a two-line
  hosted service if that ever bothers.
- **`Cultures` moving** touches the host's localization, the BFF's `ui_locales` and the index.html script's language
  table, which keeps its own copy anyway.
- **Web Push E2E is stubbed**, and iOS needs a manifest. Both stated on the settings page and here.
- **The e-mail link needs the app's public URL**, which the AppHost knows (the ingress or the BFF) and a deployment
  configures. A wrong value is a dead button, caught by the E2E assertion on the link.
- **Broadcast is N rows**: fine for hundreds of users, a batched intake for thousands.
- **Two features in one host** for the first time: the no-other-feature test is new, and `gen.mjs`, the solution file
  and the fixtures each learn a second feature folder.

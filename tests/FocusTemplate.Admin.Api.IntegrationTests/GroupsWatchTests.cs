using System.Net;
using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using FocusTemplate.Admin.Api.Features;
using FocusTemplate.Admin.Client;
using FocusTemplate.Admin.Client.UserManagement.Groups;
using FocusTemplate.Admin.Shared.UserManagement;
using FocusTemplate.Data.Entities;
using FocusTemplate.Primitives;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit.Sdk;

namespace FocusTemplate.Admin.Api.IntegrationTests;

public sealed class GroupsWatchTests(ApiFixture factory) : ApiTestBase(factory)
{
	private static readonly TimeSpan Patience = TimeSpan.FromSeconds(10);

	[Fact]
	public async Task ACreatedGroupReachesAnOpenStream()
	{
		User caller = await AdminAsync();
		GroupsClient api = new(Factory.CreateAuthenticatedClient(caller.Id));

		using CancellationTokenSource patience = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		patience.CancelAfter(Patience);

		await using ServerEventReader<GroupChanged> changes = await api.WatchAsync(patience.Token);
		await using IAsyncEnumerator<GroupChanged> next = changes.GetAsyncEnumerator(patience.Token);
		GroupsCreateResult result = await api.CreateAsync(new GroupRequest("Editors"), TestContext.Current.CancellationToken);

		GroupResponse group = result switch
		{
			GroupResponse value => value,
			_ => throw new XunitException($"Expected the group, got {result}"),
		};
		Assert.True(await next.MoveNextAsync());
		Assert.Equal(new GroupChanged(group.Id, GroupChange.Created), next.Current);
	}

	[Fact]
	public async Task ASignalPublishedOnOneReplicaReachesAStreamOnTheOther()
	{
		User caller = await AdminAsync();
		await using WebApplicationFactory<Program> replica = Factory.CreateReplica();

		using CancellationTokenSource patience = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		patience.CancelAfter(Patience);

		await using ServerEventReader<GroupChanged> changes = await GroupsOn(Factory, caller).WatchAsync(patience.Token);
		await using IAsyncEnumerator<GroupChanged> next = changes.GetAsyncEnumerator(patience.Token);
		GroupsCreateResult result = await GroupsOn(replica, caller)
			.CreateAsync(new GroupRequest("Editors"), TestContext.Current.CancellationToken);

		GroupResponse group = result switch
		{
			GroupResponse value => value,
			_ => throw new XunitException($"Expected the group, got {result}"),
		};
		Assert.True(await next.MoveNextAsync());
		Assert.Equal(new GroupChanged(group.Id, GroupChange.Created), next.Current);
	}

	[Fact]
	public async Task AStreamClosesOnOneReplicaWhenItsSubscriberLeavesAGroupOnTheOther()
	{
		User admin = await AdminAsync();
		User subscriber = await Factory.AddUserAsync("Subscriber");
		Group grant = await Factory.GrantAsync(subscriber.Id, UserManagementPermissions.ViewGroups);
		await using WebApplicationFactory<Program> replica = Factory.CreateReplica();

		using CancellationTokenSource patience = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		patience.CancelAfter(Patience);

		await using ServerEventReader<GroupChanged> changes = await GroupsOn(Factory, subscriber).WatchAsync(patience.Token);
		await using IAsyncEnumerator<GroupChanged> next = changes.GetAsyncEnumerator(patience.Token);
		GroupsRemoveMemberResult result = await GroupsOn(replica, admin)
			.RemoveMemberAsync(grant.Id, subscriber.Id, TestContext.Current.CancellationToken);

		// The removal's own signal travels on a Redis channel of its own and may arrive before the close request
		int signals = 0;
		while (await next.MoveNextAsync())
		{
			signals++;
		}

		Assert.True(result is Success, $"Expected Success, got {result}");
		Assert.InRange(signals, 0, 1);
	}

	[Fact]
	public async Task AStreamClosesWhenItsSubscriberLeavesTheGrantingGroup()
	{
		User admin = await AdminAsync();
		User subscriber = await Factory.AddUserAsync("Subscriber");
		Group grant = await Factory.GrantAsync(subscriber.Id, UserManagementPermissions.ViewGroups);

		using CancellationTokenSource patience = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		patience.CancelAfter(Patience);

		await using ServerEventReader<GroupChanged> changes =
			await new GroupsClient(Factory.CreateAuthenticatedClient(subscriber.Id)).WatchAsync(patience.Token);
		await using IAsyncEnumerator<GroupChanged> next = changes.GetAsyncEnumerator(patience.Token);
		GroupsRemoveMemberResult result = await new GroupsClient(Factory.CreateAuthenticatedClient(admin.Id))
			.RemoveMemberAsync(grant.Id, subscriber.Id, TestContext.Current.CancellationToken);

		// The removal's own signal travels on a Redis channel of its own and may arrive before the close request
		int signals = 0;
		while (await next.MoveNextAsync())
		{
			signals++;
		}

		Assert.True(result is Success, $"Expected Success, got {result}");
		Assert.InRange(signals, 0, 1);
	}

	[Fact]
	public async Task AStreamLeftWhileWaitingReleasesItsSourceCleanly()
	{
		using CancellationTokenSource request = new();
		await using IAsyncEnumerator<SseItem<GroupChanged?>> response = ServerEventSupport
			.WithKeepAlive(SlowToEndAsync(request.Token), "groupChanged", request.Token)
			.GetAsyncEnumerator(request.Token);

		Assert.True(await response.MoveNextAsync());
		ValueTask<bool> waiting = response.MoveNextAsync();
		await request.CancelAsync();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await waiting);
	}

	[Fact]
	public async Task WatchingNeedsThePermissionToViewGroups()
	{
		User caller = await Factory.AddUserAsync("Nobody");
		GroupsClient api = new(Factory.CreateAuthenticatedClient(caller.Id));

		HttpRequestException refused =
			await Assert.ThrowsAsync<HttpRequestException>(() => api.WatchAsync(TestContext.Current.CancellationToken));

		Assert.Equal(HttpStatusCode.Forbidden, refused.StatusCode);
	}

	// Ends a while after its token is cancelled, as a subscription's channel read can when the request goes
	private static async IAsyncEnumerable<GroupChanged> SlowToEndAsync([EnumeratorCancellation] CancellationToken cancellationToken)
	{
		try
		{
			await Task.Delay(Timeout.Infinite, cancellationToken);
		}
		catch (OperationCanceledException)
		{
			await Task.Delay(TimeSpan.FromMilliseconds(100), CancellationToken.None);
			throw;
		}

		yield break;
	}

	private static GroupsClient GroupsOn(WebApplicationFactory<Program> host, User user) =>
		new(ApiFixture.Authenticate(host.CreateClient(), user.Id));

	private async Task<User> AdminAsync()
	{
		User caller = await Factory.AddUserAsync("Admin");
		await Factory.GrantAsync(caller.Id, UserManagementPermissions.ViewGroups, UserManagementPermissions.ManageGroups);

		return caller;
	}
}
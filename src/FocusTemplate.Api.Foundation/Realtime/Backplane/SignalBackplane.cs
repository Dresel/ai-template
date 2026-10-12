using System.Text.Json;
using FocusTemplate.Primitives;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace FocusTemplate.Api.Foundation.Realtime.Backplane;

// Carries signals and requests to close streams over Redis Pub/Sub to every API process, the publishing one included.
// Fire and forget keeps a command independent of Redis, and a lost message costs freshness until the next reconnect
public sealed class SignalBackplane(
	IConnectionMultiplexer redis,
	IEnumerable<IEventStreams> streams,
	IOptions<JsonOptions> jsonOptions) : IHostedService
{
	private static readonly RedisChannel CloseRequests = RedisChannel.Literal("streams:close");

	private ChannelMessageQueue? closeRequests;

	public static RedisChannel ChannelOf<TSignal>() => RedisChannel.Literal($"signals:{typeof(TSignal).FullName}");

	// The API's JSON options resolve every wire model through the generated context
	public void Publish<TSignal>(TSignal signal) =>
		redis.GetSubscriber().Publish(
			ChannelOf<TSignal>(),
			JsonSerializer.SerializeToUtf8Bytes(signal, jsonOptions.Value.SerializerOptions.GetTypeInfo<TSignal>()),
			CommandFlags.FireAndForget);

	public void CloseStreamsFor(params IEnumerable<UserId> users)
	{
		string ids = string.Join(',', users.Select(user => user.Value));
		if (ids.Length > 0)
		{
			redis.GetSubscriber().Publish(CloseRequests, ids, CommandFlags.FireAndForget);
		}
	}

	public async Task StartAsync(CancellationToken cancellationToken)
	{
		redis.ConnectionRestored += CloseAllAfterReconnect;

		this.closeRequests = await redis.GetSubscriber().SubscribeAsync(CloseRequests);
		this.closeRequests.OnMessage(message =>
			Close([.. message.Message.ToString().Split(',').Select(id => UserId.From(Guid.Parse(id))),]));
	}

	public async Task StopAsync(CancellationToken cancellationToken)
	{
		redis.ConnectionRestored -= CloseAllAfterReconnect;

		if (this.closeRequests is not null)
		{
			await this.closeRequests.UnsubscribeAsync();
		}
	}

	private void Close(UserId[] users)
	{
		foreach (IEventStreams signalStreams in streams)
		{
			signalStreams.Close(users);
		}
	}

	// The client resubscribes by itself, and what was published meanwhile is lost, so the pages reconnect and reload
	private void CloseAllAfterReconnect(object? sender, ConnectionFailedEventArgs args)
	{
		if (args.ConnectionType == ConnectionType.Subscription)
		{
			foreach (IEventStreams signalStreams in streams)
			{
				signalStreams.CloseAll();
			}
		}
	}
}
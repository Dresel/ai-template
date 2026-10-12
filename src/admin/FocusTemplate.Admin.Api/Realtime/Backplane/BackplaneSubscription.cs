using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace FocusTemplate.Admin.Api.Realtime.Backplane;

// Receives one signal type from every API process and hands each signal to this process's streams of that type
public sealed class BackplaneSubscription<TSignal>(
	IConnectionMultiplexer redis,
	EventStreams<TSignal> streams,
	IOptions<JsonOptions> jsonOptions) : IHostedService
{
	private readonly JsonTypeInfo<TSignal> typeInfo = jsonOptions.Value.SerializerOptions.GetTypeInfo<TSignal>();

	private ChannelMessageQueue? queue;

	// The queue hands its messages over one after the other, so a stream sees the signals in the order they came
	public async Task StartAsync(CancellationToken cancellationToken)
	{
		this.queue = await redis.GetSubscriber().SubscribeAsync(SignalBackplane.ChannelOf<TSignal>());
		this.queue.OnMessage(message =>
			streams.Deliver(JsonSerializer.Deserialize(((ReadOnlyMemory<byte>)message.Message).Span, this.typeInfo)!));
	}

	public async Task StopAsync(CancellationToken cancellationToken)
	{
		if (this.queue is not null)
		{
			await this.queue.UnsubscribeAsync();
		}
	}
}
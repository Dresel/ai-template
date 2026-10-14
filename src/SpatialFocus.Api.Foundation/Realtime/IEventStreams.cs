using SpatialFocus.Primitives;

namespace SpatialFocus.Api.Foundation.Realtime;

// The streams of one signal type as the backplane sees them, so it closes a user's streams across every type
public interface IEventStreams
{
	public void Close(IEnumerable<UserId> users);

	public void CloseAll();
}
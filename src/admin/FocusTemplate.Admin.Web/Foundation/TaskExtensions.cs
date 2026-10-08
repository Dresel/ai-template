namespace FocusTemplate.Admin.Web.Foundation;

internal static class TaskExtensions
{
	extension<T>(Task<T> work)
	{
		// Stops on every exit. For a replaced run the stop throws, which replaces whatever the work ended with, so nothing
		// it fetched gets shown
		public async Task<T> WithBusy(BusyState busy, CancellationToken cancellationToken)
		{
			busy.Start();

			try
			{
				return await work;
			}
			finally
			{
				await busy.StopAsync(cancellationToken);
			}
		}
	}
}
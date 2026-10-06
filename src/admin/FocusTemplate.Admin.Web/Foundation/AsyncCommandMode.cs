namespace FocusTemplate.Admin.Web.Foundation;

internal enum AsyncCommandMode
{
	// A second execution while one runs is dropped, as for a button clicked twice
	IgnoreWhileRunning,

	// A second execution cancels the running one and takes its place, as for a list asked for another page
	ReplaceRunning,
}
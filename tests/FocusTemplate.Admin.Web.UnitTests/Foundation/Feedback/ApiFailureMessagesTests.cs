using FocusTemplate.Admin.Client;
using FocusTemplate.Admin.Shared;
using FocusTemplate.Admin.Web.Foundation.Feedback;

namespace FocusTemplate.Admin.Web.UnitTests.Foundation.Feedback;

public sealed class ApiFailureMessagesTests
{
	private static readonly ApiFailureMessages Messages = new(Localizations.Get<IApiFailureMessagesLocalizations>());

	[Fact]
	public void TheServersOwnWordsComeFirst()
	{
		using UiCulture german = UiCulture.Use("de");

		Assert.Equal("Taken.", Messages.Of(new ApiFailure(409, new ProblemDetails(Detail: "Taken."))));
	}

	[Fact]
	public void WithoutWordsTheMessageIsInTheLanguageOfTheUi()
	{
		using UiCulture german = UiCulture.Use("de");

		Assert.Equal("Der Server antwortete mit 503.", Messages.Of(new ApiFailure(503, null)));
		Assert.Equal("Der Server antwortet gerade nicht.", Messages.Of(new ApiFailure(null, null)));
	}
}
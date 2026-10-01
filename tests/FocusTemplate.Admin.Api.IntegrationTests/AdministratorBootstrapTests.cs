using FocusTemplate.Admin.Api.Authorization;
using FocusTemplate.Data;
using FocusTemplate.Data.Entities;
using FocusTemplate.Primitives;
using Microsoft.EntityFrameworkCore;

namespace FocusTemplate.Admin.Api.IntegrationTests;

// What the one-off job "bootstrap-admin --subject <sub>" runs against the database of a fresh installation
public sealed class AdministratorBootstrapTests(ApiFixture factory) : ApiTestBase(factory)
{
	// The repair when the managed group lost its last active member
	[Fact]
	public async Task ADeactivatedAdministratorIsActiveAgain()
	{
		User user = await Factory.AddUserAsync(isActive: false);
		await Factory.AddAdministratorsAsync(user.Id);

		await AdministratorBootstrap.PromoteAsync(Factory.Services, user.Id, TestContext.Current.CancellationToken);

		Assert.True((await Factory.FindUserAsync(user.Id)).IsActive);
		Assert.Equal([user.Id,], await AdministratorsAsync());
	}

	[Fact]
	public async Task ASubjectNotSeenYetBecomesAnAdministratorBeforeTheirFirstSignIn()
	{
		await Factory.AddAdministratorsAsync();
		UserId subject = UserId.From(Guid.CreateVersion7());

		await AdministratorBootstrap.PromoteAsync(Factory.Services, subject, TestContext.Current.CancellationToken);

		Assert.True((await Factory.FindUserAsync(subject)).IsActive);
		Assert.Equal([subject,], await AdministratorsAsync());
	}

	[Fact]
	public async Task RunningItAgainChangesNothing()
	{
		await Factory.AddAdministratorsAsync();
		UserId subject = UserId.From(Guid.CreateVersion7());

		await AdministratorBootstrap.PromoteAsync(Factory.Services, subject, TestContext.Current.CancellationToken);
		await AdministratorBootstrap.PromoteAsync(Factory.Services, subject, TestContext.Current.CancellationToken);

		Assert.Equal([subject,], await AdministratorsAsync());
	}

	private async Task<List<UserId>> AdministratorsAsync()
	{
		await using AppDbContext dbContext = Factory.CreateDbContext();

		return await dbContext.GroupMembers.Where(member => member.GroupId == WellKnownGroups.Administrators)
			.Select(member => member.UserId)
			.ToListAsync(TestContext.Current.CancellationToken);
	}
}
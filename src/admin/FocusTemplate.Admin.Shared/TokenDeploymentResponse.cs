namespace FocusTemplate.Admin.Shared;

public sealed record TokenDeploymentResponse(
	long ChainId,
	string ContractAddress,
	string DeployerAddress,
	string? FactoryAddress,
	string? LaunchpadName,
	string? TokenName,
	string? TokenSymbol,
	int? TokenDecimals,
	long BlockNumber,
	DateTimeOffset DetectedAt);
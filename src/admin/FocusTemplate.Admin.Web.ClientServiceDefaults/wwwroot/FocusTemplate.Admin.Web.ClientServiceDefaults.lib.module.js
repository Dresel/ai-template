globalThis.blazorClientAnalytics = {
	load: (scriptUrl, websiteId) => {
		const script = document.createElement('script');
		script.defer = true;
		script.src = new URL(scriptUrl, document.baseURI).href;
		script.setAttribute('data-website-id', websiteId);
		document.head.appendChild(script);
	},
};

// Called by dotnet.js when the mono runtime config is loaded.
// Fetches client configuration from the BFF's /_blazor/_configuration endpoint
// and injects the values as environment variables on the MonoConfig.
export async function onRuntimeConfigLoaded(config) {
	try {
		// Resolve relative to <base href> so it works under path prefixes (e.g., /app/_blazor/_configuration)
		const configUrl = new URL('_blazor/_configuration', document.baseURI).href;
		const response = await fetch(configUrl);
		if (response.ok) {
			const serverConfig = await response.json();

			const envVars = serverConfig?.webAssembly?.environment;
			if (envVars && Object.keys(envVars).length > 0) {
				config.environmentVariables ??= {};

				for (const [key, value] of Object.entries(envVars)) {
					config.environmentVariables[key] = value;
				}
			}
		}
	} catch (error) {
		console.warn('Failed to load Aspire client configuration:', error);
	}
}
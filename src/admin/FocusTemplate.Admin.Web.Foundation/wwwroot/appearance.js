// The appearance a user chose, light, dark or system, kept in the browser. A classic script in the page's head, so
// <html data-theme> is set before the first paint, and AppearanceStorage calls window.appearance afterwards.
(() => {
	const key = "appearance";
	const prefersDark = window.matchMedia("(prefers-color-scheme: dark)");
	let watcher = null;

	const read = () => {
		try {
			const stored = localStorage.getItem(key);
			return stored === "light" || stored === "dark" ? stored : "system";
		} catch {
			// Some browsers refuse local storage in a private window
			return "system";
		}
	};

	const resolve = mode => (mode === "system" ? (prefersDark.matches ? "dark" : "light") : mode);

	const apply = mode => {
		document.documentElement.dataset.theme = resolve(mode);
		return document.documentElement.dataset.theme;
	};

	prefersDark.addEventListener("change", () => {
		if (read() === "system") {
			watcher?.invokeMethodAsync("OnSystemChanged", apply("system"));
		}
	});

	window.appearance = {
		mode: read,
		theme: () => resolve(read()),
		choose: mode => {
			try {
				if (mode === "system") {
					localStorage.removeItem(key);
				} else {
					localStorage.setItem(key, mode);
				}
			} catch {
				// Without local storage the choice holds until the next load
			}

			return apply(mode);
		},
		watch: reference => {
			watcher = reference;
		},
	};

	apply(read());
})();
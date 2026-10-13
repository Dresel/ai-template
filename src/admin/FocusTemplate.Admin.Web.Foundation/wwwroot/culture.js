// The language a user chose, kept in the browser. A classic script in the page's head, so the loading screen speaks it
// before .NET runs, and CultureStorage calls window.culture afterwards.
(() => {
	const key = "culture";

	window.culture = {
		read: () => {
			try {
				return localStorage.getItem(key);
			} catch {
				// Some browsers refuse local storage in a private window
				return null;
			}
		},
		choose: name => {
			try {
				localStorage.setItem(key, name);
			} catch {
				// Without local storage the choice is lost with the reload
			}
		},
	};
})();
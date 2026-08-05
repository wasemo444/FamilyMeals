window.linknestTheme = {
    STORAGE_KEY: "linknest-theme",
    DEFAULT_THEME: "dark",

    get: function () {
        try {
            var stored = localStorage.getItem(this.STORAGE_KEY);
            if (stored === "light" || stored === "dark") {
                return stored;
            }
        } catch (e) {
            /* ignore */
        }

        return this.DEFAULT_THEME;
    },

    set: function (theme) {
        if (theme !== "light" && theme !== "dark") {
            return;
        }

        try {
            localStorage.setItem(this.STORAGE_KEY, theme);
        } catch (e) {
            /* ignore */
        }

        document.documentElement.setAttribute("data-theme", theme);
        document.documentElement.style.colorScheme = theme;
    },

    applyStored: function () {
        var theme = this.get();
        document.documentElement.setAttribute("data-theme", theme);
        document.documentElement.style.colorScheme = theme;
    },

    ensureDefault: function () {
        try {
            var stored = localStorage.getItem(this.STORAGE_KEY);
            if (stored !== "light" && stored !== "dark") {
                localStorage.setItem(this.STORAGE_KEY, this.DEFAULT_THEME);
            }
        } catch (e) {
            /* ignore */
        }

        this.applyStored();
    }
};

linknestTheme.ensureDefault();

document.addEventListener("enhancedload", function () {
    linknestTheme.applyStored();
});

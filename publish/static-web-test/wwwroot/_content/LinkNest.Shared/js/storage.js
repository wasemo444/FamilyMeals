export function getItem(key) {
    return localStorage.getItem(key);
}

export function setItem(key, value) {
    localStorage.setItem(key, value);
}

export function getBrowserLanguage() {
    return navigator.language || "en";
}

export function setDocumentCulture(culture, isRightToLeft) {
    document.documentElement.lang = culture;
    document.documentElement.dir = isRightToLeft ? "rtl" : "ltr";
}

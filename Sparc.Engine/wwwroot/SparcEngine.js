import MD5 from "./MD5.js";
//const baseUrl = 'https://localhost:7185';
const baseUrl = 'https://engine.sparc.coop';
export default class SparcEngine {
    static userLang;
    static async hi() {
        return await this.getLanguage();
    }
    static async getLanguages() {
        return await this.fetch('translate/languages');
    }
    static async getLanguage() {
        const response = await this.fetch('user/language');
        if (response && response.id) {
            this.userLang = response.id;
        }
        else {
            return null;
        }
    }
    static async setLanguage(language) {
        await this.fetch('user/language', { Id: language });
        document.documentElement.lang = language;
        this.userLang = language;
        document.dispatchEvent(new CustomEvent('kori-language-changed', { detail: language }));
    }
    static idHash(text, lang = null) {
        if (!lang)
            lang = this.userLang;
        return MD5(text.trim() + ':' + lang);
    }
    static async translate(text, fromLang) {
        const request = {
            id: this.idHash(text, fromLang),
            Domain: window.location.host,
            LanguageId: fromLang,
            Language: { Id: fromLang },
            Text: text
        };
        return await this.fetch('translate', request, this.userLang);
    }
    static async fetch(url, body = null, language = null) {
        const options = {
            credentials: 'include',
            method: body ? 'POST' : 'GET',
            headers: new Headers()
        };
        if (body) {
            options.headers.append('Content-Type', 'application/json');
            options.body = JSON.stringify(body);
        }
        if (language) {
            options.headers.append('Accept-Language', language);
        }
        const response = await fetch(`${baseUrl}/${url}`, options);
        if (response.ok)
            return await response.json();
        else
            throw new Error(`Failed to fetch data from ${url}`);
    }
}
//# sourceMappingURL=SparcEngine.js.map
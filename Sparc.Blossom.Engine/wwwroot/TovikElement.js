import db from './TovikDb.js';
import TovikEngine from './TovikEngine.js';
export default class TovikElement extends HTMLElement {
    observer;
    forceReload = false;
    #observedElement;
    #originalLang;
    constructor() {
        super();
    }
    async connectedCallback() {
        this.#observedElement = this;
        this.#originalLang = this.lang || TovikEngine.documentLang;
        // if the attribute 'for' is set, observe the element with that selector
        if (this.hasAttribute('for')) {
            const selector = this.getAttribute('for');
            this.#observedElement = document.querySelector(selector);
        }
        await this.translatePage(this.#observedElement);
        document.addEventListener('tovik-language-changed', async (event) => {
            await this.translatePage(this.#observedElement, true);
        });
        document.addEventListener('tovik-content-changed', async (event) => {
            await this.translatePage(this.#observedElement);
        });
        this.observer = new MutationObserver(this.#observer);
        this.observer.observe(this.#observedElement, { childList: true, characterData: false, subtree: true });
    }
    disconnectedCallback() {
        if (this.observer)
            this.observer.disconnect();
    }
    async translatePage(element, forceReload = false) {
        if (!TovikEngine.detectedLang)
            TovikEngine.registerVisit();
        // Only translate if the first two characters of originalLang don't match the first two characters of TovikEngine.userLang
        if (this.#originalLang && this.#originalLang.substring(0, 2) === TovikEngine.userLang.substring(0, 2) && !forceReload) {
            return;
        }
        document.documentElement.classList.add('tovik-translating');
        await this.wrapTextNodes(element, forceReload);
        await this.translateAttribute(element, 'placeholder', forceReload);
        document.documentElement.classList.remove('tovik-translating');
    }
    async wrapTextNodes(element, forceReload = false) {
        var nodes = [];
        TovikEngine.sampleText = '';
        var treeWalker = document.createTreeWalker(element, NodeFilter.SHOW_TEXT, this.#tovikIgnoreFilter);
        while (treeWalker.nextNode()) {
            const node = treeWalker.currentNode;
            if (node['originalText'])
                TovikEngine.sampleText += (node['preWhiteSpace'] ? ' ' : '') + node['originalText'] + (node['postWhiteSpace'] ? ' ' : '');
            else
                TovikEngine.sampleText += node.textContent + ' ';
            if (this.shouldTranslate(node, forceReload)) {
                node['translating'] = true;
                nodes.push(node);
            }
        }
        await this.translateTextNodes(nodes);
    }
    shouldTranslate(node, forceReload) {
        return node
            && node.textContent
            && (forceReload || !node.translating)
            && (forceReload || !node.translated)
            && /\p{Letter}/u.test(node.textContent) // Check if the text contains any letter
            && !Date.parse(node.textContent) // Exclude text that can be parsed as a date
            && !(node.parentElement && node.parentElement.tagName === 'TOVIK-T');
    }
    #observer = mutations => {
        document.dispatchEvent(new CustomEvent('tovik-content-changed'));
    };
    #tovikIgnoreFilter = function (node) {
        var approvedNodes = ['#text'];
        if (!approvedNodes.includes(node.nodeName) || node.parentNode.nodeName == 'SCRIPT' || node.parentNode.nodeName == 'STYLE')
            return NodeFilter.FILTER_SKIP;
        var closest = node.parentElement.closest('[translate="no"]');
        if (closest)
            return NodeFilter.FILTER_SKIP;
        return NodeFilter.FILTER_ACCEPT;
    };
    async translateAttribute(element, attributeName, forceReload = false) {
        const elements = element.querySelectorAll('[' + attributeName + ']');
        let pendingTranslations = [];
        for (const el of elements) {
            const original = el['original-' + attributeName] || el.getAttribute(attributeName);
            if (!el['original-' + attributeName]) {
                el['original-' + attributeName] = original;
            }
            const hash = TovikEngine.idHash(original);
            const translation = await db.translations.get(hash);
            if (translation && !forceReload) {
                el.setAttribute(attributeName, translation.text);
            }
            else {
                if (!pendingTranslations.some(e => e.hash === hash)) {
                    pendingTranslations.push({ element: el, hash: hash });
                }
            }
        }
        await TovikEngine.stream(pendingTranslations, x => x['original-' + attributeName], this.#originalLang, (el, translation) => el.setAttribute(attributeName, translation.text));
    }
    async translateTextNodes(textNodes) {
        let pendingTranslations = [];
        await Promise.all(textNodes.map(async (textNode) => {
            if (!textNode.textContent)
                return;
            if (!textNode.originalText) {
                textNode.originalText = textNode.textContent.trim();
                textNode.preWhiteSpace = /^\s/.test(textNode.textContent);
                textNode.postWhiteSpace = /\s$/.test(textNode.textContent);
            }
            textNode.hash = TovikEngine.idHash(textNode.originalText);
            const translation = await db.translations.get(textNode.hash);
            if (translation) {
                textNode.textContent = (textNode.preWhiteSpace ? ' ' : '')
                    + translation.text
                    + (textNode.postWhiteSpace ? ' ' : '');
            }
            else {
                pendingTranslations.push({ element: textNode, hash: textNode.hash });
                //    if (textNode.parentElement)
                //        textNode.parentElement.classList.add('tovik-translating');
            }
        }));
        document.documentElement.classList.remove('tovik-translating');
        if (window.parent && window.parent.postMessage)
            window.parent.postMessage('tovik-translating');
        await TovikEngine.stream(pendingTranslations, node => node.originalText, this.#originalLang, (el, translation) => {
            el.textContent =
                (el.preWhiteSpace ? ' ' : '')
                    + translation.text
                    + (el.postWhiteSpace ? ' ' : '');
            el.translating = false;
            el.translated = true;
            if (window.parent && window.parent.postMessage)
                window.parent.postMessage('tovik-translated');
        });
        if (window.parent && window.parent.postMessage)
            window.parent.postMessage('tovik-translated');
    }
}
//# sourceMappingURL=TovikElement.js.map
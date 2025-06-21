export default class KoriEngine {
    constructor() {
        this.observer = new MutationObserver(this.#observer);
    }

    init(element) {
        this.wrapTextNodes(element || document.body);
        this.observer.observe(element || document.body, { childList: true, characterData: true, subtree: true });
    }

    wrapTextNodes(element) {
        console.log('wrapping text nodes');
        var nodes = [];
        var treeWalker = document.createTreeWalker(element, NodeFilter.SHOW_TEXT, this.#koriIgnoreFilter);
        while (treeWalker.nextNode()) {
            const node = treeWalker.currentNode;
            if (this.isValid(node)) {
                nodes.push(node);
                console.log('oop', node);
            }
        }

        nodes.forEach(node => this.wrapTextNode(node));
    }

    wrapTextNode(node) {
        if (this.isValid(node)) {
            // wrap the text node in a KoriTranslateElement
            const wrapper = document.createElement('kori-translate');
            wrapper.textContent = node.textContent;
            node.parentElement.replaceChild(wrapper, node);
        }
    }

    isValid(node) {
        return node
            && node.textContent
            && node.textContent.trim()
            && !(node.parentElement && node.parentElement.tagName === 'KORI-TRANSLATE');
    }

    #observer = mutations => {
        for (let mutation of mutations) {
            if (mutation.target.classList?.contains('kori-ignore')
                || mutation.target.parentElement?.classList.contains('kori-ignore'))
                return;

            if (mutation.type == 'characterData') {
                console.log('Character data mutation', mutation.target);
            }
            else if (mutation.type == 'childList') {
                console.log('Mutation childList', mutation.target);
            }
            else {
                mutation.addedNodes.forEach(this.wrapTextNode);
            }
        }
    }

    #koriIgnoreFilter = function (node) {
        var approvedNodes = ['#text'];

        if (!approvedNodes.includes(node.nodeName) || node.parentNode.nodeName == 'SCRIPT')
            return NodeFilter.FILTER_SKIP;

        var closest = node.parentElement.closest('.kori-ignore');
        if (closest)
            return NodeFilter.FILTER_SKIP;

        return NodeFilter.FILTER_ACCEPT;
    }
}

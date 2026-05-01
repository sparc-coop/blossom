import TovikEngine from './TovikEngine.js';
export default class KoriElement extends HTMLElement {
    target;
    constructor() {
        super();
    }
    async connectedCallback() {
        document.addEventListener('click', async (event) => {
            if (this.target != event.target) {
                if (this.target)
                    await this.save();
                this.beginEdit(event.target);
                event.stopPropagation();
            }
        });
    }
    disconnectedCallback() {
    }
    beginEdit(element) {
        var textNodes = Array.from(element.childNodes).filter(node => node['nodeType'] === Node.TEXT_NODE);
        if (textNodes.length != 1)
            return;
        this.target = element;
        if (!this.target.originalText)
            this.target.originalText = this.target.innerText;
        this.target.contentEditable = true;
        this.target.focus();
        console.log('Kori edit started on element:', this.target);
    }
    async save() {
        if (!this.target)
            return;
        if (this.target.originalText == this.target.innerText) {
            this.cancel();
            return;
        }
        await TovikEngine.update(this.target);
        this.target.contentEditable = false;
        this.target = null;
        console.log('Kori edit saved');
    }
    cancel() {
        if (!this.target)
            return;
        this.target.innerText = this.target.originalText;
        this.target.contentEditable = false;
        this.target = null;
    }
}
//# sourceMappingURL=KoriElement.js.map
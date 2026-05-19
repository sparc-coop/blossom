import TovikEngine from './TovikEngine.js';
export default class KoriElement extends HTMLElement {
    potentialTarget;
    target;
    verticalBox;
    horizontalBox;
    iframe;
    constructor() {
        super();
    }
    async connectedCallback() {
        // Add 2 boxes to this custom element, to be positioned absolutely on top of the target element as bordered identifiers for the element
        this.verticalBox = document.createElement('div');
        this.verticalBox.classList.add('kori-box', 'kori-box-vertical');
        this.appendChild(this.verticalBox);
        this.horizontalBox = document.createElement('div');
        this.horizontalBox.classList.add('kori-box', 'kori-box-horizontal');
        this.appendChild(this.horizontalBox);
        this.iframe = document.createElement('iframe');
        this.iframe.classList.add('kori-iframe');
        this.iframe.src = "https://localhost:7198/sites/abc123/widget";
        this.appendChild(this.iframe);
        document.addEventListener('mouseover', (event) => {
            if (!this.potentialTarget && this.isEditable(event.target)) {
                this.markTarget(event.target);
                event.stopPropagation();
            }
        });
        document.addEventListener('click', async (event) => {
            if (!this.isEditable(event.target))
                return;
            event.preventDefault();
            if (this.target != event.target) {
                this.beginEdit(event.target);
                event.stopPropagation();
            }
        });
        document.addEventListener('scroll', () => this.positionBoxes());
        window.addEventListener('message', async (event) => {
            if (!event.data)
                return;
            try {
                var data = JSON.parse(event.data);
                if (!data)
                    return;
                switch (data.command) {
                    case 'bold':
                        document.execCommand('bold');
                        event.source.postMessage(JSON.stringify({ type: "method", method: "Bolded" }), event.origin);
                        break;
                    case 'italic':
                        document.execCommand('italic');
                        event.source.postMessage(JSON.stringify({ type: "method", method: "Italicized" }), event.origin);
                        break;
                }
            }
            catch (e) { }
        });
    }
    disconnectedCallback() {
    }
    isEditable(element) {
        var textNodes = Array.from(element.childNodes).filter(node => node['nodeType'] === Node.TEXT_NODE && node['nodeValue'].trim() !== '');
        return textNodes.length == 1;
    }
    markTarget(element) {
        if (!element) {
            this.verticalBox.style.display = 'none';
            this.horizontalBox.style.display = 'none';
            if (this.potentialTarget)
                this.potentialTarget.classList.remove('kori-editable');
            this.potentialTarget = null;
        }
        else {
            this.potentialTarget = element;
            this.positionBoxes();
            this.potentialTarget.classList.add('kori-editable');
            var self = this;
            this.potentialTarget.addEventListener('mouseleave', function onMouseMove(event) {
                console.log('mouseleave', self.potentialTarget, self.target, event.target);
                if (self.potentialTarget == event.target && self.target != event.target)
                    self.markTarget(null);
            });
        }
    }
    positionBoxes() {
        if (!this.potentialTarget)
            return;
        const rect = this.potentialTarget.getBoundingClientRect();
        this.verticalBox.style.left = `${rect.left}px`;
        this.verticalBox.style.width = `${rect.width}px`;
        this.verticalBox.style.display = 'block';
        this.horizontalBox.style.top = `${rect.top}px`;
        this.horizontalBox.style.height = `${rect.height}px`;
        this.horizontalBox.style.display = 'block';
    }
    beginEdit(element) {
        this.markTarget(element);
        this.target = element;
        if (!this.target.originalText)
            this.target.originalText = this.target.innerText;
        this.target.contentEditable = true;
        this.target.focus();
        var el = this.target;
        //this.target.addEventListener('blur', () => this.save(el), { once: true });
    }
    async save(element) {
        if (!element)
            return;
        if (element.originalText != element.innerText)
            await TovikEngine.update(element);
        element.contentEditable = false;
        element.classList.remove('kori-editable');
        if (this.target == element)
            this.target = null;
        if (this.potentialTarget == element)
            this.markTarget(null);
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
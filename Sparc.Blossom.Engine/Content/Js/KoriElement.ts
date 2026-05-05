import TovikEngine from './TovikEngine.js';

export default class KoriElement extends HTMLElement {
    potentialTarget;
    target;
    verticalBox;
    horizontalBox;

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

        document.addEventListener('mouseover', (event: any) => {
            if (!this.potentialTarget && this.isEditable(event.target)) {
                this.markTarget(event.target);
                event.stopPropagation();
            }
        });

        document.addEventListener('click', async (event: any) => {
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
        } else {
            this.potentialTarget = element;
            const rect = this.potentialTarget.getBoundingClientRect();

            this.verticalBox.style.left = `${rect.left}px`;
            this.verticalBox.style.width = `${rect.width}px`;
            this.verticalBox.style.display = 'block';

            this.horizontalBox.style.top = `${rect.top}px`;
            this.horizontalBox.style.height = `${rect.height}px`;
            this.horizontalBox.style.display = 'block';

            this.potentialTarget.classList.add('kori-editable');
            this.potentialTarget.addEventListener('mouseleave', (event: any) => {
                if (this.potentialTarget == event.target) {
                    this.markTarget(null);
                }
            }, { once: true });
        }
    }

    beginEdit(element) {
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

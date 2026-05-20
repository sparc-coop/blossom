export default class BlossomEvent {
    constructor(type: string, name: string, body: any = null) {
        this.type = type;
        this.name = name;
        this.body = body;
    }

    type: string;
    name: string;
    body: any;
}
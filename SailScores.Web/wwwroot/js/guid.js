export class Guid {
    constructor(guid) {
        this.guid = guid;
        this._guid = guid;
    }
    ToString() {
        return this.guid;
    }
    // Static member
    static MakeNew() {
        var result;
        var i;
        var j;
        result = "";
        for (j = 0; j < 32; j++) {
            if (j == 8 || j == 12 || j == 16 || j == 20)
                result = result + '-';
            i = Math.floor(Math.random() * 16).toString(16).toUpperCase();
            result = result + i;
        }
        return new Guid(result);
    }
}
//# sourceMappingURL=guid.js.map
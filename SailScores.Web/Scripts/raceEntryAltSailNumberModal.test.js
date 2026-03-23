import fs from 'fs';
import path from 'path';

describe('Race entry alt sail number modal partial', () => {
    test('contains required modal fields and actions', () => {
        const viewPath = path.resolve(process.cwd(), 'Views/Race/_editAltSailNumberModal.cshtml');
        const content = fs.readFileSync(viewPath, 'utf8');

        expect(content).toContain('id="editAltSailNumberModal"');
        expect(content).toContain('id="altSailNumberCompetitorId"');
        expect(content).toContain('id="altSailNumberCompetitorName"');
        expect(content).toContain('id="altSailNumberSailNumber"');
        expect(content).toContain('id="altSailNumberInput"');
        expect(content).toContain('maxlength="20"');
        expect(content).toContain('id="altSailNumberSaveButton"');
        expect(content).toContain('data-bs-dismiss="modal"');
    });
});

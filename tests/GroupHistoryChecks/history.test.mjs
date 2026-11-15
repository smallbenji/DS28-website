import test from 'node:test';
import assert from 'node:assert/strict';
import { registrationHistoryRows, registrationHistoryTotals } from '../../src/DS.Frontend/src/lib/groupRegistrationHistory.ts';

const pre = (category, participantCount, guestCount = 0) => ({ campYear: 2024, category, participantCount, guestCount });
const signup = (category, dayIndex, participantCount) => ({ campYear: 2024, category, dayIndex, participantCount });

test('comparison uses day 0, preserves guests, and never sums attendance days', () => {
    const rows = registrationHistoryRows({
        presignups: [pre('leader', 10), pre('beaver', 20, 3)],
        signups: [signup('leader', 0, 14), signup('beaver', 0, 15), signup('beaver', 1, 15), signup('beaver', 2, 15), signup('beaver', 4, 0)]
    });
    assert.deepEqual(rows.map(row => row.category), ['beaver', 'leader']);
    assert.equal(rows[0].change, -5);
    assert.equal(rows[1].change, 4);
    assert.equal(rows[0].days[4], 0);
    assert.equal(rows[1].days[1], undefined);
    assert.deepEqual(registrationHistoryTotals(rows), { presignup: 30, guests: 3, signup: 29, change: -1 });
});

test('missing categories on either side stay unknown, while recorded zero stays zero', () => {
    const rows = registrationHistoryRows({
        presignups: [pre('family', 4), pre('junior', 0)],
        signups: [signup('leader', 0, 14), signup('junior', 0, 0)]
    });
    assert.equal(rows.find(row => row.category === 'family').signup, null);
    assert.equal(rows.find(row => row.category === 'leader').presignup, null);
    assert.equal(rows.find(row => row.category === 'family').change, null);
    assert.equal(rows.find(row => row.category === 'junior').change, 0);
    assert.deepEqual(registrationHistoryTotals(rows), { presignup: null, guests: null, signup: null, change: null });
});

test('attendance without day 0 does not invent an end-total', () => {
    const rows = registrationHistoryRows({ presignups: [pre('trop', 25)], signups: [signup('trop', 1, 22)] });
    assert.equal(rows[0].signup, null);
    assert.equal(rows[0].change, null);
    assert.equal(registrationHistoryTotals(rows).signup, null);
});

test('empty history has no rows or invented zero totals', () => {
    assert.deepEqual(registrationHistoryRows({ presignups: [], signups: [] }), []);
    assert.deepEqual(registrationHistoryTotals([]), { presignup: null, guests: null, signup: null, change: null });
});

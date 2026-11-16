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

test('API string counts sum to 49 for the reported group and preserve unknown final counts', () => {
    const categories = ['family', 'beaver', 'wolf', 'junior', 'trop', 'senior', 'rover', 'leader'];
    const counts = ['0', '10', '14', '6', '10', '1', '0', '8'];
    const rows = registrationHistoryRows({
        presignups: categories.map((category, index) => pre(category, counts[index], '0')),
        signups: [signup('wolf', '1', '6'), signup('junior', '1', '4')]
    });
    assert.deepEqual(registrationHistoryTotals(rows), { presignup: 49, guests: 0, signup: null, change: null });
    assert.equal(rows.find(row => row.category === 'wolf').days[1], 6);
});

test('API string day indices identify final counts and distinguish attendance from day zero', () => {
    const rows = registrationHistoryRows({
        presignups: [pre('junior', '6', '2'), pre('rover', '0', '0')],
        signups: [signup('junior', '0', '4'), signup('junior', '1', '3'), signup('rover', '0', '0')]
    });
    assert.equal(rows[0].signup, 4);
    assert.equal(rows[0].change, -2);
    assert.deepEqual(rows[0].days, { 1: 3 });
    assert.equal(rows[1].signup, 0);
    assert.deepEqual(registrationHistoryTotals(rows), { presignup: 6, guests: 2, signup: 4, change: -2 });
});

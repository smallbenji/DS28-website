import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { stripTypeScriptTypes } from 'node:module';
import { test } from 'node:test';

const source = await readFile(new URL('../../src/DS.Frontend/src/lib/org/orgAccess.ts', import.meta.url), 'utf8');
const { canManagePosition, canCreateChild } = await import(`data:text/javascript;base64,${Buffer.from(stripTypeScriptTypes(source)).toString('base64')}`);

function position(id, parentPositionId, holders = [], extra = {}) {
    return {
        id,
        title: `Position ${id}`,
        parentPositionId,
        paForPositionId: null,
        isSystem: false,
        color: null,
        groups: [],
        holders: holders.map((id) => ({ id, firstName: 'Test', lastName: 'User', profilePicture: null })),
        ...extra
    };
}

test('API user summaries grant management to parent and ancestor holders only', () => {
    const root = position(1, null, ['root-holder']);
    const parent = position(2, 1, ['parent-holder']);
    const child = position(3, 2, ['child-holder']);
    const positions = [root, parent, child];

    for (const userId of ['root-holder', 'parent-holder']) {
        assert.equal(canManagePosition(child, positions, userId, false), true);
        assert.equal(canCreateChild(child, positions, userId, false), true);
    }
    assert.equal(canManagePosition(child, positions, 'child-holder', false), false);
    assert.equal(canCreateChild(child, positions, 'child-holder', false), true);
    assert.equal(canManagePosition(child, positions, 'unrelated', false), false);
    assert.equal(canCreateChild(child, positions, 'unrelated', false), false);
    assert.equal(canCreateChild(root, positions, 'root-holder', false), true);
    assert.equal(canCreateChild(root, positions, 'admin', true), true);
});

test('system positions remain restricted to system administrators', () => {
    const root = position(1, null, ['root-holder']);
    const system = position(2, 1, [], { isSystem: true });
    assert.equal(canManagePosition(system, [root, system], 'root-holder', false), false);
    assert.equal(canManagePosition(system, [root, system], 'admin', true), true);
});

test('assistant management follows the assisted position and its ancestors', () => {
    const root = position(1, null, ['root-holder']);
    const assisted = position(2, 1, ['assisted-holder']);
    const assistant = position(3, null, ['assistant-holder'], { paForPositionId: 2 });
    for (const userId of ['root-holder', 'assisted-holder']) {
        assert.equal(canManagePosition(assistant, [root, assisted, assistant], userId, false), true);
    }
    const positions = [root, assisted, assistant];
    assert.equal(canManagePosition(assistant, positions, 'unrelated', false), false);
    assert.equal(canCreateChild(assisted, positions, 'assistant-holder', false), false);
    for (const userId of ['root-holder', 'assisted-holder', 'assistant-holder']) {
        assert.equal(canCreateChild(assistant, positions, userId, false), false);
    }
    assert.equal(canCreateChild(assistant, positions, 'admin', true), false);
});

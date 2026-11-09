import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { createRequire, stripTypeScriptTypes } from 'node:module';
import { pathToFileURL } from 'node:url';
import { test } from 'node:test';

const require = createRequire(new URL('../../src/DS.Frontend/package.json', import.meta.url));
const { parse, compileScript } = require('@vue/compiler-sfc');
const { createRenderer, nextTick } = require('vue');
const filename = new URL('../../src/DS.Frontend/src/Components/User/ProfilePictureUpload.vue', import.meta.url);
const { descriptor } = parse(await readFile(filename, 'utf8'));
const compiled = compileScript(descriptor, { id: 'profile-crop-check' }).content
    .replace("import 'vue-advanced-cropper/dist/style.css';", '')
    .replace(/from (['"])([^'"]+)\1/g, (_, quote, name) => `from '${pathToFileURL(require.resolve(name))}'`);
const { default: component } = await import(`data:text/javascript;base64,${Buffer.from(stripTypeScriptTypes(compiled)).toString('base64')}`);
component.render = () => null;

const renderer = createRenderer({
    createComment: () => ({}),
    insert() {},
    remove() {},
    parentNode: () => null,
    nextSibling: () => null
});

function mount() {
    const files = [];
    const app = renderer.createApp(component, {
        hasPicture: false,
        disabled: false,
        onConfirm: (file) => files.push(file)
    });
    app.mount({});
    return { app, state: app._instance.setupState, files };
}

test('only confirmation uploads the cropped PNG, not the original file', async () => {
    const { app, state, files } = mount();
    try {
        state.selectedFile = new File(['original'], 'original.jpg', { type: 'image/jpeg' });
        await nextTick();
        assert.equal(state.active, true);
        assert.equal(files.length, 0);
        await state.confirm();
        assert.equal(files.length, 0);
        state.ready = true;
        state.cropper = { getResult: () => ({ canvas: { toBlob: (callback) => callback(new Blob(['cropped'])) } }) };
        await state.confirm();
        assert.equal(files.length, 1);
        assert.equal(files[0].type, 'image/png');
        assert.equal(await files[0].text(), 'cropped');
        assert.equal(state.active, false);
    } finally {
        app.unmount();
    }
});

test('conversion errors keep the editor open and do not upload', async () => {
    const { app, state, files } = mount();
    try {
        state.active = true;
        state.ready = true;
        state.cropper = { getResult: () => ({ canvas: { toBlob: (callback) => callback(null) } }) };
        await state.confirm();
        assert.equal(files.length, 0);
        assert.equal(state.active, true);
        assert.ok(state.error);
        assert.equal(state.saving, false);
        state.imageFailed();
        assert.equal(state.ready, false);
    } finally {
        app.unmount();
    }
});

test('cancelling discards the local image without uploading', async () => {
    const { app, state, files } = mount();
    try {
        state.selectedFile = new File(['original'], 'original.jpg', { type: 'image/jpeg' });
        await nextTick();
        const url = state.imageUrl;
        assert.ok(url.startsWith('blob:'));
        state.active = false;
        state.clearImage();
        assert.equal(state.imageUrl, null);
        assert.equal(state.ready, false);
        assert.equal(files.length, 0);
        await assert.rejects(fetch(url));
    } finally {
        app.unmount();
    }
});

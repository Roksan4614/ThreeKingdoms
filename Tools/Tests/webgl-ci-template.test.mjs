import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import vm from 'node:vm';
import assert from 'node:assert/strict';
import { test } from 'node:test';

const template = readFileSync(fileURLToPath(new URL('../../Assets/_Core/Scripts/Editor/WebglCiTemplate/index.html', import.meta.url)), 'utf8').replaceAll('\r', '');
const handler = template.match(/function onResize\(\) \{[\s\S]*?\n        \}/)?.[0];
assert.ok(handler, 'The actual template resize handler must be exercised');

test('resize before Unity initializes is safe, and later resizes reach the ready instance', () => {
    const calls = [];
    const window = { innerHeight: 600 };
    const context = vm.createContext({ window, screen: { height: 720 } });
    vm.runInContext(handler, context);
    assert.doesNotThrow(() => vm.runInContext('onResize()', context));
    window.unityInstance = { SendMessage: (...args) => calls.push(args) };
    vm.runInContext('onResize()', context);
    assert.deepEqual(calls, [['MessageHandler', 'SetFullScreen', 1]]);
    window.innerHeight = 720;
    vm.runInContext('onResize()', context);
    assert.deepEqual(calls.at(-1), ['MessageHandler', 'SetFullScreen', 0]);
});

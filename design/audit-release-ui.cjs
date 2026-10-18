// Local audit only: mock bridge, no real executable launch or profile deletion.
const fs = require('fs');
const path = require('path');
const assert = require('assert/strict');
const { pathToFileURL } = require('url');
const { chromium } = require('playwright');
(async () => {
  const cache = path.join(process.env.LOCALAPPDATA, 'ms-playwright');
  const revision = fs.readdirSync(cache).filter(n => n.startsWith('chromium_headless_shell-')).sort().at(-1);
  const browser = await chromium.launch({ headless: true, executablePath: path.join(cache, revision, 'chrome-headless-shell-win64/chrome-headless-shell.exe') });
  try {
    const page = await browser.newPage();
    const errors = [];
    page.on('pageerror', e => errors.push(e.message));
    await page.addInitScript(() => { window.auditMessages = []; window.chrome = { webview: { postMessage: m => auditMessages.push(m) } }; });
    await page.goto(pathToFileURL(path.resolve('UI/index.html')).href);
    await page.evaluate(() => {
      // Entity text is valid in a Windows filename; it becomes a JS quote in an HTML attribute.
      const basename = 'audit&apos;);window.__auditInjection=true;void(&apos;';
      onProcessesLoaded([{ pid: 99999, name: basename + '.exe', title: 'Audit fixture', exePath: 'C:\\Audit\\' + basename + '.exe', memMb: 1, hasWindow: true }]);
    });
    assert.equal(await page.locator('.isolate-proc-btn').count(), 1);
    // Click only within the simulated page, where the bridge records inert JSON messages.
    await page.locator('.isolate-proc-btn').evaluate(button => button.click());
    const result = await page.evaluate(() => ({ injectionConfirmed: window.__auditInjection === true, nativeActions: auditMessages.map(m => m.action) }));
    assert.equal(result.injectionConfirmed, false, 'Injection must not be executed');
    fs.writeFileSync('design/release-ui-audit.json', JSON.stringify({ ...result, errors, method: 'HTML-entity filename, actual Tasks renderer, simulated native bridge; harmless boolean marker only.' }, null, 2));
    console.log(JSON.stringify(result));
  } finally { await browser.close(); }
})().catch(e => { console.error(e); process.exitCode = 1; });

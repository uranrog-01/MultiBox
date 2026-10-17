const fs = require('fs');
const path = require('path');
const assert = require('assert/strict');
const { pathToFileURL } = require('url');
const { chromium } = require('C:/Users/zer0ne_LP_Local/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
(async () => {
  const cache = path.join(process.env.LOCALAPPDATA, 'ms-playwright');
  const revision = fs.readdirSync(cache).filter(name => name.startsWith('chromium_headless_shell-')).sort().at(-1);
  const browser = await chromium.launch({ headless: true, executablePath: path.join(cache, revision, 'chrome-headless-shell-win64', 'chrome-headless-shell.exe') });
  try {
    const page = await browser.newPage({ viewport: { width: 620, height: 960 } });
    const errors = [];
    page.on('pageerror', error => errors.push(error.message));
    await page.addInitScript(() => { window.nativeMessages = []; window.chrome = window.chrome || {}; window.chrome.webview = { postMessage: m => nativeMessages.push(m) }; });
    await page.goto(pathToFileURL(path.resolve('UI/index.html')).href);
    await page.waitForFunction(() => !!window.MultiBoxSessions);
    const sessions = [
      { id: 'chrome-one', appName: 'Chrome.exe', appPath: 'C:\\Apps\\Chrome.exe', instance: 1, state: 'running', isBusy: false, pid: 2100, ramMb: 186, cpuPercent: 2.1, elapsed: '00:04:32', vaultPath: 'C:\\MultiBox\\Vaults\\a', hasSnapshot: true, fromSavedProfile: false },
      { id: 'chrome-two', appName: 'Chrome.exe', appPath: 'C:\\Apps\\Chrome.exe', instance: 2, state: 'running', isBusy: false, pid: 2200, ramMb: 112, cpuPercent: .8, elapsed: '00:01:15', vaultPath: 'C:\\MultiBox\\Vaults\\b', hasSnapshot: true, fromSavedProfile: true },
      { id: 'discord-one', appName: 'Discord.exe', appPath: 'C:\\Apps\\Discord.exe', instance: 1, state: 'preserved', isBusy: false, pid: 0, ramMb: 0, cpuPercent: 0, elapsed: '00:02:10', vaultPath: 'C:\\MultiBox\\Vaults\\c', hasSnapshot: false, fromSavedProfile: false }
    ];
    let rev = 1;
    const update = async (selectedId, list = sessions) => page.evaluate(snapshot => onSessionsChanged(snapshot), { revision: rev++, selectedId, sessions: list });
    const messages = action => page.evaluate(action => nativeMessages.filter(m => m.action === action), action);
    await update('chrome-one');
    await page.waitForTimeout(500);
    assert.equal(await page.locator('.session-tab').count(), 3);
    assert.equal(await page.locator('#appTitle').textContent(), 'Chrome #1');
    assert.equal(await page.locator('#hudModeText').textContent(), 'FRESH');
    await page.screenshot({ path: 'design/sessions-main.png' });
    await page.locator('#powerBtnContainer').click();
    await page.locator('.session-tab[data-session-id="chrome-two"]').click();
    assert.equal((await messages('selectSession')).at(-1).sessionId, 'chrome-two');
    await update('chrome-two');
    assert.equal(await page.locator('#ramText').textContent(), '112 MB');
    assert.equal(await page.locator('#hudModeText').textContent(), 'SAVED COPY');
    // The first click belonged to another session and must not complete a wipe.
    await page.locator('#powerBtnContainer').click();
    assert.equal((await messages('terminateAndWipe')).length, 0);
    await page.waitForTimeout(350);
    await page.locator('#powerBtnContainer').dblclick({ delay: 80 });
    assert.equal((await messages('terminateAndWipe')).at(-1).sessionId, 'chrome-two');
    await update('chrome-one', sessions.map(s => s.id === 'chrome-two' ? { ...s, state: 'wiping', isBusy: true } : s));
    assert.equal(await page.locator('#appTitle').textContent(), 'Chrome #1');
    assert.equal(await page.locator('#powerLabel').textContent(), 'Session active');
    await update('chrome-one', sessions.filter(s => s.id !== 'chrome-two'));
    await page.locator('#duplicateSessionButton').click();
    assert.equal((await messages('duplicateSession')).at(-1).sessionId, 'chrome-one');
    await page.locator('.save-btn-pill').click();
    await page.locator('#profileNameInput').fill('Work');
    // Saving keeps the session captured when the dialog opened.
    await update('chrome-two');
    await page.locator('#submitSaveProfile').click();
    assert.equal((await messages('saveToSaveBox')).at(-1).sessionId, 'chrome-one');
    assert.equal((await messages('saveToSaveBox')).at(-1).profileName, 'Work');
    await page.evaluate(() => onNamedProfileSaved({ ...nativeMessages.filter(m => m.action === 'saveToSaveBox').at(-1), ok: true }));
    await update('chrome-one');
    await page.evaluate(() => hideNotification());
    assert.equal(await page.locator('#newSessionButton').count(), 0);
    // Out-of-order telemetry must not restore an old selection.
    await page.evaluate(snapshot => onSessionsChanged(snapshot), { revision: 0, selectedId: 'chrome-two', sessions });
    assert.equal(await page.locator('#appTitle').textContent(), 'Chrome #1');
    await update('discord-one');
    assert.equal(await page.locator('#powerLabel').textContent(), 'Reopen');
    await page.locator('#powerBtnContainer').click();
    assert.equal((await messages('reopenApp')).at(-1).sessionId, 'discord-one');
    await page.locator('#navVault').click();
    await page.waitForTimeout(490);
    assert.equal(await page.locator('.vault-session-row').count(), 3);
    await page.evaluate(() => { document.activeElement?.blur(); hideNotification(); });
    await page.screenshot({ path: 'design/sessions-vault.png' });
    await page.locator('#vaultSavedTab').click();
    await page.evaluate(() => onSaveBoxLoaded([{ profileId: 'chrome-work', profileName: 'Work', appName: 'Chrome.exe', appPath: 'C:\\Apps\\Chrome.exe', sizeMb: 42.8, savedAt: 'Oct 16, 2026' }]));
    await page.locator('.saved-profile .primary').click();
    assert.equal((await messages('launchSaveBoxSnapshot')).at(-1).profileId, 'chrome-work');
    assert.equal((await messages('terminateAndWipe')).length, 1, 'Saved launch must not wipe any existing session');
    await page.locator('#navVault').click();
    await page.waitForTimeout(460);
    await page.locator('#vaultCurrentTab').click();
    await page.locator('#vaultAdvancedDetails > summary').click();
    await page.locator('#endAllSessionsButton').click();
    const newer = { ...sessions[0], id: 'newer-session', instance: 3 };
    await update('newer-session', [...sessions, newer]);
    await page.locator('#confirmEndAllSessions').click();
    assert.deepEqual((await messages('endAllSessions')).at(-1).sessionIds, ['chrome-one', 'chrome-two', 'discord-one']);
    const layouts = [];
    for (const size of [{ width: 620, height: 960 }, { width: 470, height: 770 }, { width: 400, height: 600 }]) {
      await page.setViewportSize(size);
      await update('chrome-one');
      await page.waitForTimeout(120);
      const bounds = await page.evaluate(() => {
        const nav = document.querySelector('.nav-bar').getBoundingClientRect();
        const actions = document.querySelector('.session-actions').getBoundingClientRect();
        return { navBottom: nav.bottom, actionsRight: actions.right, width: innerWidth, height: innerHeight, stageHeight: dropStage.clientHeight };
      });
      assert(bounds.navBottom <= size.height + 1 && bounds.actionsRight < size.width, 'Multi-session controls clipped: ' + JSON.stringify(bounds));
      layouts.push(bounds);
    }
    await page.setViewportSize({ width: 620, height: 960 });
    await update('discord-one');
    await page.locator('#navVault').click();
    await page.waitForTimeout(470);
    await page.locator('#vaultWipeButton').click();
    assert(await page.locator('#removeSessionDialog').isVisible());
    assert.equal(await page.locator('#removeSessionName').textContent(), 'Discord #1');
    await update('chrome-one');
    await page.locator('#confirmRemoveSession').click();
    assert.equal((await messages('terminateAndWipe')).at(-1).sessionId, 'discord-one', 'Remove confirmation followed a changed selection');
    await update('chrome-two');
    await page.waitForTimeout(250);
    await page.screenshot({ path: 'design/sessions-selected.png' });
    assert.deepEqual(errors, []);
    fs.writeFileSync('design/session-ui-verification.json', JSON.stringify({ errors, layouts, checks: 'Session switching, per-instance telemetry, click identity, targeted wipe/save/duplicate/reopen, fresh vs saved state, stale update rejection, non-destructive saved launches and captured bulk IDs passed.' }, null, 2));
    await page.setViewportSize({ width: 1320, height: 790 });
    await page.goto(pathToFileURL(path.resolve('design/sessions-preview.html')).href);
    await page.screenshot({ path: 'design/sessions-preview.png', fullPage: true });
    console.log('Multi-session UI and identity checks passed.');
  } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });

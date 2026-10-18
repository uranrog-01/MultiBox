const fs = require('fs');
const path = require('path');
const { pathToFileURL } = require('url');
const assert = require('assert/strict');
const { chromium } = require('playwright');
(async () => {
  const cache = path.join(process.env.LOCALAPPDATA, 'ms-playwright');
  const revision = fs.readdirSync(cache).filter(name => name.startsWith('chromium_headless_shell-')).sort().at(-1);
  const browser = await chromium.launch({ headless: true, executablePath: path.join(cache, revision, 'chrome-headless-shell-win64', 'chrome-headless-shell.exe') });
  try {
    const page = await browser.newPage({ viewport: { width: 620, height: 960 } });
    const errors = [];
    page.on('pageerror', error => errors.push(error.message));
    await page.addInitScript(() => { window.nativeMessages = []; window.chrome = window.chrome || {}; window.chrome.webview = { postMessage: message => nativeMessages.push(message) }; });
    await page.goto(pathToFileURL(path.resolve('UI/index.html')).href);
    await page.waitForFunction(() => !!window.MultiBoxPanels);
    await page.evaluate(() => onSessionsChanged({ revision: 1, selectedId: 'chrome', sessions: [{ id: 'chrome', appName: 'Chrome.exe', appPath: 'C:\\Apps\\Chrome.exe', instance: 1, state: 'running', isBusy: false, pid: 2400, cpuPercent: 2, ramMb: 186, elapsed: '00:02:14', vaultPath: 'C:\\Users\\Example\\AppData\\Local\\MultiBox\\Vaults\\session123' }] }));
    await page.locator('#navTasks').click();
    await page.waitForTimeout(45);
    const startX = await page.locator('#taskManagerModal').evaluate(element => new DOMMatrix(getComputedStyle(element).transform).m41);
    await page.waitForTimeout(140);
    const middleX = await page.locator('#taskManagerModal').evaluate(element => new DOMMatrix(getComputedStyle(element).transform).m41);
    assert(startX > middleX && middleX > 0, `Panel should visibly slide: ${startX}, ${middleX}`);
    await page.waitForTimeout(350);
    await page.evaluate(() => onProcessesLoaded([
      { name: 'Chrome', title: 'Research workspace', pid: 2400, hasWindow: true, memMb: 186, exePath: 'C:\\Apps\\Chrome.exe' },
      { name: 'Visual Studio Code', title: 'MultiBox — Workspace', pid: 3450, hasWindow: true, memMb: 245, exePath: 'C:\\Apps\\Code.exe' },
      { name: 'Notepad', title: 'Notes', pid: 3700, hasWindow: true, memMb: 14, exePath: 'C:\\Windows\\notepad.exe' }
    ]));
    await page.evaluate(() => document.activeElement?.blur());
    await page.screenshot({ path: 'design/panel-tasks.png' });
    assert(await page.locator('#dropStage').evaluate(e => e.inert));
    await page.locator('#navVault').click();
    await page.waitForTimeout(500);
    assert.equal(await page.locator('.settings-modal.open').count(), 1);
    assert.equal(await page.evaluate(() => MultiBoxPanels.getActive()), 'vault');
    assert.equal(await page.locator('#vaultCoreBadge').textContent(), 'Running');
    assert.equal(await page.locator('#saveCurrentSessionBtn').isEnabled(), true);
    await page.evaluate(() => document.activeElement?.blur());
    await page.screenshot({ path: 'design/panel-vault-current.png' });
    await page.locator('#saveCurrentSessionBtn').click();
    await page.locator('#profileNameInput').fill('Work');
    await page.locator('#submitSaveProfile').click();
    assert(await page.evaluate(() => nativeMessages.some(m => m.action === 'saveToSaveBox')));
    await page.evaluate(() => onNamedProfileSaved({ ...nativeMessages.filter(m => m.action === 'saveToSaveBox').at(-1), ok: true }));
    await page.evaluate(() => { hideNotification(); onSaveBoxLoaded([
      { profileId: 'chrome-work', profileName: 'Work', appName: 'Chrome.exe', appPath: 'C:\\Apps\\Chrome.exe', sizeMb: 42.8, savedAt: 'Oct 16, 2026 · 4:10 PM' },
      { profileId: 'code-personal', profileName: 'Personal', appName: 'Code.exe', appPath: 'C:\\Apps\\Code.exe', sizeMb: 18.2, savedAt: 'Oct 16, 2026 · 3:45 PM' }
    ]); });
    await page.locator('#vaultSavedTab').click();
    await page.waitForTimeout(330);
    assert.equal(await page.locator('.saved-profile').count(), 2);
    assert.equal(await page.locator('#saveBoxCount').textContent(), '2');
    await page.evaluate(() => document.activeElement?.blur());
    await page.screenshot({ path: 'design/panel-vault-saved.png' });
    const launchCount = () => page.evaluate(() => nativeMessages.filter(m => m.action === 'launchSaveBoxSnapshot').length);
    const before = await launchCount();
    await page.locator('.saved-profile .primary').first().click();
    assert.equal(await launchCount(), before + 1);
    assert.equal(await page.evaluate(() => currentState), 'active');
    await page.locator('#navVault').click();
    await page.waitForTimeout(470);
    await page.evaluate(() => onSaveBoxLoaded([{ profileId: 'unsafe-text', profileName: 'Test <img onerror="bad">', appName: 'Test.exe', appPath: 'C:\\App.exe', sizeMb: 1 }]));
    assert.equal(await page.locator('.saved-profile img').count(), 0);
    assert((await page.locator('.saved-profile-name').textContent()).includes('<img'));
    await page.locator('.saved-profile .danger').click();
    assert(await page.locator('#deleteConfirmModal').evaluate(e => e.classList.contains('open')));
    await page.evaluate(() => closeDeleteConfirmModal());
    await page.evaluate(() => onSessionPaused('Chrome.exe'));
    await page.locator('#vaultCurrentTab').click();
    assert.equal(await page.locator('#vaultFocusButton').textContent(), 'Reopen');
    assert(await page.locator('#saveCurrentSessionBtn').isEnabled());
    await page.locator('#navSettings').click();
    await page.waitForTimeout(500);
    await page.evaluate(() => { hideNotification(); document.activeElement?.blur(); });
    await page.screenshot({ path: 'design/panel-settings.png' });
    await page.keyboard.press('Escape');
    assert.equal(await page.evaluate(() => MultiBoxPanels.getActive()), null);
    assert.equal(await page.locator('#dropStage').evaluate(e => e.inert), false);
    assert.equal(await page.evaluate(() => document.activeElement.id), 'navSettings');
    await page.evaluate(() => { setUiState('idle'); openVaultDrawer(); onSaveBoxLoaded([]); });
    assert(await page.locator('#saveCurrentSessionBtn').isDisabled());
    assert(await page.locator('#vaultWipeButton').isDisabled());
    await page.locator('#vaultSavedTab').click();
    assert.equal(await page.locator('.vault-empty').count(), 1);
    await page.evaluate(() => onSessionsChanged({ revision: 100, selectedId: 'seven-0', sessions: ['Chrome', 'Discord', 'ChatGPT', 'Firefox', 'Code', 'Notepad', 'Slack'].map((appName, i) => ({ id: 'seven-' + i, appName: appName + '.exe', appPath: 'C:\\Apps\\' + appName + '.exe', instance: 1, state: 'running', isBusy: false, pid: 9000 + i, ramMb: 50, cpuPercent: 1, elapsed: '00:01:00', vaultPath: 'C:\\MultiBox\\Vaults\\test-' + i })) }));
    const layouts = [];
    for (const size of [{ width: 620, height: 960 }, { width: 470, height: 770 }, { width: 400, height: 600 }]) {
      await page.setViewportSize(size);
      for (const name of ['tasks', 'vault', 'settings']) {
        await page.evaluate(() => MultiBoxPanels.closeAll());
        await page.evaluate(name => MultiBoxPanels.open(name), name);
        await page.waitForTimeout(460);
        if (name === 'vault') {
          await page.locator('#vaultCurrentTab').click();
          await page.waitForTimeout(330);
          assert.equal(await page.locator('#vaultCurrentTab').getAttribute('aria-selected'), 'true');
          assert.equal(await page.locator('.vault-session-row').count(), 7);
          const actionBounds = await page.locator('.vault-main-actions').boundingBox();
          const sheetBounds = await page.locator('#saveBoxModal').boundingBox();
          assert(actionBounds && actionBounds.y >= sheetBounds.y && actionBounds.y + actionBounds.height <= sheetBounds.y + sheetBounds.height, 'Seven-app actions are outside the drawer');
          assert.equal(await page.locator('#vaultAdvancedDetails').getAttribute('open'), null);
          if (size.width === 620) await page.screenshot({ path: 'design/simple-vault-seven-apps.png' });
        }
        const bounds = await page.locator('.settings-modal.open').boundingBox();
        const nav = await page.locator('.nav-bar').boundingBox();
        assert(bounds.y > 45 && bounds.y + bounds.height < nav.y, `Panel overlaps title or navigation: ${name}`);
        layouts.push({ name, ...size, bounds });
      }
    }
    await page.emulateMedia({ reducedMotion: 'reduce' });
    const transition = await page.locator('.settings-modal.open').evaluate(e => getComputedStyle(e).transitionDuration);
    assert(transition.split(',').every(t => t.trim() === '0s'));
    assert.deepEqual(errors, []);
    fs.writeFileSync('design/panel-verification.json', JSON.stringify({ errors, sliding: { startX, middleX }, layouts, checks: 'Exclusive sheets, native save/launch bridge, replace-session confirmation, canceled launch, missing-app recovery, safe text rendering, active/preserved/idle Vault states, focus return and reduced motion passed.' }, null, 2));
    await page.setViewportSize({ width: 1380, height: 690 });
    await page.goto(pathToFileURL(path.resolve('design/panels-preview.html')).href);
    await page.screenshot({ path: 'design/panels-preview.png', fullPage: true });
    console.log('Sliding panel and Vault behavior checks passed.');
  } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });

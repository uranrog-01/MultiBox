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
    const page = await browser.newPage({ viewport: { width: 620, height: 960 } });
    const errors = [];
    page.on('pageerror', e => errors.push(e.message));
    await page.addInitScript(() => { window.nativeMessages = []; window.chrome = { webview: { postMessage: m => nativeMessages.push(m) } }; });
    await page.goto(pathToFileURL(path.resolve('UI/index.html')).href);
    const base = { id: 'chrome', appName: 'Chrome.exe', appPath: 'C:\\Apps\\Chrome.exe', instance: 1, state: 'preserved', isBusy: false, ramMb: 0, cpuPercent: 0, elapsed: '00:04:12', vaultPath: 'C:\\MultiBox\\Vaults\\example' };
    const observed = { status: 'observed', label: 'Profile activity confirmed', detail: 'New file activity was observed in the requested profile folder. This checks profile storage, not all filesystem or network access.', method: 'Chromium/Electron profile argument', profilePath: base.vaultPath };
    await page.evaluate(({ base, observed }) => {
      onSessionsChanged({ revision: 1, selectedId: base.id, sessions: [{ ...base, profileCheck: observed }] });
      onSaveBoxLoaded([
        { ProfileId: 'work', ProfileName: 'Work', AppName: base.appName, AppPath: base.appPath, SizeMb: 42.8, SavedAt: 'Oct 16, 2026' },
        { ProfileId: 'personal', ProfileName: 'Personal', AppName: base.appName, AppPath: base.appPath, SizeMb: 18.2, SavedAt: 'Oct 16, 2026' }
      ]);
    }, { base, observed });
    assert.equal(await page.locator('.pro-badge').count(), 0);
    assert.equal(await page.locator('#profileCheckLabel').textContent(), 'Profile activity confirmed');
    await page.locator('#navVault').click(); await page.waitForTimeout(470);
    await page.locator('#vaultAdvancedDetails > summary').click();
    await page.locator('.profile-evidence summary').click();
    assert.match(await page.locator('#profileCheckDetail').textContent(), /not all filesystem/);
    await page.screenshot({ path: 'design/profiles-check.png' });
    await page.locator('#saveCurrentSessionBtn').click();
    assert.match(await page.locator('#saveProfileDialog .vault-note').textContent(), /kept files/);
    await page.locator('#profileNameInput').fill('work');
    assert(await page.locator('#profileReplaceRow').isVisible());
    assert(await page.locator('#submitSaveProfile').isDisabled());
    await page.locator('#profileReplaceCheck').check();
    await page.screenshot({ path: 'design/profiles-save.png' });
    await page.locator('#submitSaveProfile').click();
    let sent = await page.evaluate(() => nativeMessages.filter(m => m.action === 'saveToSaveBox').at(-1));
    assert.match(await page.locator('#profileSaveStatus').textContent(), /Saving to SaveBox/);
    assert(await page.locator('#cancelSaveProfile').isDisabled());
    await page.keyboard.press('Escape');
    assert(await page.locator('#saveProfileDialog').isVisible(), 'The save result must remain visible until the native save completes');
    await page.locator('#saveProfileForm').evaluate(form => form.requestSubmit());
    assert.equal(await page.evaluate(() => nativeMessages.filter(m => m.action === 'saveToSaveBox').length), 1, 'Repeated submit must not create another save');
    assert.equal(sent.replaceProfileId, 'work');
    assert.equal(sent.sessionId, 'chrome');
    await page.evaluate(() => onNamedProfileSaved({ requestId: 'stale-result', ok: true }));
    assert(await page.locator('#saveProfileDialog').isVisible());
    await page.evaluate(requestId => onNamedProfileSaved({ requestId, ok: false, error: 'A file is locked. Your previous copy was kept.' }), sent.requestId);
    assert.match(await page.locator('#profileSaveError').textContent(), /previous copy was kept/);
    assert(await page.locator('#submitSaveProfile').isEnabled());
    assert(await page.locator('#cancelSaveProfile').isEnabled());
    await page.locator('#profileNameInput').fill('Travel');
    assert.equal(await page.locator('#submitSaveProfile').textContent(), 'Save to SaveBox');
    assert(!(await page.locator('#profileReplaceCheck').isChecked()), 'Changing the name must clear replacement approval');
    await page.locator('#submitSaveProfile').click();
    sent = await page.evaluate(() => nativeMessages.filter(m => m.action === 'saveToSaveBox').at(-1));
    assert.equal(sent.replaceProfileId, null);
    await page.evaluate(requestId => onNamedProfileSaved({ requestId, ok: true }), sent.requestId);
    assert(!(await page.locator('#saveProfileDialog').isVisible()));
    await page.evaluate(base => onSessionsChanged({ revision: 2, selectedId: base.id, sessions: [{ ...base, state: 'running' }] }), base);
    await page.evaluate(() => MultiBoxProfiles.openSave());
    await page.locator('#profileNameInput').fill('Restart copy');
    assert.equal(await page.locator('#submitSaveProfile').textContent(), 'Save & reopen');
    assert.match(await page.locator('#profileSaveStatus').textContent(), /will close.*reopen.*Save your work/);
    await page.locator('#submitSaveProfile').click();
    const restarting = await page.evaluate(() => nativeMessages.filter(m => m.action === 'saveToSaveBox').at(-1));
    assert.match(await page.locator('#profileSaveStatus').textContent(), /Closing this app/);
    await page.evaluate(() => onProfileSaveProgress({ requestId: 'old-request', phase: 'reopening' }));
    assert.match(await page.locator('#profileSaveStatus').textContent(), /Closing this app/);
    for (const [phase, label] of [['saving', /Saving to SaveBox/], ['reopening', /Reopening this app/]]) {
      await page.evaluate(update => onProfileSaveProgress(update), { requestId: restarting.requestId, phase });
      assert.match(await page.locator('#profileSaveStatus').textContent(), label);
    }
    await page.evaluate(requestId => onNamedProfileSaved({ requestId, ok: true }), restarting.requestId);
    await page.locator('#vaultSavedTab').click();
    await page.evaluate(() => MultiBoxPanels.renderSavedProfiles());
    assert.equal(await page.locator('.saved-profile').count(), 2);
    assert.equal(await page.locator('.saved-profile-name').allTextContents().then(a => a.join('/')), 'Work/Personal');
    await page.screenshot({ path: 'design/profiles-library.png' });
    await page.locator('.saved-profile .danger').first().click();
    await page.evaluate(() => confirmDeleteSnapshot());
    assert.equal(await page.evaluate(() => nativeMessages.filter(m => m.action === 'deleteSaveBoxSnapshot').at(-1).profileId), 'work');
    await page.locator('#navSettings').click(); await page.waitForTimeout(470);
    await page.evaluate(() => onRecoveryStatus({ isChecking: true, report: null }));
    assert(await page.locator('#retryCrashCleanup').isDisabled());
    await page.evaluate(() => onRecoveryStatus({ isChecking: false, report: { removed: 2, activeKept: 1, unknownKept: 1, failed: 0 } }));
    assert.match(await page.locator('#recoveryStatus').textContent(), /2 abandoned sessions cleaned/);
    await page.locator('#settingsAdvanced > summary').click();
    await page.locator('#retryCrashCleanup').click();
    assert(await page.evaluate(() => nativeMessages.some(m => m.action === 'runCrashCleanup')));
    for (const mode of ['off', 'label']) {
      await page.locator(`[data-indicator-mode="${mode}"]`).click();
      assert.equal(await page.locator('#indicatorPreview').getAttribute('data-mode'), mode);
      assert.equal(await page.evaluate(() => nativeMessages.filter(m => m.action === 'setIndicatorSettings').at(-1).mode), mode);
    }
    await page.evaluate(() => onIndicatorSettings({ mode: 'label' }));
    assert.equal(await page.locator('[data-indicator-mode="label"]').getAttribute('aria-pressed'), 'true');
    await page.locator('#indicatorMode').scrollIntoViewIfNeeded();
    await page.screenshot({ path: 'design/quiet-indicator-settings.png' });
    await page.screenshot({ path: 'design/profiles-recovery.png' });
    for (const viewport of [{ width: 470, height: 770 }, { width: 400, height: 600 }]) {
      await page.setViewportSize(viewport);
      await page.evaluate(() => { MultiBoxPanels.closeAll(); MultiBoxProfiles.openSave(); });
      const bounds = await page.locator('#saveProfileDialog').boundingBox();
      assert(bounds.x >= 0 && bounds.y >= 0 && bounds.x + bounds.width <= viewport.width && bounds.y + bounds.height <= viewport.height);
      await page.evaluate(() => { document.getElementById('profileSaveError').textContent = 'C:\\Users\\Example\\AppData\\Local\\MultiBox\\Vaults\\' + 'very-long-path-'.repeat(45) + '\\runtimes\\codex-runtime-install-test\\pyhumps-3.8.0.dist-info'; });
      assert(await page.locator('#saveProfileDialog').evaluate(e => e.scrollWidth <= e.clientWidth + 1), 'Long save errors must wrap, without horizontal scrolling');
      await page.locator('#submitSaveProfile').scrollIntoViewIfNeeded();
      if (viewport.width === 400) await page.screenshot({ path: 'design/quiet-save-error.png' });
      await page.locator('#cancelSaveProfile').click();
    }
    await page.evaluate(base => onSessionsChanged({ revision: 3, selectedId: base.id, sessions: [base] }), base);
    await page.locator('.session-tab').hover(); await page.waitForTimeout(350);
    assert(await page.locator('#sessionHint').isVisible());
    assert.match(await page.locator('#sessionHint').textContent(), /temporary files are kept/);
    await page.keyboard.press('Escape');
    assert(await page.locator('#sessionHint').isHidden());
    assert.equal(await page.locator('#profileCheckLabel').textContent(), 'Profile unverified');
    assert.deepEqual(errors, []);
    fs.writeFileSync('design/profile-verification.json', JSON.stringify({ errors, checks: 'Compatibility evidence under Details, named save collision consent, immutable save target, failed replacement recovery, stale callback rejection, named library/delete identities, crash-cleanup status/retry, small auto-hide pill/off settings, session hints, long-error wrapping and responsive dialogs passed.' }, null, 2));
    console.log('Named profiles, compatibility evidence and crash-cleanup UI checks passed.');
  } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });

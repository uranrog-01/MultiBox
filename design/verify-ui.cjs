const fs = require('fs');
const path = require('path');
const { pathToFileURL } = require('url');
const { chromium } = require('C:/Users/zer0ne_LP_Local/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');

(async () => {
  const cache = path.join(process.env.LOCALAPPDATA, 'ms-playwright');
  const revision = fs.readdirSync(cache).filter(name => name.startsWith('chromium_headless_shell-')).sort().at(-1);
  const executablePath = path.join(cache, revision, 'chrome-headless-shell-win64', 'chrome-headless-shell.exe');
  const browser = await chromium.launch({ headless: true, executablePath });
  try {
    const page = await browser.newPage({ viewport: { width: 620, height: 960 }, deviceScaleFactor: 1 });
    const errors = [];
    page.on('pageerror', error => errors.push(error.message));
    await page.addInitScript(() => {
      window.nativeMessages = [];
      window.chrome = window.chrome || {};
      window.chrome.webview = { postMessage: message => window.nativeMessages.push(message) };
    });
    await page.goto(pathToFileURL(path.resolve('UI/index.html')).href);
    await page.waitForFunction(() => !!window.MultiBoxVisuals);
    if (await page.locator('#newSessionButton, #resumeAction, .session-action.primary, .pro-badge').count()) throw new Error('Removed dashboard controls are still present');
    if (await page.locator('.ambient-background').getAttribute('aria-hidden') !== 'true') throw new Error('Decorative background must stay out of the accessibility tree');
    if (await page.locator('.ambient-background').evaluate(e => getComputedStyle(e).pointerEvents) !== 'none') throw new Error('Background intercepts clicks');
    await page.waitForTimeout(550);
    await page.screenshot({ path: 'design/ui-idle.png' });
    const motionBefore = await page.evaluate(() => MultiBoxVisuals.getStats());
    await page.waitForTimeout(250);
    const motionAfter = await page.evaluate(() => MultiBoxVisuals.getStats());
    if (motionAfter.points < 7000 || motionAfter.renderedFrames <= motionBefore.renderedFrames) throw new Error('Dense halo is not animating');
    await page.locator('#powerBtnContainer').click();
    if (!await page.evaluate(() => nativeMessages.some(m => m.action === 'browseApp'))) throw new Error('Browse bridge not called');
    await page.evaluate(() => {
      onAppSelected('Google Chrome', 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe');
      startIsolation(selectedAppPath);
    });
    await page.waitForTimeout(550);
    await page.screenshot({ path: 'design/ui-loading.png' });
    await page.evaluate(() => updateTelemetry({ appName: 'Google Chrome', pid: 8420, ramMb: 186.4, cpuPercent: 2.1, elapsed: '00:04:32', hasSnapshot: false }));
    await page.waitForTimeout(550);
    await page.screenshot({ path: 'design/ui-active.png' });
    for (const [selector, modal] of [['#navTasks', '#taskManagerModal'], ['#navVault', '#vaultDrawerModal'], ['#navSettings', '#settingsModal']]) {
      await page.locator(selector).click();
      if (modal === '#vaultDrawerModal') {
        if (!await page.locator('.settings-modal.open').count()) throw new Error('Vault drawer did not open');
      } else if (!await page.locator(modal).evaluate(e => e.classList.contains('open'))) throw new Error('Drawer did not open: ' + modal);
      await page.keyboard.press('Escape');
      await page.evaluate(() => document.activeElement?.blur());
    }
    await page.evaluate(() => onSessionPaused('Google Chrome'));
    await page.evaluate(() => hideNotification());
    await page.waitForTimeout(550);
    await page.screenshot({ path: 'design/ui-preserved.png' });
    await page.locator('.wipe-btn-pill').click();
    if (!await page.evaluate(() => nativeMessages.some(m => m.action === 'terminateAndWipe'))) throw new Error('Wipe bridge not called');
    await page.evaluate(() => { onSessionTerminated(); hideNotification(); });
    await page.waitForTimeout(550);
    await page.screenshot({ path: 'design/ui-ended.png' });
    const layouts = [];
    for (const size of [{ width: 620, height: 960 }, { width: 560, height: 840 }, { width: 470, height: 770 }, { width: 470, height: 650 }, { width: 400, height: 600 }]) {
      await page.setViewportSize(size);
      for (const state of ['idle', 'loading', 'active', 'preserved']) {
        await page.evaluate(state => setUiState(state), state);
        const layout = await page.evaluate(() => {
          const nav = document.querySelector('.nav-bar').getBoundingClientRect();
          const stage = document.querySelector('.stage').getBoundingClientRect();
          const actions = document.querySelector('.session-actions').getBoundingClientRect();
          return { state: document.body.dataset.state, width: innerWidth, height: innerHeight, navBottom: nav.bottom, stageHeight: stage.height, actionsBottom: actions.bottom, bodyScroll: document.body.scrollHeight };
        });
        layouts.push(layout);
        if (layout.navBottom > size.height + 1 || layout.stageHeight < 180) throw new Error('Clipped layout: ' + JSON.stringify(layout));
      }
    }
    await page.emulateMedia({ reducedMotion: 'reduce' });
    await page.evaluate(() => setUiState('active'));
    const animation = await page.locator('.power-btn').evaluate(e => getComputedStyle(e, '::before').animationName);
    if (animation !== 'none') throw new Error('Reduced motion animation still enabled');
    const backgroundAnimations = await page.locator('.ambient-light, .ambient-arc').evaluateAll(elements => elements.map(e => getComputedStyle(e).animationName));
    if (backgroundAnimations.some(name => name !== 'none')) throw new Error('Reduced motion background is still animating');
    await page.waitForFunction(() => MultiBoxVisuals.getStats().reducedMotion && !MultiBoxVisuals.getStats().animating, null, { polling: 50, timeout: 5000 });
    await page.waitForTimeout(120);
    const frozenBefore = await page.evaluate(() => MultiBoxVisuals.getStats());
    await page.waitForTimeout(200);
    const frozenAfter = await page.evaluate(() => MultiBoxVisuals.getStats());
    if (frozenAfter.animating || frozenAfter.renderedFrames !== frozenBefore.renderedFrames) throw new Error('Canvas did not stop for reduced motion');
    if (errors.length) throw new Error(errors.join('\n'));
    fs.writeFileSync('design/ui-verification.json', JSON.stringify({ errors, layouts, halo: motionAfter, reducedMotion: 'passed', bridge: 'browse, launch, wipe and drawer navigation passed' }, null, 2));
    const published = ['index.html', 'modern.css', 'visuals.js', 'panels.css', 'panels.js', 'profiles.js', 'multibox.svg', 'icon.png', 'sessions.js', 'sessions.css'].every(file => fs.readFileSync(path.join('UI', file)).equals(fs.readFileSync(path.join('publish/UI', file))));
    if (!published) throw new Error('Published UI assets differ from source');
    await page.setViewportSize({ width: 1380, height: 690 });
    await page.goto(pathToFileURL(path.resolve('design/ui-preview.html')).href);
    await page.screenshot({ path: 'design/ui-preview.png', fullPage: true });
    console.log('UI checks passed. State screenshots and layout report saved in design/.');
  } finally {
    await browser.close();
  }
})().catch(error => { console.error(error); process.exitCode = 1; });

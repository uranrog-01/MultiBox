const fs = require('fs');
const path = require('path');
const { chromium } = require('C:/Users/zer0ne_LP_Local/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
(async () => {
  const cache = path.join(process.env.LOCALAPPDATA, 'ms-playwright');
  const revision = fs.readdirSync(cache).filter(name => name.startsWith('chromium_headless_shell-')).sort().at(-1);
  const browser = await chromium.launch({ headless: true, executablePath: path.join(cache, revision, 'chrome-headless-shell-win64', 'chrome-headless-shell.exe') });
  try {
    const page = await browser.newPage({ viewport: { width: 256, height: 256 }, deviceScaleFactor: 1 });
    await page.setContent('<style>body{margin:0;background:transparent}svg{display:block;width:256px;height:256px}</style>' + fs.readFileSync('UI/multibox.svg', 'utf8'));
    await page.screenshot({ path: 'UI/icon.png', omitBackground: true });
    fs.copyFileSync('UI/icon.png', 'icon.png');
    await page.setViewportSize({ width: 660, height: 230 });
    await page.setContent('<style>body{margin:0;padding:30px;background:#10160f;color:#b5c5a8;font:12px Segoe UI;display:flex;align-items:center;justify-content:space-around;height:170px}figure{margin:0;text-align:center}img{display:block;margin:0 auto 14px}</style>' + [16,24,32,48,64,128].map(size => '<figure><img src="data:image/svg+xml;base64,' + Buffer.from(fs.readFileSync('UI/multibox.svg')).toString('base64') + '" width="' + size + '" height="' + size + '"><figcaption>' + size + ' px</figcaption></figure>').join(''));
    await page.screenshot({ path: 'design/icon-preview.png' });
    console.log('Rendered vector icon to PNG and small-size preview.');
  } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });

const fs = require('fs');
let html = fs.readFileSync('UI/index.html', 'utf8');
const archive = '<rect x="3" y="3" width="18" height="5" rx="1.5"/><path d="M5 8v13h14V8M9 12h6"/>';
const tasks = '<rect x="3" y="3" width="18" height="18" rx="4"/><path d="M10 8h7M10 12h7M10 16h7M7 8h.01M7 12h.01M7 16h.01"/>';
const settings = '<path d="M4 6h5m5 0h6M4 12h9m5 0h2M4 18h3m5 0h8"/><circle cx="11.5" cy="6" r="2.5"/><circle cx="15.5" cy="12" r="2.5"/><circle cx="9.5" cy="18" r="2.5"/>';
const folder = '<path d="M3 7h7l2 2h9v11H3ZM3 7V4h7l2 3"/>';
const icon = (content, cls = '') => `<svg ${cls ? 'class="' + cls + '" ' : ''}viewBox="0 0 24 24" aria-hidden="true">${content}</svg>`;
const heading = (name, description, content, id) => `<div class="panel-heading"><div class="panel-heading-icon">${icon(content)}</div><div><div class="panel-kicker">Workspace</div><div class="settings-title" id="${id}">${name}</div><div class="panel-subtitle">${description}</div></div></div>`;

html = html.replace('<link rel="stylesheet" href="modern.css">', '<link rel="stylesheet" href="modern.css">\n  <link rel="stylesheet" href="panels.css">');
html = html.replace(/<svg class="brand-cube"[\s\S]*?<\/svg>/, '<img class="brand-icon" src="multibox.svg" alt="">');
html = html.replace(/(<div class="app-icon-circle" id="appIconCircle">)[\s\S]*?(\n        <\/div>)/, '$1\n          <img class="app-mark" src="multibox.svg" alt="">$2');
for (const [id, content] of [['navTasks', tasks], ['navVault', archive], ['navSettings', settings]]) {
  const start = html.indexOf(`id="${id}"`);
  const svgStart = html.indexOf('<svg', start), svgEnd = html.indexOf('</svg>', svgStart) + 6;
  html = html.slice(0, svgStart) + icon(content, 'nav-icon') + html.slice(svgEnd);
}
html = html.replace('  <!-- Settings Slide-Up Drawer -->', '  <button type="button" class="panel-backdrop" id="panelBackdrop" aria-label="Close panel" tabindex="-1"></button>\n\n  <!-- Settings sliding sheet -->');
html = html.replace('<div class="settings-title">MultiBox Settings</div>', heading('Settings', 'Make MultiBox work your way.', settings, 'settingsHeading'));
html = html.replace('<div class="settings-modal" id="settingsModal">', '<div class="settings-modal" id="settingsModal" aria-labelledby="settingsHeading">');
// Replace the Tasks header while keeping its search and native-backed process list.
const taskStart = html.indexOf('  <div class="settings-modal" id="taskManagerModal">');
const taskBody = html.indexOf('    <div style="display:flex; flex-direction:column;', taskStart);
html = html.slice(0, taskStart) + `<div class="settings-modal" id="taskManagerModal" aria-labelledby="tasksHeading">
    <div class="settings-header">${heading('Tasks', 'Choose a running app to start a separate session.', tasks, 'tasksHeading')}
      <button class="settings-close" id="taskManagerCloseBtn" onclick="closeTaskManager()" type="button" aria-label="Close Tasks">×</button>
    </div>
` + html.slice(taskBody);
// The Vault separates disposable session files from deliberately saved profiles.
const vaultStart = html.indexOf('  <!-- Vault & SaveBox Slide-Up Drawer -->');
const vaultEnd = html.indexOf('  <!-- In-App Delete Snapshot Confirmation Modal', vaultStart);
if (vaultStart < 0 || vaultEnd < 0) throw new Error('Vault edit anchors missing');
html = html.slice(0, vaultStart) + `  <!-- Vault sliding sheet -->
  <div class="settings-modal" id="saveBoxModal" aria-labelledby="vaultHeading">
    <div class="settings-header">${heading('Vault', 'Your temporary session and the profiles you choose to keep.', archive, 'vaultHeading')}
      <button class="settings-close" id="saveBoxCloseBtn" onclick="closeVaultDrawer()" type="button" aria-label="Close Vault">×</button>
    </div>
    <div class="panel-tabs" role="tablist" aria-label="Vault views">
      <button class="panel-tab" id="vaultCurrentTab" role="tab" data-vault-tab="current" aria-selected="true" aria-controls="vaultCurrentPane">Current session</button>
      <button class="panel-tab" id="vaultSavedTab" role="tab" data-vault-tab="saved" aria-selected="false" aria-controls="vaultSavedPane" tabindex="-1">Saved profiles <span class="tab-count" id="saveBoxCount">0</span></button>
    </div>
    <div class="settings-scroll-body">
      <section class="vault-pane" id="vaultCurrentPane" role="tabpanel" aria-labelledby="vaultCurrentTab">
        <div class="vault-guide"><strong>Temporary session ≠ saved profile.</strong><br>Closing the app keeps its session here. <strong>Wipe</strong> removes temporary session files. Saved profiles stay until you delete them.</div>
        <div class="vault-card">
          <div class="vault-card-header"><span class="vault-eyebrow">Current session</span><span id="vaultCoreBadge" class="vault-status">No session</span></div>
          <div id="vaultSessionText" class="vault-app-name">No app open</div>
          <p id="vaultSessionDetail" class="vault-description">Launch an app from Isolate to create a temporary profile.</p>
          <div class="vault-actions">
            <button class="vault-button" id="vaultFocusButton" type="button" onclick="window.chrome_backend?.reopenOrFocusApp()" disabled>Focus app ↗</button>
            <button class="vault-button danger" id="vaultWipeButton" type="button" disabled>End &amp; wipe session</button>
          </div>
          <details class="vault-path"><summary>Storage location</summary><code id="vaultPathText">%LOCALAPPDATA%\\MultiBox\\Vaults</code><button class="vault-button" type="button" style="margin-top:10px" onclick="openVaultExplorer()">${icon(folder)}Open session folders</button></details>
        </div>
        <div class="vault-card">
          <div class="vault-card-header"><span class="vault-eyebrow">Keep a reusable copy</span><span class="vault-status" id="saveBoxLiveBadge">Unavailable</span></div>
          <p class="vault-description" id="saveBoxActiveText">Launch an app before saving its profile.</p>
          <button class="vault-button primary full" id="saveCurrentSessionBtn" type="button" onclick="saveCurrentSession()" style="margin-top:15px" disabled>${icon(archive)}Save profile</button>
          <p class="vault-note"><strong>For a more consistent copy, close the app first.</strong> Its session stays here so you can save it. Saving again replaces this app’s saved copy.</p>
        </div>
      </section>
      <section class="vault-pane" id="vaultSavedPane" role="tabpanel" aria-labelledby="vaultSavedTab" hidden>
        <div class="vault-guide"><strong>Saved profiles survive a session wipe.</strong><br>Launching a saved profile starts a fresh session with that data. Saved data is also reused when the same app starts in a fresh vault.</div>
        <div class="vault-section-row"><h3>Your saved profiles</h3><button class="vault-button" type="button" onclick="refreshSaveBoxList()">Refresh</button></div>
        <div id="saveBoxListContainer"><div class="panel-loading">Loading saved profiles…</div></div>
        <button class="vault-button full" type="button" onclick="openSaveBoxFolder()">${icon(folder)}Open saved profiles folder</button>
        <p class="vault-note">Saved profiles can include sign-in data and app settings. Deleting a saved profile removes that copy; wiping a session does not.</p>
      </section>
    </div>
  </div>
  <dialog class="vault-launch-dialog" id="savedProfileLaunchDialog" aria-labelledby="savedLaunchHeading">
    <h2 id="savedLaunchHeading">Start saved profile?</h2>
    <p>This will close the current session and remove its temporary files, then open <strong id="savedProfileLaunchName"></strong> with its saved data.</p>
    <p>Save the current profile first if you want to keep it. Your existing saved profiles will stay.</p>
    <div class="vault-actions"><button class="vault-button" id="cancelSavedLaunch" type="button" autofocus>Cancel</button><button class="vault-button primary" id="confirmSavedLaunch" type="button">Start saved profile</button></div>
  </dialog>

` + html.slice(vaultEnd);

// Real buttons support keyboard activation and accessible close names.
html = html.replace('<div class="settings-close" id="settingsCloseBtn" onclick="closeSettings()" title="Close Settings">✕</div>', '<button class="settings-close" id="settingsCloseBtn" onclick="closeSettings()" type="button" aria-label="Close Settings">×</button>');
const replaceFunction = (name, body) => {
  const expression = new RegExp('    function ' + name + '\\([^)]*\\) \\{[\\s\\S]*?\\n    \\}');
  if (!expression.test(html)) throw new Error('Missing function ' + name);
  html = html.replace(expression, `    function ${name}() {\n${body}\n    }`);
};
replaceFunction('openVaultDrawer', "      window.MultiBoxPanels?.open('vault');\n      refreshVaultDrawerStatus();\n      refreshSaveBoxDrawerStatus();\n      refreshSaveBoxList();");
replaceFunction('closeVaultDrawer', "      window.MultiBoxPanels?.close('vault');");
replaceFunction('openSettings', "      window.MultiBoxPanels?.open('settings');\n      window.chrome_backend?.getContextMenuStatus();");
replaceFunction('closeSettings', "      window.MultiBoxPanels?.close('settings');");
replaceFunction('openTaskManager', "      window.MultiBoxPanels?.open('tasks');\n      refreshProcessList();");
replaceFunction('closeTaskManager', "      window.MultiBoxPanels?.close('tasks');");
replaceFunction('refreshVaultDrawerStatus', `      const usable = currentState === 'active' || currentState === 'preserved';
      const badge = document.getElementById('vaultCoreBadge');
      badge.dataset.kind = currentState;
      badge.textContent = currentState === 'active' ? 'Running' : currentState === 'preserved' ? 'Kept after close' : currentState === 'loading' ? 'Working…' : 'No session';
      const appName = currentTelemetry?.appName || (selectedAppPath ? selectedAppPath.split('\\\\').pop() : 'App session');
      document.getElementById('vaultSessionText').textContent = usable ? appName : currentState === 'loading' ? 'Preparing your session' : 'No app open';
      document.getElementById('vaultSessionDetail').textContent = currentState === 'active' ? 'The app is using a separate profile. Closing it keeps this session; wiping removes its temporary files.' : currentState === 'preserved' ? 'The app has closed. Its profile files are still here. Reopen, save a copy, or wipe when you are finished.' : currentState === 'loading' ? 'Wait for the current session operation to finish.' : 'Launch an app from Isolate to create a temporary profile.';
      document.getElementById('vaultPathText').textContent = currentTelemetry?.vaultPath || '%LOCALAPPDATA%\\\\MultiBox\\\\Vaults';
      const focus = document.getElementById('vaultFocusButton');
      focus.disabled = !usable;
      focus.textContent = currentState === 'preserved' ? 'Reopen app ↗' : 'Focus app ↗';
      document.getElementById('vaultWipeButton').disabled = !usable;`);
replaceFunction('refreshSaveBoxDrawerStatus', `      const available = currentState === 'active' || currentState === 'preserved';
      const badge = document.getElementById('saveBoxLiveBadge');
      badge.textContent = available ? 'Ready to save' : 'Unavailable';
      badge.dataset.kind = available ? 'active' : 'idle';
      document.getElementById('saveBoxActiveText').textContent = available ? 'Save the current profile to reuse its app settings and sign-in data in a future session.' : 'Launch an app before saving its profile.';
      document.getElementById('saveCurrentSessionBtn').disabled = !available;`);
html = html.replace('  <script src="visuals.js"></script>', '  <script src="panels.js"></script>\n  <script src="visuals.js"></script>');
html = html.replaceAll('Ephemeral Vault Virtualization', 'Separate app profile folders').replaceAll('SaveBox Persistent Snapshots', 'Saved app profiles').replaceAll('Job Object Atomic Tree-Kill', 'Close app process tree');
fs.writeFileSync('UI/index.html', html);
console.log('Sliding panel navigation, Vault views and icon updated.');

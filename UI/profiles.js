/* Named saves capture session identity before the user types or switches apps. */
(() => {
  'use strict';
  const dialog = document.getElementById('saveProfileDialog');
  const input = document.getElementById('profileNameInput');
  const submit = document.getElementById('submitSaveProfile');
  const replace = document.getElementById('profileReplaceCheck');
  const error = document.getElementById('profileSaveError');
  const status = document.getElementById('profileSaveStatus');
  const cancel = document.getElementById('cancelSaveProfile');
  const readyStatus = () => target?.state === 'running' ? 'This app will close, save its profile, then reopen. Save your work in the app first.' : 'Saves this app’s kept files to SaveBox. Other apps stay open.';
  let target = null, pending = null, collisionId = null;
  const field = (profile, name) => profile[name] ?? profile[name[0].toUpperCase() + name.slice(1)];
  function refreshSaveForm() {
    if (!target || pending) return;
    const collision = allSnapshots.find(p => String(field(p, 'appPath')).toLowerCase() === target.appPath.toLowerCase() && String(field(p, 'profileName') || 'Default').toLowerCase() === input.value.trim().toLowerCase());
    const nextId = collision ? field(collision, 'profileId') : null;
    if (nextId !== collisionId) replace.checked = false;
    collisionId = nextId;
    document.getElementById('profileReplaceRow').hidden = !collisionId;
    document.getElementById('profileReplaceText').textContent = `Replace the existing “${input.value.trim()}” copy`;
    submit.textContent = target.state === 'running' ? (collisionId ? 'Replace & reopen' : 'Save & reopen') : (collisionId ? 'Replace saved copy' : 'Save to SaveBox');
    submit.disabled = !input.value.trim() || (!!collisionId && !replace.checked);
  }
  function openSave() {
    const workspace = window.MultiBoxSessions.snapshot();
    const session = workspace.sessions.find(s => s.id === workspace.selectedId);
    if (!session || session.isBusy || !['running', 'preserved'].includes(session.state)) return;
    target = { ...session };
    pending = null; collisionId = null; replace.checked = false;
    cancel.disabled = false; status.textContent = readyStatus();
    input.value = session.profileName || '';
    input.disabled = false; error.textContent = '';
    document.getElementById('savingSessionLabel').textContent = `${MultiBoxSessions.label(session)} · ${session.profileName ? 'Saved copy' : 'Temporary session'}`;
    refreshSaveForm();
    dialog.showModal(); input.focus();
    window.chrome_backend.getSaveBoxSnapshots();
  }
  document.getElementById('saveProfileForm').addEventListener('submit', event => {
    event.preventDefault();
    if (!target || pending) return;
    refreshSaveForm();
    if (submit.disabled) return;
    pending = crypto.randomUUID();
    submit.disabled = true; input.disabled = true; replace.disabled = true;
    cancel.disabled = true;
    submit.textContent = 'Saving…'; error.textContent = '';
    status.textContent = target.state === 'running' ? 'Closing this app… Your session files will be kept.' : 'Saving to SaveBox…';
    window.chrome_backend.saveToSaveBox({ sessionId: target.id, profileName: input.value.trim(), replaceProfileId: collisionId || null, requestId: pending });
  });
  window.onNamedProfileSaved = result => {
    if (result.requestId !== pending) return;
    pending = null; input.disabled = false; replace.disabled = false;
    cancel.disabled = false; status.textContent = readyStatus();
    if (result.ok) { dialog.close(); target = null; }
    else { error.textContent = result.error || 'The profile could not be saved.'; refreshSaveForm(); window.chrome_backend.getSaveBoxSnapshots(); }
  };
  window.onProfileSaveProgress = update => {
    if (!pending || update.requestId !== pending) return;
    const labels = { closing: 'Closing this app… Your session files will be kept.', saving: 'Saving to SaveBox…', reopening: 'Reopening this app…' };
    if (labels[update.phase]) status.textContent = labels[update.phase];
  };
  cancel.addEventListener('click', () => { if (!pending) dialog.close(); });
  dialog.addEventListener('cancel', event => { if (pending) event.preventDefault(); });
  dialog.addEventListener('close', () => {
    if (dialog.open) return; // A queued close event may belong to a dialog already reopened.
    target = null; pending = null; replace.disabled = false; cancel.disabled = false;
  });
  input.addEventListener('input', () => { error.textContent = ''; refreshSaveForm(); });
  replace.addEventListener('change', refreshSaveForm);
  function renderCheck(session) {
    const check = session?.profileCheck;
    const label = document.getElementById('profileCheckLabel');
    label.textContent = check?.label || 'Profile unverified';
    label.dataset.status = check?.status || 'unverified';
    document.getElementById('profileCheckDetail').textContent = check?.detail || (session ? 'No compatibility evidence is available for this app yet.' : 'Launch an app to check its profile activity.');
    const method = document.getElementById('profileCheckMethod');
    method.textContent = check?.method || '';
    method.title = check?.profilePath || '';
  }
  window.onRecoveryStatus = state => {
    const button = document.getElementById('retryCrashCleanup');
    button.disabled = !!state.isChecking;
    const report = state.report;
    document.getElementById('recoveryStatus').textContent = state.isChecking ? 'Checking abandoned sessions…' : !report ? 'Ready to check abandoned sessions.' : `${report.removed} abandoned session${report.removed === 1 ? '' : 's'} cleaned · ${report.activeKept} active kept${report.unknownKept ? ` · ${report.unknownKept} unrecognized kept` : ''}${report.failed ? ` · ${report.failed} could not be cleaned — try again` : ''}`;
  };
  document.getElementById('retryCrashCleanup').addEventListener('click', () => window.chrome?.webview?.postMessage({ action: 'runCrashCleanup' }));
  const descriptions = {
    label: 'Hides after 1.8s',
    off: 'Label hidden'
  };
  const indicatorMode = document.getElementById('indicatorMode');
  function renderIndicator(mode) {
    if (!descriptions[mode]) return;
    indicatorMode.querySelectorAll('[data-indicator-mode]').forEach(button => {
      button.setAttribute('aria-pressed', String(button.dataset.indicatorMode === mode));
    });
    document.getElementById('indicatorDescription').textContent = descriptions[mode];
    document.getElementById('indicatorPreview').dataset.mode = mode;
  }
  indicatorMode.addEventListener('click', event => {
    const button = event.target.closest('[data-indicator-mode]');
    if (!button || !indicatorMode.contains(button)) return;
    const mode = button.dataset.indicatorMode;
    if (!descriptions[mode]) return;
    renderIndicator(mode);
    window.chrome?.webview?.postMessage({ action: 'setIndicatorSettings', mode });
  });
  window.onIndicatorSettings = settings => renderIndicator(settings.mode);
  window.MultiBoxProfiles = { openSave, refreshSaveForm, renderCheck };
  renderCheck(null);
  window.chrome?.webview?.postMessage({ action: 'getRecoveryStatus' });
  window.chrome?.webview?.postMessage({ action: 'getIndicatorSettings' });
})();

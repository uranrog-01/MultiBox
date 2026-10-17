/* Session identity is authoritative. Switching never changes an operation's target. */
(() => {
  'use strict';
  let workspace = { revision: -1, selectedId: null, sessions: [] };
  const tabs = document.getElementById('sessionTabs');
  const vaultList = document.getElementById('vaultSessionList');
  const endAllDialog = document.getElementById('endAllSessionsDialog');
  let pendingWipeIds = [];
  let listSignature = '';
  const colors = { lime: '#C6FA46', blue: '#7CB7FF', violet: '#BCA1FF', rose: '#F5A1BB', amber: '#EFC478', teal: '#79D9CB' };
  const colorOf = session => Object.hasOwn(colors, session.color) ? session.color : 'lime';
  const hint = document.createElement('div');
  hint.id = 'sessionHint'; hint.className = 'session-hint'; hint.role = 'tooltip'; hint.hidden = true;
  document.body.append(hint);
  let hintTimer;
  const hideHint = () => { clearTimeout(hintTimer); hint.hidden = true; };
  function attachHint(element, session) {
    const profile = session.profileName ? `Saved copy · ${session.profileName}` : 'Fresh temporary profile';
    const action = session.state === 'preserved' ? 'The app is closed; its temporary files are kept. Select to reopen, save or wipe.' : 'Select to see its controls. Other apps keep running.';
    element.setAttribute('aria-label', `${name(session)}. ${stateLabel(session)}. ${profile}. ${action}`);
    element.setAttribute('aria-describedby', hint.id);
    function show() {
      if (window.MultiBoxPanels?.getActive() && element.classList.contains('session-tab')) return;
      hint.replaceChildren();
      const title = document.createElement('strong'); title.textContent = name(session);
      const status = document.createElement('span'); status.textContent = `${stateLabel(session)} · ${profile}`;
      const help = document.createElement('small'); help.textContent = action;
      hint.append(title, status, help); hint.hidden = false;
      const bounds = element.getBoundingClientRect();
      hint.style.left = `${Math.max(16, Math.min(bounds.left, innerWidth - hint.offsetWidth - 16))}px`;
      hint.style.top = `${Math.max(12, Math.min(bounds.bottom + 8, innerHeight - hint.offsetHeight - 12))}px`;
    }
    element.addEventListener('pointerenter', () => { clearTimeout(hintTimer); hintTimer = setTimeout(show, 300); });
    element.addEventListener('pointerleave', hideHint);
    element.addEventListener('focus', () => { clearTimeout(hintTimer); show(); });
    element.addEventListener('blur', hideHint);
    element.addEventListener('click', hideHint);
  }
  document.addEventListener('keydown', event => { if (event.key === 'Escape') hideHint(); });
  document.addEventListener('pointerdown', hideHint);
  const appName = session => `${(session.appName || 'App').replace(/\.exe$/i, '')}${session.instance ? ' #' + session.instance : ''}`;
  const name = session => !session ? '' : session.displayName ? `${session.displayName} · ${appName(session)}` : `${appName(session)}${session.profileName ? ' · ' + session.profileName : ''}`;
  const stateLabel = session => session.state === 'running' ? 'Running' : session.state === 'preserved' ? 'Closed' : session.state === 'wiping' ? 'Removing…' : 'Opening…';
  const send = message => window.chrome?.webview?.postMessage(message);
  function select(id) { send({ action: 'selectSession', sessionId: id }); }
  function renderList() {
    const signature = JSON.stringify([workspace.selectedId, workspace.sessions.map(s => [s.id, s.appName, s.instance, s.profileName, s.displayName, s.color, s.state, s.isBusy])]);
    if (signature === listSignature) return;
    listSignature = signature;
    hideHint();
    const focused = document.activeElement?.closest('.session-tab, .vault-session-row');
    const focusedId = focused?.dataset.sessionId;
    const focusedInVault = focused?.classList.contains('vault-session-row');
    const scrollLeft = tabs.scrollLeft;
    tabs.replaceChildren();
    vaultList.replaceChildren();
    document.getElementById('vaultCurrentPane').dataset.empty = String(!workspace.sessions.length);
    if (!workspace.sessions.length) {
      const hint = document.createElement('span');
      hint.className = 'session-empty-hint'; hint.textContent = 'A fresh profile for every app'; tabs.append(hint);
      const empty = document.createElement('div'); empty.className = 'vault-empty';
      const title = document.createElement('h3'); title.textContent = 'No apps open';
      const detail = document.createElement('p'); detail.textContent = 'Open an app to get started.';
      const browse = document.createElement('button'); browse.className = 'vault-button primary'; browse.textContent = 'Choose an app'; browse.type = 'button';
      browse.addEventListener('click', () => { MultiBoxPanels.closeAll(); window.chrome_backend.browseApp(); });
      empty.append(title, detail, browse); vaultList.append(empty);
    }
    for (const session of workspace.sessions) {
      const selected = workspace.selectedId === session.id;
      const tab = document.createElement('button');
      tab.className = 'session-tab'; tab.setAttribute('role', 'tab');
      tab.setAttribute('aria-selected', String(selected));
      tab.tabIndex = selected ? 0 : -1;
      tab.dataset.sessionId = session.id;
      tab.style.setProperty('--session-color', colors[colorOf(session)]);
      tab.title = 'Double-click or press F2 for name & color';
      attachHint(tab, session);
      const dot = document.createElement('span'); dot.className = `session-dot ${session.state}`; dot.setAttribute('aria-hidden', 'true');
      const label = document.createElement('span'); label.textContent = name(session);
      tab.append(dot, label);
      tab.addEventListener('click', () => select(session.id));
      tab.addEventListener('dblclick', () => openIdentity(session.id));
      tab.addEventListener('keydown', event => {
        if (event.key === 'F2') { event.preventDefault(); openIdentity(session.id); return; }
        if (event.key !== 'ArrowLeft' && event.key !== 'ArrowRight') return;
        event.preventDefault();
        const index = workspace.sessions.findIndex(s => s.id === session.id);
        select(workspace.sessions[(index + (event.key === 'ArrowLeft' ? -1 : 1) + workspace.sessions.length) % workspace.sessions.length].id);
      });
      tabs.append(tab);
      const row = document.createElement('button'); row.type = 'button'; row.className = 'vault-session-row';
      row.setAttribute('aria-pressed', String(selected)); row.dataset.sessionId = session.id;
      row.style.setProperty('--session-color', colors[colorOf(session)]);
      attachHint(row, session);
      const rowName = document.createElement('span'); rowName.className = 'session-row-name';
      const sameAppCount = workspace.sessions.filter(s => s.appName.toLowerCase() === session.appName.toLowerCase()).length;
      rowName.textContent = session.displayName || session.appName.replace(/\.exe$/i, '') + (sameAppCount > 1 ? ' #' + session.instance : '');
      const copy = document.createElement('span'); copy.className = 'session-row-copy';
      const profile = document.createElement('small'); profile.textContent = (session.displayName ? appName(session) + ' · ' : '') + (session.profileName ? `Saved · ${session.profileName}` : 'Temporary');
      copy.append(rowName, profile);
      const status = document.createElement('span'); status.className = 'session-row-state'; status.textContent = stateLabel(session);
      row.append(dot.cloneNode(), copy, status);
      row.addEventListener('click', () => select(session.id)); vaultList.append(row);
    }
    document.body.classList.toggle('has-multiple-sessions', workspace.sessions.length > 1);
    document.getElementById('vaultSessionsCount').textContent = workspace.sessions.length;
    document.getElementById('endAllSessionsButton').disabled = !workspace.sessions.length || workspace.sessions.some(s => s.isBusy);
    tabs.scrollLeft = scrollLeft;
    if (focusedId) (focusedInVault ? vaultList : tabs).querySelector(`[data-session-id="${workspace.selectedId}"]`)?.focus({ preventScroll: true });
  }
  function renderSelected(previous) {
    const session = workspace.sessions.find(s => s.id === workspace.selectedId);
    if (previous !== workspace.selectedId) {
      clearTimeout(clickTimeout); clickTimeout = null; reactorClickSessionId = null;
    }
    if (!session) {
      currentTelemetry = null; selectedAppPath = '';
      appTitle.textContent = 'Select Application'; appSub.title = '';
      appTitle.style.removeProperty('--session-color'); appTitle.classList.remove('has-session-color');
      document.getElementById('sessionError').textContent = '';
      setUiState('idle');
    } else {
      selectedAppPath = session.appPath;
      currentTelemetry = { ...session };
      if (session.state === 'running') updateTelemetry(session);
      else {
        setUiState(session.state === 'preserved' ? 'preserved' : 'loading');
        if (session.state !== 'preserved') {
          powerLabel.textContent = session.state === 'wiping' ? 'Wiping…' : 'Opening app…';
          statusText.textContent = `${stateLabel(session)} ${name(session)}`;
        }
        document.getElementById('timerText').textContent = session.elapsed;
      }
      appTitle.textContent = name(session); appTitle.title = session.appPath;
      appTitle.classList.add('has-session-color'); appTitle.style.setProperty('--session-color', colors[colorOf(session)]);
      appSub.title = session.vaultPath;
      document.getElementById('hudModeText').textContent = session.fromSavedProfile ? 'SAVED COPY' : 'FRESH';
      document.getElementById('sessionError').textContent = session.error || '';
    }
    const busy = !session || session.isBusy;
    window.MultiBoxProfiles?.renderCheck(session);
    document.getElementById('powerBtnContainer').setAttribute('aria-disabled', String(!!session?.isBusy));
    document.querySelectorAll('.session-actions button, #vaultWipeButton, #vaultFocusButton, #saveCurrentSessionBtn').forEach(button => { button.disabled = busy; });
    document.querySelectorAll('.save-btn-pill, .wipe-btn-pill').forEach(button => { button.disabled = busy; });
    document.getElementById('duplicateSessionButton').disabled = busy;
    document.getElementById('editSessionButton').disabled = busy;
    if (session) document.getElementById('vaultSessionText').textContent = name(session);
  }
  window.onSessionsChanged = snapshot => {
    if (!snapshot || snapshot.revision < workspace.revision) return;
    const previous = workspace.selectedId;
    workspace = snapshot;
    renderList();
    renderSelected(previous);
    if (previous && !workspace.sessions.length) MultiBoxVisuals.showCompletion();
  };
  document.getElementById('duplicateSessionButton').addEventListener('click', () => {
    const id = workspace.selectedId;
    if (id) send({ action: 'duplicateSession', sessionId: id });
  });
  function requestEndAll() {
    pendingWipeIds = workspace.sessions.map(s => s.id);
    if (!pendingWipeIds.length) return;
    document.getElementById('endAllSessionsCount').textContent = pendingWipeIds.length;
    endAllDialog.showModal();
  }
  document.getElementById('endAllSessionsButton').addEventListener('click', requestEndAll);
  document.getElementById('cancelEndAllSessions').addEventListener('click', () => endAllDialog.close());
  document.getElementById('confirmEndAllSessions').addEventListener('click', () => {
    const ids = pendingWipeIds.slice(); pendingWipeIds = [];
    endAllDialog.close(); MultiBoxPanels.closeAll();
    send({ action: 'endAllSessions', sessionIds: ids });
  });
  endAllDialog.addEventListener('cancel', () => { pendingWipeIds = []; });
  const identityDialog = document.getElementById('sessionIdentityDialog');
  const identityInput = document.getElementById('sessionNameInput');
  const identityError = document.getElementById('sessionIdentityError');
  const picker = document.getElementById('sessionColorPicker');
  const applyIdentity = document.getElementById('applySessionIdentity');
  const cancelIdentity = document.getElementById('cancelSessionIdentity');
  const resetIdentity = document.getElementById('resetSessionIdentity');
  let editingId = null, identityRequest = null, editingColor = 'lime';
  function chooseColor(color) {
    editingColor = Object.hasOwn(colors, color) ? color : 'lime';
    picker.querySelectorAll('button').forEach(button => button.setAttribute('aria-pressed', String(button.dataset.color === editingColor)));
  }
  for (const [color, hex] of Object.entries(colors)) {
    const button = document.createElement('button'); button.type = 'button'; button.dataset.color = color;
    button.className = 'session-color-swatch'; button.style.setProperty('--swatch-color', hex);
    button.setAttribute('aria-label', color[0].toUpperCase() + color.slice(1)); button.title = button.getAttribute('aria-label');
    button.setAttribute('aria-pressed', 'false');
    button.addEventListener('click', () => chooseColor(color)); picker.append(button);
  }
  function identityBusy(busy) {
    [identityInput, applyIdentity, cancelIdentity, resetIdentity, ...picker.querySelectorAll('button')].forEach(control => { control.disabled = busy; });
    applyIdentity.textContent = busy ? 'Applying…' : 'Apply';
  }
  function openIdentity(id) {
    const session = workspace.sessions.find(s => s.id === id);
    if (!session || session.isBusy || identityDialog.open) return;
    hideHint(); editingId = id; identityRequest = null; identityBusy(false);
    identityInput.value = session.displayName || ''; identityInput.placeholder = appName(session);
    document.getElementById('sessionIdentityApp').textContent = appName(session);
    identityError.textContent = ''; chooseColor(colorOf(session));
    identityDialog.showModal(); identityInput.focus(); identityInput.select();
  }
  document.getElementById('editSessionButton').addEventListener('click', () => openIdentity(workspace.selectedId));
  resetIdentity.addEventListener('click', () => { identityInput.value = ''; chooseColor('lime'); identityError.textContent = ''; identityInput.focus(); });
  cancelIdentity.addEventListener('click', () => identityDialog.close());
  identityDialog.addEventListener('cancel', event => { if (identityRequest) event.preventDefault(); });
  identityDialog.addEventListener('close', () => { if (identityDialog.open) return; editingId = null; identityRequest = null; });
  document.getElementById('sessionIdentityForm').addEventListener('submit', event => {
    event.preventDefault();
    if (!editingId || identityRequest) return;
    const displayName = identityInput.value.trim();
    if (displayName.length > 32 || /[\u0000-\u001f\u007f-\u009f]/.test(displayName)) { identityError.textContent = 'Use a name of up to 32 characters.'; return; }
    identityRequest = crypto.randomUUID(); identityError.textContent = ''; identityBusy(true);
    send({ action: 'customizeSession', sessionId: editingId, displayName, color: editingColor, requestId: identityRequest });
  });
  window.onSessionCustomized = result => {
    if (!identityRequest || result.requestId !== identityRequest) return;
    identityRequest = null; identityBusy(false);
    if (result.ok) identityDialog.close();
    else identityError.textContent = result.error || 'Could not update this session.';
  };
  window.MultiBoxSessions = { selectedId: () => workspace.selectedId, ids: () => workspace.sessions.map(s => s.id), select, requestEndAll, snapshot: () => workspace, label: name };
  renderList();
  send({ action: 'getSessions' });
})();

/* Shared slide-sheet navigation, focus handling and the Vault's two views. */
(() => {
  'use strict';
  const sheets = {
    tasks: { panel: document.getElementById('taskManagerModal'), nav: document.getElementById('navTasks') },
    vault: { panel: document.getElementById('saveBoxModal'), nav: document.getElementById('navVault') },
    settings: { panel: document.getElementById('settingsModal'), nav: document.getElementById('navSettings') }
  };
  const backdrop = document.getElementById('panelBackdrop');
  const workspace = document.querySelectorAll('body > .session-switcher, body > .header-bar, body > .metrics-container, body > .stage, body > .session-features, body > .bottom-section');
  let active = null;
  let returnFocus = null;
  function sync() {
    for (const [key, { panel, nav }] of Object.entries(sheets)) {
      const selected = key === active;
      panel.classList.toggle('open', selected);
      panel.inert = !selected;
      panel.setAttribute('aria-hidden', String(!selected));
      nav.classList.toggle('active', selected);
      nav.setAttribute('aria-expanded', String(selected));
    }
    document.getElementById('navIsolate').classList.toggle('active', !active);
    backdrop.classList.toggle('open', !!active);
    backdrop.tabIndex = active ? 0 : -1;
    backdrop.setAttribute('aria-hidden', String(!active));
    workspace.forEach(element => { element.inert = !!active; });
  }
  function open(name) {
    if (!sheets[name]) return;
    if (active === name) { close(name); return; }
    returnFocus = sheets[name].nav;
    active = name;
    sync();
    sheets[name].panel.querySelector('.settings-close').focus({ preventScroll: true });
  }
  function close(name) {
    if (name && active !== name) return;
    active = null;
    sync();
    if (returnFocus) { returnFocus.focus({ preventScroll: true }); returnFocus = null; }
  }
  for (const { panel, nav } of Object.values(sheets)) {
    panel.setAttribute('role', 'dialog');
    panel.setAttribute('aria-modal', 'false');
    nav.setAttribute('aria-controls', panel.id);
  }
  backdrop.addEventListener('click', () => close());
  window.MultiBoxPanels = { open, close, closeAll: () => close(), getActive: () => active, renderSavedProfiles, launchSaved };

  function selectVaultTab(name, focus = false) {
    document.querySelector('.vault-pill-tabs').dataset.selected = name;
    for (const tab of document.querySelectorAll('[data-vault-tab]')) {
      const selected = tab.dataset.vaultTab === name;
      tab.setAttribute('aria-selected', String(selected));
      tab.tabIndex = selected ? 0 : -1;
      document.getElementById(tab.getAttribute('aria-controls')).hidden = !selected;
      if (selected && focus) tab.focus();
    }
  }
  document.querySelectorAll('[data-vault-tab]').forEach(tab => {
    tab.addEventListener('click', () => selectVaultTab(tab.dataset.vaultTab));
    tab.addEventListener('keydown', event => {
      if (['ArrowLeft', 'ArrowRight', 'Home', 'End'].includes(event.key)) {
        event.preventDefault();
        selectVaultTab(event.key === 'Home' ? 'current' : event.key === 'End' ? 'saved' : tab.dataset.vaultTab === 'current' ? 'saved' : 'current', true);
      }
    });
  });

  function launchSaved(profileId) {
    if (!profileId) { showNotification('error', 'Refresh the library to load this profile.'); return; }
    close();
    window.chrome_backend?.launchSaveBoxSnapshot(profileId);
  }
  const removeDialog = document.getElementById('removeSessionDialog');
  let removingId = null;
  document.getElementById('vaultWipeButton').addEventListener('click', () => {
    const workspace = MultiBoxSessions.snapshot();
    const session = workspace.sessions.find(s => s.id === workspace.selectedId);
    if (!session || session.isBusy) return;
    removingId = session.id;
    document.getElementById('removeSessionName').textContent = MultiBoxSessions.label(session);
    removeDialog.showModal();
  });
  document.getElementById('cancelRemoveSession').addEventListener('click', () => removeDialog.close());
  document.getElementById('confirmRemoveSession').addEventListener('click', () => {
    const id = removingId;
    removeDialog.close(); close();
    if (id) executeWipeAndTerminate(id);
  });
  removeDialog.addEventListener('close', () => { removingId = null; });
  const archiveIcon = '<svg viewBox="0 0 24 24" aria-hidden="true"><rect x="3" y="3" width="18" height="5" rx="1.5"/><path d="M5 8v13h14V8M9 12h6"/></svg>';
  function renderSavedProfiles() {
    const list = document.getElementById('saveBoxListContainer');
    list.replaceChildren();
    if (!allSnapshots.length) {
      const empty = document.createElement('div');
      empty.className = 'vault-empty';
      empty.innerHTML = archiveIcon + '<h3>Nothing saved yet</h3><p>Choose an app in <strong>Apps</strong>, then tap <strong>Save</strong>.</p>';
      list.append(empty);
      return;
    }
    for (const snapshot of allSnapshots) {
      const name = snapshot.appName || snapshot.AppName || 'Unknown app';
      const profileId = snapshot.profileId || snapshot.ProfileId;
      const profileName = snapshot.profileName || snapshot.ProfileName || 'Default';
      const path = snapshot.appPath || snapshot.AppPath || '';
      const date = snapshot.savedAt || snapshot.SavedAt || 'Recently';
      const size = snapshot.sizeMb ?? snapshot.SizeMb ?? 0;
      const card = document.createElement('article');
      card.className = 'saved-profile';
      card.innerHTML = '<div class="saved-profile-top"><div class="saved-profile-mark">' + archiveIcon + '</div><div class="saved-profile-info"><div class="saved-profile-name"></div><div class="saved-profile-meta"></div></div></div><div class="saved-profile-actions"><button class="vault-button primary" type="button">Open</button><button class="vault-button danger" type="button">Delete</button></div>';
      card.querySelector('.saved-profile-name').textContent = profileName;
      card.querySelector('.saved-profile-meta').textContent = name.replace(/\.exe$/i, '');
      card.querySelector('.saved-profile-meta').title = `${size} MB · Saved ${date}`;
      card.querySelector('.saved-profile-name').title = path;
      card.querySelector('.primary').addEventListener('click', () => launchSaved(profileId));
      const remove = card.querySelector('.danger');
      remove.setAttribute('aria-label', `Delete ${profileName} for ${name}`);
      remove.addEventListener('click', () => deleteSnapshot(profileId, `${profileName} · ${name}`));
      list.append(card);
    }
  };
  sync();
  selectVaultTab('current');
})();

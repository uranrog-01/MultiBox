/* Decorative effects only: the native bridge remains the source of session state. */
(() => {
  'use strict';
  const stage = document.getElementById('dropStage');
  const canvas = document.getElementById('particleCanvas');
  const ctx = canvas.getContext('2d');
  const reducedMotion = matchMedia('(prefers-reduced-motion: reduce)');
  const subtitle = document.getElementById('powerSubtitle');
  const caption = document.getElementById('stageCaption');
  const footnote = document.getElementById('stageFootnote');
  let state = 'idle', width = 0, height = 0, frame = 0, last = 0, time = 0;
  let pointer = null;
  // A regular polar mesh produces a fabric-like wave instead of sparse orbit lines.
  const rows = 32, columns = 240;
  const flecks = Array.from({ length: 240 }, (_, i) => ({
    angle: i * 2.399963, band: ((i * 73) % 241) / 241,
    seed: ((i * 37) % 199) / 199
  }));
  let coreRadius = 118;
  let transitionEnergy = 0;
  let drawMs = 0, renderedFrames = 0;
  const labels = {
    idle: ['YOUR NEXT CLEAN START', 'or click to choose one', 'A separate profile for your next session.'],
    loading: ['PREPARING YOUR WORKSPACE', 'Please wait', 'Setting up the application session…'],
    active: ['YOUR SESSION, IN ITS OWN SPACE', 'Double-click to end & wipe', 'Close when ready. Wipe when finished.'],
    preserved: ['PICK UP WHERE YOU LEFT OFF', 'Click to resume', 'Session data is kept until you wipe it.'],
    ended: ['READY FOR A FRESH START', 'Click to launch again', 'Session ended · saved snapshots are kept.']
  };
  function setState(next) {
    if (state !== next) transitionEnergy = 1;
    state = labels[next] ? next : 'idle';
    document.body.dataset.state = state;
    document.body.style.setProperty('--session-rgb', state === 'preserved' ? '131, 232, 221' : '198, 250, 70');
    [caption.textContent, subtitle.textContent, footnote.textContent] = labels[state];
    if (state === 'active') document.getElementById('powerLabel').textContent = 'Session active';
    if (state === 'ended') {
      document.getElementById('powerLabel').textContent = 'Session ended';
      document.getElementById('statusText').textContent = 'Session ended';
      document.getElementById('statusDot').classList.add('active');
    }
    if (state === 'idle') {
      document.getElementById('powerLabel').textContent = selectedAppPath ? 'Launch app' : 'Drop an app';
      document.getElementById('statusText').textContent = 'Ready to launch';
    }
    document.getElementById('powerBtnContainer').setAttribute('aria-label', state === 'active' ? 'Session active. Double-click to end and wipe this session.' : document.getElementById('powerLabel').textContent);
    draw();
  }
  function resize() {
    width = stage.clientWidth;
    height = stage.clientHeight;
    const dpr = Math.min(devicePixelRatio || 1, 2);
    canvas.width = Math.round(width * dpr);
    canvas.height = Math.round(height * dpr);
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    coreRadius = document.getElementById('powerBtn').offsetWidth / 2;
    draw();
  }
  function draw() {
    if (!width || !height) return;
    const started = performance.now();
    ctx.clearRect(0, 0, width, height);
    const cx = width / 2, cy = height / 2;
    const outer = Math.min(width * .43, height / 2 - 18, 248);
    const inner = Math.min(coreRadius + 13, outer * .68);
    const breadth = Math.max(24, outer - inner - 12);
    const amplitude = Math.min(15, breadth * .24);
    const motion = state === 'loading' ? time * 1.4 : time;
    const cyan = state === 'preserved';
    const color = cyan ? '179,235,225' : '233,243,229';
    const layers = Array.from({ length: 8 }, () => new Path2D());
    ctx.save();
    ctx.globalCompositeOperation = 'lighter';
    // Each row shares the same waves with a small phase offset, forming a rippling sheet.
    for (let row = 0; row < rows; row++) {
      const v = row / (rows - 1);
      for (let col = 0; col < columns; col++) {
        const a = col / columns * Math.PI * 2 + (row % 2) * Math.PI / columns;
        const wave = Math.sin(a * 5 + motion * .72 + v * 2.7) * .53
          + Math.sin(a * 9 - motion * .48 + v * 4.5) * .28
          + Math.cos(a * 3 + motion * .32 - v * 2) * .2;
        const ripple = Math.sin(v * 17 - motion * 1.2 + a * 4) * 2.3;
        const radius = inner + v * breadth + wave * amplitude * (.25 + v * .85) + ripple;
        const angle = a + motion * .035 + Math.sin(v * 5 + motion * .3) * .016;
        let x = cx + Math.cos(angle) * radius;
        let y = cy + Math.sin(angle) * radius * .98;
        if (pointer && !reducedMotion.matches) {
          const dx = x - pointer.x, dy = y - pointer.y;
          const d = Math.hypot(dx, dy);
          if (d > .1 && d < 72) { const force = (72 - d) * .075; x += dx / d * force; y += dy / d * force; }
        }
        const rim = Math.pow(Math.sin(v * Math.PI * .93), 1.6);
        const light = (.28 + .72 * rim) * (.56 + .44 * Math.sin(a * 3 - motion * .55 + v * 7) ** 2);
        const bucket = Math.min(7, Math.floor(light * 7.6));
        const size = .6 + light * .65;
        layers[bucket].rect(x, y, size, size);
      }
    }
    for (let i = 0; i < layers.length; i++) {
      ctx.fillStyle = 'rgba(' + color + ',' + (.12 + i * .12) + ')';
      ctx.fill(layers[i]);
    }
    // A little spray beyond the edge makes the moving fabric feel less mechanical.
    for (const f of flecks) {
      const a = f.angle + motion * (.045 + f.seed * .025);
      const wave = Math.sin(a * 5 + motion * .72 + 2.7) * amplitude * .53 + Math.sin(a * 9 - motion * .48 + 4.5) * amplitude * .28;
      const r = inner + breadth + wave + (f.band - .25) * 21;
      const x = cx + Math.cos(a) * r, y = cy + Math.sin(a) * r * .98;
      ctx.fillStyle = 'rgba(' + color + ',' + (.12 + f.seed * .55) + ')';
      ctx.fillRect(x, y, .7 + f.seed, .7 + f.seed);
    }
    if (transitionEnergy > .01 && !reducedMotion.matches) {
      ctx.beginPath();
      ctx.arc(cx, cy, inner + (1 - transitionEnergy) * breadth, 0, Math.PI * 2);
      ctx.strokeStyle = 'rgba(' + (cyan ? '131,232,221' : '198,250,70') + ',' + transitionEnergy * .18 + ')';
      ctx.lineWidth = 1;
      ctx.stroke();
    }
    ctx.restore();
    drawMs = performance.now() - started;
    renderedFrames++;
  }
  function tick(now) {
    if (document.hidden || reducedMotion.matches) { frame = 0; draw(); return; }
    frame = requestAnimationFrame(tick);
    if (now - last < 1000 / 30) return;
    const delta = Math.min((now - (last || now)) / 1000, .1);
    last = now;
    time += delta;
    transitionEnergy = Math.max(0, transitionEnergy - delta * .75);
    draw();
  }
  function schedule() {
    document.body.classList.toggle('effects-paused', document.hidden);
    cancelAnimationFrame(frame);
    frame = 0; last = 0;
    if (!document.hidden && !reducedMotion.matches) frame = requestAnimationFrame(tick);
    else draw();
  }
  new ResizeObserver(resize).observe(stage);
  stage.addEventListener('pointermove', e => { const r = stage.getBoundingClientRect(); pointer = { x: e.clientX - r.left, y: e.clientY - r.top }; });
  stage.addEventListener('pointerleave', () => { pointer = null; });
  reducedMotion.addEventListener('change', schedule);
  document.addEventListener('visibilitychange', schedule);
  // Give the existing clickable navigation and launch surface keyboard parity.
  document.querySelectorAll('.nav-item, #powerBtnContainer, #appCard').forEach(element => {
    element.tabIndex = 0;
    element.setAttribute('role', 'button');
    element.addEventListener('keydown', event => {
      if (event.target !== element) return;
      if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); element.click(); }
    });
  });
  window.MultiBoxVisuals = { setState, showCompletion: () => setState('ended'), getStats: () => ({ points: rows * columns + flecks.length, drawMs, renderedFrames, animating: !!frame, reducedMotion: reducedMotion.matches }) };
  setState(currentState);
  resize();
  schedule();
})();

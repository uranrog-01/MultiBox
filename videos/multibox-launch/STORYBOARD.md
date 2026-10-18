---
format: 1920x1080
duration: 30s
message: "Run the same Windows app many times, each with its own profile"
arc: Creator → Hook → Product intro → Demo (one copy → many copies) → Save → Breadth → CTA
audience: "Windows power users and developers who juggle several accounts of the same desktop app"
mode: collaborative
music: none
---

<!-- 2026-10-18 · v2 intro (replaces the 10s v1 in ../../intro-video). No narration: on-screen supers carry the words; music + SFX carry the energy.
     music: none here because the bed is generated locally with MusicGen and registered as host audio (assets/bgm/track.wav) — prompt: modern minimal electronic tech pulse, confident and clean, 120 bpm, warm synth plucks, soft four-on-the-floor kick, no vocals. -->

## Video direction

- **Palette (frame.md roles):** canvas `bg` #07080a everywhere except Frame 1; `primary` lime #c6fa46 is the single accent (session active, chips, CTA, glows); `text` #eef5e6 for supers; `text-muted` for secondary lines; card surfaces use `card-bg`/`border` (lime-tinted 4%/20%). Session colors only on session chips/borders: lime #c6fa46 (Work), blue #7cb7ff (Personal), violet #bca1ff (Client). Frame 1 alone is zer0ne's world: black with `creator-orange` #ff7a1a light; the orange never appears after Frame 1.
- **Type:** display = frame.md display family (Segoe UI Variable Display), heavy weight for supers; body/mono by role. Supers are short, sentence-case, never more than one line at a time on screen.
- **Motion grammar:** long-tail `power3` settles; smooth over bouncy (one restrained spring only on the "+" in Frame 3 and on session chips). Reveals are paced to the music's 120 bpm grid (one beat = 0.5s): each element lands on a beat, never all at t=0. Within the demo the camera is a slow, motivated push toward where the cursor acts, then holds — no back-half drift.
- **Continuity:** Frames 4→5 are one world: the MultiBox window keeps its exact position/scale across the cut (handoff below); the first Cline window stays put while copies 2–3 join.
- **Rhythm / holds:** Frame 2 is staccato (fast), Frame 3 lands and holds ~1s (breather before the demo), Frames 4–5 are the busiest, Frame 6 calms, Frame 7 snaps, Frame 8 holds long on the address.
- **Depth on every frame:** background (dark ground + one soft lime radial glow + faint 48px grid at 3–4% opacity) · midground (UI / windows) · foreground (cursor, labels, supers).
- **Negative list:** no slideshow (front-load then freeze), no screensaver (everything floating independently), no lazy breathing, no infinite loops, no random particles, no purple/blue "AI" gradients, no bokeh, no "100% isolated"/"sandbox" wording, no real OS chrome beyond the app windows themselves, nothing important in the bottom ~17%.

## Frame 1 — zer0ne presents

- scene: The zer0ne creator banner ignites out of black: orange light streaks race in, a light sweep crosses the Z1 mark, binary digits drift; "PRESENTS" settles beneath.
- voiceover: ""
- duration: 3.5s
- transition_in: cut
- status: animated
- src: compositions/frames/01-creator.html
- type: branding
- persuasion: Authority by association
- beat: intrigue
- blueprint: logo-assemble-lockup (Adapt)
- asset_candidates: assets/zer0ne-banner.png — zer0ne creator banner, silver Z1 monogram with orange streaks on black
- focal: assets/zer0ne-banner.png
- roles: zer0ne-banner = cutout (centered hero, never above ~1.15x of its 1024px width)
- sfx: whoosh, impact-soft

narrativeRole: Signs the film: a real maker stands behind this tool.
keyMessage: "zer0ne presents."

Adapt: keep the "mark comes to exist on a cleared stage" signature (the banner is revealed by light, not faded); the parts are light streaks, not logo pieces.
Scene 1 (0.0–0.8s): pure black; two thin orange light streaks race in diagonally from opposite corners (motion-blur streak) and cross at center — centered, nothing else on screen.
Scene 2 (0.8–1.8s): at the crossing the banner is revealed through a widening horizontal light slit (clip reveal) with an orange ambient glow blooming behind it; centered, ~60% frame width, 3 layers (glow / banner / streak remnants).
Scene 3 (1.8–2.6s): a bright specular light sweep travels left→right across the Z1 mark once; a sparse column of binary digits flip-decodes at the far left and right edges (hacker-flip, deterministic).
Scene 4 (2.6–3.5s): "PRESENTS" tracks in letter-spaced beneath the banner (per-word reveal, muted text) and everything holds still; the final frame is the clean lockup.

## Frame 2 — One app, one account

- scene: Kinetic type on near-black: "One app." → "One login." → "Sign out. Sign in. Repeat." stacking with a tired, repeating rhythm, then a hard stop.
- voiceover: ""
- duration: 3s
- transition_in: blur-crossfade
- status: animated
- src: compositions/frames/02-hook.html
- type: hook
- persuasion: Pain agitation
- beat: frustration
- blueprint: kinetic-type-beats (Reproduce)
- sfx: ui click, ui click, glitch

narrativeRole: Names the everyday pain every multi-account user knows: desktop apps give you one profile.
keyMessage: Switching accounts in desktop apps is a chore.

Scene 1 (0.0–0.75s): dark ground with faint grid; "One app." slams in centered, display heavy, text color — centered, ~45% width.
Scene 2 (0.75–1.5s): hard-cut word swap to "One login." (same position; the swap is the beat).
Scene 3 (1.5–2.5s): hard cut to a three-token line "Sign out." "Sign in." "Repeat." each token landing on its own beat left→right; "Repeat." in muted text and slightly smaller.
Scene 4 (2.5–3.0s): the three tokens flicker once (brief glitch offset) and hold — the frustration lands before the blur-crossfade takes it away.

## Frame 3 — Meet MultiBox

- scene: The MultiBox icon builds itself: a dark window outline draws, a second window slides on top, the "+" springs in, the lime squircle blooms behind; "MultiBox" wordmark and the tagline rise beside it.
- voiceover: ""
- duration: 3s
- transition_in: zoom-through
- status: animated
- src: compositions/frames/03-intro.html
- type: product_intro
- persuasion: Friction reduction
- beat: curiosity → clarity
- blueprint: logo-assemble-lockup (Reproduce)
- asset_candidates: assets/multibox-icon.svg — MultiBox app icon (lime squircle, two stacked windows, plus)
- focal: assets/multibox-icon.svg
- roles: multibox-icon = cutout (built from its own SVG parts)
- sfx: riser, impact-soft

narrativeRole: The answer arrives; the icon itself shows the idea (windows stacking, one more).
keyMessage: "MultiBox — run the same Windows app many times, each with its own profile."

Scene 1 (0.0–0.9s): center stage; the back window of the icon draws itself (SVG path draw), then the front window slides in diagonally to overlap it — icon parts only, no squircle yet; centered, ~22% height.
Scene 2 (0.9–1.4s): the lime squircle blooms from zero behind the windows (spring-pop entrance, smooth settle) with a soft lime ambient glow; the "+" pops in last with one restrained overshoot.
Scene 3 (1.4–2.2s): the icon glides left to a rule-of-thirds lockup position as the "MultiBox" wordmark rises in beside it (display heavy, text color); asymmetric 40/60.
Scene 4 (2.2–3.0s): the tagline "Run the same Windows app many times — each with its own profile." fades up under the wordmark (muted text, body size) and the lockup holds still.

## Frame 4 — Drop Cline in

- scene: The real MultiBox window floats left of center. A cursor drags a Cline app icon onto "Drop an app"; the power button flips to lime "Session active"; a real Cline window springs out to the right wearing a lime session border and the label "Cline · Work".
- voiceover: ""
- duration: 5.5s
- transition_in: crossfade
- status: animated
- src: compositions/frames/04-demo-first.html
- type: feature_showcase
- persuasion: Show-don't-tell proof
- beat: curiosity → control
- blueprint: cursor-ui-demo (Adapt)
- asset_candidates: assets/ui-idle.png — MultiBox ready state, "Drop an app"; assets/ui-one.png — one Cline session running, "Session active"; assets/cline-window.png — real Cline window from a fresh isolated session
- focal: assets/ui-one.png
- roles: ui-idle = supporting (the window before the drop) · ui-one = cutout (same window after; crossfades over ui-idle in place) · cline-window = cutout (spawned app window, right side)
- sfx: whoosh, ui click, pop, whoosh
- handoff_out: to Frame 5 — MultiBox window: left edge at x=170, top at y=110, height 860 (620x960 UI at scale 0.896), opacity 1, static; Cline window #1: x=860, y=150, width 920, opacity 1, static; camera scale 1.0

narrativeRole: First proof: one drag, and the app runs in its own fresh profile.
keyMessage: Drop an app in. It opens in a fresh, separate profile.

Adapt: keep the cursor-driven state change on a reconstructed-from-real UI (the UI images ARE the real app); the drag is a single drop, not a multi-click flow.
Scene 1 (0.0–1.0s): dark ground with grid + soft lime glow; the MultiBox window (ui-idle) rises into place left of center with a soft shadow-stack; super "Drop any app in." types top-right (text, display semibold). Layout asymmetric 45/55, 3 layers.
Scene 2 (1.0–2.4s): a small Cline app tile (robot mark on white rounded square) enters from the right edge held by a custom cursor and is dragged onto the "Drop an app" circle; the circle's ring brightens as the tile hovers (keyword glow on the target).
Scene 3 (2.4–3.0s): drop — the tile shrinks into the circle (press-release spring), a lime ring ripple expands (cursor click ripple), and the window crossfades in place from ui-idle to ui-one ("Session active").
Scene 4 (3.0–4.4s): a real Cline window (cline-window) springs out of the MultiBox window toward the right, scaling up to its slot; a 3px lime session border draws around it and a lime pill label "Cline · Work" pops at its top-left corner.
Scene 5 (4.4–5.5s): super swaps to "A fresh profile. Its own login." and the composition holds still (handoff state reached at 4.6s).

## Frame 5 — Another copy. And another.

- scene: The cursor clicks "Another copy" twice; session chips pop in (Work lime, Personal blue, Client violet) and two more real Cline windows fan out beside the first, each with its own colored border and label. Super: "Each copy. Its own login."
- voiceover: ""
- duration: 6s
- transition_in: cut
- status: animated
- src: compositions/frames/05-demo-copies.html
- type: feature_showcase
- persuasion: Value stacking
- beat: excitement + power
- blueprint: cursor-ui-demo (Reproduce)
- asset_candidates: assets/ui-three.png — three Cline sessions as lime/blue/violet chips; assets/cline-window.png — real Cline window (shown three times, one per session)
- focal: assets/cline-window.png
- roles: ui-three = supporting (MultiBox window; crossfades in place from Frame 4's ui-one look) · cline-window = cutout ×3 (stacked cascade, one per session color)
- sfx: ui click, pop, ui click, pop, whoosh
- handoff_in: from Frame 4 — MultiBox window: left edge at x=170, top at y=110, height 860 (620x960 UI at scale 0.896), opacity 1, static; Cline window #1: x=860, y=150, width 920, opacity 1, static; camera scale 1.0

narrativeRole: The payoff: the same app, three times, three accounts, side by side.
keyMessage: Click "Another copy". Every copy has its own login, cookies and settings.

Scene 1 (0.0–0.8s): exact Frame 4 end state (handoff); the cursor glides to the MultiBox window's "Another copy" button.
Scene 2 (0.8–2.2s): click 1 (press-release spring on the button + click ripple); a second Cline window springs out, landing offset down-right of the first with a 3px blue border and a blue pill "Cline · Personal"; the MultiBox window's chip row updates (crossfade toward ui-three).
Scene 3 (2.2–3.6s): click 2; a third Cline window springs out further down-right with a violet border and pill "Cline · Client"; the MultiBox window now shows ui-three with all three chips.
Scene 4 (3.6–4.6s): the camera pushes gently toward the three-window cascade (coordinate zoom to the stack, ~1.08x) as the super "Each copy. Its own login." lands top-right (display heavy, text) with "own" in lime.
Scene 5 (4.6–6.0s): hold the read — three distinct sessions side by side, everything still.

## Frame 6 — Save it. Reopen anytime.

- scene: Close on the MultiBox window: the "Give it a name" dialog types "Work" and confirms "Save & reopen"; the window slides to Vault → Saved where "Work" and "Personal" profiles sit with date and size. Super: "Save a setup. Reopen it anytime."
- voiceover: ""
- duration: 4s
- transition_in: push-slide LEFT
- status: animated
- src: compositions/frames/06-save.html
- type: feature_showcase
- persuasion: Risk reversal
- beat: peace of mind
- blueprint: device-surface-showcase (Adapt)
- asset_candidates: assets/ui-save.png — save dialog "Give it a name", name Work, Save & reopen; assets/ui-vault.png — Vault saved profiles Work and Personal with date and size
- focal: assets/ui-vault.png
- roles: ui-save = supporting (first screen in the held window) · ui-vault = cutout (second screen, the payoff)
- sfx: ui click, whoosh, pop

narrativeRole: Removes the fear of losing a session: keep the ones you want.
keyMessage: Save any copy as a named profile and reopen it later.

Adapt: keep the "one floating window held as hero while its screens cycle a real flow" signature (static tour, two screens); no device mockup — the app window is the device.
Scene 1 (0.0–1.4s): the MultiBox window floats center-left, larger (~78% frame height) showing ui-save; a cursor clicks "Save & reopen" (press-release spring + ripple); super "Save a setup." lands right (display heavy). Layout asymmetric 45/55.
Scene 2 (1.4–2.4s): the window's screen slides left to ui-vault (in-window push, the window frame stays put); the "Work" row gets a lime keyword glow as it lands.
Scene 3 (2.4–4.0s): second super line "Reopen it anytime." joins beneath (muted→text); hold still.

## Frame 7 — Works with your apps

- scene: Pills cascade into a tidy grid around a short line: Electron · Chromium · Tauri · WebView2 · Node.js · Bun · Store apps; a second row: No admin · No drivers · Portable.
- voiceover: ""
- duration: 2.5s
- transition_in: zoom-through
- status: animated
- src: compositions/frames/07-breadth.html
- type: benefit_highlight
- persuasion: Rule of three
- beat: confidence
- blueprint: grid-card-assemble (Reproduce)
- sfx: pop

narrativeRole: Breadth and low friction in one glance.
keyMessage: Works with the app types you use; no admin, no drivers.

Scene 1 (0.0–0.5s): headline "Works with the apps you use." rises centered upper-third (display semibold).
Scene 2 (0.5–1.6s): seven pills (card-bg, 1.5px border, pill radius, text) cascade in a centered row-wrap grid — Electron, Chromium, Tauri, WebView2, Node.js, Bun, Store apps — staggered left→right on the beat grid.
Scene 3 (1.6–2.5s): a second row of three lime-filled pills (dark text) springs in — No admin · No drivers · Portable — and the grid holds.

## Frame 8 — Get MultiBox

- scene: The MultiBox lockup lands center (icon + wordmark), the download address types beneath — github.com/uranrog-01/MultiBox — with "Free & open source · v1.0.4"; a small zer0ne mark signs the bottom corner; light keeps drifting behind.
- voiceover: ""
- duration: 3.5s
- transition_in: zoom-through
- status: animated
- src: compositions/frames/08-cta.html
- type: cta
- persuasion: Friction reduction
- beat: urgency-to-act
- blueprint: logo-assemble-lockup (Adapt)
- asset_candidates: assets/multibox-icon.svg — app icon for the lockup; assets/zer0ne-logo.png — small zer0ne mark for the corner credit
- focal: assets/multibox-icon.svg
- roles: multibox-icon = cutout (lockup) · zer0ne-logo = supporting (small credit, top-right corner, ~5% height)
- sfx: impact-soft, typing

narrativeRole: Close on the address while the logo still moves.
keyMessage: Free on GitHub: github.com/uranrog-01/MultiBox.

Adapt: keep the lockup-resolving-into-URL signature; the icon arrives already built (it was built in Frame 3) and scales into the lockup.
Scene 1 (0.0–0.9s): the icon springs to center from slightly larger with a lime ambient glow blooming; "MultiBox" wordmark lands to its right — centered lockup, ~55% width.
Scene 2 (0.9–2.3s): beneath the lockup a terminal-style pill (card-bg, border) types "github.com/uranrog-01/MultiBox" with a blinking caret (finite blinks only), mono font, text color with "MultiBox" in lime.
Scene 3 (2.3–2.8s): "Free & open source · v1.0.4" fades up under the pill (muted); the zer0ne mark fades in top-right as a small credit.
Scene 4 (2.8–3.5s): hold; the only motion is the soft glow behind the icon slowly brightening once (bounded, no loop) — the film ends on a moving, lit logo.

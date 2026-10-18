---
workflow: product-launch-video
flow: automation
storyboard: yes
message: "Run the same Windows app many times, each with its own profile"
destination: youtube
aspect: 1920x1080
language: en
audience: "Windows power users and developers who juggle several accounts of the same desktop app"
length: 30s
angle: product-demo
---

## Intent

A modern, professional 30-second intro for MultiBox v1.0.4 (an open-source Windows app by zer0ne) for the GitHub README, the project website and YouTube. Sell the product with a real demo: take Cline (a Tauri AI coding app), open it isolated in MultiBox, and open it several more times, each copy its own separate session. Open with the zer0ne creator banner and "very nice modern visual effects", professional look.

User's words: "make better video intro ... make it modern base own our project and include some demo how to open ... cline ... as a demo app to isolated and open multiply time. and include zer0ne creator banner to make some very nice modern visual effects and professional look"

## Assets

- ../../docs/assets/zer0ne-banner.png — zer0ne creator banner; the opening beat.
- ../../design/icon/multibox-icon.svg — new MultiBox app icon (lime #c6fa46 rounded square, two stacked windows with a "+"); logo moments and close.
- ../../docs/assets/social-preview.png — brand card (icon + tagline); reference for the close.
- ../../docs/screenshots/*.png — real MultiBox UI captures (dashboard with sessions, Vault saved profiles, Tasks, Settings), rendered from the shipping UI.
- Real MultiBox UI (../../UI/index.html) driven with sample data — the product's actual interface, not a re-creation.
- Cline window — captured from a fresh, empty isolated session so no personal data appears.

## Customizations

- Music bed with sound effects timed to cuts and clicks (no voiceover).
- Demo beat: drop Cline → session active → "Another copy" ×2 → three Cline windows, each with MultiBox's colored session border and label.
- zer0ne creator banner as the opening beat with a premium light-sweep reveal.

## Notes

- Never claim "100% isolated" or "sandbox": MultiBox separates app profiles; it is not a security boundary.
- Version shown: v1.0.4. Close on the download address: github.com/uranrog-01/MultiBox.
- Brand palette from the app: near-black #07080a / #0a0e0b canvas, lime accent #c6fa46, soft white text.
- The old 10s intro lives in ../../intro-video (v1); this project replaces it.

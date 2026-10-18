# Asset inventory (no website capture; real product assets)

All files are in `capture/assets/`. UI images are the shipping MultiBox interface (UI/index.html) rendered headlessly at 2x with sample Cline sessions; nothing is mocked up by hand.

| File | Size | What it shows | Use |
| --- | --- | --- | --- |
| `zer0ne-banner.png` | 1024x341 | zer0ne creator banner: silver Z1 monogram, "zer0ne" wordmark, orange light streaks and binary digits on black. Low-res: show at <=1.15x. | Opening beat (creator) |
| `zer0ne-logo.png` | 185x120 | Small zer0ne Z1 mark | Small credit on the close |
| `multibox-icon.svg` | vector | MultiBox app icon: lime #c6fa46 rounded square, two stacked dark windows, "+" on the front one | Logo reveal, close (scales to any size) |
| `multibox-icon-512.png` | 512x512 | Raster of the same icon | Fallback |
| `social-preview.png` | 1280x640 | Brand card: icon, "MultiBox", tagline, chips | Reference for the close card |
| `ui-idle.png` | 1240x1920 | MultiBox window, ready state: "Drop an app", power button, "Select Application / Browse" | Demo start |
| `ui-one.png` | 1240x1920 | One Cline session running ("Cline · Work"), lime "Session active" button, telemetry, Save, "Another copy" | Demo: first copy |
| `ui-three.png` | 1240x1920 | Three Cline sessions as chips (Work/lime, Personal/cyan, Client/violet), session running | Demo: many copies |
| `ui-save.png` | 1240x1920 | "Give it a name" save dialog, name "Work", "Save & reopen" button, dimmed app behind | Save beat |
| `ui-vault.png` | 1240x1920 | Vault → Saved: "Work" and "Personal" Cline profiles with date and size, Open/Delete | Save beat payoff |
| `cline-window.png` | 1271x820 | Real Cline app (Tauri) window content from a fresh, empty isolated session: robot mark, "Cline — Build software your way", "Get started" | The demo app's windows, shown 3x with MultiBox session borders/labels |

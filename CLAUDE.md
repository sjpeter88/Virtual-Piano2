# CLAUDE.md — Project Architecture & Build Guide

This repository (`d:\AntiGravityDesktop\Virtual-Piano2`) contains the **Concert Grand 3D Virtual Piano** web application and its **.NET 10 + WebView2** single-file Windows desktop executable wrapper.

## 1. Key Constraints & Design Rules
- **No Web Build Tools or External Runtime Fetches**: The web app must remain 100% self-contained in a single [index.html](index.html) file (inline CSS, HTML, and ES module JavaScript) loading Three.js locally from [lib/three.module.min.js](lib/three.module.min.js). Never add external CDN URLs (`http://` or `https://`) for scripts, fonts, textures, or audio samples. Audio and geometry are synthesized procedurally offline; the only non-procedural assets are the photographic room textures in [images/textures/](images/textures), which are embedded into `index.html` as base64 data URIs (see "Textures" below), never fetched at runtime.
- **Physical-Modeling AudioWorklet**: All piano audio is synthesized inside `WORKLET_PROCESSOR_CODE` (loaded via an inline `Blob` URL into `audioCtx.audioWorklet.addModule`).
- **Dual Visual Themes (`Christmas` & `Classic Studio`)**: Keep both themes intact and switchable via `toggleVisualTheme()`. In `Classic Studio` mode, `christmasRoomGroup.visible` and `pianoDecorGroup.visible` are `false`, restoring the original dark studio background and camera angle.
- **Version Numbering Rule**:
  - The current version is displayed in the top-left brand header (`#app-version-badge` and `APP_VERSION` in `index.html`).
  - When building a new Windows executable release, increment the version (e.g., `v1.0.1` or `v1.1.0`) in three places so they stay in sync:
    1. `index.html`: `#app-version-badge` HTML text and `const APP_VERSION = 'vX.Y.Z';`
    2. `desktop-shell/Program.cs`: `public const string AppVersion = "vX.Y.Z";`
    3. `desktop-shell/ConcertGrandShell.csproj`: `<AssemblyName>ConcertGrandPiano-vX.Y.Z</AssemblyName>` and `<Version>X.Y.Z</Version>` (so the output executable has the version right before `.exe`, e.g., `ConcertGrandPiano-v1.2.0.exe`).

## 2. File Structure
- `index.html` — Complete 3D Virtual Piano application (14 numbered code sections: settings, 26 public-domain songs with two-hand note data & synchronized lyrics, AudioWorklet processor, Three.js 88-key grand piano + 3D Christmas living room, mouse/multi-touch/DAW keyboard/Web MIDI input, chords/scales/metronome, and WebView2 desktop bridge).
- `lib/three.module.min.js` — Local Three.js r160 ES module.
- `desktop-shell/ConcertGrandShell.csproj` — .NET 10 WinForms + WebView2 project that embeds `..\index.html` and `..\lib\three.module.min.js` directly into the `.exe` binary as `EmbeddedResource` items.
- `desktop-shell/Program.cs` — Single-file WebView2 host (`MainForm`). Serves embedded resources via `https://concertgrand.app/index.html`, enables zero-click audio autoplay (`--autoplay-policy=no-user-gesture-required`), auto-grants MIDI permissions, supports `F11` / `Alt+Enter` fullscreen, and supports **Option 1B** (`Ctrl+Shift+G` or View menu button) to toggle between the embedded offline build and `https://sjpeter88.github.io/Virtual-Piano2/`.
- `desktop-shell/piano.ico` — Multi-resolution (16–256 px) app icon, wired in via `<ApplicationIcon>`; also used as the window/taskbar icon at runtime.
- `ConcertGrandPiano-v1.2.0.exe` — Framework-dependent single-file executable (~2 MB, needs .NET 10 Desktop Runtime) in the project root.
- `ConcertGrandPiano-v1.2.0-Standalone.exe` — Self-contained build (~118 MB, no .NET needed). **Git-ignored** because it exceeds GitHub's 100 MB file limit; rebuild locally.
- v1.2.0 shell features: window size/position/maximized state remembered in `%LOCALAPPDATA%\ConcertGrandPiano\window-state.json`; browser back/forward and off-site navigation blocked (DevTools F12 still available); `--kiosk` launch argument for borderless fullscreen with a floating Minimize/Windowed/Exit pill; native Save As dialog for downloads; **Record** button (top bar) captures the piano output to a 16-bit stereo `.wav` (metronome clicks excluded).

## 2b. Textures
- Source images (12 seamless JPGs generated with Gemini) live in `images/textures/`: `floor_oak`, `wall_plaster`, `brick_fireplace`, `bookcase_oak`, `pine_needles`, `knit_red`, `knit_green`, `book_spines`, `window_night`, `rug_persian`, `gift_wrap_red`, `gift_wrap_gold`.
- `tools/embed_textures.py` (needs Pillow) downsizes them and rewrites the `<script id="texture-data">` block in `index.html` (`window.TEXTURE_DATA`, ~1.4 MB). Re-run it after changing any source image: `python tools/embed_textures.py`.
- In the scene, `roomTex(key, repeatX, repeatY, mirror)` loads a texture; each material keeps a colour tint that multiplies the photo. If a texture is missing the material falls back to its plain tint colour.
- The Christmas camera is aspect-aware and the 3D canvas is full-bleed behind the toolbars in Christmas mode. Glow/bloom and lacquer reflections are Christmas-only (View menu has a Glow toggle).

## 3. Common Commands

### Run Local Static Web Server
*(Note: A background `python -m http.server 8080` may already be running on port 8080.)*
```powershell
python -m http.server 8080
```
Then open `http://localhost:8080`.

### Build the Single-File Windows `.exe`
*(Make sure any running `ConcertGrandPiano-vX.Y.Z.exe` window is closed before overwriting the `.exe`.)*
```powershell
dotnet publish desktop-shell/ConcertGrandShell.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false -o dist
Copy-Item dist\ConcertGrandPiano-v*.exe .\ -Force
```
Self-contained build (~118 MB, includes the .NET runtime; use a separate output folder and rename):
```powershell
dotnet publish desktop-shell/ConcertGrandShell.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false -o dist-standalone
Copy-Item dist-standalone\ConcertGrandPiano-vX.Y.Z.exe .\ConcertGrandPiano-vX.Y.Z-Standalone.exe -Force
```

### Push Updates to GitHub & GitHub Pages
- **Repository:** `https://github.com/sjpeter88/Virtual-Piano2`
- **Live GitHub Pages URL:** `https://sjpeter88.github.io/Virtual-Piano2/`
```powershell
git add index.html desktop-shell/ CLAUDE.md README.md
git commit -m "Describe changes"
git push origin main
```

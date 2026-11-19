# Concert Grand — 3D Virtual Piano (`v1.1.1`)

A self-contained, offline-capable 3D 88-key Concert Grand Virtual Piano built with **Three.js** and a physical-modeling **Web Audio API `AudioWorklet`** engine, plus a single-file **.NET 10 + WebView2** Windows desktop application (`ConcertGrandPiano-v1.1.1.exe`).

- **Live Website (GitHub Pages):** https://sjpeter88.github.io/Virtual-Piano2/
- **GitHub Repository:** https://github.com/sjpeter88/Virtual-Piano2

## Features
- **88-Key 3D Grand Piano (`A0` to `C8`)**: Realistic key pivot physics, soft shadows, note labels with Middle C (`C4`) badge, zoom/scroll controls, and two switchable visual themes:
  - **Christmas Living Room Theme**: Cozy 3D living room with a flickering brick fireplace, stockings, mantel garland, wreath with red bow & gold star, two paned night windows with falling snow, twin decorated Christmas trees with twinkling bokeh fairy lights, a 3-bay oak bookcase with colorful books & glowing candles, and poinsettia/holly garlands on the piano fallboard.
  - **Classic Studio Theme**: Warm, dark concert studio backdrop with subtle lacquer reflections.
- **Physical-Modeling AudioWorklet Synthesizer**: Stiff-string inharmonic overtones, unison string detuning, velocity-controlled spectral brightness, soft-felt hammer thump, 36-voice polyphony with click-free voice stealing, sustain pedal with 4-comb sympathetic string resonance, stereo room reverb, and soft limiter.
- **Ways to Play**: Mouse click & glissando drag (front-to-back positional velocity), DAW-style computer keyboard (`A W S E D F T G Y H U J K O L P ;` + `Z`/`X` octave shift + `Space` sustain), multi-touch, and plug-and-play Web MIDI (`CC 64` sustain).
- **Practice Helpers**: 8 quick chord buttons (`C`, `G`, `D`, `Am`, `Em`, `F`, `Dm`, `E7`), scale/key highlighter across all 88 keys, and a precision Web Audio metronome.
- **26 Complete Public-Domain Songs with Synchronized Lyrics**: 18 full multi-verse Christmas & holiday classics and 8 classical/folk pieces arranged for both hands (Left Hand in Warm Amber, Right Hand in Sapphire Blue), with a toggleable live Sing-Along Lyrics banner and an interactive Full Lyrics Sheet modal.
- **.wav Recorder**: The top-bar **⏺ Record** button captures your playing (or a self-playing song) to a 16-bit stereo `.wav`; in the desktop app it opens a native Save As dialog.
- **Single-File Windows `.exe` (`ConcertGrandPiano-v1.1.1.exe`, ~2 MB, needs .NET 10 Desktop Runtime; plus a ~118 MB self-contained `-Standalone.exe` built locally)**: Embeds `index.html` and `lib/three.module.min.js` for 100% offline use, with `F11` fullscreen, `Ctrl + Shift + G` to toggle between the embedded build and live GitHub Pages, remembered window size/position, locked browser chrome, a piano `.ico`, and a `--kiosk` launch mode.

See [CLAUDE.md](CLAUDE.md) for detailed architecture and build instructions.

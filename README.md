# Vigilante Custom Music

A BepInEx plugin that replaces the original soundtrack in the Windows build of **Vigilante 8: 2nd Offense (v8so)** with a local personal playlist.

No music is bundled with this project. Users provide their own MP3, OGG, or WAV files.

## Features

- Replaces the original menu and gameplay music at runtime.
- Preserves sound effects, vehicle sounds, interface sounds, and voices.
- Supports `.mp3`, `.ogg`, and `.wav` files in the same playlist.
- Plays tracks alphabetically or in random order.
- Automatically advances and repeats the playlist.
- Lets you skip to the next or previous track with configurable keyboard or gamepad controls.
- Provides a separate volume multiplier.
- Keeps the official soundtrack muted when the custom folder is empty, if configured.
- Falls back safely when an individual file cannot be decoded.

## Installation

1. Install BepInEx 5 x64 in the directory containing `v8so.exe`.
2. Download `VigilanteCustomMusic-v1.0.0.zip` from Releases.
3. Extract it into the directory containing `v8so.exe`.
4. Put personal tracks in:

```text
BepInEx/plugins/VigilanteCustomMusic/Music
```

5. Start the game.

Example ordering:

```text
01 - Opening.mp3
02 - Battle.ogg
03 - Finale.wav
```

The game must be restarted after adding or removing files.

## Configuration

After the first launch, edit:

```text
BepInEx/config/community.v8so.custommusic.cfg
```

Available options:

- `Enabled`: enable or disable soundtrack replacement.
- `Shuffle`: choose random tracks instead of alphabetical order.
- `VolumeMultiplier`: adjust custom music relative to the game's music volume.
- `MuteOriginalWhenNoTracks`: keep the official soundtrack silent if no custom tracks load.

### Manual track controls

- `F8`: next track.
- `F7`: previous track.
- Right stick right/left: next/previous track.
- Right-stick click: toggle between sequential and shuffle playback. The selected mode is saved in the configuration.
- `NextTrackKey` and `PreviousTrackKey`: change the keyboard shortcuts.
- `NextTrackGamepadButton` and `PreviousTrackGamepadButton`: optionally assign controller buttons. They default to `None` so they do not conflict with gameplay controls. Values such as `JoystickButton4` and `JoystickButton5` can be used.
- `RightStickControls`: enable or disable all right-stick shortcuts.
- `RightStickHorizontalAxisId`, `RightStickClickButtonId`, and `RightStickThreshold`: adjust physical Rewired input detection for a different controller.

## How it works

The game stores soundtrack clips in `GameManager.track` and plays them through voice channel 24. The plugin patches `GameManager.PlayTrack` and `GameManager.LoopTrack`, stops the official clip, and uses the same `AudioSource` for the custom playlist. This preserves the game's existing volume and pause behavior while leaving effects and voices untouched.

Audio files are loaded locally through Unity's audio download handler. Unsupported files are skipped and reported in `BepInEx/LogOutput.log`.

## Compatibility

Validated with:

- Windows x64 game build dated 2024-03-04
- Unity 2021.2.x
- BepInEx 5.4.23.5
- MP3 and OGG local playback

## Build

No game or BepInEx binaries are committed to the source tree. Build against an installed copy:

```powershell
.\Build.ps1 -GameDirectory "C:\path\to\v8so"
```

Output: `release/VigilanteCustomMusic.dll`.

## Copyright

This project contains no copyrighted soundtrack files. Users are responsible for the audio files they add and should not redistribute music without permission.

## Português

O mod substitui as músicas oficiais por arquivos pessoais MP3, OGG ou WAV sem alterar efeitos e vozes. Extraia a release na pasta de `v8so.exe` e coloque suas músicas em `BepInEx/plugins/VigilanteCustomMusic/Music`.

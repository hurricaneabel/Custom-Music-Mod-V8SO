# Changelog

## 1.3.0

- Enable shuffle by default for new installations.
- Replace individual random picks with a Fisher-Yates shuffled queue.
- Play every track once before reshuffling the full playlist.
- Generate a fresh random order on each game launch and whenever shuffle is enabled with R3.
- Make previous-track navigation follow the active shuffled order.

## 1.2.2

- Detect the right-stick horizontal axis from the Rewired element name.
- Use axis index 2 for the four-axis XInput layout reported by the gamepad fix.
- Migrate the previous Raw Input fallback automatically when an existing config still contains index 3.

## 1.2.1

- Fix right-stick controls on Raw Input controllers by reading physical axis and button indexes instead of Rewired element ids.
- Log the detected controller and its available axis/button counts once for troubleshooting.

## 1.2.0

- Move to the next or previous track by flicking the right stick right or left.
- Toggle sequential and shuffle playback by clicking the right stick.
- Add configurable Rewired axis, button, and activation threshold values.
- Require the stick to return to center before another track change.

## 1.1.0

- Add configurable next-track and previous-track keyboard shortcuts.
- Add optional configurable gamepad buttons for manual track changes.
- Prevent held buttons from skipping multiple tracks by using press events.

## 1.0.0

- Replace official menu and gameplay music with a local playlist.
- Support MP3, OGG, and WAV files.
- Add alphabetical and shuffle playback modes.
- Advance and repeat the playlist automatically.
- Preserve game music volume and pause behavior.
- Keep effects, interface sounds, and voices unchanged.
- Add configurable volume and empty-playlist behavior.

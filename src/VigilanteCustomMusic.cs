using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Rewired;
using UnityEngine;
using UnityEngine.Networking;

[BepInPlugin("community.v8so.custommusic", "Vigilante Custom Music", "1.2.1")]
public sealed class VigilanteCustomMusic : BaseUnityPlugin {
    private static VigilanteCustomMusic instance;
    private Harmony harmony;
    private readonly List<AudioClip> playlist = new List<AudioClip>();
    private readonly System.Random random = new System.Random();
    private ConfigEntry<bool> enabledConfig;
    private ConfigEntry<bool> shuffleConfig;
    private ConfigEntry<float> volumeConfig;
    private ConfigEntry<bool> muteWhenEmptyConfig;
    private ConfigEntry<KeyCode> nextTrackKeyConfig;
    private ConfigEntry<KeyCode> previousTrackKeyConfig;
    private ConfigEntry<KeyCode> nextTrackGamepadConfig;
    private ConfigEntry<KeyCode> previousTrackGamepadConfig;
    private ConfigEntry<bool> rightStickControlsConfig;
    private ConfigEntry<int> rightStickHorizontalAxisIndexConfig;
    private ConfigEntry<int> rightStickClickButtonIndexConfig;
    private ConfigEntry<float> rightStickThresholdConfig;
    private FieldInfo voicesField;
    private AudioSource musicSource;
    private int currentIndex = -1;
    private bool loading;
    private bool trackRequested;
    private float playbackGuard;
    private float gameMusicVolume = 1f;
    private bool rightStickHorizontalLatched;
    private bool rewiredPollingWarningLogged;
    private bool rewiredControllerLogged;

    private string MusicDirectory {
        get { return Path.Combine(Paths.PluginPath, "VigilanteCustomMusic", "Music"); }
    }

    private void Awake() {
        instance = this;
        enabledConfig = Config.Bind("General", "Enabled", true, "Replace the original soundtrack with the custom playlist.");
        shuffleConfig = Config.Bind("Playback", "Shuffle", false, "Choose a random track instead of alphabetical order.");
        volumeConfig = Config.Bind("Playback", "VolumeMultiplier", 1.0f,
            new ConfigDescription("Custom music volume multiplier.", new AcceptableValueRange<float>(0f, 2f)));
        muteWhenEmptyConfig = Config.Bind("General", "MuteOriginalWhenNoTracks", true,
            "Keep the original soundtrack muted when no supported custom tracks were loaded.");
        nextTrackKeyConfig = Config.Bind("Controls", "NextTrackKey", KeyCode.F8,
            "Keyboard key used to play the next custom track.");
        previousTrackKeyConfig = Config.Bind("Controls", "PreviousTrackKey", KeyCode.F7,
            "Keyboard key used to play the previous custom track.");
        nextTrackGamepadConfig = Config.Bind("Controls", "NextTrackGamepadButton", KeyCode.None,
            "Optional gamepad button used for the next track, for example JoystickButton5. None disables it.");
        previousTrackGamepadConfig = Config.Bind("Controls", "PreviousTrackGamepadButton", KeyCode.None,
            "Optional gamepad button used for the previous track, for example JoystickButton4. None disables it.");
        rightStickControlsConfig = Config.Bind("Controls", "RightStickControls", true,
            "Use the right stick for track controls: right/left changes track and clicking toggles shuffle.");
        rightStickHorizontalAxisIndexConfig = Config.Bind("Controls", "RightStickHorizontalAxisIndex", 3,
            "Rewired physical axis index for the right stick horizontal axis.");
        rightStickClickButtonIndexConfig = Config.Bind("Controls", "RightStickClickButtonIndex", 9,
            "Rewired physical button index for the right stick click button.");
        rightStickThresholdConfig = Config.Bind("Controls", "RightStickThreshold", 0.75f,
            new ConfigDescription("How far the stick must move before changing tracks.",
                new AcceptableValueRange<float>(0.5f, 0.95f)));

        Directory.CreateDirectory(MusicDirectory);
        Type gameManager = AccessTools.TypeByName("GameManager");
        if (gameManager == null) {
            Logger.LogError("GameManager was not found. The plugin cannot control music in this build.");
            return;
        }
        voicesField = AccessTools.Field(gameManager, "voices");
        harmony = new Harmony("community.v8so.custommusic");
        MethodInfo playTrack = AccessTools.Method(gameManager, "PlayTrack", new Type[] { typeof(int), typeof(bool) });
        MethodInfo loopTrack = AccessTools.Method(gameManager, "LoopTrack", new Type[] { typeof(bool) });
        if (playTrack != null)
            harmony.Patch(playTrack, postfix: new HarmonyMethod(typeof(VigilanteCustomMusic), "AfterGamePlayTrack"));
        if (loopTrack != null)
            harmony.Patch(loopTrack, postfix: new HarmonyMethod(typeof(VigilanteCustomMusic), "AfterGameLoopTrack"));

        StartCoroutine(LoadPlaylist());
    }

    private IEnumerator LoadPlaylist() {
        loading = true;
        string[] files = Directory.GetFiles(MusicDirectory, "*", SearchOption.TopDirectoryOnly);
        Array.Sort(files, StringComparer.OrdinalIgnoreCase);
        foreach (string path in files) {
            AudioType type;
            string extension = Path.GetExtension(path).ToLowerInvariant();
            if (extension == ".mp3") type = AudioType.MPEG;
            else if (extension == ".ogg") type = AudioType.OGGVORBIS;
            else if (extension == ".wav") type = AudioType.WAV;
            else continue;

            string uri = new Uri(path).AbsoluteUri;
            using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(uri, type)) {
                DownloadHandlerAudioClip handler = request.downloadHandler as DownloadHandlerAudioClip;
                if (handler != null) handler.streamAudio = true;
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success) {
                    Logger.LogWarning("Could not load '" + Path.GetFileName(path) + "': " + request.error);
                    continue;
                }
                AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
                if (clip == null) {
                    Logger.LogWarning("Unity returned no audio clip for '" + Path.GetFileName(path) + "'.");
                    continue;
                }
                clip.name = Path.GetFileNameWithoutExtension(path);
                playlist.Add(clip);
                Logger.LogInfo("Loaded custom track: " + Path.GetFileName(path));
            }
        }
        loading = false;
        Logger.LogInfo("Custom playlist ready: " + playlist.Count + " track(s). Folder: " + MusicDirectory);
        if (trackRequested && enabledConfig.Value && playlist.Count > 0) PlayNext(true);
    }

    private static void AfterGamePlayTrack() {
        if (instance != null) instance.OnGameTrackRequested();
    }

    private static void AfterGameLoopTrack() {
        if (instance != null) instance.ApplyLoopMode();
    }

    private void OnGameTrackRequested() {
        if (!enabledConfig.Value) return;
        trackRequested = true;
        ResolveMusicSource();
        if (musicSource != null) {
            gameMusicVolume = musicSource.volume;
            musicSource.Stop();
        }
        if (!loading && playlist.Count > 0) PlayNext(currentIndex < 0);
        else if (!loading && playlist.Count == 0 && !muteWhenEmptyConfig.Value) trackRequested = false;
    }

    private void ResolveMusicSource() {
        if (musicSource != null) return;
        AudioSource[] voices = voicesField == null ? null : voicesField.GetValue(null) as AudioSource[];
        if (voices != null && voices.Length > 24) musicSource = voices[24];
    }

    private void PlayNext(bool firstTrack) {
        ResolveMusicSource();
        if (musicSource == null || playlist.Count == 0) return;
        if (shuffleConfig.Value && playlist.Count > 1) {
            int next;
            do { next = random.Next(playlist.Count); } while (!firstTrack && next == currentIndex);
            currentIndex = next;
        } else {
            currentIndex = firstTrack || currentIndex < 0 ? 0 : (currentIndex + 1) % playlist.Count;
        }
        musicSource.Stop();
        musicSource.clip = playlist[currentIndex];
        musicSource.loop = playlist.Count == 1;
        musicSource.volume = Mathf.Clamp01(gameMusicVolume * volumeConfig.Value);
        musicSource.Play();
        playbackGuard = Time.unscaledTime + 1f;
        Logger.LogInfo("Now playing: " + musicSource.clip.name);
    }

    private void PlayPrevious() {
        ResolveMusicSource();
        if (musicSource == null || playlist.Count == 0) return;
        currentIndex = currentIndex <= 0 ? playlist.Count - 1 : currentIndex - 1;
        PlayCurrent();
    }

    private void PlayCurrent() {
        if (musicSource == null || playlist.Count == 0 || currentIndex < 0) return;
        musicSource.Stop();
        musicSource.clip = playlist[currentIndex];
        musicSource.loop = playlist.Count == 1;
        musicSource.volume = Mathf.Clamp01(gameMusicVolume * volumeConfig.Value);
        musicSource.Play();
        playbackGuard = Time.unscaledTime + 1f;
        Logger.LogInfo("Now playing: " + musicSource.clip.name);
    }

    private bool WasPressed(ConfigEntry<KeyCode> binding) {
        return binding != null && binding.Value != KeyCode.None && Input.GetKeyDown(binding.Value);
    }

    private bool PollRightStickControls() {
        if (!rightStickControlsConfig.Value || !ReInput.isReady) return false;
        try {
            IList<Joystick> joysticks = ReInput.controllers.Joysticks;
            bool centered = true;
            for (int i = 0; i < joysticks.Count; i++) {
                Joystick joystick = joysticks[i];
                if (joystick == null || !joystick.isConnected || !joystick.enabled) continue;

                if (!rewiredControllerLogged) {
                    rewiredControllerLogged = true;
                    Logger.LogInfo("Right-stick controls connected to '" + joystick.name + "' (axes=" +
                        joystick.axisCount + ", buttons=" + joystick.buttonCount + ").");
                }

                int buttonIndex = rightStickClickButtonIndexConfig.Value;
                if (buttonIndex >= 0 && buttonIndex < joystick.buttonCount && joystick.GetButtonDown(buttonIndex)) {
                    shuffleConfig.Value = !shuffleConfig.Value;
                    Logger.LogInfo("Playback mode: " + (shuffleConfig.Value ? "shuffle" : "sequential"));
                    return true;
                }

                int axisIndex = rightStickHorizontalAxisIndexConfig.Value;
                if (axisIndex < 0 || axisIndex >= joystick.axisCount) continue;
                float horizontal = joystick.GetAxisRaw(axisIndex);
                if (Mathf.Abs(horizontal) >= rightStickThresholdConfig.Value) {
                    centered = false;
                    if (!rightStickHorizontalLatched) {
                        rightStickHorizontalLatched = true;
                        if (horizontal > 0f) PlayNext(false);
                        else PlayPrevious();
                        return true;
                    }
                }
            }
            if (centered) rightStickHorizontalLatched = false;
        } catch (Exception ex) {
            if (!rewiredPollingWarningLogged) {
                rewiredPollingWarningLogged = true;
                Logger.LogWarning("Could not read right-stick controls: " + ex.Message);
            }
        }
        return false;
    }

    private void ApplyLoopMode() {
        if (!enabledConfig.Value || musicSource == null || musicSource.clip == null) return;
        if (playlist.Contains(musicSource.clip)) musicSource.loop = playlist.Count == 1;
    }

    private void Update() {
        if (!enabledConfig.Value || loading || !trackRequested || playlist.Count == 0) return;
        ResolveMusicSource();
        if (musicSource == null) return;
        if (PollRightStickControls()) return;
        if (WasPressed(nextTrackKeyConfig) || WasPressed(nextTrackGamepadConfig)) {
            PlayNext(false);
            return;
        }
        if (WasPressed(previousTrackKeyConfig) || WasPressed(previousTrackGamepadConfig)) {
            PlayPrevious();
            return;
        }
        if (AudioListener.pause || Time.unscaledTime < playbackGuard) return;
        if (playlist.Contains(musicSource.clip) && !musicSource.isPlaying) PlayNext(false);
    }

    private void OnDestroy() {
        if (harmony != null) harmony.UnpatchSelf();
        foreach (AudioClip clip in playlist) if (clip != null) Destroy(clip);
        playlist.Clear();
        if (instance == this) instance = null;
    }
}

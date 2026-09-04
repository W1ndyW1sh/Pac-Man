using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CampusMaze
{
    public sealed class MazeAudio : MonoBehaviour
    {
        const int SampleRate = 44100;

        readonly Dictionary<string, AudioClip> effects = new Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, float> lastCueTimes = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        AudioSource musicSource;
        AudioSource effectsSource;
        AudioClip intro;
        AudioClip menuLoop;
        AudioClip normalLoop;
        AudioClip scaredLoop;
        AudioClip deadLoop;
        Coroutine introRoutine;
        bool initialized;
        bool scaredMood;
        bool deadMood;
        bool introPlaying;
        bool stopped;

        void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (initialized)
                return;

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = false;
            musicSource.spatialBlend = 0f;
            musicSource.volume = 0.2f;
            musicSource.priority = 32;

            effectsSource = gameObject.AddComponent<AudioSource>();
            effectsSource.playOnAwake = false;
            effectsSource.loop = false;
            effectsSource.spatialBlend = 0f;
            effectsSource.volume = 0.32f;
            effectsSource.priority = 64;

            intro = Load("GameIntro");
            if (intro == null)
                intro = Sequence("Maze_Start", new[] { 392f, 523.25f, 659.25f, 783.99f, 1046.5f }, 0.14f, 0.7f, false);
            menuLoop = Load("StartSceneMusic");
            if (menuLoop == null)
                menuLoop = MusicLoop("Maze_MenuLoop", false);
            normalLoop = Load("GhostNormalMusic");
            if (normalLoop == null)
                normalLoop = MusicLoop("Maze_NormalLoop", false);
            scaredLoop = Load("GhostScaredMusic");
            if (scaredLoop == null)
                scaredLoop = MusicLoop("Maze_ScaredLoop", true);
            deadLoop = Load("GhostDeadMusic");
            if (deadLoop == null)
                deadLoop = Sequence("Maze_DeadLoop", new[] { 293.66f, 392f, 246.94f, 329.63f }, 0.18f, 0.32f, false);
            effects["pellet"] = Load("EatPellet");
            if (effects["pellet"] == null)
                effects["pellet"] = Sequence("Maze_Pellet", new[] { 880f, 1174.66f }, 0.045f, 0.62f, false);
            effects["power"] = Sequence("Maze_Power", new[] { 392f, 523.25f, 783.99f }, 0.105f, 0.75f, false);
            effects["ghost"] = Load("EatGhost");
            if (effects["ghost"] == null)
                effects["ghost"] = Sequence("Maze_Ghost", new[] { 987.77f, 739.99f, 1108.73f }, 0.07f, 0.68f, true);
            effects["bonus"] = Load("EatBonus");
            if (effects["bonus"] == null)
                effects["bonus"] = Sequence("Maze_Bonus", new[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 0.075f, 0.66f, false);
            effects["wall"] = Load("HitWall");
            if (effects["wall"] == null)
                effects["wall"] = Sweep("Maze_Wall", 150f, 82f, 0.12f, 0.55f, true);
            effects["death"] = Load("PlayerDeath");
            if (effects["death"] == null)
                effects["death"] = Sequence("Maze_Death", new[] { 659.25f, 523.25f, 392f, 293.66f, 196f }, 0.13f, 0.74f, true);
            effects["win"] = Sequence("Maze_Win", new[] { 392f, 493.88f, 587.33f, 783.99f, 987.77f, 1174.66f }, 0.105f, 0.72f, false);
            effects["move"] = Load("PlayerMove");
            if (effects["move"] == null)
                effects["move"] = Sequence("Maze_Move", new[] { 220f }, 0.045f, 0.28f, true);
            initialized = true;
        }

        public void Play(string cue)
        {
            Initialize();
            if (string.IsNullOrEmpty(cue))
                return;

            if (string.Equals(cue, "start", StringComparison.OrdinalIgnoreCase))
            {
                lastCueTimes[cue] = Time.unscaledTime;
                stopped = false;
                if (introRoutine != null)
                    StopCoroutine(introRoutine);
                musicSource.Stop();
                musicSource.loop = false;
                musicSource.clip = intro;
                musicSource.Play();
                introPlaying = true;
                introRoutine = StartCoroutine(BeginLoopAfterIntro());
                return;
            }

            if (string.Equals(cue, "menu", StringComparison.OrdinalIgnoreCase))
            {
                lastCueTimes[cue] = Time.unscaledTime;
                stopped = false;
                introPlaying = false;
                if (introRoutine != null)
                {
                    StopCoroutine(introRoutine);
                    introRoutine = null;
                }
                musicSource.Stop();
                musicSource.clip = menuLoop;
                musicSource.loop = true;
                musicSource.Play();
                return;
            }

            AudioClip clip;
            if (effects.TryGetValue(cue, out clip))
            {
                lastCueTimes[cue] = Time.unscaledTime;
                effectsSource.PlayOneShot(clip);
            }
        }

        public bool IsCueCoolingDown(string cue)
        {
            if (string.IsNullOrEmpty(cue))
                return false;
            float playedAt;
            return lastCueTimes.TryGetValue(cue, out playedAt) && Time.unscaledTime - playedAt < 0.2f;
        }

        public void SetMood(bool scared)
        {
            Initialize();
            if (scaredMood == scared)
                return;
            scaredMood = scared;
            if (!stopped && !introPlaying)
                StartLoop();
        }

        public void SetDead(bool dead)
        {
            Initialize();
            if (deadMood == dead)
                return;
            deadMood = dead;
            if (!stopped && !introPlaying)
                StartLoop();
        }

        public void StopMusic()
        {
            Initialize();
            stopped = true;
            introPlaying = false;
            if (introRoutine != null)
            {
                StopCoroutine(introRoutine);
                introRoutine = null;
            }
            musicSource.Stop();
        }

        public void ResumeMusic()
        {
            Initialize();
            if (!stopped && musicSource.isPlaying)
                return;
            stopped = false;
            introPlaying = false;
            StartLoop();
        }

        IEnumerator BeginLoopAfterIntro()
        {
            yield return new WaitForSecondsRealtime(Mathf.Min(intro.length, 3f));
            introPlaying = false;
            introRoutine = null;
            if (!stopped)
                StartLoop();
        }

        void StartLoop()
        {
            musicSource.Stop();
            musicSource.clip = deadMood ? deadLoop : scaredMood ? scaredLoop : normalLoop;
            musicSource.loop = true;
            musicSource.Play();
        }

        static AudioClip Load(string name)
        {
            return Resources.Load<AudioClip>("Audio Clips/" + name);
        }

        static AudioClip Sequence(string name, float[] notes, float noteLength, float level, bool square)
        {
            float duration = Mathf.Max(0.03f, notes.Length * noteLength);
            int sampleCount = Mathf.CeilToInt(duration * SampleRate);
            float[] data = new float[sampleCount];
            int noteSamples = Mathf.Max(1, Mathf.RoundToInt(noteLength * SampleRate));

            for (int i = 0; i < sampleCount; i++)
            {
                int noteIndex = Mathf.Min(notes.Length - 1, i / noteSamples);
                float localTime = (i % noteSamples) / (float)SampleRate;
                float phase = localTime * notes[noteIndex] * Mathf.PI * 2f;
                float progress = (i % noteSamples) / (float)noteSamples;
                float attack = Mathf.Clamp01(progress / 0.08f);
                float release = Mathf.Clamp01((1f - progress) / 0.22f);
                float wave = square ? Mathf.Sign(Mathf.Sin(phase)) * 0.72f : Mathf.Sin(phase) + Mathf.Sin(phase * 2f) * 0.12f;
                data[i] = wave * Mathf.Min(attack, release) * level * 0.42f;
            }

            return Create(name, data);
        }

        static AudioClip Sweep(string name, float from, float to, float duration, float level, bool square)
        {
            int sampleCount = Mathf.CeilToInt(duration * SampleRate);
            float[] data = new float[sampleCount];
            float phase = 0f;
            for (int i = 0; i < sampleCount; i++)
            {
                float progress = i / (float)Mathf.Max(1, sampleCount - 1);
                float frequency = Mathf.Lerp(from, to, progress);
                phase += frequency * Mathf.PI * 2f / SampleRate;
                float envelope = Mathf.Sin(progress * Mathf.PI);
                float wave = square ? Mathf.Sign(Mathf.Sin(phase)) * 0.65f : Mathf.Sin(phase);
                data[i] = wave * envelope * level * 0.42f;
            }
            return Create(name, data);
        }

        static AudioClip MusicLoop(string name, bool scared)
        {
            float duration = scared ? 1.6f : 3.2f;
            int sampleCount = Mathf.CeilToInt(duration * SampleRate);
            float[] data = new float[sampleCount];
            float[] notes = scared
                ? new[] { 146.83f, 174.61f, 146.83f, 123.47f }
                : new[] { 196f, 246.94f, 293.66f, 246.94f, 220f, 261.63f, 329.63f, 261.63f };
            float noteLength = duration / notes.Length;
            int noteSamples = Mathf.Max(1, Mathf.RoundToInt(noteLength * SampleRate));

            for (int i = 0; i < sampleCount; i++)
            {
                int noteIndex = Mathf.Min(notes.Length - 1, i / noteSamples);
                float localTime = (i % noteSamples) / (float)SampleRate;
                float progress = (i % noteSamples) / (float)noteSamples;
                float frequency = notes[noteIndex];
                float phase = localTime * frequency * Mathf.PI * 2f;
                float gate = Mathf.Clamp01(progress / 0.05f) * Mathf.Clamp01((1f - progress) / 0.16f);
                float wave;
                if (scared)
                    wave = Mathf.Sign(Mathf.Sin(phase)) * 0.42f + Mathf.Sin(phase * 0.5f) * 0.18f;
                else
                    wave = Mathf.Sin(phase) + Mathf.Sin(phase * 2f) * 0.13f + Mathf.Sin(phase * 0.5f) * 0.16f;
                data[i] = wave * gate * (scared ? 0.12f : 0.1f);
            }
            return Create(name, data);
        }

        static AudioClip Create(string name, float[] data)
        {
            AudioClip clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}

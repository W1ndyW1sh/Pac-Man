using System;
using System.Collections.Generic;
using System.IO;
using CampusMaze;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CampusMazeProjectBuilder
{
    const string SpriteFolder = "Assets/Resources/Generated";
    const string AudioFolder = "Assets/Resources/Audio Clips";
    const string AnimatorFolder = "Assets/Animators";

    [MenuItem("Campus Maze/Build Complete Project")]
    public static void Build()
    {
        ExportSprites();
        ExportAudio();
        CreateAnimators();
        CreateStartScene();
        CreateGameScene();
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/StartScene.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/RecreatedLevel.unity", true)
        };
        PlayerSettings.productName = "Campus Maze";
        PlayerSettings.companyName = "UTS Student";
        PlayerSettings.defaultScreenWidth = 900;
        PlayerSettings.defaultScreenHeight = 900;
        AssetDatabase.SaveAssets();
        Debug.Log("CAMPUS MAZE BUILD COMPLETE");
    }

    static void ExportSprites()
    {
        if (AssetDatabase.IsValidFolder(SpriteFolder))
            AssetDatabase.DeleteAsset(SpriteFolder);
        Directory.CreateDirectory(SpriteFolder);
        MazeArt.ClearCache();
        List<string> paths = new List<string>();
        foreach (KeyValuePair<string, Sprite> pair in MazeArt.AllSprites())
        {
            string path = SpriteFolder + "/" + pair.Key + ".png";
            File.WriteAllBytes(path, pair.Value.texture.EncodeToPNG());
            paths.Add(path);
        }
        AssetDatabase.Refresh();
        for (int i = 0; i < paths.Count; i++)
        {
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(paths[i]);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 64f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.isReadable = true;
            importer.SaveAndReimport();
        }
        MazeArt.ClearCache();
    }

    static void ExportAudio()
    {
        Directory.CreateDirectory(AudioFolder);
        WriteTone("GameIntro", new[] { 392f, 523.25f, 659.25f, 783.99f, 1046.5f }, 0.8f);
        WriteTone("StartSceneMusic", new[] { 220f, 277.18f, 329.63f, 440f }, 3.2f);
        WriteTone("GhostNormalMusic", new[] { 196f, 246.94f, 293.66f, 246.94f }, 3.2f);
        WriteTone("GhostScaredMusic", new[] { 146.83f, 174.61f, 123.47f, 174.61f }, 1.6f);
        WriteTone("GhostDeadMusic", new[] { 329.63f, 246.94f, 164.81f }, 1.2f);
        WriteTone("PlayerMove", new[] { 220f }, 0.08f);
        WriteTone("EatPellet", new[] { 880f, 1174.66f }, 0.1f);
        WriteTone("EatGhost", new[] { 987.77f, 739.99f, 1108.73f }, 0.24f);
        WriteTone("EatBonus", new[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 0.32f);
        WriteTone("HitWall", new[] { 150f, 82f }, 0.16f);
        WriteTone("PlayerDeath", new[] { 659.25f, 523.25f, 392f, 293.66f, 196f }, 0.65f);
        AssetDatabase.Refresh();
    }

    static void WriteTone(string name, float[] notes, float duration)
    {
        const int sampleRate = 44100;
        int count = Mathf.CeilToInt(duration * sampleRate);
        byte[] data = new byte[count * 2];
        float noteDuration = duration / notes.Length;
        for (int i = 0; i < count; i++)
        {
            float time = i / (float)sampleRate;
            int note = Mathf.Min(notes.Length - 1, Mathf.FloorToInt(time / noteDuration));
            float local = time - note * noteDuration;
            float position = local / noteDuration;
            float envelope = Mathf.Clamp01(position / 0.06f) * Mathf.Clamp01((1f - position) / 0.18f);
            float sample = Mathf.Sin(local * notes[note] * Mathf.PI * 2f) * envelope * 0.32f;
            short value = (short)(sample * short.MaxValue);
            data[i * 2] = (byte)(value & 255);
            data[i * 2 + 1] = (byte)((value >> 8) & 255);
        }
        string path = AudioFolder + "/" + name + ".wav";
        using (BinaryWriter writer = new BinaryWriter(File.Create(path)))
        {
            writer.Write(new[] { 'R', 'I', 'F', 'F' });
            writer.Write(36 + data.Length);
            writer.Write(new[] { 'W', 'A', 'V', 'E' });
            writer.Write(new[] { 'f', 'm', 't', ' ' });
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write(sampleRate);
            writer.Write(sampleRate * 2);
            writer.Write((short)2);
            writer.Write((short)16);
            writer.Write(new[] { 'd', 'a', 't', 'a' });
            writer.Write(data.Length);
            writer.Write(data);
        }
    }

    static void CreateAnimators()
    {
        if (AssetDatabase.IsValidFolder(AnimatorFolder))
            AssetDatabase.DeleteAsset(AnimatorFolder);
        Directory.CreateDirectory(AnimatorFolder);
        CreatePacStudentAnimator();
        CreatePowerAnimator();
        for (int i = 0; i < 4; i++)
            CreateGhostAnimator(i);
        AssetDatabase.SaveAssets();
    }

    static void CreatePacStudentAnimator()
    {
        string[] names = { "Walking_Right", "Walking_Left", "Walking_Up", "Walking_Down", "Dead" };
        Sprite[][] frames =
        {
            Frames("Player_1_0_"), Frames("Player_-1_0_"), Frames("Player_0_1_"), Frames("Player_0_-1_"), Frames("PlayerDead_")
        };
        CreateController("PacStudentAnimator", names, frames);
    }

    static void CreatePowerAnimator()
    {
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(AnimatorFolder + "/PowerPelletAnimator.controller");
        AnimationClip clip = CreateClip("PowerPellet_Flash", new[] { LoadSprite("PowerPellet"), LoadSprite("Glow") }, 2f);
        controller.layers[0].stateMachine.AddState("Flashing").motion = clip;
    }

    static void CreateGhostAnimator(int ghost)
    {
        string[] directions = { "1_0", "-1_0", "0_1", "0_-1" };
        List<string> names = new List<string>();
        List<Sprite[]> frames = new List<Sprite[]>();
        string[] labels = { "Right", "Left", "Up", "Down" };
        for (int i = 0; i < directions.Length; i++)
        {
            names.Add("Walking_" + labels[i]);
            frames.Add(GhostFrames(ghost, directions[i], "False_False"));
        }
        for (int i = 0; i < directions.Length; i++)
        {
            names.Add("Scared_" + labels[i]);
            frames.Add(GhostFrames(ghost, directions[i], "True_False"));
        }
        names.Add("Recovering");
        frames.Add(new[] { GhostFrames(ghost, "1_0", "True_False")[0], GhostFrames(ghost, "1_0", "False_False")[0] });
        names.Add("Dead");
        frames.Add(GhostFrames(ghost, "1_0", "False_True"));
        CreateController("GhostAnimator_" + (ghost + 1), names.ToArray(), frames.ToArray());
    }

    static void CreateController(string name, string[] stateNames, Sprite[][] frames)
    {
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(AnimatorFolder + "/" + name + ".controller");
        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        AnimatorState[] states = new AnimatorState[stateNames.Length];
        for (int i = 0; i < stateNames.Length; i++)
        {
            states[i] = machine.AddState(stateNames[i]);
            states[i].motion = CreateClip(name + "_" + stateNames[i], frames[i], 2f);
        }
        machine.defaultState = states[0];
        for (int i = 0; i < states.Length; i++)
        {
            AnimatorStateTransition transition = states[i].AddTransition(states[(i + 1) % states.Length]);
            transition.hasExitTime = true;
            transition.exitTime = 3f;
            transition.hasFixedDuration = true;
            transition.duration = 0.05f;
        }
    }

    static AnimationClip CreateClip(string name, Sprite[] frames, float duration)
    {
        AnimationClip clip = new AnimationClip { name = name, frameRate = 8f };
        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[frames.Length + 1];
        for (int i = 0; i < frames.Length; i++)
            keys[i] = new ObjectReferenceKeyframe { time = duration * i / frames.Length, value = frames[i] };
        keys[keys.Length - 1] = new ObjectReferenceKeyframe { time = duration, value = frames[0] };
        EditorCurveBinding binding = EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        SerializedObject serialized = new SerializedObject(clip);
        SerializedProperty loop = serialized.FindProperty("m_AnimationClipSettings.m_LoopTime");
        if (loop != null) loop.boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(clip, AnimatorFolder + "/" + name + ".anim");
        return clip;
    }

    static Sprite[] Frames(string prefix)
    {
        return new[] { LoadSprite(prefix + "0"), LoadSprite(prefix + "1") };
    }

    static Sprite[] GhostFrames(int ghost, string direction, string state)
    {
        return new[]
        {
            LoadSprite("Ghost_" + ghost + "_" + direction + "_0_" + state),
            LoadSprite("Ghost_" + ghost + "_" + direction + "_1_" + state)
        };
    }

    static Sprite LoadSprite(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFolder + "/" + name + ".png");
    }

    static void CreateStartScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject menu = new GameObject("StartScene Controller");
        menu.AddComponent<MainMenuView>();
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/StartScene.unity");
    }

    static void CreateGameScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject controller = new GameObject("Game Controller");
        MazeGame game = controller.AddComponent<MazeGame>();
        LevelGenerator level = controller.AddComponent<LevelGenerator>();
        TextAsset map = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Resources/PacMan Level Map.csv");
        level.mapCsv = map;
        SerializedObject serialized = new SerializedObject(game);
        serialized.FindProperty("levelMap").objectReferenceValue = map;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        level.Generate();
        level.levelRoot.name = "Level01";
        CreateGallery();
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/RecreatedLevel.unity");
    }

    static void CreateGallery()
    {
        GameObject gallery = new GameObject("Visual Assets Preview");
        CreatePreview(gallery.transform, "PacStudent Animation Preview", new Vector3(-4f, -15.5f, 0f), "PacStudentAnimator");
        CreatePreview(gallery.transform, "Power Pellet Animation Preview", new Vector3(-2f, -15.5f, 0f), "PowerPelletAnimator");
        for (int i = 0; i < 4; i++)
            CreatePreview(gallery.transform, "Ghost " + (i + 1) + " Animation Preview", new Vector3(i * 2f, -15.5f, 0f), "GhostAnimator_" + (i + 1));
    }

    static void CreatePreview(Transform parent, string name, Vector3 position, string controllerName)
    {
        GameObject preview = new GameObject(name);
        preview.transform.SetParent(parent);
        preview.transform.position = position;
        SpriteRenderer renderer = preview.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = 30;
        Animator animator = preview.AddComponent<Animator>();
        animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimatorFolder + "/" + controllerName + ".controller");
    }
}

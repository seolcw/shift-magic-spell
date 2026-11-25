using System.Collections.Generic;
using ShiftMagicSpell.Audio;
using UnityEditor;
using UnityEngine;

namespace ShiftMagicSpell.Editor
{
    /// <summary>
    /// 소리 목록(SoundLibrary.asset)과 Audio.prefab을 만든다. 이미 있으면 빠진 이벤트만 기본값으로 채우고,
    /// 인스펙터에서 바꾼 값은 그대로 둔다. 배치 모드: -executeMethod ShiftMagicSpell.Editor.AudioSetupUtils.SetupAudio
    /// </summary>
    public static class AudioSetupUtils
    {
        private const string SFX_DIR = "Assets/Audio/Sfx/";
        private const string MUSIC_PATH = "Assets/Audio/Music/ChiptuneGrooving.mp3";
        private const string LIBRARY_PATH = "Assets/Audio/SoundLibrary.asset";
        public const string PREFAB_PATH = "Assets/Prefabs/Audio.prefab";

        // 이벤트 → (클립 파일, 볼륨, 피치, 피치 랜덤폭, 최소 간격). Assets/Audio/Sfx/README.md의 표와 같다.
        private static readonly (SoundKey Key, string Clip, float Volume, float Pitch, float Jitter, float MinInterval)[] Defaults =
        {
            (SoundKey.Start, "Move", 0.7f, 1.5f, 0f, 0f),
            (SoundKey.Move, "Move", 0.15f, 1f, 0.05f, 0.05f),
            (SoundKey.Rotate, "Rotate", 0.45f, 1f, 0.05f, 0f),
            (SoundKey.Deny, "Deny", 0.5f, 1f, 0f, 0.1f),
            (SoundKey.LetterMatch, "Coin", 0.25f, 1.5f, 0f, 0f),
            (SoundKey.WordComplete, "Coin", 0.7f, 1f, 0f, 0f),
            (SoundKey.Store, "Store", 0.55f, 1f, 0f, 0f),
            (SoundKey.SpellFire, "Laser", 0.55f, 0.6f, 0.05f, 0f),
            (SoundKey.SpellBolt, "Thunder", 0.7f, 1f, 0.05f, 0f),
            (SoundKey.SpellBeam, "Laser", 0.6f, 1f, 0.05f, 0f),
            (SoundKey.SpellMagic, "Sparkle", 0.6f, 1f, 0.05f, 0f),
            (SoundKey.SpellShift, "Finisher", 0.8f, 1f, 0f, 0f),
            (SoundKey.SpellImpact, "Explosion", 0.6f, 1f, 0.08f, 0f),
            (SoundKey.Combo, "Sparkle", 0.6f, 1.35f, 0f, 0f),
            (SoundKey.PlayerHurt, "Hurt", 0.7f, 1f, 0.05f, 0f),
            (SoundKey.HandWarn, "Warn", 0.35f, 1f, 0f, 0.15f),
            (SoundKey.HandClick, "Click", 0.6f, 1f, 0.08f, 0f),
            (SoundKey.HandSlam, "Explosion", 0.55f, 0.65f, 0.05f, 0f),
            (SoundKey.BlockBreak, "Explosion", 0.5f, 1.7f, 0.05f, 0f),
            (SoundKey.Warp, "Warp", 0.5f, 1f, 0f, 0f),
            (SoundKey.PhaseClear, "Fanfare", 0.7f, 1f, 0f, 0f),
            (SoundKey.Win, "Fanfare", 0.8f, 0.85f, 0f, 0f),
            (SoundKey.Death, "Death", 0.7f, 1f, 0f, 0f),
            (SoundKey.EndingCrack, "Crack", 0.6f, 1f, 0.05f, 0f),
            (SoundKey.EndingCrackBig, "Crack", 0.75f, 0.8f, 0f, 0f),
            (SoundKey.EndingPop1, "Pop", 0.55f, 1f, 0f, 0f),
            (SoundKey.EndingPop2, "Pop", 0.55f, 1.15f, 0f, 0f),
            (SoundKey.EndingPop3, "Pop", 0.55f, 1.3f, 0f, 0f),
            (SoundKey.EndingZip, "Zip", 0.6f, 1f, 0f, 0f),
            (SoundKey.EndingType, "Move", 0.12f, 1.6f, 0.08f, 0.03f),
        };

        [MenuItem("Tools/Shift Magic Spell/Setup Audio (Library, Prefab)")]
        public static void SetupAudio()
        {
            SoundLibrary library = CreateOrUpdateLibrary();
            CreatePrefab(library);
            AssetDatabase.SaveAssets();
            Debug.Log($"[INFO] AudioSetupUtils::SetupAudio() - Audio library and prefab ready. Details: [Library={LIBRARY_PATH}, Prefab={PREFAB_PATH}, Entries={library.Entries.Count}]");
        }

        private static SoundLibrary CreateOrUpdateLibrary()
        {
            var library = AssetDatabase.LoadAssetAtPath<SoundLibrary>(LIBRARY_PATH);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<SoundLibrary>();
                AssetDatabase.CreateAsset(library, LIBRARY_PATH);
            }

            if (library.Music == null)
            {
                library.Music = AssetDatabase.LoadAssetAtPath<AudioClip>(MUSIC_PATH);
            }

            var existing = new HashSet<SoundKey>();
            foreach (SoundEntry entry in library.Entries)
            {
                existing.Add(entry.Key);
            }

            foreach (var item in Defaults)
            {
                if (existing.Contains(item.Key))
                {
                    continue;
                }

                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{SFX_DIR}{item.Clip}.wav");
                if (clip == null)
                {
                    Debug.LogError($"[ERROR] AudioSetupUtils::CreateOrUpdateLibrary() - Clip not found. Details: [Key={item.Key}, Clip={item.Clip}]");
                }

                library.Entries.Add(new SoundEntry
                {
                    Key = item.Key,
                    Clip = clip,
                    Volume = item.Volume,
                    Pitch = item.Pitch,
                    PitchJitter = item.Jitter,
                    MinInterval = item.MinInterval,
                });
            }

            EditorUtility.SetDirty(library);
            return library;
        }

        private static void CreatePrefab(SoundLibrary library)
        {
            var root = new GameObject("Audio");
            try
            {
                AudioSource music = root.AddComponent<AudioSource>();
                music.playOnAwake = false;
                music.loop = true;
                SoundManager manager = root.AddComponent<SoundManager>();
                var serialized = new SerializedObject(manager);
                serialized.FindProperty("_library").objectReferenceValue = library;
                serialized.FindProperty("_musicSource").objectReferenceValue = music;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}

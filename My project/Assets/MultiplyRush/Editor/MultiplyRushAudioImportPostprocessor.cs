#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace MultiplyRush.Editor
{
    public sealed class MultiplyRushAudioImportPostprocessor : AssetPostprocessor
    {
        private const string GameplayMusicPath = "Assets/Resources/MultiplyRush/Music/Gameplay";
        private static bool _ranInitialValidation;

        [InitializeOnLoadMethod]
        private static void RunInitialValidation()
        {
            if (_ranInitialValidation)
            {
                return;
            }

            _ranInitialValidation = true;
            EditorApplication.delayCall += () => EnsureGameplayMusicImportSettings(false);
        }

        [MenuItem("Multiply Rush/Audio/Reimport Gameplay Music")]
        private static void ReimportGameplayMusic()
        {
            EnsureGameplayMusicImportSettings(true);
            AssetDatabase.Refresh();
            Debug.Log("Multiply Rush: gameplay music import settings refreshed.");
        }

        private void OnPreprocessAudio()
        {
            if (string.IsNullOrEmpty(assetPath) ||
                !assetPath.StartsWith(GameplayMusicPath + "/", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var importer = assetImporter as AudioImporter;
            if (importer == null)
            {
                return;
            }

            ApplyImportSettings(importer);
        }

        private static void EnsureGameplayMusicImportSettings(bool forceReimport)
        {
            if (!AssetDatabase.IsValidFolder(GameplayMusicPath))
            {
                return;
            }

            var assetGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { GameplayMusicPath });
            for (var i = 0; i < assetGuids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(assetGuids[i]);
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                var importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer == null)
                {
                    continue;
                }

                var changed = ApplyImportSettings(importer);
                if (changed || forceReimport)
                {
                    importer.SaveAndReimport();
                }
            }
        }

        private static bool ApplyImportSettings(AudioImporter importer)
        {
            if (importer == null)
            {
                return false;
            }

            var changed = false;
            if (importer.forceToMono)
            {
                importer.forceToMono = false;
                changed = true;
            }

            if (!importer.loadInBackground)
            {
                importer.loadInBackground = true;
                changed = true;
            }

            if (importer.ambisonic)
            {
                importer.ambisonic = false;
                changed = true;
            }

            var sampleSettings = importer.defaultSampleSettings;
            var target = sampleSettings;
            target.loadType = AudioClipLoadType.CompressedInMemory;
            target.compressionFormat = AudioCompressionFormat.Vorbis;
            target.quality = 0.72f;
            target.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            target.preloadAudioData = true;

            if (sampleSettings.loadType != target.loadType ||
                sampleSettings.compressionFormat != target.compressionFormat ||
                Mathf.Abs(sampleSettings.quality - target.quality) > 0.001f ||
                sampleSettings.sampleRateSetting != target.sampleRateSetting ||
                sampleSettings.preloadAudioData != target.preloadAudioData)
            {
                importer.defaultSampleSettings = target;
                changed = true;
            }

            return changed;
        }
    }
}
#endif

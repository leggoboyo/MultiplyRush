#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace MultiplyRush.Editor
{
    public sealed class MultiplyRushAudioImportPostprocessor : AssetPostprocessor
    {
        private const string GameplayMusicPath = "Assets/Resources/MultiplyRush/Music/Gameplay/";

        private void OnPreprocessAudio()
        {
            if (string.IsNullOrEmpty(assetPath) ||
                !assetPath.StartsWith(GameplayMusicPath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var importer = assetImporter as AudioImporter;
            if (importer == null)
            {
                return;
            }

            importer.forceToMono = false;
            importer.loadInBackground = true;
            importer.ambisonic = false;

            var sampleSettings = importer.defaultSampleSettings;
            sampleSettings.loadType = AudioClipLoadType.Streaming;
            sampleSettings.compressionFormat = AudioCompressionFormat.Vorbis;
            sampleSettings.quality = 0.58f;
            sampleSettings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            sampleSettings.preloadAudioData = true;
            importer.defaultSampleSettings = sampleSettings;
        }
    }
}
#endif

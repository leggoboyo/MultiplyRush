#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class MultiplyRushReleaseTools
{
    private const string DocsFolder = "Assets/MultiplyRush/Docs";
    private const string AuditReportPath = DocsFolder + "/ReleaseAudit.md";
    private const string MainMenuScenePath = "Assets/MultiplyRush/Scenes/MainMenu.unity";
    private const string GameScenePath = "Assets/MultiplyRush/Scenes/Game.unity";

    [MenuItem("Multiply Rush/Release/Prepare iOS Release Candidate")]
    public static void PrepareIosReleaseCandidate()
    {
        ApplyIosPublishDefaults();
        RunReleaseAudit();
    }

    [MenuItem("Multiply Rush/Release/Apply iOS Publish Defaults")]
    public static void ApplyIosPublishDefaults()
    {
        EnsureDocsFolder();
        ApplyBuildSceneDefaults();

        PlayerSettings.companyName = string.IsNullOrWhiteSpace(PlayerSettings.companyName)
            ? "MultiplyRush"
            : PlayerSettings.companyName;
        PlayerSettings.productName = "Multiply Rush";
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.MTRendering = true;
        PlayerSettings.stripEngineCode = true;
        PlayerSettings.runInBackground = false;
        PlayerSettings.iOS.targetOSVersionString = "13.0";
        PlayerSettings.iOS.appleEnableAutomaticSigning = true;
        PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneAndiPad;

        SetScriptingBackendIos(ScriptingImplementation.IL2CPP);
        SetManagedStrippingLevelIos(ManagedStrippingLevel.Medium);
        SetApiCompatibilityLevelIos(ApiCompatibilityLevel.NET_Standard);

        // iOS requires ARM64 for App Store uploads.
        PlayerSettings.SetArchitecture(BuildTargetGroup.iOS, 1);

        if (string.IsNullOrWhiteSpace(PlayerSettings.bundleVersion))
        {
            PlayerSettings.bundleVersion = "1.0.0";
        }

        if (string.IsNullOrWhiteSpace(PlayerSettings.iOS.buildNumber))
        {
            PlayerSettings.iOS.buildNumber = "1";
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Multiply Rush: Applied iOS publish defaults.");
    }

    [MenuItem("Multiply Rush/Release/Run Release Audit")]
    public static void RunReleaseAudit()
    {
        EnsureDocsFolder();

        var report = new StringBuilder(4096);
        var issues = new List<AuditItem>(64);

        report.AppendLine("# Multiply Rush Release Audit");
        report.AppendLine();
        report.AppendLine("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        report.AppendLine();

        AuditBuildScenes(issues);
        AuditIosPlayerSettings(issues);
        AuditRuntimeSafety(issues);
        AuditOfflineCompliance(issues);

        var passCount = 0;
        var warnCount = 0;
        var failCount = 0;
        for (var i = 0; i < issues.Count; i++)
        {
            switch (issues[i].severity)
            {
                case AuditSeverity.Pass:
                    passCount++;
                    break;
                case AuditSeverity.Warning:
                    warnCount++;
                    break;
                case AuditSeverity.Fail:
                    failCount++;
                    break;
            }
        }

        report.AppendLine("## Summary");
        report.AppendLine();
        report.AppendLine("- Pass: " + passCount);
        report.AppendLine("- Warning: " + warnCount);
        report.AppendLine("- Fail: " + failCount);
        report.AppendLine();

        report.AppendLine("## Checks");
        report.AppendLine();
        for (var i = 0; i < issues.Count; i++)
        {
            var item = issues[i];
            var icon = item.severity == AuditSeverity.Pass
                ? "[PASS]"
                : item.severity == AuditSeverity.Warning
                    ? "[WARN]"
                    : "[FAIL]";
            report.AppendLine("- " + icon + " " + item.title + " - " + item.details);
        }

        report.AppendLine();
        report.AppendLine("## Manual Submission Steps (Apple)");
        report.AppendLine();
        report.AppendLine("1. Confirm Apple Developer account is active and App Store Connect access works.");
        report.AppendLine("2. In Unity, run Multiply Rush > Release > Prepare iOS Release Candidate.");
        report.AppendLine("3. Set the final bundle identifier + app version/build number.");
        report.AppendLine("4. Add final app icons and launch screen assets in Player Settings.");
        report.AppendLine("5. Build iOS project, archive from Xcode with automatic signing (or your cert/profile).");
        report.AppendLine("6. Upload to App Store Connect, fill metadata/screenshots/privacy/age rating.");
        report.AppendLine("7. Submit for review and answer reviewer follow-up.");

        File.WriteAllText(AuditReportPath, report.ToString());
        AssetDatabase.Refresh();

        if (failCount > 0)
        {
            EditorUtility.DisplayDialog(
                "Multiply Rush Release Audit",
                "Audit completed with failures. Open Assets/MultiplyRush/Docs/ReleaseAudit.md.",
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog(
                "Multiply Rush Release Audit",
                "Audit completed. Open Assets/MultiplyRush/Docs/ReleaseAudit.md.",
                "OK");
        }

        Debug.Log("Multiply Rush release audit written to: " + AuditReportPath);
    }

    [MenuItem("Multiply Rush/Release/Open Last Audit Report")]
    public static void OpenLastAuditReport()
    {
        EnsureDocsFolder();
        if (!File.Exists(AuditReportPath))
        {
            EditorUtility.DisplayDialog(
                "Multiply Rush Release Audit",
                "No audit report found yet. Run 'Multiply Rush/Release/Run Release Audit' first.",
                "OK");
            return;
        }

        var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(AuditReportPath);
        if (asset != null)
        {
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            return;
        }

        EditorUtility.RevealInFinder(AuditReportPath);
    }

    private static void AuditBuildScenes(List<AuditItem> issues)
    {
        var hasMainMenuAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuScenePath) != null;
        var hasGameAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(GameScenePath) != null;

        issues.Add(new AuditItem(
            hasMainMenuAsset ? AuditSeverity.Pass : AuditSeverity.Fail,
            "MainMenu Scene Asset",
            hasMainMenuAsset ? MainMenuScenePath : "Missing scene asset: " + MainMenuScenePath));

        issues.Add(new AuditItem(
            hasGameAsset ? AuditSeverity.Pass : AuditSeverity.Fail,
            "Game Scene Asset",
            hasGameAsset ? GameScenePath : "Missing scene asset: " + GameScenePath));

        var hasMainMenu = false;
        var hasGame = false;
        var enabledCount = 0;
        var firstEnabledScene = string.Empty;
        var scenes = EditorBuildSettings.scenes;
        for (var i = 0; i < scenes.Length; i++)
        {
            if (!scenes[i].enabled)
            {
                continue;
            }

            if (string.IsNullOrEmpty(firstEnabledScene))
            {
                firstEnabledScene = scenes[i].path.Replace('\\', '/');
            }

            enabledCount++;
            var path = scenes[i].path.Replace('\\', '/');
            hasMainMenu |= path.EndsWith("/MainMenu.unity", StringComparison.Ordinal);
            hasGame |= path.EndsWith("/Game.unity", StringComparison.Ordinal);
        }

        issues.Add(new AuditItem(
            enabledCount >= 2 ? AuditSeverity.Pass : AuditSeverity.Fail,
            "Build Scenes",
            "Enabled scenes: " + enabledCount + ". Expected at least MainMenu + Game."));

        issues.Add(new AuditItem(
            hasMainMenu ? AuditSeverity.Pass : AuditSeverity.Fail,
            "MainMenu Scene Included",
            hasMainMenu ? "MainMenu scene is in build settings." : "MainMenu scene missing from build settings."));

        issues.Add(new AuditItem(
            hasGame ? AuditSeverity.Pass : AuditSeverity.Fail,
            "Game Scene Included",
            hasGame ? "Game scene is in build settings." : "Game scene missing from build settings."));

        var startsOnMenu = firstEnabledScene.EndsWith("/MainMenu.unity", StringComparison.Ordinal);
        issues.Add(new AuditItem(
            startsOnMenu ? AuditSeverity.Pass : AuditSeverity.Warning,
            "Startup Scene",
            startsOnMenu ? "MainMenu is first enabled scene." : "First enabled scene should be MainMenu."));
    }

    private static void AuditIosPlayerSettings(List<AuditItem> issues)
    {
        var bundle = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.iOS);
        var hasRealBundle = !string.IsNullOrWhiteSpace(bundle) &&
                            !bundle.Equals("com.Company.ProductName", StringComparison.OrdinalIgnoreCase);
        issues.Add(new AuditItem(
            hasRealBundle ? AuditSeverity.Pass : AuditSeverity.Warning,
            "Bundle Identifier",
            hasRealBundle ? bundle : "Set a unique iOS bundle identifier before submission."));

        issues.Add(new AuditItem(
            PlayerSettings.defaultInterfaceOrientation == UIOrientation.Portrait ? AuditSeverity.Pass : AuditSeverity.Warning,
            "Portrait Orientation",
            "Current orientation: " + PlayerSettings.defaultInterfaceOrientation));

        issues.Add(new AuditItem(
            PlayerSettings.runInBackground ? AuditSeverity.Warning : AuditSeverity.Pass,
            "Run In Background",
            PlayerSettings.runInBackground
                ? "Enabled. Recommended OFF for mobile battery/runtime safety."
                : "Disabled."));

        issues.Add(new AuditItem(
            GetScriptingBackendIos() == ScriptingImplementation.IL2CPP ? AuditSeverity.Pass : AuditSeverity.Fail,
            "iOS Scripting Backend",
            "Current backend: " + GetScriptingBackendIos()));

        var architecture = PlayerSettings.GetArchitecture(BuildTargetGroup.iOS);
        issues.Add(new AuditItem(
            architecture == 1 ? AuditSeverity.Pass : AuditSeverity.Fail,
            "iOS Architecture",
            architecture == 1 ? "ARM64" : "Architecture value is " + architecture + " (App Store requires ARM64)."));

        issues.Add(new AuditItem(
            GetManagedStrippingLevelIos() >= ManagedStrippingLevel.Medium ? AuditSeverity.Pass : AuditSeverity.Warning,
            "Managed Stripping",
            "Current stripping: " + GetManagedStrippingLevelIos()));

        issues.Add(new AuditItem(
            GetApiCompatibilityLevelIos() == ApiCompatibilityLevel.NET_Standard ? AuditSeverity.Pass : AuditSeverity.Warning,
            "API Compatibility",
            "Current API compatibility: " + GetApiCompatibilityLevelIos()));

        issues.Add(new AuditItem(
            PlayerSettings.stripEngineCode ? AuditSeverity.Pass : AuditSeverity.Warning,
            "Strip Engine Code",
            "Strip Engine Code: " + PlayerSettings.stripEngineCode));

        issues.Add(new AuditItem(
            !string.IsNullOrWhiteSpace(PlayerSettings.bundleVersion) ? AuditSeverity.Pass : AuditSeverity.Warning,
            "App Version",
            "Current version: " + PlayerSettings.bundleVersion));

        issues.Add(new AuditItem(
            !string.IsNullOrWhiteSpace(PlayerSettings.iOS.buildNumber) ? AuditSeverity.Pass : AuditSeverity.Warning,
            "iOS Build Number",
            "Current build number: " + PlayerSettings.iOS.buildNumber));

        issues.Add(new AuditItem(
            PlayerSettings.iOS.targetOSVersionString == "13.0" ? AuditSeverity.Pass : AuditSeverity.Warning,
            "Minimum iOS Version",
            "Current target OS version: " + PlayerSettings.iOS.targetOSVersionString));

        issues.Add(new AuditItem(
            PlayerSettings.iOS.targetDevice == iOSTargetDevice.iPhoneAndiPad ? AuditSeverity.Pass : AuditSeverity.Warning,
            "Target Devices",
            "Current iOS target devices: " + PlayerSettings.iOS.targetDevice));
    }

    private static void AuditRuntimeSafety(List<AuditItem> issues)
    {
        var requiredScripts = new[]
        {
            "Assets/MultiplyRush/Scripts/Core/RuntimeBootstrap.cs",
            "Assets/MultiplyRush/Scripts/Core/AppLifecycleController.cs",
            "Assets/MultiplyRush/Scripts/Core/HapticsDirector.cs",
            "Assets/MultiplyRush/Scripts/Audio/AudioDirector.cs",
            "Assets/MultiplyRush/Scripts/Core/DeviceRuntimeSettings.cs"
        };

        for (var i = 0; i < requiredScripts.Length; i++)
        {
            var path = requiredScripts[i];
            var exists = AssetDatabase.LoadAssetAtPath<MonoScript>(path) != null;
            issues.Add(new AuditItem(
                exists ? AuditSeverity.Pass : AuditSeverity.Fail,
                "Runtime Script: " + Path.GetFileName(path),
                exists ? "Found." : "Missing required script: " + path));
        }
    }

    private static void AuditOfflineCompliance(List<AuditItem> issues)
    {
        var root = Directory.GetParent(Application.dataPath);
        if (root == null)
        {
            issues.Add(new AuditItem(AuditSeverity.Fail, "Project Root", "Could not resolve project root path."));
            return;
        }

        var manifestPath = Path.Combine(root.FullName, "Packages", "manifest.json");
        if (!File.Exists(manifestPath))
        {
            issues.Add(new AuditItem(AuditSeverity.Fail, "Packages Manifest", "manifest.json not found."));
            return;
        }

        var manifest = File.ReadAllText(manifestPath);
        var bannedPackages = new[]
        {
            "com.unity.ads",
            "com.unity.services.analytics",
            "com.unity.purchasing",
            "com.unity.modules.unitywebrequest",
            "com.unity.services.core"
        };

        var foundBanned = new List<string>();
        for (var i = 0; i < bannedPackages.Length; i++)
        {
            if (manifest.IndexOf(bannedPackages[i], StringComparison.OrdinalIgnoreCase) >= 0)
            {
                foundBanned.Add(bannedPackages[i]);
            }
        }

        issues.Add(new AuditItem(
            foundBanned.Count == 0 ? AuditSeverity.Pass : AuditSeverity.Warning,
            "Package Compliance",
            foundBanned.Count == 0
                ? "No obvious ads/analytics/IAP packages found in manifest dependencies scan."
                : "Review/remove these packages if unused: " + string.Join(", ", foundBanned)));

        var scriptsRoot = Path.Combine(Application.dataPath, "MultiplyRush", "Scripts");
        var networkHits = ScanForPatterns(scriptsRoot, new[]
        {
            "UnityWebRequest",
            "System.Net",
            "HttpClient",
            "WebSocket",
            "Socket",
            "Advertisement",
            "Purchasing",
            "AnalyticsService",
            "RemoteConfig",
            "Firebase"
        });

        issues.Add(new AuditItem(
            networkHits.Count == 0 ? AuditSeverity.Pass : AuditSeverity.Warning,
            "Offline Code Scan",
            networkHits.Count == 0
                ? "No obvious networking/ads/analytics API usage found in Assets/MultiplyRush/Scripts."
                : "Review references: " + string.Join(" | ", networkHits)));
    }

    private static List<string> ScanForPatterns(string directory, string[] patterns)
    {
        var hits = new List<string>(32);
        if (!Directory.Exists(directory))
        {
            return hits;
        }

        var files = Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories);
        for (var f = 0; f < files.Length; f++)
        {
            var path = files[f];
            var content = File.ReadAllText(path);
            for (var p = 0; p < patterns.Length; p++)
            {
                var pattern = patterns[p];
                if (content.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                var relativePath = "Assets" + path.Replace(Application.dataPath, string.Empty).Replace('\\', '/');
                hits.Add(relativePath + " -> " + pattern);
                if (hits.Count >= 14)
                {
                    return hits;
                }

                break;
            }
        }

        return hits;
    }

    private static void EnsureDocsFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/MultiplyRush/Docs"))
        {
            AssetDatabase.CreateFolder("Assets/MultiplyRush", "Docs");
        }
    }

    private static void ApplyBuildSceneDefaults()
    {
        var hasMainMenu = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuScenePath) != null;
        var hasGame = AssetDatabase.LoadAssetAtPath<SceneAsset>(GameScenePath) != null;
        if (!hasMainMenu || !hasGame)
        {
            Debug.LogWarning("Multiply Rush: could not apply build scenes defaults because one or more scenes are missing.");
            return;
        }

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MainMenuScenePath, true),
            new EditorBuildSettingsScene(GameScenePath, true)
        };
    }

    private static ScriptingImplementation GetScriptingBackendIos()
    {
#if UNITY_2021_2_OR_NEWER
        return PlayerSettings.GetScriptingBackend(NamedBuildTarget.iOS);
#else
        return PlayerSettings.GetScriptingBackend(BuildTargetGroup.iOS);
#endif
    }

    private static void SetScriptingBackendIos(ScriptingImplementation implementation)
    {
#if UNITY_2021_2_OR_NEWER
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, implementation);
#else
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.iOS, implementation);
#endif
    }

    private static ManagedStrippingLevel GetManagedStrippingLevelIos()
    {
#if UNITY_2021_2_OR_NEWER
        return PlayerSettings.GetManagedStrippingLevel(NamedBuildTarget.iOS);
#else
        return PlayerSettings.GetManagedStrippingLevel(BuildTargetGroup.iOS);
#endif
    }

    private static void SetManagedStrippingLevelIos(ManagedStrippingLevel level)
    {
#if UNITY_2021_2_OR_NEWER
        PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.iOS, level);
#else
        PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.iOS, level);
#endif
    }

    private static void SetApiCompatibilityLevelIos(ApiCompatibilityLevel level)
    {
#if UNITY_2021_2_OR_NEWER
        PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.iOS, level);
#else
        PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.iOS, level);
#endif
    }

    private static ApiCompatibilityLevel GetApiCompatibilityLevelIos()
    {
#if UNITY_2021_2_OR_NEWER
        return PlayerSettings.GetApiCompatibilityLevel(NamedBuildTarget.iOS);
#else
        return PlayerSettings.GetApiCompatibilityLevel(BuildTargetGroup.iOS);
#endif
    }

    private readonly struct AuditItem
    {
        public readonly AuditSeverity severity;
        public readonly string title;
        public readonly string details;

        public AuditItem(AuditSeverity severity, string title, string details)
        {
            this.severity = severity;
            this.title = title;
            this.details = details;
        }
    }

    private enum AuditSeverity
    {
        Pass,
        Warning,
        Fail
    }
}
#endif

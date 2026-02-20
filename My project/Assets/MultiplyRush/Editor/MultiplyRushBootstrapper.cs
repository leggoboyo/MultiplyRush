#if UNITY_EDITOR
using System.IO;
using MultiplyRush;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MultiplyRushBootstrapper
{
    private const string Root = "Assets/MultiplyRush";
    private const string PrefabsFolder = Root + "/Prefabs";
    private const string ScenesFolder = Root + "/Scenes";
    private const string MaterialsFolder = Root + "/Art/Materials";

    [MenuItem("Multiply Rush/Step 1/Bootstrap Project")]
    public static void BootstrapProject()
    {
        EnsureFolders();

        var trackMaterial = CreateOrUpdateMaterial(MaterialsFolder + "/M_Track.mat", new Color(0.18f, 0.22f, 0.29f));
        var crowdMaterial = CreateOrUpdateMaterial(MaterialsFolder + "/M_Crowd.mat", new Color(0.18f, 0.62f, 1f));
        var enemyMaterial = CreateOrUpdateMaterial(MaterialsFolder + "/M_Enemy.mat", new Color(0.95f, 0.28f, 0.34f));
        var gateBodyMaterial = CreateOrUpdateMaterial(MaterialsFolder + "/M_GateBody.mat", new Color(0.11f, 0.14f, 0.2f));
        var gatePanelMaterial = CreateOrUpdateMaterial(MaterialsFolder + "/M_GatePanel.mat", new Color(0.82f, 0.84f, 0.88f));
        var finishMaterial = CreateOrUpdateMaterial(MaterialsFolder + "/M_Finish.mat", new Color(0.95f, 0.84f, 0.28f));

        var soldierUnitPrefab = CreateSoldierUnitPrefab(crowdMaterial);
        var enemyUnitPrefab = CreateEnemyUnitPrefab(enemyMaterial);
        var playerCrowdPrefab = CreatePlayerCrowdPrefab(soldierUnitPrefab, crowdMaterial);
        var gatePrefab = CreateGatePrefab(gateBodyMaterial, gatePanelMaterial);
        var enemyPrefab = CreateEnemyPrefab(enemyUnitPrefab);
        var finishPrefab = CreateFinishPrefab(enemyPrefab, finishMaterial);

        var gameScenePath = CreateGameScene(playerCrowdPrefab, gatePrefab, finishPrefab, trackMaterial);
        var mainMenuScenePath = CreateMainMenuScene();

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(mainMenuScenePath, true),
            new EditorBuildSettingsScene(gameScenePath, true)
        };

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Multiply Rush Bootstrap",
            "Step 1 bootstrap complete. Scenes and prefabs are ready.\n\nOpen Assets/MultiplyRush/Scenes/MainMenu.unity and press Play.",
            "OK");
    }

    private static void EnsureFolders()
    {
        CreateFolderIfMissing("Assets", "MultiplyRush");
        CreateFolderIfMissing(Root, "Art");
        CreateFolderIfMissing(Root + "/Art", "Materials");
        CreateFolderIfMissing(Root, "Prefabs");
        CreateFolderIfMissing(Root, "Scenes");
        CreateFolderIfMissing(Root, "Scripts");
    }

    private static void CreateFolderIfMissing(string parent, string name)
    {
        var path = parent + "/" + name;
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, name);
        }
    }

    private static Material CreateOrUpdateMaterial(string path, Color color)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", 0.24f);
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    private static GameObject CreateSoldierUnitPrefab(Material material)
    {
        var root = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        root.name = "SoldierUnit";
        root.transform.localScale = new Vector3(0.35f, 0.5f, 0.35f);

        var collider = root.GetComponent<Collider>();
        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }

        ApplyMaterial(root, material);
        return SaveAsPrefab(root, PrefabsFolder + "/SoldierUnit.prefab");
    }

    private static GameObject CreateEnemyUnitPrefab(Material material)
    {
        var root = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        root.name = "EnemyUnit";
        root.transform.localScale = new Vector3(0.35f, 0.5f, 0.35f);

        var collider = root.GetComponent<Collider>();
        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }

        ApplyMaterial(root, material);
        return SaveAsPrefab(root, PrefabsFolder + "/EnemyUnit.prefab");
    }

    private static GameObject CreatePlayerCrowdPrefab(GameObject soldierUnitPrefab, Material crowdMaterial)
    {
        var root = new GameObject("PlayerCrowd");
        root.AddComponent<TouchDragInput>();

        var collider = root.AddComponent<CapsuleCollider>();
        collider.center = new Vector3(0f, 0.9f, -1.2f);
        collider.radius = 1.35f;
        collider.height = 2.2f;

        var body = root.AddComponent<Rigidbody>();
        body.useGravity = false;
        body.isKinematic = true;

        var crowdController = root.AddComponent<CrowdController>();

        var leader = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        leader.name = "LeaderVisual";
        leader.transform.SetParent(root.transform, false);
        leader.transform.localPosition = new Vector3(0f, 0.55f, 0f);
        leader.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
        ApplyMaterial(leader, crowdMaterial);
        var leaderCollider = leader.GetComponent<Collider>();
        if (leaderCollider != null)
        {
            Object.DestroyImmediate(leaderCollider);
        }

        var formationRoot = new GameObject("FormationRoot").transform;
        formationRoot.SetParent(root.transform, false);

        crowdController.formationRoot = formationRoot;
        crowdController.soldierUnitPrefab = soldierUnitPrefab;
        crowdController.dragInput = root.GetComponent<TouchDragInput>();
        crowdController.maxVisibleUnits = 120;
        crowdController.minCount = 1;

        return SaveAsPrefab(root, PrefabsFolder + "/PlayerCrowd.prefab");
    }

    private static Gate CreateGatePrefab(Material bodyMaterial, Material panelMaterial)
    {
        var root = new GameObject("Gate");
        var collider = root.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.center = new Vector3(0f, 1f, 0f);
        collider.size = new Vector3(2.1f, 2.15f, 1.15f);

        var gate = root.AddComponent<Gate>();

        CreatePrimitiveChild(PrimitiveType.Cube, "LeftPost", root.transform, new Vector3(-1.26f, 1f, 0f), new Vector3(0.12f, 2f, 0.18f), bodyMaterial);
        CreatePrimitiveChild(PrimitiveType.Cube, "RightPost", root.transform, new Vector3(1.26f, 1f, 0f), new Vector3(0.12f, 2f, 0.18f), bodyMaterial);

        var panel = CreatePrimitiveChild(
            PrimitiveType.Cube,
            "Panel",
            root.transform,
            new Vector3(0f, 1f, 0f),
            new Vector3(2.2f, 1.6f, 0.22f),
            panelMaterial);

        var label = CreateWorldText(root.transform, "Label", "+10", new Vector3(0f, 1.06f, -0.42f), 150, 4.8f);

        gate.panelRenderer = panel.GetComponent<MeshRenderer>();
        gate.labelText = label;

        return SaveAsPrefab(root, PrefabsFolder + "/Gate.prefab").GetComponent<Gate>();
    }

    private static GameObject CreateEnemyPrefab(GameObject enemyUnitPrefab)
    {
        var root = new GameObject("Enemy");
        var enemyGroup = root.AddComponent<EnemyGroup>();

        var unitsRoot = new GameObject("EnemyUnits").transform;
        unitsRoot.SetParent(root.transform, false);
        unitsRoot.localPosition = new Vector3(0f, 0f, 0f);

        var countLabel = CreateWorldText(root.transform, "EnemyCount", "100", new Vector3(0f, 2.2f, 0f), 72, 2.2f);

        enemyGroup.unitsRoot = unitsRoot;
        enemyGroup.countLabel = countLabel;
        enemyGroup.enemyUnitPrefab = enemyUnitPrefab;
        enemyGroup.maxVisibleUnits = 100;

        return SaveAsPrefab(root, PrefabsFolder + "/Enemy.prefab");
    }

    private static FinishLine CreateFinishPrefab(GameObject enemyPrefab, Material finishMaterial)
    {
        var root = new GameObject("Finish");
        var collider = root.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.center = new Vector3(0f, 1f, 0f);
        collider.size = new Vector3(9f, 2f, 2f);

        var finish = root.AddComponent<FinishLine>();

        CreatePrimitiveChild(
            PrimitiveType.Cube,
            "FinishLineMesh",
            root.transform,
            new Vector3(0f, 0.05f, 0f),
            new Vector3(9f, 0.1f, 1.3f),
            finishMaterial);

        var enemyInstance = PrefabUtility.InstantiatePrefab(enemyPrefab) as GameObject;
        if (enemyInstance != null)
        {
            enemyInstance.name = "Enemy";
            enemyInstance.transform.SetParent(root.transform, false);
            enemyInstance.transform.localPosition = new Vector3(0f, 0f, 4f);
            finish.enemyGroup = enemyInstance.GetComponent<EnemyGroup>();
        }

        finish.enemyCountLabel = CreateWorldText(root.transform, "FinishLabel", "Enemy 100", new Vector3(0f, 3f, 0f), 56, 2.2f);

        return SaveAsPrefab(root, PrefabsFolder + "/Finish.prefab").GetComponent<FinishLine>();
    }

    private static string CreateGameScene(GameObject playerPrefab, Gate gatePrefab, FinishLine finishPrefab, Material trackMaterial)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var cameraGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(CameraFollower));
        cameraGo.tag = "MainCamera";
        cameraGo.transform.position = new Vector3(0f, 10f, -14f);
        cameraGo.transform.rotation = Quaternion.Euler(20f, 0f, 0f);
        var gameplayCamera = cameraGo.GetComponent<Camera>();
        gameplayCamera.clearFlags = CameraClearFlags.SolidColor;
        gameplayCamera.backgroundColor = new Color(0.72f, 0.84f, 0.95f, 1f);
        gameplayCamera.fieldOfView = 58f;

        var lightGo = new GameObject("Directional Light", typeof(Light));
        var directional = lightGo.GetComponent<Light>();
        directional.type = LightType.Directional;
        directional.intensity = 1.22f;
        directional.color = new Color(1f, 0.95f, 0.86f, 1f);
        lightGo.transform.rotation = Quaternion.Euler(43f, 33f, 0f);

        EnsureEventSystem();

        var track = GameObject.CreatePrimitive(PrimitiveType.Cube);
        track.name = "Track";
        track.transform.position = new Vector3(0f, -0.55f, 40f);
        track.transform.localScale = new Vector3(11f, 1f, 80f);
        ApplyMaterial(track, trackMaterial);

        var trackCollider = track.GetComponent<Collider>();
        if (trackCollider != null)
        {
            Object.DestroyImmediate(trackCollider);
        }

        var playerInstance = PrefabUtility.InstantiatePrefab(playerPrefab) as GameObject;
        if (playerInstance == null)
        {
            throw new MissingReferenceException("Could not instantiate PlayerCrowd prefab.");
        }

        playerInstance.name = "PlayerCrowd";
        playerInstance.transform.position = Vector3.zero;

        var crowdController = playerInstance.GetComponent<CrowdController>();
        var cameraFollower = cameraGo.GetComponent<CameraFollower>();
        cameraFollower.target = playerInstance.transform;
        cameraFollower.followLerpSpeed = 9f;
        cameraFollower.lookLerpSpeed = 12f;
        cameraFollower.baseFieldOfView = 58f;
        cameraFollower.maxFieldOfView = 66f;
        cameraFollower.rollByLateralVelocity = 0.42f;
        cameraFollower.maxRollDegrees = 3.2f;
        cameraFollower.horizontalFollowFactor = 0.72f;

        var levelRoot = new GameObject("LevelRoot").transform;
        var gateRoot = new GameObject("GateRoot").transform;
        gateRoot.SetParent(levelRoot, false);

        var systems = new GameObject("GameSystems");
        systems.AddComponent<DeviceRuntimeSettings>();
        var levelGenerator = systems.AddComponent<LevelGenerator>();
        var gameManager = systems.AddComponent<GameManager>();

        levelGenerator.gatePrefab = gatePrefab;
        levelGenerator.finishPrefab = finishPrefab;
        levelGenerator.levelRoot = levelRoot;
        levelGenerator.gateRoot = gateRoot;
        levelGenerator.trackVisual = track.transform;
        levelGenerator.laneSpacing = 3.6f;
        levelGenerator.minLaneSpacing = 3.6f;
        levelGenerator.levelLengthMultiplier = 1.5f;
        levelGenerator.useForwardSpeedCap = false;
        levelGenerator.forwardSpeedPerLevel = 0.035f;
        levelGenerator.enemyFormulaBase = 26;
        levelGenerator.enemyFormulaLinear = 6.2f;
        levelGenerator.enemyFormulaPowerMultiplier = 1.65f;
        levelGenerator.enemyFormulaPower = 1.09f;
        levelGenerator.gateDifficultyRamp = 0.028f;
        levelGenerator.gateWidthAtStart = 2.15f;
        levelGenerator.gateWidthAtHighDifficulty = 1.3f;
        levelGenerator.panelWidthAtStart = 2.2f;
        levelGenerator.panelWidthAtHighDifficulty = 1.6f;
        levelGenerator.movingGateChanceAtStart = 0f;
        levelGenerator.movingGateChanceAtHighDifficulty = 0.75f;
        levelGenerator.enableTrackDecor = true;
        levelGenerator.stripePoolSize = 110;
        levelGenerator.stripeLength = 1.9f;
        levelGenerator.stripeGap = 1.35f;
        levelGenerator.stripeWidth = 0.16f;
        levelGenerator.backdropQuality = BackdropQuality.Auto;
        levelGenerator.enableBackdrop = true;
        levelGenerator.enableClouds = true;

        var crowdStartPoint = new GameObject("CrowdStartPoint").transform;
        crowdStartPoint.position = Vector3.zero;

        var canvas = CreateCanvas("GameCanvas");
        var safeAreaRoot = CreateSafeAreaRoot(canvas.transform, "SafeArea");
        var hud = CreateHUD(safeAreaRoot);
        var overlay = CreateResultOverlay(safeAreaRoot);
        var dragHint = CreateDragHint(safeAreaRoot);
        dragHint.dragInput = playerInstance.GetComponent<TouchDragInput>();

        gameManager.levelGenerator = levelGenerator;
        gameManager.playerCrowd = crowdController;
        gameManager.hud = hud;
        gameManager.resultsOverlay = overlay;
        gameManager.crowdStartPoint = crowdStartPoint;

        var scenePath = ScenesFolder + "/Game.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        return scenePath;
    }

    private static string CreateMainMenuScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var cameraGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraGo.tag = "MainCamera";
        cameraGo.transform.position = new Vector3(0f, 0f, -10f);
        var menuCamera = cameraGo.GetComponent<Camera>();
        menuCamera.clearFlags = CameraClearFlags.SolidColor;
        menuCamera.backgroundColor = new Color(0.06f, 0.08f, 0.14f, 1f);

        var lightGo = new GameObject("Directional Light", typeof(Light));
        var directional = lightGo.GetComponent<Light>();
        directional.type = LightType.Directional;
        directional.intensity = 1f;
        lightGo.transform.rotation = Quaternion.Euler(45f, 30f, 0f);

        EnsureEventSystem();

        var canvas = CreateCanvas("MainMenuCanvas");
        var background = CreateImage(canvas.transform, "Background", new Color(0.06f, 0.08f, 0.14f, 1f));
        StretchToFull(background.rectTransform);
        background.transform.SetAsFirstSibling();

        var safeAreaRoot = CreateSafeAreaRoot(canvas.transform, "SafeArea");

        CreateText(safeAreaRoot, "Title", "Multiply Rush", 96, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.78f), new Vector2(0.5f, 0.78f), Vector2.zero, new Vector2(900f, 180f));

        var bestLevelText = CreateText(safeAreaRoot, "BestLevel", "Best Level: 1", 48, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.62f), Vector2.zero, new Vector2(700f, 90f));

        var playButton = CreateButton(safeAreaRoot, "PlayButton", "Play", new Vector2(420f, 120f), new Vector2(0.5f, 0.45f));

        var controllerGo = new GameObject("MainMenuController");
        controllerGo.AddComponent<DeviceRuntimeSettings>();
        var controller = controllerGo.AddComponent<MainMenuController>();
        controller.bestLevelText = bestLevelText;
        controller.gameSceneName = "Game";

        UnityEventTools.AddPersistentListener(playButton.onClick, controller.Play);
        EditorUtility.SetDirty(playButton);

        var scenePath = ScenesFolder + "/MainMenu.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        return scenePath;
    }

    private static void EnsureEventSystem()
    {
        var eventSystem = Object.FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            var go = new GameObject("EventSystem");
            eventSystem = go.AddComponent<EventSystem>();
        }

        var eventSystemGo = eventSystem.gameObject;
        var standalone = eventSystemGo.GetComponent<StandaloneInputModule>();
        if (standalone != null)
        {
            Object.DestroyImmediate(standalone);
        }

        if (eventSystemGo.GetComponent<InputSystemUIInputModule>() == null)
        {
            eventSystemGo.AddComponent<InputSystemUIInputModule>();
        }
    }

    private static Canvas CreateCanvas(string name)
    {
        var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var rect = go.GetComponent<RectTransform>();
        rect.localScale = Vector3.one;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;

        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    private static Transform CreateSafeAreaRoot(Transform parent, string name)
    {
        var safeAreaGo = new GameObject(name, typeof(RectTransform), typeof(SafeAreaFitter));
        safeAreaGo.transform.SetParent(parent, false);
        var safeAreaRect = safeAreaGo.GetComponent<RectTransform>();
        StretchToFull(safeAreaRect);
        return safeAreaGo.transform;
    }

    private static DragHintController CreateDragHint(Transform parent)
    {
        var root = new GameObject("DragHint", typeof(RectTransform), typeof(DragHintController));
        root.transform.SetParent(parent, false);

        var rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 64f);
        rect.sizeDelta = new Vector2(680f, 96f);

        var background = CreateImage(root.transform, "BG", new Color(0f, 0f, 0f, 0.48f));
        StretchToFull(background.rectTransform);

        var text = CreateText(root.transform, "HintText", "Drag left/right to steer", 38, TextAnchor.MiddleCenter,
            new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        text.color = new Color(0.95f, 0.95f, 0.98f, 1f);

        var hint = root.GetComponent<DragHintController>();
        hint.rootPanel = root;
        hint.hintText = text;
        return hint;
    }

    private static HUDController CreateHUD(Transform parent)
    {
        var hudRoot = new GameObject("HUD", typeof(RectTransform), typeof(HUDController));
        hudRoot.transform.SetParent(parent, false);
        var hudRect = hudRoot.GetComponent<RectTransform>();
        StretchToFull(hudRect);

        var levelText = CreateText(hudRect, "LevelText", "Level 1", 52, TextAnchor.MiddleLeft,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -56f), new Vector2(360f, 70f));

        var countText = CreateText(hudRect, "CountText", "Count: 20", 52, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -56f), new Vector2(440f, 70f));

        var progressText = CreateText(hudRect, "ProgressText", "0%", 48, TextAnchor.MiddleRight,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -56f), new Vector2(250f, 70f));

        levelText.color = new Color(0.96f, 0.98f, 1f, 1f);
        countText.color = new Color(0.94f, 0.97f, 1f, 1f);
        progressText.color = new Color(0.96f, 0.98f, 1f, 1f);

        var progressBackground = CreateImage(hudRect, "ProgressBG", new Color(0f, 0f, 0f, 0.45f));
        var progressBgRect = progressBackground.rectTransform;
        progressBgRect.anchorMin = new Vector2(0.5f, 1f);
        progressBgRect.anchorMax = new Vector2(0.5f, 1f);
        progressBgRect.pivot = new Vector2(0.5f, 0.5f);
        progressBgRect.anchoredPosition = new Vector2(0f, -120f);
        progressBgRect.sizeDelta = new Vector2(760f, 36f);

        var progressFill = CreateImage(progressBackground.transform, "ProgressFill", new Color(0.2f, 0.86f, 0.4f, 1f));
        progressFill.type = Image.Type.Filled;
        progressFill.fillMethod = Image.FillMethod.Horizontal;
        progressFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        progressFill.fillAmount = 0f;
        StretchToFull(progressFill.rectTransform);

        var hud = hudRoot.GetComponent<HUDController>();
        hud.levelText = levelText;
        hud.countText = countText;
        hud.progressText = progressText;
        hud.progressFill = progressFill;
        return hud;
    }

    private static ResultOverlayController CreateResultOverlay(Transform parent)
    {
        var overlayRoot = new GameObject("ResultOverlay", typeof(RectTransform), typeof(ResultOverlayController));
        overlayRoot.transform.SetParent(parent, false);

        var overlayRootRect = overlayRoot.GetComponent<RectTransform>();
        StretchToFull(overlayRootRect);

        var dimBackground = CreateImage(overlayRoot.transform, "Dim", new Color(0f, 0f, 0f, 0.7f));
        StretchToFull(dimBackground.rectTransform);

        var panel = CreateImage(overlayRoot.transform, "Panel", new Color(0.09f, 0.11f, 0.18f, 0.96f));
        var panelRect = panel.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(760f, 560f);

        var title = CreateText(panel.transform, "Title", "WIN", 90, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(600f, 140f));
        title.color = new Color(0.24f, 0.95f, 0.42f, 1f);

        var detail = CreateText(panel.transform, "Detail", "Level 1", 44, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.62f), Vector2.zero, new Vector2(620f, 160f));
        detail.color = new Color(0.9f, 0.94f, 1f, 1f);

        var retryButton = CreateButton(panel.transform, "RetryButton", "Retry", new Vector2(300f, 100f), new Vector2(0.5f, 0.22f));
        var nextButton = CreateButton(panel.transform, "NextButton", "Next Level", new Vector2(300f, 100f), new Vector2(0.5f, 0.22f));

        var overlay = overlayRoot.GetComponent<ResultOverlayController>();
        overlay.rootPanel = overlayRoot;
        overlay.titleText = title;
        overlay.detailText = detail;
        overlay.retryButton = retryButton;
        overlay.nextButton = nextButton;

        overlayRoot.SetActive(false);
        return overlay;
    }

    private static Text CreateText(Transform parent, string name, string textValue, int fontSize, TextAnchor alignment,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        var text = go.GetComponent<Text>();
        var builtInFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (builtInFont == null)
        {
            builtInFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        text.font = builtInFont;
        text.text = textValue;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static Image CreateImage(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        var image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static Button CreateButton(Transform parent, string name, string buttonText, Vector2 size, Vector2 anchor)
    {
        var buttonGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGo.transform.SetParent(parent, false);

        var rect = buttonGo.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;

        var image = buttonGo.GetComponent<Image>();
        image.color = new Color(0.12f, 0.64f, 1f, 1f);

        var button = buttonGo.GetComponent<Button>();

        var label = CreateText(buttonGo.transform, "Label", buttonText, 44, TextAnchor.MiddleCenter,
            new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        label.color = Color.white;

        return button;
    }

    private static TextMesh CreateWorldText(Transform parent, string name, string textValue, Vector3 localPosition, int fontSize, float scale)
    {
        var go = new GameObject(name, typeof(TextMesh));
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one * 0.025f * scale;

        var textMesh = go.GetComponent<TextMesh>();
        textMesh.text = textValue;
        textMesh.fontSize = fontSize;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.characterSize = 0.5f;
        textMesh.color = Color.white;
        return textMesh;
    }

    private static GameObject CreatePrimitiveChild(PrimitiveType type, string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = localScale;

        ApplyMaterial(go, material);

        var collider = go.GetComponent<Collider>();
        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }

        return go;
    }

    private static void ApplyMaterial(GameObject go, Material material)
    {
        var renderer = go.GetComponent<MeshRenderer>();
        if (renderer != null && material != null)
        {
            renderer.sharedMaterial = material;
        }
    }

    private static void StretchToFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static GameObject SaveAsPrefab(GameObject source, string path)
    {
        var folder = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(folder) && !AssetDatabase.IsValidFolder(folder))
        {
            Directory.CreateDirectory(folder);
        }

        var prefab = PrefabUtility.SaveAsPrefabAsset(source, path);
        Object.DestroyImmediate(source);
        return prefab;
    }
}
#endif

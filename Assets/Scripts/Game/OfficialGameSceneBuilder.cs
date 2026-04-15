#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using BoardOfEducation.Lessons;

namespace BoardOfEducation.Game
{
    /// <summary>
    /// Builds a single combined "OfficialGame" scene containing all 6 phases.
    /// Phases use CanvasGroup crossfades instead of scene loading for seamless play.
    /// Also provides a secondary menu item to build all individual scenes.
    /// </summary>
    public static class OfficialGameSceneBuilder
    {
        // ── Shared sprite helpers (created once, used across phases) ──
        private static Sprite circleSprite;
        private static Sprite pillSprite;
        private static Sprite btnPillSprite;
        private static Sprite arrowSprite;
        private static Sprite xpGradientSprite;

        // ── Shared fonts ──
        private static TMP_FontAsset cinzelFont;
        private static TMP_FontAsset fredokaFont;
        private static TMP_FontAsset amaticFont;
        private static TMP_FontAsset patrickFont;

        [MenuItem("Knight's Quest: Math Adventures/Build Official Game", priority = 0)]
        public static void BuildCombinedScene()
        {
            Debug.Log("[OfficialGame] ── Building combined scene ──");

            // ══════════════════════════════════════════════════
            // PRE-STEP: Import all textures as sprites
            // ══════════════════════════════════════════════════
            string[] allSpritePaths = new[]
            {
                // Phase 1 (Intro2)
                "Assets/Textures/title-bg.png",
                "Assets/Textures/shield-bg.png",
                "Assets/Textures/shield-bg-grey.png",
                "Assets/Textures/play-text.png",
                "Assets/Textures/robot-yellow.png",
                "Assets/Textures/robot-purple.png",
                "Assets/Textures/robot-orange.png",
                "Assets/Textures/robot-pink.png",
                "Assets/Textures/intro/level-map-0.png",
                // Phase 2 (Intro3)
                "Assets/Textures/landscape_bg_soft.png",
                "Assets/Textures/intro/knight.png",
                // (Intro3 also uses level-map-0.png, already listed above)
                // Phase 3 (Lesson)
                "Assets/Textures/variants/chalkboard-layers/layer-0-background.png",
                "Assets/Textures/variants/chalkboard-layers/layer-1-frame.png",
                "Assets/Textures/variants/chalkboard-layers/layer-2-board.png",
                "Assets/Textures/variants/chalkboard-layers/layer-3-tray.png",
                "Assets/Textures/variants/chalkboard-layers/layer-4-border.png",
                // Phase 4 (Results)
                "Assets/Textures/results-bg.png",
                // Phase 5 (Outro)
                "Assets/Textures/knight-bronze.png",
                // Phase 6 (LevelMap)
                "Assets/Textures/intro/level-map-1.png",
            };
            foreach (var path in allSpritePaths)
                EnsureSpriteImport(path);
            // level-map-1 needs higher max texture size
            EnsureLevelMapImport("Assets/Textures/intro/level-map-1.png");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            // ══════════════════════════════════════════════════
            // SCENE SETUP
            // ══════════════════════════════════════════════════
            var scene = EditorSceneManager.NewScene(
                NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Configure the default camera (already has AudioListener)
            var cameraGo = Camera.main.gameObject;
            var cam = Camera.main;
            cam.orthographic = true;
            cam.orthographicSize = 5;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = HexColor("#0f0e2a");
            // Remove default directional light
            var defaultLight = GameObject.Find("Directional Light");
            if (defaultLight != null) Object.DestroyImmediate(defaultLight);

            // EventSystem
            var eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            var inputModuleType = System.Type.GetType(
                "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputModuleType != null)
                eventSystemGo.AddComponent(inputModuleType);
            else
                eventSystemGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            // Board SDK input module — bridges Board finger/piece contacts to Unity UI pointer events.
            // Auto-disables competing modules on Board hardware; uses mouse fallback in Editor.
            var boardInputModule = eventSystemGo.AddComponent<Board.Input.BoardUIInputModule>();
            // Enable both Finger and Glyph contacts so robot pieces also trigger buttons.
            // Finger = bit 0, Glyph = bit 1 → mask = 3
            var boardInputSO = new SerializedObject(boardInputModule);
            var maskProp = boardInputSO.FindProperty("m_InputMask.m_Bits");
            if (maskProp != null)
            {
                maskProp.longValue = 3;
                boardInputSO.ApplyModifiedPropertiesWithoutUndo();
            }

            // MainCanvas
            var canvasGo = new GameObject("MainCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            // Load fonts
            cinzelFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Cinzel SDF.asset");
            fredokaFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Fredoka-VariableFont_wdth,wght SDF.asset");
            amaticFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/AmaticSC-Bold SDF.asset");
            patrickFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/PatrickHand-Regular SDF.asset");

            // Create procedural sprites
            circleSprite = Navigation.NavigationHelper.EnsureCircleSprite();
            pillSprite = CreatePillSprite(64, 28, 14);
            btnPillSprite = CreatePillSprite(128, 64, 32);
            arrowSprite = CreateArrowSprite(32, 20);
            xpGradientSprite = CreateHorizontalGradientSprite(64, 4,
                new Color(0.95f, 0.87f, 0.58f), new Color(0.86f, 0.68f, 0.25f));

            // ══════════════════════════════════════════════════
            // BUILD ALL 7 PHASES
            // ══════════════════════════════════════════════════

            // Phase 1: Intro2
            var phase1Result = BuildPhase1_Intro2(canvasGo.transform);
            Debug.Log("[OfficialGame] Phase 1 (Intro2) built");

            // Phase 2: Intro3
            var phase2Result = BuildPhase2_Intro3(canvasGo.transform);
            Debug.Log("[OfficialGame] Phase 2 (Intro3) built");

            // Phase 3: Lesson (TotalFractions2DemoWithBG)
            var phase3Result = BuildPhase3_Lesson(canvasGo.transform);
            Debug.Log("[OfficialGame] Phase 3 (Lesson) built");

            // Phase 4: Practice (FractionsDemo5)
            var phase4Result = BuildPhase4_Practice(canvasGo.transform);
            Debug.Log("[OfficialGame] Phase 4 (Practice) built");

            // Phase 5: Results
            var phase5Result = BuildPhase5_Results(canvasGo.transform);
            Debug.Log("[OfficialGame] Phase 5 (Results) built");

            // Phase 6: Outro
            var phase6Result = BuildPhase6_Outro(canvasGo.transform);
            Debug.Log("[OfficialGame] Phase 6 (Outro) built");

            // Phase 7: LevelMap
            var phase7Result = BuildPhase7_LevelMap(canvasGo.transform);
            Debug.Log("[OfficialGame] Phase 7 (LevelMap) built");

            // ══════════════════════════════════════════════════
            // GAMECORE — all managers on one object
            // ══════════════════════════════════════════════════
            var gameCoreGo = new GameObject("GameCore");
            gameCoreGo.AddComponent<Core.BoardStartup>();
            gameCoreGo.AddComponent<Input.PieceManager>();

            // Phase 1 manager (active)
            var intro2Mgr = gameCoreGo.AddComponent<Intro2Manager>();

            // Phase 2 manager (disabled — enabled by orchestrator)
            var intro3Mgr = gameCoreGo.AddComponent<Intro3Manager>();
            intro3Mgr.enabled = false;

            // Phase 3 managers (Lesson)
            var chalkboardMgr = gameCoreGo.AddComponent<ChalkboardDemoManager>();
            chalkboardMgr.enabled = false;
            var lessonMgr = gameCoreGo.AddComponent<TotalFractions2Manager>();
            lessonMgr.enabled = false;
            var sequencer = gameCoreGo.AddComponent<LessonSequencer>();
            var lessonOrch = gameCoreGo.AddComponent<TotalFractions2DemoWithBGManager>();
            lessonOrch.enabled = false;

            // Phase 4 manager (Practice — disabled)
            var practiceMgr = gameCoreGo.AddComponent<FractionsDemo5Manager>();
            practiceMgr.enabled = false;
            var practiceSequencer = gameCoreGo.AddComponent<LessonSequencer>();

            // Phase 5 manager (Results — disabled)
            var results1Mgr = gameCoreGo.AddComponent<Results1Manager>();
            results1Mgr.enabled = false;

            // Phase 6 manager (Outro — disabled)
            var outro1Mgr = gameCoreGo.AddComponent<Outro1Manager>();
            outro1Mgr.enabled = false;

            // Audio
            gameCoreGo.AddComponent<AudioSource>();
            gameCoreGo.AddComponent<BoardOfEducation.Audio.TTSAudioProvider>();
            gameCoreGo.AddComponent<BoardOfEducation.Audio.GameAudioManager>();

            // Master orchestrator
            var officialMgr = gameCoreGo.AddComponent<OfficialGameManager>();

            // ══════════════════════════════════════════════════
            // WIRE UP ALL SERIALIZED REFERENCES
            // ══════════════════════════════════════════════════

            // Intro2Manager
            var intro2SO = new SerializedObject(intro2Mgr);
            SetRef(intro2SO, "titleScreen", phase1Result.titleGroup);
            SetRef(intro2SO, "mapScreen", phase1Result.mapGroup);
            SetRef(intro2SO, "playButton", phase1Result.playButton);
            SetRef(intro2SO, "goButton", phase1Result.goButton);
            SetRef(intro2SO, "subtitleText", phase1Result.subtitleText);
            intro2SO.ApplyModifiedPropertiesWithoutUndo();

            // Intro3Manager
            var intro3SO = new SerializedObject(intro3Mgr);
            SetRef(intro3SO, "introScreen", phase2Result.introGroup);
            SetRef(intro3SO, "mapScreen", phase2Result.mapGroup);
            SetRef(intro3SO, "subtitleText", phase2Result.subtitleText);
            SetRef(intro3SO, "continueButton", phase2Result.continueButton);
            SetRef(intro3SO, "continueButtonGroup", phase2Result.continueButtonGroup);
            SetRef(intro3SO, "continueButtonRect", phase2Result.continueButtonRect);
            SetRef(intro3SO, "goButton", phase2Result.goButton);
            SetRef(intro3SO, "knightRect", phase2Result.knightRect);
            SetRef(intro3SO, "knightImage", phase2Result.knightImage);
            intro3SO.ApplyModifiedPropertiesWithoutUndo();

            // ChalkboardDemoManager
            var chalkSO = new SerializedObject(chalkboardMgr);
            var layersProp = chalkSO.FindProperty("layers");
            layersProp.arraySize = 4;
            for (int i = 0; i < 4; i++)
                layersProp.GetArrayElementAtIndex(i).objectReferenceValue = phase3Result.overlayImages[i];
            chalkSO.ApplyModifiedPropertiesWithoutUndo();

            // LessonSequencer
            var seqSO = new SerializedObject(sequencer);
            SetRef(seqSO, "subtitleText", phase3Result.subtitleText);
            seqSO.ApplyModifiedPropertiesWithoutUndo();

            // TotalFractions2Manager
            var lessonSO = new SerializedObject(lessonMgr);
            SetRef(lessonSO, "contentArea", phase3Result.contentArea);
            SetRef(lessonSO, "playButton", phase3Result.playButton);
            SetRef(lessonSO, "playButtonGo", phase3Result.playButtonGo);
            SetRef(lessonSO, "sequencer", sequencer);
            lessonSO.FindProperty("autoPlay").boolValue = true;
            lessonSO.ApplyModifiedPropertiesWithoutUndo();

            // TotalFractions2DemoWithBGManager (orchestrator)
            var orchSO = new SerializedObject(lessonOrch);
            SetRef(orchSO, "chalkboardManager", chalkboardMgr);
            SetRef(orchSO, "lessonManager", lessonMgr);
            SetRef(orchSO, "contentGroup", phase3Result.contentGroup);
            orchSO.ApplyModifiedPropertiesWithoutUndo();

            // FractionsDemo5Manager (practice)
            var practiceSO = new SerializedObject(practiceMgr);
            SetRef(practiceSO, "contentArea", phase4Result.contentArea);
            SetRef(practiceSO, "scoreArea", phase4Result.scoreArea);
            SetRef(practiceSO, "playButton", phase4Result.playButton);
            SetRef(practiceSO, "playButtonGo", phase4Result.playButtonGo);
            SetRef(practiceSO, "sequencer", practiceSequencer);
            SetRef(practiceSO, "contentGroup", phase4Result.contentGroup);
            practiceSO.FindProperty("autoPlay").boolValue = true;
            practiceSO.ApplyModifiedPropertiesWithoutUndo();

            // Practice LessonSequencer
            var practiceSeqSO = new SerializedObject(practiceSequencer);
            SetRef(practiceSeqSO, "subtitleText", phase4Result.subtitleText);
            practiceSeqSO.ApplyModifiedPropertiesWithoutUndo();

            // Results1Manager
            var resSO = new SerializedObject(results1Mgr);
            SetRef(resSO, "screenGroup", phase5Result.screenGroup);
            SetRef(resSO, "headerText", phase5Result.headerText);
            SetRef(resSO, "levelSection", phase5Result.levelSection);
            SetRef(resSO, "levelBeforeText", phase5Result.levelBeforeText);
            SetRef(resSO, "levelArrowImage", phase5Result.levelArrowImage);
            SetRef(resSO, "levelAfterText", phase5Result.levelAfterText);
            SetRef(resSO, "titleBanner", phase5Result.titleBanner);
            SetRef(resSO, "titleBannerText", phase5Result.titleBannerText);
            SetRef(resSO, "titleBeforeText", phase5Result.titleBeforeText);
            SetRef(resSO, "titleArrowImage", phase5Result.titleArrowImage);
            SetRef(resSO, "titleAfterText", phase5Result.titleAfterText);
            SetRef(resSO, "xpSection", phase5Result.xpSection);
            SetRef(resSO, "xpBarFill", phase5Result.xpBarFill);
            SetRef(resSO, "xpText", phase5Result.xpText);
            SetRef(resSO, "xpBonusGroup", phase5Result.xpBonusGroup);
            SetRef(resSO, "xpBonusText", phase5Result.xpBonusText);
            SetRef(resSO, "continueButton", phase5Result.continueButton);
            SetRef(resSO, "continueButtonGroup", phase5Result.continueButtonGroup);
            SetRef(resSO, "meadowGlow", phase5Result.meadowGlow);
            SetRef(resSO, "levelUpFlash", phase5Result.levelUpFlash);
            SetRef(resSO, "rankTitleText", phase5Result.rankTitleText);
            SetRef(resSO, "continueButtonRect", phase5Result.continueButtonRect);
            var flowerProp = resSO.FindProperty("flowerRoots");
            if (flowerProp != null)
            {
                flowerProp.arraySize = 12;
                for (int i = 0; i < 12; i++)
                    flowerProp.GetArrayElementAtIndex(i).objectReferenceValue = phase5Result.flowerRoots[i];
            }
            resSO.ApplyModifiedPropertiesWithoutUndo();

            // Outro1Manager
            var outroSO = new SerializedObject(outro1Mgr);
            SetRef(outroSO, "subtitleText", phase6Result.subtitleText);
            SetRef(outroSO, "screenGroup", phase6Result.screenGroup);
            SetRef(outroSO, "continueButton", phase6Result.continueButton);
            SetRef(outroSO, "continueButtonGroup", phase6Result.continueButtonGroup);
            SetRef(outroSO, "knightRect", phase6Result.knightRect);
            SetRef(outroSO, "knightImage", phase6Result.knightImage);
            outroSO.ApplyModifiedPropertiesWithoutUndo();

            // OfficialGameManager (master orchestrator)
            var officialSO = new SerializedObject(officialMgr);
            SetRef(officialSO, "phase1Group", phase1Result.phaseGroup);
            SetRef(officialSO, "phase2Group", phase2Result.phaseGroup);
            SetRef(officialSO, "phase3Group", phase3Result.phaseGroup);
            SetRef(officialSO, "phase4Group", phase4Result.phaseGroup);
            SetRef(officialSO, "phase5Group", phase5Result.phaseGroup);
            SetRef(officialSO, "phase6Group", phase6Result.phaseGroup);
            SetRef(officialSO, "phase7Group", phase7Result.phaseGroup);
            SetRef(officialSO, "intro2Manager", intro2Mgr);
            SetRef(officialSO, "intro3Manager", intro3Mgr);
            SetRef(officialSO, "chalkboardManager", chalkboardMgr);
            SetRef(officialSO, "lessonOrchestrator", lessonOrch);
            SetRef(officialSO, "lessonManager", lessonMgr);
            SetRef(officialSO, "practiceManager", practiceMgr);
            SetRef(officialSO, "results1Manager", results1Mgr);
            SetRef(officialSO, "outro1Manager", outro1Mgr);
            officialSO.ApplyModifiedPropertiesWithoutUndo();

            // ══════════════════════════════════════════════════
            // SAVE + CONFIGURE
            // ══════════════════════════════════════════════════
            string scenePath = "Assets/Scenes/OfficialGame.unity";
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            EditorSceneManager.SaveScene(scene, scenePath);
            AddToBuildSettings(scenePath);
            AddAllScenesToBuildSettings();

            Debug.Log("[OfficialGame] ── Combined scene build complete ──");
        }

        // ══════════════════════════════════════════════════════════════
        // PHASE BUILDERS — each returns a struct with references
        // ══════════════════════════════════════════════════════════════

        // ── Phase 1: Intro2 ─────────────────────────────────────────

        private struct Phase1Refs
        {
            public CanvasGroup phaseGroup;
            public CanvasGroup titleGroup;
            public CanvasGroup mapGroup;
            public Button playButton;
            public Button goButton;
            public TextMeshProUGUI subtitleText;
        }

        private static Phase1Refs BuildPhase1_Intro2(Transform canvasTransform)
        {
            var phaseGo = CreateUIElement("Phase1_Intro2", canvasTransform);
            StretchFill(phaseGo);
            var phaseGroup = phaseGo.AddComponent<CanvasGroup>();

            // ── TitleScreen ──
            var titleScreenGo = CreateUIElement("TitleScreen", phaseGo.transform);
            StretchFill(titleScreenGo);
            var titleGroup = titleScreenGo.AddComponent<CanvasGroup>();

            // Background
            var bgGo = CreateUIElement("Background", titleScreenGo.transform);
            StretchFill(bgGo);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.raycastTarget = false;
            var bgSprite = LoadSprite("Assets/Textures/title-bg.png");
            if (bgSprite != null) { bgImg.sprite = bgSprite; bgImg.color = Color.white; bgImg.preserveAspect = false; }
            else { bgImg.color = HexColor("#0f0e2a"); }

            // PlayZone — hex, anchored bottom-center
            var playZoneGo = new GameObject("PlayZone");
            playZoneGo.transform.SetParent(titleScreenGo.transform, false);
            var playZoneRect = playZoneGo.AddComponent<RectTransform>();
            playZoneRect.anchorMin = new Vector2(0.5f, 0.08f);
            playZoneRect.anchorMax = new Vector2(0.5f, 0.08f);
            playZoneRect.pivot = new Vector2(0.5f, 0f);
            playZoneRect.sizeDelta = new Vector2(546, 546);

            var playZoneImg = playZoneGo.AddComponent<Image>();
            var greenShieldSprite = LoadSprite("Assets/Textures/shield-bg.png");
            if (greenShieldSprite != null)
            {
                playZoneImg.sprite = greenShieldSprite;
                playZoneImg.color = Color.white;
                playZoneImg.preserveAspect = true;
                playZoneImg.type = Image.Type.Simple;
            }

            var playBtn = playZoneGo.AddComponent<Button>();
            playBtn.targetGraphic = playZoneImg;
            var playBtnColors = playBtn.colors;
            playBtnColors.normalColor = Color.white;
            playBtnColors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            playBtnColors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            playBtn.colors = playBtnColors;

            // Green hex overlay
            var greenOverlayGo = CreateUIElement("GreenOverlay", playZoneGo.transform);
            StretchFill(greenOverlayGo);
            var greenOverlayImg = greenOverlayGo.AddComponent<Image>();
            if (greenShieldSprite != null)
            {
                greenOverlayImg.sprite = greenShieldSprite;
                greenOverlayImg.color = new Color(1, 1, 1, 0f);
                greenOverlayImg.preserveAspect = true;
                greenOverlayImg.type = Image.Type.Simple;
                greenOverlayImg.raycastTarget = false;
            }

            // PLAY text PNG
            var playTitleGo = new GameObject("PlayTitle");
            playTitleGo.transform.SetParent(titleScreenGo.transform, false);
            var playTitleRect = playTitleGo.AddComponent<RectTransform>();
            playTitleRect.anchorMin = new Vector2(0.5f, 0.5f);
            playTitleRect.anchorMax = new Vector2(0.5f, 0.5f);
            playTitleRect.pivot = new Vector2(0.5f, 0.5f);
            playTitleRect.sizeDelta = new Vector2(875, 220);
            playTitleRect.anchoredPosition = new Vector2(0, -125);
            var playImg = playTitleGo.AddComponent<Image>();
            var playTextSprite = LoadSprite("Assets/Textures/play-text.png");
            if (playTextSprite != null) { playImg.sprite = playTextSprite; playImg.color = Color.white; playImg.preserveAspect = true; playImg.raycastTarget = false; }

            // Subtitle
            var subtitleGo = new GameObject("Subtitle");
            subtitleGo.transform.SetParent(titleScreenGo.transform, false);
            var subtitleRect = subtitleGo.AddComponent<RectTransform>();
            subtitleRect.anchorMin = new Vector2(0.5f, 0.5f);
            subtitleRect.anchorMax = new Vector2(0.5f, 0.5f);
            subtitleRect.pivot = new Vector2(0.5f, 0.5f);
            subtitleRect.sizeDelta = new Vector2(500, 40);
            subtitleRect.anchoredPosition = new Vector2(0, -175);
            var subTmp = subtitleGo.AddComponent<TextMeshProUGUI>();
            subTmp.text = "Place a robot on the board to begin!";
            subTmp.fontSize = 14;
            subTmp.alignment = TextAlignmentOptions.Center;
            subTmp.color = new Color(0.91f, 0.94f, 0.85f, 0.75f);
            subTmp.characterSpacing = 2f;
            subTmp.textWrappingMode = TextWrappingModes.NoWrap;
            subTmp.overflowMode = TextOverflowModes.Overflow;
            if (cinzelFont != null) subTmp.font = cinzelFont;
            subTmp.raycastTarget = false;

            // Robots
            var robotContainer = new GameObject("RobotContainer");
            robotContainer.transform.SetParent(playZoneGo.transform, false);
            var containerRect = robotContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.32f, 0.25f);
            containerRect.anchorMax = new Vector2(0.68f, 0.43f);
            containerRect.offsetMin = Vector2.zero;
            containerRect.offsetMax = Vector2.zero;

            string[] robotPaths = { "Assets/Textures/robot-yellow.png", "Assets/Textures/robot-purple.png",
                                    "Assets/Textures/robot-orange.png", "Assets/Textures/robot-pink.png" };
            string[] robotNames = { "RobotYellow", "RobotPurple", "RobotOrange", "RobotPink" };
            for (int i = 0; i < 4; i++)
            {
                var sprite = LoadSprite(robotPaths[i]);
                if (sprite == null) continue;
                var robotGo = new GameObject(robotNames[i]);
                robotGo.transform.SetParent(robotContainer.transform, false);
                var robotRect = robotGo.AddComponent<RectTransform>();
                float slotWidth = 1f / 4f;
                float centerX = slotWidth * i + slotWidth * 0.5f;
                robotRect.anchorMin = new Vector2(centerX - 0.12f, 0f);
                robotRect.anchorMax = new Vector2(centerX + 0.12f, 1f);
                robotRect.offsetMin = Vector2.zero;
                robotRect.offsetMax = Vector2.zero;
                float t = (i - 1.5f) / 1.5f;
                float arcOffset = (1f - t * t) * 12f;
                robotRect.anchoredPosition = new Vector2(0, arcOffset - 15f);
                float tilt = -t * 5f;
                robotGo.transform.localRotation = Quaternion.Euler(0, 0, tilt);
                var robotImg = robotGo.AddComponent<Image>();
                robotImg.sprite = sprite;
                robotImg.preserveAspect = true;
                robotImg.raycastTarget = false;
            }

            // ── MapScreen ──
            var mapScreenGo = CreateUIElement("MapScreen", phaseGo.transform);
            StretchFill(mapScreenGo);
            var mapGroup = mapScreenGo.AddComponent<CanvasGroup>();

            var mapBgGo = CreateUIElement("MapImage", mapScreenGo.transform);
            StretchFill(mapBgGo);
            var mapImg = mapBgGo.AddComponent<Image>();
            mapImg.sprite = LoadSprite("Assets/Textures/intro/level-map-0.png");
            mapImg.preserveAspect = false;
            mapImg.raycastTarget = false;

            var goBtnGo = CreateButton(mapScreenGo.transform, "GoButton", "GO >", HexColor("#c9a96e"));
            SetAnchored(goBtnGo, new Vector2(0.40f, 0.03f), new Vector2(0.60f, 0.12f));

            return new Phase1Refs
            {
                phaseGroup = phaseGroup,
                titleGroup = titleGroup,
                mapGroup = mapGroup,
                playButton = playBtn,
                goButton = goBtnGo.GetComponent<Button>(),
                subtitleText = subTmp,
            };
        }

        // ── Phase 2: Intro3 ─────────────────────────────────────────

        private struct Phase2Refs
        {
            public CanvasGroup phaseGroup;
            public CanvasGroup introGroup;
            public CanvasGroup mapGroup;
            public TextMeshProUGUI subtitleText;
            public Button continueButton;
            public CanvasGroup continueButtonGroup;
            public RectTransform continueButtonRect;
            public Button goButton;
            public RectTransform knightRect;
            public Image knightImage;
        }

        private static Phase2Refs BuildPhase2_Intro3(Transform canvasTransform)
        {
            var phaseGo = CreateUIElement("Phase2_Intro3", canvasTransform);
            StretchFill(phaseGo);
            var phaseGroup = phaseGo.AddComponent<CanvasGroup>();

            // ── IntroScreen ──
            var introScreenGo = CreateUIElement("IntroScreen", phaseGo.transform);
            StretchFill(introScreenGo);
            var introGroup = introScreenGo.AddComponent<CanvasGroup>();

            // Landscape background
            var bgGo = CreateUIElement("Background", introScreenGo.transform);
            StretchFill(bgGo);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.raycastTarget = false;
            var bgSprite = LoadSprite("Assets/Textures/landscape_bg_soft.png");
            if (bgSprite != null) { bgImg.sprite = bgSprite; bgImg.color = Color.white; bgImg.preserveAspect = false; }
            else { bgImg.color = HexColor("#2a4a2a"); }

            // Knight character
            var knightGo = new GameObject("Knight");
            knightGo.transform.SetParent(introScreenGo.transform, false);
            var knightRect = knightGo.AddComponent<RectTransform>();
            knightRect.anchorMin = new Vector2(0.5f, 0.5f);
            knightRect.anchorMax = new Vector2(0.5f, 0.5f);
            knightRect.pivot = new Vector2(0.5f, 0f);
            knightRect.anchoredPosition = new Vector2(0, -400);
            knightRect.sizeDelta = new Vector2(600, 800);
            var knightImg = knightGo.AddComponent<Image>();
            var knightSprite = LoadSprite("Assets/Textures/intro/knight.png");
            if (knightSprite != null) { knightImg.sprite = knightSprite; knightImg.preserveAspect = true; knightImg.raycastTarget = false; }

            // Subtitle text
            var subtitleGo = new GameObject("SubtitleText");
            subtitleGo.transform.SetParent(introScreenGo.transform, false);
            var subtitleRect = subtitleGo.AddComponent<RectTransform>();
            subtitleRect.anchorMin = new Vector2(0.1f, 0.02f);
            subtitleRect.anchorMax = new Vector2(0.9f, 0.15f);
            subtitleRect.offsetMin = Vector2.zero;
            subtitleRect.offsetMax = Vector2.zero;
            var subtitleTmp = subtitleGo.AddComponent<TextMeshProUGUI>();
            subtitleTmp.text = "";
            subtitleTmp.fontSize = 28;
            subtitleTmp.alignment = TextAlignmentOptions.Center;
            subtitleTmp.color = Color.white;
            subtitleTmp.textWrappingMode = TextWrappingModes.Normal;
            subtitleTmp.overflowMode = TextOverflowModes.Overflow;
            subtitleTmp.richText = true;
            subtitleTmp.raycastTarget = false;
            if (fredokaFont != null) subtitleTmp.font = fredokaFont;

            // Continue button (nature-framed, matching Results1 style)
            var contGo = CreateUIElement("ContinueSection", introScreenGo.transform);
            var contRect = contGo.GetComponent<RectTransform>();
            contRect.anchorMin = new Vector2(0.63f, 0.35f);
            contRect.anchorMax = new Vector2(0.93f, 0.58f);
            contRect.offsetMin = Vector2.zero;
            contRect.offsetMax = Vector2.zero;
            var contGroup = contGo.AddComponent<CanvasGroup>();

            var btnWrapGo = CreateUIElement("ButtonWrap", contGo.transform);
            var btnWrapRect = btnWrapGo.GetComponent<RectTransform>();
            btnWrapRect.anchorMin = new Vector2(0.5f, 0.5f);
            btnWrapRect.anchorMax = new Vector2(0.5f, 0.5f);
            btnWrapRect.pivot = new Vector2(0.5f, 0.5f);
            btnWrapRect.sizeDelta = new Vector2(540, 200);

            if (btnPillSprite == null) btnPillSprite = CreatePillSprite(128, 64, 32);

            var woodRingGo = new GameObject("WoodRing");
            woodRingGo.transform.SetParent(btnWrapGo.transform, false);
            var woodRingRect = woodRingGo.AddComponent<RectTransform>();
            woodRingRect.anchorMin = new Vector2(0.5f, 0.5f);
            woodRingRect.anchorMax = new Vector2(0.5f, 0.5f);
            woodRingRect.pivot = new Vector2(0.5f, 0.5f);
            woodRingRect.sizeDelta = new Vector2(540, 200);
            var woodRingImg = woodRingGo.AddComponent<Image>();
            woodRingImg.sprite = btnPillSprite;
            woodRingImg.type = Image.Type.Sliced;
            woodRingImg.pixelsPerUnitMultiplier = 2f;
            woodRingImg.color = HexColor("#8B6B3E");
            woodRingImg.raycastTarget = false;

            var darkBorderGo = new GameObject("DarkBorder");
            darkBorderGo.transform.SetParent(btnWrapGo.transform, false);
            var darkBorderRect = darkBorderGo.AddComponent<RectTransform>();
            darkBorderRect.anchorMin = new Vector2(0.5f, 0.5f);
            darkBorderRect.anchorMax = new Vector2(0.5f, 0.5f);
            darkBorderRect.pivot = new Vector2(0.5f, 0.5f);
            darkBorderRect.sizeDelta = new Vector2(530, 188);
            var darkBorderImg = darkBorderGo.AddComponent<Image>();
            darkBorderImg.sprite = btnPillSprite;
            darkBorderImg.type = Image.Type.Sliced;
            darkBorderImg.pixelsPerUnitMultiplier = 2f;
            darkBorderImg.color = HexColor("#5A7A42");
            darkBorderImg.raycastTarget = false;

            var continueBtnGo = new GameObject("ContinueButton");
            continueBtnGo.transform.SetParent(btnWrapGo.transform, false);
            var continueBtnRect = continueBtnGo.AddComponent<RectTransform>();
            continueBtnRect.anchorMin = new Vector2(0.5f, 0.5f);
            continueBtnRect.anchorMax = new Vector2(0.5f, 0.5f);
            continueBtnRect.pivot = new Vector2(0.5f, 0.5f);
            continueBtnRect.sizeDelta = new Vector2(510, 168);
            var continueBtnImg = continueBtnGo.AddComponent<Image>();
            continueBtnImg.sprite = btnPillSprite;
            continueBtnImg.type = Image.Type.Sliced;
            continueBtnImg.pixelsPerUnitMultiplier = 2f;
            continueBtnImg.color = HexColor("#8FBC6B");
            var continueBtn = continueBtnGo.AddComponent<Button>();
            continueBtn.targetGraphic = continueBtnImg;
            var continueBtnColors = continueBtn.colors;
            continueBtnColors.normalColor = HexColor("#8FBC6B");
            continueBtnColors.highlightedColor = HexColor("#A3D48C");
            continueBtnColors.pressedColor = HexColor("#7DAF5A");
            continueBtnColors.selectedColor = HexColor("#8FBC6B");
            continueBtn.colors = continueBtnColors;

            var btnTextGo = new GameObject("ButtonText");
            btnTextGo.transform.SetParent(continueBtnGo.transform, false);
            var btnTextRect = btnTextGo.AddComponent<RectTransform>();
            btnTextRect.anchorMin = Vector2.zero;
            btnTextRect.anchorMax = Vector2.one;
            btnTextRect.offsetMin = Vector2.zero;
            btnTextRect.offsetMax = Vector2.zero;
            var btnTextTmp = btnTextGo.AddComponent<TextMeshProUGUI>();
            btnTextTmp.text = "CONTINUE";
            btnTextTmp.fontSize = 46;
            btnTextTmp.alignment = TextAlignmentOptions.Center;
            btnTextTmp.color = HexColor("#FFF5D4");
            btnTextTmp.raycastTarget = false;
            if (fredokaFont != null) btnTextTmp.font = fredokaFont;

            // Corner leaves
            if (circleSprite == null) circleSprite = Navigation.NavigationHelper.EnsureCircleSprite();
            BuildLeaf(btnWrapGo.transform, "Leaf_TL", circleSprite, new Vector2(-280, 102), -20f, new Vector2(36, 22), "#8FBC6B");
            BuildLeaf(btnWrapGo.transform, "Leaf_TR", circleSprite, new Vector2(280, 102), 70f, new Vector2(36, 22), "#8FBC6B");
            BuildLeaf(btnWrapGo.transform, "Leaf_BL", circleSprite, new Vector2(-280, -102), -70f, new Vector2(36, 22), "#8FBC6B");
            BuildLeaf(btnWrapGo.transform, "Leaf_BR", circleSprite, new Vector2(280, -102), 20f, new Vector2(36, 22), "#8FBC6B");
            BuildLeaf(btnWrapGo.transform, "LeafSm1", circleSprite, new Vector2(-235, 108), -40f, new Vector2(24, 14), "#A3D48C");
            BuildLeaf(btnWrapGo.transform, "LeafSm2", circleSprite, new Vector2(235, 108), 40f, new Vector2(24, 14), "#A3D48C");
            BuildLeaf(btnWrapGo.transform, "LeafSm3", circleSprite, new Vector2(-225, -108), 50f, new Vector2(24, 14), "#A3D48C");
            BuildLeaf(btnWrapGo.transform, "LeafSm4", circleSprite, new Vector2(225, -108), -50f, new Vector2(24, 14), "#A3D48C");

            // Grass clusters
            BuildGrassCluster(btnWrapGo.transform, "GrassCluster_L", -220f);
            BuildGrassCluster(btnWrapGo.transform, "GrassCluster_C", 0f);
            BuildGrassCluster(btnWrapGo.transform, "GrassCluster_R", 220f);

            // ── MapScreen (child of phase, not canvas) ──
            var mapScreenGo = CreateUIElement("MapScreen", phaseGo.transform);
            StretchFill(mapScreenGo);
            var mapGroup = mapScreenGo.AddComponent<CanvasGroup>();

            var mapBgGo = CreateUIElement("MapImage", mapScreenGo.transform);
            StretchFill(mapBgGo);
            var mapBgImg = mapBgGo.AddComponent<Image>();
            mapBgImg.sprite = LoadSprite("Assets/Textures/intro/level-map-0.png");
            mapBgImg.preserveAspect = false;
            mapBgImg.raycastTarget = false;

            var goBtnGo = CreateButton(mapScreenGo.transform, "GoButton", "GO >", HexColor("#c9a96e"));
            SetAnchored(goBtnGo, new Vector2(0.40f, 0.03f), new Vector2(0.60f, 0.12f));

            return new Phase2Refs
            {
                phaseGroup = phaseGroup,
                introGroup = introGroup,
                mapGroup = mapGroup,
                subtitleText = subtitleTmp,
                continueButton = continueBtn,
                continueButtonGroup = contGroup,
                continueButtonRect = continueBtnRect,
                goButton = goBtnGo.GetComponent<Button>(),
                knightRect = knightRect,
                knightImage = knightImg,
            };
        }

        // ── Phase 3: Lesson ─────────────────────────────────────────

        private struct Phase3Refs
        {
            public CanvasGroup phaseGroup;
            public Image[] overlayImages;
            public RectTransform contentArea;
            public CanvasGroup contentGroup;
            public TextMeshProUGUI subtitleText;
            public Button playButton;
            public GameObject playButtonGo;
        }

        private static Phase3Refs BuildPhase3_Lesson(Transform canvasTransform)
        {
            var phaseGo = CreateUIElement("Phase3_Lesson", canvasTransform);
            StretchFill(phaseGo);
            var phaseGroup = phaseGo.AddComponent<CanvasGroup>();

            // Background (layer-0, always visible)
            var bgGo = CreateUIElement("Background", phaseGo.transform);
            StretchFill(bgGo);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.sprite = LoadSprite("Assets/Textures/variants/chalkboard-layers/layer-0-background.png");
            bgImg.preserveAspect = false;
            bgImg.raycastTarget = false;

            // Overlay layers 1-4 (start at alpha 0)
            string[] layerPaths = {
                "Assets/Textures/variants/chalkboard-layers/layer-1-frame.png",
                "Assets/Textures/variants/chalkboard-layers/layer-2-board.png",
                "Assets/Textures/variants/chalkboard-layers/layer-3-tray.png",
                "Assets/Textures/variants/chalkboard-layers/layer-4-border.png",
            };
            string[] layerNames = { "Frame", "Board", "Tray", "Border" };
            var overlayImages = new Image[4];
            for (int i = 0; i < 4; i++)
            {
                var go = CreateUIElement(layerNames[i], phaseGo.transform);
                StretchFill(go);
                var img = go.AddComponent<Image>();
                img.sprite = LoadSprite(layerPaths[i]);
                img.preserveAspect = false;
                img.raycastTarget = false;
                var c = img.color; c.a = 0f; img.color = c;
                overlayImages[i] = img;
            }

            // ContentArea
            var contentAreaGo = CreateUIElement("ContentArea", phaseGo.transform);
            var contentAreaRect = contentAreaGo.GetComponent<RectTransform>();
            contentAreaRect.anchorMin = new Vector2(0.05f, 0.18f);
            contentAreaRect.anchorMax = new Vector2(0.95f, 0.95f);
            contentAreaRect.offsetMin = Vector2.zero;
            contentAreaRect.offsetMax = Vector2.zero;
            var contentGroup = contentAreaGo.AddComponent<CanvasGroup>();
            contentGroup.alpha = 0f;

            // SubtitleText
            var subtitleGo = CreateText(phaseGo.transform, "SubtitleText", "",
                36, TextAlignmentOptions.Center, new Color(1, 1, 1, 0.9f));
            SetAnchored(subtitleGo, new Vector2(0.1f, 0.03f), new Vector2(0.9f, 0.14f));

            // PlayButton (hidden)
            var playBtnGo = CreateButtonTwoColor(phaseGo.transform, "PlayButton",
                "\u25b6 PLAY", HexColor("#2ecc71"), HexColor("#555555"));
            SetAnchored(playBtnGo, new Vector2(0.38f, 0.22f), new Vector2(0.62f, 0.33f));
            playBtnGo.SetActive(false);

            return new Phase3Refs
            {
                phaseGroup = phaseGroup,
                overlayImages = overlayImages,
                contentArea = contentAreaRect,
                contentGroup = contentGroup,
                subtitleText = subtitleGo.GetComponent<TextMeshProUGUI>(),
                playButton = playBtnGo.GetComponent<Button>(),
                playButtonGo = playBtnGo,
            };
        }

        // ── Phase 4: Practice (FractionsDemo5) ─────────────────────

        private struct Phase4Refs
        {
            public CanvasGroup phaseGroup;
            public RectTransform contentArea;
            public RectTransform scoreArea;
            public CanvasGroup contentGroup;
            public TextMeshProUGUI subtitleText;
            public Button playButton;
            public GameObject playButtonGo;
        }

        private static Phase4Refs BuildPhase4_Practice(Transform canvasTransform)
        {
            var phaseGo = CreateUIElement("Phase4_Practice", canvasTransform);
            StretchFill(phaseGo);
            var phaseGroup = phaseGo.AddComponent<CanvasGroup>();

            // Background (chalkboard layer-0)
            var bgGo = CreateUIElement("Background", phaseGo.transform);
            StretchFill(bgGo);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.sprite = LoadSprite("Assets/Textures/variants/chalkboard-layers/layer-0-background.png");
            bgImg.preserveAspect = false;
            bgImg.raycastTarget = false;

            // Overlay layers 1-4 (fully visible from the start)
            string[] layerPaths = {
                "Assets/Textures/variants/chalkboard-layers/layer-1-frame.png",
                "Assets/Textures/variants/chalkboard-layers/layer-2-board.png",
                "Assets/Textures/variants/chalkboard-layers/layer-3-tray.png",
                "Assets/Textures/variants/chalkboard-layers/layer-4-border.png",
            };
            string[] layerNames = { "Frame", "Board", "Tray", "Border" };
            for (int i = 0; i < 4; i++)
            {
                var go = CreateUIElement(layerNames[i], phaseGo.transform);
                StretchFill(go);
                var img = go.AddComponent<Image>();
                img.sprite = LoadSprite(layerPaths[i]);
                img.preserveAspect = false;
                img.raycastTarget = false;
            }

            // LessonUI wrapper
            var lessonUIGo = CreateUIElement("LessonUI", phaseGo.transform);
            StretchFill(lessonUIGo);
            var contentGroup = lessonUIGo.AddComponent<CanvasGroup>();
            contentGroup.alpha = 1f;

            // ContentArea
            var contentAreaGo = CreateUIElement("ContentArea", lessonUIGo.transform);
            var contentAreaRect = contentAreaGo.GetComponent<RectTransform>();
            contentAreaRect.anchorMin = new Vector2(0.05f, 0.18f);
            contentAreaRect.anchorMax = new Vector2(0.95f, 0.82f);
            contentAreaRect.offsetMin = Vector2.zero;
            contentAreaRect.offsetMax = Vector2.zero;

            // SubtitleText
            var subtitleGo = CreateText(phaseGo.transform, "SubtitleText", "",
                36, TextAlignmentOptions.Center, new Color(1, 1, 1, 0.9f));
            SetAnchored(subtitleGo, new Vector2(0.1f, 0.03f), new Vector2(0.9f, 0.14f));

            // ScoreArea
            var scoreAreaGo = CreateUIElement("ScoreArea", lessonUIGo.transform);
            var scoreAreaRect = scoreAreaGo.GetComponent<RectTransform>();
            scoreAreaRect.anchorMin = new Vector2(0.25f, 0.92f);
            scoreAreaRect.anchorMax = new Vector2(0.75f, 0.98f);
            scoreAreaRect.offsetMin = Vector2.zero;
            scoreAreaRect.offsetMax = Vector2.zero;

            // PlayButton (hidden at start)
            var playBtnGo = CreateButtonTwoColor(phaseGo.transform, "PlayButton",
                "\u25b6 PLAY", HexColor("#2ecc71"), HexColor("#555555"));
            SetAnchored(playBtnGo, new Vector2(0.38f, 0.22f), new Vector2(0.62f, 0.33f));
            playBtnGo.SetActive(false);

            return new Phase4Refs
            {
                phaseGroup = phaseGroup,
                contentArea = contentAreaRect,
                scoreArea = scoreAreaRect,
                contentGroup = contentGroup,
                subtitleText = subtitleGo.GetComponent<TextMeshProUGUI>(),
                playButton = playBtnGo.GetComponent<Button>(),
                playButtonGo = playBtnGo,
            };
        }

        // ── Phase 5: Results ────────────────────────────────────────

        private struct Phase5Refs
        {
            public CanvasGroup phaseGroup;
            public CanvasGroup screenGroup;
            public TextMeshProUGUI headerText;
            public CanvasGroup levelSection;
            public TextMeshProUGUI levelBeforeText;
            public Image levelArrowImage;
            public TextMeshProUGUI levelAfterText;
            public CanvasGroup titleBanner;
            public TextMeshProUGUI titleBannerText;
            public TextMeshProUGUI titleBeforeText;
            public Image titleArrowImage;
            public TextMeshProUGUI titleAfterText;
            public CanvasGroup xpSection;
            public Image xpBarFill;
            public TextMeshProUGUI xpText;
            public CanvasGroup xpBonusGroup;
            public TextMeshProUGUI xpBonusText;
            public Button continueButton;
            public CanvasGroup continueButtonGroup;
            public Image meadowGlow;
            public Image levelUpFlash;
            public TextMeshProUGUI rankTitleText;
            public RectTransform continueButtonRect;
            public RectTransform[] flowerRoots;
        }

        private static Phase5Refs BuildPhase5_Results(Transform canvasTransform)
        {
            var phaseGo = CreateUIElement("Phase5_Results", canvasTransform);
            StretchFill(phaseGo);
            var phaseGroup = phaseGo.AddComponent<CanvasGroup>();

            // ScreenGroup
            var screenGo = CreateUIElement("ScreenGroup", phaseGo.transform);
            StretchFill(screenGo);
            var screenGroup = screenGo.AddComponent<CanvasGroup>();

            // Background
            var bgGo = CreateUIElement("Background", screenGo.transform);
            StretchFill(bgGo);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.raycastTarget = false;
            var bgSprite = LoadSprite("Assets/Textures/results-bg.png");
            if (bgSprite != null) { bgImg.sprite = bgSprite; bgImg.color = Color.white; bgImg.preserveAspect = false; }
            else { bgImg.color = HexColor("#a8d8c8"); }

            // MeadowGlow
            var meadowGlowGo = CreateUIElement("MeadowGlow", screenGo.transform);
            var meadowGlowRect = meadowGlowGo.GetComponent<RectTransform>();
            meadowGlowRect.anchorMin = new Vector2(0.5f, 0.16f);
            meadowGlowRect.anchorMax = new Vector2(0.5f, 0.16f);
            meadowGlowRect.pivot = new Vector2(0.5f, 0.5f);
            meadowGlowRect.sizeDelta = new Vector2(500, 60);
            var meadowGlowImg = meadowGlowGo.AddComponent<Image>();
            meadowGlowImg.sprite = circleSprite;
            meadowGlowImg.color = new Color(1f, 0.86f, 0.55f, 0f);
            meadowGlowImg.raycastTarget = false;

            // LevelUpFlash
            var flashGo = CreateUIElement("LevelUpFlash", screenGo.transform);
            StretchFill(flashGo);
            var flashImg = flashGo.AddComponent<Image>();
            flashImg.color = new Color(1f, 0.90f, 0.59f, 0f);
            flashImg.raycastTarget = false;

            // HeaderText
            var headerGo = new GameObject("HeaderText");
            headerGo.transform.SetParent(screenGo.transform, false);
            var headerRect = headerGo.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0.25f, 0.68f);
            headerRect.anchorMax = new Vector2(0.75f, 0.76f);
            headerRect.offsetMin = Vector2.zero;
            headerRect.offsetMax = Vector2.zero;
            var headerTmp = headerGo.AddComponent<TextMeshProUGUI>();
            headerTmp.text = "~ Quest Completed! ~";
            headerTmp.fontSize = 72;
            headerTmp.alignment = TextAlignmentOptions.Center;
            headerTmp.color = HexColor("#8a6830");
            headerTmp.raycastTarget = false;
            if (amaticFont != null) headerTmp.font = amaticFont;

            // ── Level Section ──
            var levelGo = CreateUIElement("LevelSection", screenGo.transform);
            var levelRect = levelGo.GetComponent<RectTransform>();
            levelRect.anchorMin = new Vector2(0.33f, 0.555f);
            levelRect.anchorMax = new Vector2(0.67f, 0.66f);
            levelRect.offsetMin = Vector2.zero;
            levelRect.offsetMax = Vector2.zero;
            var levelGroup = levelGo.AddComponent<CanvasGroup>();

            // LevelBeforeText
            var lvlBeforeGo = new GameObject("LevelBeforeText");
            lvlBeforeGo.transform.SetParent(levelGo.transform, false);
            var lvlBeforeRect = lvlBeforeGo.AddComponent<RectTransform>();
            lvlBeforeRect.anchorMin = new Vector2(0.05f, 0.45f);
            lvlBeforeRect.anchorMax = new Vector2(0.42f, 1f);
            lvlBeforeRect.offsetMin = Vector2.zero;
            lvlBeforeRect.offsetMax = Vector2.zero;
            var lvlBeforeTmp = lvlBeforeGo.AddComponent<TextMeshProUGUI>();
            lvlBeforeTmp.text = "Level 1";
            lvlBeforeTmp.fontSize = 32;
            lvlBeforeTmp.fontStyle = FontStyles.Bold;
            lvlBeforeTmp.alignment = TextAlignmentOptions.Right;
            lvlBeforeTmp.color = HexColor("#7a6040");
            lvlBeforeTmp.raycastTarget = false;
            if (patrickFont != null) lvlBeforeTmp.font = patrickFont;

            // LevelArrowImage
            var lvlArrowGo = new GameObject("LevelArrowImage");
            lvlArrowGo.transform.SetParent(levelGo.transform, false);
            var lvlArrowRect = lvlArrowGo.AddComponent<RectTransform>();
            lvlArrowRect.anchorMin = new Vector2(0.5f, 0.72f);
            lvlArrowRect.anchorMax = new Vector2(0.5f, 0.72f);
            lvlArrowRect.pivot = new Vector2(0.5f, 0.5f);
            lvlArrowRect.sizeDelta = new Vector2(32, 20);
            var lvlArrowImg = lvlArrowGo.AddComponent<Image>();
            lvlArrowImg.sprite = arrowSprite;
            lvlArrowImg.color = HexColor("#c8a050");
            lvlArrowImg.raycastTarget = false;

            // LevelAfterText
            var lvlAfterGo = new GameObject("LevelAfterText");
            lvlAfterGo.transform.SetParent(levelGo.transform, false);
            var lvlAfterRect = lvlAfterGo.AddComponent<RectTransform>();
            lvlAfterRect.anchorMin = new Vector2(0.58f, 0.45f);
            lvlAfterRect.anchorMax = new Vector2(0.95f, 1f);
            lvlAfterRect.offsetMin = Vector2.zero;
            lvlAfterRect.offsetMax = Vector2.zero;
            var lvlAfterTmp = lvlAfterGo.AddComponent<TextMeshProUGUI>();
            lvlAfterTmp.text = "Level 2";
            lvlAfterTmp.fontSize = 32;
            lvlAfterTmp.fontStyle = FontStyles.Bold;
            lvlAfterTmp.alignment = TextAlignmentOptions.Left;
            lvlAfterTmp.color = HexColor("#c88a20");
            lvlAfterTmp.raycastTarget = false;
            if (patrickFont != null) lvlAfterTmp.font = patrickFont;

            // RankTitle
            var rankTitleGo = new GameObject("RankTitle");
            rankTitleGo.transform.SetParent(levelGo.transform, false);
            var rankTitleRect = rankTitleGo.AddComponent<RectTransform>();
            rankTitleRect.anchorMin = new Vector2(0f, 0f);
            rankTitleRect.anchorMax = new Vector2(1f, 0.35f);
            rankTitleRect.offsetMin = Vector2.zero;
            rankTitleRect.offsetMax = Vector2.zero;
            var rankTitleTmp = rankTitleGo.AddComponent<TextMeshProUGUI>();
            rankTitleTmp.text = "Trainee";
            rankTitleTmp.fontSize = 22;
            rankTitleTmp.fontStyle = FontStyles.Bold;
            rankTitleTmp.alignment = TextAlignmentOptions.Center;
            rankTitleTmp.color = HexColor("#8a7050");
            rankTitleTmp.raycastTarget = false;
            if (patrickFont != null) rankTitleTmp.font = patrickFont;

            // ── Title Banner ──
            var titleGo = CreateUIElement("TitleBanner", screenGo.transform);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.30f, 0.47f);
            titleRect.anchorMax = new Vector2(0.70f, 0.555f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            var titleGroup = titleGo.AddComponent<CanvasGroup>();

            // TitlePromotedLabel
            var promotedLabelGo = CreateUIElement("TitlePromotedLabel", titleGo.transform);
            var promotedLabelRect = promotedLabelGo.GetComponent<RectTransform>();
            promotedLabelRect.anchorMin = new Vector2(0f, 0.55f);
            promotedLabelRect.anchorMax = new Vector2(1f, 1f);
            promotedLabelRect.offsetMin = Vector2.zero;
            promotedLabelRect.offsetMax = Vector2.zero;
            promotedLabelGo.AddComponent<CanvasGroup>();

            var titleBannerGo = new GameObject("TitleBannerText");
            titleBannerGo.transform.SetParent(promotedLabelGo.transform, false);
            var titleBannerRect = titleBannerGo.AddComponent<RectTransform>();
            titleBannerRect.anchorMin = Vector2.zero;
            titleBannerRect.anchorMax = Vector2.one;
            titleBannerRect.offsetMin = Vector2.zero;
            titleBannerRect.offsetMax = Vector2.zero;
            var titleBannerTmp = titleBannerGo.AddComponent<TextMeshProUGUI>();
            titleBannerTmp.text = "Title Promoted!";
            titleBannerTmp.fontSize = 38;
            titleBannerTmp.alignment = TextAlignmentOptions.Center;
            titleBannerTmp.color = HexColor("#c88a20");
            titleBannerTmp.raycastTarget = false;
            if (amaticFont != null) titleBannerTmp.font = amaticFont;

            // TitleTransitionRow
            var transitionRowGo = CreateUIElement("TitleTransitionRow", titleGo.transform);
            var transitionRowRect = transitionRowGo.GetComponent<RectTransform>();
            transitionRowRect.anchorMin = new Vector2(0f, 0f);
            transitionRowRect.anchorMax = new Vector2(1f, 0.55f);
            transitionRowRect.offsetMin = Vector2.zero;
            transitionRowRect.offsetMax = Vector2.zero;
            transitionRowGo.AddComponent<CanvasGroup>();

            // TitleBeforeText
            var titleBeforeGo = new GameObject("TitleBeforeText");
            titleBeforeGo.transform.SetParent(transitionRowGo.transform, false);
            var titleBeforeRect = titleBeforeGo.AddComponent<RectTransform>();
            titleBeforeRect.anchorMin = new Vector2(0.12f, 0f);
            titleBeforeRect.anchorMax = new Vector2(0.44f, 1f);
            titleBeforeRect.offsetMin = Vector2.zero;
            titleBeforeRect.offsetMax = Vector2.zero;
            var titleBeforeTmp = titleBeforeGo.AddComponent<TextMeshProUGUI>();
            titleBeforeTmp.text = "Trainee";
            titleBeforeTmp.fontSize = 24;
            titleBeforeTmp.fontStyle = FontStyles.Bold;
            titleBeforeTmp.alignment = TextAlignmentOptions.Right;
            titleBeforeTmp.color = HexColor("#a08868");
            titleBeforeTmp.raycastTarget = false;
            if (patrickFont != null) titleBeforeTmp.font = patrickFont;

            // Strikethrough
            var strikeGo = new GameObject("Strikethrough");
            strikeGo.transform.SetParent(titleBeforeGo.transform, false);
            var strikeRect = strikeGo.AddComponent<RectTransform>();
            strikeRect.anchorMin = new Vector2(1f, 0.465f);
            strikeRect.anchorMax = new Vector2(1f, 0.535f);
            strikeRect.pivot = new Vector2(1f, 0.5f);
            strikeRect.sizeDelta = new Vector2(72, 0);
            strikeRect.anchoredPosition = new Vector2(4, 0);
            var strikeImg = strikeGo.AddComponent<Image>();
            strikeImg.color = HexColor("#a08868");
            strikeImg.raycastTarget = false;

            // TitleArrowImage
            var titleArrowGo = new GameObject("TitleArrowImage");
            titleArrowGo.transform.SetParent(transitionRowGo.transform, false);
            var titleArrowRect = titleArrowGo.AddComponent<RectTransform>();
            titleArrowRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleArrowRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleArrowRect.pivot = new Vector2(0.5f, 0.5f);
            titleArrowRect.sizeDelta = new Vector2(26, 16);
            var titleArrowImg = titleArrowGo.AddComponent<Image>();
            titleArrowImg.sprite = arrowSprite;
            titleArrowImg.color = HexColor("#c8a050");
            titleArrowImg.raycastTarget = false;

            // TitleAfterText
            var titleAfterGo = new GameObject("TitleAfterText");
            titleAfterGo.transform.SetParent(transitionRowGo.transform, false);
            var titleAfterRect = titleAfterGo.AddComponent<RectTransform>();
            titleAfterRect.anchorMin = new Vector2(0.56f, 0f);
            titleAfterRect.anchorMax = new Vector2(0.88f, 1f);
            titleAfterRect.offsetMin = Vector2.zero;
            titleAfterRect.offsetMax = Vector2.zero;
            var titleAfterTmp = titleAfterGo.AddComponent<TextMeshProUGUI>();
            titleAfterTmp.text = "Explorer";
            titleAfterTmp.fontSize = 28;
            titleAfterTmp.fontStyle = FontStyles.Bold;
            titleAfterTmp.alignment = TextAlignmentOptions.Left;
            titleAfterTmp.color = HexColor("#c88a20");
            titleAfterTmp.raycastTarget = false;
            if (patrickFont != null) titleAfterTmp.font = patrickFont;

            // ── XP Section ──
            var xpGo = CreateUIElement("XPSection", screenGo.transform);
            var xpRect = xpGo.GetComponent<RectTransform>();
            xpRect.anchorMin = new Vector2(0.30f, 0.40f);
            xpRect.anchorMax = new Vector2(0.70f, 0.48f);
            xpRect.offsetMin = Vector2.zero;
            xpRect.offsetMax = Vector2.zero;
            var xpGroup = xpGo.AddComponent<CanvasGroup>();

            // FlowerRow
            var flowerRowGo = CreateUIElement("FlowerRow", xpGo.transform);
            var flowerRowRect = flowerRowGo.GetComponent<RectTransform>();
            flowerRowRect.anchorMin = new Vector2(0.5f, 0.70f);
            flowerRowRect.anchorMax = new Vector2(0.5f, 1f);
            flowerRowRect.pivot = new Vector2(0.5f, 0.5f);
            flowerRowRect.sizeDelta = new Vector2(520, 0);

            string[,] flowerColors = {
                { "#f5b8a0", "#d48a60" }, { "#f0a088", "#c87060" }, { "#f5d880", "#c8a030" },
                { "#f8ecd0", "#d4b870" }, { "#f0c070", "#c89838" }, { "#f0c0b0", "#d09078" },
                { "#f5b8a0", "#d48a60" }, { "#f0a088", "#c87060" }, { "#f5d880", "#c8a030" },
                { "#f8ecd0", "#d4b870" }, { "#f0c070", "#c89838" }, { "#f0c0b0", "#d09078" },
            };
            var flowerRoots = new RectTransform[12];
            for (int i = 0; i < 12; i++)
            {
                float xPos = (i + 0.5f) / 12f;
                flowerRoots[i] = BuildFlower(flowerRowGo.transform, $"Flower{i}",
                    circleSprite, xPos, HexColor(flowerColors[i, 0]), HexColor(flowerColors[i, 1]));
            }

            // XPBarBorder
            var xpBarBorderGo = new GameObject("XPBarBorder");
            xpBarBorderGo.transform.SetParent(xpGo.transform, false);
            var xpBarBorderRect = xpBarBorderGo.AddComponent<RectTransform>();
            xpBarBorderRect.anchorMin = new Vector2(0.5f, 0.45f);
            xpBarBorderRect.anchorMax = new Vector2(0.5f, 0.45f);
            xpBarBorderRect.pivot = new Vector2(0.5f, 0.5f);
            xpBarBorderRect.sizeDelta = new Vector2(524, 32);
            var xpBarBorderImg = xpBarBorderGo.AddComponent<Image>();
            xpBarBorderImg.sprite = pillSprite;
            xpBarBorderImg.type = Image.Type.Sliced;
            xpBarBorderImg.pixelsPerUnitMultiplier = 1f;
            xpBarBorderImg.color = HexColor("#c89830");
            xpBarBorderImg.raycastTarget = false;

            // XPBarBg
            var xpBarBgGo = new GameObject("XPBarBg");
            xpBarBgGo.transform.SetParent(xpGo.transform, false);
            var xpBarBgRect = xpBarBgGo.AddComponent<RectTransform>();
            xpBarBgRect.anchorMin = new Vector2(0.5f, 0.45f);
            xpBarBgRect.anchorMax = new Vector2(0.5f, 0.45f);
            xpBarBgRect.pivot = new Vector2(0.5f, 0.5f);
            xpBarBgRect.sizeDelta = new Vector2(520, 28);
            var xpBarBgImg = xpBarBgGo.AddComponent<Image>();
            xpBarBgImg.sprite = pillSprite;
            xpBarBgImg.type = Image.Type.Sliced;
            xpBarBgImg.pixelsPerUnitMultiplier = 1f;
            xpBarBgImg.color = new Color(1f, 1f, 1f, 0.75f);
            xpBarBgImg.raycastTarget = false;
            var xpBarMask = xpBarBgGo.AddComponent<Mask>();
            xpBarMask.showMaskGraphic = true;

            // XPBarFill
            var xpBarFillGo = new GameObject("XPBarFill");
            xpBarFillGo.transform.SetParent(xpBarBgGo.transform, false);
            var xpBarFillRect = xpBarFillGo.AddComponent<RectTransform>();
            xpBarFillRect.anchorMin = Vector2.zero;
            xpBarFillRect.anchorMax = new Vector2(0f, 1f);
            xpBarFillRect.offsetMin = Vector2.zero;
            xpBarFillRect.offsetMax = Vector2.zero;
            var xpBarFillImg = xpBarFillGo.AddComponent<Image>();
            xpBarFillImg.sprite = xpGradientSprite;
            xpBarFillImg.color = Color.white;
            xpBarFillImg.raycastTarget = false;

            // XPText
            var xpTextGo = new GameObject("XPText");
            xpTextGo.transform.SetParent(xpGo.transform, false);
            var xpTextRect = xpTextGo.AddComponent<RectTransform>();
            xpTextRect.anchorMin = new Vector2(0f, 0f);
            xpTextRect.anchorMax = new Vector2(1f, 0.35f);
            xpTextRect.offsetMin = Vector2.zero;
            xpTextRect.offsetMax = Vector2.zero;
            var xpTextTmp = xpTextGo.AddComponent<TextMeshProUGUI>();
            xpTextTmp.text = "0 / 200 XP";
            xpTextTmp.fontSize = 13;
            xpTextTmp.alignment = TextAlignmentOptions.Center;
            xpTextTmp.color = HexColor("#7a6040");
            xpTextTmp.raycastTarget = false;
            if (fredokaFont != null) xpTextTmp.font = fredokaFont;

            // ── XP Bonus Group ──
            var xpBonusGo = CreateUIElement("XPBonusGroup", screenGo.transform);
            var xpBonusRect = xpBonusGo.GetComponent<RectTransform>();
            xpBonusRect.anchorMin = new Vector2(0.30f, 0.34f);
            xpBonusRect.anchorMax = new Vector2(0.70f, 0.39f);
            xpBonusRect.offsetMin = Vector2.zero;
            xpBonusRect.offsetMax = Vector2.zero;
            var xpBonusGroupCg = xpBonusGo.AddComponent<CanvasGroup>();

            var xpBonusTextGo = new GameObject("XPBonusText");
            xpBonusTextGo.transform.SetParent(xpBonusGo.transform, false);
            var xpBonusTextRect = xpBonusTextGo.AddComponent<RectTransform>();
            xpBonusTextRect.anchorMin = Vector2.zero;
            xpBonusTextRect.anchorMax = Vector2.one;
            xpBonusTextRect.offsetMin = Vector2.zero;
            xpBonusTextRect.offsetMax = Vector2.zero;
            var xpBonusTmp = xpBonusTextGo.AddComponent<TextMeshProUGUI>();
            xpBonusTmp.text = "+500 XP";
            xpBonusTmp.fontSize = 36;
            xpBonusTmp.alignment = TextAlignmentOptions.Center;
            xpBonusTmp.color = HexColor("#c88a20");
            xpBonusTmp.raycastTarget = false;
            if (patrickFont != null) xpBonusTmp.font = patrickFont;

            // ── Continue Button (nature-framed) ──
            var contGo = CreateUIElement("ContinueSection", screenGo.transform);
            var contRect = contGo.GetComponent<RectTransform>();
            contRect.anchorMin = new Vector2(0.30f, 0.22f);
            contRect.anchorMax = new Vector2(0.70f, 0.34f);
            contRect.offsetMin = Vector2.zero;
            contRect.offsetMax = Vector2.zero;
            var contGroup = contGo.AddComponent<CanvasGroup>();

            var btnWrapGo = CreateUIElement("ButtonWrap", contGo.transform);
            var btnWrapRect = btnWrapGo.GetComponent<RectTransform>();
            btnWrapRect.anchorMin = new Vector2(0.5f, 0.5f);
            btnWrapRect.anchorMax = new Vector2(0.5f, 0.5f);
            btnWrapRect.pivot = new Vector2(0.5f, 0.5f);
            btnWrapRect.sizeDelta = new Vector2(340, 100);

            // WoodRing
            var woodRingGo = new GameObject("WoodRing");
            woodRingGo.transform.SetParent(btnWrapGo.transform, false);
            var woodRingRect = woodRingGo.AddComponent<RectTransform>();
            woodRingRect.anchorMin = new Vector2(0.5f, 0.5f);
            woodRingRect.anchorMax = new Vector2(0.5f, 0.5f);
            woodRingRect.pivot = new Vector2(0.5f, 0.5f);
            woodRingRect.sizeDelta = new Vector2(340, 100);
            var woodRingImg = woodRingGo.AddComponent<Image>();
            woodRingImg.sprite = btnPillSprite;
            woodRingImg.type = Image.Type.Sliced;
            woodRingImg.pixelsPerUnitMultiplier = 2f;
            woodRingImg.color = HexColor("#8B6B3E");
            woodRingImg.raycastTarget = false;

            // DarkBorder
            var darkBorderGo = new GameObject("DarkBorder");
            darkBorderGo.transform.SetParent(btnWrapGo.transform, false);
            var darkBorderRect = darkBorderGo.AddComponent<RectTransform>();
            darkBorderRect.anchorMin = new Vector2(0.5f, 0.5f);
            darkBorderRect.anchorMax = new Vector2(0.5f, 0.5f);
            darkBorderRect.pivot = new Vector2(0.5f, 0.5f);
            darkBorderRect.sizeDelta = new Vector2(332, 92);
            var darkBorderImg = darkBorderGo.AddComponent<Image>();
            darkBorderImg.sprite = btnPillSprite;
            darkBorderImg.type = Image.Type.Sliced;
            darkBorderImg.pixelsPerUnitMultiplier = 2f;
            darkBorderImg.color = HexColor("#5A7A42");
            darkBorderImg.raycastTarget = false;

            // ContinueButton
            var btnGo = new GameObject("ContinueButton");
            btnGo.transform.SetParent(btnWrapGo.transform, false);
            var btnRect = btnGo.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0.5f);
            btnRect.anchorMax = new Vector2(0.5f, 0.5f);
            btnRect.pivot = new Vector2(0.5f, 0.5f);
            btnRect.sizeDelta = new Vector2(320, 76);
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.sprite = btnPillSprite;
            btnImg.type = Image.Type.Sliced;
            btnImg.pixelsPerUnitMultiplier = 2f;
            btnImg.color = HexColor("#8FBC6B");
            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            var colors = btn.colors;
            colors.normalColor = HexColor("#8FBC6B");
            colors.highlightedColor = HexColor("#A3D48C");
            colors.pressedColor = HexColor("#7DAF5A");
            colors.selectedColor = HexColor("#8FBC6B");
            btn.colors = colors;

            var btnTextGo = new GameObject("ButtonText");
            btnTextGo.transform.SetParent(btnGo.transform, false);
            var btnTextRect = btnTextGo.AddComponent<RectTransform>();
            btnTextRect.anchorMin = Vector2.zero;
            btnTextRect.anchorMax = Vector2.one;
            btnTextRect.offsetMin = Vector2.zero;
            btnTextRect.offsetMax = Vector2.zero;
            var btnTextTmp = btnTextGo.AddComponent<TextMeshProUGUI>();
            btnTextTmp.text = "CONTINUE";
            btnTextTmp.fontSize = 32;
            btnTextTmp.alignment = TextAlignmentOptions.Center;
            btnTextTmp.color = HexColor("#FFF5D4");
            btnTextTmp.raycastTarget = false;
            if (fredokaFont != null) btnTextTmp.font = fredokaFont;

            // Leaves
            BuildLeaf(btnWrapGo.transform, "Leaf_TL", circleSprite, new Vector2(-180, 52), -20f, new Vector2(28, 16), "#8FBC6B");
            BuildLeaf(btnWrapGo.transform, "Leaf_TR", circleSprite, new Vector2(180, 52), 70f, new Vector2(28, 16), "#8FBC6B");
            BuildLeaf(btnWrapGo.transform, "Leaf_BL", circleSprite, new Vector2(-180, -52), -70f, new Vector2(28, 16), "#8FBC6B");
            BuildLeaf(btnWrapGo.transform, "Leaf_BR", circleSprite, new Vector2(180, -52), 20f, new Vector2(28, 16), "#8FBC6B");
            BuildLeaf(btnWrapGo.transform, "LeafSm1", circleSprite, new Vector2(-148, 56), -40f, new Vector2(18, 10), "#A3D48C");
            BuildLeaf(btnWrapGo.transform, "LeafSm2", circleSprite, new Vector2(148, 56), 40f, new Vector2(18, 10), "#A3D48C");
            BuildLeaf(btnWrapGo.transform, "LeafSm3", circleSprite, new Vector2(-140, -56), 50f, new Vector2(18, 10), "#A3D48C");
            BuildLeaf(btnWrapGo.transform, "LeafSm4", circleSprite, new Vector2(140, -56), -50f, new Vector2(18, 10), "#A3D48C");

            // Grass clusters
            BuildGrassCluster(btnWrapGo.transform, "GrassCluster_L", -140f);
            BuildGrassCluster(btnWrapGo.transform, "GrassCluster_C", 0f);
            BuildGrassCluster(btnWrapGo.transform, "GrassCluster_R", 140f);

            return new Phase5Refs
            {
                phaseGroup = phaseGroup,
                screenGroup = screenGroup,
                headerText = headerTmp,
                levelSection = levelGroup,
                levelBeforeText = lvlBeforeTmp,
                levelArrowImage = lvlArrowImg,
                levelAfterText = lvlAfterTmp,
                titleBanner = titleGroup,
                titleBannerText = titleBannerTmp,
                titleBeforeText = titleBeforeTmp,
                titleArrowImage = titleArrowImg,
                titleAfterText = titleAfterTmp,
                xpSection = xpGroup,
                xpBarFill = xpBarFillImg,
                xpText = xpTextTmp,
                xpBonusGroup = xpBonusGroupCg,
                xpBonusText = xpBonusTmp,
                continueButton = btn,
                continueButtonGroup = contGroup,
                meadowGlow = meadowGlowImg,
                levelUpFlash = flashImg,
                rankTitleText = rankTitleTmp,
                continueButtonRect = btnRect,
                flowerRoots = flowerRoots,
            };
        }

        // ── Phase 6: Outro ──────────────────────────────────────────

        private struct Phase6Refs
        {
            public CanvasGroup phaseGroup;
            public CanvasGroup screenGroup;
            public TextMeshProUGUI subtitleText;
            public Button continueButton;
            public CanvasGroup continueButtonGroup;
            public RectTransform knightRect;
            public Image knightImage;
        }

        private static Phase6Refs BuildPhase6_Outro(Transform canvasTransform)
        {
            var phaseGo = CreateUIElement("Phase6_Outro", canvasTransform);
            StretchFill(phaseGo);
            var phaseGroup = phaseGo.AddComponent<CanvasGroup>();

            // ScreenGroup
            var screenGo = CreateUIElement("ScreenGroup", phaseGo.transform);
            StretchFill(screenGo);
            var screenGroup = screenGo.AddComponent<CanvasGroup>();

            // Background
            var bgGo = CreateUIElement("Background", screenGo.transform);
            StretchFill(bgGo);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.raycastTarget = false;
            var bgSprite = LoadSprite("Assets/Textures/landscape_bg_soft.png");
            if (bgSprite != null) { bgImg.sprite = bgSprite; bgImg.color = Color.white; bgImg.preserveAspect = false; }
            else { bgImg.color = HexColor("#1a1a2e"); }

            // Knight (bronze)
            var knightGo = new GameObject("Knight");
            knightGo.transform.SetParent(screenGo.transform, false);
            var knightRect = knightGo.AddComponent<RectTransform>();
            knightRect.anchorMin = new Vector2(0.5f, 0.5f);
            knightRect.anchorMax = new Vector2(0.5f, 0.5f);
            knightRect.pivot = new Vector2(0.5f, 0f);
            knightRect.sizeDelta = new Vector2(600, 800);
            knightRect.anchoredPosition = new Vector2(0, -400);
            var knightImg = knightGo.AddComponent<Image>();
            knightImg.raycastTarget = false;
            var knightSprite = LoadSprite("Assets/Textures/knight-bronze.png");
            if (knightSprite != null) { knightImg.sprite = knightSprite; knightImg.color = Color.white; knightImg.preserveAspect = true; }
            else { knightImg.color = new Color(1, 1, 1, 0); }

            // Subtitle
            var subtitleGo = new GameObject("Subtitle");
            subtitleGo.transform.SetParent(screenGo.transform, false);
            var subtitleRect = subtitleGo.AddComponent<RectTransform>();
            subtitleRect.anchorMin = new Vector2(0.1f, 0.05f);
            subtitleRect.anchorMax = new Vector2(0.9f, 0.18f);
            subtitleRect.offsetMin = Vector2.zero;
            subtitleRect.offsetMax = Vector2.zero;
            var subTmp = subtitleGo.AddComponent<TextMeshProUGUI>();
            subTmp.text = "";
            subTmp.fontSize = 32;
            subTmp.alignment = TextAlignmentOptions.Center;
            subTmp.color = Color.white;
            subTmp.textWrappingMode = TextWrappingModes.Normal;
            subTmp.overflowMode = TextOverflowModes.Overflow;
            subTmp.raycastTarget = false;
            if (cinzelFont != null) subTmp.font = cinzelFont;

            // Continue button (nature-framed, matching Intro3 style)
            var contGo = CreateUIElement("ContinueSection", screenGo.transform);
            var contRect = contGo.GetComponent<RectTransform>();
            contRect.anchorMin = new Vector2(0.63f, 0.35f);
            contRect.anchorMax = new Vector2(0.93f, 0.58f);
            contRect.offsetMin = Vector2.zero;
            contRect.offsetMax = Vector2.zero;
            var contGroup = contGo.AddComponent<CanvasGroup>();

            var btnWrapGo5 = CreateUIElement("ButtonWrap", contGo.transform);
            var btnWrapRect5 = btnWrapGo5.GetComponent<RectTransform>();
            btnWrapRect5.anchorMin = new Vector2(0.5f, 0.5f);
            btnWrapRect5.anchorMax = new Vector2(0.5f, 0.5f);
            btnWrapRect5.pivot = new Vector2(0.5f, 0.5f);
            btnWrapRect5.sizeDelta = new Vector2(540, 200);

            if (btnPillSprite == null) btnPillSprite = CreatePillSprite(128, 64, 32);

            var woodRingGo5 = new GameObject("WoodRing");
            woodRingGo5.transform.SetParent(btnWrapGo5.transform, false);
            var woodRingRect5 = woodRingGo5.AddComponent<RectTransform>();
            woodRingRect5.anchorMin = new Vector2(0.5f, 0.5f);
            woodRingRect5.anchorMax = new Vector2(0.5f, 0.5f);
            woodRingRect5.pivot = new Vector2(0.5f, 0.5f);
            woodRingRect5.sizeDelta = new Vector2(540, 200);
            var woodRingImg5 = woodRingGo5.AddComponent<Image>();
            woodRingImg5.sprite = btnPillSprite;
            woodRingImg5.type = Image.Type.Sliced;
            woodRingImg5.pixelsPerUnitMultiplier = 2f;
            woodRingImg5.color = HexColor("#8B6B3E");
            woodRingImg5.raycastTarget = false;

            var darkBorderGo5 = new GameObject("DarkBorder");
            darkBorderGo5.transform.SetParent(btnWrapGo5.transform, false);
            var darkBorderRect5 = darkBorderGo5.AddComponent<RectTransform>();
            darkBorderRect5.anchorMin = new Vector2(0.5f, 0.5f);
            darkBorderRect5.anchorMax = new Vector2(0.5f, 0.5f);
            darkBorderRect5.pivot = new Vector2(0.5f, 0.5f);
            darkBorderRect5.sizeDelta = new Vector2(530, 188);
            var darkBorderImg5 = darkBorderGo5.AddComponent<Image>();
            darkBorderImg5.sprite = btnPillSprite;
            darkBorderImg5.type = Image.Type.Sliced;
            darkBorderImg5.pixelsPerUnitMultiplier = 2f;
            darkBorderImg5.color = HexColor("#5A7A42");
            darkBorderImg5.raycastTarget = false;

            var continueBtnGo = new GameObject("ContinueButton");
            continueBtnGo.transform.SetParent(btnWrapGo5.transform, false);
            var continueBtnRect5 = continueBtnGo.AddComponent<RectTransform>();
            continueBtnRect5.anchorMin = new Vector2(0.5f, 0.5f);
            continueBtnRect5.anchorMax = new Vector2(0.5f, 0.5f);
            continueBtnRect5.pivot = new Vector2(0.5f, 0.5f);
            continueBtnRect5.sizeDelta = new Vector2(510, 168);
            var continueBtnImg = continueBtnGo.AddComponent<Image>();
            continueBtnImg.sprite = btnPillSprite;
            continueBtnImg.type = Image.Type.Sliced;
            continueBtnImg.pixelsPerUnitMultiplier = 2f;
            continueBtnImg.color = HexColor("#8FBC6B");
            var continueBtn = continueBtnGo.AddComponent<Button>();
            continueBtn.targetGraphic = continueBtnImg;
            var continueBtnColors = continueBtn.colors;
            continueBtnColors.normalColor = HexColor("#8FBC6B");
            continueBtnColors.highlightedColor = HexColor("#A3D48C");
            continueBtnColors.pressedColor = HexColor("#7DAF5A");
            continueBtnColors.selectedColor = HexColor("#8FBC6B");
            continueBtn.colors = continueBtnColors;

            var btnTextGo5 = new GameObject("ButtonText");
            btnTextGo5.transform.SetParent(continueBtnGo.transform, false);
            var btnTextRect5 = btnTextGo5.AddComponent<RectTransform>();
            btnTextRect5.anchorMin = Vector2.zero;
            btnTextRect5.anchorMax = Vector2.one;
            btnTextRect5.offsetMin = Vector2.zero;
            btnTextRect5.offsetMax = Vector2.zero;
            var btnTextTmp5 = btnTextGo5.AddComponent<TextMeshProUGUI>();
            btnTextTmp5.text = "CONTINUE";
            btnTextTmp5.fontSize = 46;
            btnTextTmp5.alignment = TextAlignmentOptions.Center;
            btnTextTmp5.color = HexColor("#FFF5D4");
            btnTextTmp5.raycastTarget = false;
            if (fredokaFont != null) btnTextTmp5.font = fredokaFont;

            // Corner leaves
            if (circleSprite == null) circleSprite = Navigation.NavigationHelper.EnsureCircleSprite();
            BuildLeaf(btnWrapGo5.transform, "Leaf_TL", circleSprite, new Vector2(-280, 102), -20f, new Vector2(36, 22), "#8FBC6B");
            BuildLeaf(btnWrapGo5.transform, "Leaf_TR", circleSprite, new Vector2(280, 102), 70f, new Vector2(36, 22), "#8FBC6B");
            BuildLeaf(btnWrapGo5.transform, "Leaf_BL", circleSprite, new Vector2(-280, -102), -70f, new Vector2(36, 22), "#8FBC6B");
            BuildLeaf(btnWrapGo5.transform, "Leaf_BR", circleSprite, new Vector2(280, -102), 20f, new Vector2(36, 22), "#8FBC6B");
            BuildLeaf(btnWrapGo5.transform, "LeafSm1", circleSprite, new Vector2(-235, 108), -40f, new Vector2(24, 14), "#A3D48C");
            BuildLeaf(btnWrapGo5.transform, "LeafSm2", circleSprite, new Vector2(235, 108), 40f, new Vector2(24, 14), "#A3D48C");
            BuildLeaf(btnWrapGo5.transform, "LeafSm3", circleSprite, new Vector2(-225, -108), 50f, new Vector2(24, 14), "#A3D48C");
            BuildLeaf(btnWrapGo5.transform, "LeafSm4", circleSprite, new Vector2(225, -108), -50f, new Vector2(24, 14), "#A3D48C");

            // Grass clusters
            BuildGrassCluster(btnWrapGo5.transform, "GrassCluster_L", -220f);
            BuildGrassCluster(btnWrapGo5.transform, "GrassCluster_C", 0f);
            BuildGrassCluster(btnWrapGo5.transform, "GrassCluster_R", 220f);

            return new Phase6Refs
            {
                phaseGroup = phaseGroup,
                screenGroup = screenGroup,
                subtitleText = subTmp,
                continueButton = continueBtn,
                continueButtonGroup = contGroup,
                knightRect = knightRect,
                knightImage = knightImg,
            };
        }

        // ── Phase 6: LevelMap ───────────────────────────────────────

        private struct Phase7Refs
        {
            public CanvasGroup phaseGroup;
        }

        private static Phase7Refs BuildPhase7_LevelMap(Transform canvasTransform)
        {
            var phaseGo = CreateUIElement("Phase7_LevelMap", canvasTransform);
            StretchFill(phaseGo);
            var phaseGroup = phaseGo.AddComponent<CanvasGroup>();

            // Map image (level-map-1.png, stretch-fill)
            var mapBgGo = CreateUIElement("MapImage", phaseGo.transform);
            StretchFill(mapBgGo);
            var mapImg = mapBgGo.AddComponent<Image>();
            mapImg.sprite = LoadSprite("Assets/Textures/intro/level-map-1.png");
            mapImg.type = Image.Type.Simple;
            mapImg.preserveAspect = false;
            mapImg.raycastTarget = false;

            return new Phase7Refs { phaseGroup = phaseGroup };
        }

        // ══════════════════════════════════════════════════════════════
        // SHARED HELPERS
        // ══════════════════════════════════════════════════════════════

        private static Sprite LoadSprite(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static GameObject CreateUIElement(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        private static void StretchFill(GameObject go)
        {
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetAnchored(GameObject go, Vector2 anchorMin, Vector2 anchorMax)
        {
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static GameObject CreateButton(Transform parent, string name,
            string label, Color bgColor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();

            var img = go.AddComponent<Image>();
            img.color = bgColor;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);
            btn.colors = colors;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            textGo.AddComponent<RectTransform>();
            StretchFill(textGo);

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 42;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;

            return go;
        }

        private static GameObject CreateButtonTwoColor(Transform parent, string name,
            string label, Color activeColor, Color disabledColor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();

            var img = go.AddComponent<Image>();
            img.color = activeColor;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);
            btn.colors = colors;

            var textGo = CreateText(go.transform, "Text", label,
                36, TextAlignmentOptions.Center, Color.white);
            StretchFill(textGo);
            textGo.GetComponent<TextMeshProUGUI>().raycastTarget = false;

            return go;
        }

        private static GameObject CreateText(Transform parent, string name, string text,
            float fontSize, TextAlignmentOptions alignment, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            return go;
        }

        private static void SetRef(SerializedObject so, string fieldName, Object value)
        {
            var prop = so.FindProperty(fieldName);
            if (prop != null)
                prop.objectReferenceValue = value;
            else
                Debug.LogWarning($"[OfficialGameSceneBuilder] Could not find field '{fieldName}' on {so.targetObject.GetType().Name}");
        }

        private static Color HexColor(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var color);
            return color;
        }

        // ── Procedural Sprite Helpers ───────────────────────────────

        private static RectTransform BuildFlower(Transform parent, string name,
            Sprite circleSpr, float xNormalized, Color petalColor, Color centerColor)
        {
            var rootGo = CreateUIElement(name, parent);
            var rootRect = rootGo.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(xNormalized, 0.5f);
            rootRect.anchorMax = new Vector2(xNormalized, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = new Vector2(20, 20);
            rootRect.localScale = Vector3.zero;

            var centerGo = CreateUIElement("Center", rootGo.transform);
            var centerRect = centerGo.GetComponent<RectTransform>();
            centerRect.anchorMin = new Vector2(0.5f, 0.5f);
            centerRect.anchorMax = new Vector2(0.5f, 0.5f);
            centerRect.pivot = new Vector2(0.5f, 0.5f);
            centerRect.sizeDelta = new Vector2(6, 6);
            var centerImg = centerGo.AddComponent<Image>();
            centerImg.sprite = circleSpr;
            centerImg.color = centerColor;
            centerImg.raycastTarget = false;

            for (int p = 0; p < 6; p++)
            {
                float angle = p * 60f * Mathf.Deg2Rad;
                float radius = 5f;
                Vector2 offset = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);

                var petalGo = CreateUIElement($"Petal{p}", rootGo.transform);
                var petalRect = petalGo.GetComponent<RectTransform>();
                petalRect.anchorMin = new Vector2(0.5f, 0.5f);
                petalRect.anchorMax = new Vector2(0.5f, 0.5f);
                petalRect.pivot = new Vector2(0.5f, 0.5f);
                petalRect.sizeDelta = new Vector2(8, 8);
                petalRect.anchoredPosition = offset;
                var petalImg = petalGo.AddComponent<Image>();
                petalImg.sprite = circleSpr;
                petalImg.color = petalColor;
                petalImg.raycastTarget = false;
            }

            return rootRect;
        }

        private static void BuildLeaf(Transform parent, string name, Sprite circleSpr,
            Vector2 position, float rotation, Vector2 size, string hex)
        {
            var go = CreateUIElement(name, parent);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            rect.localEulerAngles = new Vector3(0, 0, rotation);
            var img = go.AddComponent<Image>();
            img.sprite = circleSpr;
            img.color = HexColor(hex);
            img.raycastTarget = false;
        }

        private static void BuildGrassCluster(Transform parent, string name, float xOffset)
        {
            var clusterGo = CreateUIElement(name, parent);
            var clusterRect = clusterGo.GetComponent<RectTransform>();
            clusterRect.anchorMin = new Vector2(0.5f, 0f);
            clusterRect.anchorMax = new Vector2(0.5f, 0f);
            clusterRect.pivot = new Vector2(0.5f, 0f);
            clusterRect.sizeDelta = new Vector2(30, 16);
            clusterRect.anchoredPosition = new Vector2(xOffset, -4);

            float[] heights = { 12f, 16f, 10f, 14f };
            float[] xPositions = { -8f, -2f, 4f, 10f };
            for (int i = 0; i < 4; i++)
            {
                var bladeGo = CreateUIElement($"Blade{i}", clusterGo.transform);
                var bladeRect = bladeGo.GetComponent<RectTransform>();
                bladeRect.anchorMin = new Vector2(0.5f, 0f);
                bladeRect.anchorMax = new Vector2(0.5f, 0f);
                bladeRect.pivot = new Vector2(0.5f, 0f);
                bladeRect.sizeDelta = new Vector2(3, heights[i]);
                bladeRect.anchoredPosition = new Vector2(xPositions[i], 0);
                bladeRect.localEulerAngles = new Vector3(0, 0, (i % 2 == 0 ? -8f : 8f));
                var bladeImg = bladeGo.AddComponent<Image>();
                bladeImg.color = HexColor("#5A7A42");
                bladeImg.raycastTarget = false;
            }
        }

        private static Sprite CreatePillSprite(int width, int height, int radius)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var pixels = new Color32[width * height];
            Color32 white = new Color32(255, 255, 255, 255);
            Color32 clear = new Color32(0, 0, 0, 0);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = 0f, dy = 0f;
                    if (x < radius) dx = radius - x;
                    else if (x >= width - radius) dx = x - (width - radius - 1);
                    if (y < radius) dy = radius - y;
                    else if (y >= height - radius) dy = y - (height - radius - 1);

                    bool inside = (dx * dx + dy * dy) <= (radius * radius);
                    if (dx == 0 || dy == 0) inside = true;

                    pixels[y * width + x] = inside ? white : clear;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, width, height),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius));
            sprite.name = "PillSprite";
            return sprite;
        }

        private static Sprite CreateArrowSprite(int width, int height)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var pixels = new Color32[width * height];
            Color32 white = new Color32(255, 255, 255, 255);
            Color32 clear = new Color32(0, 0, 0, 0);

            float centerY = (height - 1) * 0.5f;
            int shaftThickness = Mathf.Max(height / 3, 3);
            float halfShaft = shaftThickness * 0.5f;
            int headStartX = width * 3 / 5;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool inside = false;
                    float dy = Mathf.Abs(y - centerY);

                    if (x < headStartX)
                    {
                        inside = dy <= halfShaft;
                    }
                    else
                    {
                        float headProgress = (float)(x - headStartX) / (width - 1 - headStartX);
                        float halfHead = 0.5f * height * (1f - headProgress);
                        inside = dy <= halfHead;
                    }

                    pixels[y * width + x] = inside ? white : clear;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, width, height),
                new Vector2(0.5f, 0.5f), 100f);
            sprite.name = "ArrowSprite";
            return sprite;
        }

        private static Sprite CreateHorizontalGradientSprite(int width, int height, Color left, Color right)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float t = (float)x / (width - 1);
                    pixels[y * width + x] = Color.Lerp(left, right, t);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, width, height),
                new Vector2(0.5f, 0.5f), 100f);
            sprite.name = "HGradientSprite";
            return sprite;
        }

        // ── Texture Import Helpers ──────────────────────────────────

        private static void EnsureSpriteImport(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            bool needsReimport = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                needsReimport = true;
            }
            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                needsReimport = true;
            }
            if (needsReimport)
            {
                importer.spritePixelsPerUnit = 100;
                importer.SaveAndReimport();
            }
        }

        private static void EnsureLevelMapImport(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            bool needsReimport = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                needsReimport = true;
            }
            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                needsReimport = true;
            }
            if (importer.maxTextureSize < 2048)
            {
                importer.maxTextureSize = 2048;
                needsReimport = true;
            }
            if (importer.npotScale != TextureImporterNPOTScale.None)
            {
                importer.npotScale = TextureImporterNPOTScale.None;
                needsReimport = true;
            }
            importer.spritePixelsPerUnit = 100;
            if (needsReimport)
                importer.SaveAndReimport();
        }

        private static void AddAllScenesToBuildSettings()
        {
            var guids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                AddToBuildSettings(path);
            }
            Debug.Log($"[OfficialGame] Added all {guids.Length} scenes to Build Settings");
        }

        private static void AddToBuildSettings(string scenePath)
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(
                EditorBuildSettings.scenes);
            foreach (var s in scenes)
            {
                if (s.path == scenePath) return;
            }
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif

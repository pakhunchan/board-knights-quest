#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace BoardOfEducation.Game
{
    /// <summary>
    /// Editor utility that builds the Results1 scene.
    /// Enhanced version: layered meadow background, flowers, sparkle-ready layout,
    /// nature-framed continue button.
    /// Menu: Board of Education > Build Results1 Scene
    /// </summary>
    public static class Results1SceneBuilder
    {
        [MenuItem("Knight's Quest: Math Adventures/Build Results1 Scene")]
        public static void BuildScene()
        {
            if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            // ── Pre-step: ensure textures are imported as Sprite ──
            string[] spritePaths = new[]
            {
                "Assets/Textures/results-bg.png",
            };
            foreach (var path in spritePaths)
                EnsureSpriteImport(path);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                UnityEditor.SceneManagement.NewSceneMode.Single);

            // ── Main Camera ──
            var cameraGo = new GameObject("Main Camera");
            var cam = cameraGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = HexColor("#a8d8c8");
            cameraGo.tag = "MainCamera";

            // ── EventSystem ──
            var eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            var inputModuleType = System.Type.GetType(
                "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputModuleType != null)
                eventSystemGo.AddComponent(inputModuleType);
            else
                eventSystemGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            var boardInputModule = eventSystemGo.AddComponent<Board.Input.BoardUIInputModule>();
            var boardInputSO = new SerializedObject(boardInputModule);
            var maskProp = boardInputSO.FindProperty("m_InputMask.m_Bits");
            if (maskProp != null) { maskProp.longValue = 3; boardInputSO.ApplyModifiedPropertiesWithoutUndo(); }

            // ── MainCanvas ──
            var canvasGo = new GameObject("MainCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // Load fonts — matching HTML mockup font assignments
            var amaticFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/AmaticSC-Bold SDF.asset");
            if (amaticFont == null)
                Debug.LogWarning("[Results1SceneBuilder] AmaticSC-Bold SDF font not found. Run Tools > Setup AmaticSC Font.");

            var patrickFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/PatrickHand-Regular SDF.asset");
            if (patrickFont == null)
                Debug.LogWarning("[Results1SceneBuilder] PatrickHand-Regular SDF font not found. Run Tools > Setup PatrickHand Font.");

            var fredokaFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/Fonts/Fredoka-VariableFont_wdth,wght SDF.asset");
            if (fredokaFont == null)
                Debug.LogWarning("[Results1SceneBuilder] Fredoka SDF font not found.");

            // Procedural circle sprite for various elements
            var circleSprite = Navigation.NavigationHelper.EnsureCircleSprite();
            var pillSprite = CreatePillSprite(64, 28, 14);

            // ══════════════════════════════════════════════════
            // SCREEN GROUP
            // ══════════════════════════════════════════════════

            var screenGo = CreateUIElement("ScreenGroup", canvasGo.transform);
            StretchFill(screenGo);
            var screenGroup = screenGo.AddComponent<CanvasGroup>();

            // ══════════════════════════════════════════════════
            // 1A. BACKGROUND (PNG image, stretch-fill)
            // ══════════════════════════════════════════════════

            var bgGo = CreateUIElement("Background", screenGo.transform);
            StretchFill(bgGo);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.raycastTarget = false;
            var bgSprite = LoadSprite("Assets/Textures/results-bg.png");
            if (bgSprite != null)
            {
                bgImg.sprite = bgSprite;
                bgImg.color = Color.white;
                bgImg.preserveAspect = false;
            }
            else
            {
                bgImg.color = HexColor("#a8d8c8");
                Debug.LogWarning("[Results1SceneBuilder] results-bg.png not found, using fallback color.");
            }

            // MeadowGlow — circle sprite, bottom center, starts invisible
            var meadowGlowGo = CreateUIElement("MeadowGlow", screenGo.transform);
            var meadowGlowRect = meadowGlowGo.GetComponent<RectTransform>();
            meadowGlowRect.anchorMin = new Vector2(0.5f, 0.16f);
            meadowGlowRect.anchorMax = new Vector2(0.5f, 0.16f);
            meadowGlowRect.pivot = new Vector2(0.5f, 0.5f);
            meadowGlowRect.sizeDelta = new Vector2(500, 60);
            var meadowGlowImg = meadowGlowGo.AddComponent<Image>();
            meadowGlowImg.sprite = circleSprite;
            meadowGlowImg.color = new Color(1f, 0.86f, 0.55f, 0f); // #ffdc8c alpha 0
            meadowGlowImg.raycastTarget = false;

            // LevelUpFlash — stretch-fill, starts invisible
            var flashGo = CreateUIElement("LevelUpFlash", screenGo.transform);
            StretchFill(flashGo);
            var flashImg = flashGo.AddComponent<Image>();
            flashImg.color = new Color(1f, 0.90f, 0.59f, 0f); // #ffe696 alpha 0
            flashImg.raycastTarget = false;

            // ══════════════════════════════════════════════════
            // 1B. CONTENT ELEMENTS
            // ══════════════════════════════════════════════════

            // ── HeaderText ──
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

            // ══════════════════════════════════════════════════
            // LEVEL SECTION
            // ══════════════════════════════════════════════════

            var levelGo = CreateUIElement("LevelSection", screenGo.transform);
            var levelRect = levelGo.GetComponent<RectTransform>();
            levelRect.anchorMin = new Vector2(0.33f, 0.555f);
            levelRect.anchorMax = new Vector2(0.67f, 0.66f);
            levelRect.offsetMin = Vector2.zero;
            levelRect.offsetMax = Vector2.zero;
            var levelGroup = levelGo.AddComponent<CanvasGroup>();

            // LevelBeforeText (left third)
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

            // LevelArrowImage (center, narrow — procedural arrow →)
            var arrowSprite = CreateArrowSprite(32, 20);
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

            // LevelAfterText (right third)
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

            // RankTitle — below level row
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

            // ══════════════════════════════════════════════════
            // TITLE BANNER — split into promoted label + transition row
            // ══════════════════════════════════════════════════

            var titleGo = CreateUIElement("TitleBanner", screenGo.transform);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.30f, 0.47f);
            titleRect.anchorMax = new Vector2(0.70f, 0.555f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            var titleGroup = titleGo.AddComponent<CanvasGroup>();

            // TitlePromotedLabel — sub-group with its own CanvasGroup
            var promotedLabelGo = CreateUIElement("TitlePromotedLabel", titleGo.transform);
            var promotedLabelRect = promotedLabelGo.GetComponent<RectTransform>();
            promotedLabelRect.anchorMin = new Vector2(0f, 0.55f);
            promotedLabelRect.anchorMax = new Vector2(1f, 1f);
            promotedLabelRect.offsetMin = Vector2.zero;
            promotedLabelRect.offsetMax = Vector2.zero;
            var promotedLabelCg = promotedLabelGo.AddComponent<CanvasGroup>();

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

            // TitleTransitionRow — sub-group with its own CanvasGroup
            var transitionRowGo = CreateUIElement("TitleTransitionRow", titleGo.transform);
            var transitionRowRect = transitionRowGo.GetComponent<RectTransform>();
            transitionRowRect.anchorMin = new Vector2(0f, 0f);
            transitionRowRect.anchorMax = new Vector2(1f, 0.55f);
            transitionRowRect.offsetMin = Vector2.zero;
            transitionRowRect.offsetMax = Vector2.zero;
            var transitionRowCg = transitionRowGo.AddComponent<CanvasGroup>();

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

            // Thick strikethrough line overlaid on TitleBeforeText (right-aligned to match text)
            var strikeGo = new GameObject("Strikethrough");
            strikeGo.transform.SetParent(titleBeforeGo.transform, false);
            var strikeRect = strikeGo.AddComponent<RectTransform>();
            strikeRect.anchorMin = new Vector2(1f, 0.465f);
            strikeRect.anchorMax = new Vector2(1f, 0.535f);
            strikeRect.pivot = new Vector2(1f, 0.5f);
            strikeRect.sizeDelta = new Vector2(72, 0); // covers "Trainee" at 24pt bold
            strikeRect.anchoredPosition = new Vector2(4, 0); // slight overshoot past right edge
            var strikeImg = strikeGo.AddComponent<Image>();
            strikeImg.color = HexColor("#a08868");
            strikeImg.raycastTarget = false;

            // TitleArrowImage (procedural arrow →)
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

            // ══════════════════════════════════════════════════
            // XP SECTION (with FlowerRow above bar)
            // ══════════════════════════════════════════════════

            var xpGo = CreateUIElement("XPSection", screenGo.transform);
            var xpRect = xpGo.GetComponent<RectTransform>();
            xpRect.anchorMin = new Vector2(0.30f, 0.40f);
            xpRect.anchorMax = new Vector2(0.70f, 0.48f);
            xpRect.offsetMin = Vector2.zero;
            xpRect.offsetMax = Vector2.zero;
            var xpGroup = xpGo.AddComponent<CanvasGroup>();

            // FlowerRow — 12 procedural flowers above the XP bar
            var flowerRowGo = CreateUIElement("FlowerRow", xpGo.transform);
            var flowerRowRect = flowerRowGo.GetComponent<RectTransform>();
            flowerRowRect.anchorMin = new Vector2(0.5f, 0.70f);
            flowerRowRect.anchorMax = new Vector2(0.5f, 1f);
            flowerRowRect.pivot = new Vector2(0.5f, 0.5f);
            flowerRowRect.sizeDelta = new Vector2(520, 0);  // match XP bar width

            string[] flowerTypes = { "peach", "coral", "gold", "cream", "amber", "blush",
                                      "peach", "coral", "gold", "cream", "amber", "blush" };
            string[,] flowerColors = {
                { "#f5b8a0", "#d48a60" }, // peach
                { "#f0a088", "#c87060" }, // coral
                { "#f5d880", "#c8a030" }, // gold
                { "#f8ecd0", "#d4b870" }, // cream
                { "#f0c070", "#c89838" }, // amber
                { "#f0c0b0", "#d09078" }, // blush
                { "#f5b8a0", "#d48a60" },
                { "#f0a088", "#c87060" },
                { "#f5d880", "#c8a030" },
                { "#f8ecd0", "#d4b870" },
                { "#f0c070", "#c89838" },
                { "#f0c0b0", "#d09078" },
            };

            var flowerRoots = new RectTransform[12];
            for (int i = 0; i < 12; i++)
            {
                float xPos = (i + 0.5f) / 12f; // evenly spaced
                var flowerRoot = BuildFlower(flowerRowGo.transform, $"Flower{i}",
                    circleSprite, xPos, HexColor(flowerColors[i, 0]), HexColor(flowerColors[i, 1]));
                flowerRoots[i] = flowerRoot;
            }

            // XPBarBorder — darker orange ring behind the bar
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
            // Mask clips the fill to the pill shape
            var xpBarMask = xpBarBgGo.AddComponent<Mask>();
            xpBarMask.showMaskGraphic = true;

            // XPBarFill (gold gradient fill, child of XPBarBg — clipped by mask)
            // Uses a horizontal gradient texture stretched via anchors instead of Image.Filled,
            // so the gradient always spans the filled portion (light gold left → rich gold right).
            var xpGradientSprite = CreateHorizontalGradientSprite(64, 4,
                new Color(0.95f, 0.87f, 0.58f),   // left: light gold
                new Color(0.86f, 0.68f, 0.25f));   // right: rich gold
            var xpBarFillGo = new GameObject("XPBarFill");
            xpBarFillGo.transform.SetParent(xpBarBgGo.transform, false);
            var xpBarFillRect = xpBarFillGo.AddComponent<RectTransform>();
            xpBarFillRect.anchorMin = Vector2.zero;
            xpBarFillRect.anchorMax = new Vector2(0f, 1f); // right anchor = fill amount
            xpBarFillRect.offsetMin = Vector2.zero;
            xpBarFillRect.offsetMax = Vector2.zero;
            var xpBarFillImg = xpBarFillGo.AddComponent<Image>();
            xpBarFillImg.sprite = xpGradientSprite;
            xpBarFillImg.color = Color.white;
            xpBarFillImg.raycastTarget = false;

            // XPText (below bar)
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

            // ══════════════════════════════════════════════════
            // XP BONUS GROUP
            // ══════════════════════════════════════════════════

            var xpBonusGo = CreateUIElement("XPBonusGroup", screenGo.transform);
            var xpBonusRect = xpBonusGo.GetComponent<RectTransform>();
            xpBonusRect.anchorMin = new Vector2(0.30f, 0.34f);
            xpBonusRect.anchorMax = new Vector2(0.70f, 0.39f);
            xpBonusRect.offsetMin = Vector2.zero;
            xpBonusRect.offsetMax = Vector2.zero;
            var xpBonusGroup = xpBonusGo.AddComponent<CanvasGroup>();

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

            // ══════════════════════════════════════════════════
            // 1C. ENHANCED CONTINUE BUTTON — Nature-framed
            // ══════════════════════════════════════════════════

            var contGo = CreateUIElement("ContinueSection", screenGo.transform);
            var contRect = contGo.GetComponent<RectTransform>();
            contRect.anchorMin = new Vector2(0.30f, 0.22f);
            contRect.anchorMax = new Vector2(0.70f, 0.34f);
            contRect.offsetMin = Vector2.zero;
            contRect.offsetMax = Vector2.zero;
            var contGroup = contGo.AddComponent<CanvasGroup>();

            // ButtonWrap — parent container
            var btnWrapGo = CreateUIElement("ButtonWrap", contGo.transform);
            var btnWrapRect = btnWrapGo.GetComponent<RectTransform>();
            btnWrapRect.anchorMin = new Vector2(0.5f, 0.5f);
            btnWrapRect.anchorMax = new Vector2(0.5f, 0.5f);
            btnWrapRect.pivot = new Vector2(0.5f, 0.5f);
            btnWrapRect.sizeDelta = new Vector2(340, 100);

            var btnPillSprite = CreatePillSprite(128, 64, 32);

            // WoodRing (rounded background behind button)
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

            // DarkBorder (rounded)
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

            // ContinueButton (rounded, lighter meadow green)
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

            // ButtonText
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

            // Corner leaves (TL, TR, BL, BR) — pushed outward to clear rounded corners
            BuildLeaf(btnWrapGo.transform, "Leaf_TL", circleSprite, new Vector2(-180, 52), -20f, new Vector2(28, 16), "#8FBC6B");
            BuildLeaf(btnWrapGo.transform, "Leaf_TR", circleSprite, new Vector2(180, 52), 70f, new Vector2(28, 16), "#8FBC6B");
            BuildLeaf(btnWrapGo.transform, "Leaf_BL", circleSprite, new Vector2(-180, -52), -70f, new Vector2(28, 16), "#8FBC6B");
            BuildLeaf(btnWrapGo.transform, "Leaf_BR", circleSprite, new Vector2(180, -52), 20f, new Vector2(28, 16), "#8FBC6B");

            // Small leaves between corners
            BuildLeaf(btnWrapGo.transform, "LeafSm1", circleSprite, new Vector2(-148, 56), -40f, new Vector2(18, 10), "#A3D48C");
            BuildLeaf(btnWrapGo.transform, "LeafSm2", circleSprite, new Vector2(148, 56), 40f, new Vector2(18, 10), "#A3D48C");
            BuildLeaf(btnWrapGo.transform, "LeafSm3", circleSprite, new Vector2(-140, -56), 50f, new Vector2(18, 10), "#A3D48C");
            BuildLeaf(btnWrapGo.transform, "LeafSm4", circleSprite, new Vector2(140, -56), -50f, new Vector2(18, 10), "#A3D48C");

            // Grass clusters (L, C, R) — narrow green rects at bottom edge
            BuildGrassCluster(btnWrapGo.transform, "GrassCluster_L", -140f);
            BuildGrassCluster(btnWrapGo.transform, "GrassCluster_C", 0f);
            BuildGrassCluster(btnWrapGo.transform, "GrassCluster_R", 140f);

            // ══════════════════════════════════════════════════
            // GAMECORE
            // ══════════════════════════════════════════════════

            var gameCoreGo = new GameObject("GameCore");
            gameCoreGo.AddComponent<Core.BoardStartup>();
            gameCoreGo.AddComponent<Input.PieceManager>();
            gameCoreGo.AddComponent<BoardOfEducation.Audio.GameAudioManager>();

            var manager = gameCoreGo.AddComponent<Results1Manager>();

            // ══════════════════════════════════════════════════
            // WIRE UP SERIALIZED REFERENCES
            // ══════════════════════════════════════════════════

            var so = new SerializedObject(manager);
            SetRef(so, "screenGroup", screenGroup);
            SetRef(so, "headerText", headerTmp);
            SetRef(so, "levelSection", levelGroup);
            SetRef(so, "levelBeforeText", lvlBeforeTmp);
            SetRef(so, "levelArrowImage", lvlArrowImg);
            SetRef(so, "levelAfterText", lvlAfterTmp);
            SetRef(so, "titleBanner", titleGroup);
            SetRef(so, "titleBannerText", titleBannerTmp);
            SetRef(so, "titleBeforeText", titleBeforeTmp);
            SetRef(so, "titleArrowImage", titleArrowImg);
            SetRef(so, "titleAfterText", titleAfterTmp);
            SetRef(so, "xpSection", xpGroup);
            SetRef(so, "xpBarFill", xpBarFillImg);
            SetRef(so, "xpText", xpTextTmp);
            SetRef(so, "xpBonusGroup", xpBonusGroup);
            SetRef(so, "xpBonusText", xpBonusTmp);
            SetRef(so, "continueButton", btn);
            SetRef(so, "continueButtonGroup", contGroup);

            // New enhanced references
            SetRef(so, "meadowGlow", meadowGlowImg);
            SetRef(so, "levelUpFlash", flashImg);
            SetRef(so, "rankTitleText", rankTitleTmp);
            SetRef(so, "continueButtonRect", btnRect);

            // Wire flower roots array
            var flowerProp = so.FindProperty("flowerRoots");
            if (flowerProp != null)
            {
                flowerProp.arraySize = 12;
                for (int i = 0; i < 12; i++)
                    flowerProp.GetArrayElementAtIndex(i).objectReferenceValue = flowerRoots[i];
            }
            else
            {
                Debug.LogWarning("[Results1SceneBuilder] Could not find field 'flowerRoots'");
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            // ── Save Scene ──
            string scenePath = "Assets/Scenes/Results1.unity";
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);
            AddToBuildSettings(scenePath);
            Debug.Log($"[Results1SceneBuilder] Scene built and saved to {scenePath}");
        }

        // ══════════════════════════════════════════════════════
        // BUILDER HELPERS
        // ══════════════════════════════════════════════════════

        private static RectTransform BuildFlower(Transform parent, string name,
            Sprite circleSprite, float xNormalized, Color petalColor, Color centerColor)
        {
            // Root container — starts at scale 0
            var rootGo = CreateUIElement(name, parent);
            var rootRect = rootGo.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(xNormalized, 0.5f);
            rootRect.anchorMax = new Vector2(xNormalized, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = new Vector2(20, 20);
            rootRect.localScale = Vector3.zero;

            // Center
            var centerGo = CreateUIElement("Center", rootGo.transform);
            var centerRect = centerGo.GetComponent<RectTransform>();
            centerRect.anchorMin = new Vector2(0.5f, 0.5f);
            centerRect.anchorMax = new Vector2(0.5f, 0.5f);
            centerRect.pivot = new Vector2(0.5f, 0.5f);
            centerRect.sizeDelta = new Vector2(6, 6);
            var centerImg = centerGo.AddComponent<Image>();
            centerImg.sprite = circleSprite;
            centerImg.color = centerColor;
            centerImg.raycastTarget = false;

            // 6 petals, radially positioned
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
                petalImg.sprite = circleSprite;
                petalImg.color = petalColor;
                petalImg.raycastTarget = false;
            }

            return rootRect;
        }

        private static void BuildLeaf(Transform parent, string name, Sprite circleSprite,
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
            img.sprite = circleSprite;
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

        // ── Core Helpers ──────────────────────────────────────

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
            var sprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = "HGradientSprite";
            return sprite;
        }

        private static Sprite CreateArrowSprite(int width, int height)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var pixels = new Color32[width * height];
            Color32 white = new Color32(255, 255, 255, 255);
            Color32 clear = new Color32(0, 0, 0, 0);

            // Right-pointing arrow: horizontal shaft + arrowhead (→ shape)
            float centerY = (height - 1) * 0.5f;
            int shaftThickness = Mathf.Max(height / 3, 3); // shaft is ~33% of height
            float halfShaft = shaftThickness * 0.5f;
            int headStartX = width * 3 / 5; // arrowhead starts at 60% across

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool inside = false;
                    float dy = Mathf.Abs(y - centerY);

                    if (x < headStartX)
                    {
                        // Shaft region: horizontal bar centered vertically
                        inside = dy <= halfShaft;
                    }
                    else
                    {
                        // Arrowhead region: triangle narrowing to the right
                        float headProgress = (float)(x - headStartX) / (width - 1 - headStartX);
                        float halfHead = 0.5f * height * (1f - headProgress);
                        inside = dy <= halfHead;
                    }

                    pixels[y * width + x] = inside ? white : clear;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            var sprite = Sprite.Create(
                tex,
                new Rect(0, 0, width, height),
                new Vector2(0.5f, 0.5f),
                100f
            );
            sprite.name = "ArrowSprite";
            return sprite;
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
                    // Distance from nearest edge considering rounded corners
                    float dx = 0f, dy = 0f;
                    if (x < radius) dx = radius - x;
                    else if (x >= width - radius) dx = x - (width - radius - 1);
                    if (y < radius) dy = radius - y;
                    else if (y >= height - radius) dy = y - (height - radius - 1);

                    bool inside = (dx * dx + dy * dy) <= (radius * radius);
                    if (dx == 0 || dy == 0) inside = true; // rectangular body

                    pixels[y * width + x] = inside ? white : clear;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            // Create sprite with 9-slice borders so it stretches properly
            var sprite = Sprite.Create(
                tex,
                new Rect(0, 0, width, height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius) // L, B, R, T borders
            );
            sprite.name = "PillSprite";
            return sprite;
        }

        private static Sprite LoadSprite(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void EnsureSpriteImport(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[Results1SceneBuilder] No texture importer found for {path}");
                return;
            }
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
                Debug.Log($"[Results1SceneBuilder] Reimported {path} as Single Sprite");
            }
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

        private static void SetRef(SerializedObject so, string fieldName, Object value)
        {
            var prop = so.FindProperty(fieldName);
            if (prop != null)
                prop.objectReferenceValue = value;
            else
                Debug.LogWarning($"[Results1SceneBuilder] Could not find field '{fieldName}' on {so.targetObject.GetType().Name}");
        }

        private static Color HexColor(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var color);
            return color;
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
            Debug.Log($"[Results1SceneBuilder] Added {scenePath} to Build Settings");
        }
    }
}
#endif

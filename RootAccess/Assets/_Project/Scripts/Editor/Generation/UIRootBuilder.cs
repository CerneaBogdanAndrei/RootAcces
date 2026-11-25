using RootAccess.Core;
using RootAccess.Hacking;
using RootAccess.Input;
using RootAccess.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RootAccess.EditorTools
{
    /// <summary>Builds the persistent UIRoot prefab (HUD and every overlay screen) and the scene-level menu canvases.</summary>
    public static class UIRootBuilder
    {
        public static GameObject BuildUIRoot(BuildContext ctx)
        {
            GameObject root = UIFactory.Canvas("UIRoot", 100);
            BuildHud(root.transform, ctx);
            BuildTerminal(root.transform, ctx);
            BuildDialogue(root.transform, ctx);
            BuildHacking(root.transform, ctx);
            BuildPause(root.transform, ctx);
            BuildSettings(root.transform, ctx);
            BuildGameOver(root.transform);
            BuildLevelComplete(root.transform);
            BuildRubricPanel(root.transform, ctx);
            BuildOperatorConsole(root.transform);
            BuildLoading(root.transform);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{EditorPaths.PrefabsUI}/UIRoot.prefab");
            Object.DestroyImmediate(root);
            ctx.Prefabs["UIRoot"] = prefab;
            return prefab;
        }

        private static void BuildHud(Transform parent, BuildContext ctx)
        {
            RectTransform hud = UIFactory.Stretch(UIFactory.Rect("HUD", parent));
            CanvasGroup group = UIFactory.Group(hud.gameObject);
            var view = hud.gameObject.AddComponent<HudView>();

            Image panel = UIFactory.Image("StatusPanel", hud, new Color(0.03f, 0.02f, 0.08f, 0.7f));
            UIFactory.Place(panel.rectTransform, new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(480f, 150f));
            TextMeshProUGUI healthLabel = UIFactory.Text("HealthLabel", panel.transform, Strings.Hud.Health, 24f);
            UIFactory.Place(healthLabel.rectTransform, new Vector2(0f, 1f), new Vector2(16f, -10f), new Vector2(440f, 30f));
            Image healthFill = UIFactory.FillBar("HealthBar", panel.transform, new Vector2(0f, 1f), new Vector2(16f, -44f), new Vector2(448f, 22f), new Color(1f, 0.25f, 0.45f));
            TextMeshProUGUI traceLabel = UIFactory.Text("TraceLabel", panel.transform, Strings.Hud.Trace, 24f);
            UIFactory.Place(traceLabel.rectTransform, new Vector2(0f, 1f), new Vector2(16f, -76f), new Vector2(440f, 30f));
            Image traceFill = UIFactory.FillBar("TraceBar", panel.transform, new Vector2(0f, 1f), new Vector2(16f, -110f), new Vector2(448f, 22f), UIFactory.Cyan);
            traceFill.fillAmount = 0f;

            TextMeshProUGUI hidden = UIFactory.Text("HiddenLabel", hud, string.Empty, 26f, TextAlignmentOptions.Left, UIFactory.Cyan, FontStyles.Bold);
            UIFactory.Place(hidden.rectTransform, new Vector2(0f, 1f), new Vector2(28f, -184f), new Vector2(400f, 36f));
            TextMeshProUGUI gadget = UIFactory.Text("GadgetLabel", hud, string.Empty, 24f, TextAlignmentOptions.Left, UIFactory.Yellow, FontStyles.Bold);
            UIFactory.Place(gadget.rectTransform, new Vector2(0f, 1f), new Vector2(28f, -222f), new Vector2(300f, 32f));

            TextMeshProUGUI credits = UIFactory.Text("Credits", hud, "0", 30f, TextAlignmentOptions.Right, UIFactory.Yellow, FontStyles.Bold);
            UIFactory.Place(credits.rectTransform, new Vector2(1f, 1f), new Vector2(-28f, -24f), new Vector2(500f, 40f));
            TextMeshProUGUI questTitle = UIFactory.Text("QuestTitle", hud, Strings.Hud.NoActiveQuest, 26f, TextAlignmentOptions.Right, UIFactory.TextColor, FontStyles.Bold);
            UIFactory.Place(questTitle.rectTransform, new Vector2(1f, 1f), new Vector2(-28f, -72f), new Vector2(700f, 36f));
            TextMeshProUGUI objective = UIFactory.Text("QuestObjective", hud, string.Empty, 22f, TextAlignmentOptions.Right, UIFactory.Muted);
            UIFactory.Place(objective.rectTransform, new Vector2(1f, 1f), new Vector2(-28f, -110f), new Vector2(700f, 60f));

            TextMeshProUGUI prompt = UIFactory.Text("Prompt", hud, string.Empty, 30f, TextAlignmentOptions.Center, UIFactory.Cyan, FontStyles.Bold);
            UIFactory.Place(prompt.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(1000f, 50f));
            TextMeshProUGUI toast = UIFactory.Text("Toast", hud, string.Empty, 28f, TextAlignmentOptions.Center, UIFactory.Yellow);
            UIFactory.Place(toast.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(1000f, 50f));

            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(UIFactory.Cyan, 0f), new GradientColorKey(UIFactory.Yellow, 0.5f), new GradientColorKey(new Color(1f, 0.2f, 0.25f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });

            EditorAssetUtil.SetFields(view, so =>
            {
                so.Require("group").objectReferenceValue = group;
                so.Require("healthFill").objectReferenceValue = healthFill;
                so.Require("healthLabel").objectReferenceValue = healthLabel;
                so.Require("traceFill").objectReferenceValue = traceFill;
                so.Require("traceLabel").objectReferenceValue = traceLabel;
                so.Require("creditsLabel").objectReferenceValue = credits;
                so.Require("questTitle").objectReferenceValue = questTitle;
                so.Require("questObjective").objectReferenceValue = objective;
                so.Require("promptLabel").objectReferenceValue = prompt;
                so.Require("hiddenLabel").objectReferenceValue = hidden;
                so.Require("toastLabel").objectReferenceValue = toast;
                so.Require("gadgetLabel").objectReferenceValue = gadget;
                so.Require("trackedGadget").objectReferenceValue = ctx.EmpGrenade;
                so.Require("traceGradient").gradientValue = gradient;
            });
        }

        /// <summary>Personal terminal: tab row, entry list (left), details + action (right). Entries are cloned from a template.</summary>
        private static void BuildTerminal(Transform parent, BuildContext ctx)
        {
            var size = new Vector2(1560f, 860f);
            Image panel = CenterPanel("TerminalScreen", parent, size, out TextMeshProUGUI title, Strings.Panels.Title);
            UIFactory.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(40f, -30f), new Vector2(760f, 60f));
            title.alignment = TextAlignmentOptions.Left;
            TextMeshProUGUI header = UIFactory.Text("Header", panel.transform, string.Empty, 28f, TextAlignmentOptions.Right, UIFactory.Yellow, FontStyles.Bold);
            UIFactory.Place(header.rectTransform, new Vector2(1f, 1f), new Vector2(-40f, -38f), new Vector2(700f, 44f));

            RectTransform tabs = UIFactory.Rect("Tabs", panel.transform);
            UIFactory.Place(tabs, new Vector2(0f, 1f), new Vector2(40f, -100f), new Vector2(size.x - 80f, 58f));
            UIFactory.Horizontal(tabs.gameObject, 14f);
            string[] tabTitles = { Strings.Panels.Inventory, Strings.Panels.Skills, Strings.Panels.Quests, Strings.Panels.Shop, Strings.Panels.Contracts, Strings.Panels.Travel };
            var tabButtons = new Button[tabTitles.Length];
            for (int i = 0; i < tabTitles.Length; i++) tabButtons[i] = UIFactory.Button($"Tab_{i}", tabs, tabTitles[i], 232f, 54f);

            RectTransform list = UIFactory.Rect("List", panel.transform);
            UIFactory.Place(list, new Vector2(0f, 1f), new Vector2(40f, -180f), new Vector2(900f, 580f));
            var grid = list.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(440f, 58f);
            grid.spacing = new Vector2(16f, 10f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            Button template = UIFactory.Button("EntryTemplate", list, string.Empty, 440f, 58f);
            TMP_Text templateLabel = template.GetComponentInChildren<TMP_Text>(true);
            templateLabel.fontSize = 22f;
            templateLabel.alignment = TextAlignmentOptions.Left;

            Image details = UIFactory.Image("Details", panel.transform, new Color(0.02f, 0.02f, 0.06f, 0.9f));
            UIFactory.Place(details.rectTransform, new Vector2(1f, 1f), new Vector2(-40f, -180f), new Vector2(540f, 550f), new Vector2(1f, 1f));
            TextMeshProUGUI detailTitle = UIFactory.Text("DetailTitle", details.transform, string.Empty, 30f, TextAlignmentOptions.Left, UIFactory.Cyan, FontStyles.Bold);
            UIFactory.Place(detailTitle.rectTransform, new Vector2(0f, 1f), new Vector2(20f, -16f), new Vector2(500f, 44f));
            TextMeshProUGUI detailBody = UIFactory.Text("DetailBody", details.transform, string.Empty, 22f, TextAlignmentOptions.TopLeft);
            UIFactory.Place(detailBody.rectTransform, new Vector2(0f, 1f), new Vector2(20f, -70f), new Vector2(500f, 330f));
            Button action = UIFactory.Button("Action", details.transform, Strings.Panels.Buy, 300f, 58f);
            UIFactory.Place((RectTransform)action.transform, new Vector2(0.5f, 0f), new Vector2(0f, 64f), new Vector2(300f, 58f));
            TextMeshProUGUI status = UIFactory.Text("Status", details.transform, string.Empty, 20f, TextAlignmentOptions.Center, UIFactory.Yellow);
            UIFactory.Place(status.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(500f, 36f));

            Button close = UIFactory.Button("Close", panel.transform, Strings.Panels.Close, 220f, 50f);
            UIFactory.Place((RectTransform)close.transform, new Vector2(1f, 0f), new Vector2(-150f, 40f), new Vector2(220f, 50f));
            TextMeshProUGUI hint = UIFactory.Text("Hint", panel.transform, Strings.Panels.CloseHint, 20f, TextAlignmentOptions.Left, UIFactory.Muted);
            UIFactory.Place(hint.rectTransform, new Vector2(0f, 0f), new Vector2(40f, 30f), new Vector2(600f, 30f));

            GameObject host = panel.transform.parent.gameObject;
            var view = host.AddComponent<TerminalScreenView>();
            UIFactory.SetPanel(view, tabButtons[0]);
            EditorAssetUtil.SetFields(view, so =>
            {
                so.Require("input").objectReferenceValue = ctx.Input;
                so.SetArray("tabButtons", tabButtons);
                so.Require("headerLabel").objectReferenceValue = header;
                so.Require("listRoot").objectReferenceValue = list;
                so.Require("entryTemplate").objectReferenceValue = template;
                so.Require("detailTitle").objectReferenceValue = detailTitle;
                so.Require("detailBody").objectReferenceValue = detailBody;
                so.Require("actionButton").objectReferenceValue = action;
                so.Require("actionLabel").objectReferenceValue = action.GetComponentInChildren<TMP_Text>(true);
                so.Require("statusLabel").objectReferenceValue = status;
                so.Require("closeButton").objectReferenceValue = close;
            });
        }

        private static void BuildDialogue(Transform parent, BuildContext ctx)
        {
            Image box = UIFactory.Image("DialogueBox", parent, UIFactory.Panel);
            UIFactory.Place(box.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(1300f, 210f));
            UIFactory.Group(box.gameObject);
            var view = box.gameObject.AddComponent<DialogueBoxView>();
            TextMeshProUGUI speaker = UIFactory.Text("Speaker", box.transform, string.Empty, 28f, TextAlignmentOptions.Left, UIFactory.Magenta, FontStyles.Bold);
            UIFactory.Place(speaker.rectTransform, new Vector2(0f, 1f), new Vector2(28f, -16f), new Vector2(600f, 40f));
            TextMeshProUGUI body = UIFactory.Text("Body", box.transform, string.Empty, 28f);
            UIFactory.Place(body.rectTransform, new Vector2(0f, 1f), new Vector2(28f, -62f), new Vector2(1240f, 120f));
            TextMeshProUGUI hint = UIFactory.Text("Hint", box.transform, Strings.Panels.Continue, 18f, TextAlignmentOptions.Right, UIFactory.Muted);
            UIFactory.Place(hint.rectTransform, new Vector2(1f, 0f), new Vector2(-20f, 10f), new Vector2(300f, 26f));
            EditorAssetUtil.SetFields(view, so =>
            {
                so.Require("input").objectReferenceValue = ctx.Input;
                so.Require("speakerLabel").objectReferenceValue = speaker;
                so.Require("bodyLabel").objectReferenceValue = body;
            });
        }

        private static void BuildHacking(Transform parent, BuildContext ctx)
        {
            RectTransform overlay = UIFactory.Stretch(UIFactory.Rect("HackingOverlay", parent));
            CanvasGroup group = UIFactory.Group(overlay.gameObject);
            UIFactory.FullscreenBackdrop(overlay, new Color(0.01f, 0.03f, 0.05f, 0.88f));
            Image frame = UIFactory.Image("Frame", overlay, new Color(0.03f, 0.08f, 0.1f, 0.95f));
            UIFactory.Place(frame.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100f, 760f));
            Image border = UIFactory.Image("Border", frame.transform, UIFactory.Cyan);
            UIFactory.Place(border.rectTransform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(1100f, 4f));

            TextMeshProUGUI title = UIFactory.Text("Title", frame.transform, Strings.Hacking.Title, 40f, TextAlignmentOptions.Center, UIFactory.Cyan, FontStyles.Bold);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(1000f, 56f));
            TextMeshProUGUI hint = UIFactory.Text("Hint", frame.transform, string.Empty, 24f, TextAlignmentOptions.Center, UIFactory.Muted);
            UIFactory.Place(hint.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -86f), new Vector2(1000f, 70f));
            TextMeshProUGUI result = UIFactory.Text("Result", frame.transform, string.Empty, 44f, TextAlignmentOptions.Center, UIFactory.Cyan, FontStyles.Bold);
            UIFactory.Place(result.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(1000f, 60f));

            HackingMinigame Minigame<T>(string name) where T : HackingMinigame
            {
                RectTransform host = UIFactory.Rect(name, frame.transform);
                UIFactory.Place(host, new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(1000f, 520f));
                RectTransform content = UIFactory.Rect("Content", host);
                UIFactory.Place(content, new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(900f, 440f));
                Button template = UIFactory.Button("CellTemplate", content, string.Empty, 100f, 100f);
                TextMeshProUGUI status = UIFactory.Text("Status", host, string.Empty, 26f, TextAlignmentOptions.Center, UIFactory.Yellow);
                UIFactory.Place(status.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(900f, 40f));
                var minigame = host.gameObject.AddComponent<T>();
                EditorAssetUtil.SetFields(minigame, so =>
                {
                    so.Require("content").objectReferenceValue = content;
                    so.Require("buttonTemplate").objectReferenceValue = template;
                    so.Require("statusLabel").objectReferenceValue = status;
                });
                return minigame;
            }

            HackingMinigame pattern = Minigame<PatternMemoryMinigame>("PatternMemory");
            HackingMinigame wires = Minigame<WireConnectMinigame>("WireConnect");
            HackingMinigame portScan = Minigame<PortScanMinigame>("PortScan");

            var view = overlay.gameObject.AddComponent<HackingOverlay>();
            EditorAssetUtil.SetFields(view, so =>
            {
                so.Require("group").objectReferenceValue = group;
                so.Require("input").objectReferenceValue = ctx.Input;
                so.Require("titleLabel").objectReferenceValue = title;
                so.Require("hintLabel").objectReferenceValue = hint;
                so.Require("resultLabel").objectReferenceValue = result;
                so.SetArray("minigames", new[] { pattern, wires, portScan });
            });
        }

        private static RectTransform MenuColumn(Transform panel, float y = -150f)
        {
            RectTransform column = UIFactory.Rect("Buttons", panel);
            UIFactory.Place(column, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(520f, 500f), new Vector2(0.5f, 1f));
            UIFactory.Vertical(column.gameObject, 14f);
            return column;
        }

        private static Image CenterPanel(string name, Transform parent, Vector2 size, out TextMeshProUGUI title, string titleText)
        {
            RectTransform host = UIFactory.Stretch(UIFactory.Rect(name, parent));
            UIFactory.FullscreenBackdrop(host, UIFactory.Backdrop);
            Image panel = UIFactory.Image("Panel", host, UIFactory.Panel);
            UIFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, size);
            Image accent = UIFactory.Image("Accent", panel.transform, UIFactory.Magenta);
            UIFactory.Place(accent.rectTransform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(size.x, 4f));
            title = UIFactory.Text("Title", panel.transform, titleText, 46f, TextAlignmentOptions.Center, UIFactory.Cyan, FontStyles.Bold);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(size.x - 40f, 70f));
            return panel;
        }

        private static void BuildPause(Transform parent, BuildContext ctx)
        {
            Image panel = CenterPanel("PauseMenu", parent, new Vector2(620f, 560f), out _, Strings.Menu.Paused);
            RectTransform column = MenuColumn(panel.transform);
            Button resume = UIFactory.Button("Resume", column, Strings.Menu.Resume);
            Button settings = UIFactory.Button("Settings", column, Strings.Menu.Settings);
            Button menu = UIFactory.Button("MainMenu", column, Strings.Menu.MainMenu);
            Button quit = UIFactory.Button("Quit", column, Strings.Menu.Quit);
            GameObject host = panel.transform.parent.gameObject;
            var view = host.AddComponent<PauseMenuView>();
            UIFactory.SetPanel(view, resume);
            EditorAssetUtil.SetFields(view, so =>
            {
                so.Require("input").objectReferenceValue = ctx.Input;
                so.Require("resumeButton").objectReferenceValue = resume;
                so.Require("settingsButton").objectReferenceValue = settings;
                so.Require("mainMenuButton").objectReferenceValue = menu;
                so.Require("quitButton").objectReferenceValue = quit;
            });
        }

        private static void BuildSettings(Transform parent, BuildContext ctx)
        {
            Image panel = CenterPanel("SettingsMenu", parent, new Vector2(900f, 900f), out _, Strings.Settings.Title);
            RectTransform column = UIFactory.Rect("Rows", panel.transform);
            UIFactory.Place(column, new Vector2(0.5f, 1f), new Vector2(0f, -130f), new Vector2(800f, 740f), new Vector2(0.5f, 1f));
            UIFactory.Vertical(column.gameObject, 10f);

            Slider master = UIFactory.Slider("Master", column, Strings.Settings.MasterVolume);
            Slider music = UIFactory.Slider("Music", column, Strings.Settings.MusicVolume);
            Slider sfx = UIFactory.Slider("Sfx", column, Strings.Settings.SfxVolume);
            Slider ui = UIFactory.Slider("UI", column, Strings.Settings.UiVolume);
            TMP_Dropdown resolution = UIFactory.Dropdown("Resolution", column, Strings.Settings.Resolution);
            Toggle fullscreen = UIFactory.Toggle("Fullscreen", column, Strings.Settings.Fullscreen);
            Toggle vsync = UIFactory.Toggle("VSync", column, Strings.Settings.VSync);
            RebindActionButton jump = RebindRow(column, InputActionNames.Jump, Strings.Settings.Jump);
            RebindActionButton interact = RebindRow(column, InputActionNames.Interact, Strings.Settings.Interact);
            Button reset = UIFactory.Button("ResetBindings", column, Strings.Settings.ResetBindings, 760f, 54f);
            Button back = UIFactory.Button("Back", column, Strings.Menu.Back, 760f, 58f);

            GameObject host = panel.transform.parent.gameObject;
            var view = host.AddComponent<SettingsMenuView>();
            UIFactory.SetPanel(view, master);
            EditorAssetUtil.SetFields(view, so =>
            {
                so.Require("input").objectReferenceValue = ctx.Input;
                so.Require("masterSlider").objectReferenceValue = master;
                so.Require("musicSlider").objectReferenceValue = music;
                so.Require("sfxSlider").objectReferenceValue = sfx;
                so.Require("uiSlider").objectReferenceValue = ui;
                so.Require("resolutionDropdown").objectReferenceValue = resolution;
                so.Require("fullscreenToggle").objectReferenceValue = fullscreen;
                so.Require("vsyncToggle").objectReferenceValue = vsync;
                so.SetArray("rebindButtons", new[] { jump, interact });
                so.Require("resetBindingsButton").objectReferenceValue = reset;
                so.Require("backButton").objectReferenceValue = back;
            });
        }

        private static RebindActionButton RebindRow(Transform parent, string action, string label)
        {
            Button button = UIFactory.Button($"Rebind_{action}", parent, string.Empty, 760f, 54f);
            TMP_Text existing = button.GetComponentInChildren<TMP_Text>();
            existing.alignment = TextAlignmentOptions.Right;
            existing.color = UIFactory.Yellow;
            existing.name = "Binding";
            TextMeshProUGUI actionLabel = UIFactory.Text("Action", button.transform, label, 28f);
            UIFactory.Stretch(actionLabel.rectTransform, 12f);
            var rebind = button.gameObject.AddComponent<RebindActionButton>();
            EditorAssetUtil.SetFields(rebind, so =>
            {
                so.Require("actionName").stringValue = action;
                so.Require("bindingGroup").stringValue = ControlSchemes.KeyboardMouse;
                so.Require("actionLabel").objectReferenceValue = actionLabel;
                so.Require("bindingLabel").objectReferenceValue = existing;
                so.Require("displayName").stringValue = label;
            });
            return rebind;
        }

        private static void BuildGameOver(Transform parent)
        {
            Image panel = CenterPanel("GameOver", parent, new Vector2(760f, 460f), out TextMeshProUGUI title, Strings.Menu.GameOver);
            title.color = UIFactory.Magenta;
            TextMeshProUGUI hint = UIFactory.Text("Hint", panel.transform, Strings.Menu.GameOverHint, 26f, TextAlignmentOptions.Center, UIFactory.Muted);
            UIFactory.Place(hint.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(700f, 40f));
            RectTransform column = MenuColumn(panel.transform, -190f);
            Button retry = UIFactory.Button("Retry", column, Strings.Menu.Retry);
            Button menu = UIFactory.Button("MainMenu", column, Strings.Menu.MainMenu);
            var view = panel.transform.parent.gameObject.AddComponent<GameOverView>();
            UIFactory.SetPanel(view, retry);
            EditorAssetUtil.SetFields(view, so =>
            {
                so.Require("titleLabel").objectReferenceValue = title;
                so.Require("hintLabel").objectReferenceValue = hint;
                so.Require("retryButton").objectReferenceValue = retry;
                so.Require("mainMenuButton").objectReferenceValue = menu;
            });
        }

        private static void BuildLevelComplete(Transform parent)
        {
            Image panel = CenterPanel("LevelComplete", parent, new Vector2(760f, 560f), out TextMeshProUGUI title, Strings.Menu.LevelComplete);
            TextMeshProUGUI summary = UIFactory.Text("Summary", panel.transform, string.Empty, 28f, TextAlignmentOptions.Center);
            UIFactory.Place(summary.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -130f), new Vector2(700f, 140f));
            RectTransform column = MenuColumn(panel.transform, -300f);
            Button next = UIFactory.Button("Continue", column, Strings.Menu.NextLevel);
            Button menu = UIFactory.Button("MainMenu", column, Strings.Menu.MainMenu);
            var view = panel.transform.parent.gameObject.AddComponent<LevelCompleteView>();
            UIFactory.SetPanel(view, next);
            EditorAssetUtil.SetFields(view, so =>
            {
                so.Require("titleLabel").objectReferenceValue = title;
                so.Require("summaryLabel").objectReferenceValue = summary;
                so.Require("continueButton").objectReferenceValue = next;
                so.Require("mainMenuButton").objectReferenceValue = menu;
            });
        }

        private static void BuildRubricPanel(Transform parent, BuildContext ctx)
        {
            RectTransform host = UIFactory.Stretch(UIFactory.Rect("DebugRubricPanel", parent));
            Image panel = UIFactory.Image("Panel", host, UIFactory.Panel, raycast: true);
            UIFactory.Place(panel.rectTransform, new Vector2(0f, 0.5f), new Vector2(30f, 0f), new Vector2(1180f, 960f), new Vector2(0f, 0.5f));
            TextMeshProUGUI title = UIFactory.Text("Title", panel.transform, Strings.Rubric.PanelTitle, 34f, TextAlignmentOptions.Left, UIFactory.Cyan, FontStyles.Bold);
            UIFactory.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(24f, -18f), new Vector2(1100f, 48f));
            RectTransform list = UIFactory.Rect("List", panel.transform);
            UIFactory.Place(list, new Vector2(0f, 1f), new Vector2(24f, -80f), new Vector2(1130f, 860f));
            UIFactory.Vertical(list.gameObject, 6f, 0, TextAnchor.UpperLeft);

            RectTransform row = UIFactory.Row("RowTemplate", list, 1130f, 56f);
            TextMeshProUGUI label = UIFactory.Text("Label", row, string.Empty, 20f);
            label.rectTransform.sizeDelta = new Vector2(960f, 56f);
            label.textWrappingMode = TextWrappingModes.Normal;
            UIFactory.Button("GoTo", row, Strings.Rubric.GoTo, 140f, 48f);

            var view = host.gameObject.AddComponent<DebugRubricPanel>();
            UIFactory.SetPanel(view, null);
            EditorAssetUtil.SetFields(view, so =>
            {
                so.Require("input").objectReferenceValue = ctx.Input;
                so.Require("catalog").objectReferenceValue = ctx.Rubric;
                so.Require("list").objectReferenceValue = list;
                so.Require("rowTemplate").objectReferenceValue = row.gameObject;
                so.Require("titleLabel").objectReferenceValue = title;
            });
        }

        private static void BuildLoading(Transform parent)
        {
            RectTransform host = UIFactory.Stretch(UIFactory.Rect("LoadingScreen", parent));
            UIFactory.Group(host.gameObject);
            UIFactory.FullscreenBackdrop(host, new Color(0.02f, 0.01f, 0.05f, 1f));
            TextMeshProUGUI title = UIFactory.Text("Title", host, Strings.Menu.Loading, 40f, TextAlignmentOptions.Center, UIFactory.Cyan, FontStyles.Bold);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(900f, 60f));
            Image fill = UIFactory.FillBar("Progress", host, new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(800f, 18f), UIFactory.Magenta);
            fill.fillAmount = 0f;
            TextMeshProUGUI percent = UIFactory.Text("Percent", host, "0%", 26f, TextAlignmentOptions.Center, UIFactory.Muted);
            UIFactory.Place(percent.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(300f, 40f));
            var view = host.gameObject.AddComponent<LoadingScreenView>();
            EditorAssetUtil.SetFields(view, so =>
            {
                so.Require("progressFill").objectReferenceValue = fill;
                so.Require("progressLabel").objectReferenceValue = percent;
                so.Require("titleLabel").objectReferenceValue = title;
            });
        }

        /// <summary>
        /// Operator console (co-op). The frame's anchors are switched at runtime: right half in split-screen, full screen on
        /// a LAN client. Content is laid out for a 960-wide column so it fits both.
        /// </summary>
        private static void BuildOperatorConsole(Transform parent)
        {
            RectTransform host = UIFactory.Stretch(UIFactory.Rect("OperatorConsole", parent));
            Image frame = UIFactory.Image("Frame", host, new Color(0.02f, 0.03f, 0.07f, 0.96f), raycast: true);
            UIFactory.Stretch(frame.rectTransform);
            Image edge = UIFactory.Image("Edge", frame.transform, UIFactory.Cyan);
            UIFactory.Place(edge.rectTransform, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(4f, 2400f), new Vector2(0f, 0.5f));

            RectTransform column = UIFactory.Rect("Column", frame.transform);
            UIFactory.Place(column, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(920f, 1080f), new Vector2(0.5f, 1f));
            TextMeshProUGUI title = UIFactory.Text("Title", column, Strings.Coop.ConsoleTitle, 44f, TextAlignmentOptions.Left, UIFactory.Cyan, FontStyles.Bold);
            UIFactory.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(10f, -24f), new Vector2(600f, 54f));
            TextMeshProUGUI status = UIFactory.Text("Status", column, string.Empty, 22f, TextAlignmentOptions.Left, UIFactory.Muted);
            UIFactory.Place(status.rectTransform, new Vector2(0f, 1f), new Vector2(10f, -82f), new Vector2(900f, 30f));
            Image energy = UIFactory.FillBar("EnergyBar", column, new Vector2(0f, 1f), new Vector2(10f, -124f), new Vector2(520f, 22f), UIFactory.Yellow);
            TextMeshProUGUI energyLabel = UIFactory.Text("EnergyLabel", column, string.Empty, 22f, TextAlignmentOptions.Left, UIFactory.Yellow);
            UIFactory.Place(energyLabel.rectTransform, new Vector2(0f, 1f), new Vector2(548f, -118f), new Vector2(360f, 30f));

            Image mapFrame = UIFactory.Image("Map", column, new Color(0.01f, 0.01f, 0.03f, 1f));
            UIFactory.Place(mapFrame.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -166f), new Vector2(900f, 320f), new Vector2(0.5f, 1f));
            RectTransform terrainRect = UIFactory.Rect("Terrain", mapFrame.transform);
            UIFactory.Place(terrainRect, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(880f, 300f));
            var terrain = terrainRect.gameObject.AddComponent<RawImage>();
            terrain.raycastTarget = false;
            RectTransform icons = UIFactory.Rect("Icons", mapFrame.transform);
            UIFactory.Place(icons, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(880f, 300f));
            Image iconTemplate = UIFactory.Image("IconTemplate", icons, Color.white);
            iconTemplate.rectTransform.sizeDelta = new Vector2(12f, 12f);
            TextMeshProUGUI empty = UIFactory.Text("Empty", mapFrame.transform, Strings.Coop.NoSignal, 26f, TextAlignmentOptions.Center, UIFactory.Muted);
            UIFactory.Place(empty.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860f, 60f));

            RectTransform list = UIFactory.Rect("Targets", column);
            UIFactory.Place(list, new Vector2(0.5f, 1f), new Vector2(0f, -506f), new Vector2(900f, 440f), new Vector2(0.5f, 1f));
            UIFactory.Vertical(list.gameObject, 6f);
            Button entryTemplate = UIFactory.Button("EntryTemplate", list, string.Empty, 900f, 48f);
            TMP_Text entryLabel = entryTemplate.GetComponentInChildren<TMP_Text>(true);
            entryLabel.fontSize = 22f;
            entryLabel.alignment = TextAlignmentOptions.Left;

            TextMeshProUGUI hint = UIFactory.Text("Hint", column, string.Empty, 20f, TextAlignmentOptions.Left, UIFactory.Muted);
            UIFactory.Place(hint.rectTransform, new Vector2(0f, 0f), new Vector2(10f, 30f), new Vector2(900f, 56f));
            Button leave = UIFactory.Button("Leave", column, Strings.Coop.LeaveConsole, 280f, 50f);
            UIFactory.Place((RectTransform)leave.transform, new Vector2(1f, 0f), new Vector2(-150f, 40f), new Vector2(280f, 50f));

            var view = host.gameObject.AddComponent<OperatorConsoleView>();
            UIFactory.SetPanel(view, null);
            EditorAssetUtil.SetFields(view, so =>
            {
                so.Require("frame").objectReferenceValue = frame.rectTransform;
                so.Require("statusLabel").objectReferenceValue = status;
                so.Require("energyFill").objectReferenceValue = energy;
                so.Require("energyLabel").objectReferenceValue = energyLabel;
                so.Require("terrain").objectReferenceValue = terrain;
                so.Require("iconLayer").objectReferenceValue = icons;
                so.Require("iconTemplate").objectReferenceValue = iconTemplate;
                so.Require("listRoot").objectReferenceValue = list;
                so.Require("entryTemplate").objectReferenceValue = entryTemplate;
                so.Require("emptyLabel").objectReferenceValue = empty;
                so.Require("hintLabel").objectReferenceValue = hint;
                so.Require("leaveButton").objectReferenceValue = leave;
                so.Require("mapArea").vector2Value = new Vector2(880f, 300f);
            });
        }

        // ---------------------------------------------------------------- scene canvases

        public static void BuildMainMenuCanvas(BuildContext ctx)
        {
            GameObject canvas = UIFactory.Canvas("MainMenuCanvas", 10);
            RectTransform host = UIFactory.Stretch(UIFactory.Rect("MainMenu", canvas.transform));
            Image side = UIFactory.Image("SidePanel", host, new Color(0.03f, 0.02f, 0.08f, 0.78f));
            UIFactory.Place(side.rectTransform, new Vector2(0f, 0.5f), new Vector2(120f, 0f), new Vector2(620f, 1080f), new Vector2(0f, 0.5f));
            TextMeshProUGUI title = UIFactory.Text("Title", side.transform, Strings.GameTitle, 72f, TextAlignmentOptions.Center, UIFactory.Cyan, FontStyles.Bold);
            title.textWrappingMode = TextWrappingModes.NoWrap;
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -130f), new Vector2(600f, 100f));
            TextMeshProUGUI subtitle = UIFactory.Text("Subtitle", side.transform, Strings.GameSubtitle, 26f, TextAlignmentOptions.Center, UIFactory.Magenta);
            UIFactory.Place(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -250f), new Vector2(600f, 40f));
            RectTransform column = MenuColumn(side.transform, -320f);
            column.sizeDelta = new Vector2(520f, 580f);
            Button cont = UIFactory.Button("Continue", column, Strings.Menu.Continue, 460f, 58f);
            Button newGame = UIFactory.Button("NewGame", column, Strings.Menu.NewGame, 460f, 58f);
            Button coop = UIFactory.Button("Coop", column, Strings.Coop.MenuButton, 460f, 58f);
            Button settings = UIFactory.Button("Settings", column, Strings.Menu.Settings, 460f, 58f);
            Button rubric = UIFactory.Button("Rubric", column, Strings.Menu.RubricShowcase, 460f, 58f);
            Button credits = UIFactory.Button("Credits", column, Strings.Menu.Credits, 460f, 58f);
            Button quit = UIFactory.Button("Quit", column, Strings.Menu.Quit, 460f, 58f);
            CoopMenuView coopMenu = BuildCoopMenu(canvas.transform);
            TextMeshProUGUI version = UIFactory.Text("Version", side.transform, Strings.Menu.Version, 20f, TextAlignmentOptions.Center, UIFactory.Muted);
            UIFactory.Place(version.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(600f, 30f));

            var view = host.gameObject.AddComponent<MainMenuView>();
            UIFactory.SetPanel(view, newGame);
            EditorAssetUtil.SetFields(view, so =>
            {
                so.Require("continueButton").objectReferenceValue = cont;
                so.Require("newGameButton").objectReferenceValue = newGame;
                so.Require("coopButton").objectReferenceValue = coop;
                so.Require("coopMenu").objectReferenceValue = coopMenu;
                so.Require("settingsButton").objectReferenceValue = settings;
                so.Require("rubricButton").objectReferenceValue = rubric;
                so.Require("creditsButton").objectReferenceValue = credits;
                so.Require("quitButton").objectReferenceValue = quit;
                so.Require("versionLabel").objectReferenceValue = version;
            });

            ctx.Prefabs["MainMenuCanvas"] = PrefabUtility.SaveAsPrefabAsset(canvas, $"{EditorPaths.PrefabsUI}/MainMenuCanvas.prefab");
            Object.DestroyImmediate(canvas);
        }

        /// <summary>Co-op panel of the main menu: local split-screen, LAN host, join by IP.</summary>
        private static CoopMenuView BuildCoopMenu(Transform canvas)
        {
            RectTransform host = UIFactory.Stretch(UIFactory.Rect("CoopMenu", canvas));
            Image panel = UIFactory.Image("Panel", host, UIFactory.Panel);
            UIFactory.Place(panel.rectTransform, new Vector2(0f, 0.5f), new Vector2(120f, 0f), new Vector2(760f, 1080f), new Vector2(0f, 0.5f));
            TextMeshProUGUI title = UIFactory.Text("Title", panel.transform, Strings.Coop.Title, 52f, TextAlignmentOptions.Center, UIFactory.Cyan, FontStyles.Bold);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(720f, 70f));
            TextMeshProUGUI explain = UIFactory.Text("Explain", panel.transform, Strings.Coop.Explain, 24f, TextAlignmentOptions.Center, UIFactory.Muted);
            UIFactory.Place(explain.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -200f), new Vector2(680f, 80f));

            RectTransform column = MenuColumn(panel.transform, -310f);
            column.sizeDelta = new Vector2(620f, 560f);
            Button local = UIFactory.Button("Local", column, Strings.Coop.Local, 600f, 60f);
            TextMeshProUGUI localHint = UIFactory.Text("LocalHint", column, Strings.Coop.LocalHint, 18f, TextAlignmentOptions.Center, UIFactory.Muted);
            localHint.rectTransform.sizeDelta = new Vector2(600f, 50f);
            Button hostButton = UIFactory.Button("Host", column, Strings.Coop.Host, 600f, 60f);

            Image addressBox = UIFactory.Image("Address", column, new Color(0.01f, 0.01f, 0.04f, 1f), raycast: true);
            addressBox.rectTransform.sizeDelta = new Vector2(600f, 58f);
            var field = addressBox.gameObject.AddComponent<TMP_InputField>();
            RectTransform area = UIFactory.Stretch(UIFactory.Rect("TextArea", addressBox.transform), 12f);
            area.gameObject.AddComponent<RectMask2D>();
            TextMeshProUGUI placeholder = UIFactory.Text("Placeholder", area, Strings.Coop.AddressPlaceholder, 24f, TextAlignmentOptions.Left, UIFactory.Muted);
            UIFactory.Stretch(placeholder.rectTransform);
            TextMeshProUGUI text = UIFactory.Text("Text", area, string.Empty, 24f, TextAlignmentOptions.Left, Color.white);
            UIFactory.Stretch(text.rectTransform);
            field.textViewport = area;
            field.textComponent = text;
            field.placeholder = placeholder;
            field.characterLimit = 64;
            Button join = UIFactory.Button("Join", column, Strings.Coop.Join, 600f, 60f);
            TextMeshProUGUI status = UIFactory.Text("Status", column, string.Empty, 22f, TextAlignmentOptions.Center, UIFactory.Yellow);
            status.rectTransform.sizeDelta = new Vector2(600f, 40f);
            Button back = UIFactory.Button("Back", column, Strings.Coop.Back, 600f, 60f);

            var view = host.gameObject.AddComponent<CoopMenuView>();
            UIFactory.SetPanel(view, local);
            EditorAssetUtil.SetFields(view, so =>
            {
                so.Require("localButton").objectReferenceValue = local;
                so.Require("hostButton").objectReferenceValue = hostButton;
                so.Require("joinButton").objectReferenceValue = join;
                so.Require("backButton").objectReferenceValue = back;
                so.Require("addressField").objectReferenceValue = field;
                so.Require("statusLabel").objectReferenceValue = status;
            });
            return view;
        }

        public static void BuildCreditsCanvas(BuildContext ctx)
        {
            GameObject canvas = UIFactory.Canvas("CreditsCanvas", 10);
            UIFactory.FullscreenBackdrop(canvas.transform, new Color(0.02f, 0.01f, 0.05f, 0.6f), false);
            RectTransform viewport = UIFactory.Stretch(UIFactory.Rect("Viewport", canvas.transform));
            viewport.gameObject.AddComponent<RectMask2D>();
            RectTransform scroller = UIFactory.Rect("Scroller", viewport);
            UIFactory.Place(scroller, new Vector2(0.5f, 0f), new Vector2(0f, 80f), new Vector2(1200f, 900f), new Vector2(0.5f, 1f));
            TextMeshProUGUI body = UIFactory.Text("Body", scroller, string.Empty, 34f, TextAlignmentOptions.Top);
            UIFactory.Stretch(body.rectTransform);
            TextMeshProUGUI hint = UIFactory.Text("Hint", canvas.transform, string.Empty, 22f, TextAlignmentOptions.Right, UIFactory.Muted);
            UIFactory.Place(hint.rectTransform, new Vector2(1f, 0f), new Vector2(-30f, 20f), new Vector2(500f, 30f));
            var view = canvas.AddComponent<CreditsView>();
            EditorAssetUtil.SetFields(view, so =>
            {
                so.Require("input").objectReferenceValue = ctx.Input;
                so.Require("scroller").objectReferenceValue = scroller;
                so.Require("body").objectReferenceValue = body;
                so.Require("hint").objectReferenceValue = hint;
                so.Require("speed").floatValue = 60f;
                so.Require("endY").floatValue = 2000f;
            });

            ctx.Prefabs["CreditsCanvas"] = PrefabUtility.SaveAsPrefabAsset(canvas, $"{EditorPaths.PrefabsUI}/CreditsCanvas.prefab");
            Object.DestroyImmediate(canvas);
        }
    }
}

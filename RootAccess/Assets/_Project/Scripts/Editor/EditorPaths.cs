namespace RootAccess.EditorTools
{
    /// <summary>Single source of truth for every asset path the editor tooling writes to.</summary>
    public static class EditorPaths
    {
        public const string Root = "Assets/_Project";
        public const string ThirdParty = "Assets/ThirdParty";

        public const string Art = Root + "/Art";
        public const string Sprites = Art + "/Sprites";
        public const string Tilesets = Art + "/Tilesets";
        public const string Materials = Art + "/Materials";
        public const string Shaders = Art + "/Shaders";
        public const string Animations = Art + "/Animations";
        public const string ArtVfx = Art + "/VFX";
        public const string Skybox = Art + "/Skybox";

        public const string Audio = Root + "/Audio";
        public const string Music = Audio + "/Music";
        public const string Sfx = Audio + "/SFX";
        public const string Ambience = Audio + "/Ambience";
        public const string Mixers = Audio + "/Mixers";

        public const string Prefabs = Root + "/Prefabs";
        public const string PrefabsPlayer = Prefabs + "/Player";
        public const string PrefabsEnemies = Prefabs + "/Enemies";
        public const string PrefabsInteractables = Prefabs + "/Interactables";
        public const string PrefabsEnvironment = Prefabs + "/Environment";
        public const string PrefabsUI = Prefabs + "/UI";
        public const string PrefabsVfx = Prefabs + "/VFX";
        public const string PrefabsSystems = Prefabs + "/Systems";
        public const string PrefabsSystemsResources = PrefabsSystems + "/Resources";

        public const string Scenes = Root + "/Scenes";

        public const string ScriptableObjects = Root + "/ScriptableObjects";
        public const string SoItems = ScriptableObjects + "/Items";
        public const string SoQuests = ScriptableObjects + "/Quests";
        public const string SoSkills = ScriptableObjects + "/Skills";
        public const string SoLevels = ScriptableObjects + "/Levels";
        public const string SoAudio = ScriptableObjects + "/Audio";
        public const string SoDialogue = ScriptableObjects + "/Dialogue";
        public const string SoStrings = ScriptableObjects + "/Strings";

        public const string Settings = Root + "/Settings";
        public const string SettingsUrp = Settings + "/URP";
        public const string SettingsInput = Settings + "/Input";
        public const string SettingsMixer = Settings + "/Mixer";

        public const string Scripts = Root + "/Scripts";
        public const string ScriptsInput = Scripts + "/Input";
        public const string Tests = Root + "/Tests";

        public const string InputActionsAsset = ScriptsInput + "/RootAccessInputActions.inputactions";
        public const string InputActionsWrapper = ScriptsInput + "/RootAccessInputActions.cs";
        public const string InputActionsClass = "RootAccessInputActions";
        public const string InputActionsNamespace = "RootAccess.Input";

        public const string UrpAsset = SettingsUrp + "/RootAccess_URP.asset";
        public const string UrpRenderer2D = SettingsUrp + "/RootAccess_Renderer2D.asset";
        public const string UrpUniversalRenderer = SettingsUrp + "/RootAccess_UniversalRenderer.asset";
        public const string UrpPackage = "Packages/com.unity.render-pipelines.universal";
        public const string UrpDefaultPostProcessData = UrpPackage + "/Runtime/Data/PostProcessData.asset";
        public const string TmpEssentialsPackage = "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage";
        public const string TmpEssentialsFolder = "Assets/TextMesh Pro";

        public static readonly string[] SceneFolders =
        {
            "Bootstrap", "MainMenu", "Hub", "Level01_Rooftops", "Level02_Office", "Level03_ServerFarm",
            "Level04_Sewers", "Level05_HQ", "RubricShowcase", "Credits"
        };

        public static readonly string[] ScriptFolders =
        {
            "Core", "Input", "Player", "AI", "Gameplay", "Hacking", "Systems", "UI", "Audio", "VFX", "Cinematics", "Net", "Editor"
        };

        public static readonly string[] AllFolders =
        {
            Sprites, Tilesets, Materials, Shaders, Animations, ArtVfx, Skybox,
            Music, Sfx, Ambience, Mixers,
            PrefabsPlayer, PrefabsEnemies, PrefabsInteractables, PrefabsEnvironment, PrefabsUI, PrefabsVfx, PrefabsSystems,
            PrefabsSystemsResources,
            SoItems, SoQuests, SoSkills, SoLevels, SoAudio, SoDialogue, SoStrings,
            SettingsUrp, SettingsInput, SettingsMixer,
            Tests + "/EditMode", Tests + "/PlayMode",
            ThirdParty
        };

        public static string SceneFolder(string sceneName) => Scenes + "/" + sceneName;
        public static string ScenePath(string sceneName) => SceneFolder(sceneName) + "/" + sceneName + ".unity";
    }
}

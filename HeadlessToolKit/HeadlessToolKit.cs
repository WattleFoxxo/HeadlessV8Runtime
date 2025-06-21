using System;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;

using FrooxEngine;
using HarmonyLib;
using ResoniteModLoader;
using Elements.Core;

using Microsoft.ClearScript;
using Microsoft.ClearScript.JavaScript;
using Microsoft.ClearScript.V8;

using System.Collections.Generic;

namespace HeadlessToolKit;

public class ModuleInfo {
    public required string name { get; set; }
    public required string id { get; set; }
    public required string entry { get; set; }
    public required string type { get; set; }
}

public class HeadlessToolKit : ResoniteMod
{
    public override string Name => "HeadlessToolKit";
    public override string Author => "WattleFoxxo";
    public override string Version => "0.1.0";
    public override string Link => "https://github.com/WattleFoxxo/HeadlessToolKit/";
    const string HarmonyId = "au.wattlefoxxo.HeadlessToolKit";

    [AutoRegisterConfigKey]
    private static readonly ModConfigurationKey<bool> enabled = new ModConfigurationKey<bool>("enabled", "Should the mod be enabled", () => true);
    private static ModConfiguration Config;

    private static string WorkingDirectory = Directory.GetCurrentDirectory();

    private static List<V8ScriptEngine> Engines = new();

    public static object SharedJavascriptObject = new();
    public static EventWrapper OnEngineTick = new();

    public override void OnEngineInit()
    {
        Harmony harmony = new Harmony(HarmonyId);
        harmony.PatchAll();

        Msg("Hello World!");

        FrooxEngine.Engine.Current.OnReady += () =>
        {
            LoadModules("./Scripts");
        };
    }

    [HarmonyPatch(typeof(Userspace))]
    public static class RunUpdates_Patch
    {
        [HarmonyPatch("OnCommonUpdate")]
        [HarmonyPrefix]
        public static void RunUpdates()
        {
            OnEngineTick.Invoke();
        }
    }

    private static void LoadModules(string path)
    {
        DirectoryInfo directoryInfo = new DirectoryInfo(path);
        IEnumerable<DirectoryInfo> directories = directoryInfo.EnumerateDirectories();

        foreach (var directory in directories)
        {
            LoadModule(directory.FullName);
        }
    }

    private static void LoadModule(string path)
    {
        ModuleInfo? moduleInfo = ParseModuleInfo(path);

        if (moduleInfo == null) return;

        DocumentCategory type = moduleInfo.type == "moduleJS" ? ModuleCategory.Standard : ModuleCategory.CommonJS;
        string entry = Path.Combine(path, moduleInfo.entry);

        var engine = new V8ScriptEngine();

        engine.DocumentSettings.AccessFlags =
            DocumentAccessFlags.EnableFileLoading |
            DocumentAccessFlags.AllowCategoryMismatch;
        
        engine.AddHostType(typeof(System.Action));
        engine.AddHostType(typeof(Console));
        engine.AddHostType(typeof(FrooxEngine.IUpdatable));
        engine.AddHostType(typeof(Elements.Core.Transform));
        engine.AddHostType(typeof(Elements.Core.float3));
        engine.AddHostType(typeof(Elements.Core.floatQ));

        engine.AddHostObject("Host", new HostFunctions());
        engine.AddHostObject("SharedObject", SharedJavascriptObject);
        engine.AddHostObject("FrooxEngine", FrooxEngine.Engine.Current);
        engine.AddHostObject("OnEngineTick", OnEngineTick);

        engine.ExecuteDocument(entry, type);
    }

    static ModuleInfo? ParseModuleInfo(string path)
    {
        string moduleFile = Path.Combine(path, "module.json");

        if (!File.Exists(moduleFile))
            throw new FileNotFoundException($"module.json is missing in: {path}");

        string json = File.ReadAllText(moduleFile);


        if (string.IsNullOrEmpty(json))
        {
            Error($"module.json in {path} is missing or invalid.");
            return null;
        }

        ModuleInfo? moduleInfo = JsonSerializer.Deserialize<ModuleInfo>(json);

        if (moduleInfo == null)
        {
            Error($"module.json in {path} is invalid.");
            return null;
        }

        return moduleInfo;
    }
}

public class EventWrapper
{
    public event Action OnEvent;

    public void Invoke()
    {
        OnEvent?.Invoke();
    }

    public void AddListener(Action callback)
    {
        OnEvent += callback;
    }

    public void RemoveListener(Action callback)
    {
        OnEvent -= callback;
    }
}
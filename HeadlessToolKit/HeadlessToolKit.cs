using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;

using FrooxEngine;
using HarmonyLib;
using ResoniteModLoader;

using Microsoft.JavaScript.NodeApi;
using Microsoft.JavaScript.NodeApi.Runtime;
using Microsoft.JavaScript.NodeApi.Interop;
using System.Dynamic;

namespace HeadlessToolKit;

[JSExport]
public class Helpers
{
    public string Name { get; set; } = "HeadlessToolKit";

    public void Ping()
    {
        Console.WriteLine("pong!");
    }

    public int Add(int a, int b) => a + b;
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

    private static string MainScript = @"
        globalThis.require = require('module').createRequire(process.execPath);
        globalThis.node_api_dotnet = {
            import: (specifier) => import(specifier)
        };
    ";

    private static NodeEmbeddingPlatform NodejsPlatform = new NodeEmbeddingPlatform(
        new NodeEmbeddingPlatformSettings
        {
            LibNodePath = Path.Combine(WorkingDirectory, "libnode.so")
        }
    );

    // https://github.com/microsoft/node-api-dotnet/issues/348
    private static NodeEmbeddingThreadRuntime NodejsRuntime = NodejsPlatform.CreateThreadRuntime(
        WorkingDirectory,
        new NodeEmbeddingRuntimeSettings
        {
            MainScript = MainScript,
            Modules = new[] {
                new NodeEmbeddingModuleInfo {
                    Name = "HeadlessToolKit",
                    OnInitialize = (runtime, moduleName, exports) => {
                        exports.SetProperty("Helpers", runtime.Environment.CreateType(typeof(Helpers)));

                        return exports;
                    }
                }
            }
        }
    );

    public override void OnEngineInit()
    {
        Harmony harmony = new Harmony(HarmonyId);
        harmony.PatchAll();

        Msg("Hello World!");

        Task.Run(() => LoadModules(["Test", "EpicGame"]));
    }

    static (string, bool) ParsePackageInfo(string path)
    {
        string packageJson = Path.Combine(path, "package.json");

        if (!File.Exists(packageJson))
            throw new FileNotFoundException($"package.json not found in {path}");

        JsonDocument doc = JsonDocument.Parse(File.ReadAllText(packageJson));
        JsonElement root = doc.RootElement;

        string entry = root.TryGetProperty("main", out var mainProp) && !string.IsNullOrWhiteSpace(mainProp.GetString()) ? mainProp.GetString()! : "index.js";
        bool isModule = root.TryGetProperty("type", out var typeProp) && !string.IsNullOrWhiteSpace(typeProp.GetString()) ? typeProp.GetString()! == "module" : false;

        return (entry, isModule);
    }

    static async Task LoadModules(string[] scripts, string directory = "Scripts")
    {
        await NodejsRuntime.RunAsync(() =>
        {
            var helpers = new Helpers();
            var obj = new JSObject();

            foreach (string script in scripts)
            {
                string path = Path.Combine(Directory.GetCurrentDirectory(), $"{directory}/{script}");
                (string entry, bool isModule) = ParsePackageInfo(path);
                string scriptPath = Path.Combine(path, entry);

                JSValue module = NodejsRuntime.Import(scriptPath, null, isModule);
            }

            return Task.CompletedTask;
        });
    }
}

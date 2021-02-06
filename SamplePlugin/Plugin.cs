using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Hooking;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace NoKillPlugin
{
    public class Plugin : IDalamudPlugin
    {
        public string Name => "No Kill Plugin";

        private const string commandName = "/nokill";

        private DalamudPluginInterface pi;
        private Configuration configuration;
        private PluginUI ui;

        public IntPtr DemoFunc;
        private delegate char DemoFuncDelegate(Int64 a1, Int64 a2, Int64 a3);
        private Hook<DemoFuncDelegate> DemoFuncHook;
        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pi = pluginInterface;

            this.DemoFunc = this.pi.TargetModuleScanner.ScanText("40 53 48 83 EC 30 48 8B D9 49 8B C8 E8 ?? ?? ?? ?? 8B D0");
            this.DemoFuncHook = new Hook<DemoFuncDelegate>(
                DemoFunc,
                new DemoFuncDelegate(DemoFuncDetour)
            );

            this.configuration = this.pi.GetPluginConfig() as Configuration ?? new Configuration();
            this.configuration.Initialize(this.pi);

            // you might normally want to embed resources and load them from the manifest stream
            /*
            var imagePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"goat.png");
            var goatImage = this.pi.UiBuilder.LoadImage(imagePath);
            this.ui = new PluginUI(this.configuration, goatImage);
            */

            this.pi.CommandManager.AddHandler(commandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "A useful message to display in /xlhelp"
            });

            this.DemoFuncHook.Enable();
            /*
            this.pi.UiBuilder.OnBuildUi += DrawUI;
            this.pi.UiBuilder.OnOpenConfigUi += (sender, args) => DrawConfigUI();
            */
        }

        private char DemoFuncDetour(Int64 a1, Int64 a2, Int64 a3)
        {
            IntPtr p3 = new IntPtr(a3);
            var t1 = Marshal.ReadByte(p3);
            var v4 = ((t1 & 0xF) > 0) ? (uint)Marshal.ReadInt32(p3 + 8) : 0;
            PluginLog.Log($"DemoFunc a1:{a1} a2:{a2} a3:{a3} t1:{t1} v4:{v4}");
            if(v4 > 0)
            {
                if(v4 != 340780 || true) // Auth failed
                {
                    Marshal.WriteInt64(p3 + 8, 81536);
                    v4 = ((t1 & 0xF) > 0) ? (uint)Marshal.ReadInt32(p3 + 8) : 0;
                    PluginLog.Log($"After DemoFunc a1:{a1} a2:{a2} a3:{a3} t1:{t1} v4:{v4}");
                }
                else
                {
                    PluginLog.LogError($"账号认证失败，请重新启动游戏。");
                }
            }
            return this.DemoFuncHook.Original(a1, a2, a3);
        }

        public void Dispose()
        {
            this.DemoFuncHook.Disable();
            // this.ui.Dispose();

            this.pi.CommandManager.RemoveHandler(commandName);
            this.pi.Dispose();
        }

        private void OnCommand(string command, string args)
        {
            // in response to the slash command, just display our main ui
            this.ui.Visible = true;
        }

        private void DrawUI()
        {
            this.ui.Draw();
        }

        private void DrawConfigUI()
        {
            this.ui.SettingsVisible = true;
        }
    }
}

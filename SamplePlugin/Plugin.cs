using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Hooking;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.IoC;
using Dalamud.Logging;

namespace NoKillPlugin
{
    public class Plugin : IDalamudPlugin
    {
        public string Name => "No Kill Plugin";

        // private const string commandName = "/nokill";

        [PluginService] private static DalamudPluginInterface pi { get; set; }
        [PluginService] private static SigScanner TargetModuleScanner { get; set; }

        private Configuration configuration;

        public IntPtr DemoFunc;
        private delegate char DemoFuncDelegate(Int64 a1, Int64 a2, Int64 a3);
        private Hook<DemoFuncDelegate> DemoFuncHook;
        public Plugin()
        {
            this.DemoFunc = TargetModuleScanner.ScanText("40 53 48 83 EC 30 48 8B D9 49 8B C8 E8 ?? ?? ?? ?? 8B D0");
            this.DemoFuncHook = new Hook<DemoFuncDelegate>(
                DemoFunc,
                new DemoFuncDelegate(DemoFuncDetour)
            );

            this.configuration = pi.GetPluginConfig() as Configuration ?? new Configuration();
            this.configuration.Initialize(pi);

            this.DemoFuncHook.Enable();
        }

        private char DemoFuncDetour(Int64 a1, Int64 a2, Int64 a3)
        {
            IntPtr p3 = new IntPtr(a3);
            var t1 = Marshal.ReadByte(p3);
            var v4 = ((t1 & 0xF) > 0) ? (uint)Marshal.ReadInt32(p3 + 8) : 0;
            // PluginLog.Log($"DemoFunc a1:{a1} a2:{a2} a3:{a3} t1:{t1} v4:{v4}");
            if(v4 > 0)
            {
                if(v4 != 340780 || true) // Auth failed
                {
                    Marshal.WriteInt64(p3 + 8, 81536);
                    v4 = ((t1 & 0xF) > 0) ? (uint)Marshal.ReadInt32(p3 + 8) : 0;
                    // PluginLog.Log($"After DemoFunc a1:{a1} a2:{a2} a3:{a3} t1:{t1} v4:{v4}");
                }
                else
                {
                    PluginLog.LogError($"Auth error. 账号认证失败，请重新启动游戏。");
                }
            }
            return this.DemoFuncHook.Original(a1, a2, a3);
        }

        public void Dispose()
        {
            this.DemoFuncHook.Disable();
        }
    }
}

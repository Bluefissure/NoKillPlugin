using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Hooking;
using System;
using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Game.ClientState.Conditions;

namespace NoKillPlugin
{
    public class NoKillPlugin : IDalamudPlugin
    {
        public string Name => "No Kill Plugin";

        [PluginService] internal DalamudPluginInterface PluginInterface { get; set; }
        [PluginService] internal Condition Conditions { get; set; }
        [PluginService] internal SigScanner SigScanner { get; set; }
        [PluginService] internal CommandManager CommandManager { get; set; }

        internal static Configuration Config;
        public PluginUi Gui { get; private set; }

        internal IntPtr DemoFunc;
        private delegate char DemoFuncDelegate(Int64 a1, Int64 a2, Int64 a3);
        private Hook<DemoFuncDelegate> DemoFuncHook;
        public NoKillPlugin()
        {
            this.DemoFunc = SigScanner.ScanText("40 53 48 83 EC 30 48 8B D9 49 8B C8 E8 ?? ?? ?? ?? 8B D0");
            this.DemoFuncHook = new Hook<DemoFuncDelegate>(
                DemoFunc,
                new DemoFuncDelegate(DemoFuncDetour)
            );

            Config = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Config.Initialize(PluginInterface);

            CommandManager.AddHandler("/nokill", new CommandInfo(CommandHandler)
            {
                HelpMessage = "/nokill - open the no kill plugin panel."
            });

            Gui = new PluginUi(this);

            this.DemoFuncHook.Enable();
        }
        public void CommandHandler(string command, string arguments)
        {
            var args = arguments.Trim().Replace("\"", string.Empty);

            if (string.IsNullOrEmpty(args) || args.Equals("config", StringComparison.OrdinalIgnoreCase))
            {
                Gui.ConfigWindow.Visible = !Gui.ConfigWindow.Visible;
                return;
            }
        }

        private char DemoFuncDetour(Int64 a1, Int64 a2, Int64 a3)
        {
            IntPtr p3 = new IntPtr(a3);
            var t1 = Marshal.ReadByte(p3);
            var v4 = ((t1 & 0xF) > 0) ? (uint)Marshal.ReadInt32(p3 + 8) : 0;
            UInt16 v4_16 = (UInt16)(v4);
            PluginLog.Log($"DemoFunc a1:{a1} a2:{a2} a3:{a3} t1:{t1} v4:{v4_16}");
            if(v4 > 0)
            {
                this.Gui.ConfigWindow.Visible = true;
                if (v4_16 == 0x332C && Config.SkipAuthError) // Auth failed
                {
                    PluginLog.Log($"Skip Auth Error");
                }
                else
                {
                    Marshal.WriteInt64(p3 + 8, 0x3E80);
                    v4 = ((t1 & 0xF) > 0) ? (uint)Marshal.ReadInt32(p3 + 8) : 0;
                    v4_16 = (UInt16)(v4);
                }
            }
            PluginLog.Log($"After DemoFunc a1:{a1} a2:{a2} a3:{a3} t1:{t1} v4:{v4_16}");
            return this.DemoFuncHook.Original(a1, a2, a3);
        }

        public void Dispose()
        {
            this.DemoFuncHook.Disable();
            CommandManager.RemoveHandler("/nokill");
            Gui?.Dispose();
            PluginInterface?.Dispose();
        }
    }
}

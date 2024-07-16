using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Hooking;
using System;
using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.IoC;
using Dalamud.Plugin.Services;

namespace NoKillPlugin
{
    public class NoKillPlugin : IDalamudPlugin
    {
        public string Name => "No Kill Plugin";

        internal static Configuration Config;
        public PluginUi Gui { get; private set; }

        [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; }
        [PluginService] internal static ICondition Conditions { get; private set; }
        [PluginService] internal static ISigScanner SigScanner { get; private set; }
        [PluginService] internal static ICommandManager CommandManager { get; private set; }
        [PluginService] internal static IGameNetwork GameNetwork { get; private set; }
        [PluginService] internal static IChatGui ChatGui { get; private set; }
        [PluginService] internal static IDataManager DataManager { get; private set; }
        [PluginService] internal static IClientState ClientState { get; private set; }
        [PluginService] internal static IGameInteropProvider HookProvider { get; private set; }
        [PluginService] internal static IPluginLog PluginLog { get; private set; }


        internal IntPtr StartHandler;
        internal IntPtr LoginHandler;
        internal IntPtr LobbyErrorHandler;
        private delegate Int64 StartHandlerDelegate(Int64 a1, Int64 a2);
        private delegate Int64 LoginHandlerDelegate(Int64 a1, Int64 a2);
        private delegate char LobbyErrorHandlerDelegate(Int64 a1, Int64 a2, Int64 a3);
        private Hook<StartHandlerDelegate> StartHandlerHook;
        private Hook<LoginHandlerDelegate> LoginHandlerHook;
        private Hook<LobbyErrorHandlerDelegate> LobbyErrorHandlerHook;
        public NoKillPlugin()
        {
            this.LobbyErrorHandler = SigScanner.ScanText("40 53 48 83 EC 30 48 8B D9 49 8B C8 E8 ?? ?? ?? ?? 8B D0");
            this.LobbyErrorHandlerHook = HookProvider.HookFromAddress<LobbyErrorHandlerDelegate>(
                LobbyErrorHandler, 
                new LobbyErrorHandlerDelegate(LobbyErrorHandlerDetour)
            );
            this.StartHandler = SigScanner.ScanText("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 49 8B CD E8 ?? ?? ?? ?? 45 88 66 08");
            this.StartHandlerHook = HookProvider.HookFromAddress<StartHandlerDelegate>(
                StartHandler,
                new StartHandlerDelegate(StartHandlerDetour)
            );
            this.LoginHandler = SigScanner.ScanText("40 55 53 56 57 41 54 48 8D AC 24 ?? ?? ?? ?? 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 85 ?? ?? ?? ?? 8B B1 ?? ?? ?? ??");
            this.LoginHandlerHook = HookProvider.HookFromAddress<LoginHandlerDelegate>(
                LoginHandler,
                new LoginHandlerDelegate(LoginHandlerDetour)
            );

            Config = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            // Config.Initialize(PluginInterface);

            CommandManager.AddHandler("/nokill", new CommandInfo(CommandHandler)
            {
                HelpMessage = "/nokill - open the no kill plugin panel."
            });

            Gui = new PluginUi(this);

            this.LobbyErrorHandlerHook.Enable();
            this.StartHandlerHook.Enable();
            this.LoginHandlerHook.Enable();
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

        private Int64 StartHandlerDetour(Int64 a1, Int64 a2)
        {
            var a1_88 = (UInt16)Marshal.ReadInt16(new IntPtr(a1 + 88));
            var a1_4320 = Marshal.ReadInt32(new IntPtr(a1 + 4320));
            PluginLog.Debug($"Start a1_4320:{a1_4320}");
            if (a1_4320 != 0 && Config.QueueMode)
            {
                Marshal.WriteInt32(new IntPtr(a1 + 4320), 0);
                PluginLog.Debug($"a1_4320: {a1_4320} => 0");
            }
            return this.StartHandlerHook.Original(a1, a2);
        }
        private Int64 LoginHandlerDetour(Int64 a1, Int64 a2)
        {
            var a1_4321 = Marshal.ReadByte(new IntPtr(a1 + 4321));
            PluginLog.Debug($"Login a1_4321:{a1_4321}");
            if (a1_4321 != 0 && Config.QueueMode)
            {
                Marshal.WriteByte(new IntPtr(a1 + 4321), 0);
                PluginLog.Debug($"a1_4321: {a1_4321} => 0");
            }
            return this.LoginHandlerHook.Original(a1, a2);
        }


        private char LobbyErrorHandlerDetour(Int64 a1, Int64 a2, Int64 a3)
        {
            IntPtr p3 = new IntPtr(a3);
            var t1 = Marshal.ReadByte(p3);
            var v4 = ((t1 & 0xF) > 0) ? (uint)Marshal.ReadInt32(p3 + 8) : 0;
            UInt16 v4_16 = (UInt16)(v4);
            PluginLog.Debug($"LobbyErrorHandler a1:{a1} a2:{a2} a3:{a3} t1:{t1} v4:{v4_16}");
            if (v4 > 0)
            {
                this.Gui.ConfigWindow.Visible = true;
                if (v4_16 == 0x332C && Config.SkipAuthError) // Auth failed
                {
                    PluginLog.Debug($"Skip Auth Error");
                }
                else
                {
                    Marshal.WriteInt64(p3 + 8, 0x3E80); // server connection lost
                    // 0x3390: maintenance
                    v4 = ((t1 & 0xF) > 0) ? (uint)Marshal.ReadInt32(p3 + 8) : 0;
                    v4_16 = (UInt16)(v4);
                }
            }
            PluginLog.Debug($"After LobbyErrorHandler a1:{a1} a2:{a2} a3:{a3} t1:{t1} v4:{v4_16}");
            return this.LobbyErrorHandlerHook.Original(a1, a2, a3);
        }

        public void Dispose()
        {
            this.LobbyErrorHandlerHook.Dispose();
            this.StartHandlerHook.Dispose();
            this.LoginHandlerHook.Dispose();
            CommandManager.RemoveHandler("/nokill");
            Gui?.Dispose();
        }
    }
}

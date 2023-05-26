using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Hooking;
using System;
using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Game.Network;
using Dalamud.Game.Gui;
using Dalamud.Data;
using Dalamud.Game.ClientState;

namespace NoKillPlugin
{
    public class NoKillPlugin : IDalamudPlugin
    {
        public string Name => "No Kill Plugin";

        [PluginService] internal DalamudPluginInterface PluginInterface { get; set; }
        [PluginService] internal Dalamud.Game.ClientState.Conditions.Condition Conditions { get; set; }
        [PluginService] internal SigScanner SigScanner { get; set; }
        [PluginService] internal CommandManager CommandManager { get; set; }
        [PluginService] internal GameNetwork GameNetwork { get; set; }
        [PluginService] internal ChatGui ChatGui { get; set; }
        [PluginService] internal DataManager DataManager { get; set; }
        [PluginService] internal ClientState ClientState { get; set; }

        internal static Configuration Config;
        public PluginUi Gui { get; private set; }

        internal IntPtr StartHandler;
        internal IntPtr LoginHandler;
        internal IntPtr LobbyErrorHandler;
        internal IntPtr DecodeSeStringHandler;
        // internal IntPtr RequestHandler;
        // internal IntPtr ResponseHandler;
        private delegate Int64 StartHandlerDelegate(Int64 a1, Int64 a2);
        private delegate Int64 LoginHandlerDelegate(Int64 a1, Int64 a2);
        private delegate char LobbyErrorHandlerDelegate(Int64 a1, Int64 a2, Int64 a3);
        private delegate void DecodeSeStringHandlerDelegate(Int64 a1, Int64 a2, Int64 a3, Int64 a4);
        // private delegate char RequestHandlerDelegate(Int64 a1, int a2);
        // private delegate void ResponseHandlerDelegate(Int64 a1, Int64 a2, Int64 a3, int a4);
        private Hook<StartHandlerDelegate> StartHandlerHook;
        private Hook<LoginHandlerDelegate> LoginHandlerHook;
        //private Hook<DecodeSeStringHandlerDelegate> DecodeSeStringHandlerHook;
        private Hook<LobbyErrorHandlerDelegate> LobbyErrorHandlerHook;
        // private Regex rx = new Regex(@"2E .. .. .. (?!03)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        /*
        private Hook<RequestHandlerDelegate> RequestHandlerHook;
        private Hook<ResponseHandlerDelegate> ResponseHandlerHook;
        */
        public NoKillPlugin()
        {
            this.LobbyErrorHandler = SigScanner.ScanText("40 53 48 83 EC 30 48 8B D9 49 8B C8 E8 ?? ?? ?? ?? 8B D0");
            this.LobbyErrorHandlerHook = new Hook<LobbyErrorHandlerDelegate>(
                LobbyErrorHandler,
                new LobbyErrorHandlerDelegate(LobbyErrorHandlerDetour)
            );
            try
            {
                this.StartHandler = SigScanner.ScanText("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? B2 01 49 8B CC");
            } catch (Exception)
            {
                this.StartHandler = SigScanner.ScanText("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? B2 01 49 8B CD");
            }
            this.StartHandlerHook = new Hook<StartHandlerDelegate>(
                StartHandler,
                new StartHandlerDelegate(StartHandlerDetour)
            );
            this.LoginHandler = SigScanner.ScanText("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 0F B6 81 ?? ?? ?? ?? 40 32 FF");
            this.LoginHandlerHook = new Hook<LoginHandlerDelegate>(
                LoginHandler,
                new LoginHandlerDelegate(LoginHandlerDetour)
            );

            Config = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Config.Initialize(PluginInterface);

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
            var a1_456 = Marshal.ReadInt32(new IntPtr(a1 + 456));
            PluginLog.Log($"Start a1_456:{a1_456}");
            if (a1_456 != 0 && Config.QueueMode)
            {
                Marshal.WriteInt32(new IntPtr(a1 + 456), 0);
                PluginLog.Log($"a1_456: {a1_456} => 0");
            }
            return this.StartHandlerHook.Original(a1, a2);
        }
        private Int64 LoginHandlerDetour(Int64 a1, Int64 a2)
        {
            var a1_2165 = Marshal.ReadByte(new IntPtr(a1 + 2165));
            PluginLog.Log($"Login a1_2165:{a1_2165}");
            if (a1_2165 != 0 && Config.QueueMode)
            {
                Marshal.WriteByte(new IntPtr(a1 + 2165), 0);
                PluginLog.Log($"a1_2165: {a1_2165} => 0");
            }
            return this.LoginHandlerHook.Original(a1, a2);
        }


        private char LobbyErrorHandlerDetour(Int64 a1, Int64 a2, Int64 a3)
        {
            IntPtr p3 = new IntPtr(a3);
            var t1 = Marshal.ReadByte(p3);
            var v4 = ((t1 & 0xF) > 0) ? (uint)Marshal.ReadInt32(p3 + 8) : 0;
            UInt16 v4_16 = (UInt16)(v4);
            PluginLog.Log($"LobbyErrorHandler a1:{a1} a2:{a2} a3:{a3} t1:{t1} v4:{v4_16}");
            if (v4 > 0)
            {
                this.Gui.ConfigWindow.Visible = true;
                if (v4_16 == 0x332C && Config.SkipAuthError) // Auth failed
                {
                    PluginLog.Log($"Skip Auth Error");
                }
                else
                {
                    Marshal.WriteInt64(p3 + 8, 0x3E80); // server connection lost
                    // 0x3390: maintenance
                    v4 = ((t1 & 0xF) > 0) ? (uint)Marshal.ReadInt32(p3 + 8) : 0;
                    v4_16 = (UInt16)(v4);
                }
            }
            PluginLog.Log($"After LobbyErrorHandler a1:{a1} a2:{a2} a3:{a3} t1:{t1} v4:{v4_16}");
            return this.LobbyErrorHandlerHook.Original(a1, a2, a3);
        }

        public void Dispose()
        {
            this.LobbyErrorHandlerHook.Disable();
            this.StartHandlerHook.Disable();
            this.LoginHandlerHook.Disable();
            CommandManager.RemoveHandler("/nokill");
            Gui?.Dispose();
        }
    }
}

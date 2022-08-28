using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Hooking;
using System;
using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Network;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using System.Collections.Generic;
using Dalamud.Data;
using Dalamud.Game.ClientState;
using Lumina.Excel.GeneratedSheets;
using System.Text.RegularExpressions;

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
        private Hook<DecodeSeStringHandlerDelegate> DecodeSeStringHandlerHook;
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
            /*
            this.DecodeSeStringHandler = SigScanner.ScanText("E8 ?? ?? ?? ?? 8B 5E 60 48 8D 4C 24 ??");
            this.DecodeSeStringHandlerHook = new Hook<DecodeSeStringHandlerDelegate>(
                DecodeSeStringHandler,
                new DecodeSeStringHandlerDelegate(DecodeSeStringHandlerDetour)
            );
            */
            /*
            this.RequestHandler = SigScanner.ScanText("E8 ?? ?? ?? ?? 48 8B 9C 24 ?? ?? ?? ?? 48 8B B4 24 ?? ?? ?? ?? 83 7F 20 00");
            this.RequestHandlerHook = new Hook<RequestHandlerDelegate>(
                RequestHandler,
                new RequestHandlerDelegate(RequestHandlerDetour)
            );
            this.ResponseHandler = SigScanner.ScanText("E8 ?? ?? ?? ?? 48 8D 85 ?? ?? ?? ?? 48 3B F8");
            this.ResponseHandlerHook = new Hook<ResponseHandlerDelegate>(
                ResponseHandler,
                new ResponseHandlerDelegate(ResponseHandlerDetour)
            );
            */

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
            ChatGui.ChatMessage += OnChatMessage;
            //this.DecodeSeStringHandlerHook.Enable();
            //this.RequestHandlerHook.Enable();
            //this.ResponseHandlerHook.Enable();
            //GameNetwork.NetworkMessage += OnNetwork;
        }

        private void OnChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
        {
            if (!Config.SaferMode)
            {
                return;
            }
            PluginLog.Log($"OnChatMessage: {message}");
            foreach (var payLoad in message.Payloads)
            {
                if (payLoad is ItemPayload itemPayload)
                {
                    if ((DataManager.GetExcelSheet<EventItem>(ClientState.ClientLanguage).GetRow(itemPayload.ItemId) == null
                        && (itemPayload.IsHQ && DataManager.GetExcelSheet<EventItem>(ClientState.ClientLanguage).GetRow(itemPayload.ItemId + 1000000) == null))
                        && DataManager.GetExcelSheet<Item>(ClientState.ClientLanguage).GetRow(itemPayload.ItemId) == null)
                    {
                        PluginLog.Log($"ItemId: {itemPayload.ItemId} not found, may crash the game.");
                        isHandled = true;
                    }
                }
            }
        }
        public void CommandHandler(string command, string arguments)
        {
            var args = arguments.Trim().Replace("\"", string.Empty);

            if (string.IsNullOrEmpty(args) || args.Equals("config", StringComparison.OrdinalIgnoreCase))
            {
                Gui.ConfigWindow.Visible = !Gui.ConfigWindow.Visible;
                return;
            }/*
                if (args.Equals("send1", StringComparison.OrdinalIgnoreCase))
                {
                    var payloadList = new List<Payload> {
                        new UIForegroundPayload(551),
                        new UIGlowPayload(552),
                        new ItemPayload(100 + 1500000),
                        new UIForegroundPayload(500),
                        new UIGlowPayload(501),
                        new TextPayload($"{(char) SeIconChar.LinkMarker}"),
                        new UIForegroundPayload(0),
                        new UIGlowPayload(0),
                        new TextPayload("CrashItem"),
                        new RawPayload(new byte[] {0x02, 0x27, 0x07, 0xCF, 0x01, 0x01, 0x01, 0xFF, 0x01, 0x03}),
                        new RawPayload(new byte[] {0x02, 0x13, 0x02, 0xEC, 0x03})
                    };
                    ChatGui.Print("======");
                    ChatGui.Print(new SeString(payloadList));
                    ChatGui.Print("======");
                }
                if (args.Equals("send2", StringComparison.OrdinalIgnoreCase))
                {
                    var payloadList = new List<Payload> {
                        new RawPayload(new byte[] {0x02, 0x2e, 0x0a, 0xc9, 0x05, 0x02, 0x02, 0x01, 0x01, 0xff, 0x02, 0x02, 0x03, 0x00}),
                    };
                    ChatGui.Print("======");
                    ChatGui.Print(new SeString(payloadList));
                    ChatGui.Print("======");
                }
            */
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

        private bool isValidSeString(byte[] managedArray, int len)
        {
            int i = 0;
            while (i < len && i + 1 < len)
            {
                if (managedArray[i] == 0x2E)
                {
                    var sz = managedArray[i + 1];
                    if (i + 1 + sz>= len) return false;
                    if (managedArray[i + 1 + sz] != 0x03) return false;
                }
                i++;
            }
            return true;
        }
        private void DecodeSeStringHandlerDetour(Int64 a1, Int64 a2, Int64 a3, Int64 a4)
        {
            if (!Config.SaferMode)
            {
                this.DecodeSeStringHandlerHook.Original(a1, a2, a3, a4);
                return;
            }

            try
            {
                var a2_byte = Marshal.ReadByte(new IntPtr(a2));
                if (a2_byte == 2)
                {
                    var a2pointer = new IntPtr(a2);
                    var maxlen = 256;
                    int len = 0;
                    while (len < maxlen && Marshal.ReadByte(a2pointer + len) != 0) len++;
                    byte[] managedArray = new byte[len];
                    Marshal.Copy(a2pointer, managedArray, 0, len);
                    var bytesString = BitConverter.ToString(managedArray).Replace("-", " ");
                    if (managedArray[0] == 0x02 && managedArray[1] == 0x2E)
                    {
                        if (!isValidSeString(managedArray, len))
                        {
                            PluginLog.Log($"invalid auto trans array:{bytesString}");
                            return;
                        }else
                        {
                            PluginLog.Log($"valid auto trans array:{bytesString}");
                        }
                    }
                }
            } catch (Exception e)
            {
                PluginLog.Log("Don't crash");
                PluginLog.Log(e.StackTrace);
            }
            this.DecodeSeStringHandlerHook.Original(a1, a2, a3, a4);
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
        /*
        private char RequestHandlerDetour(Int64 a1, int a2)
        {
            IntPtr p1 = new IntPtr(a1 + 2100);
            IntPtr p2 = new IntPtr(a1 + 2082);
            var t1 = Marshal.ReadByte(p1);
            var t2 = Marshal.ReadInt16(p2);
            PluginLog.Log($"RequestHandlerDetour a1:{a1:X} *(a1+2100):{t1} *(a1+2082):{t2} a2:{a2}");
            return this.RequestHandlerHook.Original(a1, a2);
        }
        private void ResponseHandlerDetour(Int64 a1, Int64 a2, Int64 a3, int a4)
        {
            UInt32 A1 = (UInt32)Marshal.ReadInt32(new IntPtr(a1));
            this.ResponseHandlerHook.Original(a1, a2, a3, a4);
            Int32 A2 = Marshal.ReadInt32(new IntPtr(a2));
            Int64 A3 = Marshal.ReadInt64(new IntPtr(a3));
            Int32 v14 = Marshal.ReadInt32(new IntPtr(a3 + 8));
            PluginLog.Log($"ResponseHandlerDetour a1:{a1:X} a2:{a2:X} a3:{a3:X} A3:{A3:X} a4:{a4}");
            return;
        }
        */

        public void Dispose()
        {
            ChatGui.ChatMessage -= OnChatMessage;
            this.LobbyErrorHandlerHook.Disable();
            this.StartHandlerHook.Disable();
            this.LoginHandlerHook.Disable();
            //this.DecodeSeStringHandlerHook.Disable();
            //this.RequestHandlerHook.Disable();
            //this.ResponseHandlerHook.Disable();
            //GameNetwork.NetworkMessage -= OnNetwork;
            CommandManager.RemoveHandler("/nokill");
            Gui?.Dispose();
        }
    }
}

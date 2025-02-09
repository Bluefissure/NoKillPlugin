using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Hooking;
using System;
using Dalamud.IoC;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace NoKillPlugin;

public class NoKillPlugin : IDalamudPlugin
{
    public string Name => "No Kill Plugin";

    internal static Configuration Config;
    private         PluginUi      Gui { get; set; }

    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; }
    [PluginService] internal static ICondition              Conditions      { get; private set; }
    [PluginService] internal static ICommandManager         CommandManager  { get; private set; }
    [PluginService] internal static IClientState            ClientState     { get; private set; }
    [PluginService] internal static IGameInteropProvider    HookProvider    { get; private set; }
    [PluginService] internal static IPluginLog              PluginLog       { get; private set; }

    private unsafe delegate bool LobbyErrorHandlerDelegate(AtkMessageBoxManager* manager, nint a2, AtkValue* values);
    private readonly Hook<LobbyErrorHandlerDelegate> LobbyErrorHandlerHook;
    
    public unsafe NoKillPlugin()
    {
        Config = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        Gui = new PluginUi(this);
        
        CommandManager.AddHandler("/nokill", new CommandInfo(CommandHandler)
        {
            HelpMessage = "/nokill - open the no kill plugin panel."
        });
        
        LobbyErrorHandlerHook =
            HookProvider.HookFromSignature<LobbyErrorHandlerDelegate>(
                "40 53 48 83 EC 30 48 8B D9 49 8B C8 E8 ?? ?? ?? ?? 8B D0",
                LobbyErrorHandlerDetour);
        LobbyErrorHandlerHook.Enable();
    }

    private void CommandHandler(string command, string arguments)
    {
        var args = arguments.Trim().Replace("\"", string.Empty);
        if(!string.IsNullOrEmpty(args) && !args.Equals("config", StringComparison.OrdinalIgnoreCase)) return;
        
        Gui.ConfigWindow.Visible = !Gui.ConfigWindow.Visible;
    }

    private unsafe bool LobbyErrorHandlerDetour(AtkMessageBoxManager* manager, nint a2, AtkValue* values)
    {
        var errorCase = values->UInt;
        
        PluginLog.Debug($"LobbyErrorHandler Error Case: {errorCase}");
        if (errorCase > 0)
        {
            Gui.ConfigWindow.Visible = true;
            if (errorCase == 0x332C && Config.SkipAuthError) // Auth failed
            {
                PluginLog.Debug("Skip Auth Error");
            }
            else
            {
                values->UInt = 0x3E80; // server connection lost; 0x3390: maintenance
            }
        }
        
        PluginLog.Debug($"After LobbyErrorHandler Error Case: {values->UInt}");
        return LobbyErrorHandlerHook.Original(manager, a2, values);
    }

    public void Dispose()
    {
        CommandManager.RemoveHandler("/nokill");
        Gui?.Dispose();
        
        LobbyErrorHandlerHook.Dispose();
    }
}
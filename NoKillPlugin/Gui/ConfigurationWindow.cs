using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.Text;
using ImGuiNET;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Data;
using Dalamud.Logging;
using System.Diagnostics;

namespace NoKillPlugin.Gui
{
    public class ConfigurationWindow
    {
        public NoKillPlugin Plugin;
        public bool WindowVisible;
        public virtual bool Visible
        {
            get => WindowVisible;
            set => WindowVisible = value;
        }
        public Configuration Config => NoKillPlugin.Config;

        public ConfigurationWindow(NoKillPlugin plugin)
        {
            Plugin = plugin;
        }

        public void DrawUi()
        {
            if (NoKillPlugin.Conditions == null) return;
            if (!Visible)
            {
                return;
            }
            ImGui.SetNextWindowSize(new Vector2(530, 160), ImGuiCond.Appearing);
            if (ImGui.Begin($"{Plugin.Name} Panel", ref WindowVisible, ImGuiWindowFlags.NoScrollWithMouse))
            {
                var QueueMode = Config.QueueMode;
                if (ImGui.Checkbox("Queue Mode", ref QueueMode))
                {
                    Config.QueueMode = QueueMode;
                    Config.Save();
                }
                if(ImGui.IsItemHovered())
                    ImGui.SetTooltip("Click this if you encounter lobby error while waiting in queue.");
                var SkipAuthError = Config.SkipAuthError;
                if (ImGui.Checkbox("Skip Auth Error", ref SkipAuthError))
                {
                    Config.SkipAuthError = SkipAuthError;
                    Config.Save();
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Since the auth error won't be gone until a re-login,\n" +
                        "you should close the game and login again.");

                if (ImGui.Button("Donate"))
                {
                    var noKillUrl = "https://www.google.com/search?q=no+kill+shelter";
                    if (((int)NoKillPlugin.ClientState.ClientLanguage) > 3)
                    {
                        noKillUrl = "https://www.baidu.com/s?wd=%E5%AE%A0%E7%89%A9%E6%95%91%E5%8A%A9";
                    }
                    try
                    {
                        Process.Start(new ProcessStartInfo()
                        {
                            FileName = noKillUrl,
                            UseShellExecute = true,
                        });
                    }
                    catch (Exception ex)
                    {
                        NoKillPlugin.PluginLog.Error(ex, "Could not open nokill url");
                    }
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("To No-kill shelters.");
                ImGui.End();
            }
        }
    }
}
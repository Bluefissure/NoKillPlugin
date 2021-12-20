using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.Text;
using ImGuiNET;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Data;
using Dalamud.Logging;

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
            if (Plugin.Conditions == null) return;
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
                    ImGui.SetTooltip("Since the auth error won't be fixed until a re-login,\n" +
                        "you should close the game and login again.");
                ImGui.End();
            }
        }
    }
}
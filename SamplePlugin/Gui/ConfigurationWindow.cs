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
            if (!Visible && Plugin.Conditions.Any())
            {
                return;
            }
            ImGui.SetNextWindowSize(new Vector2(200, 100), ImGuiCond.FirstUseEver);
            if (ImGui.Begin($"{Plugin.Name} Panel", ref WindowVisible, ImGuiWindowFlags.NoScrollWithMouse))
            {
                var SkipAuthError = Config.SkipAuthError;
                if (ImGui.Checkbox("Skip Auth Error", ref SkipAuthError))
                {
                    Config.SkipAuthError = SkipAuthError;
                    Config.Save();
                }
                ImGui.End();
            }
        }
    }
}
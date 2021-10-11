using System;
using NoKillPlugin.Gui;

namespace NoKillPlugin
{
    public class PluginUi : IDisposable
    {
        private readonly NoKillPlugin _plugin;
        public ConfigurationWindow ConfigWindow { get; }

        public PluginUi(NoKillPlugin plugin)
        {
            ConfigWindow = new ConfigurationWindow(plugin);
            _plugin = plugin;
            _plugin.PluginInterface.UiBuilder.Draw += Draw;
            _plugin.PluginInterface.UiBuilder.OpenConfigUi += OnOpenConfigUi;
        }

        private void Draw()
        {
            ConfigWindow.DrawUi();
        }
        private void OnOpenConfigUi()
        {
            ConfigWindow.Visible = true;
        }

        public void Dispose()
        {
            _plugin.PluginInterface.UiBuilder.Draw -= Draw;
            _plugin.PluginInterface.UiBuilder.OpenConfigUi -= OnOpenConfigUi;
        }
    }
}
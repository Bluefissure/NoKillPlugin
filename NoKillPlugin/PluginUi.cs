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
            Service.PluginInterface.UiBuilder.Draw += Draw;
            Service.PluginInterface.UiBuilder.OpenConfigUi += OnOpenConfigUi;
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
            Service.PluginInterface.UiBuilder.Draw -= Draw;
            Service.PluginInterface.UiBuilder.OpenConfigUi -= OnOpenConfigUi;
        }
    }
}
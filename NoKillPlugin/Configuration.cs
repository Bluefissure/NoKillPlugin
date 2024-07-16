using Dalamud.Configuration;
using System;

namespace NoKillPlugin
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public bool SkipAuthError { get; set; } = true;
        public bool QueueMode { get; set; } = false;
        public bool SaferMode { get; set; } = true;
        public int Version { get; set; } = 0;

        public void Save()
        {
            Service.PluginInterface.SavePluginConfig(this);
        }
    }
}

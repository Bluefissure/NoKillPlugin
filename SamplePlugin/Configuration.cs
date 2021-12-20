using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace NoKillPlugin
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public bool SkipAuthError { get; set; } = true;
        public bool QueueMode { get; set; } = false;
        public int Version { get; set; } = 0;

        // the below exist just to make saving less cumbersome

        [NonSerialized]
        private DalamudPluginInterface pluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
        }

        public void Save()
        {
            this.pluginInterface.SavePluginConfig(this);
        }
    }
}

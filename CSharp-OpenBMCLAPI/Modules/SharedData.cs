﻿using CSharpOpenBMCLAPI.Modules.Plugin;

namespace CSharpOpenBMCLAPI.Modules
{
    public static class SharedData
    {
        public static Config Config { get; set; } = new Config();
        public static Logger Logger { get; set; } = new Logger();
        public static ClusterInfo ClusterInfo { get; set; }
        public static TokenManager? Token { get; set; }
        public static PluginManager PluginManager { get; set; } = new PluginManager();
    }
}

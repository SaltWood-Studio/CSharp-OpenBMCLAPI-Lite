﻿using CSharpOpenBMCLAPI.Modules;
using CSharpOpenBMCLAPI.Modules.Plugin;
using CSharpOpenBMCLAPI.Modules.Statistician;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TeraIO.Runnable;
using YamlDotNet.Serialization;

namespace CSharpOpenBMCLAPI
{
    internal class Program : RunnableBase
    {
        public Program() : base() { }

        static void Main(string[] args)
        {
            SharedData.Logger.LogSystem($"Starting CSharp-OpenBMCLAPI v{SharedData.Config.clusterVersion}");
            SharedData.Logger.LogSystem("高性能、低メモリ占有！");
            SharedData.Logger.LogSystem($"运行时环境：{Utils.GetRuntime()}");
            Program program = new Program();
            program.Start();
            program.WaitForStop();
        }

        protected void LoadPlugins()
        {
            string path = Path.Combine(SharedData.Config.clusterFileDirectory, "plugins");

            Directory.CreateDirectory(path);

            foreach (var file in Directory.GetFiles(path))
            {
                if (!file.EndsWith(".dll")) continue;
                try
                {
                    Assembly assembly = Assembly.LoadFrom(file);
                    foreach (var type in assembly.GetTypes())
                    {
                        Type? parent = type;
                        while (parent != null)
                        {
                            if (parent == typeof(PluginBase))
                            {
                                PluginAttribute? attr = type.GetCustomAttribute<PluginAttribute>();
                                if (attr == null || !attr.Hidden)
                                {
                                    SharedData.PluginManager.RegisterPlugin(type);
                                }
                                break;
                            }
                            parent = parent.BaseType;
                        }
                    }
                }
                catch (Exception ex)
                {
                    SharedData.Logger.LogError($"跳过加载插件 {Path.Combine(file)}。加载插件时出现未知错误。\n", Utils.ExceptionToDetail(ex));
                }
            }
        }

        protected Config GetConfig()
        {
            const string configPath = "config.yml";
            if (!File.Exists(configPath))
            {
                Config config = new Config();
                Serializer serializer = new Serializer();
                File.WriteAllText(configPath, serializer.Serialize(config));
                return config;
            }
            else
            {
                string file = File.ReadAllText(configPath);
                Deserializer deserializer = new Deserializer();
                Config? config = deserializer.Deserialize<Config>(file);
                Config result;
                if (config != null)
                {
                    result = config;
                }
                else
                {
                    result = new Config();
                }
                Serializer serializer = new Serializer();
                File.WriteAllText(configPath, serializer.Serialize(config));
                return result;
            }
        }

        protected override int Run(string[] args)
        {
            SharedData.Config = GetConfig();
            LoadPlugins();
            SharedData.PluginManager.TriggerEvent(this, ProgramEventType.ProgramStarted);

            int returns = 0;

            if (File.Exists("totals.bson"))
            {
                DataStatistician t = Utils.BsonDeserializeObject<DataStatistician>(File.ReadAllBytes("totals.bson")).ThrowIfNull();
                SharedData.DataStatistician = t;
            }
            else
            {
                const string bsonFilePath = "totals.bson";
                using (var file = File.Create(bsonFilePath))
                {
                    file.Write(Utils.BsonSerializeObject(SharedData.DataStatistician));
                }
            }

            // 从 .env.json 读取密钥然后 FetchToken
            ClusterInfo info = JsonConvert.DeserializeObject<ClusterInfo>(File.ReadAllTextAsync(".env.json").Result);
            SharedData.ClusterInfo = info;
            SharedData.Logger.LogSystem($"Cluster id: {info.ClusterID}");
            TokenManager token = new TokenManager(info);
            token.FetchToken().Wait();

            SharedData.Token = token;

            Cluster cluster = new(info, token);
            SharedData.Logger.LogSystem($"成功创建 Cluster 实例");
            AppDomain.CurrentDomain.ProcessExit += (sender, e) => Utils.ExitCluster(cluster).Wait();
            Console.CancelKeyPress += (sender, e) => Utils.ExitCluster(cluster).Wait();

            cluster.Start();
            cluster.WaitForStop();

            SharedData.PluginManager.TriggerEvent(this, ProgramEventType.ProgramStopped);
            return returns;
        }
    }
}

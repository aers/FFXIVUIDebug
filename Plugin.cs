using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Dalamud.Plugin;
using FFXIVUIDebug.Attributes;

namespace FFXIVUIDebug
{
    public class Plugin : IDalamudPlugin
    {
        public DalamudPluginInterface pluginInterface;
        public PluginCommandManager<Plugin> commandManager;
        public Configuration config;
        public PluginUI ui;

        public string Name => "UI Debug";

        private void SetLocation(string dllPath)
        {
            Location = dllPath;

            AppDomain.CurrentDomain.AssemblyResolve += delegate (object sender, ResolveEventArgs args)
            {
                string assemblyFile = (args.Name.Contains(','))
                    ? args.Name.Substring(0, args.Name.IndexOf(','))
                    : args.Name;

                assemblyFile += ".dll";

                string absoluteFolder = new FileInfo((new System.Uri(Location)).LocalPath).Directory.FullName;
                string targetPath = Path.Combine(absoluteFolder, assemblyFile);

                try
                {
                    return Assembly.LoadFile(targetPath);
                }
                catch (Exception)
                {
                    return null;
                }
            };

        }

        public string Location { get; private set; } = Assembly.GetExecutingAssembly().Location;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;

            this.config = (Configuration)this.pluginInterface.GetPluginConfig() ?? new Configuration();
            this.config.Initialize(this.pluginInterface);

            this.ui = new PluginUI(this);
            this.ui.Init();
            this.ui.IsVisible = true;
            this.pluginInterface.UiBuilder.OnBuildUi += this.ui.Draw;

            this.commandManager = new PluginCommandManager<Plugin>(this, this.pluginInterface);
        }

        [Command("/uidebug")]
        [HelpMessage("opens UI debug page")]
        public void UIDebugCommand(string command, string args)
        {
            this.ui.IsVisible = true;
        }

        #region IDisposable Support
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            this.commandManager.Dispose();

            this.pluginInterface.SavePluginConfig(this.config);

            this.pluginInterface.UiBuilder.OnBuildUi -= this.ui.Draw;

            this.pluginInterface.Dispose();

            this.ui.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}

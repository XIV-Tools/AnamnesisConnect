// © Anamnesis Connect.
// Licensed under the MIT license.

namespace CustomizePlus
{
	using System;
	using System.Diagnostics;
	using System.IO;
	using System.Reflection;
	using AnamnesisConnect;
	using Dalamud.IoC;
	using Dalamud.Logging;
	using Dalamud.Plugin;

	public sealed class Plugin : IDalamudPlugin
    {
		private readonly CommFile comm;

		public Plugin(
			[RequiredVersion("1.0")] DalamudPluginInterface pluginInterface)
        {
			this.PluginInterface = pluginInterface;

			PluginLog.Information("Starting Anamnesis Connect");

			string? assemblyLocation = Assembly.GetExecutingAssembly().Location;
			string? assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
			string commFilePath = Path.Combine(assemblyDirectory!, "CommFile.txt");

			this.comm = new CommFile(commFilePath);
			this.comm.OnCommandRecieved = (s) =>
			{
				PluginLog.Information(s);
			};

			this.comm.SetAction("TestSomething");
		}

		public DalamudPluginInterface PluginInterface { get; private set; }
		public string Name => "Anamnesis Connect";

		public void Dispose()
        {
			PluginLog.Information("Disposing Anamnesis Connect");
		}
	}
}

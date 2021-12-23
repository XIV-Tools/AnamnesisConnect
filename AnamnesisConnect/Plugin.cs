// © Anamnesis Connect.
// Licensed under the MIT license.

namespace CustomizePlus
{
	using System;
	using System.Diagnostics;
	using AnamnesisConnect;
	using Dalamud.IoC;
	using Dalamud.Logging;
	using Dalamud.Plugin;
	using NamedPipeWrapper;

	public sealed class Plugin : IDalamudPlugin
    {
		private readonly INamedPipe pipe;

		public Plugin(
			[RequiredVersion("1.0")] DalamudPluginInterface pluginInterface)
        {
			this.PluginInterface = pluginInterface;

			PluginLog.Information("Starting Anamnesis Connect");

			int procId = Process.GetCurrentProcess().Id;
			string name = Settings.PipeName + procId;

			Pipe.MessageRecieved += this.Pipe_MessageRecieved;
			this.pipe = Pipe.Connect(name, true);
		}

		public DalamudPluginInterface PluginInterface { get; private set; }
		public string Name => "Anamnesis Connect";

		public void Send(string message)
		{
			try
			{
				Pipe.Send(message);
			}
			catch (Exception ex)
			{
				PluginLog.Error(ex, "Failed to send message");
			}
		}

		public void Dispose()
        {
			PluginLog.Information("Disposing Anamnesis Connect");

			this.pipe.Stop();
		}

		private void Pipe_MessageRecieved(string message)
		{
			PluginLog.Information(message);
		}
	}
}

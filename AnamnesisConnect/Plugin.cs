// © Anamnesis Connect.
// Licensed under the MIT license.

namespace AnamnesisConnect
{
	using System;
	using System.Diagnostics;
	using System.Text.RegularExpressions;
	using System.Threading.Tasks;
	using Dalamud.Game.Command;
	using Dalamud.Game.Gui;
	using Dalamud.Game.Text;
	using Dalamud.IoC;
	using Dalamud.Logging;
	using Dalamud.Plugin;

	public sealed class Plugin : IDalamudPlugin
    {
		public static DalamudPluginInterface? PluginInterface;
		public static ChatGui? Chat;
		public static CommandManager? CommandManager;
		public static PenumbraInterface? PenumbraInterface;

		private readonly CommFile comm;

		public Plugin(
			[RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
			[RequiredVersion("1.0")] ChatGui chatGui,
			[RequiredVersion("1.0")] CommandManager commandManager)
        {
			PluginInterface = pluginInterface;
			Chat = chatGui;
			CommandManager = commandManager;

			PluginLog.Information("Starting Anamnesis Connect");

			Process proc = Process.GetCurrentProcess();
			this.comm = new CommFile(proc, CommFile.Mode.Server);
			this.comm.OnLog = (s) => PluginLog.Information(s);
			this.comm.OnError = (ex) => PluginLog.Error(ex, "Anamnesis Connect Error");

			this.comm.AddHandler(Actions.Handshake, this.OnConnect);
			this.comm.AddHandler(Actions.Disconnect, this.OnDisconnect);

			Task.Run(this.Start);

			try
			{
				IDalamudPlugin? penumbra = DalamudInterface.GetPlugin("Penumbra");
				if (penumbra != null)
				{
					PenumbraInterface = new(penumbra);
					this.comm.AddHandler(Actions.PenumbraRedraw, PenumbraInterface.Redraw);
				}
			}
			catch (Exception ex)
			{
				PluginLog.Error(ex, "Error in penumbra interface");
			}

			Chat?.Print("Anamnesis Connect has started", XivChatType.Debug);
		}

		public string Name => "Anamnesis Connect";

		public void Dispose()
        {
			PluginLog.Information("Disposing Anamnesis Connect");
			this.comm?.Stop();
		}

		private void OnConnect()
		{
			Chat?.Print("Anamnesis Connected");
			PenumbraInterface?.SetPlayerWatcherEnabled(false);
		}

		private void OnDisconnect()
		{
			Chat?.Print("Anamnesis Disconnected");
			PenumbraInterface?.RestorePlayerWatchEnabled();
		}

		private async Task Start()
		{
			PluginLog.Information($"Connecting");
			bool connected = await this.comm.Connect();

			if (!connected)
			{
				PluginLog.Error($"Failed to connect");
			}
		}
	}
}

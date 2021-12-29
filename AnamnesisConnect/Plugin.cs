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

			Task.Run(this.Start);

			try
			{
				IDalamudPlugin? penumbra = DalamudInterface.GetPlugin("Penumbra");
				if (penumbra != null)
				{
					PenumbraInterface = new(penumbra);
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

		private async Task Start()
		{
			PluginLog.Information($"Connecting");
			bool connected = await this.comm.Connect();

			if (!connected)
			{
				PluginLog.Error($"Failed to connect");
			}
		}

		private void ProcessCommand(string str)
		{
			if (str.StartsWith("/"))
			{
				CommandManager?.ProcessCommand(str);
			}
			else if (str.StartsWith("-"))
			{
				// split by spaces unless in quotes.
				string[] parts = Regex.Split(str, "(?<=^[^\"]*(?:\"[^\"]*\"[^\"]*)*) (?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");

				for (int i = 0; i < parts.Length; i++)
					parts[i] = parts[i].Trim().Replace("\"", string.Empty);

				try
				{
					switch (parts[0])
					{
						case "-penumbra":
						{
							PenumbraInterface?.Redraw(parts[1]);
							break;
						}
					}
				}
				catch (Exception ex)
				{
					PluginLog.Error(ex, $"Failed to process Anamnesis command: \"{str}\"");
				}
			}
			else
			{
				Chat?.Print(this.Name + ": " + str, XivChatType.Debug);
			}

			PluginLog.Information($"Recieved Anamnesis command: \"{str}\"");
		}
	}
}

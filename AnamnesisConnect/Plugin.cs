// © Anamnesis Connect.
// Licensed under the MIT license.

namespace CustomizePlus
{
	using System.IO;
	using System.Reflection;
	using AnamnesisConnect;
	using Dalamud.Game.Command;
	using Dalamud.Game.Gui;
	using Dalamud.Game.Text;
	using Dalamud.Game.Text.SeStringHandling;
	using Dalamud.Game.Text.SeStringHandling.Payloads;
	using Dalamud.IoC;
	using Dalamud.Logging;
	using Dalamud.Plugin;

	public sealed class Plugin : IDalamudPlugin
    {
		private readonly CommFile comm;
		private readonly ChatGui chat;
		private readonly CommandManager commandManager;

		public Plugin(
			[RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
			[RequiredVersion("1.0")] ChatGui chatGui,
			[RequiredVersion("1.0")] CommandManager commandManager)
        {
			this.PluginInterface = pluginInterface;
			this.chat = chatGui;
			this.commandManager = commandManager;

			PluginLog.Information("Starting Anamnesis Connect");

			string? assemblyLocation = Assembly.GetExecutingAssembly().Location;
			string? assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
			string commFilePath = Path.Combine(assemblyDirectory!, "CommFile.txt");

			this.comm = new CommFile(commFilePath);
			this.comm.OnCommandRecieved = (s) =>
			{
				if (!this.commandManager.ProcessCommand(s))
					this.SendChat($"Anamneis Connect: {s}", XivChatType.Debug);

				PluginLog.Information($"Recieved Anamnesis command: \"{s}\"");
			};

			this.SendChat("Anamneis Connect has started", XivChatType.Debug);
		}

		public DalamudPluginInterface PluginInterface { get; private set; }
		public string Name => "Anamnesis Connect";

		public void SendChat(string message, XivChatType chatType = XivChatType.Debug)
		{
			TextPayload textPayload = new(message);
			SeString seString = new(textPayload);
			XivChatEntry entry = new();
			entry.Message = seString;
			entry.Type = chatType;
			this.chat.PrintChat(entry);
		}

		public void Dispose()
        {
			PluginLog.Information("Disposing Anamnesis Connect");
			this.comm?.Stop();
		}
	}
}

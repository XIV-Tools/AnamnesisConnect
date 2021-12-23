// © Anamnesis Connect.
// Licensed under the MIT license.

namespace CustomizePlus
{
	using System;
	using System.Diagnostics;
	using System.IO.Pipes;
	using System.Threading.Tasks;
	using AnamnesisConnect;
	using AnamnesisConnect.Shared;
	using Dalamud.IoC;
	using Dalamud.Logging;
	using Dalamud.Plugin;

	public sealed class Plugin : IDalamudPlugin
    {
		private NamedPipeServerStream? server;

		private bool loaded;

		public Plugin(
			[RequiredVersion("1.0")] DalamudPluginInterface pluginInterface)
        {
			this.PluginInterface = pluginInterface;

			PluginLog.Information("Starting Anamnesis Connect");
			this.loaded = true;

			Task.Run(this.Run);
		}

		public DalamudPluginInterface PluginInterface { get; private set; }
		public string Name => "Anamnesis Connect";

		public void Send(string message)
		{
			try
			{
				Pipe.SendMessage(this.server, message);
			}
			catch (Exception ex)
			{
				PluginLog.Error(ex, "Failed to send message");
			}
		}

		public void Dispose()
        {
			PluginLog.Information("Disposing Anamnesis Connect");

			this.loaded = false;

			if (this.server?.IsConnected == true)
				this.server.Disconnect();

			this.server?.Dispose();
		}

		private async Task Run()
		{
			while (this.loaded)
			{
				try
				{
					int procId = Process.GetCurrentProcess().Id;
					string name = Settings.PipeName + procId;

					PluginLog.Information($"Starting server for pipe: {name}");
					this.server = new(name);

					await this.server.WaitForConnectionAsync();
					this.server.ReadMode = PipeTransmissionMode.Message;

					PluginLog.Information("Server connected");

					_ = Task.Run(async () =>
					{
						await Task.Delay(3000);
						this.Send("Hello");
					});

					while (this.server.IsConnected)
					{
						string? message = Pipe.ReadMessage(this.server);

						if (message == null)
							continue;

						PluginLog.Information("Recieved message: " + message);
					}

					PluginLog.Information("Server disconnected");
				}
				catch (Exception ex)
				{
					PluginLog.Error(ex, "Anamnesis Connect server error");

					await Task.Delay(3000);

					if (this.server != null)
					{
						if (this.server.IsConnected)
							this.server.Disconnect();

						this.server.Dispose();
					}
				}
			}

			PluginLog.Information("Shutting down Anamnesis Connect");

			if (this.server != null)
			{
				if (this.server.IsConnected)
					this.server.Disconnect();

				this.server.Dispose();
			}

			PluginLog.Information("Anamnesis Connect has terminated");
		}
    }
}

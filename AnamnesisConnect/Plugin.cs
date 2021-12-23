// © Anamnesis Connect.
// Licensed under the MIT license.

namespace CustomizePlus
{
	using System;
	using System.IO;
	using System.IO.Pipes;
	using System.Threading.Tasks;
	using AnamnesisConnect;
	using Dalamud.IoC;
	using Dalamud.Logging;
	using Dalamud.Plugin;

	public sealed class Plugin : IDalamudPlugin
    {
		private NamedPipeServerStream? server;
		private StreamReader? reader;
		private StreamWriter? writer;

		private bool loaded = true;

		public Plugin(
			[RequiredVersion("1.0")] DalamudPluginInterface pluginInterface)
        {
			this.PluginInterface = pluginInterface;

			PluginLog.Information("Starting Anamnesis Connect");

			Task.Run(this.Run);
		}

		public DalamudPluginInterface PluginInterface { get; private set; }
		public string Name => "Anamnesis Connect";

		public void Send(string message)
		{
			if (this.writer == null)
				return;

			this.writer.WriteLine(message);
			this.writer.Flush();
		}

		public void Dispose()
        {
			this.loaded = false;

			if (this.server != null)
			{
				if (this.server.IsConnected)
					this.server.Disconnect();

				this.server.Dispose();
			}
        }

		private async Task Run()
		{
			while (this.loaded)
			{
				try
				{
					PluginLog.Information("Starting server.");
					this.server = new(Settings.PipeName);

					await this.server.WaitForConnectionAsync();

					this.reader = new StreamReader(this.server);
					this.writer = new StreamWriter(this.server);

					PluginLog.Information("Server connected");

					this.Send("Hello world");

					while (this.server.IsConnected)
					{
						string? message = await this.reader.ReadLineAsync();

						if (message == null)
							continue;

						PluginLog.Information("Recieved message: " + message);
					}

					PluginLog.Information("Server disconnected");
				}
				catch (Exception ex)
				{
					PluginLog.Error(ex, "Anamnesis Connect server error");

					if (this.server != null)
					{
						this.server.Disconnect();
						this.server.Dispose();
					}
				}
			}
		}
    }
}

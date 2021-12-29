using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace AnamnesisConnect.Tests
{
	internal class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("Hello World!");
			Task.Run(Run).Wait();
		}

		private static async Task Run()
		{
			try
			{
				Process proc = Process.GetCurrentProcess();

				Console.WriteLine("Start server...");
				CommFile s = new CommFile(proc, CommFile.Mode.Server);
				s.OnError = (ex) => throw new Exception("Server error", ex);
				s.OnLog = (s) => Console.WriteLine("Server " + s);
				s.AddHandler(Actions.Disconnect, () => Console.WriteLine("Server: Disconnected"));
				bool connected = await s.Connect();
				if (!connected)
					throw new Exception("Failed to connect server");

				Console.WriteLine("Start client...");
				CommFile c = new CommFile(proc, CommFile.Mode.Client);
				c.OnError = (ex) => throw new Exception("Client error", ex);
				c.OnLog = (s) => Console.WriteLine("Client " + s);
				c.AddHandler(Actions.Disconnect, () => Console.WriteLine("Client: Disconnected"));
				connected = await c.Connect();
				if (!connected)
					throw new Exception("Failed to connect server");

				Console.WriteLine("Connected");

				c.Send(Actions.PenumbraRedraw, "Some Person");
				await Task.Delay(1000);

				s.Stop();
				await Task.Delay(1000);
				c.Stop();

				Console.WriteLine("Complete");
			}
			catch (Exception ex)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine(ex.Message);
				Console.WriteLine(ex.StackTrace);
			}
		}
	}
}

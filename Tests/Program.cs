// © Anamnesis Connect.
// Licensed under the MIT license.

namespace Tests
{
	using System;
	using System.Threading.Tasks;
	using AnamnesisConnect;

	internal class Program
	{
		private static CommFile comm;

		static void Main(string[] args)
		{
			Console.WriteLine("Hello World!");

			string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			comm = new CommFile(appData + "\\XIVLauncher\\devPlugins\\AnamnesisConnect\\CommFile.txt", false);

			Random rng = new Random();

			Task.Run(async () =>
			{
				while (true)
				{
					int v = (int)(rng.NextDouble() * 5000);
					await Task.Delay(v);
					Console.WriteLine("> TestSomething " + v);
					comm.SetAction("TestSomething " + v);
				}
			});

			Console.ReadLine();
		}
	}
}

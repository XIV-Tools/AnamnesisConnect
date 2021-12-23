// © Anamnesis Connect.
// Licensed under the MIT license.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AnamnesisConnect
{
	// this is an extremely hacky way to communicate between processes and accross user boundries.
	// may Bill Gates have mercy on my soul.
	public class CommFile
	{
		private readonly string filePath;
		private readonly Mutex mutex;

		public Action<string>? OnCommandRecieved;

		public CommFile(string path, bool canRead = true)
		{
			this.filePath = path;

			if (!File.Exists(path))
			{
				File.CreateText(path);
			}

			mutex = new Mutex(true, "AnamnesisConnectMutexC");

			if (canRead)
			{
				ThreadStart ts = new ThreadStart(WorkerThread);
				Thread t = new Thread(ts);
				t.IsBackground = true;
				t.Start();
			}
		}

		public void SetAction(string action)
		{
			if (!mutex.WaitOne(1000))
				return;

			string current = File.ReadAllText(this.filePath);
			current += "\n" + action;
			File.WriteAllText(this.filePath, current);
			mutex.ReleaseMutex();
		}

		public void WorkerThread()
		{
			while (true)
			{
				Thread.Sleep(100);

				if (!File.Exists(filePath))
					continue;

				if (!mutex.WaitOne(1000))
					continue;
				
				string text = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
				File.WriteAllText(filePath, string.Empty);
				mutex.ReleaseMutex();

				if (string.IsNullOrEmpty(text))
					continue;

				string[] lines = text.Split('\n');

				foreach (string line in lines)
				{
					if (string.IsNullOrEmpty(line))
						continue;

					OnCommandRecieved?.Invoke(line);
				}
			}
		}
	}
}

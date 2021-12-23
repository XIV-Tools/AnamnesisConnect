// © Anamnesis Connect.
// Licensed under the MIT license.

using System;
using System.Diagnostics;
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
		private readonly Semaphore semaphore;
		private bool running;

		public Action<string>? OnCommandRecieved;

		public CommFile(Process proc, bool canRead = true)
		{
			if (proc.MainModule == null)
				throw new Exception("Process has no main module");

			this.filePath = Path.GetDirectoryName(proc.MainModule.FileName) + "/acf.txt";

			if (!File.Exists(this.filePath))
				File.CreateText(this.filePath);

			try
			{
				semaphore = new Semaphore(1,1, @"Global\AnamnesisConnectMutex_" + proc.Id);
			}
			catch (Exception ex)
			{
				throw new Exception("Failed to get semaphor", ex);
			}

			if (canRead)
			{
				this.running = true;
				ThreadStart ts = new ThreadStart(WorkerThread);
				Thread t = new Thread(ts);
				t.IsBackground = true;
				t.Start();
			}
		}

		public void Stop()
		{
			this.running = false;
		}

		public void SetAction(string action)
		{
			try
			{
				if (!semaphore.WaitOne(1000))
					return;

				string current = File.ReadAllText(this.filePath);
				current += "\n" + action;
				File.WriteAllText(this.filePath, current);
				semaphore.Release();
			}
			catch (Exception)
			{
				semaphore.Release();
				throw;
			}
		}

		public void WorkerThread()
		{
			try
			{
				while (this.running)
				{
					Thread.Sleep(100);

					if (!File.Exists(filePath))
						continue;

					if (!semaphore.WaitOne(1000))
						continue;

					string text = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
					File.WriteAllText(filePath, string.Empty);
					semaphore.Release();

					if (string.IsNullOrEmpty(text))
						continue;

					string[] lines = text.Split('\n');

					foreach (string line in lines)
					{
						if (string.IsNullOrEmpty(line))
							continue;

						OnCommandRecieved?.Invoke(line.Trim());
					}
				}
			}
			catch (Exception)
			{
				semaphore.Release();
				throw;
			}
		}
	}
}

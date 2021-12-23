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
		private readonly Mutex mutex;
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
				mutex = new Mutex(true, "AnamnesisConnectMutex_" + proc.Id);
				mutex.WaitOne();
				mutex.ReleaseMutex();
				mutex.ReleaseMutex();
			}
			catch (Exception ex)
			{
				throw new Exception("Failed to get mutex", ex);
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
				if (!mutex.WaitOne(1000))
					return;

				string current = File.ReadAllText(this.filePath);
				current += "\n" + action;
				File.WriteAllText(this.filePath, current);
				mutex.ReleaseMutex();
			}
			catch (Exception)
			{
				mutex.ReleaseMutex();
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

						OnCommandRecieved?.Invoke(line.Trim());
					}
				}
			}
			catch (Exception)
			{
				mutex.ReleaseMutex();
				throw;
			}
		}
	}
}

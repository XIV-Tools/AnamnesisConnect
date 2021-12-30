// © Anamnesis Connect.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AnamnesisConnect
{
	// this is an extremely hacky way to communicate between processes and accross user boundries.
	// may Bill Gates have mercy on my soul.
	public class CommFile
	{
		private readonly string serverToClientPath;
		private readonly string clientToServerPath;
		private readonly Semaphore semaphore;
		private readonly Mode mode;

		private bool running;
		private bool handshakeRecieved = false;

		public Action<Exception>? OnError;
		public Action<string>? OnLog;

		private readonly Dictionary<Actions, List<ListenerBase>> listeners = new Dictionary<Actions, List<ListenerBase>>();

		public enum Mode
		{
			Server,
			Client
		}

		public CommFile(Process proc, Mode mode)
		{
			if (proc.MainModule == null)
				throw new Exception("Process has no main module");

			this.mode = mode;
			this.serverToClientPath = Path.GetDirectoryName(proc.MainModule.FileName) + "/acs.txt";
			this.clientToServerPath = Path.GetDirectoryName(proc.MainModule.FileName) + "/acc.txt";

			try
			{
				semaphore = new Semaphore(1, 1, @"Global\AnamnesisConnect_" + proc.Id);
			}
			catch (Exception ex)
			{
				throw new Exception("Failed to get semaphor", ex);
			}

			this.AddHandler(Actions.Handshake, OnHandshake);
			this.AddHandler(Actions.Disconnect, OnDisconnect);
		}

		private string OutFilePath => mode == Mode.Server ? serverToClientPath : clientToServerPath;
		private string InFilePath => mode == Mode.Server ? clientToServerPath : serverToClientPath;

		public async Task<bool> Connect(int timeout = 1000)
		{
			// only the server can create the files
			try
			{
				if (!semaphore.WaitOne(1000))
					throw new Exception("Failed to get semaphore for file creation");

				if (this.mode == Mode.Server)
				{
					if (!File.Exists(this.serverToClientPath))
					{
						await File.CreateText(this.serverToClientPath).DisposeAsync();
					}

					if (!File.Exists(this.clientToServerPath))
					{
						await File.CreateText(this.clientToServerPath).DisposeAsync();
					}
				}
				else
				{
					// If the files don't exist, then there is no server to connect to.
					if (!File.Exists(this.serverToClientPath))
					{
						return false;
					}

					if (!File.Exists(this.clientToServerPath))
					{
						return false;
					}
				}
			}
			catch (Exception)
			{
				throw;
			}
			finally
			{
				semaphore.Release();
			}

			this.running = true;
			ThreadStart ts = new ThreadStart(WorkerThread);
			Thread t = new Thread(ts);
			t.IsBackground = true;
			t.Start();

			// server does not wait for a connection.
			if (this.mode == Mode.Client)
			{
				Send(Actions.Handshake);

				this.handshakeRecieved = false;

				Stopwatch sw = new Stopwatch();
				sw.Start();
				while (sw.ElapsedMilliseconds < timeout && this.handshakeRecieved == false)
					await Task.Delay(1);

				if (!this.handshakeRecieved)
				{
					return false;
				}
			}

			return true;
		}

		public void Stop()
		{
			if (!this.running)
				return;

			this.running = false;

			if (this.mode == Mode.Server)
			{
				try
				{
					if (!semaphore.WaitOne(1000))
						throw new Exception("Failed to get server semaphore for file deletion");

					File.Delete(this.serverToClientPath);
					File.Delete(this.clientToServerPath);
				}
				catch (Exception)
				{
					throw;
				}
				finally
				{
					semaphore.Release();
				}
			}
			else
			{
				this.Send(Actions.Disconnect);
			}
		}

		public void Send(Actions action, params string[] parameters)
		{
			this.OnLog?.Invoke($"Sending action: \"{action}\" with {parameters.Length} parameters");

			try
			{
				if (!semaphore.WaitOne(1000))
					return;

				string current = File.ReadAllText(this.OutFilePath);

				StringBuilder builder = new StringBuilder();
				builder.AppendLine(current);
				builder.Append(action.ToString());

				// append all params, and wrap them in quotes
				foreach (string param in parameters)
				{
					builder.Append(" \"");
					builder.Append(param);
					builder.Append("\"");
				}

				File.WriteAllText(this.OutFilePath, builder.ToString());
				semaphore.Release();
			}
			catch (Exception)
			{
				semaphore.Release();
				throw;
			}
		}

		public void AddHandler(Actions action, Action callback)
		{
			this.AddHandler(action, new Listener(callback));
		}

		public void AddHandler(Actions action, Action<string> callback)
		{
			this.AddHandler(action, new Listener1(callback));
		}

		public void AddHandler(Actions action, Action<string, string> callback)
		{
			this.AddHandler(action, new Listener2(callback));
		}

		private void AddHandler(Actions action, ListenerBase listener)
		{
			if (!listeners.ContainsKey(action))
				listeners.Add(action, new List<ListenerBase>());

			listeners[action].Add(listener);

			OnLog?.Invoke("Adding Handler:" + action);
		}

		private void WorkerThread()
		{
			try
			{
				while (this.running)
				{
					Thread.Sleep(100);

					string text;

					try
					{
						if (!semaphore.WaitOne(1000))
							continue;

						if (!File.Exists(InFilePath))
						{
							if (this.mode == Mode.Client && this.handshakeRecieved)
								this.HandleAction(Actions.Disconnect, new string[0]);

							continue;
						}

						text = File.ReadAllText(InFilePath, Encoding.UTF8);
						File.WriteAllText(InFilePath, string.Empty);
					}
					finally
					{
						semaphore.Release();
					}

					if (string.IsNullOrEmpty(text))
						continue;

					string[] lines = text.Split('\n');

					foreach (string line in lines)
					{
						if (string.IsNullOrEmpty(line))
							continue;

						this.HandleCommand(line.Trim());
					}
				}
			}
			catch (Exception ex)
			{
				this.OnError?.Invoke(ex);
			}
		}

		private void HandleCommand(string line)
		{
			if (string.IsNullOrEmpty(line))
				return;

			try
			{
				string[] parts = Regex.Split(line, "(?<=^[^\"]*(?:\"[^\"]*\"[^\"]*)*) (?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");

				Actions action;
				if (!Enum.TryParse(parts[0], out action))
					throw new Exception("Unrecognized action string: " + parts[0]);

				string[] param = new string[parts.Length - 1];
				if (parts.Length >= 1)
				{
					for (int i = 1; i < parts.Length; i++)
					{
						string part = parts[i];
						part = part.Trim('\"');
						part = part.Trim();
						param[i - 1] = part;
					}
				}

				this.HandleAction(action, param);
			}
			catch (Exception ex)
			{
				this.OnError?.Invoke(ex);
			}
		}

		private void HandleAction(Actions action, string[] param)
		{
			OnLog?.Invoke("Recieved Action: " + action);

			if (!this.listeners.ContainsKey(action))
				return;

			foreach (ListenerBase listener in this.listeners[action])
			{
				try
				{
					listener.Invoke(param);
				}
				catch (Exception ex)
				{
					this.OnError?.Invoke(ex);
				}
			}
		}

		private void OnHandshake()
		{
			this.handshakeRecieved = true;

			// Servers should respond with a handshake so the
			// client knows we exist.
			if (this.mode == Mode.Server)
			{
				this.Send(Actions.Handshake);
			}
		}

		private void OnDisconnect()
		{
			this.handshakeRecieved = false;
		}

		private abstract class ListenerBase
		{
			public abstract void Invoke(string[] param);
		}

		private class Listener : ListenerBase
		{
			private readonly Action callback;

			public Listener(Action callback)
			{
				this.callback = callback;
			}

			public override void Invoke(string[] param)
			{
				if (param.Length != 0)
					throw new Exception($"Incorrect number of paramaters. Expected 0, got {param.Length}");

				this.callback.Invoke();
			}
		}

		private class Listener1 : ListenerBase
		{
			private readonly Action<string> callback;

			public Listener1(Action<string> callback)
			{
				this.callback = callback;
			}

			public override void Invoke(string[] param)
			{
				if (param.Length != 1)
					throw new Exception($"Incorrect number of paramaters. Expected 1, got {param.Length}");

				this.callback.Invoke(param[0]);
			}
		}

		private class Listener2 : ListenerBase
		{
			private readonly Action<string, string> callback;

			public Listener2(Action<string, string> callback)
			{
				this.callback = callback;
			}

			public override void Invoke(string[] param)
			{
				if (param.Length != 2)
					throw new Exception($"Incorrect number of paramaters. Expected 2, got {param.Length}");

				this.callback.Invoke(param[0], param[1]);
			}
		}
	}
}

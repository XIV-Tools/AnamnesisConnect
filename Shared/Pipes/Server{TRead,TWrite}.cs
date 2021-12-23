// © XIV-Tools.
// Licensed under the MIT license.

// NamedPipeWrapper - https://github.com/LarryMai/named-pipe-wrapper
namespace NamedPipeWrapper
{
	using System;
	using System.Collections.Generic;
	using System.IO.Pipes;
	using NamedPipeWrapper.IO;
	using NamedPipeWrapper.Threading;

	/// <summary>
	/// Wraps a <see cref="NamedPipeServerStream"/> and provides multiple simultaneous client connection handling.
	/// </summary>
	/// <typeparam name="TRead">Reference type to read from the named pipe.</typeparam>
	/// <typeparam name="TWrite">Reference type to write to the named pipe.</typeparam>
	public class Server<TRead, TWrite> : INamedPipe
		where TRead : class
		where TWrite : class
	{
		private readonly string pipeName;
		private readonly PipeSecurity pipeSecurity;
		private readonly List<NamedPipeConnection<TRead, TWrite>> connections = new List<NamedPipeConnection<TRead, TWrite>>();

		private int nextPipeId;

		private volatile bool shouldKeepRunning;
		private volatile bool isRunning;

		/// <summary>
		/// Initializes a new instance of the <see cref="Server{TRead, TWrite}"/> class that listens for client connections on the given <paramref name="pipeName"/>.
		/// </summary>
		/// <param name="pipeName">Name of the pipe to listen on.</param>
		/// <param name="pipeSecurity">the security settings for this pipe.</param>
		public Server(string pipeName, PipeSecurity pipeSecurity)
		{
			this.pipeName = pipeName;
			this.pipeSecurity = pipeSecurity;
		}

		/// <summary>
		/// Invoked whenever a client connects to the server.
		/// </summary>
		public event ConnectionEventHandler<TRead, TWrite> ClientConnected;

		/// <summary>
		/// Invoked whenever a client disconnects from the server.
		/// </summary>
		public event ConnectionEventHandler<TRead, TWrite> ClientDisconnected;

		/// <summary>
		/// Invoked whenever a client sends a message to the server.
		/// </summary>
		public event ConnectionMessageEventHandler<TRead, TWrite> ClientMessage;

		/// <summary>
		/// Invoked whenever an exception is thrown during a read or write operation.
		/// </summary>
		public event PipeExceptionEventHandler Error;

		public bool IsRunning => this.isRunning;

		/// <summary>
		/// Begins listening for client connections in a separate background thread.
		/// This method returns immediately.
		/// </summary>
		public void Start()
		{
			this.shouldKeepRunning = true;
			Worker worker = new Worker();
			worker.Error += this.OnError;
			worker.DoWork(this.ListenSync);
		}

		/// <summary>
		/// Sends a message to all connected clients asynchronously.
		/// This method returns immediately, possibly before the message has been sent to all clients.
		/// </summary>
		public void PushMessage(TWrite message)
		{
			lock (this.connections)
			{
				foreach (NamedPipeConnection<TRead, TWrite> client in this.connections)
				{
					client.PushMessage(message);
				}
			}
		}

		/// <summary>
		/// push message to the given client.
		/// </summary>
		public void PushMessage(TWrite message, string clientName)
		{
			lock (this.connections)
			{
				foreach (NamedPipeConnection<TRead, TWrite> client in this.connections)
				{
					if (client.Name == clientName)
						client.PushMessage(message);
				}
			}
		}

		/// <summary>
		/// Closes all open client connections and stops listening for new ones.
		/// </summary>
		public void Stop()
		{
			this.shouldKeepRunning = false;

			lock (this.connections)
			{
				foreach (NamedPipeConnection<TRead, TWrite> client in this.connections.ToArray())
				{
					client.Close();
				}
			}

			// If background thread is still listening for a client to connect,
			// initiate a dummy connection that will allow the thread to exit.
			// dummy connection will use the local server name.
			NamedPipeClient<TRead, TWrite> dummyClient = new NamedPipeClient<TRead, TWrite>(this.pipeName, ".");
			dummyClient.Start();
			dummyClient.WaitForConnection(TimeSpan.FromSeconds(2));
			dummyClient.Stop();
			dummyClient.WaitForDisconnection(TimeSpan.FromSeconds(2));
		}

		private static void Cleanup(NamedPipeServerStream pipe)
		{
			if (pipe == null) return;
			using (NamedPipeServerStream x = pipe)
			{
				x.Close();
			}
		}

		private void ListenSync()
		{
			this.isRunning = true;
			while (this.shouldKeepRunning)
			{
				this.WaitForConnection(this.pipeName, this.pipeSecurity);
			}

			this.isRunning = false;
		}

		private void WaitForConnection(string pipeName, PipeSecurity pipeSecurity)
		{
			NamedPipeServerStream handshakePipe = null;
			NamedPipeServerStream dataPipe = null;
			NamedPipeConnection<TRead, TWrite> connection = null;

			string connectionPipeName = this.GetNextConnectionPipeName(pipeName);

			try
			{
				// Send the client the name of the data pipe to use
				handshakePipe = PipeServerFactory.CreateAndConnectPipe(pipeName, pipeSecurity);
				PipeStreamWrapper<string, string> handshakeWrapper = new PipeStreamWrapper<string, string>(handshakePipe);
				handshakeWrapper.WriteObject(connectionPipeName);
				handshakeWrapper.WaitForPipeDrain();
				handshakeWrapper.Close();

				// Wait for the client to connect to the data pipe
				dataPipe = PipeServerFactory.CreatePipe(connectionPipeName, pipeSecurity);
				dataPipe.WaitForConnection();

				// Add the client's connection to the list of connections
				connection = ConnectionFactory.CreateConnection<TRead, TWrite>(dataPipe);
				connection.ReceiveMessage += this.ClientOnReceiveMessage;
				connection.Disconnected += this.ClientOnDisconnected;
				connection.Error += this.ConnectionOnError;
				connection.Open();

				lock (this.connections)
				{
					this.connections.Add(connection);
				}

				this.ClientOnConnected(connection);
			}

			// Catch the IOException that is raised if the pipe is broken or disconnected.
			catch (Exception e)
			{
				Console.Error.WriteLine("Named pipe is broken or disconnected: {0}", e);

				Cleanup(handshakePipe);
				Cleanup(dataPipe);

				this.ClientOnDisconnected(connection);
			}
		}

		private void ClientOnConnected(NamedPipeConnection<TRead, TWrite> connection)
		{
			if (this.ClientConnected != null)
				this.ClientConnected(connection);
		}

		private void ClientOnReceiveMessage(NamedPipeConnection<TRead, TWrite> connection, TRead message)
		{
			if (this.ClientMessage != null)
				this.ClientMessage(connection, message);
		}

		private void ClientOnDisconnected(NamedPipeConnection<TRead, TWrite> connection)
		{
			if (connection == null)
				return;

			lock (this.connections)
			{
				this.connections.Remove(connection);
			}

			if (this.ClientDisconnected != null)
				this.ClientDisconnected(connection);
		}

		/// <summary>
		///     Invoked on the UI thread.
		/// </summary>
		private void ConnectionOnError(NamedPipeConnection<TRead, TWrite> connection, Exception exception)
		{
			this.OnError(exception);
		}

		/// <summary>
		///     Invoked on the UI thread.
		/// </summary>
		private void OnError(Exception exception)
		{
			if (this.Error != null)
				this.Error(exception);
		}

		private string GetNextConnectionPipeName(string pipeName)
		{
			return string.Format("{0}_{1}", pipeName, ++this.nextPipeId);
		}
	}
}

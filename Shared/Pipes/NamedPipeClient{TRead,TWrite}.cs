// © XIV-Tools.
// Licensed under the MIT license.

// NamedPipeWrapper - https://github.com/LarryMai/named-pipe-wrapper
namespace NamedPipeWrapper
{
	using System;
	using System.IO.Pipes;
	using System.Threading;
	using NamedPipeWrapper.IO;
	using NamedPipeWrapper.Threading;

	/// <summary>
	/// Wraps a <see cref="NamedPipeClientStream"/>.
	/// </summary>
	/// <typeparam name="TRead">Reference type to read from the named pipe.</typeparam>
	/// <typeparam name="TWrite">Reference type to write to the named pipe.</typeparam>
	public class NamedPipeClient<TRead, TWrite> : INamedPipe
		where TRead : class
		where TWrite : class
	{
		private readonly AutoResetEvent connected = new AutoResetEvent(false);
		private readonly AutoResetEvent disconnected = new AutoResetEvent(false);

		private readonly string pipeName;
		private NamedPipeConnection<TRead, TWrite> connection;

		private volatile bool closedExplicitly;

		/// <summary>
		/// Initializes a new instance of the <see cref="NamedPipeClient{TRead, TWrite}"/> classto connect to the NamedPipeServer specified by <paramref name="pipeName"/>.
		/// </summary>
		/// <param name="pipeName">Name of the server's pipe.</param>
		/// <param name="serverName">the Name of the server, default is  local machine.</param>
		public NamedPipeClient(string pipeName, string serverName)
		{
			this.pipeName = pipeName;
			this.ServerName = serverName;
			this.AutoReconnect = true;
		}

		/// <summary>
		/// Invoked whenever a message is received from the server.
		/// </summary>
		public event ConnectionMessageEventHandler<TRead, TWrite> ServerMessage;

		/// <summary>
		/// Invoked when the client disconnects from the server (e.g., the pipe is closed or broken).
		/// </summary>
		public event ConnectionEventHandler<TRead, TWrite> Disconnected;

		/// <summary>
		/// Invoked whenever an exception is thrown during a read or write operation on the named pipe.
		/// </summary>
		public event PipeExceptionEventHandler Error;

		/// <summary>
		/// Gets or sets a value indicating whether the client should attempt to reconnect when the pipe breaks
		/// due to an error or the other end terminating the connection.
		/// Default value is <c>true</c>.
		/// </summary>
		public bool AutoReconnect { get; set; }

		/// <summary>
		/// Gets or sets the server name, which client will connect to.
		/// </summary>
		private string ServerName { get; set; }

		/// <summary>
		/// Connects to the named pipe server asynchronously.
		/// This method returns immediately, possibly before the connection has been established.
		/// </summary>
		public void Start()
		{
			this.closedExplicitly = false;
			Worker worker = new Worker();
			worker.Error += this.OnError;
			worker.DoWork(this.ListenSync);
		}

		/// <summary>
		///     Sends a message to the server over a named pipe.
		/// </summary>
		/// <param name="message">Message to send to the server.</param>
		public void PushMessage(TWrite message)
		{
			if (this.connection != null)
				this.connection.PushMessage(message);
		}

		/// <summary>
		/// Closes the named pipe.
		/// </summary>
		public void Stop()
		{
			this.closedExplicitly = true;
			if (this.connection != null)
				this.connection.Close();
		}

		public void WaitForConnection()
		{
			this.connected.WaitOne();
		}

		public void WaitForConnection(int millisecondsTimeout)
		{
			this.connected.WaitOne(millisecondsTimeout);
		}

		public void WaitForConnection(TimeSpan timeout)
		{
			this.connected.WaitOne(timeout);
		}

		public void WaitForDisconnection()
		{
			this.disconnected.WaitOne();
		}

		public void WaitForDisconnection(int millisecondsTimeout)
		{
			this.disconnected.WaitOne(millisecondsTimeout);
		}

		public void WaitForDisconnection(TimeSpan timeout)
		{
			this.disconnected.WaitOne(timeout);
		}

		private void ListenSync()
		{
			// Get the name of the data pipe that should be used from now on by this NamedPipeClient
			PipeStreamWrapper<string, string> handshake = PipeClientFactory.Connect<string, string>(this.pipeName, this.ServerName);
			string dataPipeName = handshake.ReadObject();
			handshake.Close();

			// Connect to the actual data pipe
			NamedPipeClientStream dataPipe = PipeClientFactory.CreateAndConnectPipe(dataPipeName, this.ServerName);

			// Create a Connection object for the data pipe
			this.connection = ConnectionFactory.CreateConnection<TRead, TWrite>(dataPipe);
			this.connection.Disconnected += this.OnDisconnected;
			this.connection.ReceiveMessage += this.OnReceiveMessage;
			this.connection.Error += this.ConnectionOnError;
			this.connection.Open();

			this.connected.Set();
		}

		private void OnDisconnected(NamedPipeConnection<TRead, TWrite> connection)
		{
			if (this.Disconnected != null)
				this.Disconnected(connection);

			this.disconnected.Set();

			// Reconnect
			if (this.AutoReconnect && !this.closedExplicitly)
				this.Start();
		}

		private void OnReceiveMessage(NamedPipeConnection<TRead, TWrite> connection, TRead message)
		{
			if (this.ServerMessage != null)
				this.ServerMessage(connection, message);
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
	}
}

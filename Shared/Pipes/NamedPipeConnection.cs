// © XIV-Tools.
// Licensed under the MIT license.

// NamedPipeWrapper - https://github.com/LarryMai/named-pipe-wrapper
namespace NamedPipeWrapper
{
	using System;
	using System.Collections.Concurrent;
	using System.IO.Pipes;
	using System.Runtime.Serialization;
	using System.Threading;
	using NamedPipeWrapper.IO;
	using NamedPipeWrapper.Threading;

	/// <summary>
	/// Handles new connections.
	/// </summary>
	/// <param name="connection">The newly established connection.</param>
	/// <typeparam name="TRead">read type.</typeparam>
	/// <typeparam name="TWrite">written type.</typeparam>
	public delegate void ConnectionEventHandler<TRead, TWrite>(NamedPipeConnection<TRead, TWrite> connection)
		where TRead : class
		where TWrite : class;

	/// <summary>
	/// Handles messages received from a named pipe.
	/// </summary>
	/// <typeparam name="TRead">Reference type.</typeparam>
	/// <typeparam name="TWrite">read type.</typeparam>
	/// <param name="connection">Connection that received the message.</param>
	/// <param name="message">Message sent by the other end of the pipe.</param>
	public delegate void ConnectionMessageEventHandler<TRead, TWrite>(NamedPipeConnection<TRead, TWrite> connection, TRead message)
		where TRead : class
		where TWrite : class;

	/// <summary>
	/// Handles exceptions thrown during read/write operations.
	/// </summary>
	/// <typeparam name="TRead">read type.</typeparam>
	/// <typeparam name="TWrite">written type.</typeparam>
	/// <param name="connection">Connection that threw the exception.</param>
	/// <param name="exception">The exception that was thrown.</param>
	public delegate void ConnectionExceptionEventHandler<TRead, TWrite>(NamedPipeConnection<TRead, TWrite> connection, Exception exception)
		where TRead : class
		where TWrite : class;

	/// <summary>
	/// Represents a connection between a named pipe client and server.
	/// </summary>
	/// <typeparam name="TRead">Reference type to read from the named pipe.</typeparam>
	/// <typeparam name="TWrite">Reference type to write to the named pipe.</typeparam>
	public class NamedPipeConnection<TRead, TWrite>
		where TRead : class
		where TWrite : class
	{
		/// <summary>
		/// Gets the connection's unique identifier.
		/// </summary>
		public readonly int Id;

		private readonly PipeStreamWrapper<TRead, TWrite> streamWrapper;
		private readonly AutoResetEvent writeSignal = new AutoResetEvent(false);

		/// <summary>
		/// To support Multithread, we should use BlockingCollection.
		/// </summary>
		private readonly BlockingCollection<TWrite> writeQueue = new BlockingCollection<TWrite>();

		private bool notifiedSucceeded;

		internal NamedPipeConnection(int id, string name, PipeStream serverStream)
		{
			this.Id = id;
			this.Name = name;
			this.streamWrapper = new PipeStreamWrapper<TRead, TWrite>(serverStream);
		}

		/// <summary>
		/// Invoked when the named pipe connection terminates.
		/// </summary>
		public event ConnectionEventHandler<TRead, TWrite> Disconnected;

		/// <summary>
		/// Invoked whenever a message is received from the other end of the pipe.
		/// </summary>
		public event ConnectionMessageEventHandler<TRead, TWrite> ReceiveMessage;

		/// <summary>
		/// Invoked when an exception is thrown during any read/write operation over the named pipe.
		/// </summary>
		public event ConnectionExceptionEventHandler<TRead, TWrite> Error;

		/// <summary>
		/// Gets the connection's name.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Gets a value indicating whether the pipe is connected or not.
		/// </summary>
		public bool IsConnected => this.streamWrapper.IsConnected;

		/// <summary>
		/// Begins reading from and writing to the named pipe on a background thread.
		/// This method returns immediately.
		/// </summary>
		public void Open()
		{
			Worker readWorker = new Worker();
			readWorker.Succeeded += this.OnSucceeded;
			readWorker.Error += this.OnError;
			readWorker.DoWork(this.ReadPipe);

			Worker writeWorker = new Worker();
			writeWorker.Succeeded += this.OnSucceeded;
			writeWorker.Error += this.OnError;
			writeWorker.DoWork(this.WritePipe);
		}

		/// <summary>
		/// Adds the specified <paramref name="message"/> to the write queue.
		/// The message will be written to the named pipe by the background thread
		/// at the next available opportunity.
		/// </summary>
		public void PushMessage(TWrite message)
		{
			this.writeQueue.Add(message);
			this.writeSignal.Set();
		}

		/// <summary>
		/// Closes the named pipe connection and underlying <c>PipeStream</c>.
		/// </summary>
		public void Close()
		{
			this.CloseImpl();
		}

		/// <summary>
		///     Invoked on the background thread.
		/// </summary>
		private void CloseImpl()
		{
			this.streamWrapper.Close();
			this.writeSignal.Set();
		}

		/// <summary>
		///     Invoked on the UI thread.
		/// </summary>
		private void OnSucceeded()
		{
			// Only notify observers once
			if (this.notifiedSucceeded)
				return;

			this.notifiedSucceeded = true;

			if (this.Disconnected != null)
				this.Disconnected(this);
		}

		/// <summary>
		/// Invoked on the UI thread.
		/// </summary>
		private void OnError(Exception exception)
		{
			if (this.Error != null)
				this.Error(this, exception);
		}

		/// <summary>
		///     Invoked on the background thread.
		/// </summary>
		/// <exception cref="SerializationException">An object in the graph of type parameter <typeparamref name="TRead"/> is not marked as serializable.</exception>
		private void ReadPipe()
		{
			while (this.IsConnected && this.streamWrapper.CanRead)
			{
				try
				{
					TRead obj = this.streamWrapper.ReadObject();
					if (obj == null)
					{
						this.CloseImpl();
						return;
					}

					if (this.ReceiveMessage != null)
					{
						this.ReceiveMessage(this, obj);
					}
				}
				catch
				{
					// we must igonre exception, otherwise, the namepipe wrapper will stop work.
				}
			}
		}

		/// <summary>
		///     Invoked on the background thread.
		/// </summary>
		/// <exception cref="SerializationException">An object in the graph of type parameter <typeparamref name="TWrite"/> is not marked as serializable.</exception>
		private void WritePipe()
		{
			while (this.IsConnected && this.streamWrapper.CanWrite)
			{
				try
				{
					// using blockcollection, we needn't use singal to wait for result.
					// writeSignal.WaitOne();
					// while (_writeQueue.Count > 0)
					{
						this.streamWrapper.WriteObject(this.writeQueue.Take());
						this.streamWrapper.WaitForPipeDrain();
					}
				}
				catch
				{
					// we must igonre exception, otherwise, the namepipe wrapper will stop work.
				}
			}
		}
	}
}

// © Anamnesis Connect.
// Licensed under the MIT license.

namespace AnamnesisConnect
{
	using System;
	using System.IO.Pipes;
	using System.Security.AccessControl;
	using System.Security.Principal;
	using NamedPipeWrapper;

	public class Pipe
	{
		public static Action<string>? Log;

		private static NamedPipeServer<string>? server;
		private static NamedPipeClient<string>? client;

		public delegate void PacketRecievedDelegate(string message);

		public static event PacketRecievedDelegate? MessageRecieved;

		public static bool IsConnected { get; private set; }

		public static INamedPipe Connect(string pipeName, bool isServer)
		{
			if (isServer)
			{
				PipeSecurity pipeSecurity = new PipeSecurity();
				pipeSecurity.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null), PipeAccessRights.ReadWrite, AccessControlType.Allow));

				NamedPipeServer<string> server = new NamedPipeServer<string>(pipeName, pipeSecurity);
				ConnectInternal(server, pipeName);
				return server;
			}
			else
			{
				NamedPipeClient<string> client = new NamedPipeClient<string>(pipeName);
				ConnectInternal(client, pipeName);
				return client;
			}
		}

		public static void Send(string message)
		{
			// Not connected
			if (!IsConnected)
				throw new Exception("Pipe is not connected");

			server?.PushMessage(message);
			client?.PushMessage(message);
		}

		protected static void ConnectInternal(NamedPipeServer<string> newServer, string pipeName)
		{
			server = newServer;

			if (server != null)
			{
				server.ClientConnected += (c) =>
				{
					Log?.Invoke("Client connected");
				};

				server.ClientMessage += (c, message) => HandleMessage(message);

				Log?.Invoke($"Starting Pipe Server: {pipeName}");
				server.Start();
			}

			IsConnected = true;
		}

		protected static void ConnectInternal(NamedPipeClient<string> newClient, string pipeName)
		{
			client = newClient;

			if (client != null)
			{
				client.ServerMessage += (c, message) => HandleMessage(message);

				Log?.Invoke($"Starting Pipe Client: {pipeName}");
				client.Start();
			}

			IsConnected = true;
		}

		private static void HandleMessage(string message)
		{
			try
			{
				MessageRecieved?.Invoke(message);
			}
			catch (Exception ex)
			{
				Log?.Invoke("Failed to handle pipe message");
			}
		}
	}
}

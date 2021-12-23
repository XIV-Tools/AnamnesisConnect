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

	internal static class ConnectionFactory
	{
		private static int lastId;

		public static NamedPipeConnection<TRead, TWrite> CreateConnection<TRead, TWrite>(PipeStream pipeStream)
			where TRead : class
			where TWrite : class
		{
			return new NamedPipeConnection<TRead, TWrite>(++lastId, "Client " + lastId, pipeStream);
		}
	}
}

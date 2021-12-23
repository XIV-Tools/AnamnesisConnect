// © XIV-Tools.
// Licensed under the MIT license.

// NamedPipeWrapper - https://github.com/LarryMai/named-pipe-wrapper
namespace NamedPipeWrapper
{
	using System.IO.Pipes;

	/// <summary>
	/// Wraps a <see cref="NamedPipeServerStream"/> and provides multiple simultaneous client connection handling.
	/// </summary>
	/// <typeparam name="TReadWrite">Reference type to read from and write to the named pipe.</typeparam>
	public class NamedPipeServer<TReadWrite> : Server<TReadWrite, TReadWrite>
		where TReadWrite : class
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="NamedPipeServer{TReadWrite}"/> class that listens for client connections on the given <paramref name="pipeName"/>.
		/// </summary>
		/// <param name="pipeName">Name of the pipe to listen on.</param>
		public NamedPipeServer(string pipeName)
			: base(pipeName, null)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="NamedPipeServer{TReadWrite}"/> class that listens for client connections on the given <paramref name="pipeName"/>.
		/// </summary>
		/// <param name="pipeName">Name of the pipe to listen on.</param>
		/// <param name="pipeSecurity">the security settings for this pipe.</param>
		public NamedPipeServer(string pipeName, PipeSecurity pipeSecurity)
			: base(pipeName, pipeSecurity)
		{
		}
	}
}

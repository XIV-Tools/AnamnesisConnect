// © XIV-Tools.
// Licensed under the MIT license.

// NamedPipeWrapper - https://github.com/LarryMai/named-pipe-wrapper
namespace NamedPipeWrapper
{
	using System.IO.Pipes;

	/// <summary>
	/// Wraps a <see cref="NamedPipeClientStream"/>.
	/// </summary>
	/// <typeparam name="TReadWrite">Reference type to read from and write to the named pipe.</typeparam>
	public class NamedPipeClient<TReadWrite> : NamedPipeClient<TReadWrite, TReadWrite>
		where TReadWrite : class
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="NamedPipeClient{TReadWrite}"/> class to connect to the NamedPipeNamedPipeServer specified by <paramref name="pipeName"/>.
		/// </summary>
		/// <param name="pipeName">Name of the server's pipe.</param>
		/// <param name="serverName">server name default is local.</param>
		public NamedPipeClient(string pipeName, string serverName = ".")
			: base(pipeName, serverName)
		{
		}
	}
}

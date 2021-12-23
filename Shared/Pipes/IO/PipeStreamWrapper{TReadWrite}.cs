// © XIV-Tools.
// Licensed under the MIT license.

// NamedPipeWrapper - https://github.com/LarryMai/named-pipe-wrapper
namespace NamedPipeWrapper.IO
{
	using System.IO.Pipes;

	/// <summary>
	/// Wraps a <see cref="PipeStream"/> object to read and write .NET CLR objects.
	/// </summary>
	/// <typeparam name="TReadWrite">Reference type to read from and write to the pipe.</typeparam>
	public class PipeStreamWrapper<TReadWrite> : PipeStreamWrapper<TReadWrite, TReadWrite>
		where TReadWrite : class
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PipeStreamWrapper{TReadWrite}"/> class that reads from and writes to the given <paramref name="stream"/>.
		/// </summary>
		/// <param name="stream">Stream to read from and write to.</param>
		public PipeStreamWrapper(PipeStream stream)
			: base(stream)
		{
		}
	}
}

// © XIV-Tools.
// Licensed under the MIT license.

// NamedPipeWrapper - https://github.com/LarryMai/named-pipe-wrapper
namespace NamedPipeWrapper
{
	using System;

	/// <summary>
	/// Handles exceptions thrown during a read or write operation on a named pipe.
	/// </summary>
	/// <param name="exception">Exception that was thrown.</param>
	public delegate void PipeExceptionEventHandler(Exception exception);
}
// © XIV-Tools.
// Licensed under the MIT license.

// NamedPipeWrapper - https://github.com/LarryMai/named-pipe-wrapper
namespace NamedPipeWrapper
{
	using System.IO.Pipes;

	internal static class PipeServerFactory
	{
		public static NamedPipeServerStream CreateAndConnectPipe(string pipeName, PipeSecurity pipeSecurity)
		{
			NamedPipeServerStream pipe = CreatePipe(pipeName, pipeSecurity);
			pipe.WaitForConnection();
			return pipe;
		}

		public static NamedPipeServerStream CreatePipe(string pipeName, PipeSecurity pipeSecurity)
		{
			return NamedPipeServerStreamConstructors.New(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.WriteThrough, 0, 0, pipeSecurity);
		}
	}
}

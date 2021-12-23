// © Anamnesis Connect.
// Licensed under the MIT license.

namespace AnamnesisConnect.Shared
{
	using System.IO;
	using System.IO.Pipes;
	using System.Text;
	using System.Threading.Tasks;

	public static class Pipe
	{
		public static async Task<string?> ReadMessage(PipeStream? pipe)
		{
			if (pipe == null)
				return null;

			if (!pipe.CanRead)
				return null;

			byte[] buffer = new byte[1024];
			using MemoryStream? ms = new MemoryStream();

			do
			{
				int readBytes = await pipe.ReadAsync(buffer, 0, buffer.Length);
				await ms.WriteAsync(buffer, 0, readBytes);
			}
			while (!pipe.IsMessageComplete);

			byte[] totalBytes = ms.ToArray();

			if (totalBytes.Length <= 0)
				return null;

			return Encoding.UTF8.GetString(totalBytes);
		}

		public static async Task SendMessage(PipeStream? pipe, string message)
		{
			if (pipe == null)
				return;

			if (!pipe.CanWrite)
				return;

			byte[] bytes = Encoding.Default.GetBytes(message);
			await pipe.WriteAsync(bytes, 0, bytes.Length);
		}
	}
}

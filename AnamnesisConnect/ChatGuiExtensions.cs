// © Anamnesis Connect.
// Licensed under the MIT license.

namespace AnamnesisConnect
{
	using Dalamud.Game.Gui;
	using Dalamud.Game.Text;
	using Dalamud.Game.Text.SeStringHandling;
	using Dalamud.Game.Text.SeStringHandling.Payloads;

	public static class ChatGuiExtensions
	{
		public static void Print(this ChatGui self, string message, XivChatType chatType = XivChatType.Debug)
		{
			TextPayload textPayload = new(message);
			SeString seString = new(textPayload);
			XivChatEntry entry = new();
			entry.Message = seString;
			entry.Type = chatType;
			self.PrintChat(entry);
		}
	}
}

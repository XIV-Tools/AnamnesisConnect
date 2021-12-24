// © Anamnesis Connect.
// Licensed under the MIT license.

namespace AnamnesisConnect
{
	using System;
	using System.Reflection;
	using Dalamud.Logging;
	using Dalamud.Plugin;

	public class PenumbraInterface
	{
		private readonly IDalamudPlugin? plugin;
		private readonly object collectionManager;

		////  ModManager.Collections.SetCharacterCollection( name, _collections[ tmp ] );

		public PenumbraInterface()
		{
			this.plugin = DalamudInterface.GetPlugin("Penumbra");
			this.collectionManager = this.GetCollectionManager();
		}

		/// <summary>
		/// Gets the Penumbra.Mods.CollectionManager instance.
		/// </summary>
		private object GetCollectionManager()
		{
			Assembly? penumbraAssembly = this.plugin?.GetType().Assembly;

			if (penumbraAssembly == null)
				throw new Exception("Failed to get penumbra assembly from plugin");

			Type? modManagerType = penumbraAssembly.GetType("Penumbra.Mods.ModManager");
			Type? modManagerServiceType = penumbraAssembly.GetType("Penumbra.Util.Service`1[[Penumbra.Mods.ModManager]]");
			object? modManager = modManagerServiceType?.GetMethod("Get")?.Invoke(null, null);

			if (modManagerType == null || modManager == null)
				throw new Exception("Failed to get penumbra mod manager");

			Type? collectionManagerType = penumbraAssembly.GetType("Penumbra.Mods.CollectionManager");
			object? collectionManager = modManagerType.GetProperty("Collections")?.GetValue(modManager);

			if (collectionManagerType == null || collectionManager == null)
				throw new Exception("Failed to get penumbra mod manager");

			return (collectionManager, collectionManagerType);
		}
	}
}

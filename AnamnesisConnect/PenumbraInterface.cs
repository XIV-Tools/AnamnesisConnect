// © Anamnesis Connect.
// Licensed under the MIT license.

namespace AnamnesisConnect
{
	using System;
	using System.Reflection;
	using Dalamud.Logging;
	using Dalamud.Plugin;

	public class PenumbraInterface : IDisposable
	{
		private readonly IDalamudPlugin plugin;
		private readonly Assembly assembly;
		private readonly object playerWatcher;
		private readonly object configuration;
		private readonly object objectReloader;
		private readonly object collectionManager;

		////  ModManager.Collections.SetCharacterCollection( name, _collections[ tmp ] );

		public PenumbraInterface(IDalamudPlugin penumbra)
		{
			this.plugin = penumbra;
			this.assembly = this.GetAssembly();
			this.configuration = this.GetConfiguration();
			this.objectReloader = this.GetObjectReloader();
			this.collectionManager = this.GetCollectionManager();
			this.playerWatcher = this.GetPlayerWatcher();
		}

		public void Dispose()
		{
			this.RestorePlayerWatchEnabled();
		}

		public void Redraw(string actorName)
		{
			// Since penumbra has a slash command for this, lets just use it.
			Plugin.CommandManager?.ProcessCommand($"/penumbra redraw {actorName}");
		}

		public void SetPlayerWatcherEnabled(bool enable)
		{
			this.playerWatcher.GetType()?.GetMethod("SetStatus")?.Invoke(this.playerWatcher, new object?[] { enable });
		}

		public void RestorePlayerWatchEnabled()
		{
			this.SetPlayerWatcherEnabled(this.GetConfig<bool>("EnablePlayerWatch"));
		}

		private Assembly GetAssembly()
		{
			Assembly? assembly = this.plugin?.GetType().Assembly;

			if (assembly == null)
				throw new Exception("Failed to get penumbra assembly from plugin");

			return assembly;
		}

		private object GetConfiguration()
		{
			object? config = this.plugin.GetType()?.GetProperty("Config")?.GetValue(null);

			if (config == null)
				throw new Exception("Failed to get Config");

			return config;
		}

		private T GetConfig<T>(string name)
		{
			PropertyInfo? prop = this.configuration.GetType().GetProperty(name);

			if (prop == null)
				throw new Exception($"Failed to get Penumbra configuration option: {name}");

			object? val = prop.GetValue(this.configuration);

			if (val is T tVal)
				return tVal;

			throw new Exception($"Penumbra configuration option: {name} was not type: {typeof(T)}");
		}

		private object GetPlayerWatcher()
		{
			object? objectReloader = this.plugin.GetType()?.GetProperty("PlayerWatcher")?.GetValue(null);

			if (objectReloader == null)
				throw new Exception("Failed to get Player Watcher");

			return objectReloader;
		}

		/// <summary>
		/// Gets the Penumbra.Interop.ObjectReloader.
		/// </summary>
		private object GetObjectReloader()
		{
			object? objectReloader = this.plugin.GetType()?.GetProperty("ObjectReloader")?.GetValue(this.plugin);

			if (objectReloader == null)
				throw new Exception("Failed to get object reloader");

			return objectReloader;
		}

		/// <summary>
		/// Gets the Penumbra.Mods.CollectionManager instance.
		/// </summary>
		private object GetCollectionManager()
		{
			Type? modManagerType = this.assembly.GetType("Penumbra.Mods.ModManager");
			Type? modManagerServiceType = this.assembly.GetType("Penumbra.Util.Service`1[[Penumbra.Mods.ModManager]]");
			object? modManager = modManagerServiceType?.GetMethod("Get")?.Invoke(null, null);

			if (modManagerType == null || modManager == null)
				throw new Exception("Failed to get penumbra mod manager");

			Type? collectionManagerType = this.assembly.GetType("Penumbra.Mods.CollectionManager");
			object? collectionManager = modManagerType.GetProperty("Collections")?.GetValue(modManager);

			if (collectionManagerType == null || collectionManager == null)
				throw new Exception("Failed to get penumbra mod manager");

			return (collectionManager, collectionManagerType);
		}

	}
}

// © Anamnesis Connect.
// Licensed under the MIT license.

namespace AnamnesisConnect
{
	using System;
	using System.Collections;
	using System.Reflection;
	using Dalamud.Logging;
	using Dalamud.Plugin;

	public static class DalamudInterface
	{
		/// <summary>
		/// Gets an isntance of the loaded dalamud plugin, if it exists.
		/// Does not work for DevPlugins, only live.
		/// </summary>
		public static IDalamudPlugin? GetPlugin(string name)
		{
			try
			{
				Type? pluginManagerServiceType = Type.GetType("Dalamud.Service`1[[Dalamud.Plugin.Internal.PluginManager, Dalamud]], Dalamud");
				MethodInfo? method = pluginManagerServiceType?.GetMethod("Get");
				object? manager = method?.Invoke(null, null);

				if (manager == null)
					throw new Exception("Failed to get dalamud plugin manager");

				Type pluginManagerType = manager.GetType();
				IEnumerable? installedPlugins = pluginManagerType.GetProperty("InstalledPlugins")?.GetValue(manager, null) as IEnumerable;

				if (installedPlugins == null)
					throw new Exception("Failed to get installed plugins");

				Type? localPluginType = Type.GetType("Dalamud.Plugin.Internal.LocalPlugin, Dalamud");
				FieldInfo? instanceField = localPluginType?.GetField("instance", BindingFlags.NonPublic | BindingFlags.Instance);

				if (instanceField == null)
					throw new Exception("Faield to get local plugin instance field or type");

				foreach (object? plugin in installedPlugins)
				{
					IDalamudPlugin? dalamudPlugin = instanceField.GetValue(plugin) as IDalamudPlugin;

					if (dalamudPlugin?.Name == name)
					{
						return dalamudPlugin;
					}
				}

				return null;
			}
			catch (Exception ex)
			{
				PluginLog.Error(ex, "Error attempting to get dalamud plugin");
			}

			return null;
		}
	}
}

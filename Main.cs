using System;
using System.Collections.Generic;
using System.IO;
using BetterTOS2;
using Server.Shared.Extensions;
using SML;
using UnityEngine;

namespace MatchReplaysPlus
{
    /// <summary>
    /// Mod entry point. Sets up the replay storage folder, loads the mod's asset
    /// bundle (sprites/UI prefab), and pulls in BetterTOS2's role icons if that mod
    /// is loaded.
    /// </summary>
    [Mod.SalemMod]
    public class Main
    {
        public static readonly string FolderPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), "SalemModLoader", "ModFolders", "MatchReplays");
        public static readonly string DebugPath = Path.Combine(FolderPath, "Debug");
        public static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();
        public static readonly Dictionary<string, Sprite> BtosSprites = new Dictionary<string, Sprite>();
        public static AssetBundle Bundle;

        public void Start()
        {
            if (!Directory.Exists(FolderPath))
            {
                Directory.CreateDirectory(FolderPath);
                ModLogger.Log("Created directory " + FolderPath);
            }

            Bundle = FromAssetBundle.GetAssetBundleFromResources("MatchReplaysPlus.Resources.Replays", typeof(Main).Assembly);
            Bundle.LoadAllAssets<Sprite>().ForEach(sprite => Sprites.Add(sprite.name, sprite));
			// DumpAssetBundleContents();

            if (ModStates.IsLoaded("curtis.tuba.better.tos2"))
            {
                FetchBtosIcons();
            }
        }

        // NOTE: BTOSInfo (from BetterTOS2) is an optional dependency - it may not be
        // installed at all. Referencing it directly inside a try/catch is NOT enough
        // to guard against that: the CLR must still JIT-verify every type touched by
        // a method's IL before that method can run, so a missing BetterTOS2.dll would
        // throw a TypeLoadException as soon as *this* method were entered, before the
        // try block ever executes. Splitting the BTOSInfo access out into its own
        // method sidesteps that: the outer method's JIT succeeds regardless of whether
        // BetterTOS2 is present, and only the call to the inner method can fail - which
        // the try/catch here can actually catch.
        public static bool IsBtos()
        {
            try
            {
                return IsBtosBypass();
            }
            catch
            {
                return false;
            }
        }

        private static bool IsBtosBypass()
        {
            return BTOSInfo.IS_MODDED;
        }

        // See the note on IsBtos() above - same JIT-isolation trick.
        private static void FetchBtosIcons()
        {
            try
            {
                FetchBtosIconsBypass();
            }
            catch
            {
                // Ignored - BetterTOS2 isn't installed/loaded, so there are no icons to fetch.
            }
        }

        private static void FetchBtosIconsBypass()
        {
            foreach (KeyValuePair<string, Sprite> entry in BTOSInfo.sprites)
            {
                BtosSprites.Add(entry.Key, entry.Value);
            }
        }
		
		private static void DumpAssetBundleContents()
		{
			try
			{
				if (!Directory.Exists(FolderPath))
				{
					Directory.CreateDirectory(FolderPath);
				}

				string logPath = Path.Combine(FolderPath, "AssetBundleContents.txt");

				string[] assets = Bundle.GetAllAssetNames();

				using (StreamWriter writer = new StreamWriter(logPath, false))
				{
					writer.WriteLine("MatchReplaysPlus Asset Bundle Dump");
					writer.WriteLine("================================");
					writer.WriteLine($"Total Assets: {assets.Length}");
					writer.WriteLine();

					foreach (string asset in assets)
					{
						writer.WriteLine(asset);
					}
				}

				ModLogger.Log($"Exported asset bundle contents to {logPath}");
			}
			catch (Exception ex)
			{
				ModLogger.Log($"Failed to dump asset bundle contents: {ex}");
			}
		}
    }
}
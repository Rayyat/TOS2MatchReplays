using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using BetterTOS2;
using Home.Common;
using Home.Common.Tooltips;
using Home.Shared;
using Home.Shared.Enums;
using SalemModLoader;
using SalemModLoaderUI;
using Server.Shared.Info;
using Server.Shared.State;
using Services;
using SML;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UObject = UnityEngine.Object;

namespace MatchReplaysPlus
{
    /// <summary>
    /// Drives the Match Replays browser UI: lists every saved .replay file with its
    /// role/faction/outcome, and lets the player launch or delete one, or jump to
    /// installing BetterTOS2 if a replay needs it.
    /// </summary>
    public class MatchReplayUIController : UIController
    {
        private GameObject Template;
        private GameObject NoReplays;
        private GameObject ScrollView;
        private GameObject OpenDirectoryButton;
        private GameObject BackButton;
        private Coroutine CoSetup;

        public void Start()
        {
            CacheObjects();
            CoSetup = StartCoroutine(SetupReplayMenu());
        }

        public void OnDestroy()
        {
            if (CoSetup != null)
            {
                StopCoroutine(CoSetup);
            }
            CoSetup = null;
        }

        private void CacheObjects()
        {
            Template = transform.Find("Scroll View/Viewport/Content/ReplayTemplate").gameObject;
            NoReplays = transform.Find("NoReplays").gameObject;
            ScrollView = transform.Find("Scroll View").gameObject;
            OpenDirectoryButton = transform.Find("Buttons/OpenDirectory").gameObject;
            BackButton = transform.Find("Buttons/BackButton").gameObject;
            Template.SetActive(false);
        }

        private IEnumerator SetupReplayMenu()
        {
            BackButton.GetComponent<Button>().onClick.AddListener(() => UObject.Destroy(gameObject));
            BackButton.AddComponent<TooltipTrigger>().NonLocalizedString = "Close Replay Menu";

            OpenDirectoryButton.GetComponent<Button>().onClick.AddListener(OpenDirectory);
            OpenDirectoryButton.AddComponent<TooltipTrigger>().NonLocalizedString = "Open Replay Folder";

            DirectoryInfo directoryInfo = new DirectoryInfo(Main.FolderPath);
            IOrderedEnumerable<FileInfo> files = directoryInfo.GetFiles().OrderByDescending(f => f.LastWriteTime);

            int replayCount = files.Count(f => f.FullName.EndsWith(".replay"));
            ScrollView.SetActive(replayCount > 0);
            NoReplays.SetActive(replayCount == 0);

            foreach (FileInfo fileInfo in files)
            {
                if (fileInfo.FullName.EndsWith(".replay"))
                {
                    ReplayInfo replayInfo = GetReplayInfo(fileInfo.FullName);
                    if (replayInfo != null)
                    {
                        CreateReplayListEntry(fileInfo, replayInfo);
                    }
                }

                // Spread the directory listing across frames rather than doing it all at once.
                yield return null;
            }
        }

        private void CreateReplayListEntry(FileInfo fileInfo, ReplayInfo replayInfo)
        {
            GameObject entry = UObject.Instantiate(Template, Template.transform.parent);
            TextMeshProUGUI nameText = entry.transform.Find("ReplayName").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI roleText = entry.transform.Find("RoleName").GetComponent<TextMeshProUGUI>();
            Button playButton = entry.transform.Find("PlayReplay").GetComponent<Button>();
            Image thumbnail = entry.transform.Find("Thumbnail").GetComponent<Image>();

            nameText.SetText(BuildDisplayName(fileInfo.FullName, replayInfo));

            if (!replayInfo.BetterTOS2)
            {
                SetupVanillaReplayEntry(fileInfo, replayInfo, roleText, thumbnail, playButton);
            }
            else if (ModStates.IsEnabled("curtis.tuba.better.tos2"))
            {
                SetupBtosReplayEntry(fileInfo, replayInfo, roleText, thumbnail, playButton);
            }
            else if (ModStates.IsLoaded("curtis.tuba.better.tos2"))
            {
                roleText.text = "Enable <b>BetterTOS2</b> in the mod settings to view this replay";
                thumbnail.sprite = Main.Sprites["Unknown"];
                playButton.gameObject.SetActive(false);
            }
            else
            {
                roleText.text = "Install <b>BetterTOS2</b> from the mod browser to view this replay";
                thumbnail.sprite = Main.Sprites["Unknown"];
                playButton.onClick.AddListener(() => SearchForBrowserMod("BetterTOS2"));
                playButton.GetComponentInChildren<TextMeshProUGUI>().text = "Install";
            }

            entry.SetActive(true);
        }

        /// <summary>
        /// Derives the display name for a replay from its file name: strips the
        /// ".replay" extension and the trailing "_&lt;date&gt;" / "#&lt;date&gt;" saved by
        /// <see cref="SaveAsReplayFile"/>, splits the remaining camelCase/PascalCase text
        /// into words, and flags the entry as outdated if it was recorded on a different
        /// build than the one currently running.
        /// </summary>
        private static string BuildDisplayName(string fullPath, ReplayInfo replayInfo)
        {
            string fileNameWithoutSuffix = Path.GetFileNameWithoutExtension(fullPath)
                .Split('_')[0]
                .Split('%')[0]
                .Split('#')[0];

            string displayName = SplitCamelCase(fileNameWithoutSuffix);

            if (replayInfo.BuildVersion != SharedConstants.BUILD_VERSION)
            {
                displayName += "<color=#DD0000> Outdated</color>";
            }

            return displayName;
        }

        /// <summary>
        /// Inserts a space before each capital letter that isn't itself preceded by a
        /// capital letter or whitespace, e.g. "TownTraitor" -&gt; "Town Traitor".
        /// </summary>
        private static string SplitCamelCase(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            StringBuilder result = new StringBuilder();
            result.Append(text[0]);

            for (int i = 1; i < text.Length; i++)
            {
                if (char.IsUpper(text[i]) && !char.IsUpper(text[i - 1]) && !char.IsWhiteSpace(text[i - 1]))
                {
                    result.Append(' ');
                }
                result.Append(text[i]);
            }

            return result.ToString();
        }

        private void SetupVanillaReplayEntry(FileInfo fileInfo, ReplayInfo replayInfo, TextMeshProUGUI roleText, Image thumbnail, Button playButton)
        {
            bool fancyUi = ModStates.IsEnabled("alchlcsystm.fancy.ui");
            int winningFactionIconId = GetVanillaRoleIconId(replayInfo.FactionWin);

            string factionSuffix = fancyUi ? $" ({FactionName(replayInfo.PlayerFaction).Replace(" ", "")})" : "";
            roleText.text =
                $"Role: <sprite=\"RoleIcons{factionSuffix}\" " +
                $"name=\"Role{(int)replayInfo.PlayerRole}\">{replayInfo.PlayerRole.ToColorizedDisplayString(replayInfo.PlayerFaction)} - " +
                $"Winning Faction: <sprite=\"RoleIcons (Regular)\" " +
                $"name=\"Role{winningFactionIconId}\">{replayInfo.FactionWin.ToColorizedDisplayString()}";

            thumbnail.sprite = Main.Sprites[GetVanillaGameTypeSpriteKey(replayInfo.GameType)];

            playButton.onClick.AddListener(() => StartCoroutine(JoinReplay(fileInfo.FullName)));
        }

        private static int GetVanillaRoleIconId(FactionType factionWin)
        {
            switch (factionWin)
            {
                case FactionType.TOWN: return 100;
                case FactionType.COVEN: return 106;
                case FactionType.SERIALKILLER: return 48;
                case FactionType.ARSONIST: return 40;
                case FactionType.WEREWOLF: return 51;
                case FactionType.SHROUD: return 49;
                case FactionType.APOCALYPSE: return 114;
                case FactionType.VAMPIRE: return 52;
                case FactionType.CURSED_SOUL: return 53;
                case FactionType.DOOMSAYER: return 43;
                case FactionType.EXECUTIONER: return 44;
                case FactionType.JESTER: return 45;
                case FactionType.PIRATE: return 46;
                default:
                    return 241;
            }
        }

        private static string GetVanillaGameTypeSpriteKey(GameType gameType)
        {
            switch (gameType)
            {
                case GameType.Classic: return "Classic";
                case GameType.Custom: return "Custom";
                case GameType.AllAny: return "AllAny";
                case GameType.DraculasPalace: return "DracsPalace";
                case GameType.CursedSouls: return "WanderingSouls";
                case GameType.RankedPractice: return "RankedPractice";
                case GameType.Ranked: return "Ranked";
                case GameType.TownTraitor: return "TownTraitor";
                case GameType.VIP: return "VIP";
                case GameType.NoTownHanged: return "PerfectTown";
                case GameType.AnonymousVotes: return "AnonVotes";
                case GameType.NoRoleReveal: return "HiddenRoles";
                case GameType.HiddenKillers: return "SecretKillers";
                case GameType.GhostTown: return "GhostTown";
                case GameType.OneTrialPerDay: return "OneTrial";
                default: return "Unknown";
            }
        }

        private void SetupBtosReplayEntry(FileInfo fileInfo, ReplayInfo replayInfo, TextMeshProUGUI roleText, Image thumbnail, Button playButton)
        {
            // BTOSInfo.IS_MODDED needs to be true for BetterTOS2's colorized display
            // strings/icons to render this replay's (possibly BTOS-only) role and
            // faction correctly, regardless of whether BTOS2 mode is actually running
            // right now - so it's toggled on for the duration of this call and back
            // off again afterwards.
            ToggleModded(true);
            try
            {
                bool fancyUi = ModStates.IsEnabled("alchlcsystm.fancy.ui");
                int winningFactionIconId = GetBtosRoleIconId(replayInfo.FactionWin, fancyUi);

                string factionSuffix = fancyUi ? $" ({FactionName(replayInfo.PlayerFaction).Replace(" ", "")})" : "";
                roleText.text =
                    $"Role: <sprite=\"BTOSRoleIcons{factionSuffix}\" name=\"Role" +
                    $"{(int)replayInfo.PlayerRole}\">{replayInfo.PlayerRole.ToColorizedDisplayString(replayInfo.PlayerFaction)} - " +
                    $"Winning Faction: <sprite=\"BTOSRoleIcons (Regular)\" name=\"Role" +
                    $"{winningFactionIconId}\">{replayInfo.FactionWin.ToColorizedDisplayString()}";

                thumbnail.sprite = ResolveBtosThumbnail(fileInfo.FullName, replayInfo.GameType);

                playButton.onClick.AddListener(() => StartCoroutine(JoinReplay(fileInfo.FullName)));
            }
            finally
            {
                ToggleModded(false);
            }
        }

        private static int GetBtosRoleIconId(FactionType factionWin, bool fancyUi)
        {
            switch (factionWin)
            {
                case FactionType.TOWN: return 101;
                case FactionType.COVEN: return 107;
                case FactionType.SERIALKILLER: return 48;
                case FactionType.ARSONIST: return 40;
                case FactionType.WEREWOLF: return 51;
                case FactionType.SHROUD: return 49;
                case FactionType.APOCALYPSE: return 113;
                case FactionType.VAMPIRE: return 52;
                case FactionType.CURSED_SOUL: return 53;
                case FactionType.DOOMSAYER: return 43;
                case FactionType.EXECUTIONER: return 44;
                case FactionType.JESTER: return 45;
                case FactionType.PIRATE: return 46;
                case (FactionType)33: return 55; // Jackal
                case (FactionType)34: // Frogs
                case (FactionType)35: // Lions
                case (FactionType)36: // Hawks
                    return 213;
                case (FactionType)43: return 108; // Pandora
                case (FactionType)44: return 116; // Compliance
                case (FactionType)38: return 57; // Judge
                case (FactionType)39: return 58; // Auditor
                case (FactionType)40: return 59; // Inquisitor
                case (FactionType)41: return 60; // Starspawn
                case (FactionType)42: return 216; // Egotist
                case (FactionType)250: return 228; // Lovers
                default:
                    return 241;
            }
        }

        private static Sprite ResolveBtosThumbnail(string fullPath, GameType gameType)
        {
            string gameTypeKey = gameType switch
            {
                GameType.Custom => "Custom",
                GameType.Ranked => "RoleCard_Starspawn",
                _ => "Unknown"
            };

            string fileName = Path.GetFileName(fullPath);

            // Older replay files may embed a specific role-card sprite key between '%'
            // and '#' in the file name - if present, prefer that over the game-type sprite.
            if (fileName.Contains('%') && fileName.Contains('#'))
            {
                string roleCardKey = fileName.Split('%').Last().Split('#')[0];

                if (Main.BtosSprites.TryGetValue(roleCardKey, out Sprite roleCardSprite))
                {
                    return roleCardSprite;
                }
            }

            if (Main.BtosSprites.TryGetValue(gameTypeKey, out Sprite btosSprite))
            {
                return btosSprite;
            }

            if (Main.Sprites.TryGetValue(gameTypeKey, out Sprite sprite))
            {
                return sprite;
            }

            return Main.Sprites["Unknown"];
        }

        // NOTE: BTOSInfo (from BetterTOS2) is an optional dependency that may not be
        // installed. The actual write is split into its own method so a missing
        // BetterTOS2.dll only fails to JIT that method, not this one - see the matching
        // note on Main.IsBtos() for the full explanation.
        private static void ToggleModded(bool value)
        {
            try
            {
                ToggleModdedBypass(value);
            }
            catch
            {
                // Ignored - BetterTOS2 isn't installed/loaded.
            }
        }

        private static void ToggleModdedBypass(bool value)
        {
            BTOSInfo.IS_MODDED = value;
        }

        /// <summary>
        /// Maps a role to the faction it belongs to. Currently unused within this mod
        /// (kept for potential external/API use) - reconstructed from decompiled IL, so
        /// double-check against the game's actual Role/FactionType enums before relying
        /// on it for anything BTOS-specific.
        /// </summary>
        public static FactionType GetFactionType(Role role, bool btos)
        {
            switch (role)
            {
                case Role.ARSONIST:
                    return FactionType.ARSONIST;
                case Role.BAKER:
                case Role.BERSERKER:
                case Role.PLAGUEBEARER:
                case Role.SOULCOLLECTOR:
                    return FactionType.NONE;
                case Role.DOOMSAYER:
                    return FactionType.DOOMSAYER;
                case Role.EXECUTIONER:
                    return FactionType.EXECUTIONER;
                case Role.JESTER:
                    return FactionType.JESTER;
                case Role.PIRATE:
                    return FactionType.PIRATE;
                case Role.SERIALKILLER:
                    return FactionType.SERIALKILLER;
                case Role.SHROUD:
                    return FactionType.SHROUD;
                case Role.WEREWOLF:
                    return FactionType.WEREWOLF;
                case Role.VAMPIRE:
                    return FactionType.VAMPIRE;
                case Role.CURSED_SOUL:
                    return FactionType.CURSED_SOUL;
                case Role.SOCIALITE:
                    return btos ? FactionType.COVEN : FactionType.NONE;
                case Role.MARSHAL:
                    return btos ? (FactionType)33 : FactionType.NONE;
                case Role.ORACLE:
                case Role.ROLE_COUNT:
                    return btos ? FactionType.TOWN : FactionType.NONE;
                case Role.PILGRIM:
                    return btos ? (FactionType)38 : FactionType.NONE;
                case Role.COVENITE:
                    return btos ? (FactionType)39 : FactionType.NONE;
                case Role.CATALYST:
                    return btos ? (FactionType)40 : FactionType.NONE;
                case Role.CULTIST:
                    return btos ? (FactionType)41 : FactionType.NONE;
                case (Role)62:
                    return btos ? FactionType.APOCALYPSE : FactionType.NONE;
                default:
                    if (role <= Role.NONE)
                    {
                        return FactionType.NONE;
                    }
                    if (role >= Role.ARSONIST && role < Role.SOCIALITE)
                    {
                        return FactionType.APOCALYPSE;
                    }
                    if (role >= Role.CONJURER && role < Role.ARSONIST)
                    {
                        return FactionType.COVEN;
                    }
                    return FactionType.TOWN;
            }
        }

        public static string FactionName(FactionType faction)
        {
            switch (faction)
            {
                case FactionType.TOWN: return "Town";
                case FactionType.COVEN: return "Coven";
                case FactionType.SERIALKILLER: return "SerialKiller";
                case FactionType.ARSONIST: return "Arsonist";
                case FactionType.WEREWOLF: return "Werewolf";
                case FactionType.SHROUD: return "Shroud";
                case FactionType.APOCALYPSE: return "Apocalypse";
                case FactionType.EXECUTIONER: return "Executioner";
                case FactionType.JESTER: return "Jester";
                case FactionType.PIRATE: return "Pirate";
                case FactionType.DOOMSAYER: return "Doomsayer";
                case FactionType.VAMPIRE: return "Vampire";
                case FactionType.CURSED_SOUL: return "CursedSoul";
                case (FactionType)33: return "Jackal";
                case (FactionType)34: return "Frogs";
                case (FactionType)35: return "Lions";
                case (FactionType)36: return "Hawks";
                case (FactionType)37: return "Cannibal";
                case (FactionType)38: return "Judge";
                case (FactionType)39: return "Auditor";
                case (FactionType)40: return "Inquisitor";
                case (FactionType)41: return "Starspawn";
                case (FactionType)42: return "Egotist";
                case (FactionType)43: return "Pandora";
                case (FactionType)44: return "Compliance";
                case (FactionType)250: return "Lovers";
                default: return "Regular";
            }
        }

        private void OpenDirectory()
        {
            PlatformID platform = Environment.OSVersion.Platform;
            if (platform == PlatformID.Unix || platform == PlatformID.MacOSX)
            {
                Process.Start("open", "\"" + Main.FolderPath + "\"");
            }
            else
            {
                Application.OpenURL("file://" + Main.FolderPath);
            }
        }

        private ReplayInfo GetReplayInfo(string fileName)
        {
            try
            {
                byte[] fileBytes = File.ReadAllBytes(fileName);
                int replayInfoLength = BitConverter.ToInt32(fileBytes, 0);

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    memoryStream.Write(fileBytes, 4, replayInfoLength);
                    memoryStream.Position = 0L;
                    return (ReplayInfo)binaryFormatter.Deserialize(memoryStream);
                }
            }
            catch (Exception ex)
            {
                ModLogger.Log(ex.Message ?? "");
                return null;
            }
        }

        private void SearchForBrowserMod(string name)
        {
            SalemModLoaderMainMenuController modLoaderMainMenuController = GameObject
                .Find("HomeUI(Clone)/HomeScreenMainCanvas/SafeArea/SalemModLoaderUI/Main/Menus/MainMenu/News/")
                .GetComponent<NewsTabController>()
                .ModLoaderMainMenuController;

            modLoaderMainMenuController.ClickMainButton();
            modLoaderMainMenuController.BrowseForMod(name);
            UObject.Destroy(gameObject);
        }

        private static IEnumerator JoinReplay(string replayFile)
        {
            ApplicationController.ApplicationContext.EnableCutoutTransitionOverlay(CutoutTransitionType.TUTORIAL, "");
            ApplicationController.ApplicationContext.TransitionOverlay.JoinText.SetText("Starting Match Replay");
            yield return new WaitForSeconds(1f);
            ApplicationController.ApplicationContext.EnableTransitionOverlay(true, true, "");

            if (Service.Home.NetworkService.VerifySocketConnection())
            {
                Service.Home.UserService.CreateGame(GameType.Custom, Service.Home.PlatformService.IsOnBetaBranch());
                ActiveReplayInfo.ReplayActive = true;
                ActiveReplayInfo.ReplayFilePath = replayFile;
            }
        }
    }
}
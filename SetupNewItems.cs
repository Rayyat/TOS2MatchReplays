using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Game.Interface;
using HarmonyLib;
using Server.Shared.Messages;
using Server.Shared.State;
using Server.Shared.State.Chat;
using Services;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Home.Services;
using System.Linq;

namespace MatchReplaysPlus
{
	[HarmonyPatch(typeof(HudDockPanel), "Start")]
	public static class SetupNewItems
	{
		public static void Prefix(HudDockPanel __instance)
		{
			if (!ActiveReplayInfo.ReplayActive)
				return;

			Action listener;

			List<HudDockItem> dockItems = __instance.dockItems;
			string id = "RevealRolesSlot";
			string name = "Reveal Roles";
			string localizationKey = "REPLAY_REVEAL";
			string spriteName = "Reveal";
			InputService.HotKey.HotKeyType hotKeyType = (InputService.HotKey.HotKeyType)102;

			if ((listener = O.__RevealRoles) == null)
			{
				listener = O.__RevealRoles = new Action(RevealRoles);
			}

			dockItems.Add(BuildDockItem(__instance, id, name, localizationKey, spriteName, hotKeyType, listener));


			Action listener2;

			List<HudDockItem> dockItems2 = __instance.dockItems;
			string id2 = "TimeChangeSlot";
			string name2 = "Change Time";
			string localizationKey2 = "REPLAY_TIME";
			string spriteName2 = "Time";
			InputService.HotKey.HotKeyType hotKeyType2 = (InputService.HotKey.HotKeyType)101;

			if ((listener2 = O.__ChangeGameSpeed) == null)
			{
				listener2 = O.__ChangeGameSpeed = new Action(ChangeGameSpeed);
			}

			dockItems2.Add(BuildDockItem(__instance, id2, name2, localizationKey2, spriteName2, hotKeyType2, listener2));


			Action listener3;

			List<HudDockItem> dockItems3 = __instance.dockItems;
			string id3 = "PauseSlot";
			string name3 = "Pause";
			string localizationKey3 = "REPLAY_PAUSE";
			string spriteName3 = "Pause";
			InputService.HotKey.HotKeyType hotKeyType3 = (InputService.HotKey.HotKeyType)100;

			if ((listener3 = O.__PauseReplay) == null)
			{
				listener3 = O.__PauseReplay = new Action(PauseReplay);
			}

			dockItems3.Add(BuildDockItem(__instance, id3, name3, localizationKey3, spriteName3, hotKeyType3, listener3));
		}


		private static HudDockItem BuildDockItem(HudDockPanel __instance, string id, string name, string localizationKey, string spriteName, InputService.HotKey.HotKeyType hotKeyType, Action listener)
		{
			HudDockItem hudDockItem = UnityEngine.Object.Instantiate<HudDockItem>(
				__instance.dockItems.Find(x => x.gameObject.name == "NotesSlot"),
				__instance.transform
			);

			hudDockItem.transform.localScale = new Vector3(1f, 1f, 1f);

			Button unityButton = hudDockItem.transform.GetChild(0).GetComponent<Button>();
			if (unityButton != null)
			{
				unityButton.onClick = new Button.ButtonClickedEvent();
			}
			hudDockItem.transform.SetSiblingIndex(9);

			hudDockItem.canMove = false;
			hudDockItem.dockItemType = HudDockItem.DockItemType.GAME_ANY;

			hudDockItem.label.text = name;
			hudDockItem.localizationKey = localizationKey;
			hudDockItem.name = id;

			hudDockItem.dockFunctionType = DockFunctionType.NONE;
			hudDockItem.hotkeyType = InputService.HotKey.HotKeyType.NONE;

			HudHotkeyButton button = hudDockItem.GetComponentInChildren<HudHotkeyButton>();

			if (button != null)
			{
				button.hotkeyType = InputService.HotKey.HotKeyType.NONE;
				button.eventCallback.RemoveAllListeners();
				button.eventCallback.AddListener(new UnityAction(listener.Invoke));
			}

			Image image = hudDockItem.GetComponentInChildren<Image>();
			if (image != null)
			{
				image.sprite = Main.Sprites[spriteName];
			}

			hudDockItem.eventTriggers.triggers.Clear();

			EventTrigger.Entry entry = new EventTrigger.Entry
			{
				eventID = EventTriggerType.PointerClick
			};

			entry.callback.AddListener(delegate (BaseEventData _)
			{
				listener();
			});

			hudDockItem.eventTriggers.triggers.Add(entry);
			hudDockItem.SetupTriggers();

			return hudDockItem;
		}


		public static void ChangeGameSpeed()
		{
			ReplayMatch matchReplayGO = ActiveReplayInfo.MatchReplayGO;

			float timeScale = Time.timeScale;
			float newScale;

			if (timeScale == 0.5f)
				newScale = 1f;
			else if (timeScale == 1f)
				newScale = 2f;
			else if (timeScale == 2f)
				newScale = 4f;
			else if (timeScale == 4f)
				newScale = 8f;
			else if (timeScale == 8f)
				newScale = 16f;
			else if (timeScale == 16f)
				newScale = 0.5f;
			else
				newScale = 1f;

			matchReplayGO.TimeScale = newScale;

			Service.Game.Sim.simulation.HandleChatLog(new ChatLogMessage(
				new ChatLogCustomTextEntry
				{
					customText = string.Format(
						"Set time speed to {0}%",
						matchReplayGO.TimeScale * 100f
					),
					showInChat = true,
					showInChatLog = false
				}
			));
		}


		public static void PauseReplay()
		{
			ActiveReplayInfo.MatchReplayGO.TimeScale =
				(Time.timeScale != 0f) ? 0f : 1f;

			Service.Game.Sim.simulation.HandleChatLog(new ChatLogMessage(
				new ChatLogCustomTextEntry
				{
					customText = string.Format(
						"Set time speed to {0}%",
						ActiveReplayInfo.MatchReplayGO.TimeScale * 100f
					),
					showInChat = true,
					showInChatLog = false
				}
			));
		}


		public static void RevealRoles()
		{
			foreach (KeyValuePair<int, PreviousGameData> kvp in ActiveReplayInfo.ReplayInfo.PrevGame)
			{
				int playerId = kvp.Key;

				Role role = kvp.Value.Role;
				FactionType faction = kvp.Value.Faction;

				if (role != Role.NONE)
				{
					Service.Game.Sim.simulation.knownRolesAndFactions
						.Get()[playerId] = new Tuple<Role, FactionType>(role, faction);
				}
			}

			Service.Game.Sim.simulation.knownRolesAndFactions.Broadcast();
		}


		[CompilerGenerated]
		private static class O
		{
			public static Action __RevealRoles;
			public static Action __ChangeGameSpeed;
			public static Action __PauseReplay;
		}
	}
}
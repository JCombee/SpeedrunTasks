using BepInEx;
using Reactor;
using HarmonyLib;
using BepInEx.IL2CPP;
using Il2CppSystem.Collections.Generic;

namespace SpeedrunTasks;

[BepInAutoPlugin]
[BepInProcess("Among Us.exe")]
[BepInDependency(ReactorPlugin.Id)]
public partial class SpeedrunTasksPlugin : BasePlugin
{
    public Harmony Harmony { get; } = new(Id);

    public override void Load()
    {
        Harmony.PatchAll();
    }

    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Update))]
    public static class SetMinimumPlayers
    {
        public static void Prefix(GameStartManager __instance)
        {
            __instance.MinPlayers = 0;
        }
    }

    [HarmonyPatch(typeof(RoleManager), nameof(RoleManager.SelectRoles))]
    public static class SelectRoles
    {
        public static void Prefix(RoleManager __instance)
        {
            RoleOptionsData.RoleRate roleRate = new RoleOptionsData.RoleRate();
            roleRate.Chance = 0;
            roleRate.MaxCount = 0;
            PlayerControl.GameOptions.RoleOptions.roleRates[RoleTypes.Impostor] = roleRate;
            roleRate = new RoleOptionsData.RoleRate();
            roleRate.Chance = 100;
            roleRate.MaxCount = 15;
            PlayerControl.GameOptions.RoleOptions.roleRates[RoleTypes.Crewmate] = roleRate;
        }
    }

    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.CheckEndCriteria))]
    public static class CheckEndCriteria
    {
        public static bool Prefix(ShipStatus __instance)
        {
            if (DestroyableSingleton<TutorialManager>.InstanceExists)
            {
                return true;
            }

            if (PlayerControl.GameOptions.gameType != GameType.Normal)
            {
                return true;
            }

            foreach (PlayerControl player in PlayerControl.AllPlayerControls) {
                bool complete = true;

                foreach (PlayerTask playerTask in player.myTasks)
                {
                    if (!playerTask.IsComplete) {
                        complete = false;
                        break;
                    }
                }

                if (complete)
                {
                    __instance.enabled = false;
                    ShipStatus.RpcEndGame(GameOverReason.HumansByTask, !SaveManager.BoughtNoAds);
                    break;
                }
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
    public static class OnGameEnd
    {
        public static void Postfix(AmongUsClient __instance)
        {
            Dictionary<string, PlayerControl> players = new Dictionary<string, PlayerControl>();
            foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            {
                players[player.name] = player;
            }

            foreach (WinningPlayerData player in TempData.winners)
            {
                bool complete = true;

                foreach (PlayerTask playerTask in players[player.PlayerName].myTasks)
                {
                    if (!playerTask.IsComplete)
                    {
                        complete = false;
                    }
                }

                if (!complete)
                {
                    TempData.winners.Remove(player);
                }
            }
        }
    }
}

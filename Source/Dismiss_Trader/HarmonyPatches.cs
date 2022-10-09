using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Dismiss_Trader;

[StaticConstructorOnStartup]
public class HarmonyPatches
{
    static HarmonyPatches()
    {
        var harmony = new Harmony("mehni.rimworld.traderdismissal.main");

        harmony.Patch(AccessTools.Method(typeof(FloatMenuMakerMap), "AddHumanlikeOrders"), null,
            new HarmonyMethod(typeof(HarmonyPatches),
                nameof(FloatMenuMakerMap_AddHumanlikeOrdersToDismissTraders_PostFix)));
    }

    private static void FloatMenuMakerMap_AddHumanlikeOrdersToDismissTraders_PostFix(ref Vector3 clickPos,
        ref Pawn pawn, ref List<FloatMenuOption> opts)
    {
        foreach (var target in GenUI.TargetsAt(clickPos, TargetingParameters.ForTrade(), true))
        {
            var localpawn = pawn;
            var dest = target;
            if (!pawn.CanReach(dest, PathEndMode.OnCell, Danger.Deadly))
            {
                return;
            }

            if (pawn.skills.GetSkill(SkillDefOf.Social).TotallyDisabled)
            {
                return;
            }

            var pTarg = (Pawn)dest.Thing;

            void Action()
            {
                var job = new Job(TraderDismissalJobDefs.DismissTrader, pTarg) { playerForced = true };
                localpawn.jobs.TryTakeOrderedJob(job);
            }

            var str = string.Empty;
            if (pTarg.Faction != null)
            {
                str = $" ({pTarg.Faction.Name})";
            }

            string label = "GETOUT".Translate($"{pTarg.LabelShort}, {pTarg.TraderKind.label}") + str;

            opts.Add(FloatMenuUtility.DecoratePrioritizedTask(
                new FloatMenuOption(label, Action, MenuOptionPriority.InitiateSocial, null, dest.Thing), pawn,
                pTarg));
        }
    }
}
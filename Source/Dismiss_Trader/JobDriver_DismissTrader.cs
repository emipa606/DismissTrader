﻿using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Dismiss_Trader;

internal class JobDriver_DismissTrader : JobDriver
{
    private Pawn Trader => (Pawn)TargetThingA;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(Trader, job);
    }

    //approach: find Lord transition that is the regular time-out and add another (very short) Trigger_TicksPassed. That'll then fire, and the traders will leave.

    //other (failed) approaches:
    //- inheriting from LordJob_TradeWithColony and overriding the stategraph. Set a bool in the job, which works as a trigger. Still seems like the "correct" and OOP approach, but I suck at C#
    //- adding new LordToil_ExitMapAndEscortCarriers() & telling the lord to jump to it. (lord null, somehow not registered in graph?)
    //- Outright removing the lord. Works, but also removes the traderflag, defending at exit and the group behaviour. Bad.
    //- transpile all the things! (ain't noone got time for that)
    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedOrNull(TargetIndex.A);
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch).FailOn(() => !Trader.CanTradeNow);
        var trade = new Toil
        {
            initAction = () =>
            {
                if (!Trader.CanTradeNow)
                {
                    return;
                }

                var lord = Trader.GetLord();
                var transitions = lord.Graph.transitions.ToList();
                foreach (var transition in transitions)
                {
                    foreach (var trigger in transition.triggers)
                    {
                        if (trigger is not Trigger_TicksPassed ||
                            !transition.preActions.Any(x => x is TransitionAction_CheckGiveGift))
                        {
                            continue;
                        }

                        transition.triggers.Add(new Trigger_TicksPassed(20));
                        break;
                    }
                }
            }
        };
        yield return trade;
    }
}
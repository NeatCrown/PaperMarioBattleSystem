﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PaperMarioBattleSystem.Enumerations;

namespace PaperMarioBattleSystem
{
    /// <summary>
    /// A Paragoomba's Dive Kick attack.
    /// </summary>
    public class DiveKick : MoveAction
    {
        public DiveKick()
        {
            Name = "Dive Kick";

            MoveInfo = new MoveActionData(null, "Dive Kick a foe.", MoveResourceTypes.FP, 0, CostDisplayTypes.Shown,
                MoveAffectionTypes.Enemy, TargetSelectionMenu.EntitySelectionType.Single, true, null);

            DamageInfo = new DamageData(1, Enumerations.Elements.Normal, false, Enumerations.ContactTypes.SideDirect, null, DamageEffects.None);

            SetMoveSequence(new DiveKickSequence(this));

            //It's an Enemy move, so there's no Action Command
            //However one can be added if the player were to have a Paragoomba partner
            actionCommand = null;
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using static PaperMarioBattleSystem.Enumerations;

namespace PaperMarioBattleSystem
{
    /// <summary>
    /// Mario's Hammer action
    /// </summary>
    public class Hammer : MoveAction
    {
        public Hammer()
        {
            Name = "Hammer";

            MoveInfo = new MoveActionData(null, "Whack an enemy with your Hammer.", MoveResourceTypes.FP, 0, CostDisplayTypes.Shown,
                MoveAffectionTypes.Enemy, TargetSelectionMenu.EntitySelectionType.First, true,
                new HeightStates[] { HeightStates.Grounded, HeightStates.Hovering });

            //The base damage for Hammer is Mario's current Hammer level
            //If Mario isn't the one using this move, it defaults to 1
            int baseDamage = 1;
            MarioStats marioStats = User.BattleStats as MarioStats;
            if (marioStats != null) baseDamage = (int)marioStats.HammerLevel;

            DamageInfo = new DamageData(baseDamage, Elements.Normal, false, ContactTypes.Approach, null, DamageEffects.None);

            SetMoveSequence(new HammerSequence(this));
            actionCommand = new HammerCommand(MoveSequence, 4, 500d);
        }
    }
}

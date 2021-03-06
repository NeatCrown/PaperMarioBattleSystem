﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace PaperMarioBattleSystem
{
    /// <summary>
    /// The various miscellaneous actions available to Mario and his Partner.
    /// </summary>
    public sealed class TacticsSubMenu : ActionSubMenu
    {
        public TacticsSubMenu()
        {
            Position = new Vector2(230, 150);

            BattleEntity entity = BattleManager.Instance.EntityTurn;

            //int quickChangeCount = entity.GetEquippedBadgeCount(BadgeGlobals.BadgeTypes.QuickChange);
            //Enumerations.CostDisplayTypes costDisplayType = Enumerations.CostDisplayTypes.Shown;
            //if (quickChangeCount > 0) costDisplayType = Enumerations.CostDisplayTypes.Special;

            BattleActions.Add(new MenuAction("Change Partner", null, "Change your current partner.", //costDisplayType, 
                new ChangePartnerSubMenu()));


            #region Charge Menu

            BadgeGlobals.BadgeTypes chargeBadgeType = BadgeGlobals.BadgeTypes.Charge;

            //Check for a Player; if the Player is a Partner, check the number of Charge P's instead
            if (entity.EntityType == Enumerations.EntityTypes.Player)
            {
                BattlePlayer player = (BattlePlayer)entity;
                if (player.PlayerType == Enumerations.PlayerTypes.Partner)
                {
                    chargeBadgeType = BadgeGlobals.BadgeTypes.ChargeP;
                }
            }

            //Charge action if the Charge or Charge P Badge is equipped
            int chargeCount = entity.GetEquippedBadgeCount(chargeBadgeType);
            if (chargeCount > 0)
            {
                //Charge starts out at 2 then increases by 1 for each additional Badge
                int chargeAmount = 2 + (chargeCount - 1);

                BattleActions.Add(new MoveAction("Charge", new MoveActionData(null, "Save up strength to power up your next attack",
                    Enumerations.MoveResourceTypes.FP, chargeCount, Enumerations.CostDisplayTypes.Shown, Enumerations.MoveAffectionTypes.None,
                    TargetSelectionMenu.EntitySelectionType.Single, false, null), new ChargeSequence(null, chargeAmount)));
            }

            #endregion

            //Defend action
            BattleActions.Add(new Defend());

            //Do nothing action
            BattleActions.Add(new NoAction());
        }
    }
}

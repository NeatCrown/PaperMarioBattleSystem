﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PaperMarioBattleSystem.Enumerations;

namespace PaperMarioBattleSystem
{
    /// <summary>
    /// The base class for playable characters in battle (Ex. Mario and his Partners)
    /// </summary>
    public abstract class BattlePlayer : BattleEntity
    {
        public PlayerTypes PlayerType { get; protected set; } = PlayerTypes.Mario;

        public BattlePlayer(Stats stats) : base(stats)
        {
            DefensiveActions.Add(new Guard(this));
            DefensiveActions.Add(new Superguard(this));
        }

        public override void OnTurnStart()
        {
            base.OnTurnStart();

            AnimManager.PlayAnimation(AnimationGlobals.PlayerBattleAnimations.ChoosingActionName);

            int itemTurns = EntityProperties.GetAdditionalProperty<int>(Enumerations.AdditionalProperty.DipItemTurns);
            if (itemTurns > 0)
            {
                BattleUIManager.Instance.PushMenu(new ItemSubMenu(1, 0, true));
            }
            else
            {
                BattleUIManager.Instance.PushMenu(GetMainBattleMenu());
            }
        }

        public override void OnTurnEnd()
        {
            base.OnTurnEnd();

            AnimManager.PlayAnimation(GetIdleAnim());
        }

        public override string GetIdleAnim()
        {
            switch (HealthState)
            {
                case HealthStates.Danger:
                case HealthStates.Peril:
                    return AnimationGlobals.PlayerBattleAnimations.DangerName;
            }

            //This is hacky for now - fix this once we have priorities for idle animations
            if (PreviousAction?.Name == "Defend")
                return AnimationGlobals.PlayerBattleAnimations.GuardName;

            return base.GetIdleAnim();
        }

        /// <summary>
        /// Gets the BattleMenu the BattlePlayer uses at the start of its turn.
        /// </summary>
        /// <returns></returns>
        protected abstract BattleMenu GetMainBattleMenu();

        /// <summary>
        /// Getse the BattlePlayer's Star Power.
        /// Mario returns his own, while Partners use Mario's.
        /// </summary>
        /// <returns>A StarPowerBase with the StarPower the BattlePlayer uses.</returns>
        public abstract StarPowerBase GetStarPower(StarPowerGlobals.StarPowerTypes starPowerType);
    }
}

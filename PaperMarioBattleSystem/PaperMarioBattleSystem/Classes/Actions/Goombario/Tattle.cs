﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PaperMarioBattleSystem.Enumerations;

namespace PaperMarioBattleSystem
{
    /// <summary>
    /// Goombario's Tattle action.
    /// </summary>
    public sealed class Tattle : MoveAction
    {
        public Tattle(bool isGoombella)
        {
            Name = "Tattle";

            MoveInfo = new MoveActionData(null, "View enemies' descriptions\nand see their HP in battle.", MoveResourceTypes.FP, 0,
                CostDisplayTypes.Hidden, Enumerations.MoveAffectionTypes.Enemy, TargetSelectionMenu.EntitySelectionType.Single, false,
                null);

            SetMoveSequence(new TattleSequence(this));
            
            //Default to no Action Command
            //If this is Goombella, then set the Action Command
            actionCommand = null;
            if (isGoombella == true)
            {
                actionCommand = new TattleCommand(MoveSequence, 2f);
            }
        }

        public override void Initialize()
        {
            //Check if there are any BattleEntities that can be tattled
            //If there are none, disable the action
            BattleEntity[] tattleEntities = GetTattleableEntities();
            if (tattleEntities.Length == 0)
            {
                Disabled = true;
                DisabledString = "There's no one that can be tattled!";
            }
        }

        public override void OnMenuSelected()
        {
            //Select only entities that can be tattled
            BattleUIManager.Instance.StartTargetSelection(ActionStart, MoveProperties.SelectionType, GetTattleableEntities());
        }

        /// <summary>
        /// Gets all BattleEntities that can be tattled.
        /// They all implement the <see cref="ITattleableEntity"/> interface.
        /// </summary>
        /// <returns>An array of BattleEntities of the opposing EntityType that can be tattled.</returns>
        private BattleEntity[] GetTattleableEntities()
        {
            List<BattleEntity> tattleableEntities = new List<BattleEntity>();

            //Get all BattleEntities of the opposing EntityType
            BattleEntity[] entities = BattleManager.Instance.GetEntities(User.EntityType == EntityTypes.Player ? EntityTypes.Enemy : EntityTypes.Player);

            for (int i = 0; i < entities.Length; i++)
            {
                //Safely cast to see if the BattleEntity can be tattled and add it if so
                ITattleableEntity tattleableEntity = entities[i] as ITattleableEntity;
                if (tattleableEntity != null)
                {
                    tattleableEntities.Add(entities[i]);
                }
            }

            return tattleableEntities.ToArray();
        }
    }
}

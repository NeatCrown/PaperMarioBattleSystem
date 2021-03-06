﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static PaperMarioBattleSystem.Enumerations;

namespace PaperMarioBattleSystem
{
    /// <summary>
    /// A Sequence used in a MoveAction.
    /// </summary>
    public abstract class Sequence : IActionCommandHandler, IUpdateable, IDrawable
    {
        /// <summary>
        /// Values for each branch of a sequence.
        /// Sequences switch branches based on what happens.
        /// <para>The None branch is only used to indicate whether to jump to a certain branch after the sequence updates.
        /// The most common use-case is switching to the Interruption branch</para>
        /// </summary>
        public enum SequenceBranch
        {
            None, Start, End, Main, Success, Failed, Interruption, Miss
        }

        /// <summary>
        /// A delegate for handling the sequence interruption branch.
        /// It should follow the same conventions as the other branches.
        /// </summary>
        protected delegate void InterruptionDelegate();

        #region Fields/Properties

        /// <summary>
        /// The reference to the MoveAction this Sequence is a part of.
        /// </summary>
        public MoveAction Action { get; private set; } = null;

        public string Name => Action.Name;

        protected BattleEntity User => Action.User;

        protected ActionCommand actionCommand => Action.actionCommand;

        protected bool CommandEnabled => Action.CommandEnabled;

        protected int BaseDamage => Action.DealsDamage ? Action.DamageProperties.Damage : 0;

        /// <summary>
        /// The BattleEntities the move associated with the Sequence can affect.
        /// </summary>
        protected BattleEntity[] EntitiesAffected { get; private set; }

        /// <summary>
        /// Whether the Sequence is currently being performed or not.
        /// </summary>
        public bool InSequence { get; protected set; } = false;

        /// <summary>
        /// The current step of the sequence.
        /// </summary>
        public int SequenceStep { get; protected set; } = 0;

        /// <summary>
        /// The current branch of the sequence.
        /// </summary>
        protected SequenceBranch CurBranch { get; private set; } = SequenceBranch.Start;

        /// <summary>
        /// The current SequenceAction being performed
        /// </summary>
        protected SequenceAction CurSequenceAction { get; set; } = null;

        /// <summary>
        /// A value denoting if we should jump to a particular branch or not after the sequence progresses.
        /// This allows the sequences to remain flexible and not cause any sequence or branch conflicts with this branch.
        /// </summary>
        protected SequenceBranch JumpToBranch { get; private set; } = SequenceBranch.None;

        /// <summary>
        /// The handler used for interruptions. This exists so each action can specify different handlers for
        /// different types of damage. It defaults to the base method at the start and end of each action.
        /// </summary>
        protected InterruptionDelegate InterruptionHandler = null;

        /// <summary>
        /// The result of performing the Action Command.
        /// </summary>
        public ActionCommand.CommandResults CommandResult { get; private set; } = ActionCommand.CommandResults.Success;

        /// <summary>
        /// The highest CommandRank the player obtained when performing the Action Command.
        /// </summary>
        public ActionCommand.CommandRank HighestCommandRank { get; private set; } = ActionCommand.CommandRank.None;

        #endregion

        protected Sequence(MoveAction moveAction)
        {
            SetAction(moveAction);
            InterruptionHandler = BaseInterruptionHandler;
        }

        #region Methods

        /// <summary>
        /// Sets the MoveAction this Sequence is associated with.
        /// This is automatically set in the MoveAction's constructor if a MoveAction is being instantiated directly.
        /// </summary>
        /// <param name="moveAction">The MoveAction this Sequence is being performed by.</param>
        public void SetAction(MoveAction moveAction)
        {
            Action = moveAction;
        }

        /// <summary>
        /// Starts the action sequence.
        /// </summary>
        /// <param name="targets">The targets to perform the sequence on.</param>
        public void StartSequence(params BattleEntity[] targets)
        {
            CurBranch = SequenceBranch.Start;
            InSequence = true;
            SequenceStep = 0;
            HighestCommandRank = ActionCommand.CommandRank.None;

            ChangeJumpBranch(SequenceBranch.None);

            EntitiesAffected = targets;

            InterruptionHandler = BaseInterruptionHandler;

            OnStart();

            //Start the first sequence
            ProgressSequence(0);
        }

        /// <summary>
        /// Sequence-specific logic when the action is started.
        /// </summary>
        protected virtual void OnStart()
        {
            if (EntitiesAffected != null)
            {
                for (int i = 0; i < EntitiesAffected.Length; i++)
                {
                    EntitiesAffected[i].TargetForMove(User);
                }
            }
        }

        /// <summary>
        /// Ends the action sequence
        /// </summary>
        public void EndSequence()
        {
            CurBranch = SequenceBranch.End;
            InSequence = false;
            SequenceStep = 0;
            CurSequenceAction = null;
            HighestCommandRank = ActionCommand.CommandRank.None;

            ChangeJumpBranch(SequenceBranch.None);

            InterruptionHandler = BaseInterruptionHandler;

            OnEnd();

            EntitiesAffected = null;

            if (User == BattleManager.Instance.EntityTurn)
            {
                User.EndTurn();
            }
            else
            {
                Debug.LogError($"{User.Name} is not {BattleManager.Instance.EntityTurn.Name}, whose turn it currently is");
            }
        }

        /// <summary>
        /// Sequence-specific logic when the action is complete.
        /// </summary>
        protected virtual void OnEnd()
        {
            if (EntitiesAffected != null)
            {
                for (int i = 0; i < EntitiesAffected.Length; i++)
                {
                    EntitiesAffected[i].StopTarget();
                }
            }
        }

        /// <summary>
        /// Prints an error message when an invalid sequence has occurred.
        /// It includes information such as the action and the entity performing it, the sequence branch, and the sequence step
        /// </summary>
        protected void PrintInvalidSequence()
        {
            Debug.LogError($"{User.Name} entered an invalid state in {Name} with a {nameof(SequenceStep)} of {SequenceStep} in {nameof(CurBranch)}: {CurBranch}");
        }

        /// <summary>
        /// Progresses the BattleAction further into its sequence
        /// </summary>
        /// <param name="progressAmount">The amount to progress the sequence</param>
        private void ProgressSequence(uint progressAmount)
        {
            SequenceStep += (int)progressAmount;

            //Debug.LogWarning($"SequenceStep for {Name} is {SequenceStep}");

            OnProgressSequence();
            if (InSequence == true)
            {
                CurSequenceAction.Start();
            }
        }

        /// <summary>
        /// Switches to a new sequence branch. This also resets the current step
        /// </summary>
        /// <param name="newBranch">The new branch to switch to</param>
        protected void ChangeSequenceBranch(SequenceBranch newBranch)
        {
            //Debug.LogWarning($"Changing from {CurBranch} to {newBranch}");

            CurBranch = newBranch;

            //Set to -1 as it'll be incremented next time the sequence progresses
            SequenceStep = -1;
        }

        /// <summary>
        /// Sets the branch to jump to after the current sequence updates
        /// </summary>
        /// <param name="newJumpBranch">The new branch to jump to</param>
        protected void ChangeJumpBranch(SequenceBranch newJumpBranch)
        {
            JumpToBranch = newJumpBranch;
        }

        /// <summary>
        /// What occurs next in the sequence when it's progressed.
        /// </summary>
        private void OnProgressSequence()
        {
            switch (CurBranch)
            {
                case SequenceBranch.Start:
                    SequenceStartBranch();
                    break;
                case SequenceBranch.Main:
                    SequenceMainBranch();
                    break;
                case SequenceBranch.Success:
                    SequenceSuccessBranch();
                    break;
                case SequenceBranch.Failed:
                    SequenceFailedBranch();
                    break;
                case SequenceBranch.Interruption:
                    SequenceInterruptionBranch();
                    break;
                case SequenceBranch.Miss:
                    SequenceMissBranch();
                    break;
                case SequenceBranch.End:
                default:
                    SequenceEndBranch();
                    break;
            }
        }

        #endregion

        #region Sequence Branch Methods

        /// <summary>
        /// The start of the action sequence
        /// </summary>
        protected abstract void SequenceStartBranch();

        /// <summary>
        /// The end of the action sequence
        /// </summary>
        protected abstract void SequenceEndBranch();

        /// <summary>
        /// The main part of the action sequence. If the action has an Action Command, this branch will likely incorporate it
        /// </summary>
        protected abstract void SequenceMainBranch();

        /// <summary>
        /// What occurs when the action command for this action is performed successfully
        /// </summary>
        protected abstract void SequenceSuccessBranch();

        /// <summary>
        /// What occurs when the action command for this action is failed
        /// </summary>
        protected abstract void SequenceFailedBranch();

        /// <summary>
        /// What occurs when the action is interrupted.
        /// The most notable example of this is when Mario takes damage from jumping on a spiked enemy
        /// <para>This is overrideable through the InterruptionHandler, as actions can handle this in more than one way</para>
        /// </summary>
        protected void SequenceInterruptionBranch()
        {
            if (InterruptionHandler == null)
            {
                Debug.LogError($"{nameof(InterruptionHandler)} is null for {Name}! This should NEVER happen - look into it ASAP");
                return;
            }

            InterruptionHandler();
        }

        /// <summary>
        /// What occurs when the action misses
        /// </summary>
        protected abstract void SequenceMissBranch();

        /// <summary>
        /// The base interruption handler
        /// </summary>
        protected void BaseInterruptionHandler()
        {
            float moveX = -20f;
            float moveY = 70f;

            double time = 500d;

            switch (SequenceStep)
            {
                case 0:
                    User.AnimManager.PlayAnimation(AnimationGlobals.HurtName, true);

                    Vector2 pos = User.Position + new Vector2(moveX, -moveY);
                    CurSequenceAction = new MoveToSeqAction(pos, time / 2d);
                    break;
                case 1:
                    CurSequenceAction = new WaitForAnimationSeqAction(AnimationGlobals.HurtName);
                    break;
                case 2:
                    CurSequenceAction = new MoveAmountSeqAction(new Vector2(0f, moveY), time);
                    ChangeSequenceBranch(SequenceBranch.End);
                    break;
                default:
                    PrintInvalidSequence();
                    break;
            }
        }

        /// <summary>
        /// Starts the interruption, which occurs when a BattleEntity takes damage mid-sequence
        /// </summary>
        /// <param name="element">The elemental damage being dealt</param>
        public virtual void StartInterruption(Elements element)
        {
            ChangeJumpBranch(SequenceBranch.Interruption);

            //Call the action-specific interruption method to set the interruption handler
            OnInterruption(element);
        }

        /// <summary>
        /// How the action handles a miss.
        /// The base implementation is to do nothing, but actions such as Jump may go to the Miss branch
        /// </summary>
        protected virtual void OnMiss()
        {
            Debug.Log($"{User.Name} has missed with the {Name} Action and will act accordingly");
        }

        /// <summary>
        /// Sets the InterruptionHandler based on the type of damage dealt.
        /// </summary>
        /// <param name="element">The elemental damage being dealt</param>
        protected virtual void OnInterruption(Elements element)
        {
            InterruptionHandler = BaseInterruptionHandler;
        }

        #endregion

        #region Action Command Methods

        //We have OnCommandSuccess() non-virtual so we can perform general functionality for successfully completing any Action Command
        //Examples include showing the degrees of success ("Nice," "Great," etc.) and increasing the damage dealt by All or Nothing
        public void OnCommandSuccess()
        {
            CommandResult = ActionCommand.CommandResults.Success;

            CommandSuccess();
        }

        //We have OnCommandFailed() non-virtual so we can perform general functionality for failing to complete any Action Command
        //Examples include dealing no damage with the All or Nothing Badge
        public void OnCommandFailed()
        {
            CommandResult = ActionCommand.CommandResults.Failure;

            CommandFailed();
        }

        //NOTE: For some moves, these only show up when you hit (Ex. Jump, Hammer, Power Shell).
        //Other moves show them as you perform the command, even if they deal damage (Ex. Mini-Egg, Earth Tremor).
        //Find a way to define when to show them in each move
        public void OnCommandRankResult(ActionCommand.CommandRank commandRank)
        {
            //Don't bother if the CommandRank is nothing
            if (commandRank == ActionCommand.CommandRank.None) return;

            //Get how many Simplifiers and Unsimplifiers the entity has equipped
            int simplifierCount = UtilityGlobals.Clamp(User.GetEquippedBadgeCount(BadgeGlobals.BadgeTypes.Simplifier), 0, BadgeGlobals.MaxSimplifierCount);
            int unsimplifierCount = UtilityGlobals.Clamp(User.GetEquippedBadgeCount(BadgeGlobals.BadgeTypes.Unsimplifier), 0, BadgeGlobals.MaxUnsimplifierCount);

            int rankInt = (int)commandRank;

            //Subtract Simplifier count and add Unsimplifier count to the final rank
            rankInt -= simplifierCount;
            rankInt += unsimplifierCount;

            //Clamp the rank to the final values
            rankInt = UtilityGlobals.Clamp(rankInt, (int)ActionCommand.CommandRank.NiceM2, (int)ActionCommand.CommandRank.Excellent);

            ActionCommand.CommandRank finalRank = (ActionCommand.CommandRank)rankInt;

            //Update the highest command rank
            if (finalRank > HighestCommandRank)
            {
                HighestCommandRank = finalRank;
            }
        }

        public virtual void OnCommandResponse(object response)
        {

        }

        protected virtual void CommandSuccess()
        {
            ChangeSequenceBranch(SequenceBranch.Success);
        }

        protected virtual void CommandFailed()
        {
            ChangeSequenceBranch(SequenceBranch.Failed);
        }

        /// <summary>
        /// Starts the Action Command's input.
        /// If the Action Command is not enabled, it will call OnCommandFailed().
        /// </summary>
        /// <param name="values">Any values passed to the ActionCommand just as it starts.</param>
        protected void StartActionCommandInput(params object[] values)
        {
            if (CommandEnabled == true) actionCommand.StartInput(values);
            else OnCommandFailed();
        }

        #endregion

        #region Update Methods

        /// <summary>
        /// What occurs right before the Sequence updates.
        /// </summary>
        protected virtual void PreSequenceUpdate()
        {
            //If the action command is enabled, let it handle the sequence
            if (CommandEnabled == true)
            {
                if (actionCommand.AcceptingInput == true)
                    actionCommand.Update();
            }
        }

        public void Update()
        {
            //Perform sequence
            if (InSequence == true)
            {
                PreSequenceUpdate();

                CurSequenceAction.Update();
                if (CurSequenceAction.IsDone == true)
                {
                    ProgressSequence(1);
                }
            }

            PostUpdate();
        }

        /// <summary>
        /// Handles anything that needs to be done directly after updating the sequence.
        /// This is where it jumps to a new branch, if it should.
        /// </summary>
        protected void PostUpdate()
        {
            if (InSequence == true && JumpToBranch != SequenceBranch.None)
            {
                //Change the sequence action itself to cancel out anything that it will be waiting for to finish
                //We don't end the previous sequence action because it has been interrupted by the new branch
                CurSequenceAction = new WaitSeqAction(0d);
                ChangeSequenceBranch(JumpToBranch);

                ChangeJumpBranch(SequenceBranch.None);
            }
        }

        public virtual void Draw()
        {

        }

        #endregion

        #region Damage Methods

        /// <summary>
        /// Attempt to deal damage to a set of entities with this BattleAction.
        /// <para>Based on the ContactType of this BattleAction, this can fail, resulting in an interruption.
        /// In the event of an interruption, no further entities are tested, the ActionCommand is ended, and 
        /// we go into the Interruption branch</para>
        /// </summary>
        /// <param name="damage">The damage the BattleAction deals to the entity if the attempt was successful</param>
        /// <param name="entities">The BattleEntities to attempt to inflict damage on</param>
        /// <param name="damageInfo">The damage information to use.</param>
        /// <param name="isTotalDamage">Whether the damage passed in is the total damage or not.
        /// If false, the total damage will be calculated</param>
        /// <returns>An int array containing the damage dealt to each BattleEntity targeted, in order</returns>
        protected int[] AttemptDamage(int damage, BattleEntity[] entities, DamageData damageInfo, bool isTotalDamage)
        {
            if (entities == null || entities.Length == 0)
            {
                Debug.LogWarning($"{nameof(entities)} is null or empty in {nameof(AttemptDamage)} for Action {Name}!");
                return new int[0];
            }

            //Ensure the MoveAction associated with this sequence supports damage
            if (Action.DealsDamage == false)
            {
                Debug.LogError($"Attempting to deal damage when {Action.Name} does not support it, as {nameof(Action.DamageProperties)} is null");
                return new int[0];
            }

            int totalDamage = isTotalDamage == true ? damage : GetTotalDamage(damage);

            //Check for the All or Nothing Badge if the move is affected by it
            //We check for it here since the CommandResult is fully determined by this point
            if (damageInfo.AllOrNothingAffected == true)
            {
                //If it's equipped, add the number to the damage if the Action Command succeeded, otherwise set the damage to the minimum value
                int allOrNothingCount = User.GetEquippedBadgeCount(BadgeGlobals.BadgeTypes.AllOrNothing);
                if (allOrNothingCount > 0)
                {
                    if (CommandResult == ActionCommand.CommandResults.Success)
                    {
                        totalDamage += allOrNothingCount;
                    }
                    else if (CommandResult == ActionCommand.CommandResults.Failure)
                    {
                        totalDamage = int.MinValue;
                    }
                }
            }

            //The damage dealt to each BattleEntity
            int[] damageValues = new int[entities.Length];

            //Go through all the entities and attempt damage
            for (int i = 0; i < entities.Length; i++)
            {
                BattleEntity victim = entities[i];

                InteractionResult finalResult = Interactions.GetDamageInteraction(new InteractionParamHolder(User, victim, totalDamage,
                    damageInfo.DamagingElement, damageInfo.Piercing, damageInfo.ContactType, damageInfo.Statuses, damageInfo.DamageEffect,
                    damageInfo.CantMiss, damageInfo.DefensiveOverride));

                //Set the total damage dealt to the victim
                damageValues[i] = finalResult.VictimResult.TotalDamage;

                //Make the victim take damage upon a PartialSuccess or a Success
                if (finalResult.VictimResult.HasValue == true)
                {
                    //Check if the attacker hit
                    if (finalResult.VictimResult.Hit == true)
                    {
                        finalResult.VictimResult.Entity.TakeDamage(finalResult.VictimResult);
                    }
                    //Handle a miss otherwise
                    else
                    {
                        OnMiss();
                    }
                }

                //Make the attacker take damage upon a PartialSuccess or a Failure
                //Break out of the loop when the attacker takes damage
                if (finalResult.AttackerResult.HasValue == true)
                {
                    finalResult.AttackerResult.Entity.TakeDamage(finalResult.AttackerResult);

                    break;
                }
            }

            return damageValues;
        }

        /// <summary>
        /// Convenience function for attempting damage with only one entity.
        /// </summary>
        /// <param name="damage">The damage the BattleAction deals to the entity if the attempt was successful</param>
        /// <param name="entity">The BattleEntity to attempt to inflict damage on</param>
        /// <param name="damageInfo">The damage information to use.</param>
        /// <param name="isTotalDamage">Whether the damage passed in is the total damage or not.
        /// If false, the total damage will be calculated</param>
        protected int[] AttemptDamage(int damage, BattleEntity entity, DamageData damageInfo, bool isTotalDamage)
        {
            return AttemptDamage(damage, new BattleEntity[] { entity }, damageInfo, isTotalDamage);
        }

        /// <summary>
        /// Gets the total raw damage a BattleEntity can deal using this BattleAction.
        /// This factors in a BattleEntity's Attack stat and anything else that may influence the damage dealt.
        /// </summary>
        /// <param name="actionDamage">The damage the BattleAction deals</param>
        /// <returns>An int with the total raw damage the BattleEntity can deal when using this BattleAction</returns>
        protected int GetTotalDamage(int actionDamage)
        {
            int totalDamage = actionDamage + User.BattleStats.TotalAttack + GetChargeDamage();

            return totalDamage;
        }

        /// <summary>
        /// Gets the extra damage the user has charged up if the action uses a charge.
        /// </summary>
        /// <returns>The charged damage if the action can use a charge, otherwise 0.</returns>
        private int GetChargeDamage()
        {
            return Action.MoveProperties.UsesCharge == true ?
                User.EntityProperties.GetAdditionalProperty<int>(AdditionalProperty.ChargedDamage) : 0;
        }

        #endregion

        #region Healing Methods

        /// <summary>
        /// Heals a set of BattleEntities with this MoveAction.
        /// <para>Healing moves cannot miss.</para>
        /// </summary>
        /// <param name="healingData">The HealingData containing HP, FP, and more.</param>
        /// <param name="entities">The BattleEntities to heal.</param>
        public void PerformHeal(HealingData healingData, BattleEntity[] entities)
        {
            for (int i = 0; i < entities.Length; i++)
            {
                BattleEntity entityHealed = entities[i];

                //Heal HP and FP
                entityHealed.HealHP(healingData.HPHealed);
                entityHealed.HealFP(healingData.FPHealed);

                //Heal Status Effects
                StatusTypes[] statusesHealed = healingData.StatusEffectsHealed;
                if (statusesHealed != null)
                {
                    for (int j = 0; j < statusesHealed.Length; j++)
                    {
                        StatusTypes statusHealed = statusesHealed[j];

                        entityHealed.EntityProperties.RemoveStatus(statusHealed, true);
                    }
                }
            }
        }

        /// <summary>
        /// Convenience function for healing with only one entity.
        /// </summary>
        /// <param name="healingData">The HealingData containing HP, FP, and more.</param>
        /// <param name="entity">The BattleEntity to heal.</param>
        public void PerformHeal(HealingData healingData, BattleEntity entity)
        {
            PerformHeal(healingData, new BattleEntity[] { entity });
        }

        #endregion
    }
}

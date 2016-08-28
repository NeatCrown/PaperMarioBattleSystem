﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using static PaperMarioBattleSystem.Enumerations;
using static PaperMarioBattleSystem.StatusGlobals;

namespace PaperMarioBattleSystem
{
    /// <summary>
    /// Defines all types of interactions that exist among BattleEntities.
    /// <para>This includes Element-PhysicalAttribute interactions, ContactType-PhysicalAttribute interactions, and more.</para>
    /// </summary>
    public static class Interactions
    {
        #region Structs
        
        /// <summary>
        /// Holds the result and damage value of an damage interaction involving an Element
        /// </summary>
        private struct ElementDamageResultHolder
        {
            public ElementInteractionResult InteractionResult;
            public int Damage;

            public ElementDamageResultHolder(ElementInteractionResult interactionResult, int damage)
            {
                InteractionResult = interactionResult;
                Damage = damage;
            }
        }

        #endregion

        #region Fields    

        /// <summary>
        /// The table that determines the result of a particular ContactType and a particular PhysicalAttribute.
        /// <para>The default value is a Success, meaning the ContactType will deal damage. A Failure indicates a backfire (Ex. Mario
        /// jumping on a spiked enemy)</para>
        /// </summary>
        private static Dictionary<ContactTypes, Dictionary<PhysicalAttributes, ContactResultInfo>> ContactTable = null;

        #endregion

        static Interactions()
        {
            InitalizeContactTable();
        }

        #region Contact Table Initialization Methods

        private static void InitalizeContactTable()
        {
            ContactTable = new Dictionary<ContactTypes, Dictionary<PhysicalAttributes, ContactResultInfo>>();

            InitializeNoneContactTable();
            InitializeJumpContactTable();
            InitializeHammerContactTable();
        }

        private static void InitializeNoneContactTable()
        {

        }

        //NOTE: In the actual games, if you have the Payback status, it takes priority over any PhysicalAttributes when being dealt damage
        //For example, if you have both Return Postage and Zap Tap equipped, sucking enemies like Fuzzies will be able to touch you
        //However, normal properties apply when attacking enemies (you'll be able to jump on Electrified enemies)

        private static void InitializeJumpContactTable()
        {
            ContactTable.Add(ContactTypes.JumpContact, new Dictionary<PhysicalAttributes, ContactResultInfo>()
            {
                { PhysicalAttributes.Spiked, new ContactResultInfo(new PaybackHolder(PaybackTypes.Constant, Elements.Sharp, 1), ContactResult.Failure, false) },
                { PhysicalAttributes.Electrified, new ContactResultInfo(new PaybackHolder(PaybackTypes.Constant, Elements.Electric, 1), ContactResult.PartialSuccess, true) },
                { PhysicalAttributes.Fiery, new ContactResultInfo(new PaybackHolder(PaybackTypes.Constant, Elements.Fire, 1), ContactResult.Failure, false) }
            });
        }

        private static void InitializeHammerContactTable()
        {

        }

        #endregion

        #region Contact Table Methods

        /// <summary>
        /// Gets the result of a ContactType on a set of PhysicalAttributes
        /// </summary>
        /// <param name="attacker">The BattleEntity performing the attack</param>
        /// <param name="contactType">The ContactType performed</param>
        /// <param name="physAttributes">The set of PhysicalAttributes to test against</param>
        /// <param name="attributesToIgnore">A set of PhysicalAttributes to ignore</param>
        /// <returns>A ContactResultInfo of the interaction</returns>
        public static ContactResultInfo GetContactResult(BattleEntity attacker, ContactTypes contactType, PhysicalAttributes[] physAttributes, params PhysicalAttributes[] attributesToIgnore)
        {
            //Return the default value
            if (ContactTable.ContainsKey(contactType) == false || physAttributes == null)
            {
                Debug.LogWarning($"{nameof(physAttributes)} array is null or {nameof(ContactTable)} does not contain the ContactType {contactType}!");
                return ContactResultInfo.Default;
            }

            //Look through the attributes and find the first match
            for (int i = 0; i < physAttributes.Length; i++)
            {
                Dictionary<PhysicalAttributes, ContactResultInfo> tableForContact = ContactTable[contactType];
                PhysicalAttributes attribute = physAttributes[i];

                //If this attribute is ignored, move onto the next
                if (attributesToIgnore?.Contains(attribute) == true)
                    continue;

                if (tableForContact.ContainsKey(attribute) == true)
                {
                    ContactResultInfo contactResult = tableForContact[attribute];
                    //If the ContactResult is a Success if the entity has the same PhysicalAttribute as the one tested, set its result to Success
                    if (contactResult.SuccessIfSameAttr == true && attacker.EntityProperties.HasPhysAttributes(true, attribute) == true)
                        contactResult.ContactResult = ContactResult.Success;
                    return contactResult;
                }
            }

            return ContactResultInfo.Default;
        }

        #endregion

        #region Interaction Methods

        /* Damage Calculation Order:
         * 1. Base damage
         * 2. If Guarded, subtract 1 from the damage and add the # of Damage Dodge Badges to the victim's Defense. If Superguarded, damage = 0
         * 3. Subtract or add to the damage based on the # of P-Up, D-Down and P-Down, D-Up Badges are equipped
         * 4. Check Element Overrides to change the attacker's Element damage based on the PhysicalAttributes of the victim (Ex. Ice Power Badge)
         * 5. Calculate the victim's Weaknesses/Resistances to the element
         * 6. If the damage dealt is not Piercing, subtract the victim's Defense from the damage
         * 6. 
         */

        /// <summary>
        /// Calculates and returns the entire damage interaction between two BattleEntities.
        /// <para>This returns all the necessary information for both BattleEntities, including the total amount of damage dealt,
        /// the type of Elemental damage to deal, the Status Effects to inflict, and whether the attack successfully hit or not.</para>
        /// </summary>
        /// <param name="interactionParam">An InteractionParamHolder containing the BattleEntities interacting and data about their interaction</param>
        /// <returns>An InteractionResult containing InteractionHolders for both the victim and the attacker</returns>
        public static InteractionResult GetDamageInteraction(InteractionParamHolder interactionParam)
        {
            InteractionResult finalInteractionResult = new InteractionResult();

            BattleEntity attacker = interactionParam.Attacker;
            BattleEntity victim = interactionParam.Victim;
            ContactTypes contactType = interactionParam.ContactType;
            Elements element = interactionParam.DamagingElement;
            StatusEffect[] statuses = interactionParam.Statuses;
            int damage = interactionParam.Damage;
            bool piercing = interactionParam.Piercing;

            //Get contact results
            ContactResultInfo contactResultInfo = victim.EntityProperties.GetContactResult(attacker, contactType);
            ContactResult contactResult = contactResultInfo.ContactResult;

            //Defensive actions take priority
            BattleGlobals.DefensiveActionHolder? victimDefenseData = victim.GetDefensiveActionResult(damage, statuses);
            if (victimDefenseData.HasValue == true)
            {
                damage = victimDefenseData.Value.Damage;
                statuses = victimDefenseData.Value.Statuses;
                //If the Defensive action dealt damage and the contact was direct
                //the Defensive action has causes a Failure for the Attacker (Ex. Superguarding)
                if (contactType == ContactTypes.JumpContact && victimDefenseData.Value.ElementHolder.HasValue == true)
                {
                    contactResult = ContactResult.Failure;
                }
            }

            //Defense added from Damage Dodge Badges upon a successful Guard
            int damageDodgeDefense = 0;

            //Subtract damage reduction (P-Up, D-Down and P-Down, D-Up Badges)
            damage -= victim.BattleStats.DamageReduction;

            //Retrieve an overridden type of Elemental damage to inflict based on the Victim's PhysicalAttributes
            //(Ex. The Ice Power Badge only deals Ice damage to Fiery entities)
            Elements newElement = attacker.EntityProperties.GetTotalElementOverride(victim);
            if (newElement != Elements.Invalid)
            {
                element = newElement;
            }

            /*Get the total damage dealt to the Victim. The amount of Full or Half Payback damage dealt to the Attacker
              uses the resulting damage value from this because Payback uses the total damage that would be dealt to the Victim.
              This occurs before factoring in elemental resistances/weaknesses from the Attacker*/
            ElementDamageResultHolder victimElementDamage = GetElementalDamage(victim, element, damage);

            //Subtract Defense on non-piercing damage
            if (piercing == false)
            {
                int totalDefense = victim.BattleStats.Defense + damageDodgeDefense;
                victimElementDamage.Damage = UtilityGlobals.Clamp(victimElementDamage.Damage - totalDefense, BattleGlobals.MinDamage, BattleGlobals.MaxDamage);
            }

            //Calculating damage dealt to the Victim
            if (contactResult == ContactResult.Success || contactResult == ContactResult.PartialSuccess)
            {
                //Get the Status Effects to inflict on the Victim
                StatusEffect[] victimInflictedStatuses = GetFilteredInflictedStatuses(victim, statuses);
                bool hit = attacker.AttemptHitEntity(victim);

                finalInteractionResult.VictimResult = new InteractionHolder(victim, victimElementDamage.Damage, element, 
                    victimElementDamage.InteractionResult, contactType, piercing, victimInflictedStatuses, hit);
            }

            //Calculating damage dealt to the Attacker
            if (contactResult == ContactResult.Failure || contactResult == ContactResult.PartialSuccess)
            {
                //The damage the Attacker dealt to the Victim
                int damageDealt = victimElementDamage.Damage;
                PaybackHolder paybackHolder = contactResultInfo.Paybackholder;
                
                //Override the PaybackHolder with a Defensive Action's results, if any
                if (victimDefenseData.HasValue == true && victimDefenseData.Value.ElementHolder.HasValue == true)
                {
                    damageDealt = victimDefenseData.Value.ElementHolder.Value.Damage;
                    paybackHolder = new PaybackHolder(PaybackTypes.Constant, victimDefenseData.Value.ElementHolder.Value.Element, damageDealt);
                }

                //Get the damage done to the Attacker, factoring in Weaknesses/Resistances
                ElementDamageResultHolder attackerElementDamage = GetElementalDamage(attacker, paybackHolder.Element, damageDealt);

                //Get Payback damage - Payback damage is calculated after everything else, including Constant Payback.
                //However, it does NOT factor in Double Pain or any sort of Defense modifiers.
                int paybackDamage = paybackHolder.GetPaybackDamage(attackerElementDamage.Damage);

                //If Constant Payback, update the damage value to use the element
                if (paybackHolder.PaybackType == PaybackTypes.Constant)
                {
                    paybackDamage = GetElementalDamage(attacker, paybackHolder.Element, paybackDamage).Damage;
                }

                //Clamp damage
                attackerElementDamage.Damage = UtilityGlobals.Clamp(paybackDamage, BattleGlobals.MinDamage, BattleGlobals.MaxDamage);

                //Get the Status Effects to inflict
                StatusEffect[] attackerInflictedStatuses = GetFilteredInflictedStatuses(attacker, paybackHolder.StatusesInflicted);
                    
                finalInteractionResult.AttackerResult = new InteractionHolder(attacker, attackerElementDamage.Damage, paybackHolder.Element,
                    attackerElementDamage.InteractionResult, ContactTypes.None, true, attackerInflictedStatuses, true);
            }

            return finalInteractionResult;
        }

        /// <summary>
        /// Calculates the result of elemental damage on a BattleEntity, based on its weaknesses and resistances to that Element
        /// </summary>
        /// <param name="entity">The BattleEntity being damaged</param>
        /// <param name="element">The element the entity is attacked with</param>
        /// <param name="damage">The initial damage of the attack</param>
        /// <returns>An ElementDamageHolder stating the result and final damage dealt to this entity</returns>
        private static ElementDamageResultHolder GetElementalDamage(BattleEntity entity, Elements element, int damage)
        {
            ElementDamageResultHolder elementDamageResult = new ElementDamageResultHolder(ElementInteractionResult.Damage, damage);

            //NOTE: If an entity is both resistant and weak to a particular element, they cancel out.
            //I decided to go with this approach because it's the simplest for this situation, which
            //doesn't seem desirable to begin with but could be interesting in its application
            WeaknessHolder weakness = entity.EntityProperties.GetWeakness(element);
            ResistanceHolder resistance = entity.EntityProperties.GetResistance(element);
            
            //If there's both a weakness and resistance, return
            if (weakness.WeaknessType != WeaknessTypes.None && resistance.ResistanceType != ResistanceTypes.None)
                return elementDamageResult;

            //Handle weaknesses
            if (weakness.WeaknessType == WeaknessTypes.PlusDamage)
            {
                elementDamageResult.Damage = UtilityGlobals.Clamp(elementDamageResult.Damage + weakness.Value, BattleGlobals.MinDamage, BattleGlobals.MaxDamage);
            }
            else if (weakness.WeaknessType == WeaknessTypes.KO)
            {
                elementDamageResult.InteractionResult = ElementInteractionResult.KO;
            }

            //Handle resistances
            if (resistance.ResistanceType == ResistanceTypes.MinusDamage)
            {
                elementDamageResult.Damage = UtilityGlobals.Clamp(elementDamageResult.Damage - resistance.Value, BattleGlobals.MinDamage, BattleGlobals.MaxDamage);
            }
            else if (resistance.ResistanceType == ResistanceTypes.NoDamage)
            {
                elementDamageResult.Damage = BattleGlobals.MinDamage;
            }
            else if (resistance.ResistanceType == ResistanceTypes.Heal)
            {
                elementDamageResult.InteractionResult = ElementInteractionResult.Heal;
            }

            return elementDamageResult;
        }

        /// <summary>
        /// Filters an array of StatusEffects depending on whether they will be inflicted on a BattleEntity
        /// depending on the entity's status percentages
        /// </summary>
        /// <param name="entity">The BattleEntity to attempt to afflict the StatusEffects with</param>
        /// <param name="statusesToInflict">The original array of StatusEffects</param>
        /// <returns>An array of StatusEffects that has succeeded in being inflicted on the entity</returns>
        private static StatusEffect[] GetFilteredInflictedStatuses(BattleEntity entity, StatusEffect[] statusesToInflict)
        {
            //Handle null
            if (statusesToInflict == null) return statusesToInflict;

            //Construct a list with the original elements
            List<StatusEffect> filteredStatuses = new List<StatusEffect>(statusesToInflict);

            //Look through the list and remove any StatusEffects that fail to be afflicted onto the entity
            for (int i = 0; i < filteredStatuses.Count; i++)
            {
                StatusEffect status = filteredStatuses[i];
                if (entity.EntityProperties.TryAfflictStatus(status) == false)
                {
                    Debug.Log($"Failed to inflict {status.StatusType} on {entity.Name}");
                    filteredStatuses.RemoveAt(i);
                    i--;
                }
            }

            return filteredStatuses.ToArray();
        }

        #endregion
    }
}

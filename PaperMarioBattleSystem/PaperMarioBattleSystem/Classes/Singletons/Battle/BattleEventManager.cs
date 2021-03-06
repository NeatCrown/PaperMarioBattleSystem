﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using static PaperMarioBattleSystem.Enumerations;
using static PaperMarioBattleSystem.BattleGlobals;

namespace PaperMarioBattleSystem
{
    /// <summary>
    /// Manages BattleEvents.
    /// <para>This is a Singleton.</para>
    /// </summary>
    public sealed class BattleEventManager
    {
        #region Singleton Fields

        public static BattleEventManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new BattleEventManager();
                }

                return instance;
            }
        }

        private static BattleEventManager instance = null;

        #endregion

        /// <summary>
        /// The current Battle Events taking place. These are completed before the next BattleEntity's turn takes place.
        /// </summary>
        public readonly Dictionary<int, List<BattleEvent>> BattleEvents = new Dictionary<int, List<BattleEvent>>();

        /// <summary>
        /// The pending Battle Events waiting to be added.
        /// They are added once the Battle State matches the state they should be added in.
        /// </summary>
        public readonly List<PendingBattleEventHolder> PendingBattleEvents = new List<PendingBattleEventHolder>();

        /// <summary>
        /// The current, highest priority of Battle Events taking place.
        /// Once these are all done, the next highest priority of Battle Events is found and takes place.
        /// </summary>
        public int CurHighestPriority { get; private set; } = 0;

        /// <summary>
        /// Tells if there are Battle Events.
        /// </summary>
        public bool HasBattleEvents => (BattleEvents.Count > 0);

        private BattleEventManager()
        {

        }

        /// <summary>
        /// Places a Battle Event on the pending list.
        /// <para>If the current BattleState matches a BattleState the Battle Event takes effect in, it will take effect immediately.
        /// Otherwise, it will wait and take effect once the current BattleState matches.</para>
        /// </summary>
        /// <param name="priority">The priority the Battle Event has. Must be greater than or equal to 0.</param>
        /// <param name="battleStates">The BattleStates the Battle Event takes effect in. If none are specified, the event isn't added.</param>
        /// <param name="battleEvent">The Battle Event to add.</param>
        public void QueueBattleEvent(int priority, BattleManager.BattleState[] battleStates, BattleEvent battleEvent)
        {
            if (priority < 0)
            {
                Debug.LogError($"Not queueing BattleEvent because the priority's value is {priority} which is less than 0!");
                return;
            }

            if (battleEvent == null)
            {
                Debug.LogError($"Trying to queue null BattleEvent with priority of {priority}! Not queueing BattleEvent");
                return;
            }

            if (battleStates == null || battleStates.Length == 0)
            {
                Debug.LogError($"BattleEvent {battleEvent} with Priority {priority} was queued, but no BattleStates were specified. Not queueing.");
                return;
            }

            //Add the Battle Event directly if the current state is the state to add it in
            if (battleStates.Contains(BattleManager.Instance.State) == true)
            {
                AddBattleEvent(priority, battleEvent);
            }
            //Otherwise put it in the pending list
            else
            {
                PendingBattleEvents.Add(new PendingBattleEventHolder(priority, battleStates, battleEvent));

                Debug.Log($"Queued BattleEvent {battleEvent} with Priority {priority}");
            }
        }

        /// <summary>
        /// Adds a Battle Event to occur.
        /// </summary>
        /// <param name="priority">The priority the Battle Event has. Must be greater than or equal to 0.</param>
        /// <param name="battleEvent">The Battle Event to add.</param>
        private void AddBattleEvent(int priority, BattleEvent battleEvent)
        {
            if (priority < 0)
            {
                Debug.LogError($"Not adding BattleEvent because the priority's value is {priority} which is less than 0!");
                return;
            }

            if (battleEvent == null)
            {
                Debug.LogError($"Trying to add null BattleEvent with priority of {priority}! Not adding BattleEvent");
                return;
            }

            //If the battle event is unique, check if one with the same contents exist
            if (battleEvent.IsUnique == true)
            {
                if (BattleEvents.ContainsKey(priority) == true)
                {
                    bool sameContents = BattleEvents[priority].Exists((evt) => (evt.AreContentsEqual(battleEvent) == true));
                    if (sameContents == true)
                    {
                        Debug.Log($"Not adding BattleEvent {battleEvent} with Priority {priority} as it has the same contents as another and is a unique event.");
                        return;
                    }
                }
            }

            //Set the current highest priority to the priority if there are no Battle Events
            if (HasBattleEvents == false)
            {
                CurHighestPriority = priority;
            }

            if (BattleEvents.ContainsKey(priority) == false)
            {
                BattleEvents.Add(priority, new List<BattleEvent>());
            }

            BattleEvents[priority].Add(battleEvent);

            Debug.Log($"Added BattleEvent {battleEvent} with Priority {priority} to take effect");
        }

        /// <summary>
        /// Updates the current set of Battle Events.
        /// </summary>
        public void UpdateBattleEvents()
        {
            //There are no more BattleEvents to update
            if (HasBattleEvents == false)
            {
                return;
            }

            //Update all Battle Events
            for (int i = 0; i < BattleEvents[CurHighestPriority].Count; i++)
            {
                BattleEvent battleEvent = BattleEvents[CurHighestPriority][i];

                //Start the Battle Event if it hasn't started already
                if (battleEvent.HasStarted == false)
                {
                    battleEvent.Start();
                }

                battleEvent.Update();
                if (battleEvent.IsDone == true)
                {
                    //Remove the BattleEvent if it's finished
                    BattleEvents[CurHighestPriority].RemoveAt(i);
                    i--;
                }
            }

            //If we're done with all Battle Events with this priority, remove the priority and find the next highest priority to update
            if (BattleEvents[CurHighestPriority].Count == 0)
            {
                BattleEvents.Remove(CurHighestPriority);
                CurHighestPriority = FindNextHighestBattleEventPriority();
            }
        }

        /// <summary>
        /// Finds the next highest priority for the set of Battle Events to update.
        /// </summary>
        /// <returns>-1 if there are no Battle Events to update, otherwise the highest priority value for the Battle Events to update.</returns>
        private int FindNextHighestBattleEventPriority()
        {
            if (HasBattleEvents == false) return -1;

            int highestPriority = -1;

            foreach (int priority in BattleEvents.Keys)
            {
                if (priority > highestPriority) highestPriority = priority;
            }

            return highestPriority;
        }

        /// <summary>
        /// Adds pending Battle Events if the BattleState is the state that matches the state they're added in.
        /// </summary>
        public void AddPendingEvents()
        {
            for (int i = 0; i < PendingBattleEvents.Count; i++)
            {
                //Check if the states match
                if (PendingBattleEvents[i].States.Contains(BattleManager.Instance.State))
                {
                    //Add the event and remove it from the pending list
                    AddBattleEvent(PendingBattleEvents[i].Priority, PendingBattleEvents[i].PendingBattleEvent);
                    PendingBattleEvents.RemoveAt(i);
                    i--;
                }
            }
        }
    }
}

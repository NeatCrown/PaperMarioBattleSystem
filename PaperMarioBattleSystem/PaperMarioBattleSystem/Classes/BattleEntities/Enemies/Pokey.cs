﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PaperMarioBattleSystem
{
    /// <summary>
    /// A Pokey enemy. It has 3 segments.
    /// </summary>
    public class Pokey : BattleEnemy, ISegmentEntity, ITattleableEntity
    {
        public Pokey(Stats stats) : base(new Stats(11, 4, 0, 2, 0))
        {
            Name = "Pokey";

            #region Entity Property Setup

            EntityProperties.AddStatusProperty(Enumerations.StatusTypes.Sleep, new StatusPropertyHolder(95, 0));
            EntityProperties.AddStatusProperty(Enumerations.StatusTypes.Dizzy, new StatusPropertyHolder(80, 0));
            EntityProperties.AddStatusProperty(Enumerations.StatusTypes.Confused, new StatusPropertyHolder(90, 0));
            EntityProperties.AddStatusProperty(Enumerations.StatusTypes.Tiny, new StatusPropertyHolder(90, 0));
            EntityProperties.AddStatusProperty(Enumerations.StatusTypes.Immobilized, new StatusPropertyHolder(80, 0));
            EntityProperties.AddStatusProperty(Enumerations.StatusTypes.Soft, new StatusPropertyHolder(95, 0));
            EntityProperties.AddStatusProperty(Enumerations.StatusTypes.Burn, new StatusPropertyHolder(100, 0));
            EntityProperties.AddStatusProperty(Enumerations.StatusTypes.Frozen, new StatusPropertyHolder(60, 0));
            EntityProperties.AddStatusProperty(Enumerations.StatusTypes.Fright, new StatusPropertyHolder(100, 0));
            EntityProperties.AddStatusProperty(Enumerations.StatusTypes.Blown, new StatusPropertyHolder(90, 0));
            EntityProperties.AddStatusProperty(Enumerations.StatusTypes.KO, new StatusPropertyHolder(100, 0));

            EntityProperties.AddPhysAttribute(Enumerations.PhysicalAttributes.TopSpiked);
            EntityProperties.AddPhysAttribute(Enumerations.PhysicalAttributes.SideSpiked);

            EntityProperties.SetVulnerableDamageEffects(Enumerations.DamageEffects.RemovesSegment);

            #endregion

            Texture2D spriteSheet = AssetManager.Instance.LoadAsset<Texture2D>($"{ContentGlobals.SpriteRoot}/Enemies/Pokey");
            AnimManager.SetSpriteSheet(spriteSheet);

            //Set segment count
            CurSegmentCount = MaxSegments;
        }

        #region Interface Implementation

        public uint MaxSegments { get; protected set; } = 3;

        public uint CurSegmentCount { get; protected set; } = 0;

        public void HandleSegmentRemoved(uint segmentsRemoved)
        {

        }

        public void HandleSegmentAdded(uint segmentsAdded)
        {
            
        }

        #endregion

        #region Tattle Info

        public string[] GetTattleLogEntry()
        {
            return new string[]
            {
                $"HP: {BattleStats.MaxHP} Attack: {BattleStats.BaseAttack}\nDefense: {BattleStats.BaseDefense}",
                $"A cactus ghoul covered from head to base in nasty spines.",
                "It attacks by lobbing sections of itself at you, and can even call other Pokeys to fight alongside it."
            };
        }

        public string[] GetTattleDescription()
        {
            return new string[]
            {
                "That's a Pokey. It's a cactus ghoul that's got nasty spines all over its body.",
                $"Max HP is {BattleStats.MaxHP}, Attack is {BattleStats.BaseAttack}, and Defense is {BattleStats.BaseDefense}.",
                "Look at those spines... Those would TOTALLY hurt. If you stomp on it, you'll regret it.",
                "Pokeys attack by lobbing parts of their bodies and by charging at you...",
                "They can even call friends in for help, so be quick about taking them out."
            };
        }

        #endregion
    }
}

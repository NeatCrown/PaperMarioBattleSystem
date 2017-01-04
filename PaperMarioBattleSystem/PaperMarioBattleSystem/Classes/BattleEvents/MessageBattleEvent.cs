﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace PaperMarioBattleSystem
{
    /// <summary>
    /// A Battle Message.
    /// It shows up to mention important battle-related information, such as not being able to flee or the consequences of a Status Effect.
    /// It disappears after a certain amount of time.
    /// </summary>
    public sealed class MessageBattleEvent : WaitBattleEvent
    {
        private TextBox BattleTextBox = null;
        private string BattleMessage = string.Empty;

        public MessageBattleEvent(string battleMessage, double waitDuration) : base(waitDuration)
        {
            BattleMessage = battleMessage;
            BattleTextBox = new TextBox(SpriteRenderer.Instance.WindowCenter, new Vector2(100, 50), BattleMessage);
            BattleTextBox.ScaleToText(AssetManager.Instance.TTYDFont);
        }

        protected override void OnStart()
        {
            base.OnStart();

            BattleUIManager.Instance.AddUIElement(BattleTextBox);
        }

        protected override void OnEnd()
        {
            base.OnEnd();

            BattleUIManager.Instance.RemoveUIElement(BattleTextBox);
        }
    }
}

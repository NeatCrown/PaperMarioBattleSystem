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
    /// Move the cursor across the 3x3 grid and select the red and blue arrows to boost your stats.
    /// Hitting a Poison Mushroom halves the cursor's speed for a short time.
    /// </summary>
    public sealed class PowerLiftCommand : ActionCommand
    {
        private int NumColumns = 3;
        private int NumRows = 3;
        private Vector2 LiftGridCellSize = new Vector2(64, 64);

        private double CommandTime = 30000d;
        private int CursorSpeed = 3;
        private Vector2 CurrentCursorPos = Vector2.Zero;
        private Vector2 DestinationCursorPos = Vector2.Zero;

        private float PoisonFactor = .5f;
        private double PoisonDur = 2000d;

        private CroppedTexture2D BigCursor = null;
        private CroppedTexture2D SmallCursor = null;

        private UIGrid PowerLiftGrid = null;

        /// <summary>
        /// Tells whether the player can select an arrow with the cursor.
        /// The cursor cannot select while it is moving to another spot on the grid.
        /// </summary>
        private bool CanSelect => (CurrentCursorPos == DestinationCursorPos);

        public PowerLiftCommand(IActionCommandHandler commandHandler, double commandTime) : base(commandHandler)
        {
            Texture2D battleGFX = AssetManager.Instance.LoadAsset<Texture2D>($"{ContentGlobals.UIRoot}/Battle/BattleGFX");

            BigCursor = new CroppedTexture2D(battleGFX, new Rectangle(14, 273, 46, 46));
            SmallCursor = new CroppedTexture2D(battleGFX, new Rectangle(10, 330, 13, 12));

            CommandTime = commandTime;
        }

        public override void StartInput(params object[] values)
        {
            base.StartInput(values);

            //Set up the grid
            SetUpGrid();
        }

        public override void EndInput()
        {
            base.EndInput();

            PowerLiftGrid.ClearGrid();
            PowerLiftGrid = null;
        }

        private void SetUpGrid()
        {
            //Create the grid
            PowerLiftGrid = new UIGrid(NumColumns, NumRows, LiftGridCellSize);

            //Populate the grid based on how many columns and rows it has
            int totalSize = NumColumns * NumRows;
            for (int i = 0; i < totalSize; i++)
            {
                //Small cursors are on the grid
                PowerLiftGrid.AddGridElement(new UICroppedTexture2D(SmallCursor.Copy(), .5f, Color.White));
            }

            //Although the grid is drawn on the UI layer, we center it on the sprite layer so it's in the center of the screen
            PowerLiftGrid.Position = Camera.Instance.SpriteToUIPos(Vector2.Zero);
        }

        protected override void ReadInput()
        {
            
        }

        protected override void OnDraw()
        {
            PowerLiftGrid.Draw();
        }
    }
}

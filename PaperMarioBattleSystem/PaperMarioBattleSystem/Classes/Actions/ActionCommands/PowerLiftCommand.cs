﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PaperMarioBattleSystem
{
    /// <summary>
    /// Move the cursor across the 3x3 grid and select the red and blue arrows to boost your stats.
    /// Hitting a Poison Mushroom halves the cursor's speed for a short time.
    /// </summary>
    public sealed class PowerLiftCommand : ActionCommand
    {
        #region Enums

        public enum PowerLiftIcons
        {
            None = 0,
            Poison = 1,
            Attack = 2,
            Defense = 3
        }

        #endregion

        private int NumColumns = 3;
        private int NumRows = 3;
        private Vector2 LiftGridCellSize = new Vector2(26, 24);
        private Vector2 LiftGridSpacing = new Vector2(52, 48);

        private double CommandTime = 30000d;
        private double ElapsedCommandTime = 0d;

        private double CursorSpeedDur = 150d;
        private Vector2 PrevCursorPos = Vector2.Zero;
        private Vector2 CurrentCursorPos { get => Cursor.Position; set => Cursor.Position = value; }
        private Vector2 DestinationCursorPos = Vector2.Zero;
        private Color CursorColor = Color.White;
        private Color MovingColor = Color.Blue;
        private Color SelectedColor = Color.Red;
        private double ElapsedMoveTime = 0d;

        private double PoisonSpeedDur = 500d;
        private double PoisonDur = 2000d;
        private double ElapsedPoisonTime = 0d;
        private bool IsPoisoned = false;

        private CroppedTexture2D BigCursor = null;
        private CroppedTexture2D SmallCursor = null;

        private UIFourPiecedTex Cursor = null;
        private int CurColumn = 0;
        private int CurRow = 0;

        private bool SelectedIcon = false;

        /// <summary>
        /// The grid used for laying out the objects.
        /// </summary>
        private UIGrid PowerLiftGrid = null;

        /// <summary>
        /// The internal grid used for tracking the icons.
        /// </summary>
        private PowerLiftIconElement[][] IconGrid = null;

        /// <summary>
        /// Tells whether the player can select an arrow with the cursor.
        /// The cursor cannot select while it is moving to another spot on the grid.
        /// </summary>
        private bool CanSelect => (CurrentCursorPos == DestinationCursorPos);

        public PowerLiftCommand(IActionCommandHandler commandHandler, double commandTime) : base(commandHandler)
        {
            Texture2D battleGFX = AssetManager.Instance.LoadAsset<Texture2D>($"{ContentGlobals.UIRoot}/Battle/BattleGFX");

            BigCursor = new CroppedTexture2D(battleGFX, new Rectangle(14, 273, 46, 46));
            SmallCursor = new CroppedTexture2D(battleGFX, new Rectangle(10, 330, 13, 12));//new CroppedTexture2D(AssetManager.Instance.LoadAsset<Texture2D>($"{ContentGlobals.UIRoot}/Debug/BoxOutline2"), null);

            Cursor = new UIFourPiecedTex(BigCursor, LiftGridCellSize / 2, .6f, CursorColor);

            CommandTime = commandTime;
        }

        public override void StartInput(params object[] values)
        {
            base.StartInput(values);

            ElapsedCommandTime = ElapsedPoisonTime = 0d;
            SelectedIcon = false;
            IsPoisoned = false;

            //Set up the grid
            SetUpGrid();

            BattleUIManager.Instance.AddUIElement(PowerLiftGrid);
            BattleUIManager.Instance.AddUIElement(Cursor);

            //Center the cursor in the middle
            CurColumn = PowerLiftGrid.Columns / 2;
            CurRow = PowerLiftGrid.Rows / 2;

            int centerIndex = PowerLiftGrid.GetIndex(CurColumn, CurRow);
            PrevCursorPos = CurrentCursorPos = DestinationCursorPos = PowerLiftGrid.GetPositionAtIndex(centerIndex);
        }

        public override void EndInput()
        {
            base.EndInput();

            ElapsedCommandTime = ElapsedPoisonTime = 0d;
            SelectedIcon = true;
            IsPoisoned = false;

            BattleUIManager.Instance.RemoveUIElement(PowerLiftGrid);
            BattleUIManager.Instance.RemoveUIElement(Cursor);

            PowerLiftGrid.ClearGrid();
            PowerLiftGrid = null;
        }

        private void SetUpGrid()
        {
            //Create the grid
            PowerLiftGrid = new UIGrid(NumColumns, NumRows, LiftGridCellSize);

            //Populate the grid based on how many columns and rows it has
            int totalSize = PowerLiftGrid.MaxElementsInGrid;
            for (int i = 0; i < totalSize; i++)
            {
                //Small cursors are on the grid
                //Offset their position since they're being drawn in 4 pieces centered about an origin
                PowerLiftGrid.AddGridElement(new UIFourPiecedTex(SmallCursor.Copy(), LiftGridCellSize / 2, .5f, Color.White));
            }

            //Although the grid is drawn on the UI layer, we center it using the sprite layer's (0, 0) position for ease
            PowerLiftGrid.Position = Camera.Instance.SpriteToUIPos(Vector2.Zero);
            PowerLiftGrid.ChangeGridPivot(UIGrid.GridPivots.Center);

            PowerLiftGrid.Spacing = LiftGridSpacing;

            //Initialize the icon grid
            UtilityGlobals.InitializeJaggedArray(ref IconGrid, PowerLiftGrid.Columns, PowerLiftGrid.Rows);
        }

        protected override void ReadInput()
        {
            //if (ElapsedCommandTime >= CommandTime)
            //{
            //    OnComplete(CommandResults.Success);
            //    return;
            //}

            ElapsedCommandTime += Time.ElapsedMilliseconds;

            HandlePoisoned();
            HandleCursorInput();

            UpdateIconGrid();
        }

        private void HandleCursorInput()
        {
            if (CanSelect == true)
            {
                //Wait a frame after having just selected an icon (this matches the behavior in the game)
                if (SelectedIcon == true)
                {
                    Cursor.TintColor = CursorColor;
                    SelectedIcon = false;
                    return;
                }

                //Check for selecting the icon
                if (Input.GetKeyDown(Keys.Z) == true)
                {
                    HandleIconSelection(IconGrid[CurColumn][CurRow]);
                }

                //Don't allow cursor movement if an icon was just selected
                if (SelectedIcon == false)
                {
                    HandleCursorMovement();
                }
            }
            //Handle moving the cursor
            else
            {
                //Progress the amount of time spent moving
                ElapsedMoveTime += Time.ElapsedMilliseconds;

                //Choose the speed; if the player hit a Poison Mushroom, use the slower speed until it expires
                double speed = CursorSpeedDur;
                if (IsPoisoned == true)
                {
                    speed = PoisonSpeedDur;
                }

                //Lerp to the destination
                CurrentCursorPos = Vector2.Lerp(PrevCursorPos, DestinationCursorPos, (float)(ElapsedMoveTime / speed));

                //We're done moving to our destination
                if (ElapsedMoveTime >= speed)
                {
                    Cursor.TintColor = CursorColor;
                    CurrentCursorPos = DestinationCursorPos;
                }
            }
        }

        private void HandleCursorMovement()
        {
            int newCol = CurColumn;
            int newRow = CurRow;

            if (Input.GetKeyDown(Keys.Up) == true)
            {
                newRow -= 1;
            }
            else if (Input.GetKeyDown(Keys.Down) == true)
            {
                newRow += 1;
            }
            else if (Input.GetKeyDown(Keys.Left) == true)
            {
                newCol -= 1;
            }
            else if (Input.GetKeyDown(Keys.Right) == true)
            {
                newCol += 1;
            }

            //Check if we moved at all and make sure we're in bounds
            if (newCol != CurColumn && newCol >= 0 && newCol < PowerLiftGrid.Columns)
            {
                CurColumn = newCol;

                PrevCursorPos = CurrentCursorPos;
                DestinationCursorPos = PowerLiftGrid.GetPositionAtIndex(PowerLiftGrid.GetIndex(CurColumn, CurRow));
                Cursor.TintColor = MovingColor;

                ElapsedMoveTime = 0d;
            }
            else if (newRow != CurRow && newRow >= 0 && newRow < PowerLiftGrid.Rows)
            {
                CurRow = newRow;

                PrevCursorPos = CurrentCursorPos;
                DestinationCursorPos = PowerLiftGrid.GetPositionAtIndex(PowerLiftGrid.GetIndex(CurColumn, CurRow));
                Cursor.TintColor = MovingColor;

                ElapsedMoveTime = 0d;
            }
        }

        private void HandleIconSelection(PowerLiftIconElement iconSelected)
        {
            if (iconSelected != null)
            {
                switch (iconSelected.PowerliftIcon)
                {
                    case PowerLiftIcons.Poison:
                        IsPoisoned = true;
                        ElapsedPoisonTime = 0d;
                        break;
                    case PowerLiftIcons.Attack:
                        break;
                    case PowerLiftIcons.Defense:

                        break;
                    default:

                        break;
                }
            }

            //Pressing A to select causes the cursor to turn red for 1 frame even if you don't hit an icon
            Cursor.TintColor = SelectedColor;
            SelectedIcon = true;
        }

        private void HandlePoisoned()
        {
            //Check if the player is poisoned, which occurs after hitting a Poison Mushroom
            if (IsPoisoned == true)
            {
                //Check if the poisoned timer is done and remove the poison if the player isn't currently moving the cursor
                //The latter check prevents issues with the cursor snapping since the timing changed during the movement
                if (ElapsedPoisonTime >= PoisonDur && CanSelect == true)
                {
                    IsPoisoned = false;
                    ElapsedPoisonTime = 0d;
                }
                //Otherwise progress the timer
                else
                {
                    ElapsedPoisonTime += Time.ElapsedMilliseconds;
                }
            }
        }

        /// <summary>
        /// Updates the icon grid.
        /// </summary>
        private void UpdateIconGrid()
        {
            for (int i = 0; i < IconGrid.Length; i++)
            {
                for (int j = 0; j < IconGrid[i].Length; j++)
                {
                    //Get the icon element
                    PowerLiftIconElement iconElement = IconGrid[i][j];
                    
                    //If there's no icon element here, continue
                    if (iconElement == null)
                    {
                        continue;
                    }

                    //Update the icon element
                    iconElement.Update();

                    //If the icon is completely done, clear it
                    if (iconElement.IsDone == true)
                    {
                        IconGrid[i][j] = null;
                    }
                }
            }
        }

        /// <summary>
        /// Chooses an icon to create at a particular grid spot.
        /// </summary>
        private void CreateNextIcon(int gridCol, int gridRow)
        {
            //PowerLiftIconElement element = new PowerLiftIconElement(, )
        }

        protected override void OnDraw()
        {
            for (int i = 0; i < IconGrid.Length; i++)
            {
                for (int j = 0; j < IconGrid[i].Length; j++)
                {
                    PowerLiftIconElement iconElement = IconGrid[i][j];

                    //Draw the icon elements
                    if (iconElement != null)
                    {
                        iconElement.Draw();
                    }
                }
            }
        }
    }
}
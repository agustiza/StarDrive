using System;
using System.IO;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Audio;
using Ship_Game.Data.Mesh;
using Ship_Game.Gameplay;
using Ship_Game.GameScreens.MainMenu;
using Ship_Game.Ships;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Rendering;

namespace Ship_Game
{
    public sealed class ShipToolScreen : GameScreen
    {
        private Matrix worldMatrix = Matrix.Identity;
        private Matrix view;
        private Matrix projection = Matrix.CreatePerspectiveFieldOfView(0.7853982f, 1f, 1f, 10000f);
        private SceneObject shipSO;

        private InputState designInputState;

        private SubTexture DottedLine;

        private Restrictions DesignState;

        private Rectangle SelectionBox = new Rectangle(-1, -1, 0, 0);

        private SubTexture moduleSlot;

        private int slotsize = 16;

        private DanButton LoadModelButton;

        private DanButton SaveHullButton;

        private string DescriptionOfState = "";

        private PrimitiveQuad border;

        private Vector2 aspect;

        private Array<Ship> StartingShipList = new Array<Ship>();

        private Vector3 cameraPosition = new Vector3(0f, 0f, 1300f);

        private UITextEntry ShipNameBox;

        private Array<ToggleButton> DesignStateButtons = new Array<ToggleButton>();

        private string ModelPath;

        private Thruster thruster;

        private bool applyThruster;

        public Array<ShipModule> ModuleList = new Array<ShipModule>();

        private Array<SlotStruct> SlotList = new Array<SlotStruct>();

        private Rectangle what;

        private Vector2 tPos = Vector2.Zero;

        private float tscale = 30f;

        private float heat = 1f;

        private Array<ThrusterZone> TList = new Array<ThrusterZone>();

        private string HullName = "Hull Name";

        private int selectedShip = 0;

        private ShipModule ActiveModule;

        public ShipToolScreen(GameScreen parent) : base(parent)
        {
            TransitionOnTime = 0f;
            TransitionOffTime = 0f;
            designInputState = new InputState();
        }

        private void ConfigureSlots()
        {
            border = new PrimitiveQuad(aspect.X / 2f - 256f, aspect.Y / 2f - 256f, 512f, 512f);
            for (int x = -32; x < 32; x++)
            {
                for (int y = -32; y < 32; y++)
                {
                    var newCell = new PrimitiveQuad(((int)Center.X + 16 * x), ((int)Center.Y + 16 * y), slotsize, slotsize);
                    var newslot = new SlotStruct
                    {
                        PQ = newCell,
                        Restrictions = Restrictions.I
                    };
                    SlotList.Add(newslot);
                }
            }
        }

        public override void Draw(SpriteBatch batch)
        {
            GameTime gameTime = StarDriveGame.Instance.GameTime;
            ScreenManager.BeginFrameRendering(gameTime, ref view, ref projection);

            ScreenManager.GraphicsDevice.Clear(Color.Black);
            if (applyThruster)
            {
                thruster.Draw(ref view, ref projection);
            }
            ScreenManager.RenderSceneObjects();
            Rectangle rectangle = new Rectangle(border.X, border.Y, 512, 512);
            batch.Begin();
            Vector2 TitlePos = new Vector2(20f, 20f);
            HelperFunctions.DrawDropShadowText(batch, "Ship Mod Tools", TitlePos, Fonts.Arial20Bold);
            TitlePos.Y = TitlePos.Y + (Fonts.Arial20Bold.LineSpacing + 3);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "Use this tool to create module patterns for your ships", TitlePos, Color.White);
            if (shipSO != null)
            {
                TitlePos = new Vector2(what.X, 20f);
                SpriteFont arial12Bold = Fonts.Arial12Bold;
                float radius = shipSO.WorldBoundingSphere.Radius;
                batch.DrawString(arial12Bold, string.Concat("Radius: ", radius.ToString()), TitlePos, Color.White);
                TitlePos.Y = TitlePos.Y + 20f;
                string text = "If you can't see your model then your radius is likely too big or too small. A radius of 512 will fit snugly inside the box. Change the scale when you compile the model. If it is rotated oddly change the X, Y, and Z axis. If the model is off-center then you will need to re-export the 3D model from Blender, making sure to Set Origin to the desired pivot point of your model";
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Fonts.Arial12.ParseText(text, 600f), TitlePos, Color.White);
            }
            var whichSelectionPos = new Vector2(what.X, what.Y - Fonts.Arial20Bold.LineSpacing);
            batch.DrawString(Fonts.Arial20Bold, string.Concat(DesignState, " - ", GetDesignStateText()), whichSelectionPos, Color.Orange);
            whichSelectionPos.X = whichSelectionPos.X + 150f;
            whichSelectionPos.Y = whichSelectionPos.Y + Fonts.Arial20Bold.LineSpacing;
            whichSelectionPos.Y = whichSelectionPos.Y - Fonts.Arial12Bold.MeasureString(Fonts.Arial12Bold.ParseText(DescriptionOfState, 512f)).Y;
            ScreenManager.SpriteBatch.DrawRectangle(what, Color.White);
            foreach (SlotStruct slot in SlotList)
            {
                if (!applyThruster && slot.PQ.Filled)
                {
                    slot.Draw(batch, moduleSlot, Color.White);
                    batch.DrawString(Fonts.Arial20Bold, " "+slot.Restrictions, 
                        slot.PosVec2, Color.Navy, 0f, Vector2.Zero, 0.4f, SpriteEffects.None, 1f);
                }
                if (applyThruster || slot.ModuleUID == null)
                {
                    continue;
                }
                if (slot.Module.XSIZE > 1 || slot.Module.YSIZE > 1)
                {
                    batch.Draw(slot.Tex, slot.ModuleRect, Color.White);
                }
                else
                {
                    slot.Draw(batch, slot.Tex, Color.White);
                }
            }
            DrawHorizontalLine(batch, SelectionBox.Y);
            DrawHorizontalLine(batch, SelectionBox.Y + SelectionBox.Height);
            DrawVerticalLine(batch, SelectionBox.X);
            DrawVerticalLine(batch, SelectionBox.X + SelectionBox.Width);
            foreach (ToggleButton button in DesignStateButtons)
            {
                button.Draw(ScreenManager);
            }
            if (ActiveModule != null)
            {
                batch.Draw(ResourceManager.Texture(ResourceManager.GetModuleTemplate(ActiveModule.UID).IconTexturePath), new Rectangle(Input.MouseCurr.X, Input.MouseCurr.Y, 16 * ActiveModule.XSIZE, 16 * ActiveModule.YSIZE), Color.White);
                
                
                for (int i = 0; i < ActiveModule.XSIZE; i++)
                for (int j = 0; j < ActiveModule.YSIZE; j++)
                {
                    var pq = new PrimitiveQuad(new Rectangle(Input.MouseCurr.X + i * 16, Input.MouseCurr.Y + j * 16, 16, 16));
                    pq.Draw(batch, Color.White);
                }

            }
            Vector2 InfoPos = new Vector2(SaveHullButton.r.X - 50, SaveHullButton.r.Y - 20);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "Hulls are saved to StarDrive/Ship Tools", InfoPos, Color.White);
            ShipNameBox.Draw(Fonts.Arial20Bold, ScreenManager.SpriteBatch, new Vector2(ShipNameBox.ClickableArea.X, ShipNameBox.ClickableArea.Y), gameTime, Color.Orange);
            SaveHullButton.Draw(ScreenManager);
            LoadModelButton.Draw(ScreenManager);
            batch.End();
            ScreenManager.EndFrameRendering();
        }

        private void DrawHorizontalLine(SpriteBatch spriteBatch, int thePositionY)
        {
            if (SelectionBox.Width > 0)
            {
                for (int aCounter = 0; aCounter <= SelectionBox.Width - 10; aCounter = aCounter + 10)
                {
                    if (SelectionBox.Width - aCounter >= 0)
                    {
                        spriteBatch.Draw(DottedLine, new Rectangle(SelectionBox.X + aCounter, thePositionY, 10, 5), Color.White);
                    }
                }
                return;
            }
            if (SelectionBox.Width < 0)
            {
                for (int aCounter = -10; aCounter >= SelectionBox.Width; aCounter = aCounter - 10)
                {
                    if (SelectionBox.Width - aCounter <= 0)
                    {
                        spriteBatch.Draw(DottedLine, new Rectangle(SelectionBox.X + aCounter, thePositionY, 10, 5), Color.White);
                    }
                }
            }
        }

        private void DrawVerticalLine(SpriteBatch spriteBatch, int thePositionX)
        {
            if (SelectionBox.Height <= 0)
            {
                if (SelectionBox.Height < 0)
                {
                    for (int aCounter = 0; aCounter >= SelectionBox.Height; aCounter = aCounter - 10)
                    {
                        if (SelectionBox.Height - aCounter <= 0)
                        {
                            spriteBatch.Draw(DottedLine, new Rectangle(thePositionX - 10, SelectionBox.Y + aCounter, 10, 5), Color.White);
                        }
                    }
                }
                return;
            }
            for (int aCounter = -2; aCounter <= SelectionBox.Height; aCounter = aCounter + 10)
            {
                if (SelectionBox.Height - aCounter >= 0)
                {
                    spriteBatch.Draw(DottedLine, new Rectangle(thePositionX, SelectionBox.Y + aCounter, 10, 5), new Rectangle(0, 0, DottedLine.Width, DottedLine.Height), Color.White, 90f.ToRadians(), new Vector2(0f, 0f), SpriteEffects.None, 0f);
                }
            }
        }

        public override void ExitScreen()
        {
            ScreenManager.AddScreen(new MainMenuScreen());
            base.ExitScreen();
        }

        private string GetDesignStateText()
        {
            switch (DesignState)
            {
                case Restrictions.I:
                {
                    DescriptionOfState = "Internal hull slots are important to the health of your ship. If these slots are 70% destroyed or more, then your ship will die. There are many modules, such as power plants, that can only be placed in Internal slots";
                    return "Internal";
                }
                case Restrictions.IO:
                {
                    DescriptionOfState = "IO slots are dual-purpose slots that can be equipped with modules bearing either the I or the O restriction";
                    return "Internal / Outside";
                }
                case Restrictions.IOE:
                {
                    return "";
                }
                case Restrictions.O:
                {
                    DescriptionOfState = "O slots are slots that are on the outside of your ship. Typically weapons and armor go in external slots";
                    return "Outside";
                }
                case Restrictions.E:
                {
                    DescriptionOfState = "Engine slots may only be equipped with engine modules";
                    return "Engine";
                }
                default:
                {
                    return "";
                }
            }
        }

        public Ship GetDisplayedShip()
        {
            return StartingShipList[selectedShip];
        }

        public Restrictions GetRestrictionFromText(string text)
        {
            string str = text;
            string str1 = str;
            if (str != null)
            {
                if (str1 == "I")
                {
                    return Restrictions.I;
                }
                if (str1 == "O")
                {
                    return Restrictions.O;
                }
                if (str1 == "IO")
                {
                    return Restrictions.IO;
                }
                if (str1 == "E")
                {
                    return Restrictions.E;
                }
            }
            return Restrictions.I;
        }

        public override bool HandleInput(InputState input)
        {
            if (!IsActive)
            {
                return false;
            }
            if (LoadModelButton.HandleInput(input))
            {
                ScreenManager.AddScreen(new LoadModelScreen(this));
            }
            if (input.Escaped)
            {
                ActiveModule = null;
                ExitScreen();
            }
            if (ShipNameBox.HandlingInput)
            {
                ShipNameBox.HandleTextInput(ref HullName, input);
                ShipNameBox.Text = HullName;
            }
            if (!ShipNameBox.ClickableArea.HitTest(input.CursorPosition))
            {
                ShipNameBox.Hover = false;
                if (input.InGameSelect)
                {
                    ShipNameBox.HandlingInput = false;
                }
            }
            else
            {
                ShipNameBox.Hover = true;
                if (input.InGameSelect)
                {
                    ShipNameBox.HandlingInput = true;
                }
            }
            foreach (ToggleButton button in DesignStateButtons)
            {
                if (!button.Rect.HitTest(input.CursorPosition))
                {
                    button.Hover = false;
                }
                else
                {
                    if (!button.Hover)
                    {
                        GameAudio.ButtonMouseOver();
                    }
                    button.Hover = true;
                    if (button.HasToolTip)
                    {
                        ToolTip.CreateTooltip(button.WhichToolTip);
                    }
                    if (input.InGameSelect)
                    {
                        GameAudio.AcceptClick();
                        SetRestrictionFromText(button.Action);
                    }
                }
                if (GetRestrictionFromText(button.Action) != DesignState)
                {
                    button.Active = false;
                }
                else
                {
                    button.Active = true;
                }
            }
            if (input.C)
            {
                MarkThruster();
            }
            if (SaveHullButton.HandleInput(input))
            {
                SaveShipData("New Ship");
            }
            if (input.ScrollIn)  tscale += 1f;
            if (input.ScrollOut) tscale -= 1f;
            if (input.Right)
            {
                heat = 1f;
            }
            if (input.Left)
            {
                heat = 0.01f;
            }
            if (input.Up)
            {
                applyThruster = true;
            }
            if (input.Down)
            {
                applyThruster = false;
            }
            if (input.YButtonDown || input.Right)
            {
                NextDesignState();
                return true;
            }
            if (input.BButtonDown)
            {
                ExitScreen();
                return true;
            }
            return false;
        }

        public void HandleInput()
        {
            if (!IsActive)
            {
                return;
            }
            if (Input.LeftMouseClick)
            {
                SelectionBox = new Rectangle(Input.MouseCurr.X, Input.MouseCurr.Y, 0, 0);
            }
            if (Input.LeftMouseDown)
            {
                SelectionBox = new Rectangle(SelectionBox.X, SelectionBox.Y, Input.MouseCurr.X - SelectionBox.X, Input.MouseCurr.Y - SelectionBox.Y);
            }
            else if (Input.LeftMouseClick)
            {
                foreach (SlotStruct slot in SlotList)
                {
                    if (!slot.Intersects(SelectionBox) || ActiveModule != null)
                    {
                        continue;
                    }
                    slot.PQ.Filled = !slot.PQ.Filled;
                    slot.Restrictions = DesignState;
                }
            }
            if (Input.LeftMouseUp)
            {
                SelectionBox = new Rectangle(-1, -1, 0, 0);
            }
            if (applyThruster)
            {
                tPos = new Vector2(Input.MouseCurr.X - ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2, 
                                        Input.MouseCurr.Y - ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2);
            }
        }

        public override void LoadContent()
        {
            ScreenManager.RemoveAllObjects();
            int screenWidth  = ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth;
            int screenHeight = ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight;

            PrimitiveQuad.Device = ScreenManager.GraphicsDevice;
            aspect = new Vector2(screenWidth, screenHeight);
            border = new PrimitiveQuad(aspect.X / 2f - 512f, aspect.Y / 2f - 512f, 1024f, 1024f);
            what = border.Rect;

            var designPos = new Vector2(what.X - 32f, what.Y + 5f);
            void AddDesignBtn(string icon)
            {
                var button = new ToggleButton(designPos, ToggleButtonStyle.Formation, icon) {Action = icon};
                DesignStateButtons.Add(button);
                designPos.Y += 29f;
            }
            AddDesignBtn("I");
            AddDesignBtn("IO");
            AddDesignBtn("O");
            AddDesignBtn("E");

            LoadModelButton = new DanButton(new Vector2(20f, (screenHeight - 50)), "Load Model");
            SaveHullButton = new DanButton(new Vector2((screenWidth - 200), (screenHeight - 50)), "Save Hull");
            ShipNameBox = new UITextEntry
            {
                ClickableArea = new Rectangle(screenWidth - 200, screenHeight - 115, 180, 20),
                Text = HullName
            };
            AssignLightRig("example/ShipyardLightrig");
            ScreenManager.environment = TransientContent.Load<SceneEnvironment>("example/scene_environment");
            float aspectRatio = Viewport.Width / (float)Viewport.Height;
            Vector3 camPos = cameraPosition * new Vector3(-1f, 1f, 1f);
            view = Matrix.CreateRotationY(180f.ToRadians())
                 * Matrix.CreateLookAt(camPos, new Vector3(camPos.X, camPos.Y, 0f), new Vector3(0f, -1f, 0f));
            projection = Matrix.CreatePerspectiveFieldOfView(0.7853982f, aspectRatio, 1f, 10000f);
            moduleSlot = TransientContent.Load<SubTexture>("Textures/Ships/singlebox");
            DottedLine = TransientContent.Load<SubTexture>("Textures/UI/DottedLine");
            ConfigureSlots();
            thruster = new Thruster();
            thruster.LoadAndAssignDefaultEffects(TransientContent);
            base.LoadContent();
        }

        public void LoadModel(string modelPath)
        {
            if (shipSO != null)
                RemoveObject(shipSO);
            try
            {
                shipSO = StaticMesh.GetSceneMesh(TransientContent, modelPath);

            }
            catch (Exception)
            {
                shipSO = StaticMesh.GetSceneMesh(TransientContent, modelPath, animated:true);
            }
            shipSO.World = worldMatrix;
            ModelPath = modelPath;
            AddObject(shipSO);
        }


        private void MarkThruster()
        {
            ThrusterZone z = new ThrusterZone();
            Vector2 thrPos = (tPos + new Vector2(ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2)) - new Vector2(border.X, border.Y);
            z.Position = thrPos;
            z.Scale = tscale;
            TList.Add(z);
        }

        private void NextDesignState()
        {
            switch (DesignState)
            {
                case Restrictions.I:  DesignState = Restrictions.O; return;
                case Restrictions.IO: DesignState = Restrictions.E; return;
                case Restrictions.IOE: return;
                case Restrictions.O:  DesignState = Restrictions.IO; return;
                case Restrictions.E:  DesignState = Restrictions.I;  return;
            }
        }

        public void SaveShipData(string name)
        {
            var data = new ShipData
            {
                Name      = name,
                ModelPath = Path.GetFileNameWithoutExtension(ModelPath),
                Role      = ShipData.RoleName.carrier,
                Hull      = HullName,
                IconPath  = "ShipIcons/hunter"
            };

            var filledModules = new Array<ModuleSlotData> { Capacity = SlotList.Count };
            for (int i = 0; i < SlotList.Count; ++i)
            {
                SlotStruct slot = SlotList[i];
                if (!slot.PQ.Filled)
                    continue;

                filledModules.Add(new ModuleSlotData
                {
                    Position           = slot.ModuleCenter - border.Position,
                    InstalledModuleUID = slot.ModuleUID,
                    Restrictions       = slot.Restrictions
                });
            }
            data.ModuleSlots    = filledModules.ToArray();
            data.DefaultAIState = AIState.AwaitingOrders;
            data.ThrusterList   = TList;
            var ser = new XmlSerializer(typeof(ShipData));
            using (var wfs = new StreamWriter($"Ship Tool/{HullName}.xml"))
                ser.Serialize(wfs, data);
        }

        public void SetActiveModule(ShipModule mod)
        {
            GameAudio.SmallServo();
            ActiveModule = mod;
        }

        public void SetRestrictionFromText(string text)
        {
            switch (text)
            {
                case "I":  DesignState = Restrictions.I;  return;
                case "O":  DesignState = Restrictions.O;  return;
                case "IO": DesignState = Restrictions.IO; return;
                case "E":  DesignState = Restrictions.E;  return;
            }
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            ScreenManager.editor.Update(gameTime);
            Vector3 camPos = cameraPosition * new Vector3(-1f, 1f, 1f);
            view = Matrix.CreateTranslation(0f, 0f, 0f)
                 * Matrix.CreateRotationY(RadMath.PI)
                 * Matrix.CreateRotationX(0f)
                 * Matrix.CreateLookAt(camPos, new Vector3(camPos.X, camPos.Y, 0f), new Vector3(0f, -1f, 0f));
            designInputState.Update(gameTime);
            HandleInput();
            thruster.tscale = tscale;
            thruster.WorldPos = new Vector3(tPos.X, tPos.Y, 30f);
            thruster.Update(new Vector3(0f, -1f, 0f), heat, 0.002f, camPos, Color.LightBlue, Color.OrangeRed);
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
        }

        public struct ThrusterZone
        {
            public Vector2 Position;
            [XmlElement(ElementName = "scale")]
            public float Scale;
        }
    }
}
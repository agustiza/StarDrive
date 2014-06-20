using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SgMotion;
using Ship_Game.Gameplay;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Editor;
using SynapseGaming.LightingSystem.Lights;
using SynapseGaming.LightingSystem.Rendering;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Xml.Serialization;

namespace Ship_Game
{
	public class ShipDesignScreen : GameScreen, IDisposable
	{
		private Matrix worldMatrix = Matrix.Identity;

		private Matrix view;

		private Matrix projection;

		public Camera2d camera;

		public List<ToggleButton> CombatStatusButtons = new List<ToggleButton>();

		public bool Debug;

		public ShipData ActiveHull;

		public EmpireUIOverlay EmpireUI;

		public static UniverseScreen screen;

		private Menu1 ModuleSelectionMenu;

		private Model ActiveModel;

		private SceneObject shipSO;

		private Vector3 cameraPosition = new Vector3(0f, 0f, 1300f);

		public List<SlotStruct> Slots = new List<SlotStruct>();

		private Vector2 offset;

		private Ship_Game.Gameplay.CombatState CombatState = Ship_Game.Gameplay.CombatState.AttackRuns;

		private bool ShipSaved = true;

		private List<ShipData> AvailableHulls = new List<ShipData>();

		private List<UIButton> Buttons = new List<UIButton>();

		//private UIButton HullLeft;

		//private UIButton HullRight;

		private UIButton ToggleOverlayButton;

		private UIButton SaveButton;

		private UIButton LoadButton;

		private Submenu modSel;

		private Submenu statsSub;

		private Menu1 ShipStats;

		private Menu1 activeModWindow;

		private Submenu activeModSubMenu;

		private ScrollList weaponSL;

		private bool Reset = true;

		private Submenu ChooseFighterSub;

		private ScrollList ChooseFighterSL;

		private bool LowRes;

		private float LowestX;

		private float HighestX;

		private GenericButton ArcsButton;

		private CloseButton close;

		private float OriginalZ;

		private Rectangle choosefighterrect;

		private Rectangle SearchBar;

		private Rectangle bottom_sep;

		private ScrollList hullSL;

		private Rectangle HullSelectionRect;

		private Submenu hullSelectionSub;

		private Rectangle BlackBar;

		private Rectangle SideBar;

		private DanButton Fleets;

		private DanButton ShipList;

		private DanButton Shipyard;

		private Vector2 SelectedCatTextPos;

		private SkinnableButton wpn;

		private SkinnableButton pwr;

		private SkinnableButton def;

		private SkinnableButton spc;

		private Rectangle ModuleSelectionArea = new Rectangle();

		private List<ShipDesignScreen.ModuleCatButton> ModuleCatButtons = new List<ShipDesignScreen.ModuleCatButton>();

		private List<ModuleButton> ModuleButtons = new List<ModuleButton>();

		private Rectangle upArrow;

		private Rectangle downArrow;

		private MouseState mouseStateCurrent;

		private MouseState mouseStatePrevious;

		private ShipModule HighlightedModule;

		private Vector2 cameraVelocity = Vector2.Zero;

		private Vector2 StartDragPos = new Vector2();

		private ShipData changeto;

		private string screenToLaunch;

		private bool ShowAllArcs;

		private ShipModule HoveredModule;

		private float TransitionZoom = 1f;

		private ShipDesignScreen.SlotModOperation operation;

		//private ShipDesignScreen.Colors sColor;

		private int HullIndex;

		private ShipModule ActiveModule;

		private ShipDesignScreen.ActiveModuleState ActiveModState;

		private Selector selector;

		public bool ToggleOverlay = true;

		private Vector2 starfieldPos = Vector2.Zero;

		private int scrollPosition;

		public Stack<DesignAction> DesignStack = new Stack<DesignAction>();

		public ShipDesignScreen(EmpireUIOverlay EmpireUI)
		{
			this.EmpireUI = EmpireUI;
			base.TransitionOnTime = TimeSpan.FromSeconds(0.25);
		}

		public void ChangeHull(ShipData hull)
		{
			this.Reset = true;
			this.DesignStack.Clear();
			lock (GlobalStats.ObjectManagerLocker)
			{
				if (this.shipSO != null)
				{
					base.ScreenManager.inter.ObjectManager.Remove(this.shipSO);
				}
			}
			this.ActiveHull = new ShipData()
			{
				Animated = hull.Animated,
				CombatState = hull.CombatState,
				Hull = hull.Hull,
				IconPath = hull.IconPath,
				ModelPath = hull.ModelPath,
				Name = hull.Name,
				Role = hull.Role,
				ShipStyle = hull.ShipStyle,
				ThrusterList = hull.ThrusterList,
				ModuleSlotList = new List<ModuleSlotData>()
			};
			foreach (ModuleSlotData slot in hull.ModuleSlotList)
			{
				ModuleSlotData data = new ModuleSlotData()
				{
					Position = slot.Position,
					Restrictions = slot.Restrictions,
					facing = slot.facing,
					InstalledModuleUID = slot.InstalledModuleUID
				};
				this.ActiveHull.ModuleSlotList.Add(slot);
			}
			this.CombatState = hull.CombatState;
			if (!hull.Animated)
			{
				this.ActiveModel = Ship_Game.ResourceManager.GetModel(this.ActiveHull.ModelPath);
				ModelMesh mesh = this.ActiveModel.Meshes[0];
				this.shipSO = new SceneObject(mesh)
				{
					ObjectType = ObjectType.Dynamic,
					World = this.worldMatrix
				};
				lock (GlobalStats.ObjectManagerLocker)
				{
					base.ScreenManager.inter.ObjectManager.Submit(this.shipSO);
				}
			}
			else
			{
				SkinnedModel sm = Ship_Game.ResourceManager.GetSkinnedModel(this.ActiveHull.ModelPath);
				this.shipSO = new SceneObject(sm.Model)
				{
					ObjectType = ObjectType.Dynamic,
					World = this.worldMatrix
				};
				lock (GlobalStats.ObjectManagerLocker)
				{
					base.ScreenManager.inter.ObjectManager.Submit(this.shipSO);
				}
			}
			foreach (ToggleButton button in this.CombatStatusButtons)
			{
				string action = button.Action;
				string str = action;
				if (action == null)
				{
					continue;
				}
				if (str == "attack")
				{
					if (this.CombatState != Ship_Game.Gameplay.CombatState.AttackRuns)
					{
						button.Active = false;
					}
					else
					{
						button.Active = true;
					}
				}
				else if (str == "arty")
				{
					if (this.CombatState != Ship_Game.Gameplay.CombatState.Artillery)
					{
						button.Active = false;
					}
					else
					{
						button.Active = true;
					}
				}
				else if (str == "hold")
				{
					if (this.CombatState != Ship_Game.Gameplay.CombatState.HoldPosition)
					{
						button.Active = false;
					}
					else
					{
						button.Active = true;
					}
				}
				else if (str == "orbit_left")
				{
					if (this.CombatState != Ship_Game.Gameplay.CombatState.OrbitLeft)
					{
						button.Active = false;
					}
					else
					{
						button.Active = true;
					}
				}
				else if (str != "orbit_right")
				{
					if (str == "evade")
					{
						if (this.CombatState != Ship_Game.Gameplay.CombatState.Evade)
						{
							button.Active = false;
						}
						else
						{
							button.Active = true;
						}
					}
				}
				else if (this.CombatState != Ship_Game.Gameplay.CombatState.OrbitRight)
				{
					button.Active = false;
				}
				else
				{
					button.Active = true;
				}
			}
			this.SetupSlots();
		}

		private void ChangeModuleState(ShipDesignScreen.ActiveModuleState state)
		{
			if (this.ActiveModule == null)
			{
				return;
			}
			byte x = Ship_Game.ResourceManager.ShipModulesDict[this.ActiveModule.UID].XSIZE;
			byte y = Ship_Game.ResourceManager.ShipModulesDict[this.ActiveModule.UID].YSIZE;
			switch (state)
			{
				case ShipDesignScreen.ActiveModuleState.Normal:
				{
					this.ActiveModule.XSIZE = Ship_Game.ResourceManager.ShipModulesDict[this.ActiveModule.UID].XSIZE;
					this.ActiveModule.YSIZE = Ship_Game.ResourceManager.ShipModulesDict[this.ActiveModule.UID].YSIZE;
					this.ActiveModState = ShipDesignScreen.ActiveModuleState.Normal;
					return;
				}
				case ShipDesignScreen.ActiveModuleState.Left:
				{
					this.ActiveModule.XSIZE = y;
					this.ActiveModule.YSIZE = x;
					this.ActiveModState = ShipDesignScreen.ActiveModuleState.Left;
					this.ActiveModule.facing = 270f;
					return;
				}
				case ShipDesignScreen.ActiveModuleState.Right:
				{
					this.ActiveModule.XSIZE = y;
					this.ActiveModule.YSIZE = x;
					this.ActiveModState = ShipDesignScreen.ActiveModuleState.Right;
					this.ActiveModule.facing = 90f;
					return;
				}
				case ShipDesignScreen.ActiveModuleState.Rear:
				{
					this.ActiveModule.XSIZE = Ship_Game.ResourceManager.ShipModulesDict[this.ActiveModule.UID].XSIZE;
					this.ActiveModule.YSIZE = Ship_Game.ResourceManager.ShipModulesDict[this.ActiveModule.UID].YSIZE;
					this.ActiveModState = ShipDesignScreen.ActiveModuleState.Rear;
					this.ActiveModule.facing = 180f;
					return;
				}
				default:
				{
					return;
				}
			}
		}

		private void CheckAndPowerConduit(SlotStruct slot)
		{
			slot.module.Powered = true;
			slot.CheckedConduits = true;
			foreach (SlotStruct ss in this.Slots)
			{
				if (ss == slot || Math.Abs(slot.pq.X - ss.pq.X) / 16 + Math.Abs(slot.pq.Y - ss.pq.Y) / 16 != 1 || ss.module == null || ss.module.ModuleType != ShipModuleType.PowerConduit || ss.module.ModuleType != ShipModuleType.PowerConduit || ss.CheckedConduits)
				{
					continue;
				}
				this.CheckAndPowerConduit(ss);
			}
		}

		private bool CheckDesign()
		{
			bool EmptySlots = true;
			bool hasBridge = false;
			foreach (SlotStruct slot in this.Slots)
			{
				if (!slot.isDummy && slot.ModuleUID == null)
				{
					EmptySlots = false;
				}
				if (slot.ModuleUID == null || !slot.module.IsCommandModule)
				{
					continue;
				}
				hasBridge = true;
			}
			if (!hasBridge && this.ActiveHull.Role != "platform" && this.ActiveHull.Role != "station" || !EmptySlots)
			{
				return false;
			}
			return true;
		}

		private void ClearDestinationSlots(SlotStruct slot)
		{
			for (int y = 0; y < this.ActiveModule.YSIZE; y++)
			{
				for (int x = 0; x < this.ActiveModule.XSIZE; x++)
				{
					if (x == 0 & y == 0)
					{
						foreach (SlotStruct dummyslot in this.Slots)
						{
							if (dummyslot.pq.Y != slot.pq.Y + 16 * y || dummyslot.pq.X != slot.pq.X + 16 * x)
							{
								continue;
							}
							if (dummyslot.module != null)
							{
								SlotStruct copy = new SlotStruct()
								{
									pq = dummyslot.pq,
									Restrictions = dummyslot.Restrictions,
									facing = dummyslot.facing,
									ModuleUID = dummyslot.ModuleUID,
									module = dummyslot.module,
									state = dummyslot.state,
									slotReference = dummyslot.slotReference
								};
								if (this.DesignStack.Count > 0)
								{
									this.DesignStack.Peek().AlteredSlots.Add(copy);
								}
								this.ClearParentSlot(dummyslot);
							}
							if (dummyslot.isDummy && dummyslot.parent != null && dummyslot.parent.module != null)
							{
								this.ClearParentSlot(dummyslot.parent);
							}
							dummyslot.ModuleUID = null;
							dummyslot.isDummy = false;
							dummyslot.tex = null;
							dummyslot.module = null;
							dummyslot.state = ShipDesignScreen.ActiveModuleState.Normal;
							dummyslot.parent = slot;
						}
					}
				}
			}
		}

		private void ClearDestinationSlotsNoStack(SlotStruct slot)
		{
			for (int y = 0; y < this.ActiveModule.YSIZE; y++)
			{
				for (int x = 0; x < this.ActiveModule.XSIZE; x++)
				{
					if (x == 0 & y == 0)
					{
						foreach (SlotStruct dummyslot in this.Slots)
						{
							if (dummyslot.pq.Y != slot.pq.Y + 16 * y || dummyslot.pq.X != slot.pq.X + 16 * x)
							{
								continue;
							}
							if (dummyslot.module != null)
							{
								this.ClearParentSlot(dummyslot);
							}
							if (dummyslot.isDummy && dummyslot.parent != null && dummyslot.parent.module != null)
							{
								this.ClearParentSlotNoStack(dummyslot.parent);
							}
							dummyslot.ModuleUID = null;
							dummyslot.isDummy = false;
							dummyslot.tex = null;
							dummyslot.module = null;
							dummyslot.parent = slot;
							dummyslot.state = ShipDesignScreen.ActiveModuleState.Normal;
						}
					}
				}
			}
		}

		private void ClearParentSlot(SlotStruct parent)
		{
			SlotStruct copy = new SlotStruct()
			{
				pq = parent.pq,
				Restrictions = parent.Restrictions,
				facing = parent.facing,
				ModuleUID = parent.ModuleUID,
				module = parent.module,
				state = parent.state,
				slotReference = parent.slotReference
			};
			if (this.DesignStack.Count > 0)
			{
				this.DesignStack.Peek().AlteredSlots.Add(copy);
			}
			for (int y = 0; y < parent.module.YSIZE; y++)
			{
				for (int x = 0; x < parent.module.XSIZE; x++)
				{
					if (x == 0 & y == 0)
					{
						foreach (SlotStruct dummyslot in this.Slots)
						{
							if (dummyslot.pq.Y != parent.pq.Y + 16 * y || dummyslot.pq.X != parent.pq.X + 16 * x)
							{
								continue;
							}
							dummyslot.ModuleUID = null;
							dummyslot.isDummy = false;
							dummyslot.tex = null;
							dummyslot.module = null;
							dummyslot.parent = null;
							dummyslot.state = ShipDesignScreen.ActiveModuleState.Normal;
						}
					}
				}
			}
			parent.ModuleUID = null;
			parent.isDummy = false;
			parent.tex = null;
			parent.module = null;
			parent.state = ShipDesignScreen.ActiveModuleState.Normal;
		}

		private void ClearParentSlotNoStack(SlotStruct parent)
		{
			for (int y = 0; y < parent.module.YSIZE; y++)
			{
				for (int x = 0; x < parent.module.XSIZE; x++)
				{
					if (x == 0 & y == 0)
					{
						foreach (SlotStruct dummyslot in this.Slots)
						{
							if (dummyslot.pq.Y != parent.pq.Y + 16 * y || dummyslot.pq.X != parent.pq.X + 16 * x)
							{
								continue;
							}
							dummyslot.ModuleUID = null;
							dummyslot.isDummy = false;
							dummyslot.tex = null;
							dummyslot.module = null;
							dummyslot.parent = null;
							dummyslot.state = ShipDesignScreen.ActiveModuleState.Normal;
						}
					}
				}
			}
			parent.ModuleUID = null;
			parent.isDummy = false;
			parent.tex = null;
			parent.module = null;
			parent.state = ShipDesignScreen.ActiveModuleState.Normal;
		}

		private void ClearSlot(SlotStruct slot)
		{
			if (!slot.isDummy)
			{
				if (slot.module != null)
				{
					this.ClearParentSlot(slot);
					return;
				}
				slot.ModuleUID = null;
				slot.isDummy = false;
				slot.tex = null;
				slot.parent = null;
				slot.module = null;
				slot.state = ShipDesignScreen.ActiveModuleState.Normal;
			}
			else if (slot.parent.module != null)
			{
				this.ClearParentSlot(slot.parent);
				return;
			}
		}

		private void ClearSlotNoStack(SlotStruct slot)
		{
			if (!slot.isDummy)
			{
				if (slot.module != null)
				{
					this.ClearParentSlotNoStack(slot);
					return;
				}
				slot.ModuleUID = null;
				slot.isDummy = false;
				slot.tex = null;
				slot.parent = null;
				slot.module = null;
				slot.state = ShipDesignScreen.ActiveModuleState.Normal;
			}
			else if (slot.parent.module != null)
			{
				this.ClearParentSlotNoStack(slot.parent);
				return;
			}
		}

		public void CreateShipModuleSelectionWindow()
		{
			this.upArrow = new Rectangle(this.ModuleSelectionArea.X + this.ModuleSelectionArea.Width - 22, this.ModuleSelectionArea.Y, 22, 30);
			this.downArrow = new Rectangle(this.ModuleSelectionArea.X + this.ModuleSelectionArea.Width - 22, this.ModuleSelectionArea.Y + this.ModuleSelectionArea.Height - 32, 20, 30);
			List<string> Categories = new List<string>();
			Dictionary<string, List<ShipModule>> ModuleDict = new Dictionary<string, List<ShipModule>>();
			foreach (KeyValuePair<string, ShipModule> module in Ship_Game.ResourceManager.ShipModulesDict)
			{
				if (!EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).GetMDict()[module.Key] || module.Value.UID == "Dummy")
				{
					continue;
				}
				string cat = module.Value.ModuleType.ToString();
				if (!Categories.Contains(cat))
				{
					Categories.Add(cat);
				}
				if (ModuleDict.ContainsKey(cat))
				{
					ModuleDict[cat].Add(module.Value);
				}
				else
				{
					ModuleDict.Add(cat, new List<ShipModule>());
					ModuleDict[cat].Add(module.Value);
				}
				ModuleButton mb = new ModuleButton()
				{
					moduleRect = new Rectangle(0, 0, 128, 128),
					ModuleUID = module.Key
				};
				this.ModuleButtons.Add(mb);
			}
			Categories.Sort();
			int i = 0;
			foreach (string cat in Categories)
			{
				ShipDesignScreen.ModuleCatButton ModuleCatButton = new ShipDesignScreen.ModuleCatButton()
				{
					mRect = new Rectangle(this.ModuleSelectionArea.X + 10, this.ModuleSelectionArea.Y + 10 + i * 25, 45, 25),
					Category = cat
				};
				this.ModuleCatButtons.Add(ModuleCatButton);
				i++;
			}
			int x = 0;
			int y = 0;
			foreach (ModuleButton mb in this.ModuleButtons)
			{
				mb.moduleRect.X = this.ModuleSelectionArea.X + 20 + x * 128;
				mb.moduleRect.Y = this.ModuleSelectionArea.Y + 10 + y * 128;
				x++;
				if (base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth <= 1280)
				{
					if (x <= 1)
					{
						continue;
					}
					y++;
					x = 0;
				}
				else
				{
					if (x <= 2)
					{
						continue;
					}
					y++;
					x = 0;
				}
			}
		}

		private void CreateSOFromHull()
		{
			lock (GlobalStats.ObjectManagerLocker)
			{
				if (this.shipSO != null)
				{
					base.ScreenManager.inter.ObjectManager.Remove(this.shipSO);
				}
				ModelMesh mesh = this.ActiveModel.Meshes[0];
				this.shipSO = new SceneObject(mesh)
				{
					ObjectType = ObjectType.Dynamic,
					World = this.worldMatrix
				};
				base.ScreenManager.inter.ObjectManager.Submit(this.shipSO);
				this.SetupSlots();
			}
		}

		private void DebugAlterSlot(Vector2 SlotPos, ShipDesignScreen.SlotModOperation op)
		{
			ModuleSlotData toRemove;
			switch (op)
			{
				case ShipDesignScreen.SlotModOperation.Delete:
				{
					toRemove = null;
					foreach (ModuleSlotData slotdata in this.ActiveHull.ModuleSlotList)
					{
						if (slotdata.Position != SlotPos)
						{
							continue;
						}
						toRemove = slotdata;
						break;
					}
					if (toRemove == null)
					{
						return;
					}
					this.ActiveHull.ModuleSlotList.Remove(toRemove);
					this.ChangeHull(this.ActiveHull);
					return;
				}
				case ShipDesignScreen.SlotModOperation.I:
				{
					toRemove = null;
					foreach (ModuleSlotData slotdata in this.ActiveHull.ModuleSlotList)
					{
						if (slotdata.Position != SlotPos)
						{
							continue;
						}
						toRemove = slotdata;
						break;
					}
					if (toRemove == null)
					{
						return;
					}
					toRemove.Restrictions = Restrictions.I;
					this.ChangeHull(this.ActiveHull);
					return;
				}
				case ShipDesignScreen.SlotModOperation.IO:
				{
					toRemove = null;
					foreach (ModuleSlotData slotdata in this.ActiveHull.ModuleSlotList)
					{
						if (slotdata.Position != SlotPos)
						{
							continue;
						}
						toRemove = slotdata;
						break;
					}
					if (toRemove == null)
					{
						return;
					}
					toRemove.Restrictions = Restrictions.IO;
					this.ChangeHull(this.ActiveHull);
					return;
				}
				case ShipDesignScreen.SlotModOperation.O:
				{
					toRemove = null;
					foreach (ModuleSlotData slotdata in this.ActiveHull.ModuleSlotList)
					{
						if (slotdata.Position != SlotPos)
						{
							continue;
						}
						toRemove = slotdata;
						break;
					}
					if (toRemove == null)
					{
						return;
					}
					toRemove.Restrictions = Restrictions.O;
					this.ChangeHull(this.ActiveHull);
					return;
				}
				case ShipDesignScreen.SlotModOperation.Add:
				{
					return;
				}
				case ShipDesignScreen.SlotModOperation.E:
				{
					toRemove = null;
					foreach (ModuleSlotData slotdata in this.ActiveHull.ModuleSlotList)
					{
						if (slotdata.Position != SlotPos)
						{
							continue;
						}
						toRemove = slotdata;
						break;
					}
					if (toRemove == null)
					{
						return;
					}
					toRemove.Restrictions = Restrictions.E;
					this.ChangeHull(this.ActiveHull);
					return;
				}
				default:
				{
					return;
				}
			}
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				lock (this)
				{
				}
			}
		}

		private void DoExit(object sender, EventArgs e)
		{
			this.ReallyExit();
		}

		private void DoExitToFleetsList(object sender, EventArgs e)
		{
			base.ScreenManager.AddScreen(new FleetDesignScreen(this.EmpireUI));
			this.ReallyExit();
		}

		private void DoExitToShipList(object sender, EventArgs e)
		{
			this.ReallyExit();
		}

		private void DoExitToShipsList(object sender, EventArgs e)
		{
			base.ScreenManager.AddScreen(new ShipListScreen(base.ScreenManager, this.EmpireUI));
			this.ReallyExit();
		}

		public override void Draw(GameTime gameTime)
		{
			int x;
			int y;
			//int x;    //wtf?
			//int y;
			Color lightGreen;
			Color color;
			lock (GlobalStats.ObjectManagerLocker)
			{
				base.ScreenManager.sceneState.BeginFrameRendering(this.view, this.projection, gameTime, base.ScreenManager.environment, true);
				base.ScreenManager.editor.BeginFrameRendering(base.ScreenManager.sceneState);
				base.ScreenManager.inter.BeginFrameRendering(base.ScreenManager.sceneState);
				ShipDesignScreen.screen.bg.Draw(ShipDesignScreen.screen, ShipDesignScreen.screen.starfield);
				base.ScreenManager.inter.RenderManager.Render();
			}
			base.ScreenManager.SpriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None, this.camera.get_transformation(base.ScreenManager.GraphicsDevice));
			if (this.ToggleOverlay)
			{
				foreach (SlotStruct slot in this.Slots)
				{
					if (slot.module != null)
					{
						base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Modules/tile_concreteglass_1x1"], new Rectangle(slot.pq.enclosingRect.X, slot.pq.enclosingRect.Y, 16 * slot.module.XSIZE, 16 * slot.module.YSIZE), Color.Gray);
					}
					else if (!slot.isDummy)
					{
						if (this.ActiveModule != null)
						{
							SpriteBatch spriteBatch = base.ScreenManager.SpriteBatch;
							Texture2D item = Ship_Game.ResourceManager.TextureDict["Modules/tile_concreteglass_1x1"];
							Rectangle rectangle = slot.pq.enclosingRect;
							if (slot.ShowValid)
							{
								color = Color.LightGreen;
							}
							else
							{
								color = (slot.ShowInvalid ? Color.Red : Color.White);
							}
							spriteBatch.Draw(item, rectangle, color);
							if (slot.Powered)
							{
								base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Modules/tile_concreteglass_1x1"], slot.pq.enclosingRect, new Color(255, 255, 0, 150));
							}
						}
						else if (slot.Powered)
						{
							base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Modules/tile_concreteglass_1x1"], slot.pq.enclosingRect, Color.Yellow);
						}
						else
						{
							SpriteBatch spriteBatch1 = base.ScreenManager.SpriteBatch;
							Texture2D texture2D = Ship_Game.ResourceManager.TextureDict["Modules/tile_concreteglass_1x1"];
							Rectangle rectangle1 = slot.pq.enclosingRect;
							if (slot.ShowValid)
							{
								lightGreen = Color.LightGreen;
							}
							else
							{
								lightGreen = (slot.ShowInvalid ? Color.Red : Color.White);
							}
							spriteBatch1.Draw(texture2D, rectangle1, lightGreen);
						}
					}
					if (slot.module != null || slot.isDummy)
					{
						continue;
					}
					base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, string.Concat(" ", slot.Restrictions), new Vector2((float)slot.pq.enclosingRect.X, (float)slot.pq.enclosingRect.Y), Color.Navy, 0f, Vector2.Zero, 0.4f, SpriteEffects.None, 1f);
				}
				foreach (SlotStruct slot in this.Slots)
				{
					if (slot.ModuleUID == null || slot.tex == null)
					{
						continue;
					}
					if (slot.state != ShipDesignScreen.ActiveModuleState.Normal)
					{
						Rectangle r = new Rectangle(slot.pq.enclosingRect.X, slot.pq.enclosingRect.Y, 16 * slot.module.XSIZE, 16 * slot.module.YSIZE);
						switch (slot.state)
						{
							case ShipDesignScreen.ActiveModuleState.Left:
							{
								x = slot.module.YSIZE * 16;
								y = slot.module.XSIZE * 16;
								r.Width = x;
								r.Height = y;
								r.Y = r.Y + x;
								Rectangle? nullable = null;
								base.ScreenManager.SpriteBatch.Draw(slot.tex, r, nullable, Color.White, -1.57079637f, Vector2.Zero, SpriteEffects.None, 1f);
								break;
							}
							case ShipDesignScreen.ActiveModuleState.Right:
							{
								x = slot.module.YSIZE * 16;
								y = slot.module.XSIZE * 16;
								r.Width = x;
								r.Height = y;
								r.X = r.X + y;
								Rectangle? nullable1 = null;
								base.ScreenManager.SpriteBatch.Draw(slot.tex, r, nullable1, Color.White, 1.57079637f, Vector2.Zero, SpriteEffects.None, 1f);
								break;
							}
							case ShipDesignScreen.ActiveModuleState.Rear:
							{
								Rectangle? nullable2 = null;
								base.ScreenManager.SpriteBatch.Draw(slot.tex, r, nullable2, Color.White, 0f, Vector2.Zero, SpriteEffects.FlipVertically, 1f);
								break;
							}
						}
					}
					else if (slot.module.XSIZE <= 1 && slot.module.YSIZE <= 1)
					{
						if (slot.module.ModuleType != ShipModuleType.PowerConduit)
						{
							base.ScreenManager.SpriteBatch.Draw(slot.tex, slot.pq.enclosingRect, Color.White);
						}
						else
						{
							string graphic = this.GetConduitGraphic(slot);
							base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict[string.Concat("Conduits/", graphic)], slot.pq.enclosingRect, Color.White);
							if (slot.module.Powered)
							{
								graphic = string.Concat(graphic, "_power");
								base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict[string.Concat("Conduits/", graphic)], slot.pq.enclosingRect, Color.White);
							}
						}
					}
					else if (slot.slotReference.Position.X <= 256f)
					{
						base.ScreenManager.SpriteBatch.Draw(slot.tex, new Rectangle(slot.pq.enclosingRect.X, slot.pq.enclosingRect.Y, 16 * slot.module.XSIZE, 16 * slot.module.YSIZE), Color.White);
					}
					else
					{
						Rectangle? nullable3 = null;
						base.ScreenManager.SpriteBatch.Draw(slot.tex, new Rectangle(slot.pq.enclosingRect.X, slot.pq.enclosingRect.Y, 16 * slot.module.XSIZE, 16 * slot.module.YSIZE), nullable3, Color.White, 0f, Vector2.Zero, SpriteEffects.FlipHorizontally, 1f);
					}
					if (slot.module != this.HoveredModule)
					{
						continue;
					}
					Primitives2D.DrawRectangle(base.ScreenManager.SpriteBatch, new Rectangle(slot.pq.enclosingRect.X, slot.pq.enclosingRect.Y, 16 * slot.module.XSIZE, 16 * slot.module.YSIZE), Color.White, 2f);
				}
				foreach (SlotStruct slot in this.Slots)
				{
					if (slot.ModuleUID == null || slot.tex == null || slot.module != this.HighlightedModule && !this.ShowAllArcs)
					{
						continue;
					}
					if (slot.module.shield_power_max > 0f)
					{
						Vector2 Center = new Vector2((float)(slot.pq.enclosingRect.X + 16 * slot.module.XSIZE / 2), (float)(slot.pq.enclosingRect.Y + 16 * slot.module.YSIZE / 2));
						Primitives2D.DrawCircle(base.ScreenManager.SpriteBatch, Center, slot.module.shield_radius, 50, Color.LightGreen);
					}
					if (slot.module.FieldOfFire != 90f)
					{
						if (slot.module.FieldOfFire == 0f)
						{
							continue;
						}
						float halfArc = slot.module.FieldOfFire / 2f;
						Vector2 Center = new Vector2((float)(slot.pq.enclosingRect.X + 16 * slot.module.XSIZE / 2), (float)(slot.pq.enclosingRect.Y + 16 * slot.module.YSIZE / 2));
						Vector2 leftArc = this.findPointFromAngleAndDistance(Center, slot.module.facing + -halfArc, 300f);
						Vector2 rightArc = this.findPointFromAngleAndDistance(Center, slot.module.facing + halfArc, 300f);
						leftArc = this.findPointFromAngleAndDistance(Center, slot.module.facing + -halfArc, 300f);
						rightArc = this.findPointFromAngleAndDistance(Center, slot.module.facing + halfArc, 300f);
						Color arc = new Color(255, 165, 0, 100);
						Primitives2D.DrawLine(base.ScreenManager.SpriteBatch, Center, leftArc, arc, 3f);
						Primitives2D.DrawLine(base.ScreenManager.SpriteBatch, Center, rightArc, arc, 3f);
					}
					else
					{
						Vector2 Center = new Vector2((float)(slot.pq.enclosingRect.X + 16 * slot.module.XSIZE / 2), (float)(slot.pq.enclosingRect.Y + 16 * slot.module.YSIZE / 2));
						Vector2 Origin = new Vector2(250f, 250f);
						if (slot.module.InstalledWeapon.WeaponType == "Ballistic Cannon")
						{
							Color drawcolor = new Color(255, 255, 0, 255);
							Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
							Rectangle? nullable4 = null;
							base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc90"], toDraw, nullable4, drawcolor, (float)MathHelper.ToRadians(slot.module.facing), Origin, SpriteEffects.None, 1f);
						}
						else if (slot.module.InstalledWeapon.WeaponType == "Energy Cannon")
						{
							Color drawcolor = new Color(255, 0, 0, 255);
							Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
							Rectangle? nullable5 = null;
							base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc90"], toDraw, nullable5, drawcolor, (float)MathHelper.ToRadians(slot.module.facing), Origin, SpriteEffects.None, 1f);
						}
						else if (slot.module.InstalledWeapon.WeaponType != "Energy Beam")
						{
							Color drawcolor = new Color(255, 0, 0, 255);
							Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
							Rectangle? nullable6 = null;
							base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc90"], toDraw, nullable6, drawcolor, (float)MathHelper.ToRadians(slot.module.facing), Origin, SpriteEffects.None, 1f);
						}
						else
						{
							Color drawcolor = new Color(0, 0, 255, 255);
							Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
							Rectangle? nullable7 = null;
							base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc90"], toDraw, nullable7, drawcolor, (float)MathHelper.ToRadians(slot.module.facing), Origin, SpriteEffects.None, 1f);
						}
					}
				}
				foreach (SlotStruct ss in this.Slots)
				{
					if (ss.module == null)
					{
						continue;
					}
					Vector2 Center = new Vector2((float)(ss.pq.X + 16 * ss.module.XSIZE / 2), (float)(ss.pq.Y + 16 * ss.module.YSIZE / 2));
					Vector2 lightOrigin = new Vector2(8f, 8f);
					if (ss.module.PowerDraw <= 0f || ss.module.Powered || ss.module.ModuleType == ShipModuleType.PowerConduit)
					{
						continue;
					}
					Rectangle? nullable8 = null;
					base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["UI/lightningBolt"], Center, nullable8, Color.White, 0f, lightOrigin, 1f, SpriteEffects.None, 1f);
				}
			}
			base.ScreenManager.SpriteBatch.End();
			base.ScreenManager.SpriteBatch.Begin();
			foreach (ModuleButton mb in this.ModuleButtons)
			{
				if (!HelperFunctions.CheckIntersection(this.ModuleSelectionArea, new Vector2((float)(mb.moduleRect.X + 30), (float)(mb.moduleRect.Y + 30))))
				{
					continue;
				}
				if (mb.isHighlighted)
				{
					base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["UI/blueHighlight"], mb.moduleRect, Color.White);
				}
				Rectangle modRect = new Rectangle(0, 0, Ship_Game.ResourceManager.ShipModulesDict[mb.ModuleUID].XSIZE * 16, Ship_Game.ResourceManager.ShipModulesDict[mb.ModuleUID].YSIZE * 16);
				//{
					modRect.X = mb.moduleRect.X + 64 - modRect.Width / 2;
                    modRect.Y = mb.moduleRect.Y + 64 - modRect.Height / 2;
				//};
				base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict[Ship_Game.ResourceManager.ShipModulesDict[mb.ModuleUID].IconTexturePath], modRect, Color.White);
				float nWidth = Fonts.Arial12.MeasureString(Localizer.Token(Ship_Game.ResourceManager.ShipModulesDict[mb.ModuleUID].NameIndex)).X;
				Vector2 nameCursor = new Vector2((float)(mb.moduleRect.X + 64) - nWidth / 2f, (float)(mb.moduleRect.Y + 128 - Fonts.Arial12.LineSpacing - 2));
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, Localizer.Token(Ship_Game.ResourceManager.ShipModulesDict[mb.ModuleUID].NameIndex), nameCursor, Color.White);
			}
			float single = (float)Mouse.GetState().X;
			MouseState state = Mouse.GetState();
			Vector2 MousePos = new Vector2(single, (float)state.Y);
			if (this.ActiveModule != null && !HelperFunctions.CheckIntersection(this.activeModSubMenu.Menu, MousePos) && !HelperFunctions.CheckIntersection(this.modSel.Menu, MousePos) && (!HelperFunctions.CheckIntersection(this.choosefighterrect, MousePos) || this.ActiveModule.ModuleType != ShipModuleType.Hangar || this.ActiveModule.IsSupplyBay || this.ActiveModule.IsTroopBay))
			{
				Rectangle r = new Rectangle(this.mouseStateCurrent.X, this.mouseStateCurrent.Y, (int)((float)(16 * this.ActiveModule.XSIZE) * this.camera.Zoom), (int)((float)(16 * this.ActiveModule.YSIZE) * this.camera.Zoom));
				switch (this.ActiveModState)
				{
					case ShipDesignScreen.ActiveModuleState.Normal:
					{
						base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict[Ship_Game.ResourceManager.ShipModulesDict[this.ActiveModule.UID].IconTexturePath], r, Color.White);
						break;
					}
					case ShipDesignScreen.ActiveModuleState.Left:
					{
						r.Y = r.Y + (int)((float)(16 * Ship_Game.ResourceManager.ShipModulesDict[this.ActiveModule.UID].XSIZE) * this.camera.Zoom);
						x = r.Height;
						y = r.Width;
						r.Width = x;
						r.Height = y;
						Rectangle? nullable9 = null;
						base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict[Ship_Game.ResourceManager.ShipModulesDict[this.ActiveModule.UID].IconTexturePath], r, nullable9, Color.White, -1.57079637f, Vector2.Zero, SpriteEffects.None, 1f);
						break;
					}
					case ShipDesignScreen.ActiveModuleState.Right:
					{
						r.X = r.X + (int)((float)(16 * Ship_Game.ResourceManager.ShipModulesDict[this.ActiveModule.UID].YSIZE) * this.camera.Zoom);
						x = r.Height;
						y = r.Width;
						r.Width = x;
						r.Height = y;
						Rectangle? nullable10 = null;
						base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict[Ship_Game.ResourceManager.ShipModulesDict[this.ActiveModule.UID].IconTexturePath], r, nullable10, Color.White, 1.57079637f, Vector2.Zero, SpriteEffects.None, 1f);
						break;
					}
					case ShipDesignScreen.ActiveModuleState.Rear:
					{
						Rectangle? nullable11 = null;
						base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict[Ship_Game.ResourceManager.ShipModulesDict[this.ActiveModule.UID].IconTexturePath], r, nullable11, Color.White, 0f, Vector2.Zero, SpriteEffects.FlipVertically, 1f);
						break;
					}
				}
				if (this.ActiveModule.shield_power_max > 0f)
				{
					Vector2 center = new Vector2((float)this.mouseStateCurrent.X, (float)this.mouseStateCurrent.Y) + new Vector2((float)(Ship_Game.ResourceManager.ShipModulesDict[this.ActiveModule.UID].XSIZE * 16 / 2), (float)(Ship_Game.ResourceManager.ShipModulesDict[this.ActiveModule.UID].YSIZE * 16 / 2));
					Primitives2D.DrawCircle(base.ScreenManager.SpriteBatch, center, this.ActiveModule.shield_radius * this.camera.Zoom, 50, Color.LightGreen);
				}
			}
			this.DrawUI(gameTime);
			if (this.selector != null)
			{
				this.selector.Draw();
			}
			this.ArcsButton.DrawWithShadowCaps(base.ScreenManager);
			if (this.Debug)
			{
				Vector2 Pos = new Vector2((float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2) - Fonts.Arial20Bold.MeasureString("Debug").X, 120f);
				HelperFunctions.DrawDropShadowText(base.ScreenManager, "Debug", Pos, Fonts.Arial20Bold);
				Pos = new Vector2((float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2) - Fonts.Arial20Bold.MeasureString(this.operation.ToString()).X, 140f);
				HelperFunctions.DrawDropShadowText(base.ScreenManager, this.operation.ToString(), Pos, Fonts.Arial20Bold);
			}
			this.close.Draw(base.ScreenManager);
			base.ScreenManager.SpriteBatch.End();
			lock (GlobalStats.ObjectManagerLocker)
			{
				base.ScreenManager.inter.EndFrameRendering();
				base.ScreenManager.editor.EndFrameRendering();
				base.ScreenManager.sceneState.EndFrameRendering();
			}
		}

		private void DrawActiveModuleData()
		{
			float powerDraw;
			this.activeModSubMenu.Draw();
			Rectangle r = this.activeModSubMenu.Menu;
			r.Y = r.Y + 25;
			r.Height = r.Height - 25;
			Selector sel = new Selector(base.ScreenManager, r, new Color(0, 0, 0, 210));
			sel.Draw();
			ShipModule mod = this.ActiveModule;
			if (this.ActiveModule == null && this.HighlightedModule != null)
			{
				mod = this.HighlightedModule;
			}
			else if (this.ActiveModule != null)
			{
				mod = this.ActiveModule;
			}
			if (mod != null)
			{
				mod.HealthMax = Ship_Game.ResourceManager.ShipModulesDict[mod.UID].HealthMax;
			}
			if (this.activeModSubMenu.Tabs[0].Selected && mod != null)
			{
				Vector2 modTitlePos = new Vector2((float)(this.activeModSubMenu.Menu.X + 10), (float)(this.activeModSubMenu.Menu.Y + 35));
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, Localizer.Token(Ship_Game.ResourceManager.ShipModulesDict[mod.UID].NameIndex), modTitlePos, Color.White);
				modTitlePos.Y = modTitlePos.Y + (float)(Fonts.Arial20Bold.LineSpacing + 3);
				string rest = "";
				if (Ship_Game.ResourceManager.ShipModulesDict[mod.UID].Restrictions == Restrictions.IO)
				{
					rest = "I or O or IO";
				}
				else if (Ship_Game.ResourceManager.ShipModulesDict[mod.UID].Restrictions == Restrictions.I)
				{
					rest = "I or IO only";
				}
				else if (Ship_Game.ResourceManager.ShipModulesDict[mod.UID].Restrictions == Restrictions.O)
				{
					rest = "O or IO only";
				}
				else if (Ship_Game.ResourceManager.ShipModulesDict[mod.UID].Restrictions == Restrictions.E)
				{
					rest = "E only";
				}
				else if (Ship_Game.ResourceManager.ShipModulesDict[mod.UID].Restrictions == Restrictions.IOE)
				{
					rest = "I, O, or E";
				}
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(122), ": ", rest), modTitlePos, Color.Orange);
				modTitlePos.Y = modTitlePos.Y + (float)(Fonts.Arial12Bold.LineSpacing + 3);
				int startx = (int)modTitlePos.X;
				string tag = "";
				if (Ship_Game.ResourceManager.ShipModulesDict[mod.UID].IsWeapon && Ship_Game.ResourceManager.ShipModulesDict[mod.UID].BombType == null)
				{
					if (Ship_Game.ResourceManager.WeaponsDict[Ship_Game.ResourceManager.ShipModulesDict[mod.UID].WeaponType].Tag_Guided)
					{
						tag = string.Concat(tag, "GUIDED ");
						base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, tag, modTitlePos, Color.Orange);
						modTitlePos.X = modTitlePos.X + Fonts.Arial8Bold.MeasureString(tag).X;
						tag = "";
					}
					if (Ship_Game.ResourceManager.WeaponsDict[Ship_Game.ResourceManager.ShipModulesDict[mod.UID].WeaponType].Tag_Intercept)
					{
						tag = string.Concat(tag, "INTERCEPTABLE ");
						base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, tag, modTitlePos, Color.Orange);
						modTitlePos.X = modTitlePos.X + Fonts.Arial8Bold.MeasureString(tag).X;
						tag = "";
					}
					if (Ship_Game.ResourceManager.WeaponsDict[Ship_Game.ResourceManager.ShipModulesDict[mod.UID].WeaponType].Tag_Energy)
					{
						tag = string.Concat(tag, "ENERGY ");
						base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, tag, modTitlePos, Color.Orange);
						modTitlePos.X = modTitlePos.X + Fonts.Arial8Bold.MeasureString(tag).X;
						tag = "";
					}
					if (Ship_Game.ResourceManager.WeaponsDict[Ship_Game.ResourceManager.ShipModulesDict[mod.UID].WeaponType].Tag_Hybrid)
					{
						tag = string.Concat(tag, "HYBRID ");
						base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, tag, modTitlePos, Color.Orange);
						modTitlePos.X = modTitlePos.X + Fonts.Arial8Bold.MeasureString(tag).X;
						tag = "";
					}
					if (Ship_Game.ResourceManager.WeaponsDict[Ship_Game.ResourceManager.ShipModulesDict[mod.UID].WeaponType].Tag_Kinetic)
					{
						tag = string.Concat(tag, "KINETIC ");
						base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, tag, modTitlePos, Color.Orange);
						modTitlePos.X = modTitlePos.X + Fonts.Arial8Bold.MeasureString(tag).X;
						tag = "";
					}
					if (Ship_Game.ResourceManager.WeaponsDict[Ship_Game.ResourceManager.ShipModulesDict[mod.UID].WeaponType].Tag_Explosive)
					{
						tag = string.Concat(tag, "EXPLOSIVE ");
						base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, tag, modTitlePos, Color.Orange);
						modTitlePos.X = modTitlePos.X + Fonts.Arial8Bold.MeasureString(tag).X;
						tag = "";
					}
					if (Ship_Game.ResourceManager.WeaponsDict[Ship_Game.ResourceManager.ShipModulesDict[mod.UID].WeaponType].Tag_Subspace)
					{
						tag = string.Concat(tag, "SUBSPACE ");
						base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, tag, modTitlePos, Color.Orange);
						modTitlePos.X = modTitlePos.X + Fonts.Arial8Bold.MeasureString(tag).X;
						tag = "";
					}
					if (Ship_Game.ResourceManager.WeaponsDict[Ship_Game.ResourceManager.ShipModulesDict[mod.UID].WeaponType].Tag_Warp)
					{
						tag = string.Concat(tag, "WARP ");
						base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, tag, modTitlePos, Color.Orange);
						modTitlePos.X = modTitlePos.X + Fonts.Arial8Bold.MeasureString(tag).X;
						tag = "";
					}
					if (Ship_Game.ResourceManager.WeaponsDict[Ship_Game.ResourceManager.ShipModulesDict[mod.UID].WeaponType].Tag_PD)
					{
						tag = string.Concat(tag, "POINT DEFENSE ");
						base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, tag, modTitlePos, Color.Orange);
						modTitlePos.X = modTitlePos.X + Fonts.Arial8Bold.MeasureString(tag).X;
						tag = "";
					}
					if (Ship_Game.ResourceManager.WeaponsDict[Ship_Game.ResourceManager.ShipModulesDict[mod.UID].WeaponType].Tag_Missile)
					{
						tag = string.Concat(tag, "MISSILE ");
						base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, tag, modTitlePos, Color.Orange);
						modTitlePos.X = modTitlePos.X + Fonts.Arial8Bold.MeasureString(tag).X;
						tag = "";
					}
					if (Ship_Game.ResourceManager.WeaponsDict[Ship_Game.ResourceManager.ShipModulesDict[mod.UID].WeaponType].Tag_Beam)
					{
						tag = string.Concat(tag, "BEAM ");
						base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, tag, modTitlePos, Color.Orange);
						modTitlePos.X = modTitlePos.X + Fonts.Arial8Bold.MeasureString(tag).X;
						tag = "";
					}
					if (Ship_Game.ResourceManager.WeaponsDict[Ship_Game.ResourceManager.ShipModulesDict[mod.UID].WeaponType].Tag_Railgun)
					{
						tag = string.Concat(tag, "RAILGUN ");
						base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, tag, modTitlePos, Color.Orange);
						modTitlePos.X = modTitlePos.X + Fonts.Arial8Bold.MeasureString(tag).X;
						tag = "";
					}
					if (Ship_Game.ResourceManager.WeaponsDict[Ship_Game.ResourceManager.ShipModulesDict[mod.UID].WeaponType].Tag_Torpedo)
					{
						tag = string.Concat(tag, "TORPEDO ");
						base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, tag, modTitlePos, Color.Orange);
						modTitlePos.X = modTitlePos.X + Fonts.Arial8Bold.MeasureString(tag).X;
						tag = "";
					}
					if (Ship_Game.ResourceManager.WeaponsDict[Ship_Game.ResourceManager.ShipModulesDict[mod.UID].WeaponType].Tag_Bomb)
					{
						tag = string.Concat(tag, "BOMB ");
						base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, tag, modTitlePos, Color.Orange);
						modTitlePos.X = modTitlePos.X + Fonts.Arial8Bold.MeasureString(tag).X;
						tag = "";
					}
					if (Ship_Game.ResourceManager.WeaponsDict[Ship_Game.ResourceManager.ShipModulesDict[mod.UID].WeaponType].Tag_BioWeapon)
					{
						tag = string.Concat(tag, "BIOWEAPON ");
						base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, tag, modTitlePos, Color.Orange);
						modTitlePos.X = modTitlePos.X + Fonts.Arial8Bold.MeasureString(tag).X;
						tag = "";
					}
					if (Ship_Game.ResourceManager.WeaponsDict[Ship_Game.ResourceManager.ShipModulesDict[mod.UID].WeaponType].Tag_SpaceBomb)
					{
						tag = string.Concat(tag, "SPACEBOMB ");
						base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, tag, modTitlePos, Color.Orange);
						modTitlePos.X = modTitlePos.X + Fonts.Arial8Bold.MeasureString(tag).X;
						tag = "";
					}
					if (Ship_Game.ResourceManager.WeaponsDict[Ship_Game.ResourceManager.ShipModulesDict[mod.UID].WeaponType].Tag_Drone)
					{
						tag = string.Concat(tag, "DRONE ");
						base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, tag, modTitlePos, Color.Orange);
						modTitlePos.X = modTitlePos.X + Fonts.Arial8Bold.MeasureString(tag).X;
						tag = "";
					}
					if (Ship_Game.ResourceManager.WeaponsDict[Ship_Game.ResourceManager.ShipModulesDict[mod.UID].WeaponType].Tag_Cannon)
					{
						tag = string.Concat(tag, "CANNON ");
						base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, tag, modTitlePos, Color.Orange);
						modTitlePos.X = modTitlePos.X + Fonts.Arial8Bold.MeasureString(tag).X;
						tag = "";
					}
					modTitlePos.Y = modTitlePos.Y + (float)(Fonts.Arial8Bold.LineSpacing + 2);
					modTitlePos.X = (float)startx;
				}
				else if (Ship_Game.ResourceManager.ShipModulesDict[mod.UID].IsWeapon)
				{
					string bombType = Ship_Game.ResourceManager.ShipModulesDict[mod.UID].BombType;
				}
				string txt = this.parseText(Localizer.Token(Ship_Game.ResourceManager.ShipModulesDict[mod.UID].DescriptionIndex), (float)(this.activeModSubMenu.Menu.Width - 20), Fonts.Arial12);
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, txt, modTitlePos, Color.White);
				modTitlePos.Y = modTitlePos.Y + (Fonts.Arial12Bold.MeasureString(txt).Y + 2f);
				float starty = modTitlePos.Y;
				if (!mod.isWeapon || mod.InstalledWeapon == null)
				{
					if (mod.ModuleType == ShipModuleType.Engine)
					{
						this.DrawStat(ref modTitlePos, Localizer.Token(123), (float)EmpireManager.GetEmpireByName(ShipDesignScreen.screen.PlayerLoyalty).data.MassModifier * mod.Mass, 79);
						modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
						this.DrawStat(ref modTitlePos, Localizer.Token(124), (float)mod.HealthMax + mod.HealthMax * (float)EmpireManager.GetEmpireByName(ShipDesignScreen.screen.PlayerLoyalty).data.Traits.ModHpModifier, 80);
						modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
						this.DrawStat(ref modTitlePos, Localizer.Token(125), (mod.ModuleType != ShipModuleType.PowerPlant ? -(float)mod.PowerDraw : mod.PowerFlowMax), 81);
						modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
						if (GlobalStats.HardcoreRuleset)
						{
							this.DrawStat(ref modTitlePos, "(Afterburner)", (float)(-mod.PowerDrawWithAfterburner), "Power draw persecond of engine when afterburner engaged");
						}
						modTitlePos.X = modTitlePos.X + 152f;
						modTitlePos.Y = starty;
						this.DrawStat(ref modTitlePos, Localizer.Token(128), (float)mod.Cost * UniverseScreen.GamePaceStatic, 84);
						modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
						this.DrawStat(ref modTitlePos, Localizer.Token(131), (float)mod.thrust, 91);
						modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
						if (!GlobalStats.HardcoreRuleset)
						{
							this.DrawStat(ref modTitlePos, Localizer.Token(2064), (float)mod.WarpThrust, 92);
						}
						else
						{
							this.DrawStat(ref modTitlePos, "Afterburner", (float)mod.AfterburnerThrust, "Indicates the thrust of this module when the afterburner is engaged");
						}
						modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
						this.DrawStat(ref modTitlePos, Localizer.Token(2260), (float)mod.TurnThrust, 148);
						modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
						return;
					}
					if (mod.ModuleType == ShipModuleType.Shield)
					{
						this.DrawStat(ref modTitlePos, Localizer.Token(123), (float)EmpireManager.GetEmpireByName(ShipDesignScreen.screen.PlayerLoyalty).data.MassModifier * mod.Mass, 79);
						modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
						this.DrawStat(ref modTitlePos, Localizer.Token(124), (float)mod.HealthMax + mod.HealthMax * (float)EmpireManager.GetEmpireByName(ShipDesignScreen.screen.PlayerLoyalty).data.Traits.ModHpModifier, 80);
						modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
						this.DrawStat(ref modTitlePos, Localizer.Token(125), (mod.ModuleType != ShipModuleType.PowerPlant ? -(float)mod.PowerDraw : mod.PowerFlowMax), 81);
						modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
						modTitlePos.X = modTitlePos.X + 152f;
						modTitlePos.Y = starty;
						this.DrawStat(ref modTitlePos, Localizer.Token(128), (float)mod.Cost * UniverseScreen.GamePaceStatic, 84);
						modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
						this.DrawStat(ref modTitlePos, Localizer.Token(132), (float)mod.shield_power_max, 93);
						modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
						this.DrawStat(ref modTitlePos, Localizer.Token(133), (float)mod.shield_radius, 94);
						modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
						this.DrawStat(ref modTitlePos, Localizer.Token(134), (float)mod.shield_recharge_rate, 95);
						modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
						return;
					}
					if (mod.ModuleType == ShipModuleType.Sensors)
					{
						this.DrawStat(ref modTitlePos, Localizer.Token(123), (float)EmpireManager.GetEmpireByName(ShipDesignScreen.screen.PlayerLoyalty).data.MassModifier * mod.Mass, 79);
						modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
						this.DrawStat(ref modTitlePos, Localizer.Token(124), (float)mod.HealthMax + mod.HealthMax * (float)EmpireManager.GetEmpireByName(ShipDesignScreen.screen.PlayerLoyalty).data.Traits.ModHpModifier, 80);
						modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
						this.DrawStat(ref modTitlePos, Localizer.Token(125), (mod.ModuleType != ShipModuleType.PowerPlant ? -(float)mod.PowerDraw : mod.PowerFlowMax), 81);
						modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
						modTitlePos.X = modTitlePos.X + 152f;
						modTitlePos.Y = starty;
						this.DrawStat(ref modTitlePos, Localizer.Token(128), (float)mod.Cost * UniverseScreen.GamePaceStatic, 84);
						modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
						this.DrawStat(ref modTitlePos, Localizer.Token(126), (float)mod.SensorRange, 96);
						modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
						return;
					}
					if (mod.ModuleType == ShipModuleType.Command)
					{
						this.DrawStat(ref modTitlePos, Localizer.Token(123), (float)EmpireManager.GetEmpireByName(ShipDesignScreen.screen.PlayerLoyalty).data.MassModifier * mod.Mass, 79);
						modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
						this.DrawStat(ref modTitlePos, Localizer.Token(124), (float)mod.HealthMax + mod.HealthMax * (float)EmpireManager.GetEmpireByName(ShipDesignScreen.screen.PlayerLoyalty).data.Traits.ModHpModifier, 80);
						modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
						this.DrawStat(ref modTitlePos, Localizer.Token(125), (mod.ModuleType != ShipModuleType.PowerPlant ? -(float)mod.PowerDraw : mod.PowerFlowMax), 81);
						modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
						this.DrawStat(ref modTitlePos, Localizer.Token(2231), (float)mod.MechanicalBoardingDefense, 143);
						modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
						modTitlePos.X = modTitlePos.X + 152f;
						modTitlePos.Y = starty;
						this.DrawStat(ref modTitlePos, Localizer.Token(128), (float)mod.Cost * UniverseScreen.GamePaceStatic, 84);
						modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
						this.DrawStat(ref modTitlePos, Localizer.Token(126), (float)mod.SensorRange, 96);
						modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
						this.DrawStat(ref modTitlePos, Localizer.Token(135), (float)mod.BonusRepairRate, 97);
						modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
						return;
					}
					if (mod.ModuleType != ShipModuleType.Hangar)
					{
						this.DrawStat(ref modTitlePos, Localizer.Token(123), (float)EmpireManager.GetEmpireByName(ShipDesignScreen.screen.PlayerLoyalty).data.MassModifier * mod.Mass, 79);
						modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
						this.DrawStat(ref modTitlePos, Localizer.Token(124), (float)mod.HealthMax + mod.HealthMax * (float)EmpireManager.GetEmpireByName(ShipDesignScreen.screen.PlayerLoyalty).data.Traits.ModHpModifier, 80);
						modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
						string str = Localizer.Token(125);
						if (mod.ModuleType != ShipModuleType.PowerPlant)
						{
							powerDraw = -(float)mod.PowerDraw;
						}
						else
						{
							powerDraw = (mod.PowerDraw > 0f ? (float)(-mod.PowerDraw) : mod.PowerFlowMax);
						}
						this.DrawStat(ref modTitlePos, str, powerDraw, 81);
						modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
						if (GlobalStats.HardcoreRuleset && mod.PowerDrawAtWarp != 0f)
						{
							this.DrawStat(ref modTitlePos, "(Warp)", (float)(-mod.PowerDrawAtWarp), "Power draw persecond of engine when Warp engaged");
							modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
						}
						if (mod.BonusRepairRate > 0f)
						{
							this.DrawStat(ref modTitlePos, string.Concat(Localizer.Token(135), "+"), (float)mod.BonusRepairRate, 97);
							modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
						}
						modTitlePos.X = modTitlePos.X + 152f;
						modTitlePos.Y = starty;
						this.DrawStat(ref modTitlePos, Localizer.Token(128), (float)mod.Cost * UniverseScreen.GamePaceStatic, 84);
						modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
						if (mod.OrdinanceCapacity > 0)
						{
							this.DrawStat(ref modTitlePos, Localizer.Token(2129), (float)mod.OrdinanceCapacity, 124);
							modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
						}
						if (mod.InhibitionRadius > 0f)
						{
							this.DrawStat(ref modTitlePos, Localizer.Token(2233), (float)mod.InhibitionRadius, 144);
							modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
						}
						if (mod.PowerStoreMax > 0f)
						{
							this.DrawStat(ref modTitlePos, Localizer.Token(2235), (float)mod.PowerStoreMax, 145);
							modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
						}
					}
					else
					{
						this.DrawStat(ref modTitlePos, Localizer.Token(123), (float)EmpireManager.GetEmpireByName(ShipDesignScreen.screen.PlayerLoyalty).data.MassModifier * mod.Mass, 79);
						modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
						this.DrawStat(ref modTitlePos, Localizer.Token(124), (float)mod.HealthMax + mod.HealthMax * (float)EmpireManager.GetEmpireByName(ShipDesignScreen.screen.PlayerLoyalty).data.Traits.ModHpModifier, 80);
						modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
						this.DrawStat(ref modTitlePos, Localizer.Token(125), (mod.ModuleType != ShipModuleType.PowerPlant ? -(float)mod.PowerDraw : mod.PowerFlowMax), 81);
						modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
						modTitlePos.X = modTitlePos.X + 152f;
						modTitlePos.Y = starty;
						this.DrawStat(ref modTitlePos, Localizer.Token(128), (float)mod.Cost * UniverseScreen.GamePaceStatic, 84);
						modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
						this.DrawStat(ref modTitlePos, Localizer.Token(136), (float)mod.hangarTimerConstant, 98);
						modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
						modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
						if (!mod.IsTroopBay && !mod.IsSupplyBay)
						{
							Vector2 shipSelectionPos = new Vector2(modTitlePos.X - 152f, modTitlePos.Y);
							base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, string.Concat(Localizer.Token(137), " : ", mod.hangarShipUID), shipSelectionPos, Color.Orange);
							r = this.ChooseFighterSub.Menu;
							r.Y = r.Y + 25;
							r.Height = r.Height - 25;
							sel = new Selector(base.ScreenManager, r, new Color(0, 0, 0, 210));
							sel.Draw();
							this.ChooseFighterSub.Draw();
							this.ChooseFighterSL.Draw(base.ScreenManager.SpriteBatch);
							Vector2 bCursor = new Vector2((float)(this.ChooseFighterSub.Menu.X + 15), (float)(this.ChooseFighterSub.Menu.Y + 25));
							for (int i = this.ChooseFighterSL.indexAtTop; i < this.ChooseFighterSL.Entries.Count && i < this.ChooseFighterSL.indexAtTop + this.ChooseFighterSL.entriesToDisplay; i++)
							{
								ScrollList.Entry e = this.ChooseFighterSL.Entries[i];
								bCursor.Y = (float)e.clickRect.Y;
								base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict[Ship_Game.ResourceManager.HullsDict[(e.item as Ship).GetShipData().Hull].IconPath], new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
								Vector2 tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
								base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, ((e.item as Ship).VanityName != "" ? (e.item as Ship).VanityName : (e.item as Ship).Name), tCursor, Color.White);
								tCursor.Y = tCursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
							}
							if (this.selector != null)
							{
								this.selector.Draw();
								return;
							}
						}
					}
				}
				else
				{
					this.DrawStat(ref modTitlePos, Localizer.Token(123), (float)EmpireManager.GetEmpireByName(ShipDesignScreen.screen.PlayerLoyalty).data.MassModifier * mod.Mass, 79);
					modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
					this.DrawStat(ref modTitlePos, Localizer.Token(124), (float)mod.HealthMax + EmpireManager.GetEmpireByName(ShipDesignScreen.screen.PlayerLoyalty).data.Traits.ModHpModifier * mod.HealthMax, 80);
					modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
					this.DrawStat(ref modTitlePos, Localizer.Token(125), (mod.ModuleType != ShipModuleType.PowerPlant ? -(float)mod.PowerDraw : mod.PowerFlowMax), 81);
					modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
					this.DrawStat(ref modTitlePos, Localizer.Token(126), (float)mod.InstalledWeapon.Range, 82);
					modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
					if (mod.InstalledWeapon.WeaponType != "Energy Beam")
					{
						if (!mod.InstalledWeapon.explodes || mod.InstalledWeapon.OrdinanceRequiredToFire <= 0f)
						{
							this.DrawStat(ref modTitlePos, Localizer.Token(127), (float)mod.InstalledWeapon.DamageAmount, 83);
							modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
						}
						else
						{
							this.DrawStat(ref modTitlePos, Localizer.Token(127), (float)mod.InstalledWeapon.DamageAmount + EmpireManager.GetEmpireByName(ShipDesignScreen.screen.PlayerLoyalty).data.OrdnanceEffectivenessBonus * mod.InstalledWeapon.DamageAmount, 83);
							modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
						}
					}
					modTitlePos.X = modTitlePos.X + 152f;
					modTitlePos.Y = starty;
					this.DrawStat(ref modTitlePos, Localizer.Token(128), (float)mod.Cost * UniverseScreen.GamePaceStatic, 84);
					modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
					if (mod.InstalledWeapon.WeaponType != "Energy Beam")
					{
						this.DrawStat(ref modTitlePos, Localizer.Token(129), (float)mod.InstalledWeapon.ProjectileSpeed, 85);
						modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
					}
					if (mod.InstalledWeapon.DamageAmount > 0f)
					{
						if (mod.InstalledWeapon.WeaponType == "Energy Beam")
						{
							float dps = (float)mod.InstalledWeapon.DamageAmount * 60f * 3f / mod.InstalledWeapon.fireDelay;
							this.DrawStat(ref modTitlePos, "DPS", dps, 86);
							modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
							this.DrawStat(ref modTitlePos, "Pwr / Sec", (float)mod.InstalledWeapon.BeamPowerCostPerSecond, 87);
							modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
						}
						else if (mod.InstalledWeapon.explodes && mod.InstalledWeapon.OrdinanceRequiredToFire > 0f)
						{
							if (mod.InstalledWeapon.SalvoCount <= 1)
							{
								float dps = 1f / mod.InstalledWeapon.fireDelay * ((float)mod.InstalledWeapon.DamageAmount + EmpireManager.GetEmpireByName(ShipDesignScreen.screen.PlayerLoyalty).data.OrdnanceEffectivenessBonus * mod.InstalledWeapon.DamageAmount);
								dps = dps * (float)mod.InstalledWeapon.ProjectileCount;
								this.DrawStat(ref modTitlePos, "DPS", dps, 86);
								modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
							}
							else
							{
								float dps = (float)mod.InstalledWeapon.SalvoCount / mod.InstalledWeapon.fireDelay * ((float)mod.InstalledWeapon.DamageAmount + EmpireManager.GetEmpireByName(ShipDesignScreen.screen.PlayerLoyalty).data.OrdnanceEffectivenessBonus * mod.InstalledWeapon.DamageAmount);
								dps = dps * (float)mod.InstalledWeapon.ProjectileCount;
								this.DrawStat(ref modTitlePos, "DPS", dps, 86);
								modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
							}
						}
						else if (mod.InstalledWeapon.SalvoCount <= 1)
						{
							float dps = 1f / mod.InstalledWeapon.fireDelay * ((float)mod.InstalledWeapon.DamageAmount + (float)mod.InstalledWeapon.DamageAmount * EmpireManager.GetEmpireByName(ShipDesignScreen.screen.PlayerLoyalty).data.Traits.EnergyDamageMod);
							dps = dps * (float)mod.InstalledWeapon.ProjectileCount;
							this.DrawStat(ref modTitlePos, "DPS", dps, 86);
							modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
						}
						else
						{
							float dps = (float)mod.InstalledWeapon.SalvoCount / mod.InstalledWeapon.fireDelay * ((float)mod.InstalledWeapon.DamageAmount + (float)mod.InstalledWeapon.DamageAmount * EmpireManager.GetEmpireByName(ShipDesignScreen.screen.PlayerLoyalty).data.Traits.EnergyDamageMod);
							dps = dps * (float)mod.InstalledWeapon.ProjectileCount;
							this.DrawStat(ref modTitlePos, "DPS", dps, 86);
							modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
						}
					}
					if (mod.InstalledWeapon.EMPDamage > 0f)
					{
						this.DrawStat(ref modTitlePos, "EMP", 1f / mod.InstalledWeapon.fireDelay * (float)mod.InstalledWeapon.EMPDamage, 110);
						modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
					}
					this.DrawStat(ref modTitlePos, Localizer.Token(130), (float)mod.FieldOfFire, 88);
					modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
					if (mod.InstalledWeapon.OrdinanceRequiredToFire > 0f)
					{
						this.DrawStat(ref modTitlePos, "Ord / Shot", (float)mod.InstalledWeapon.OrdinanceRequiredToFire, 89);
						modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
					}
					if (mod.InstalledWeapon.PowerRequiredToFire > 0f)
					{
						this.DrawStat(ref modTitlePos, "Pwr / Shot", (float)mod.InstalledWeapon.PowerRequiredToFire, 90);
						modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
					}
					if (mod.InstalledWeapon.EffectVsArmor != 1f)
					{
						if (mod.InstalledWeapon.EffectVsArmor <= 1f)
						{
							float effectVsArmor = mod.InstalledWeapon.EffectVsArmor * 100f;
							this.DrawStat105Bad(ref modTitlePos, "VS Armor", string.Concat(effectVsArmor.ToString("#"), "%"), 147);
						}
						else
						{
							float single = mod.InstalledWeapon.EffectVsArmor * 100f;
							this.DrawStat105(ref modTitlePos, "VS Armor", string.Concat(single.ToString("#"), "%"), 147);
						}
						modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
					}
					if (mod.InstalledWeapon.EffectVSShields != 1f)
					{
						if (mod.InstalledWeapon.EffectVSShields <= 1f)
						{
							float effectVSShields = mod.InstalledWeapon.EffectVSShields * 100f;
							this.DrawStat105Bad(ref modTitlePos, "VS Shield", string.Concat(effectVSShields.ToString("#"), "%"), 147);
						}
						else
						{
							float effectVSShields1 = mod.InstalledWeapon.EffectVSShields * 100f;
							this.DrawStat105(ref modTitlePos, "VS Shield", string.Concat(effectVSShields1.ToString("#"), "%"), 147);
						}
						modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
						return;
					}
				}
			}
		}

		private void DrawHullSelection()
		{
			Rectangle r = this.hullSelectionSub.Menu;
			r.Y = r.Y + 25;
			r.Height = r.Height - 25;
			Selector sel = new Selector(base.ScreenManager, r, new Color(0, 0, 0, 210));
			sel.Draw();
			this.hullSL.Draw(base.ScreenManager.SpriteBatch);
			float x = (float)Mouse.GetState().X;
			MouseState state = Mouse.GetState();
			Vector2 MousePos = new Vector2(x, (float)state.Y);
			this.hullSelectionSub.Draw();
			Vector2 bCursor = new Vector2((float)(this.hullSelectionSub.Menu.X + 10), (float)(this.hullSelectionSub.Menu.Y + 45));
			for (int i = this.hullSL.indexAtTop; i < this.hullSL.Copied.Count && i < this.hullSL.indexAtTop + this.hullSL.entriesToDisplay; i++)
			{
				bCursor = new Vector2((float)(this.hullSelectionSub.Menu.X + 10), (float)(this.hullSelectionSub.Menu.Y + 45));
				ScrollList.Entry e = this.hullSL.Copied[i];
				bCursor.Y = (float)e.clickRect.Y;
				if (e.item is ModuleHeader)
				{
					(e.item as ModuleHeader).Draw(base.ScreenManager, bCursor);
				}
				else if (e.item is ShipData)
				{
					bCursor.X = bCursor.X + 10f;
					base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict[(e.item as ShipData).IconPath], new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
					Vector2 tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
					base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, (e.item as ShipData).Name, tCursor, Color.White);
					tCursor.Y = tCursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
					base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, (e.item as ShipData).Role, tCursor, Color.Orange);
					if (HelperFunctions.CheckIntersection(e.clickRect, MousePos))
					{
						if (e.clickRectHover == 0)
						{
							AudioManager.PlayCue("sd_ui_mouseover");
						}
						e.clickRectHover = 1;
					}
				}
			}
		}

		private void DrawList()
		{
			float h;
			float x = (float)Mouse.GetState().X;
			MouseState state = Mouse.GetState();
			Vector2 MousePos = new Vector2(x, (float)state.Y);
			Vector2 bCursor = new Vector2((float)(this.modSel.Menu.X + 10), (float)(this.modSel.Menu.Y + 45));
			for (int i = this.weaponSL.indexAtTop; i < this.weaponSL.Copied.Count && i < this.weaponSL.indexAtTop + this.weaponSL.entriesToDisplay; i++)
			{
				bCursor = new Vector2((float)(this.modSel.Menu.X + 10), (float)(this.modSel.Menu.Y + 45));
				ScrollList.Entry e = this.weaponSL.Copied[i];
				bCursor.Y = (float)e.clickRect.Y;
				if (e.item is ModuleHeader)
				{
					(e.item as ModuleHeader).Draw(base.ScreenManager, bCursor);
				}
				else if (e.item is ShipModule)
				{
					bCursor.X = bCursor.X + 10f;
					Rectangle modRect = new Rectangle((int)bCursor.X, (int)bCursor.Y, Ship_Game.ResourceManager.TextureDict[Ship_Game.ResourceManager.ShipModulesDict[(e.item as ShipModule).UID].IconTexturePath].Width, Ship_Game.ResourceManager.TextureDict[Ship_Game.ResourceManager.ShipModulesDict[(e.item as ShipModule).UID].IconTexturePath].Height);
					Vector2 vector2 = new Vector2(bCursor.X + 15f, bCursor.Y + 15f);
					Vector2 vector21 = new Vector2((float)(Ship_Game.ResourceManager.TextureDict[Ship_Game.ResourceManager.ShipModulesDict[(e.item as ShipModule).UID].IconTexturePath].Width / 2), (float)(Ship_Game.ResourceManager.TextureDict[Ship_Game.ResourceManager.ShipModulesDict[(e.item as ShipModule).UID].IconTexturePath].Height / 2));
					float aspectRatio = (float)Ship_Game.ResourceManager.TextureDict[Ship_Game.ResourceManager.ShipModulesDict[(e.item as ShipModule).UID].IconTexturePath].Width / (float)Ship_Game.ResourceManager.TextureDict[Ship_Game.ResourceManager.ShipModulesDict[(e.item as ShipModule).UID].IconTexturePath].Height;
					float w = (float)modRect.Width;
					for (h = (float)modRect.Height; w > 30f || h > 30f; h = h - 1.6f)
					{
						w = w - aspectRatio * 1.6f;
					}
					modRect.Width = (int)w;
					modRect.Height = (int)h;
					base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict[Ship_Game.ResourceManager.ShipModulesDict[(e.item as ShipModule).UID].IconTexturePath], modRect, Color.White);
					Vector2 tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
					base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Localizer.Token((e.item as ShipModule).NameIndex), tCursor, Color.White);
					tCursor.Y = tCursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
					base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, Ship_Game.ResourceManager.ShipModulesDict[(e.item as ShipModule).UID].Restrictions.ToString(), tCursor, Color.Orange);
					tCursor.X = tCursor.X + Fonts.Arial8Bold.MeasureString(Ship_Game.ResourceManager.ShipModulesDict[(e.item as ShipModule).UID].Restrictions.ToString()).X;
					if (Ship_Game.ResourceManager.ShipModulesDict[(e.item as ShipModule).UID].ModuleType == ShipModuleType.MainGun || Ship_Game.ResourceManager.ShipModulesDict[(e.item as ShipModule).UID].ModuleType == ShipModuleType.MissileLauncher || Ship_Game.ResourceManager.ShipModulesDict[(e.item as ShipModule).UID].CanRotate)
					{
						Rectangle rotateRect = new Rectangle((int)bCursor.X + 220, (int)bCursor.Y - 3, 27, 32);
						base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["UI/icon_can_rotate"], rotateRect, Color.White);
						if (HelperFunctions.CheckIntersection(rotateRect, MousePos))
						{
							ToolTip.CreateTooltip("Indicates that this module can be rotated using the arrow keys", base.ScreenManager);
						}
					}
					if (HelperFunctions.CheckIntersection(e.clickRect, MousePos))
					{
						if (e.clickRectHover == 0)
						{
							AudioManager.PlayCue("sd_ui_mouseover");
						}
						e.clickRectHover = 1;
					}
				}
			}
		}

		private void DrawModuleSelection()
		{
			Rectangle r = this.modSel.Menu;
			r.Y = r.Y + 25;
			r.Height = r.Height - 25;
			Selector sel = new Selector(base.ScreenManager, r, new Color(0, 0, 0, 210));
			sel.Draw();
			this.modSel.Draw();
			this.weaponSL.Draw(base.ScreenManager.SpriteBatch);
			float x = (float)Mouse.GetState().X;
			MouseState state = Mouse.GetState();
			Vector2 vector2 = new Vector2(x, (float)state.Y);
			if (this.modSel.Tabs[0].Selected)
			{
				if (this.Reset)
				{
					this.weaponSL.Entries.Clear();
					List<string> WeaponCategories = new List<string>();
					foreach (KeyValuePair<string, ShipModule> module in Ship_Game.ResourceManager.ShipModulesDict)
					{
						if (!EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).GetMDict()[module.Key] || module.Value.UID == "Dummy")
						{
							continue;
						}
						module.Value.ModuleType.ToString();
						ShipModule tmp = Ship_Game.ResourceManager.GetModule(module.Key);
						tmp.SetAttributesNoParent();
						if (this.ActiveHull.Role != "fighter" && this.ActiveHull.Role != "scout" && this.ActiveHull.Role != "corvette" && tmp.FightersOnly)
						{
							continue;
						}
						if (tmp.isWeapon)
						{
							if (!WeaponCategories.Contains(tmp.InstalledWeapon.WeaponType))
							{
								WeaponCategories.Add(tmp.InstalledWeapon.WeaponType);
								ModuleHeader type = new ModuleHeader(tmp.InstalledWeapon.WeaponType, 240f);
								this.weaponSL.AddItem(type);
							}
						}
						else if (tmp.ModuleType == ShipModuleType.Bomb && !WeaponCategories.Contains("Bomb"))
						{
							WeaponCategories.Add("Bomb");
							ModuleHeader type = new ModuleHeader("Bomb", 240f);
							this.weaponSL.AddItem(type);
						}
						tmp = null;
					}
					foreach (ScrollList.Entry e in this.weaponSL.Entries)
					{
						foreach (KeyValuePair<string, ShipModule> module in Ship_Game.ResourceManager.ShipModulesDict)
						{
							if (!EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).GetMDict()[module.Key] || module.Value.UID == "Dummy")
							{
								continue;
							}
							ShipModule tmp = Ship_Game.ResourceManager.GetModule(module.Key);
							tmp.SetAttributesNoParent();
							if (this.ActiveHull.Role != "fighter" && this.ActiveHull.Role != "scout" && this.ActiveHull.Role != "corvette" && tmp.FightersOnly)
							{
								continue;
							}
							if (tmp.isWeapon)
							{
								if ((e.item as ModuleHeader).Text == tmp.InstalledWeapon.WeaponType)
								{
									e.AddItem(module.Value);
								}
							}
							else if (tmp.ModuleType == ShipModuleType.Bomb && (e.item as ModuleHeader).Text == "Bomb")
							{
								e.AddItem(module.Value);
							}
							tmp = null;
						}
					}
					this.Reset = false;
				}
				this.DrawList();
			}
			if (this.modSel.Tabs[2].Selected)
			{
				if (this.Reset)
				{
					this.weaponSL.Entries.Clear();
					List<string> ModuleCategories = new List<string>();
					foreach (KeyValuePair<string, ShipModule> module in Ship_Game.ResourceManager.ShipModulesDict)
					{
						if (!EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).GetMDict()[module.Key] || module.Value.UID == "Dummy")
						{
							continue;
						}
						module.Value.ModuleType.ToString();
						ShipModule tmp = Ship_Game.ResourceManager.GetModule(module.Key);
						tmp.SetAttributesNoParent();
						if (this.ActiveHull.Role != "fighter" && this.ActiveHull.Role != "scout" && this.ActiveHull.Role != "corvette" && tmp.FightersOnly)
						{
							continue;
						}
						if ((tmp.ModuleType == ShipModuleType.Armor || tmp.ModuleType == ShipModuleType.Shield) && !ModuleCategories.Contains(tmp.ModuleType.ToString()))
						{
							ModuleCategories.Add(tmp.ModuleType.ToString());
							ModuleHeader type = new ModuleHeader(tmp.ModuleType.ToString(), 240f);
							this.weaponSL.AddItem(type);
						}
						tmp = null;
					}
					foreach (ScrollList.Entry e in this.weaponSL.Entries)
					{
						foreach (KeyValuePair<string, ShipModule> module in Ship_Game.ResourceManager.ShipModulesDict)
						{
							if (!EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).GetMDict()[module.Key] || module.Value.UID == "Dummy")
							{
								continue;
							}
							ShipModule tmp = Ship_Game.ResourceManager.GetModule(module.Key);
							tmp.SetAttributesNoParent();
							if (this.ActiveHull.Role != "fighter" && this.ActiveHull.Role != "scout" && this.ActiveHull.Role != "corvette" && tmp.FightersOnly)
							{
								continue;
							}
							if ((tmp.ModuleType == ShipModuleType.Armor || tmp.ModuleType == ShipModuleType.Shield) && (e.item as ModuleHeader).Text == tmp.ModuleType.ToString())
							{
								e.AddItem(module.Value);
							}
							tmp = null;
						}
					}
					this.Reset = false;
				}
				this.DrawList();
			}
			if (this.modSel.Tabs[1].Selected)
			{
				if (this.Reset)
				{
					this.weaponSL.Entries.Clear();
					List<string> ModuleCategories = new List<string>();
					foreach (KeyValuePair<string, ShipModule> module in Ship_Game.ResourceManager.ShipModulesDict)
					{
						if (!EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).GetMDict()[module.Key] || module.Value.UID == "Dummy")
						{
							continue;
						}
						module.Value.ModuleType.ToString();
						ShipModule tmp = Ship_Game.ResourceManager.GetModule(module.Key);
						tmp.SetAttributesNoParent();
						if (this.ActiveHull.Role != "fighter" && this.ActiveHull.Role != "scout" && this.ActiveHull.Role != "corvette" && tmp.FightersOnly)
						{
							continue;
						}
						if ((tmp.ModuleType == ShipModuleType.Engine || tmp.ModuleType == ShipModuleType.FuelCell || tmp.ModuleType == ShipModuleType.PowerPlant || tmp.ModuleType == ShipModuleType.PowerConduit) && !ModuleCategories.Contains(tmp.ModuleType.ToString()))
						{
							ModuleCategories.Add(tmp.ModuleType.ToString());
							ModuleHeader type = new ModuleHeader(tmp.ModuleType.ToString(), 240f);
							this.weaponSL.AddItem(type);
						}
						tmp = null;
					}
					foreach (ScrollList.Entry e in this.weaponSL.Entries)
					{
						foreach (KeyValuePair<string, ShipModule> module in Ship_Game.ResourceManager.ShipModulesDict)
						{
							if (!EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).GetMDict()[module.Key] || module.Value.UID == "Dummy")
							{
								continue;
							}
							ShipModule tmp = Ship_Game.ResourceManager.GetModule(module.Key);
							tmp.SetAttributesNoParent();
							if (this.ActiveHull.Role != "fighter" && this.ActiveHull.Role != "scout" && this.ActiveHull.Role != "corvette" && tmp.FightersOnly)
							{
								continue;
							}
							if ((tmp.ModuleType == ShipModuleType.Engine || tmp.ModuleType == ShipModuleType.FuelCell || tmp.ModuleType == ShipModuleType.PowerPlant || tmp.ModuleType == ShipModuleType.PowerConduit) && (e.item as ModuleHeader).Text == tmp.ModuleType.ToString())
							{
								e.AddItem(module.Value);
							}
							tmp = null;
						}
					}
					this.Reset = false;
				}
				this.DrawList();
			}
			if (this.modSel.Tabs[3].Selected)
			{
				if (this.Reset)
				{
					this.weaponSL.Entries.Clear();
					List<string> ModuleCategories = new List<string>();
					foreach (KeyValuePair<string, ShipModule> module in Ship_Game.ResourceManager.ShipModulesDict)
					{
						if (!EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).GetMDict()[module.Key] || module.Value.UID == "Dummy")
						{
							continue;
						}
						module.Value.ModuleType.ToString();
						ShipModule tmp = Ship_Game.ResourceManager.GetModule(module.Key);
						tmp.SetAttributesNoParent();
						if (this.ActiveHull.Role != "fighter" && this.ActiveHull.Role != "scout" && this.ActiveHull.Role != "corvette" && tmp.FightersOnly)
						{
							continue;
						}
						if ((tmp.ModuleType == ShipModuleType.Colony || tmp.ModuleType == ShipModuleType.Command || tmp.ModuleType == ShipModuleType.Storage || tmp.ModuleType == ShipModuleType.Hangar || tmp.ModuleType == ShipModuleType.Sensors || tmp.ModuleType == ShipModuleType.Special) && !ModuleCategories.Contains(tmp.ModuleType.ToString()))
						{
							ModuleCategories.Add(tmp.ModuleType.ToString());
							ModuleHeader type = new ModuleHeader(tmp.ModuleType.ToString(), 240f);
							this.weaponSL.AddItem(type);
						}
						tmp = null;
					}
					foreach (ScrollList.Entry e in this.weaponSL.Entries)
					{
						foreach (KeyValuePair<string, ShipModule> module in Ship_Game.ResourceManager.ShipModulesDict)
						{
							if (!EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).GetMDict()[module.Key] || module.Value.UID == "Dummy")
							{
								continue;
							}
							ShipModule tmp = Ship_Game.ResourceManager.GetModule(module.Key);
							tmp.SetAttributesNoParent();
							if (this.ActiveHull.Role != "fighter" && this.ActiveHull.Role != "scout" && this.ActiveHull.Role != "corvette" && tmp.FightersOnly)
							{
								continue;
							}
							if ((tmp.ModuleType == ShipModuleType.Colony || tmp.ModuleType == ShipModuleType.Command || tmp.ModuleType == ShipModuleType.Storage || tmp.ModuleType == ShipModuleType.Hangar || tmp.ModuleType == ShipModuleType.Sensors || tmp.ModuleType == ShipModuleType.Special) && (e.item as ModuleHeader).Text == tmp.ModuleType.ToString())
							{
								e.AddItem(module.Value);
							}
							tmp = null;
						}
					}
					this.Reset = false;
				}
				this.DrawList();
			}
		}

		private void DrawRequirement(ref Vector2 Cursor, string words, bool met)
		{
			float amount = 165f;
			if (GlobalStats.Config.Language == "German" || GlobalStats.Config.Language == "French" || GlobalStats.Config.Language == "Polish")
			{
				amount = amount + 35f;
			}
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, words, Cursor, Color.White);
			string stats = (met ? "OK" : "X");
			Cursor.X = Cursor.X + (amount - Fonts.Arial12Bold.MeasureString(stats).X);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, stats, Cursor, (met ? Color.LightGreen : Color.LightPink));
			Cursor.X = Cursor.X - (amount - Fonts.Arial12Bold.MeasureString(stats).X);
		}

		private void DrawShipInfoPanel()
		{
			float HitPoints = 0f;
			float Mass = 0f;
			float PowerDraw = 0f;
			float PowerCapacity = 0f;
			float OrdnanceCap = 0f;
			float PowerFlow = 0f;
			float ShieldPower = 0f;
			float Thrust = 0f;
			float AfterThrust = 0f;
			float CargoSpace = 0f;
			float Size = 0f;
			float Cost = 0f;
			float WarpThrust = 0f;
			float TurnThrust = 0f;
			float WarpableMass = 0f;
			float BurnerDrain = 0f;
			float WarpDraw = 0f;
			float FTLCount = 0f;
			float FTLSpeed = 0f;
			foreach (SlotStruct slot in this.Slots)
			{
				Size = Size + 1f;
				if (slot.isDummy || slot.module == null)
				{
					continue;
				}
				HitPoints = HitPoints + (slot.module.Health + EmpireManager.GetEmpireByName(ShipDesignScreen.screen.PlayerLoyalty).data.Traits.ModHpModifier * slot.module.Health);
				if (slot.module.Mass < 0f && slot.Powered)
				{
					Mass = Mass + slot.module.Mass;
				}
				else if (slot.module.Mass > 0f)
				{
					Mass = Mass + slot.module.Mass;
				}
				PowerCapacity = PowerCapacity + slot.module.PowerStoreMax;
				if (slot.module.ModuleType == ShipModuleType.FuelCell)
				{
					PowerCapacity = PowerCapacity + EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.FuelCellModifier * slot.module.PowerStoreMax;
				}
				OrdnanceCap = OrdnanceCap + (float)slot.module.OrdinanceCapacity;
				PowerFlow = PowerFlow + slot.module.PowerFlowMax;
				if (slot.module.Powered)
				{
					WarpableMass = WarpableMass + slot.module.WarpMassCapacity;
					PowerDraw = PowerDraw + slot.module.PowerDraw;
					BurnerDrain = BurnerDrain + slot.module.PowerDrawWithAfterburner;
					WarpDraw = WarpDraw + slot.module.PowerDrawAtWarp;
					if (slot.module.FTLSpeed > 0f)
					{
						FTLCount = FTLCount + 1f;
						FTLSpeed = FTLSpeed + slot.module.FTLSpeed;
					}
					ShieldPower = ShieldPower + slot.module.shield_power_max;
					Thrust = Thrust + slot.module.thrust;
					WarpThrust = WarpThrust + (float)slot.module.WarpThrust;
					TurnThrust = TurnThrust + (float)slot.module.TurnThrust;
					AfterThrust = AfterThrust + slot.module.AfterburnerThrust;
				}
				Cost = Cost + slot.module.Cost * UniverseScreen.GamePaceStatic;
				CargoSpace = CargoSpace + slot.module.Cargo_Capacity;
			}
			Mass = Mass + (float)(this.ActiveHull.ModuleSlotList.Count / 2);
			Mass = Mass * EmpireManager.GetEmpireByName(ShipDesignScreen.screen.PlayerLoyalty).data.MassModifier;
			if (Mass < (float)(this.ActiveHull.ModuleSlotList.Count / 2))
			{
				Mass = (float)(this.ActiveHull.ModuleSlotList.Count / 2);
			}
			float Speed = 0f;
			float WarpSpeed = WarpThrust / (Mass + 0.1f);
			WarpSpeed = WarpSpeed * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.FTLModifier;
			float single = WarpSpeed / 1000f;
			string WarpString = string.Concat(single.ToString("#.0"), "k");
			float Turn = 0f;
			if (Mass > 0f)
			{
				Speed = Thrust / Mass;
				Turn = TurnThrust / Mass / 700f;
			}
			float AfterSpeed = AfterThrust / (Mass + 0.1f);
			AfterSpeed = AfterSpeed * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.SubLightModifier;
			Turn = (float)MathHelper.ToDegrees(Turn);
			Vector2 Cursor = new Vector2((float)(this.statsSub.Menu.X + 10), (float)(this.ShipStats.Menu.Y + 33));
			this.DrawStat60(ref Cursor, string.Concat(Localizer.Token(109), ":"), (float)((int)Cost), 99);
			Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
			this.DrawStat(ref Cursor, string.Concat(Localizer.Token(110), ":"), (int)PowerCapacity, 100);
			Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
			this.DrawStat(ref Cursor, string.Concat(Localizer.Token(111), ":"), (int)(PowerFlow - PowerDraw), 101);
			Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
			if (!GlobalStats.HardcoreRuleset)
			{
				this.DrawStat(ref Cursor, string.Concat(Localizer.Token(112), ":"), (int)(PowerFlow - PowerDraw * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.FTLPowerDrainModifier), 102);
				Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
			}
			else
			{
				this.DrawStat(ref Cursor, string.Concat(Localizer.Token(112), ":"), (int)(-WarpDraw), 102);
				Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
				this.DrawStat(ref Cursor, "w/ Afterburner:", (int)(-BurnerDrain), "Power draw of the ship when the afterburner is engaged");
				Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
			}
			this.DrawStat(ref Cursor, string.Concat(Localizer.Token(113), ":"), (int)HitPoints, 103);
			Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
			this.DrawStat(ref Cursor, string.Concat(Localizer.Token(114), ":"), (int)ShieldPower, 104);
			Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
			this.DrawStat(ref Cursor, string.Concat(Localizer.Token(115), ":"), (int)Mass, 79);
			Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
			if (GlobalStats.HardcoreRuleset)
			{
				string massstring = "";
				if (Mass >= 1000f && Mass < 10000f || Mass <= -1000f && Mass > -10000f)
				{
					float single1 = (float)Mass / 1000f;
					massstring = string.Concat(single1.ToString("#.#"), "k");
				}
				else if (Mass < 10000f)
				{
					massstring = Mass.ToString("#.#");
				}
				else
				{
					float single2 = (float)Mass / 1000f;
					massstring = string.Concat(single2.ToString("#"), "k");
				}
				string wmassstring = "";
				if (WarpableMass >= 1000f && WarpableMass < 10000f || WarpableMass <= -1000f && WarpableMass > -10000f)
				{
					float single3 = (float)WarpableMass / 1000f;
					wmassstring = string.Concat(single3.ToString("#.#"), "k");
				}
				else if (WarpableMass < 10000f)
				{
					wmassstring = WarpableMass.ToString("0.#");
				}
				else
				{
					float single4 = (float)WarpableMass / 1000f;
					wmassstring = string.Concat(single4.ToString("#"), "k");
				}
				string warpmassstring = string.Concat(massstring, "/", wmassstring);
				if (Mass > WarpableMass)
				{
					this.DrawStatBad(ref Cursor, "Warpable Mass:", warpmassstring, 153);
				}
				else
				{
					this.DrawStat(ref Cursor, "Warpable Mass:", warpmassstring, 153);
				}
				Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
				this.DrawRequirement(ref Cursor, "Warp Capable", Mass <= WarpableMass);
				Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
				if (FTLCount > 0f)
				{
					float speed = FTLSpeed / FTLCount;
					speed = speed + speed * ShipDesignScreen.screen.player.data.FTLBonus;
					this.DrawStat(ref Cursor, string.Concat(Localizer.Token(2170), ":"), (int)speed, 135);
					Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
				}
			}
			else if (WarpSpeed <= 0f)
			{
				this.DrawStat(ref Cursor, string.Concat(Localizer.Token(2170), ":"), 0, 135);
				Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
			}
			else
			{
				this.DrawStat(ref Cursor, string.Concat(Localizer.Token(2170), ":"), WarpString, 135);
				Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
			}
			this.DrawStat(ref Cursor, string.Concat(Localizer.Token(116), ":"), (int)(Speed * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.SubLightModifier), 105);
			Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
			if (GlobalStats.HardcoreRuleset)
			{
				this.DrawStat(ref Cursor, "Afterburner:", (int)AfterSpeed, "Speed of the ship when sublight afterburner is engaged");
				Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
			}
			this.DrawStat60(ref Cursor, string.Concat(Localizer.Token(117), ":"), Turn, 107);
			Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
			this.DrawStat(ref Cursor, string.Concat(Localizer.Token(118), ":"), (int)OrdnanceCap, 108);
			Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
			this.DrawStat(ref Cursor, string.Concat(Localizer.Token(119), ":"), (int)CargoSpace, 109);
			Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
			Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
			bool hasBridge = false;
			bool EmptySlots = true;
			foreach (SlotStruct slot in this.Slots)
			{
				if (!slot.isDummy && slot.ModuleUID == null)
				{
					EmptySlots = false;
				}
				if (slot.ModuleUID == null || !Ship_Game.ResourceManager.ShipModulesDict[slot.ModuleUID].IsCommandModule)
				{
					continue;
				}
				hasBridge = true;
			}
			if (this.ActiveHull.Role != "platform")
			{
				this.DrawRequirement(ref Cursor, Localizer.Token(120), hasBridge);
				Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
			}
			this.DrawRequirement(ref Cursor, Localizer.Token(121), EmptySlots);
		}

		private void DrawStat(ref Vector2 Cursor, string words, float stat, string tip)
		{
			float amount = 105f;
			if (GlobalStats.Config.Language == "German" || GlobalStats.Config.Language == "Polish")
			{
				amount = amount + 20f;
			}
			float x = (float)Mouse.GetState().X;
			MouseState state = Mouse.GetState();
			Vector2 MousePos = new Vector2(x, (float)state.Y);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, words, Cursor, Color.White);
			string numbers = "";
			if (stat >= 1000f && stat < 10000f || stat <= -1000f && stat > -10000f)
			{
				float single = (float)stat / 1000f;
				numbers = string.Concat(single.ToString("#.#"), "k");
			}
			else if (stat < 10000f)
			{
				numbers = stat.ToString("#.#");
			}
			else
			{
				float single1 = (float)stat / 1000f;
				numbers = string.Concat(single1.ToString("#"), "k");
			}
			if (stat == 0f)
			{
				numbers = "0";
			}
			Cursor.X = Cursor.X + (amount - Fonts.Arial12Bold.MeasureString(numbers).X);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, numbers, Cursor, (stat > 0f ? Color.LightGreen : Color.LightPink));
			Cursor.X = Cursor.X - (amount - Fonts.Arial12Bold.MeasureString(numbers).X);
			if (HelperFunctions.CheckIntersection(new Rectangle((int)Cursor.X, (int)Cursor.Y, (int)Fonts.Arial12Bold.MeasureString(words).X + (int)Fonts.Arial12Bold.MeasureString(numbers).X, Fonts.Arial12Bold.LineSpacing), MousePos))
			{
				ToolTip.CreateTooltip(tip, base.ScreenManager);
			}
		}

		private void DrawStat(ref Vector2 Cursor, string words, float stat, int Tooltip_ID)
		{
			float amount = 105f;
			if (GlobalStats.Config.Language == "German" || GlobalStats.Config.Language == "Polish")
			{
				amount = amount + 20f;
			}
			float x = (float)Mouse.GetState().X;
			MouseState state = Mouse.GetState();
			Vector2 MousePos = new Vector2(x, (float)state.Y);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, words, Cursor, Color.White);
			string numbers = "";
			if (stat >= 1000f && stat < 10000f || stat <= -1000f && stat > -10000f)
			{
				float single = (float)stat / 1000f;
				numbers = string.Concat(single.ToString("#.#"), "k");
			}
			else if (stat < 10000f)
			{
				numbers = stat.ToString("#.#");
			}
			else
			{
				float single1 = (float)stat / 1000f;
				numbers = string.Concat(single1.ToString("#"), "k");
			}
			if (stat == 0f)
			{
				numbers = "0";
			}
			Cursor.X = Cursor.X + (amount - Fonts.Arial12Bold.MeasureString(numbers).X);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, numbers, Cursor, (stat > 0f ? Color.LightGreen : Color.LightPink));
			Cursor.X = Cursor.X - (amount - Fonts.Arial12Bold.MeasureString(numbers).X);
			if (HelperFunctions.CheckIntersection(new Rectangle((int)Cursor.X, (int)Cursor.Y, (int)Fonts.Arial12Bold.MeasureString(words).X + (int)Fonts.Arial12Bold.MeasureString(numbers).X, Fonts.Arial12Bold.LineSpacing), MousePos))
			{
				ToolTip.CreateTooltip(Tooltip_ID, base.ScreenManager);
			}
		}

		private void DrawStat(ref Vector2 Cursor, string words, int stat, string tip)
		{
			float amount = 165f;
			if (GlobalStats.Config.Language == "German" || GlobalStats.Config.Language == "French" || GlobalStats.Config.Language == "Polish")
			{
				amount = amount + 20f;
			}
			float x = (float)Mouse.GetState().X;
			MouseState state = Mouse.GetState();
			Vector2 MousePos = new Vector2(x, (float)state.Y);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, words, Cursor, Color.White);
			string numbers = "";
			if (stat >= 1000 && stat < 10000 || stat <= -1000 && stat > -10000)
			{
				float single = (float)stat / 1000f;
				numbers = string.Concat(single.ToString("#.#"), "k");
			}
			else if (stat < 10000)
			{
				numbers = stat.ToString();
			}
			else
			{
				float single1 = (float)stat / 1000f;
				numbers = string.Concat(single1.ToString("#"), "k");
			}
			if (stat == 0)
			{
				numbers = "0";
			}
			Cursor.X = Cursor.X + (amount - Fonts.Arial12Bold.MeasureString(numbers).X);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, numbers, Cursor, (stat > 0 ? Color.LightGreen : Color.LightPink));
			Cursor.X = Cursor.X - (amount - Fonts.Arial12Bold.MeasureString(numbers).X);
			if (HelperFunctions.CheckIntersection(new Rectangle((int)Cursor.X, (int)Cursor.Y, (int)Fonts.Arial12Bold.MeasureString(words).X + (int)Fonts.Arial12Bold.MeasureString(stat.ToString()).X, Fonts.Arial12Bold.LineSpacing), MousePos))
			{
				ToolTip.CreateTooltip(tip, base.ScreenManager);
			}
		}

		private void DrawStat(ref Vector2 Cursor, string words, int stat, int Tooltip_ID)
		{
			float amount = 165f;
			if (GlobalStats.Config.Language == "German" || GlobalStats.Config.Language == "French" || GlobalStats.Config.Language == "Polish")
			{
				amount = amount + 20f;
			}
			float x = (float)Mouse.GetState().X;
			MouseState state = Mouse.GetState();
			Vector2 MousePos = new Vector2(x, (float)state.Y);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, words, Cursor, Color.White);
			string numbers = "";
			if (stat >= 1000 && stat < 10000 || stat <= -1000 && stat > -10000)
			{
				float single = (float)stat / 1000f;
				numbers = string.Concat(single.ToString("#.#"), "k");
			}
			else if (stat < 10000)
			{
				numbers = stat.ToString();
			}
			else
			{
				float single1 = (float)stat / 1000f;
				numbers = string.Concat(single1.ToString("#"), "k");
			}
			Cursor.X = Cursor.X + (amount - Fonts.Arial12Bold.MeasureString(numbers).X);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, numbers, Cursor, (stat > 0 ? Color.LightGreen : Color.LightPink));
			Cursor.X = Cursor.X - (amount - Fonts.Arial12Bold.MeasureString(numbers).X);
			if (HelperFunctions.CheckIntersection(new Rectangle((int)Cursor.X, (int)Cursor.Y, (int)Fonts.Arial12Bold.MeasureString(words).X + (int)Fonts.Arial12Bold.MeasureString(stat.ToString()).X, Fonts.Arial12Bold.LineSpacing), MousePos))
			{
				ToolTip.CreateTooltip(Tooltip_ID, base.ScreenManager);
			}
		}

		private void DrawStat(ref Vector2 Cursor, string words, string stat, int Tooltip_ID)
		{
			float amount = 165f;
			if (GlobalStats.Config.Language == "German" || GlobalStats.Config.Language == "French" || GlobalStats.Config.Language == "Polish")
			{
				amount = amount + 20f;
			}
			float x = (float)Mouse.GetState().X;
			MouseState state = Mouse.GetState();
			Vector2 MousePos = new Vector2(x, (float)state.Y);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, words, Cursor, Color.White);
			Cursor.X = Cursor.X + (amount - Fonts.Arial12Bold.MeasureString(stat).X);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, stat, Cursor, Color.LightGreen);
			Cursor.X = Cursor.X - (amount - Fonts.Arial12Bold.MeasureString(stat.ToString()).X);
			if (HelperFunctions.CheckIntersection(new Rectangle((int)Cursor.X, (int)Cursor.Y, (int)Fonts.Arial12Bold.MeasureString(words).X + (int)Fonts.Arial12Bold.MeasureString(stat.ToString()).X, Fonts.Arial12Bold.LineSpacing), MousePos))
			{
				ToolTip.CreateTooltip(Tooltip_ID, base.ScreenManager);
			}
		}

		private void DrawStat105(ref Vector2 Cursor, string words, string stat, int Tooltip_ID)
		{
			float amount = 105f;
			if (GlobalStats.Config.Language == "German" || GlobalStats.Config.Language == "French" || GlobalStats.Config.Language == "Polish")
			{
				amount = amount + 20f;
			}
			float x = (float)Mouse.GetState().X;
			MouseState state = Mouse.GetState();
			Vector2 MousePos = new Vector2(x, (float)state.Y);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, words, Cursor, Color.White);
			Cursor.X = Cursor.X + (amount - Fonts.Arial12Bold.MeasureString(stat).X);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, stat, Cursor, Color.LightGreen);
			Cursor.X = Cursor.X - (amount - Fonts.Arial12Bold.MeasureString(stat.ToString()).X);
			if (HelperFunctions.CheckIntersection(new Rectangle((int)Cursor.X, (int)Cursor.Y, (int)Fonts.Arial12Bold.MeasureString(words).X + (int)Fonts.Arial12Bold.MeasureString(stat.ToString()).X, Fonts.Arial12Bold.LineSpacing), MousePos))
			{
				ToolTip.CreateTooltip(Tooltip_ID, base.ScreenManager);
			}
		}

		private void DrawStat105Bad(ref Vector2 Cursor, string words, string stat, int Tooltip_ID)
		{
			float amount = 105f;
			if (GlobalStats.Config.Language == "German" || GlobalStats.Config.Language == "French" || GlobalStats.Config.Language == "Polish")
			{
				amount = amount + 20f;
			}
			float x = (float)Mouse.GetState().X;
			MouseState state = Mouse.GetState();
			Vector2 MousePos = new Vector2(x, (float)state.Y);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, words, Cursor, Color.White);
			Cursor.X = Cursor.X + (amount - Fonts.Arial12Bold.MeasureString(stat).X);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, stat, Cursor, Color.LightPink);
			Cursor.X = Cursor.X - (amount - Fonts.Arial12Bold.MeasureString(stat.ToString()).X);
			if (HelperFunctions.CheckIntersection(new Rectangle((int)Cursor.X, (int)Cursor.Y, (int)Fonts.Arial12Bold.MeasureString(words).X + (int)Fonts.Arial12Bold.MeasureString(stat.ToString()).X, Fonts.Arial12Bold.LineSpacing), MousePos))
			{
				ToolTip.CreateTooltip(Tooltip_ID, base.ScreenManager);
			}
		}

		private void DrawStat60(ref Vector2 Cursor, string words, float stat, int Tooltip_ID)
		{
			float amount = 165f;
			if (GlobalStats.Config.Language == "German" || GlobalStats.Config.Language == "French" || GlobalStats.Config.Language == "Polish")
			{
				amount = amount + 20f;
			}
			float x = (float)Mouse.GetState().X;
			MouseState state = Mouse.GetState();
			Vector2 MousePos = new Vector2(x, (float)state.Y);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, words, Cursor, Color.White);
			string numbers = "";
			if (stat >= 1000f && stat < 10000f || stat <= -1000f && stat > -10000f)
			{
				float single = (float)stat / 1000f;
				numbers = string.Concat(single.ToString("#.#"), "k");
			}
			else if (stat < 10000f)
			{
				numbers = stat.ToString("0.#");
			}
			else
			{
				float single1 = (float)stat / 1000f;
				numbers = string.Concat(single1.ToString("#"), "k");
			}
			if (stat == 0f)
			{
				numbers = "0";
			}
			Cursor.X = Cursor.X + (amount - Fonts.Arial12Bold.MeasureString(numbers).X);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, numbers, Cursor, (stat > 0f ? Color.LightGreen : Color.LightPink));
			Cursor.X = Cursor.X - (amount - Fonts.Arial12Bold.MeasureString(numbers).X);
			if (HelperFunctions.CheckIntersection(new Rectangle((int)Cursor.X, (int)Cursor.Y, (int)Fonts.Arial12Bold.MeasureString(words).X + (int)Fonts.Arial12Bold.MeasureString(numbers).X, Fonts.Arial12Bold.LineSpacing), MousePos))
			{
				ToolTip.CreateTooltip(Tooltip_ID, base.ScreenManager);
			}
		}

		private void DrawStatBad(ref Vector2 Cursor, string words, string stat, int Tooltip_ID)
		{
			float amount = 165f;
			if (GlobalStats.Config.Language == "German" || GlobalStats.Config.Language == "French" || GlobalStats.Config.Language == "Polish")
			{
				amount = amount + 20f;
			}
			float x = (float)Mouse.GetState().X;
			MouseState state = Mouse.GetState();
			Vector2 MousePos = new Vector2(x, (float)state.Y);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, words, Cursor, Color.White);
			Cursor.X = Cursor.X + (amount - Fonts.Arial12Bold.MeasureString(stat).X);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, stat, Cursor, Color.LightPink);
			Cursor.X = Cursor.X - (amount - Fonts.Arial12Bold.MeasureString(stat.ToString()).X);
			if (HelperFunctions.CheckIntersection(new Rectangle((int)Cursor.X, (int)Cursor.Y, (int)Fonts.Arial12Bold.MeasureString(words).X + (int)Fonts.Arial12Bold.MeasureString(stat.ToString()).X, Fonts.Arial12Bold.LineSpacing), MousePos))
			{
				ToolTip.CreateTooltip(Tooltip_ID, base.ScreenManager);
			}
		}

		private void DrawUI(GameTime gameTime)
		{
			this.EmpireUI.Draw(base.ScreenManager.SpriteBatch);
			this.DrawShipInfoPanel();
			float transitionOffset = (float)Math.Pow((double)base.TransitionPosition, 2);
			Rectangle r = this.BlackBar;
			if (base.ScreenState == Ship_Game.ScreenState.TransitionOn || base.ScreenState == Ship_Game.ScreenState.TransitionOff)
			{
				r.Y = r.Y + (int)(transitionOffset * 50f);
			}
			Primitives2D.FillRectangle(base.ScreenManager.SpriteBatch, r, Color.Black);
			r = this.bottom_sep;
			if (base.ScreenState == Ship_Game.ScreenState.TransitionOn || base.ScreenState == Ship_Game.ScreenState.TransitionOff)
			{
				r.Y = r.Y + (int)(transitionOffset * 50f);
			}
			Primitives2D.FillRectangle(base.ScreenManager.SpriteBatch, r, new Color(77, 55, 25));
			r = this.SearchBar;
			if (base.ScreenState == Ship_Game.ScreenState.TransitionOn || base.ScreenState == Ship_Game.ScreenState.TransitionOff)
			{
				r.Y = r.Y + (int)(transitionOffset * 50f);
			}
			Primitives2D.FillRectangle(base.ScreenManager.SpriteBatch, r, new Color(54, 54, 54));
			if (Fonts.Arial20Bold.MeasureString(this.ActiveHull.Name).X <= (float)(this.SearchBar.Width - 5))
			{
				Vector2 Cursor = new Vector2((float)(this.SearchBar.X + 3), (float)(r.Y + 14 - Fonts.Arial20Bold.LineSpacing / 2));
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, this.ActiveHull.Name, Cursor, Color.White);
			}
			else
			{
				Vector2 Cursor = new Vector2((float)(this.SearchBar.X + 3), (float)(r.Y + 14 - Fonts.Arial12Bold.LineSpacing / 2));
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, this.ActiveHull.Name, Cursor, Color.White);
			}
			r = this.SaveButton.Rect;
			if (base.ScreenState == Ship_Game.ScreenState.TransitionOn || base.ScreenState == Ship_Game.ScreenState.TransitionOff)
			{
				r.Y = r.Y + (int)(transitionOffset * 50f);
			}
			this.SaveButton.Draw(base.ScreenManager.SpriteBatch, r);
			r = this.LoadButton.Rect;
			if (base.ScreenState == Ship_Game.ScreenState.TransitionOn || base.ScreenState == Ship_Game.ScreenState.TransitionOff)
			{
				r.Y = r.Y + (int)(transitionOffset * 50f);
			}
			this.LoadButton.Draw(base.ScreenManager.SpriteBatch, r);
			r = this.ToggleOverlayButton.Rect;
			if (base.ScreenState == Ship_Game.ScreenState.TransitionOn || base.ScreenState == Ship_Game.ScreenState.TransitionOff)
			{
				r.Y = r.Y + (int)(transitionOffset * 50f);
			}
			this.ToggleOverlayButton.Draw(base.ScreenManager.SpriteBatch, r);
			r = this.Fleets.r;
			if (base.ScreenState == Ship_Game.ScreenState.TransitionOn || base.ScreenState == Ship_Game.ScreenState.TransitionOff)
			{
				r.Y = r.Y + (int)(transitionOffset * 50f);
			}
			this.Fleets.Draw(base.ScreenManager, r);
			r = this.ShipList.r;
			if (base.ScreenState == Ship_Game.ScreenState.TransitionOn || base.ScreenState == Ship_Game.ScreenState.TransitionOff)
			{
				r.Y = r.Y + (int)(transitionOffset * 50f);
			}
			this.ShipList.Draw(base.ScreenManager, r);
			r = this.Shipyard.r;
			if (base.ScreenState == Ship_Game.ScreenState.TransitionOn || base.ScreenState == Ship_Game.ScreenState.TransitionOff)
			{
				r.Y = r.Y + (int)(transitionOffset * 50f);
			}
			this.Shipyard.Draw(base.ScreenManager, r);
			this.DrawModuleSelection();
			this.DrawHullSelection();
			if (this.ActiveModule != null || this.HighlightedModule != null)
			{
				this.DrawActiveModuleData();
			}
			foreach (ToggleButton button in this.CombatStatusButtons)
			{
				button.Draw(base.ScreenManager);
			}
			if (base.IsActive)
			{
				ToolTip.Draw(base.ScreenManager);
			}
		}

		public override void ExitScreen()
		{
			if (!this.ShipSaved && !this.CheckDesign())
			{
				MessageBoxScreen message = new MessageBoxScreen(Localizer.Token(2121), "Save", "Exit");
				message.Cancelled += new EventHandler<EventArgs>(this.DoExit);
				message.Accepted += new EventHandler<EventArgs>(this.SaveWIP);
				base.ScreenManager.AddScreen(message);
				return;
			}
			if (this.ShipSaved || !this.CheckDesign())
			{
				this.ReallyExit();
				return;
			}
			MessageBoxScreen message0 = new MessageBoxScreen(Localizer.Token(2137), "Save", "Exit");
			message0.Cancelled += new EventHandler<EventArgs>(this.DoExit);
			message0.Accepted += new EventHandler<EventArgs>(this.SaveChanges);
			base.ScreenManager.AddScreen(message0);
		}

		public void ExitToMenu(string launches)
		{
			this.screenToLaunch = launches;
			if (this.ShipSaved || this.CheckDesign())
			{
				this.LaunchScreen(null, null);
				this.ReallyExit();
				return;
			}
			MessageBoxScreen message = new MessageBoxScreen(Localizer.Token(2121), "Save", "Exit");
			message.Cancelled += new EventHandler<EventArgs>(this.LaunchScreen);
			message.Accepted += new EventHandler<EventArgs>(this.SaveWIPThenLaunchScreen);
			base.ScreenManager.AddScreen(message);
		}

		/*protected override void Finalize()
		{
			try
			{
				this.Dispose(false);
			}
			finally
			{
				base.Finalize();
			}
		}*/
        ~ShipDesignScreen() {
            //should implicitly do the same thing as the original bad finalize
        }

		public float findAngleToTarget(Vector2 origin, Vector2 target)
		{
			float theta;
			float tX = target.X;
			float tY = target.Y;
			float centerX = origin.X;
			float centerY = origin.Y;
			float angle_to_target = 0f;
			if (tX > centerX && tY < centerY)
			{
				theta = (float)Math.Atan((double)((tY - centerY) / (tX - centerX)));
				theta = theta * 180f / 3.14159274f;
				angle_to_target = 90f - Math.Abs(theta);
			}
			else if (tX > centerX && tY > centerY)
			{
				theta = (float)Math.Atan((double)((tY - centerY) / (tX - centerX)));
				angle_to_target = 90f + theta * 180f / 3.14159274f;
			}
			else if (tX < centerX && tY > centerY)
			{
				theta = (float)Math.Atan((double)((tY - centerY) / (tX - centerX)));
				theta = theta * 180f / 3.14159274f;
				angle_to_target = 270f - Math.Abs(theta);
				angle_to_target = -angle_to_target;
			}
			else if (tX < centerX && tY < centerY)
			{
				theta = (float)Math.Atan((double)((tY - centerY) / (tX - centerX)));
				angle_to_target = 270f + theta * 180f / 3.14159274f;
				angle_to_target = -angle_to_target;
			}
			if (tX == centerX && tY < centerY)
			{
				angle_to_target = 0f;
			}
			else if (tX > centerX && tY == centerY)
			{
				angle_to_target = 90f;
			}
			else if (tX == centerX && tY > centerY)
			{
				angle_to_target = 180f;
			}
			else if (tX < centerX && tY == centerY)
			{
				angle_to_target = 270f;
			}
			return angle_to_target;
		}

		private Vector2 findPointFromAngleAndDistance(Vector2 position, float angle, float distance)
		{
			float theta;
			Vector2 TargetPosition = new Vector2(0f, 0f);
			float gamma = angle;
			float D = distance;
			int gammaQuadrant = 0;
			float oppY = 0f;
			float adjX = 0f;
			if (gamma > 360f)
			{
				gamma = gamma - 360f;
			}
			if (gamma < 90f)
			{
				theta = 90f - gamma;
				theta = theta * 3.14159274f / 180f;
				oppY = D * (float)Math.Sin((double)theta);
				adjX = D * (float)Math.Cos((double)theta);
				gammaQuadrant = 1;
			}
			else if (gamma > 90f && gamma < 180f)
			{
				theta = gamma - 90f;
				theta = theta * 3.14159274f / 180f;
				oppY = D * (float)Math.Sin((double)theta);
				adjX = D * (float)Math.Cos((double)theta);
				gammaQuadrant = 2;
			}
			else if (gamma > 180f && gamma < 270f)
			{
				theta = 270f - gamma;
				theta = theta * 3.14159274f / 180f;
				oppY = D * (float)Math.Sin((double)theta);
				adjX = D * (float)Math.Cos((double)theta);
				gammaQuadrant = 3;
			}
			else if (gamma > 270f && gamma < 360f)
			{
				theta = gamma - 270f;
				theta = theta * 3.14159274f / 180f;
				oppY = D * (float)Math.Sin((double)theta);
				adjX = D * (float)Math.Cos((double)theta);
				gammaQuadrant = 4;
			}
			if (gamma == 0f)
			{
				TargetPosition.X = position.X;
				TargetPosition.Y = position.Y - D;
			}
			if (gamma == 90f)
			{
				TargetPosition.X = position.X + D;
				TargetPosition.Y = position.Y;
			}
			if (gamma == 180f)
			{
				TargetPosition.X = position.X;
				TargetPosition.Y = position.Y + D;
			}
			if (gamma == 270f)
			{
				TargetPosition.X = position.X - D;
				TargetPosition.Y = position.Y;
			}
			if (gammaQuadrant == 1)
			{
				TargetPosition.X = position.X + adjX;
				TargetPosition.Y = position.Y - oppY;
			}
			else if (gammaQuadrant == 2)
			{
				TargetPosition.X = position.X + adjX;
				TargetPosition.Y = position.Y + oppY;
			}
			else if (gammaQuadrant == 3)
			{
				TargetPosition.X = position.X - adjX;
				TargetPosition.Y = position.Y + oppY;
			}
			else if (gammaQuadrant == 4)
			{
				TargetPosition.X = position.X - adjX;
				TargetPosition.Y = position.Y - oppY;
			}
			return TargetPosition;
		}

		private Vector2 GeneratePointOnCircle(float angle, Vector2 center, float radius)
		{
			return this.findPointFromAngleAndDistance(center, angle, radius);
		}

		private string GetConduitGraphic(SlotStruct ss)
		{
			bool right = false;
			bool left = false;
			bool up = false;
			bool down = false;
			int numNear = 0;
			foreach (SlotStruct slot in this.Slots)
			{
				if (slot.module == null || slot.module.ModuleType != ShipModuleType.PowerConduit || slot == ss)
				{
					continue;
				}
				int totalDistanceX = Math.Abs(slot.pq.X - ss.pq.X) / 16;
				int totalDistanceY = Math.Abs(slot.pq.Y - ss.pq.Y) / 16;
				if (totalDistanceX == 1 && totalDistanceY == 0)
				{
					if (slot.pq.X <= ss.pq.X)
					{
						right = true;
					}
					else
					{
						left = true;
					}
				}
				if (totalDistanceY != 1 || totalDistanceX != 0)
				{
					continue;
				}
				if (slot.pq.Y <= ss.pq.Y)
				{
					down = true;
				}
				else
				{
					up = true;
				}
			}
			if (left)
			{
				numNear++;
			}
			if (right)
			{
				numNear++;
			}
			if (up)
			{
				numNear++;
			}
			if (down)
			{
				numNear++;
			}
			if (numNear <= 1)
			{
				if (up)
				{
					return "conduit_powerpoint_up";
				}
				if (down)
				{
					return "conduit_powerpoint_down";
				}
				if (left)
				{
					return "conduit_powerpoint_left";
				}
				if (right)
				{
					return "conduit_powerpoint_right";
				}
				return "conduit_intersection";
			}
			if (numNear != 3)
			{
				if (numNear == 4)
				{
					return "conduit_intersection";
				}
				if (numNear == 2)
				{
					if (left && up)
					{
						return "conduit_corner_TL";
					}
					if (left && down)
					{
						return "conduit_corner_BL";
					}
					if (right && up)
					{
						return "conduit_corner_TR";
					}
					if (right && down)
					{
						return "conduit_corner_BR";
					}
					if (up && down)
					{
						return "conduit_straight_vertical";
					}
					if (left && right)
					{
						return "conduit_straight_horizontal";
					}
				}
			}
			else
			{
				if (up && down && left)
				{
					return "conduit_tsection_right";
				}
				if (up && down && right)
				{
					return "conduit_tsection_left";
				}
				if (left && right && down)
				{
					return "conduit_tsection_up";
				}
				if (left && right && up)
				{
					return "conduit_tsection_down";
				}
			}
			return "";
		}

		private static FileInfo[] GetFilesFromDirectory(string DirPath)
		{
			return (new DirectoryInfo(DirPath)).GetFiles("*.*", SearchOption.AllDirectories);
		}

		private void GoHullLeft()
		{
			ShipDesignScreen hullIndex = this;
			hullIndex.HullIndex = hullIndex.HullIndex - 1;
			if (this.HullIndex < 0)
			{
				this.HullIndex = this.AvailableHulls.Count - 1;
			}
			this.ChangeHull(this.AvailableHulls[this.HullIndex]);
		}

		private void GoHullRight()
		{
			ShipDesignScreen hullIndex = this;
			hullIndex.HullIndex = hullIndex.HullIndex + 1;
			if (this.HullIndex > this.AvailableHulls.Count - 1)
			{
				this.HullIndex = 0;
			}
			this.ChangeHull(this.AvailableHulls[this.HullIndex]);
		}

		public override void HandleInput(InputState input)
		{
			string str;
			if (this.ActiveModule != null && (this.ActiveModule.ModuleType == ShipModuleType.MainGun || this.ActiveModule.ModuleType == ShipModuleType.MissileLauncher || this.ActiveModule.CanRotate))
			{
				if (input.Left)
				{
					this.ChangeModuleState(ShipDesignScreen.ActiveModuleState.Left);
				}
				if (input.Right)
				{
					this.ChangeModuleState(ShipDesignScreen.ActiveModuleState.Right);
				}
				if (input.Down)
				{
					this.ChangeModuleState(ShipDesignScreen.ActiveModuleState.Rear);
				}
				if (input.Up)
				{
					this.ChangeModuleState(ShipDesignScreen.ActiveModuleState.Normal);
				}
			}
			if (this.close.HandleInput(input))
			{
				this.ExitScreen();
				return;
			}
			if (input.CurrentKeyboardState.IsKeyDown(Keys.Z) && input.LastKeyboardState.IsKeyUp(Keys.Z) && input.CurrentKeyboardState.IsKeyDown(Keys.LeftControl))
			{
				if (this.DesignStack.Count > 0)
				{
					ShipModule AM = this.ActiveModule;
					DesignAction da = this.DesignStack.Pop();
					SlotStruct theslot = new SlotStruct();
					foreach (SlotStruct ss in this.Slots)
					{
						if (ss.pq == da.clickedSS.pq)
						{
							this.ClearSlotNoStack(ss);
							theslot = ss;
							theslot.facing = da.clickedSS.facing;
						}
						foreach (SlotStruct other in da.AlteredSlots)
						{
							if (ss.pq != other.pq)
							{
								continue;
							}
							this.ClearSlotNoStack(ss);
							break;
						}
					}
					if (da.clickedSS.ModuleUID != null)
					{
						this.ActiveModule = Ship_Game.ResourceManager.GetModule(da.clickedSS.ModuleUID);
						this.ResetModuleState();
						this.InstallModuleNoStack(theslot);
					}
					foreach (SlotStruct other in da.AlteredSlots)
					{
						foreach (SlotStruct ss in this.Slots)
						{
							if (ss.pq != other.pq || other.ModuleUID == null)
							{
								continue;
							}
							this.ActiveModule = Ship_Game.ResourceManager.GetModule(other.ModuleUID);
							this.ResetModuleState();
							this.InstallModuleNoStack(ss);
							ss.facing = other.facing;
							ss.ModuleUID = other.ModuleUID;
						}
					}
					this.ActiveModule = AM;
					this.ResetModuleState();
				}
				return;
			}
			if (!HelperFunctions.CheckIntersection(this.ModuleSelectionMenu.Menu, input.CursorPosition) && !HelperFunctions.CheckIntersection(this.HullSelectionRect, input.CursorPosition) && !HelperFunctions.CheckIntersection(this.ChooseFighterSub.Menu, input.CursorPosition))
			{
				if (input.ScrollOut)
				{
					ShipDesignScreen transitionZoom = this;
					transitionZoom.TransitionZoom = transitionZoom.TransitionZoom - 0.1f;
					if (this.TransitionZoom < 0.3f)
					{
						this.TransitionZoom = 0.3f;
					}
					if (this.TransitionZoom > 2.65f)
					{
						this.TransitionZoom = 2.65f;
					}
				}
				if (input.ScrollIn)
				{
					ShipDesignScreen shipDesignScreen = this;
					shipDesignScreen.TransitionZoom = shipDesignScreen.TransitionZoom + 0.1f;
					if (this.TransitionZoom < 0.3f)
					{
						this.TransitionZoom = 0.3f;
					}
					if (this.TransitionZoom > 2.65f)
					{
						this.TransitionZoom = 2.65f;
					}
				}
			}
			if (input.CurrentKeyboardState.IsKeyDown(Keys.OemTilde))
			{
				input.LastKeyboardState.IsKeyUp(Keys.OemTilde);
			}
			if (this.Debug)
			{
				if (input.CurrentKeyboardState.IsKeyDown(Keys.Enter) && input.LastKeyboardState.IsKeyUp(Keys.Enter))
				{
					foreach (ModuleSlotData slot in this.ActiveHull.ModuleSlotList)
					{
						slot.InstalledModuleUID = null;
					}
					XmlSerializer Serializer = new XmlSerializer(typeof(ShipData));
					string[] shipStyle = new string[] { "Content/Hulls/", this.ActiveHull.ShipStyle, "/", this.ActiveHull.Name, ".xml" };
					TextWriter WriteFileStream = new StreamWriter(string.Concat(shipStyle));
					Serializer.Serialize(WriteFileStream, this.ActiveHull);
				}
				if (input.Right)
				{
					ShipDesignScreen shipDesignScreen1 = this;
					shipDesignScreen1.operation = (ShipDesignScreen.SlotModOperation)((int)shipDesignScreen1.operation + (int)ShipDesignScreen.SlotModOperation.I);
				}
				if (this.operation > (ShipDesignScreen.SlotModOperation.IO | ShipDesignScreen.SlotModOperation.Add))
				{
					this.operation = ShipDesignScreen.SlotModOperation.Delete;
				}
			}
			this.HoveredModule = null;
			this.mouseStateCurrent = Mouse.GetState();
			Vector2 MousePos = new Vector2((float)this.mouseStateCurrent.X, (float)this.mouseStateCurrent.Y);
			this.selector = null;
			this.EmpireUI.HandleInput(input, this);
			if (this.Fleets.HandleInput(input))
			{
				if (this.ShipSaved || this.CheckDesign())
				{
					base.ScreenManager.AddScreen(new FleetDesignScreen(this.EmpireUI));
					this.ReallyExit();
				}
				else
				{
					MessageBoxScreen message = new MessageBoxScreen(Localizer.Token(2121), "Save", "Exit");
					message.Cancelled += new EventHandler<EventArgs>(this.DoExitToFleetsList);
					message.Accepted += new EventHandler<EventArgs>(this.SaveWIPThenExitToFleets);
					base.ScreenManager.AddScreen(message);
				}
			}
			if (this.ShipList.HandleInput(input))
			{
				if (this.ShipSaved || this.CheckDesign())
				{
					base.ScreenManager.AddScreen(new ShipListScreen(base.ScreenManager, this.EmpireUI));
					this.ReallyExit();
				}
				else
				{
					MessageBoxScreen message = new MessageBoxScreen(Localizer.Token(2121), "Save", "Exit");
					message.Cancelled += new EventHandler<EventArgs>(this.DoExitToShipsList);
					message.Accepted += new EventHandler<EventArgs>(this.SaveWIPThenExitToShipsList);
					base.ScreenManager.AddScreen(message);
				}
			}
			this.activeModSubMenu.HandleInputNoReset(this);
			this.hullSL.HandleInput(input);
			for (int i = this.hullSL.indexAtTop; i < this.hullSL.Copied.Count && i < this.hullSL.indexAtTop + this.hullSL.entriesToDisplay; i++)
			{
				ScrollList.Entry e = this.hullSL.Copied[i];
				if (e.item is ModuleHeader)
				{
					if ((e.item as ModuleHeader).HandleInput(input, e))
					{
						return;
					}
				}
				else if (!HelperFunctions.CheckIntersection(e.clickRect, MousePos))
				{
					e.clickRectHover = 0;
				}
				else
				{
					this.selector = new Selector(base.ScreenManager, e.clickRect);
					e.clickRectHover = 1;
					this.selector = new Selector(base.ScreenManager, e.clickRect);
					if (input.InGameSelect)
					{
						AudioManager.PlayCue("sd_ui_accept_alt3");
						if (this.ShipSaved || this.CheckDesign())
						{
							this.ChangeHull(e.item as ShipData);
							return;
						}
						MessageBoxScreen message = new MessageBoxScreen(Localizer.Token(2121), "Save", "No");
						message.Accepted += new EventHandler<EventArgs>(this.SaveWIPThenChangeHull);
						message.Cancelled += new EventHandler<EventArgs>(this.JustChangeHull);
						this.changeto = e.item as ShipData;
						base.ScreenManager.AddScreen(message);
						return;
					}
				}
			}
			this.modSel.HandleInput(this);
			if (this.ActiveModule != null)
			{
				if (this.ActiveModule.ModuleType == ShipModuleType.Hangar && !this.ActiveModule.IsTroopBay && !this.ActiveModule.IsSupplyBay)
				{
					this.ChooseFighterSL.HandleInput(input);
					int i = this.ChooseFighterSL.indexAtTop;
					while (i < this.ChooseFighterSL.Copied.Count)
					{
						if (i < this.ChooseFighterSL.indexAtTop + this.ChooseFighterSL.entriesToDisplay)
						{
							ScrollList.Entry e = this.ChooseFighterSL.Copied[i];
							if (HelperFunctions.CheckIntersection(e.clickRect, MousePos))
							{
								this.selector = new Selector(base.ScreenManager, e.clickRect);
								e.clickRectHover = 1;
								this.selector = new Selector(base.ScreenManager, e.clickRect);
								if (input.InGameSelect)
								{
									this.ActiveModule.hangarShipUID = (e.item as Ship).Name;
									AudioManager.PlayCue("sd_ui_accept_alt3");
									return;
								}
							}
							i++;
						}
						else
						{
							break;
						}
					}
				}
			}
			else if (this.HighlightedModule != null && this.HighlightedModule.ModuleType == ShipModuleType.Hangar && !this.HighlightedModule.IsTroopBay && !this.HighlightedModule.IsSupplyBay)
			{
				this.ChooseFighterSL.HandleInput(input);
				for (int i = this.ChooseFighterSL.indexAtTop; i < this.ChooseFighterSL.Copied.Count && i < this.ChooseFighterSL.indexAtTop + this.ChooseFighterSL.entriesToDisplay; i++)
				{
					ScrollList.Entry e = this.ChooseFighterSL.Copied[i];
					if (HelperFunctions.CheckIntersection(e.clickRect, MousePos))
					{
						this.selector = new Selector(base.ScreenManager, e.clickRect);
						e.clickRectHover = 1;
						this.selector = new Selector(base.ScreenManager, e.clickRect);
						if (input.InGameSelect)
						{
							this.HighlightedModule.hangarShipUID = (e.item as Ship).Name;
							AudioManager.PlayCue("sd_ui_accept_alt3");
							return;
						}
					}
				}
			}
			for (int i = this.weaponSL.indexAtTop; i < this.weaponSL.Copied.Count && i < this.weaponSL.indexAtTop + this.weaponSL.entriesToDisplay; i++)
			{
				ScrollList.Entry e = this.weaponSL.Copied[i];
				if (e.item is ModuleHeader)
				{
					if ((e.item as ModuleHeader).HandleInput(input, e))
					{
						return;
					}
				}
				else if (!HelperFunctions.CheckIntersection(e.clickRect, MousePos))
				{
					e.clickRectHover = 0;
				}
				else
				{
					this.selector = new Selector(base.ScreenManager, e.clickRect);
					e.clickRectHover = 1;
					this.selector = new Selector(base.ScreenManager, e.clickRect);
					if (input.InGameSelect)
					{
						this.SetActiveModule(Ship_Game.ResourceManager.GetModule((e.item as ShipModule).UID));
						this.ResetModuleState();
						return;
					}
				}
			}
			this.weaponSL.HandleInput(input);
			if (HelperFunctions.CheckIntersection(this.HullSelectionRect, input.CursorPosition) && input.CurrentMouseState.LeftButton == ButtonState.Pressed)
			{
				return;
			}
			if (HelperFunctions.CheckIntersection(this.modSel.Menu, input.CursorPosition) && input.CurrentMouseState.LeftButton == ButtonState.Pressed)
			{
				return;
			}
			if (HelperFunctions.CheckIntersection(this.activeModSubMenu.Menu, input.CursorPosition) && input.CurrentMouseState.LeftButton == ButtonState.Pressed)
			{
				return;
			}
			if (HelperFunctions.CheckIntersection(this.modSel.Menu, MousePos))
			{
				if (this.mouseStateCurrent.ScrollWheelValue > this.mouseStatePrevious.ScrollWheelValue && this.weaponSL.indexAtTop > 0)
				{
					ScrollList scrollList = this.weaponSL;
					scrollList.indexAtTop = scrollList.indexAtTop - 1;
				}
				if (this.mouseStateCurrent.ScrollWheelValue < this.mouseStatePrevious.ScrollWheelValue && this.weaponSL.indexAtTop + this.weaponSL.entriesToDisplay < this.weaponSL.Entries.Count)
				{
					ScrollList scrollList1 = this.weaponSL;
					scrollList1.indexAtTop = scrollList1.indexAtTop + 1;
				}
			}
			if (HelperFunctions.CheckIntersection(this.ArcsButton.R, input.CursorPosition))
			{
				ToolTip.CreateTooltip(134, base.ScreenManager);
			}
			if (this.ArcsButton.HandleInput(input))
			{
				this.ArcsButton.ToggleOn = !this.ArcsButton.ToggleOn;
				if (!this.ArcsButton.ToggleOn)
				{
					this.ShowAllArcs = false;
				}
				else
				{
					this.ShowAllArcs = true;
				}
			}
			if (input.Tab)
			{
				this.ShowAllArcs = !this.ShowAllArcs;
				if (!this.ShowAllArcs)
				{
					this.ArcsButton.ToggleOn = false;
				}
				else
				{
					this.ArcsButton.ToggleOn = true;
				}
			}
			if (input.CurrentMouseState.RightButton == ButtonState.Pressed && input.LastMouseState.RightButton == ButtonState.Released)
			{
				this.StartDragPos = input.CursorPosition;
				this.cameraVelocity.X = 0f;
				this.cameraVelocity.Y = 0f;
			}
			if (input.CurrentMouseState.RightButton != ButtonState.Pressed || input.LastMouseState.RightButton != ButtonState.Pressed)
			{
				this.cameraVelocity.X = 0f;
				this.cameraVelocity.Y = 0f;
			}
			else
			{
				float xDiff = input.CursorPosition.X - this.StartDragPos.X;
				float yDiff = input.CursorPosition.Y - this.StartDragPos.Y;
				Camera2d vector2 = this.camera;
				vector2._pos = vector2._pos + new Vector2(-xDiff, -yDiff);
				this.StartDragPos = input.CursorPosition;
				this.cameraPosition.X = this.cameraPosition.X + -xDiff;
				this.cameraPosition.Y = this.cameraPosition.Y + -yDiff;
			}
			this.cameraVelocity.X = MathHelper.Clamp(this.cameraVelocity.X, -10f, 10f);
			this.cameraVelocity.Y = MathHelper.Clamp(this.cameraVelocity.Y, -10f, 10f);
			if (input.Escaped)
			{
				this.ExitScreen();
			}
			if (this.ToggleOverlay)
			{
				foreach (SlotStruct ss in this.Slots)
				{
					Vector2 ScreenSpace = this.camera.GetScreenSpaceFromWorldSpace(new Vector2((float)ss.pq.enclosingRect.X, (float)ss.pq.enclosingRect.Y));
					Rectangle screenRect = new Rectangle((int)ScreenSpace.X, (int)ScreenSpace.Y, (int)(16f * this.camera.Zoom), (int)(16f * this.camera.Zoom));
					if (!HelperFunctions.CheckIntersection(screenRect, MousePos))
					{
						continue;
					}
					if (ss.isDummy && ss.parent.module != null)
					{
						this.HoveredModule = ss.parent.module;
					}
					else if (ss.module != null)
					{
						this.HoveredModule = ss.module;
					}
					if (input.CurrentMouseState.LeftButton != ButtonState.Pressed || input.LastMouseState.LeftButton != ButtonState.Released)
					{
						continue;
					}
					AudioManager.GetCue("simple_beep").Play();
					if (this.Debug)
					{
						this.DebugAlterSlot(ss.slotReference.Position, this.operation);
						return;
					}
					else if (!ss.isDummy || ss.parent.module == null)
					{
						if (ss.module == null)
						{
							continue;
						}
						this.HighlightedModule = ss.module;
					}
					else
					{
						this.HighlightedModule = ss.parent.module;
					}
				}
			}
			if (HelperFunctions.CheckIntersection(this.upArrow, MousePos) && this.mouseStateCurrent.LeftButton == ButtonState.Released && this.mouseStatePrevious.LeftButton == ButtonState.Pressed && this.scrollPosition > 0)
			{
				ShipDesignScreen shipDesignScreen2 = this;
				shipDesignScreen2.scrollPosition = shipDesignScreen2.scrollPosition - 1;
				AudioManager.GetCue("blip_click").Play();
				foreach (ModuleButton mb in this.ModuleButtons)
				{
					mb.moduleRect.Y = mb.moduleRect.Y + 128;
				}
			}
			if (HelperFunctions.CheckIntersection(this.downArrow, MousePos) && this.mouseStateCurrent.LeftButton == ButtonState.Released && this.mouseStatePrevious.LeftButton == ButtonState.Pressed)
			{
				ShipDesignScreen shipDesignScreen3 = this;
				shipDesignScreen3.scrollPosition = shipDesignScreen3.scrollPosition + 1;
				AudioManager.GetCue("blip_click").Play();
				foreach (ModuleButton mb in this.ModuleButtons)
				{
					mb.moduleRect.Y = mb.moduleRect.Y - 128;
				}
			}
			if (HelperFunctions.CheckIntersection(this.ModuleSelectionArea, MousePos))
			{
				if (input.ScrollIn && this.scrollPosition > 0)
				{
					ShipDesignScreen shipDesignScreen4 = this;
					shipDesignScreen4.scrollPosition = shipDesignScreen4.scrollPosition - 1;
					AudioManager.GetCue("blip_click").Play();
					foreach (ModuleButton mb in this.ModuleButtons)
					{
						mb.moduleRect.Y = mb.moduleRect.Y + 128;
					}
				}
				if (input.ScrollOut)
				{
					ShipDesignScreen shipDesignScreen5 = this;
					shipDesignScreen5.scrollPosition = shipDesignScreen5.scrollPosition + 1;
					AudioManager.GetCue("blip_click").Play();
					foreach (ModuleButton mb in this.ModuleButtons)
					{
						mb.moduleRect.Y = mb.moduleRect.Y - 128;
					}
				}
			}
			if (this.mouseStateCurrent.RightButton == ButtonState.Released && this.mouseStatePrevious.RightButton == ButtonState.Pressed)
			{
				this.ActiveModule = null;
				foreach (SlotStruct slot in this.Slots)
				{
					slot.ShowInvalid = false;
					slot.ShowValid = false;
					Vector2 ScreenSpace = this.camera.GetScreenSpaceFromWorldSpace(new Vector2((float)slot.pq.enclosingRect.X, (float)slot.pq.enclosingRect.Y));
					Rectangle screenRect = new Rectangle((int)ScreenSpace.X, (int)ScreenSpace.Y, (int)(16f * this.camera.Zoom), (int)(16f * this.camera.Zoom));
					if (slot.module == null && !slot.isDummy || !HelperFunctions.CheckIntersection(screenRect, MousePos))
					{
						continue;
					}
					DesignAction da = new DesignAction()
					{
						clickedSS = new SlotStruct()
					};
					da.clickedSS.pq = (slot.isDummy ? slot.parent.pq : slot.pq);
					da.clickedSS.Restrictions = slot.Restrictions;
					da.clickedSS.facing = (slot.module != null ? slot.module.facing : 0f);
					da.clickedSS.ModuleUID = (slot.isDummy ? slot.parent.ModuleUID : slot.ModuleUID);
					da.clickedSS.module = slot.module;
					da.clickedSS.slotReference = (slot.isDummy ? slot.parent.slotReference : slot.slotReference);
					this.DesignStack.Push(da);
					AudioManager.GetCue("sub_bass_whoosh").Play();
					if (!slot.isDummy)
					{
						this.ClearParentSlot(slot);
					}
					else
					{
						this.ClearParentSlot(slot.parent);
					}
					this.RecalculatePower();
				}
			}
			foreach (ModuleButton mb in this.ModuleButtons)
			{
				if (!HelperFunctions.CheckIntersection(this.ModuleSelectionArea, new Vector2((float)(mb.moduleRect.X + 30), (float)(mb.moduleRect.Y + 30))))
				{
					continue;
				}
				if (!HelperFunctions.CheckIntersection(mb.moduleRect, MousePos))
				{
					mb.isHighlighted = false;
				}
				else
				{
					if (input.InGameSelect)
					{
						this.SetActiveModule(Ship_Game.ResourceManager.GetModule(mb.ModuleUID));
					}
					mb.isHighlighted = true;
				}
			}
			if (input.CurrentMouseState.LeftButton == ButtonState.Pressed && this.ActiveModule != null)
			{
				foreach (SlotStruct slot in this.Slots)
				{
					Vector2 ScreenSpace = this.camera.GetScreenSpaceFromWorldSpace(new Vector2((float)slot.pq.enclosingRect.X, (float)slot.pq.enclosingRect.Y));
					Rectangle screenRect = new Rectangle((int)ScreenSpace.X, (int)ScreenSpace.Y, (int)(16f * this.camera.Zoom), (int)(16f * this.camera.Zoom));
					if (!HelperFunctions.CheckIntersection(screenRect, MousePos))
					{
						continue;
					}
					AudioManager.GetCue("sub_bass_mouseover").Play();
					this.InstallModule(slot);
				}
			}
			foreach (SlotStruct slot in this.Slots)
			{
				if (slot.ModuleUID == null || this.HighlightedModule == null || slot.module != this.HighlightedModule || slot.module.FieldOfFire == 0f || slot.module.ModuleType != ShipModuleType.Turret)
				{
					continue;
				}
				float halfArc = slot.module.FieldOfFire / 2f;
				Vector2 Center = this.camera.GetScreenSpaceFromWorldSpace(new Vector2((float)(slot.pq.enclosingRect.X + 16 * slot.module.XSIZE / 2), (float)(slot.pq.enclosingRect.Y + 16 * slot.module.YSIZE / 2)));
				float angleToMouse = Math.Abs(this.findAngleToTarget(Center, MousePos));
				float facing = this.HighlightedModule.facing;
				float difference = 0f;
				difference = Math.Abs(angleToMouse - facing);
				if (difference > halfArc)
				{
					if (angleToMouse > 180f)
					{
						angleToMouse = -1f * (360f - angleToMouse);
					}
					if (facing > 180f)
					{
						facing = -1f * (360f - facing);
					}
					difference = Math.Abs(angleToMouse - facing);
				}
				if (difference >= halfArc || Vector2.Distance(Center, MousePos) >= 300f || this.mouseStateCurrent.LeftButton != ButtonState.Pressed || this.mouseStatePrevious.LeftButton != ButtonState.Pressed)
				{
					continue;
				}
				this.HighlightedModule.facing = Math.Abs(this.findAngleToTarget(Center, MousePos));
			}
			foreach (UIButton b in this.Buttons)
			{
				if (!HelperFunctions.CheckIntersection(b.Rect, MousePos))
				{
					b.State = UIButton.PressState.Normal;
				}
				else
				{
					b.State = UIButton.PressState.Hover;
					if (this.mouseStateCurrent.LeftButton == ButtonState.Pressed && this.mouseStatePrevious.LeftButton == ButtonState.Pressed)
					{
						b.State = UIButton.PressState.Pressed;
					}
					if (this.mouseStateCurrent.LeftButton != ButtonState.Released || this.mouseStatePrevious.LeftButton != ButtonState.Pressed)
					{
						continue;
					}
					string launches = b.Launches;
					str = launches;
					if (launches == null)
					{
						continue;
					}
					if (str == "Toggle Overlay")
					{
						AudioManager.PlayCue("blip_click");
						this.ToggleOverlay = !this.ToggleOverlay;
					}
					else if (str != "Save As...")
					{
						if (str == "Load")
						{
							base.ScreenManager.AddScreen(new LoadDesigns(this));
						}
					}
					else if (!this.CheckDesign())
					{
						AudioManager.PlayCue("UI_Misc20");
						MessageBoxScreen messageBox = new MessageBoxScreen(Localizer.Token(2049));
						base.ScreenManager.AddScreen(messageBox);
					}
					else
					{
						base.ScreenManager.AddScreen(new DesignManager(this, this.ActiveHull.Name));
					}
				}
			}
			if (this.ActiveHull != null)
			{
				foreach (ToggleButton button in this.CombatStatusButtons)
				{
					if (!HelperFunctions.CheckIntersection(button.r, input.CursorPosition))
					{
						button.Hover = false;
					}
					else
					{
						if (button.HasToolTip)
						{
							ToolTip.CreateTooltip(button.WhichToolTip, base.ScreenManager);
						}
						if (input.InGameSelect)
						{
							AudioManager.PlayCue("sd_ui_accept_alt3");
							string action = button.Action;
							str = action;
							if (action != null)
							{
								if (str == "attack")
								{
									this.CombatState = Ship_Game.Gameplay.CombatState.AttackRuns;
								}
								else if (str == "arty")
								{
									this.CombatState = Ship_Game.Gameplay.CombatState.Artillery;
								}
								else if (str == "hold")
								{
									this.CombatState = Ship_Game.Gameplay.CombatState.HoldPosition;
								}
								else if (str == "orbit_left")
								{
									this.CombatState = Ship_Game.Gameplay.CombatState.OrbitLeft;
								}
								else if (str == "orbit_right")
								{
									this.CombatState = Ship_Game.Gameplay.CombatState.OrbitRight;
								}
								else if (str == "evade")
								{
									this.CombatState = Ship_Game.Gameplay.CombatState.Evade;
								}
							}
						}
					}
					string action1 = button.Action;
					str = action1;
					if (action1 == null)
					{
						continue;
					}
					if (str == "attack")
					{
						if (this.CombatState != Ship_Game.Gameplay.CombatState.AttackRuns)
						{
							button.Active = false;
						}
						else
						{
							button.Active = true;
						}
					}
					else if (str == "arty")
					{
						if (this.CombatState != Ship_Game.Gameplay.CombatState.Artillery)
						{
							button.Active = false;
						}
						else
						{
							button.Active = true;
						}
					}
					else if (str == "hold")
					{
						if (this.CombatState != Ship_Game.Gameplay.CombatState.HoldPosition)
						{
							button.Active = false;
						}
						else
						{
							button.Active = true;
						}
					}
					else if (str == "orbit_left")
					{
						if (this.CombatState != Ship_Game.Gameplay.CombatState.OrbitLeft)
						{
							button.Active = false;
						}
						else
						{
							button.Active = true;
						}
					}
					else if (str != "orbit_right")
					{
						if (str == "evade")
						{
							if (this.CombatState != Ship_Game.Gameplay.CombatState.Evade)
							{
								button.Active = false;
							}
							else
							{
								button.Active = true;
							}
						}
					}
					else if (this.CombatState != Ship_Game.Gameplay.CombatState.OrbitRight)
					{
						button.Active = false;
					}
					else
					{
						button.Active = true;
					}
				}
			}
			this.mouseStatePrevious = this.mouseStateCurrent;
			base.HandleInput(input);
		}

		private void InstallModule(SlotStruct slot)
		{
			int slotsAvailable = 0;
			for (int y = 0; y < this.ActiveModule.YSIZE; y++)
			{
				for (int x = 0; x < this.ActiveModule.XSIZE; x++)
				{
					foreach (SlotStruct dummyslot in this.Slots)
					{
						if (dummyslot.pq.Y != slot.pq.Y + 16 * y || dummyslot.pq.X != slot.pq.X + 16 * x || !dummyslot.ShowValid)
						{
							continue;
						}
						slotsAvailable++;
					}
				}
			}
			if (slotsAvailable != this.ActiveModule.XSIZE * this.ActiveModule.YSIZE)
			{
				this.PlayNegativeSound();
				return;
			}
			DesignAction da = new DesignAction()
			{
				clickedSS = new SlotStruct()
				{
					pq = slot.pq,
					Restrictions = slot.Restrictions
				}
			};
			da.clickedSS.facing = (slot.module != null ? slot.module.facing : 0f);
			da.clickedSS.ModuleUID = slot.ModuleUID;
			da.clickedSS.module = slot.module;
			da.clickedSS.tex = slot.tex;
			da.clickedSS.slotReference = slot.slotReference;
			da.clickedSS.state = slot.state;
			this.DesignStack.Push(da);
			this.ClearSlot(slot);
			this.ClearDestinationSlots(slot);
			slot.ModuleUID = this.ActiveModule.UID;
			slot.module = this.ActiveModule;
			slot.module.SetAttributesNoParent();
			slot.state = this.ActiveModState;
			slot.module.facing = this.ActiveModule.facing;
			slot.tex = Ship_Game.ResourceManager.TextureDict[Ship_Game.ResourceManager.ShipModulesDict[this.ActiveModule.UID].IconTexturePath];
			for (int y = 0; y < this.ActiveModule.YSIZE; y++)
			{
				for (int x = 0; x < this.ActiveModule.XSIZE; x++)
				{
					if (x == 0 & y == 0)
					{
						foreach (SlotStruct dummyslot in this.Slots)
						{
							if (dummyslot.pq.Y != slot.pq.Y + 16 * y || dummyslot.pq.X != slot.pq.X + 16 * x)
							{
								continue;
							}
							slot.facing = 0f;
							dummyslot.facing = 0f;
							dummyslot.ModuleUID = null;
							dummyslot.isDummy = true;
							dummyslot.tex = null;
							dummyslot.module = null;
							dummyslot.parent = slot;
						}
					}
				}
			}
			this.RecalculatePower();
			this.ShipSaved = false;
			this.ActiveModule = Ship_Game.ResourceManager.GetModule(this.ActiveModule.UID);
			this.ChangeModuleState(this.ActiveModState);
		}

		private void InstallModuleFromLoad(SlotStruct slot)
		{
			int slotsAvailable = 0;
			for (int y = 0; y < this.ActiveModule.YSIZE; y++)
			{
				for (int x = 0; x < this.ActiveModule.XSIZE; x++)
				{
					foreach (SlotStruct dummyslot in this.Slots)
					{
						if (dummyslot.pq.Y != slot.pq.Y + 16 * y || dummyslot.pq.X != slot.pq.X + 16 * x)
						{
							continue;
						}
						slotsAvailable++;
					}
				}
			}
			if (slotsAvailable != this.ActiveModule.XSIZE * this.ActiveModule.YSIZE)
			{
				this.PlayNegativeSound();
				return;
			}
			ShipDesignScreen.ActiveModuleState savestate = slot.state;
			this.ClearSlot(slot);
			this.ClearDestinationSlotsNoStack(slot);
			slot.ModuleUID = this.ActiveModule.UID;
			slot.module = this.ActiveModule;
			slot.module.SetAttributesNoParent();
			slot.state = savestate;
			slot.module.facing = slot.facing;
			slot.tex = Ship_Game.ResourceManager.TextureDict[Ship_Game.ResourceManager.ShipModulesDict[this.ActiveModule.UID].IconTexturePath];
			for (int y = 0; y < this.ActiveModule.YSIZE; y++)
			{
				for (int x = 0; x < this.ActiveModule.XSIZE; x++)
				{
					if (x == 0 & y == 0)
					{
						foreach (SlotStruct dummyslot in this.Slots)
						{
							if (dummyslot.pq.Y != slot.pq.Y + 16 * y || dummyslot.pq.X != slot.pq.X + 16 * x)
							{
								continue;
							}
							dummyslot.ModuleUID = null;
							dummyslot.isDummy = true;
							dummyslot.tex = null;
							dummyslot.module = null;
							dummyslot.parent = slot;
						}
					}
				}
			}
			this.RecalculatePower();
		}

		private void InstallModuleNoStack(SlotStruct slot)
		{
			int slotsAvailable = 0;
			for (int y = 0; y < this.ActiveModule.YSIZE; y++)
			{
				for (int x = 0; x < this.ActiveModule.XSIZE; x++)
				{
					foreach (SlotStruct dummyslot in this.Slots)
					{
						if (dummyslot.pq.Y != slot.pq.Y + 16 * y || dummyslot.pq.X != slot.pq.X + 16 * x)
						{
							continue;
						}
						slotsAvailable++;
					}
				}
			}
			if (slotsAvailable != this.ActiveModule.XSIZE * this.ActiveModule.YSIZE)
			{
				this.PlayNegativeSound();
				return;
			}
			this.ClearSlotNoStack(slot);
			this.ClearDestinationSlotsNoStack(slot);
			slot.ModuleUID = this.ActiveModule.UID;
			slot.module = this.ActiveModule;
			slot.module.SetAttributesNoParent();
			slot.state = this.ActiveModState;
			slot.module.facing = this.ActiveModule.facing;
			slot.tex = Ship_Game.ResourceManager.TextureDict[Ship_Game.ResourceManager.ShipModulesDict[this.ActiveModule.UID].IconTexturePath];
			for (int y = 0; y < this.ActiveModule.YSIZE; y++)
			{
				for (int x = 0; x < this.ActiveModule.XSIZE; x++)
				{
					if (x == 0 & y == 0)
					{
						foreach (SlotStruct dummyslot in this.Slots)
						{
							if (dummyslot.pq.Y != slot.pq.Y + 16 * y || dummyslot.pq.X != slot.pq.X + 16 * x)
							{
								continue;
							}
							slot.facing = 0f;
							dummyslot.facing = 0f;
							dummyslot.ModuleUID = null;
							dummyslot.isDummy = true;
							dummyslot.tex = null;
							dummyslot.module = null;
							dummyslot.parent = slot;
						}
					}
				}
			}
			this.RecalculatePower();
			this.ShipSaved = false;
			this.ActiveModule = Ship_Game.ResourceManager.GetModule(this.ActiveModule.UID);
			this.ChangeModuleState(this.ActiveModState);
		}

		private void JustChangeHull(object sender, EventArgs e)
		{
			this.ShipSaved = true;
			this.ChangeHull(this.changeto);
		}

		private void LaunchScreen(object sender, EventArgs e)
		{
			string str = this.screenToLaunch;
			string str1 = str;
			if (str != null)
			{
				if (str1 == "Research")
				{
					AudioManager.PlayCue("echo_affirm");
					base.ScreenManager.AddScreen(new ResearchScreenNew(this.EmpireUI));
				}
				else if (str1 == "Budget")
				{
					AudioManager.PlayCue("echo_affirm");
					base.ScreenManager.AddScreen(new BudgetScreen(ShipDesignScreen.screen));
				}
			}
			string str2 = this.screenToLaunch;
			string str3 = str2;
			if (str2 != null)
			{
				if (str3 == "Main Menu")
				{
					AudioManager.PlayCue("echo_affirm");
					ShipDesignScreen.screen.ScreenManager.AddScreen(new GameplayMMScreen(ShipDesignScreen.screen));
				}
				else if (str3 == "Shipyard")
				{
					AudioManager.PlayCue("echo_affirm");
				}
				else if (str3 == "Empire")
				{
					ShipDesignScreen.screen.ScreenManager.AddScreen(new EmpireScreen(ShipDesignScreen.screen.ScreenManager, this.EmpireUI));
					AudioManager.PlayCue("echo_affirm");
				}
				else if (str3 == "Diplomacy")
				{
					ShipDesignScreen.screen.ScreenManager.AddScreen(new MainDiplomacyScreen(ShipDesignScreen.screen));
					AudioManager.PlayCue("echo_affirm");
				}
				else if (str3 == "?")
				{
					AudioManager.PlayCue("sd_ui_tactical_pause");
					InGameWiki wiki = new InGameWiki(new Rectangle(0, 0, 750, 600))
					{
						TitleText = "StarDrive Help",
						MiddleText = "This help menu contains information on all of the gameplay systems contained in StarDrive. You can also watch one of several tutorial videos for a developer-guided introduction to StarDrive."
					};
				}
			}
			this.ReallyExit();
		}

		public override void LoadContent()
		{
			LightRig rig = base.ScreenManager.Content.Load<LightRig>("example/ShipyardLightrig");
			lock (GlobalStats.ObjectManagerLocker)
			{
				base.ScreenManager.inter.LightManager.Clear();
				base.ScreenManager.inter.LightManager.Submit(rig);
			}
			if (base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth <= 1280 || base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight <= 768)
			{
				this.LowRes = true;
			}
			Rectangle leftRect = new Rectangle(5, 45, 405, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 45 - (int)(0.4f * (float)base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight) + 10);
			this.ModuleSelectionMenu = new Menu1(base.ScreenManager, leftRect);
			Rectangle modSelR = new Rectangle(0, (this.LowRes ? 45 : 100), 305, (this.LowRes ? 350 : 400));
			this.modSel = new Submenu(base.ScreenManager, modSelR, true);
			this.modSel.AddTab("Wpn");
			this.modSel.AddTab("Pwr");
			this.modSel.AddTab("Def");
			this.modSel.AddTab("Spc");
			this.weaponSL = new ScrollList(this.modSel);
			Vector2 Cursor = new Vector2((float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 175), 80f);
			Rectangle active = new Rectangle(modSelR.X, modSelR.Y + modSelR.Height + 10, modSelR.Width, 220);
			this.activeModWindow = new Menu1(base.ScreenManager, active);
			Rectangle acsub = new Rectangle(active.X, modSelR.Y + modSelR.Height + 10, 305, 240);
			if (base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight > 760)
			{
				acsub.Height = acsub.Height + 40;
			}
			this.activeModSubMenu = new Submenu(base.ScreenManager, acsub);
			this.activeModSubMenu.AddTab("Active Module");
			this.choosefighterrect = new Rectangle(acsub.X + acsub.Width + 5, acsub.Y, 240, 240);
			if (this.choosefighterrect.Y + this.choosefighterrect.Height > base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight)
			{
				int diff = this.choosefighterrect.Y + this.choosefighterrect.Height - base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight;
				this.choosefighterrect.Height = this.choosefighterrect.Height - (diff + 10);
			}
			this.choosefighterrect.Height = acsub.Height;
			this.ChooseFighterSub = new Submenu(base.ScreenManager, this.choosefighterrect);
			this.ChooseFighterSub.AddTab("Choose Fighter");
			this.ChooseFighterSL = new ScrollList(this.ChooseFighterSub, 40);
			foreach (KeyValuePair<string, bool> hull in EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).GetHDict())
			{
				if (!hull.Value)
				{
					continue;
				}
				this.AvailableHulls.Add(Ship_Game.ResourceManager.HullsDict[hull.Key]);
			}
			PrimitiveQuad.graphicsDevice = base.ScreenManager.GraphicsDevice;
			float width = (float)base.ScreenManager.GraphicsDevice.Viewport.Width;
			Viewport viewport = base.ScreenManager.GraphicsDevice.Viewport;
			float aspectRatio = width / (float)viewport.Height;
			this.offset = new Vector2();
			Viewport viewport1 = base.ScreenManager.GraphicsDevice.Viewport;
			this.offset.X = (float)(viewport1.Width / 2 - 256);
			Viewport viewport2 = base.ScreenManager.GraphicsDevice.Viewport;
			this.offset.Y = (float)(viewport2.Height / 2 - 256);
			this.camera = new Camera2d();
			Camera2d vector2 = this.camera;
			Viewport viewport3 = base.ScreenManager.GraphicsDevice.Viewport;
			float single = (float)viewport3.Width / 2f;
			Viewport viewport4 = base.ScreenManager.GraphicsDevice.Viewport;
			vector2.Pos = new Vector2(single, (float)viewport4.Height / 2f);
			Vector3 camPos = this.cameraPosition * new Vector3(-1f, 1f, 1f);
			this.view = ((Matrix.CreateTranslation(0f, 0f, 0f) * Matrix.CreateRotationY(MathHelper.ToRadians(180f))) * Matrix.CreateRotationX(MathHelper.ToRadians(0f))) * Matrix.CreateLookAt(camPos, new Vector3(camPos.X, camPos.Y, 0f), new Vector3(0f, -1f, 0f));
			this.projection = Matrix.CreatePerspectiveFieldOfView(0.7853982f, aspectRatio, 1f, 20000f);
			this.ChangeHull(this.AvailableHulls[0]);
			lock (GlobalStats.ObjectManagerLocker)
			{
				if (!this.ActiveHull.Animated)
				{
					this.ActiveModel = base.ScreenManager.Content.Load<Model>(this.ActiveHull.ModelPath);
					this.CreateSOFromHull();
				}
				else
				{
					base.ScreenManager.inter.ObjectManager.Remove(this.shipSO);
					SkinnedModel sm = Ship_Game.ResourceManager.GetSkinnedModel(this.ActiveHull.ModelPath);
					this.shipSO = new SceneObject(sm.Model)
					{
						ObjectType = ObjectType.Dynamic,
						World = this.worldMatrix
					};
					base.ScreenManager.inter.ObjectManager.Submit(this.shipSO);
					this.SetupSlots();
				}
			}
			foreach (ModuleSlotData slot in this.ActiveHull.ModuleSlotList)
			{
				if (slot.Position.X < this.LowestX)
				{
					this.LowestX = slot.Position.X;
				}
				if (slot.Position.X <= this.HighestX)
				{
					continue;
				}
				this.HighestX = slot.Position.X;
			}
			float xDistance = this.HighestX - this.LowestX;
			BoundingSphere bs = this.shipSO.WorldBoundingSphere;
			Viewport viewport5 = base.ScreenManager.GraphicsDevice.Viewport;
			Vector3 pScreenSpace = viewport5.Project(Vector3.Zero, this.projection, this.view, Matrix.Identity);
			Vector2 pPos = new Vector2(pScreenSpace.X, pScreenSpace.Y);
			Vector2 radialPos = this.GeneratePointOnCircle(90f, Vector2.Zero, xDistance);
			Viewport viewport6 = base.ScreenManager.GraphicsDevice.Viewport;
			Vector3 insetRadialPos = viewport6.Project(new Vector3(radialPos, 0f), this.projection, this.view, Matrix.Identity);
			Vector2 insetRadialSS = new Vector2(insetRadialPos.X, insetRadialPos.Y);
			float Radius = Vector2.Distance(insetRadialSS, pPos) + 10f;
			if (Radius >= xDistance)
			{
				while (Radius > xDistance)
				{
					camPos = this.cameraPosition * new Vector3(-1f, 1f, 1f);
					this.view = ((Matrix.CreateTranslation(0f, 0f, 0f) * Matrix.CreateRotationY(MathHelper.ToRadians(180f))) * Matrix.CreateRotationX(MathHelper.ToRadians(0f))) * Matrix.CreateLookAt(camPos, new Vector3(camPos.X, camPos.Y, 0f), new Vector3(0f, -1f, 0f));
					bs = this.shipSO.WorldBoundingSphere;
					Viewport viewport7 = base.ScreenManager.GraphicsDevice.Viewport;
					pScreenSpace = viewport7.Project(Vector3.Zero, this.projection, this.view, Matrix.Identity);
					pPos = new Vector2(pScreenSpace.X, pScreenSpace.Y);
					radialPos = this.GeneratePointOnCircle(90f, Vector2.Zero, xDistance);
					Viewport viewport8 = base.ScreenManager.GraphicsDevice.Viewport;
					insetRadialPos = viewport8.Project(new Vector3(radialPos, 0f), this.projection, this.view, Matrix.Identity);
					insetRadialSS = new Vector2(insetRadialPos.X, insetRadialPos.Y);
					Radius = Vector2.Distance(insetRadialSS, pPos) + 10f;
					this.cameraPosition.Z = this.cameraPosition.Z + 1f;
				}
			}
			else
			{
				while (Radius < xDistance)
				{
					camPos = this.cameraPosition * new Vector3(-1f, 1f, 1f);
					this.view = ((Matrix.CreateTranslation(0f, 0f, 0f) * Matrix.CreateRotationY(MathHelper.ToRadians(180f))) * Matrix.CreateRotationX(MathHelper.ToRadians(0f))) * Matrix.CreateLookAt(camPos, new Vector3(camPos.X, camPos.Y, 0f), new Vector3(0f, -1f, 0f));
					bs = this.shipSO.WorldBoundingSphere;
					Viewport viewport9 = base.ScreenManager.GraphicsDevice.Viewport;
					pScreenSpace = viewport9.Project(Vector3.Zero, this.projection, this.view, Matrix.Identity);
					pPos = new Vector2(pScreenSpace.X, pScreenSpace.Y);
					radialPos = this.GeneratePointOnCircle(90f, Vector2.Zero, xDistance);
					Viewport viewport10 = base.ScreenManager.GraphicsDevice.Viewport;
					insetRadialPos = viewport10.Project(new Vector3(radialPos, 0f), this.projection, this.view, Matrix.Identity);
					insetRadialSS = new Vector2(insetRadialPos.X, insetRadialPos.Y);
					Radius = Vector2.Distance(insetRadialSS, pPos) + 10f;
					this.cameraPosition.Z = this.cameraPosition.Z - 1f;
				}
			}
			this.BlackBar = new Rectangle(0, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 70, 3000, 70);
			this.SideBar = new Rectangle(0, 0, 280, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight);
			this.Fleets = new DanButton(new Vector2(21f, (float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 47)), Localizer.Token(103))
			{
				IsToggle = true,
				Toggled = true,
				ToggledText = Localizer.Token(103)
			};
			this.ShipList = new DanButton(new Vector2((float)(66 + this.Fleets.r.Width), (float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 47)), Localizer.Token(104))
			{
				IsToggle = true,
				Toggled = true,
				ToggledText = Localizer.Token(104)
			};
			this.Shipyard = new DanButton(new Vector2((float)(66 + this.Fleets.r.Width + 45 + this.Fleets.r.Width), (float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 47)), Localizer.Token(98))
			{
				IsToggle = true,
				ToggledText = Localizer.Token(98)
			};
			Rectangle w = new Rectangle(20, this.modSel.Menu.Y - 10, 32, 32);
			Rectangle p = new Rectangle(80, w.Y, 32, 32);
			Rectangle df = new Rectangle(150, w.Y, 32, 32);
			Rectangle sp = new Rectangle(220, w.Y, 32, 32);
			this.wpn = new SkinnableButton(w, "Modules/FlakTurret3x3")
			{
				IsToggle = true,
				Toggled = true
			};
			this.pwr = new SkinnableButton(p, "Modules/NuclearReactorMedium")
			{
				IsToggle = true
			};
			this.def = new SkinnableButton(df, "Modules/SteelArmorMedium")
			{
				IsToggle = true
			};
			this.spc = new SkinnableButton(sp, "Modules/sensors_2x2")
			{
				IsToggle = true
			};
			this.SelectedCatTextPos = new Vector2(20f, (float)(w.Y - 25 - Fonts.Arial20Bold.LineSpacing / 2));
			this.SearchBar = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 585, this.Fleets.r.Y, 210, 25);
			Cursor = new Vector2((float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 170), (float)(modSelR.Y + modSelR.Height + 5));
			Vector2 OrdersBarPos = new Vector2(Cursor.X - 60f, (float)((int)Cursor.Y + 10));
			ToggleButton AttackRuns = new ToggleButton(new Rectangle((int)OrdersBarPos.X, (int)OrdersBarPos.Y, 24, 24), "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive", "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press", "SelectionBox/icon_formation_headon");
			this.CombatStatusButtons.Add(AttackRuns);
			AttackRuns.Action = "attack";
			AttackRuns.HasToolTip = true;
			AttackRuns.WhichToolTip = 1;
			OrdersBarPos.X = OrdersBarPos.X + 29f;
			ToggleButton Artillery = new ToggleButton(new Rectangle((int)OrdersBarPos.X, (int)OrdersBarPos.Y, 24, 24), "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive", "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press", "SelectionBox/icon_formation_aft");
			this.CombatStatusButtons.Add(Artillery);
			Artillery.Action = "arty";
			Artillery.HasToolTip = true;
			Artillery.WhichToolTip = 2;
			OrdersBarPos.X = OrdersBarPos.X + 29f;
			ToggleButton HoldPos = new ToggleButton(new Rectangle((int)OrdersBarPos.X, (int)OrdersBarPos.Y, 24, 24), "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive", "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press", "SelectionBox/icon_formation_x");
			this.CombatStatusButtons.Add(HoldPos);
			HoldPos.Action = "hold";
			HoldPos.HasToolTip = true;
			HoldPos.WhichToolTip = 65;
			OrdersBarPos.X = OrdersBarPos.X + 29f;
			ToggleButton OrbitLeft = new ToggleButton(new Rectangle((int)OrdersBarPos.X, (int)OrdersBarPos.Y, 24, 24), "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive", "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press", "SelectionBox/icon_formation_left");
			this.CombatStatusButtons.Add(OrbitLeft);
			OrbitLeft.Action = "orbit_left";
			OrbitLeft.HasToolTip = true;
			OrbitLeft.WhichToolTip = 3;
			OrdersBarPos.X = OrdersBarPos.X + 29f;
			ToggleButton OrbitRight = new ToggleButton(new Rectangle((int)OrdersBarPos.X, (int)OrdersBarPos.Y, 24, 24), "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive", "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press", "SelectionBox/icon_formation_right");
			this.CombatStatusButtons.Add(OrbitRight);
			OrbitRight.Action = "orbit_right";
			OrbitRight.HasToolTip = true;
			OrbitRight.WhichToolTip = 4;
			OrdersBarPos.X = OrdersBarPos.X + 29f;
			ToggleButton Evade = new ToggleButton(new Rectangle((int)OrdersBarPos.X, (int)OrdersBarPos.Y, 24, 24), "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive", "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press", "SelectionBox/icon_formation_stop");
			this.CombatStatusButtons.Add(Evade);
			Evade.Action = "evade";
			Evade.HasToolTip = true;
			Evade.WhichToolTip = 6;
			Cursor = new Vector2((float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 150), (float)this.Fleets.r.Y);
			this.SaveButton = new UIButton()
			{
				Rect = new Rectangle((int)Cursor.X, (int)Cursor.Y, Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px"].Width, Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px"].Height),
				NormalTexture = Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px"],
				HoverTexture = Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px_hover"],
				PressedTexture = Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px_pressed"],
				Text = Localizer.Token(105),
				Launches = "Save As..."
			};
			this.Buttons.Add(this.SaveButton);
			this.LoadButton = new UIButton()
			{
				Rect = new Rectangle((int)Cursor.X - 78, (int)Cursor.Y, Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"].Width, Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"].Height),
				NormalTexture = Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"],
				HoverTexture = Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px_hover"],
				PressedTexture = Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px_pressed"],
				Text = Localizer.Token(8),
				Launches = "Load"
			};
			this.Buttons.Add(this.LoadButton);
			this.ToggleOverlayButton = new UIButton()
			{
				Rect = new Rectangle(this.LoadButton.Rect.X - 140, (int)Cursor.Y, Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px"].Width, Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"].Height),
				NormalTexture = Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px"],
				HoverTexture = Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px_hover"],
				PressedTexture = Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px_pressed"],
				Launches = "Toggle Overlay",
				Text = Localizer.Token(106)
			};
			this.Buttons.Add(this.ToggleOverlayButton);
			this.bottom_sep = new Rectangle(this.BlackBar.X, this.BlackBar.Y, this.BlackBar.Width, 1);
			this.HullSelectionRect = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 285, (this.LowRes ? 45 : 100), 280, (this.LowRes ? 350 : 400));
			this.hullSelectionSub = new Submenu(base.ScreenManager, this.HullSelectionRect, true);
			this.weaponSL = new ScrollList(this.modSel);
			this.hullSelectionSub.AddTab(Localizer.Token(107));
			this.hullSL = new ScrollList(this.hullSelectionSub);
			List<string> Categories = new List<string>();
			foreach (KeyValuePair<string, ShipData> hull in Ship_Game.ResourceManager.HullsDict)
			{
				if (!EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).GetHDict()[hull.Key])
				{
					continue;
				}
				string cat = Localizer.GetRole(hull.Value.Role);
				if (Categories.Contains(cat))
				{
					continue;
				}
				Categories.Add(cat);
			}
			Categories.Sort();
			foreach (string cat in Categories)
			{
				ModuleHeader type = new ModuleHeader(cat, 240f);
				this.hullSL.AddItem(type);
			}
			foreach (ScrollList.Entry e in this.hullSL.Entries)
			{
				foreach (KeyValuePair<string, ShipData> hull in Ship_Game.ResourceManager.HullsDict)
				{
					if (!EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).GetHDict()[hull.Key] || !((e.item as ModuleHeader).Text == Localizer.GetRole(hull.Value.Role)))
					{
						continue;
					}
					e.AddItem(hull.Value);
				}
			}
			Rectangle ShipStatsPanel = new Rectangle(this.HullSelectionRect.X + 50, this.HullSelectionRect.Y + this.HullSelectionRect.Height + 10, 280, 320);
			this.ShipStats = new Menu1(base.ScreenManager, ShipStatsPanel);
			this.statsSub = new Submenu(base.ScreenManager, ShipStatsPanel);
			this.statsSub.AddTab(Localizer.Token(108));
			this.ArcsButton = new GenericButton(new Vector2((float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 30), 44f), "Arcs", Fonts.Pirulen20, Fonts.Pirulen16);
			this.close = new CloseButton(new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 25, 44, 20, 20));
			this.OriginalZ = this.cameraPosition.Z;
		}

		private string parseText(string text, float Width, SpriteFont font)
		{
			string line = string.Empty;
			string returnString = string.Empty;
			string[] strArrays = text.Split(new char[] { ' ' });
			for (int i = 0; i < (int)strArrays.Length; i++)
			{
				string word = strArrays[i];
				if (font.MeasureString(string.Concat(line, word)).Length() > Width)
				{
					returnString = string.Concat(returnString, line, '\n');
					line = string.Empty;
				}
				line = string.Concat(line, word, ' ');
			}
			return string.Concat(returnString, line);
		}

		public void PlayNegativeSound()
		{
			AudioManager.GetCue("UI_Misc20").Play();
		}

		private void ReallyExit()
		{
			LightRig rig = base.ScreenManager.Content.Load<LightRig>("example/NewGamelight_rig");
			lock (GlobalStats.ObjectManagerLocker)
			{
				base.ScreenManager.inter.LightManager.Clear();
				base.ScreenManager.inter.LightManager.Submit(rig);
				base.ScreenManager.inter.ObjectManager.Remove(this.shipSO);
			}
			if (Ship.universeScreen.LookingAtPlanet && Ship.universeScreen.workersPanel is ColonyScreen)
			{
				(Ship.universeScreen.workersPanel as ColonyScreen).Reset = true;
			}
			base.ExitScreen();
		}

		private void RecalculatePower()
		{
			foreach (SlotStruct slot in this.Slots)
			{
				slot.Powered = false;
				slot.CheckedConduits = false;
				if (slot.module == null)
				{
					continue;
				}
				slot.module.Powered = false;
			}
			foreach (SlotStruct powerplant in this.Slots)
			{
				if (powerplant.module == null || powerplant.module.ModuleType != ShipModuleType.PowerPlant)
				{
					if (powerplant.parent == null || powerplant.parent.module.ModuleType != ShipModuleType.PowerPlant)
					{
						continue;
					}
					foreach (SlotStruct slot in this.Slots)
					{
						if (slot.module == null || slot.module.ModuleType != ShipModuleType.PowerConduit || Math.Abs(slot.pq.X - powerplant.pq.X) / 16 + Math.Abs(slot.pq.Y - powerplant.pq.Y) / 16 != 1 || slot.module == null)
						{
							continue;
						}
						this.CheckAndPowerConduit(slot);
					}
				}
				else
				{
					foreach (SlotStruct slot in this.Slots)
					{
						if (slot.module == null || slot.module.ModuleType != ShipModuleType.PowerConduit || Math.Abs(slot.pq.X - powerplant.pq.X) / 16 + Math.Abs(slot.pq.Y - powerplant.pq.Y) / 16 != 1 || slot.module == null)
						{
							continue;
						}
						this.CheckAndPowerConduit(slot);
					}
				}
			}
			foreach (SlotStruct slot in this.Slots)
			{
				if (slot.isDummy || slot.module == null || slot.module.PowerRadius <= 0 || slot.module.ModuleType == ShipModuleType.PowerConduit && !slot.module.Powered)
				{
					continue;
				}
				foreach (SlotStruct ss in this.Slots)
				{
					if (Math.Abs(slot.pq.X - ss.pq.X) / 16 + Math.Abs(slot.pq.Y - ss.pq.Y) / 16 > slot.module.PowerRadius)
					{
						continue;
					}
					ss.Powered = true;
				}
				if (slot.module.XSIZE <= 1 && slot.module.YSIZE <= 1)
				{
					continue;
				}
				for (int y = 0; y < slot.module.YSIZE; y++)
				{
					for (int x = 0; x < slot.module.XSIZE; x++)
					{
						if (x == 0 & y == 0)
						{
							foreach (SlotStruct dummyslot in this.Slots)
							{
								if (dummyslot.pq.Y != slot.pq.Y + 16 * y || dummyslot.pq.X != slot.pq.X + 16 * x)
								{
									continue;
								}
								foreach (SlotStruct ss in this.Slots)
								{
									if (Math.Abs(dummyslot.pq.X - ss.pq.X) / 16 + Math.Abs(dummyslot.pq.Y - ss.pq.Y) / 16 > slot.module.PowerRadius)
									{
										continue;
									}
									ss.Powered = true;
								}
							}
						}
					}
				}
			}
			foreach (SlotStruct slot in this.Slots)
			{
				if (!slot.Powered)
				{
					continue;
				}
				if (slot.module != null && slot.module.ModuleType != ShipModuleType.PowerConduit)
				{
					slot.module.Powered = true;
				}
				if (slot.parent == null || slot.parent.module == null)
				{
					continue;
				}
				slot.parent.module.Powered = true;
			}
		}

		public void ResetLists()
		{
			this.Reset = true;
			this.weaponSL.indexAtTop = 0;
		}

		private void ResetModuleState()
		{
			this.ActiveModState = ShipDesignScreen.ActiveModuleState.Normal;
		}

		private void SaveChanges(object sender, EventArgs e)
		{
			base.ScreenManager.AddScreen(new DesignManager(this, this.ActiveHull.Name));
			this.ShipSaved = true;
		}

		public void SaveShipDesign(string name)
		{
			this.ActiveHull.ModuleSlotList.Clear();
			this.ActiveHull.Name = name;
			ShipData toSave = this.ActiveHull.GetClone();
			foreach (SlotStruct slot in this.Slots)
			{
				if (slot.isDummy)
				{
					ModuleSlotData data = new ModuleSlotData()
					{
						Position = slot.slotReference.Position,
						Restrictions = slot.Restrictions,
						InstalledModuleUID = "Dummy"
					};
					toSave.ModuleSlotList.Add(data);
				}
				else
				{
					ModuleSlotData data = new ModuleSlotData()
					{
						InstalledModuleUID = slot.ModuleUID,
						Position = slot.slotReference.Position,
						Restrictions = slot.Restrictions
					};
					if (slot.module != null)
					{
						data.facing = slot.module.facing;
					}
					toSave.ModuleSlotList.Add(data);
					if (slot.module != null && slot.module.ModuleType == ShipModuleType.Hangar)
					{
						data.SlotOptions = slot.module.hangarShipUID;
					}
					data.state = slot.state;
				}
			}
			string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			Ship_Game.Gameplay.CombatState combatState = toSave.CombatState;
			toSave.CombatState = this.CombatState;
			toSave.Name = name;
			XmlSerializer Serializer = new XmlSerializer(typeof(ShipData));
			TextWriter WriteFileStream = new StreamWriter(string.Concat(path, "/StarDrive/Saved Designs/", name, ".xml"));
			Serializer.Serialize(WriteFileStream, toSave);
			WriteFileStream.Close();
			this.ShipSaved = true;
			if (Ship_Game.ResourceManager.ShipsDict.ContainsKey(name))
			{
				Ship newShip = Ship.CreateShipFromShipData(toSave);
				newShip.SetShipData(toSave);
				newShip.InitForLoad();
				newShip.InitializeStatus();
				Ship_Game.ResourceManager.ShipsDict[name] = newShip;
				Ship_Game.ResourceManager.ShipsDict[name].IsPlayerDesign = true;
			}
			else
			{
				Ship newShip = Ship.CreateShipFromShipData(toSave);
				newShip.SetShipData(toSave);
				newShip.InitForLoad();
				newShip.InitializeStatus();
				Ship_Game.ResourceManager.ShipsDict.Add(name, newShip);
				Ship_Game.ResourceManager.ShipsDict[name].IsPlayerDesign = true;
			}
			EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).UpdateShipsWeCanBuild();
			this.ActiveHull.CombatState = this.CombatState;
			this.ChangeHull(this.ActiveHull);
		}

		private void SaveWIP(object sender, EventArgs e)
		{
			ShipData savedShip = new ShipData()
			{
				Animated = this.ActiveHull.Animated,
				CombatState = this.ActiveHull.CombatState,
				Hull = this.ActiveHull.Hull,
				IconPath = this.ActiveHull.IconPath,
				ModelPath = this.ActiveHull.ModelPath,
				Name = this.ActiveHull.Name,
				Role = this.ActiveHull.Role,
				ShipStyle = this.ActiveHull.ShipStyle,
				ThrusterList = this.ActiveHull.ThrusterList,
				ModuleSlotList = new List<ModuleSlotData>()
			};
			foreach (SlotStruct slot in this.Slots)
			{
				if (!slot.isDummy)
				{
					ModuleSlotData data = new ModuleSlotData()
					{
						InstalledModuleUID = slot.ModuleUID,
						Position = slot.slotReference.Position,
						Restrictions = slot.Restrictions
					};
					if (slot.module != null)
					{
						data.facing = slot.module.facing;
						data.state = slot.state;
					}
					savedShip.ModuleSlotList.Add(data);
					if (slot.module == null || slot.module.ModuleType != ShipModuleType.Hangar)
					{
						continue;
					}
					data.SlotOptions = slot.module.hangarShipUID;
				}
				else if (!slot.isDummy)
				{
					ModuleSlotData data = new ModuleSlotData()
					{
						Position = slot.slotReference.Position,
						Restrictions = slot.Restrictions,
						InstalledModuleUID = ""
					};
					savedShip.ModuleSlotList.Add(data);
				}
				else
				{
					ModuleSlotData data = new ModuleSlotData()
					{
						Position = slot.slotReference.Position,
						Restrictions = slot.Restrictions,
						InstalledModuleUID = "Dummy"
					};
					savedShip.ModuleSlotList.Add(data);
				}
			}
			string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			Ship_Game.Gameplay.CombatState defaultstate = this.ActiveHull.CombatState;
			savedShip.CombatState = this.CombatState;
			string filename = string.Format("{0:yyyy-MM-dd}__{1}", DateTime.Now, this.ActiveHull.Name);
			savedShip.Name = filename;
			XmlSerializer Serializer = new XmlSerializer(typeof(ShipData));
			TextWriter WriteFileStream = new StreamWriter(string.Concat(path, "/StarDrive/WIP/", filename, ".xml"));
			Serializer.Serialize(WriteFileStream, savedShip);
			WriteFileStream.Close();
			savedShip.CombatState = defaultstate;
			this.ShipSaved = true;
		}

		private void SaveWIPThenChangeHull(object sender, EventArgs e)
		{
			this.SaveWIP(sender, e);
			this.ChangeHull(this.changeto);
		}

		private void SaveWIPThenExitToFleets(object sender, EventArgs e)
		{
			this.SaveWIP(sender, e);
			base.ScreenManager.AddScreen(new FleetDesignScreen(this.EmpireUI));
			this.ReallyExit();
		}

		private void SaveWIPThenExitToShipsList(object sender, EventArgs e)
		{
			this.SaveWIP(sender, e);
			base.ScreenManager.AddScreen(new ShipListScreen(base.ScreenManager, this.EmpireUI));
			this.ReallyExit();
		}

		private void SaveWIPThenLaunchScreen(object sender, EventArgs e)
		{
			this.SaveWIP(sender, e);
			string str = this.screenToLaunch;
			string str1 = str;
			if (str != null)
			{
				if (str1 == "Research")
				{
					AudioManager.PlayCue("echo_affirm");
					base.ScreenManager.AddScreen(new ResearchScreenNew(this.EmpireUI));
				}
				else if (str1 == "Budget")
				{
					AudioManager.PlayCue("echo_affirm");
					base.ScreenManager.AddScreen(new BudgetScreen(ShipDesignScreen.screen));
				}
			}
			string str2 = this.screenToLaunch;
			string str3 = str2;
			if (str2 != null)
			{
				if (str3 == "Main Menu")
				{
					AudioManager.PlayCue("echo_affirm");
					ShipDesignScreen.screen.ScreenManager.AddScreen(new GameplayMMScreen(ShipDesignScreen.screen));
				}
				else if (str3 == "Shipyard")
				{
					AudioManager.PlayCue("echo_affirm");
				}
				else if (str3 == "Empire")
				{
					ShipDesignScreen.screen.ScreenManager.AddScreen(new EmpireScreen(ShipDesignScreen.screen.ScreenManager, this.EmpireUI));
					AudioManager.PlayCue("echo_affirm");
				}
				else if (str3 == "Diplomacy")
				{
					ShipDesignScreen.screen.ScreenManager.AddScreen(new MainDiplomacyScreen(ShipDesignScreen.screen));
					AudioManager.PlayCue("echo_affirm");
				}
				else if (str3 == "?")
				{
					AudioManager.PlayCue("sd_ui_tactical_pause");
					InGameWiki wiki = new InGameWiki(new Rectangle(0, 0, 750, 600))
					{
						TitleText = "StarDrive Help",
						MiddleText = "This help menu contains information on all of the gameplay systems contained in StarDrive. You can also watch one of several tutorial videos for a developer-guided introduction to StarDrive."
					};
				}
			}
			this.ReallyExit();
		}

		public void SetActiveModule(ShipModule mod)
		{
			AudioManager.GetCue("smallservo").Play();
			mod.SetAttributesNoParent();
			this.ActiveModule = mod;
			this.ResetModuleState();
			foreach (SlotStruct s in this.Slots)
			{
				s.ShowInvalid = false;
				s.ShowValid = false;
				if (Ship_Game.ResourceManager.ShipModulesDict[this.ActiveModule.UID].Restrictions == Restrictions.I && (s.Restrictions == Restrictions.I || s.Restrictions == Restrictions.IO))
				{
					s.ShowValid = true;
				}
				else if (Ship_Game.ResourceManager.ShipModulesDict[this.ActiveModule.UID].Restrictions == Restrictions.IO && (s.Restrictions == Restrictions.I || s.Restrictions == Restrictions.IO || s.Restrictions == Restrictions.O))
				{
					s.ShowValid = true;
				}
				else if (Ship_Game.ResourceManager.ShipModulesDict[this.ActiveModule.UID].Restrictions == Restrictions.O && (s.Restrictions == Restrictions.O || s.Restrictions == Restrictions.IO))
				{
					s.ShowValid = true;
				}
				else if (Ship_Game.ResourceManager.ShipModulesDict[this.ActiveModule.UID].Restrictions == Restrictions.E && s.Restrictions == Restrictions.E)
				{
					s.ShowValid = true;
				}
				else if (Ship_Game.ResourceManager.ShipModulesDict[this.ActiveModule.UID].Restrictions != Restrictions.IOE || s.Restrictions != Restrictions.I && s.Restrictions != Restrictions.IO && s.Restrictions != Restrictions.O && s.Restrictions != Restrictions.E)
				{
					s.ShowInvalid = true;
				}
				else
				{
					s.ShowValid = true;
				}
			}
			if (this.ActiveModule.ModuleType == ShipModuleType.Hangar)
			{
				this.ChooseFighterSL.Entries.Clear();
				this.ChooseFighterSL.Copied.Clear();
				foreach (string shipname in EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).ShipsWeCanBuild)
				{
					if (!this.ActiveModule.PermittedHangarRoles.Contains(Ship_Game.ResourceManager.ShipsDict[shipname].Role) || Ship_Game.ResourceManager.ShipsDict[shipname].Size >= this.ActiveModule.MaximumHangarShipSize)
					{
						continue;
					}
					this.ChooseFighterSL.AddItem(Ship_Game.ResourceManager.ShipsDict[shipname]);
				}
				if (this.ChooseFighterSL.Entries.Count > 0)
				{
					this.ActiveModule.hangarShipUID = (this.ChooseFighterSL.Entries[0].item as Ship).Name;
				}
			}
			this.HighlightedModule = null;
			this.HoveredModule = null;
			this.ResetModuleState();
		}

		private void SetupSlots()
		{
			this.Slots.Clear();
			foreach (ModuleSlotData slot in this.ActiveHull.ModuleSlotList)
			{
				SlotStruct ss = new SlotStruct();
				PrimitiveQuad pq = new PrimitiveQuad(slot.Position.X + this.offset.X - 8f, slot.Position.Y + this.offset.Y - 8f, 16f, 16f);
				ss.pq = pq;
				ss.Restrictions = slot.Restrictions;
				ss.facing = slot.facing;
				ss.ModuleUID = slot.InstalledModuleUID;
				ss.state = slot.state;
				ss.slotReference = slot;
				ss.SlotOptions = slot.SlotOptions;
				this.Slots.Add(ss);
			}
			foreach (SlotStruct slot in this.Slots)
			{
				if (slot.ModuleUID == null)
				{
					continue;
				}
				this.ActiveModule = Ship_Game.ResourceManager.GetModule(slot.ModuleUID);
				this.ChangeModuleState(slot.state);
				this.InstallModuleFromLoad(slot);
				if (slot.module == null || slot.module.ModuleType != ShipModuleType.Hangar)
				{
					continue;
				}
				slot.module.hangarShipUID = slot.SlotOptions;
			}
			this.ActiveModule = null;
			this.ActiveModState = ShipDesignScreen.ActiveModuleState.Normal;
		}

		public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
		{
			float DesiredZ = MathHelper.SmoothStep(this.camera.Zoom, this.TransitionZoom, 0.2f);
			this.camera.Zoom = DesiredZ;
			if (this.camera.Zoom < 0.3f)
			{
				this.camera.Zoom = 0.3f;
			}
			if (this.camera.Zoom > 2.65f)
			{
				this.camera.Zoom = 2.65f;
			}
			this.cameraPosition.Z = this.OriginalZ / this.camera.Zoom;
			Vector3 camPos = this.cameraPosition * new Vector3(-1f, 1f, 1f);
			this.view = ((Matrix.CreateTranslation(0f, 0f, 0f) * Matrix.CreateRotationY(MathHelper.ToRadians(180f))) * Matrix.CreateRotationX(MathHelper.ToRadians(0f))) * Matrix.CreateLookAt(camPos, new Vector3(camPos.X, camPos.Y, 0f), new Vector3(0f, -1f, 0f));
			base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
		}

		public enum ActiveModuleState
		{
			Normal,
			Left,
			Right,
			Rear
		}

		private enum Colors
		{
			Black,
			Red,
			Blue,
			Orange,
			Yellow,
			Green
		}

		private struct ModuleCatButton
		{
			public Rectangle mRect;

			public string Category;
		}

		private enum SlotModOperation
		{
			Delete,
			I,
			IO,
			O,
			Add,
			E
		}
	}
}
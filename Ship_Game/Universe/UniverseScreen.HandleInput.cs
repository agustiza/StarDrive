using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.AI;
using Ship_Game.Debug;
using Ship_Game.Gameplay;

namespace Ship_Game {
    public sealed partial class UniverseScreen
    {
        private bool HandleGUIClicks(InputState input)
        {
            bool flag = false;
            if (this.dsbw != null && this.showingDSBW && this.dsbw.HandleInput(input))
                flag = true;
            if (this.aw.isOpen && this.aw.HandleInput(input))
                return true;
            if (HelperFunctions.CheckIntersection(this.MinimapDisplayRect, input.CursorPosition) &&
                !this.SelectingWithBox)
            {
                this.HandleScrolls(input);
                if (input.CurrentMouseState.LeftButton == ButtonState.Pressed)
                {
                    Vector2 vector2 = input.CursorPosition -
                                      new Vector2((float) this.MinimapDisplayRect.X, (float) this.MinimapDisplayRect.Y);
                    float num = (float) this.MinimapDisplayRect.Width / (this.Size.X * 2);
                    this.transitionDestination.X =
                        -this.Size.X +
                        (vector2.X /
                         num); //Fixed clicking on the mini-map on location with negative coordinates -Gretman
                    this.transitionDestination.Y = -this.Size.X + (vector2.Y / num);
                    this.snappingToShip = false;
                    this.ViewingShip = false;
                }
                flag = true;
            }
            if (this.SelectedShip != null && this.ShipInfoUIElement.HandleInput(input) && !this.LookingAtPlanet)
                flag = true;
            if (this.SelectedPlanet != null && this.pInfoUI.HandleInput(input) && !this.LookingAtPlanet)
                flag = true;
            if (this.SelectedShipList != null && this.shipListInfoUI.HandleInput(input) && !this.LookingAtPlanet)
                flag = true;
            if (this.SelectedSystem != null)
            {
                if (this.sInfoUI.HandleInput(input) && !this.LookingAtPlanet)
                    flag = true;
            }
            else
                this.sInfoUI.SelectionTimer = 0.0f;
            if (this.minimap.HandleInput(input, this))
                flag = true;
            if (this.NotificationManager.HandleInput(input))
                flag = true;
            if (HelperFunctions.CheckIntersection(this.ShipsInCombat.Rect, input.CursorPosition)) //fbedard
                flag = true;
            if (HelperFunctions.CheckIntersection(this.PlanetsInCombat.Rect, input.CursorPosition)) //fbedard
                flag = true;

            return flag;
        }

        private void HandleInputNotLookingAtPlanet(InputState input)
        {
            mouseWorldPos = UnprojectToWorldPosition(input.MouseScreenPos);
            if (input.DeepSpaceBuildWindow) InputOpenDeepSpaceBuildWindow();

            if (input.FTLOverlay) ToggleUIComponent("sd_ui_accept_alt3", ref showingFTLOverlay);
            if (input.RangeOverlay) ToggleUIComponent("sd_ui_accept_alt3", ref showingRangeOverlay);
            if (input.AutomationWindow) ToggleUIComponent("sd_ui_accept_alt3", ref aw.isOpen);
            if (input.PlanetListScreen)
                ScreenManager.AddScreen(new PlanetListScreen(this, EmpireUI, "sd_ui_accept_alt3"));
            if (input.ShipListScreen) ScreenManager.AddScreen(new ShipListScreen(this, EmpireUI, "sd_ui_accept_alt3"));
            if (input.FleetDesignScreen)
                ScreenManager.AddScreen(new FleetDesignScreen(this, EmpireUI, "sd_ui_accept_alt3"));
            if (input.ZoomToShip) InputZoomToShip();
            if (input.ZoomOut) InputZoomOut();
            if (input.Escaped) DefaultZoomPoints();
            if (input.Tab) ShowShipNames = !ShowShipNames;
            if (Debug)
            {
                Empire empire = EmpireManager.Player;
                if (input.EmpireToggle)
                    empire = EmpireManager.Corsairs;

                if (input.SpawnShip)
                    Ship.CreateShipAtPoint("Bondage-Class Mk IIIa Cruiser", empire, mouseWorldPos);

                if (input.SpawnFleet1) HelperFunctions.CreateFleetAt("Fleet 1", empire, mouseWorldPos);
                if (input.SpawnFleet2) HelperFunctions.CreateFleetAt("Fleet 2", empire, mouseWorldPos);

                if (SelectedShip != null)
                {
                    if (input.EmpireToggle && input.KillThis)
                    {
                        foreach (ShipModule mod in SelectedShip.ModuleSlotList)
                        {
                            mod.Health = 1;
                        } //Added by Gretman so I can hurt ships when the disobey me... I mean for testing... Yea, thats it...
                        SelectedShip.Health = SelectedShip.ModuleSlotList.Length;
                    }
                    else if (input.KillThis)
                        SelectedShip.Die(null, false);
                }
                else if (SelectedPlanet != null && Debug && (input.KillThis))
                {
                    foreach (string troopType in ResourceManager.TroopTypes)
                        SelectedPlanet.AssignTroopToTile(
                            ResourceManager.CreateTroop(troopType, EmpireManager.Remnants));
                }

                if (input.SpawnRemnantShip)
                {
                    if (input.EmpireToggle)
                        Ship.CreateShipAtPoint("Remnant Mothership", EmpireManager.Remnants, mouseWorldPos);
                    else
                        Ship.CreateShipAtPoint("Target Dummy", EmpireManager.Remnants, mouseWorldPos);
                }

                //This little sections added to stress-test the resource manager, and load lots of models into memory.      -Gretman
                if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift) &&
                    input.CurrentKeyboardState.IsKeyDown(Keys.B) && !input.LastKeyboardState.IsKeyDown(Keys.B))
                {
                    if (DebugInfoScreen.Loadmodels == 5) //Repeat
                        DebugInfoScreen.Loadmodels = 0;

                    if (DebugInfoScreen.Loadmodels == 4) //Capital and Carrier
                    {
                        Ship.CreateShipAtPoint("Mordaving L", player, mouseWorldPos); //Cordrazine
                        Ship.CreateShipAtPoint("Revenant-Class Dreadnought", player, mouseWorldPos); //Draylock
                        Ship.CreateShipAtPoint("Draylok Warbird", player, mouseWorldPos); //Draylock
                        Ship.CreateShipAtPoint("Archangel-Class Dreadnought", player, mouseWorldPos); //Human
                        Ship.CreateShipAtPoint("Zanbato-Class Mk IV Battleship", player, mouseWorldPos); //Kulrathi
                        Ship.CreateShipAtPoint("Tarantula-Class Mk V Battleship", player, mouseWorldPos); //Opteris
                        Ship.CreateShipAtPoint("Black Widow-Class Dreadnought", player, mouseWorldPos); //Opteris
                        Ship.CreateShipAtPoint("Corpse Flower III", player, mouseWorldPos); //Pollops
                        Ship.CreateShipAtPoint("Wolfsbane-Class Mk III Battleship", player, mouseWorldPos); //Pollops
                        Ship.CreateShipAtPoint("Sceptre Torp", player, mouseWorldPos); //Rayleh
                        Ship.CreateShipAtPoint("Devourer-Class Mk V Battleship", player, mouseWorldPos); //Vulfen
                        Ship.CreateShipAtPoint("SS-Fighter Base Alpha", player, mouseWorldPos); //Station
                        ++DebugInfoScreen.Loadmodels;
                    }

                    if (DebugInfoScreen.Loadmodels == 3) //Cruiser
                    {
                        Ship.CreateShipAtPoint("Storving Laser", player, mouseWorldPos); //Cordrazine
                        Ship.CreateShipAtPoint("Draylok Bird of Prey", player, mouseWorldPos); //Draylock
                        Ship.CreateShipAtPoint("Terran Torpedo Cruiser", player, mouseWorldPos); //Human
                        Ship.CreateShipAtPoint("Terran Inhibitor", player, mouseWorldPos); //Human
                        Ship.CreateShipAtPoint("Mauler Carrier", player, mouseWorldPos); //Kulrathi
                        Ship.CreateShipAtPoint("Chitin Cruiser Zero L", player, mouseWorldPos); //Opteris
                        Ship.CreateShipAtPoint("Doom Flower", player, mouseWorldPos); //Pollops
                        Ship.CreateShipAtPoint("Missile Acolyte II", player, mouseWorldPos); //Rayleh
                        Ship.CreateShipAtPoint("Ancient Torpedo Cruiser", player, mouseWorldPos); //Remnant
                        Ship.CreateShipAtPoint("Type X Artillery", player, mouseWorldPos); //Vulfen
                        ++DebugInfoScreen.Loadmodels;
                    }

                    if (DebugInfoScreen.Loadmodels == 2) //Frigate
                    {
                        Ship.CreateShipAtPoint("Owlwok Beamer", player, mouseWorldPos); //Cordrazine
                        Ship.CreateShipAtPoint("Scythe Torpedo", player, mouseWorldPos); //Draylock
                        Ship.CreateShipAtPoint("Laser Frigate", player, mouseWorldPos); //Human
                        Ship.CreateShipAtPoint("Missile Corvette", player, mouseWorldPos); //Human
                        Ship.CreateShipAtPoint("Kulrathi Railer", player, mouseWorldPos); //Kulrathi
                        Ship.CreateShipAtPoint("Stormsoldier", player, mouseWorldPos); //Opteris
                        Ship.CreateShipAtPoint("Fern Artillery", player, mouseWorldPos); //Pollops
                        Ship.CreateShipAtPoint("Adv Zion Railer", player, mouseWorldPos); //Rayleh
                        Ship.CreateShipAtPoint("Corsair", player, mouseWorldPos); //Remnant
                        Ship.CreateShipAtPoint("Type VII Laser", player, mouseWorldPos); //Vulfen
                        ++DebugInfoScreen.Loadmodels;
                    }

                    if (DebugInfoScreen.Loadmodels == 1) //Corvette
                    {
                        Ship.CreateShipAtPoint("Laserlitving I", player, mouseWorldPos); //Cordrazine
                        Ship.CreateShipAtPoint("Crescent Rocket", player, mouseWorldPos); //Draylock
                        Ship.CreateShipAtPoint("Missile Hunter", player, mouseWorldPos); //Human
                        Ship.CreateShipAtPoint("Razor RS", player, mouseWorldPos); //Kulrathi
                        Ship.CreateShipAtPoint("Armored Worker", player, mouseWorldPos); //Opteris
                        Ship.CreateShipAtPoint("Thicket Attack Fighter", player, mouseWorldPos); //Pollops
                        Ship.CreateShipAtPoint("Ralyeh Railship", player, mouseWorldPos); //Rayleh
                        Ship.CreateShipAtPoint("Heavy Drone", player, mouseWorldPos); //Remnant
                        Ship.CreateShipAtPoint("Grinder", player, mouseWorldPos); //Vulfen
                        Ship.CreateShipAtPoint("Stalker III Hvy Laser", player, mouseWorldPos); //Vulfen
                        Ship.CreateShipAtPoint("Listening Post", player, mouseWorldPos); //Platform
                        ++DebugInfoScreen.Loadmodels;
                    }

                    if (DebugInfoScreen.Loadmodels == 0) //Fighters and freighters
                    {
                        Ship.CreateShipAtPoint("Laserving", player, mouseWorldPos); //Cordrazine
                        Ship.CreateShipAtPoint("Owlwok Freighter S", player, mouseWorldPos); //Cordrazine
                        Ship.CreateShipAtPoint("Owlwok Freighter M", player, mouseWorldPos); //Cordrazine
                        Ship.CreateShipAtPoint("Owlwok Freighter L", player, mouseWorldPos); //Cordrazine
                        Ship.CreateShipAtPoint("Laserwisp", player, mouseWorldPos); //Draylock
                        Ship.CreateShipAtPoint("Draylok Transporter", player, mouseWorldPos); //Draylock
                        Ship.CreateShipAtPoint("Draylok Medium Trans", player, mouseWorldPos); //Draylock
                        Ship.CreateShipAtPoint("Draylok Mobilizer", player, mouseWorldPos); //Draylock
                        Ship.CreateShipAtPoint("Rocket Scout", player, mouseWorldPos); //Human
                        Ship.CreateShipAtPoint("Small Transport", player, mouseWorldPos); //Human
                        Ship.CreateShipAtPoint("Medium Transport", player, mouseWorldPos); //Human
                        Ship.CreateShipAtPoint("Large Transport", player, mouseWorldPos); //Human
                        Ship.CreateShipAtPoint("Flak Fang", player, mouseWorldPos); //Kulrathi
                        Ship.CreateShipAtPoint("Drone Railer", player, mouseWorldPos); //Opteris
                        Ship.CreateShipAtPoint("Creeper Transport", player, mouseWorldPos); //Opteris
                        Ship.CreateShipAtPoint("Crawler Transport", player, mouseWorldPos); //Opteris
                        Ship.CreateShipAtPoint("Trawler Transport", player, mouseWorldPos); //Opteris
                        Ship.CreateShipAtPoint("Rocket Thorn", player, mouseWorldPos); //Pollops
                        Ship.CreateShipAtPoint("Seeder Transport", player, mouseWorldPos); //Pollops
                        Ship.CreateShipAtPoint("Sower Transport", player, mouseWorldPos); //Pollops
                        Ship.CreateShipAtPoint("Grower Transport", player, mouseWorldPos); //Pollops
                        Ship.CreateShipAtPoint("Ralyeh Interceptor", player, mouseWorldPos); //Rayleh
                        Ship.CreateShipAtPoint("Vessel S", player, mouseWorldPos); //Rayleh
                        Ship.CreateShipAtPoint("Vessel M", player, mouseWorldPos); //Rayleh
                        Ship.CreateShipAtPoint("Vessel L", player, mouseWorldPos); //Rayleh
                        Ship.CreateShipAtPoint("Xeno Fighter", player, mouseWorldPos); //Remnant
                        Ship.CreateShipAtPoint("Type I Vulcan", player, mouseWorldPos); //Vulfen
                        ++DebugInfoScreen.Loadmodels;
                    }
                }
            }
            HandleFleetSelections(input);

            HandleRightMouseNew(input);
            if (input.LeftMouseClick) InputClickableItems(input);
            HandleSelectionBox(input);
            HandleScrolls(input);
        }

        private void HandleInputLookingAtPlanet(InputState input)
        {
            if (input.Tab)
                ShowShipNames = !ShowShipNames;
            if ((input.Escaped || input.RightMouseClick || workersPanel is ColonyScreen
                 && (workersPanel as ColonyScreen).close.HandleInput(input)) &&
                (!(workersPanel is ColonyScreen) || !(workersPanel as ColonyScreen).ClickedTroop))
            {
                if (workersPanel is ColonyScreen && (workersPanel as ColonyScreen).p.Owner == null)
                {
                    AdjustCamTimer = 1f;
                    if (returnToShip)
                    {
                        ViewingShip = true;
                        returnToShip = false;
                        snappingToShip = true;
                        transitionDestination.Z = transitionStartPosition.Z;
                    }
                    else
                        transitionDestination = transitionStartPosition;
                    transitionElapsedTime = 0.0f;
                    LookingAtPlanet = false;
                }
                else
                {
                    AdjustCamTimer = 1f;
                    if (returnToShip)
                    {
                        ViewingShip = true;
                        returnToShip = false;
                        snappingToShip = true;
                        transitionDestination.Z = transitionStartPosition.Z;
                    }
                    else
                        transitionDestination = transitionStartPosition;
                    transitionElapsedTime = 0.0f;
                    LookingAtPlanet = false;
                }
            }
        }

        private bool InputIsDoubleClick()
        {
            if (ClickTimer < TimerDelay)
                return true;
            ClickTimer = 0f;
            return false;
        }

        private void HandleFleetButtonClick(InputState input)
        {
            InputCheckPreviousShip();
            SelectedShip = (Ship) null;
            SelectedShipList.Clear();
            SelectedFleet = (Fleet) null;
            lock (GlobalStats.FleetButtonLocker)
            {
                for (int i = 0; i < FleetButtons.Count; ++i)
                {
                    UniverseScreen.FleetButton fleetButton = FleetButtons[i];
                    if (!HelperFunctions.CheckIntersection(fleetButton.ClickRect, input.CursorPosition))
                        continue;

                    SelectedFleet = fleetButton.Fleet;
                    SelectedShipList.Clear();
                    for (int j = 0; j < SelectedFleet.Ships.Count; j++)
                    {
                        Ship ship = SelectedFleet.Ships[j];
                        if (ship.inSensorRange)
                            SelectedShipList.AddUnique(ship);
                    }
                    if (SelectedShipList.Count == 1)
                    {
                        InputCheckPreviousShip(SelectedShipList[0]);
                        SelectedShip = SelectedShipList[0];
                        ShipInfoUIElement.SetShip(SelectedShip);
                        SelectedShipList.Clear();
                    }
                    else if (SelectedShipList.Count > 1)
                        shipListInfoUI.SetShipList(SelectedShipList, true);
                    SelectedSomethingTimer = 3f;

                    if (!InputIsDoubleClick()) return;

                    ViewingShip = false;
                    AdjustCamTimer = 0.5f;
                    transitionDestination = SelectedFleet.FindAveragePosition().ToVec3();

                    if (viewState < UniverseScreen.UnivScreenState.SystemView)
                        transitionDestination.Z = GetZfromScreenState(UniverseScreen.UnivScreenState.SystemView);
                    return;
                }
            }
        }

        public override void HandleInput(InputState input)
        {
            this.Input = input;

            if (input.PauseGame && !GlobalStats.TakingInput) Paused = !Paused;
            if (ScreenManager.UpdateExitTimeer(!LookingAtPlanet))
                return; //if planet screen is still exiting prevent further input

            for (int index = SelectedShipList.Count - 1; index >= 0; --index)
            {
                Ship ship = SelectedShipList[index];
                if (!ship.Active)
                    SelectedShipList.RemoveSwapLast(ship);
            }
            //CG: previous target code. 
            if (previousSelection != null && input.PreviousTarget) PreviousTargetSelection(input);

            //fbedard: Set camera chase on ship
            if (input.ChaseCam) ChaseCame();

            ShowTacticalCloseup = input.TacticalIcons;

            if (input.UseRealLights)
            {
                UseRealLights = !UseRealLights; // toggle real lights
                SetLighting(UseRealLights);
            }
            if (input.ShowExceptionTracker && !ExceptionTracker.Visible) ReportManual("Manual Report", false);

            if (input.SendKudos && !ExceptionTracker.Visible) ReportManual("Kudos", true);

            if (input.DebugMode)
            {
                Debug = !Debug;
                foreach (SolarSystem solarSystem in UniverseScreen.SolarSystemList)
                    solarSystem.ExploredDict[player] = true;
                GlobalStats.LimitSpeed = GlobalStats.LimitSpeed || Debug;
            }

            HandleEdgeDetection(input);
            GameSpeedIncrease(input.SpeedUp);
            GameSpeedDecrease(input.SpeedDown);


            //fbedard: Click button to Cycle through ships in Combat
            if (!ShipsInCombat.Rect.HitTest(input.CursorPosition))
            {
                ShipsInCombat.State = UIButton.PressState.Default;
            }
            else CycleShipsInCombat(input);


            //fbedard: Click button to Cycle through Planets in Combat
            if (!HelperFunctions.CheckIntersection(PlanetsInCombat.Rect, input.CursorPosition))
            {
                PlanetsInCombat.State = UIButton.PressState.Default;
            }
            else CyclePlanetsInCombat(input);

            if (!LookingAtPlanet)
            {
                if (HandleGUIClicks(input))
                {
                    SkipRightOnce = true;
                    NeedARelease = true;
                    return;
                }
            }
            else
            {
                SelectedFleet = null;
                InputCheckPreviousShip();
                SelectedShip = null;
                SelectedShipList.Clear();
                SelectedItem = null;
                SelectedSystem = null;
            }
            if (input.ScrapShip && (SelectedItem != null && SelectedItem.AssociatedGoal.empire == player))
                HandleInputScrap(input);

            if (Debug)
            {
                if (input.ShowDebugWindow)
                {
                    if (!showdebugwindow)
                        DebugWin = new DebugInfoScreen(ScreenManager, this);
                    else
                        DebugWin = null;
                    showdebugwindow = !showdebugwindow;
                }
                if (Debug && showdebugwindow)
                {
                    DebugWin.HandleInput(input);
                }
                if (input.GetMemory)
                {
                    Memory = GC.GetTotalMemory(false) / 1024f;
                }
            }

            if (DefiningAO)
            {
                if (NeedARelease)
                {
                    if (input.LeftMouseRelease)
                        NeedARelease = false;
                }
                else
                {
                    DefineAO(input);
                    return;
                }
            }
            pickedSomethingThisFrame = false;
            if (LookingAtPlanet)
                workersPanel.HandleInput(input);
            if (IsActive)
                EmpireUI.HandleInput(input);
            if (ShowingPlanetToolTip && input.MouseScreenPos.OutsideRadius(tippedPlanet.ScreenPos, tippedPlanet.Radius))
                ResetToolTipTimer(ref ShowingPlanetToolTip);

            if (ShowingSysTooltip && input.MouseScreenPos.OutsideRadius(tippedPlanet.ScreenPos, tippedSystem.Radius))
                ResetToolTipTimer(ref ShowingSysTooltip);

            if (!LookingAtPlanet)
                HandleInputNotLookingAtPlanet(input);
            else
                HandleInputLookingAtPlanet(input);

            if (input.InGameSelect && !pickedSomethingThisFrame &&
                (!input.RepeatingKeyCheck(Keys.LeftShift) && !pieMenu.Visible))
                HandleFleetButtonClick(input);

            cState = SelectedShip != null || SelectedShipList.Count > 0
                ? UniverseScreen.CursorState.Move
                : UniverseScreen.CursorState.Normal;
            if (SelectedShip == null && SelectedShipList.Count <= 0)
                return;
            for (int i = 0; i < ClickableShipsList.Count; i++)
            {
                UniverseScreen.ClickableShip clickableShip = ClickableShipsList[i];
                if (input.CursorPosition.InRadius(clickableShip.ScreenPos, clickableShip.Radius))
                    cState = UniverseScreen.CursorState.Follow;
            }
            if (cState == UniverseScreen.CursorState.Follow)
                return;
            lock (GlobalStats.ClickableSystemsLock)
            {
                for (int i = 0; i < ClickPlanetList.Count; i++)
                {
                    UniverseScreen.ClickablePlanets planets = ClickPlanetList[i];
                    if (input.CursorPosition.InRadius(planets.ScreenPos, planets.Radius) &&
                        planets.planetToClick.habitable)
                        cState = UniverseScreen.CursorState.Orbit;
                }
            }
        }

        private int InputFleetSelection(InputState input)
        {
            if (input.Fleet1)
                return 1;
            if (input.Fleet2)
                return 2;
            if (input.Fleet3)
                return 3;
            if (input.Fleet4)
                return 4;
            if (input.Fleet5)
                return 5;
            if (input.Fleet6)
                return 6;
            if (input.Fleet7)
                return 7;
            if (input.Fleet8)
                return 8;
            if (input.Fleet9)
                return 9;

            return 10;
        }

        private void HandleFleetSelections(InputState input)
        {
            int index = InputFleetSelection(input);
            if (index == 10) return;

            //replace ships in fleet from selection
            if (input.ReplaceFleet)
            {
                if (SelectedShipList.Count == 0) return;

                for (int i = player.GetFleetsDict()[index].Ships.Count - 1; i >= 0; i--)
                {
                    Ship ship = player.GetFleetsDict()[index].Ships[i];
                    ship?.ClearFleet();
                }

                string str = Fleet.GetDefaultFleetNames(index);
                foreach (Ship ship in SelectedShipList)
                    ship.ClearFleet();
                Fleet fleet = new Fleet();
                fleet.Name = str + " Fleet";
                fleet.Owner = player;

                AddSelectedShipsToFleet(fleet);
                player.GetFleetsDict()[index] = fleet;
            }
            //added by gremlin add ships to exiting fleet
            else if (input.AddToFleet)
            {
                if (SelectedShipList.Count == 0) return;

                string str = Fleet.GetDefaultFleetNames(index);
                Fleet fleet = player.GetFleetsDict()[index];
                foreach (Ship ship in SelectedShipList)
                {
                    if (ship.fleet == fleet) continue;
                    ship.ClearFleet();
                }

                if (fleet != null && fleet.Ships.Count == 0)
                {
                    fleet = new Fleet();
                    fleet.Name = str + " Fleet";
                    fleet.Owner = player;
                }
                AddSelectedShipsToFleet(fleet);
                player.GetFleetsDict()[index] = fleet;
            }
            //end of added by
            else //populate ship info UI with ships in fleet
            {
                if (index != 10)
                {
                    SelectedPlanet = (Planet) null;
                    InputCheckPreviousShip();

                    SelectedShip = (Ship) null;
                    Fleet fleet = player.GetFleetsDict()[index] ?? new Fleet();
                    if (fleet.Ships.Count > 0)
                    {
                        SelectedFleet = fleet;
                        AudioManager.PlayCue("techy_affirm1");
                    }
                    else
                        SelectedFleet = null;
                    SelectedShipList.Clear();
                    foreach (Ship ship in fleet.Ships)
                    {
                        SelectedShipList.Add(ship);
                        SelectedSomethingTimer = 3f;
                    }
                    if (SelectedShipList.Count == 1) //fbedard:display new fleet in UI
                    {
                        InputCheckPreviousShip(SelectedShipList[0]);
                        SelectedShip = SelectedShipList[0];
                        ShipInfoUIElement.SetShip(SelectedShip);
                    }
                    else if (SelectedShipList.Count > 1)
                        shipListInfoUI.SetShipList(SelectedShipList, true);

                    if (SelectedFleet != null && ClickTimer < TimerDelay)
                    {
                        ViewingShip = false;
                        AdjustCamTimer = 0.5f;
                        transitionDestination = SelectedFleet.FindAveragePosition().ToVec3();

                        if (camHeight < GetZfromScreenState(UnivScreenState.SystemView))
                            transitionDestination.Z = GetZfromScreenState(UnivScreenState.SystemView);
                    }
                    else if (SelectedFleet != null)
                        ClickTimer = 0.0f;
                }
            }
        }

        private Ship CheckShipClick(Vector2 ClickPos, InputState input)
        {
            foreach (ClickableShip clickableShip in ClickableShipsList)
            {
                if (input.CursorPosition.InRadius(clickableShip.ScreenPos, clickableShip.Radius))
                    return clickableShip.shipToClick;
            }
            return null;
        }

        private Planet CheckPlanetClick(Vector2 ClickPos)
        {
            foreach (ClickablePlanets clickablePlanets in ClickPlanetList)
            {
                if (Input.CursorPosition.InRadius(clickablePlanets.ScreenPos, clickablePlanets.Radius + 10.0f))
                    return clickablePlanets.planetToClick;
            }
            return null;
        }

        private void HandleRightMouseNew(InputState input)
        {
            if (SkipRightOnce)
            {
                if (input.CurrentMouseState.RightButton != ButtonState.Released ||
                    input.LastMouseState.RightButton != ButtonState.Released)
                    return;
                SkipRightOnce = false;
            }
            else
            {
                Viewport viewport;
                if (input.CurrentMouseState.RightButton == ButtonState.Pressed &&
                    input.LastMouseState.RightButton == ButtonState.Released)
                {
                    SelectedSomethingTimer = 3f;
                    startDrag = new Vector2(input.CurrentMouseState.X, input.CurrentMouseState.Y);
                    startDragWorld = UnprojectToWorldPosition(startDrag);
                    ProjectedPosition = UnprojectToWorldPosition(startDrag);
                    Vector3 position =
                        ScreenManager.GraphicsDevice.Viewport.Unproject(
                            new Vector3(input.CurrentMouseState.X, input.CurrentMouseState.Y, 0.0f), this.projection,
                            this.view, Matrix.Identity);
                    viewport = ScreenManager.GraphicsDevice.Viewport;
                    Vector3 direction = viewport.Unproject(
                                            new Vector3(input.CurrentMouseState.X, input.CurrentMouseState.Y, 1f),
                                            projection, view, Matrix.Identity) - position;
                    direction.Normalize();
                    Ray ray = new Ray(position, direction);
                    float num = -ray.Position.Z / ray.Direction.Z;
                    Vector3 vector3 = new Vector3(ray.Position.X + num * ray.Direction.X,
                        ray.Position.Y + num * ray.Direction.Y, 0.0f);
                }
                if (SelectedShip != null && SelectedShip.AI.State == AIState.ManualControl &&
                    Vector2.Distance(startDragWorld, SelectedShip.Center) < 5000.0)
                    return;
                if (input.CurrentMouseState.RightButton == ButtonState.Released &&
                    input.LastMouseState.RightButton == ButtonState.Pressed)
                {
                    viewport = ScreenManager.GraphicsDevice.Viewport;
                    Vector3 position = viewport.Unproject(
                        new Vector3(input.CurrentMouseState.X, input.CurrentMouseState.Y, 0.0f), projection, view,
                        Matrix.Identity);
                    viewport = ScreenManager.GraphicsDevice.Viewport;
                    Vector3 direction = viewport.Unproject(
                                            new Vector3(input.CurrentMouseState.X, input.CurrentMouseState.Y, 1f),
                                            projection, view, Matrix.Identity) - position;
                    direction.Normalize();
                    Ray ray = new Ray(position, direction);
                    float num1 = -ray.Position.Z / ray.Direction.Z;
                    Vector3 vector3 = new Vector3(ray.Position.X + num1 * ray.Direction.X,
                        ray.Position.Y + num1 * ray.Direction.Y, 0.0f);
                    Vector2 vector2_1 = new Vector2(vector3.X, vector3.Y);
                    Vector2 target = new Vector2(input.CurrentMouseState.X, input.CurrentMouseState.Y);
                    float num2 = startDrag.RadiansToTarget(target);
                    Vector2 vector2_2 = Vector2.Normalize(target - startDrag);
                    if (input.RightMouseTimer > 0.0f)
                    {
                        if (SelectedFleet != null && SelectedFleet.Owner.isPlayer)
                        {
                            AudioManager.PlayCue("echo_affirm1");
                            SelectedSomethingTimer = 3f;
                            float num3 = SelectedFleet.Position.RadiansToTarget(vector2_1);
                            Vector2 vectorToTarget =
                                Vector2.Zero.DirectionToTarget(SelectedFleet.Position.PointFromRadians(num3, 1f));
                            foreach (Ship ship in SelectedFleet.Ships)
                                player.GetGSAI().DefensiveCoordinator.Remove(ship);
                            Ship ship1 = CheckShipClick(startDrag, input);
                            Planet planet;
                            lock (GlobalStats.ClickableSystemsLock)
                                planet = CheckPlanetClick(startDrag);
                            if (ship1 != null && ship1.loyalty != player)
                            {
                                SelectedFleet.Position = ship1.Center;
                                SelectedFleet.AssignPositions(0.0f);
                                foreach (Ship ship2 in SelectedFleet.Ships)
                                {
                                    if (ship2.shipData.Role == ShipData.RoleName.troop)
                                        ship2.AI.OrderTroopToBoardShip(ship1);
                                    else if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
                                        ship2.AI.OrderQueueSpecificTarget(ship1);
                                    else
                                        ship2.AI.OrderAttackSpecificTarget(ship1);
                                }
                            }
                            else if (planet != null)
                            {
                                SelectedFleet.Position = planet.Position; //fbedard: center fleet on planet
                                foreach (Ship ship2 in SelectedFleet.Ships)
                                {
                                    RightClickship(ship2, planet, false);
                                }
                            }
                            else if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
                                SelectedFleet.FormationWarpTo(vector2_1, num3, vectorToTarget, true);
                            else if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftAlt))
                                SelectedFleet.MoveToDirectly(vector2_1, num3, vectorToTarget);
                            else
                                SelectedFleet.FormationWarpTo(vector2_1, num3, vectorToTarget);
                        }
                        else if (SelectedShip != null && SelectedShip.loyalty.isPlayer)
                        {
                            player.GetGSAI().DefensiveCoordinator.DefensiveForcePool.Remove(SelectedShip);
                            SelectedSomethingTimer = 3f;
                            Ship ship = CheckShipClick(startDrag, input);
                            Planet planet;
                            lock (GlobalStats.ClickableSystemsLock)
                                planet = CheckPlanetClick(startDrag);
                            if (ship != null && ship != SelectedShip)
                            {
                                if (SelectedShip.isConstructor ||
                                    SelectedShip.shipData.Role == ShipData.RoleName.supply)
                                {
                                    AudioManager.PlayCue("UI_Misc20");
                                    return;
                                }
                                else
                                {
                                    AudioManager.PlayCue("echo_affirm1");
                                    if (ship.loyalty == player)
                                    {
                                        if (SelectedShip.shipData.Role == ShipData.RoleName.troop)
                                        {
                                            if (ship.TroopList.Count < ship.TroopCapacity)
                                                SelectedShip.AI.OrderTroopToShip(ship);
                                            else
                                                SelectedShip.DoEscort(ship);
                                        }
                                        else
                                            SelectedShip.DoEscort(ship);
                                    }
                                    else if (SelectedShip.shipData.Role == ShipData.RoleName.troop)
                                        SelectedShip.AI.OrderTroopToBoardShip(ship);
                                    else if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
                                        SelectedShip.AI.OrderQueueSpecificTarget(ship);
                                    else
                                        SelectedShip.AI.OrderAttackSpecificTarget(ship);
                                }
                            }
                            // else if (ship != null && ship == this.SelectedShip)
                            else if (ship != null && ship == SelectedShip && SelectedShip.Mothership == null &&
                                     !SelectedShip.isConstructor) //fbedard: prevent hangar ship and constructor
                            {
                                if (ship.loyalty == player)
                                    LoadShipMenuNodes(1);
                                else
                                    LoadShipMenuNodes(0);
                                if (!pieMenu.Visible)
                                {
                                    pieMenu.RootNode = shipMenu;
                                    pieMenu.Show(pieMenu.Position);
                                }
                                else
                                    pieMenu.ChangeTo(null);
                            }
                            else if (planet != null)
                            {
                                RightClickship(SelectedShip, planet, true);
                            }
                            else if (SelectedShip.isConstructor ||
                                     SelectedShip.shipData.Role == ShipData.RoleName.supply)
                            {
                                AudioManager.PlayCue("UI_Misc20");
                                return;
                            }
                            else
                            {
                                AudioManager.PlayCue("echo_affirm1");
                                if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
                                {
                                    if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftAlt))
                                        SelectedShip.AI.OrderMoveDirectlyTowardsPosition(vector2_1, num2, vector2_2,
                                            false);
                                    else
                                        SelectedShip.AI.OrderMoveTowardsPosition(vector2_1, num2, vector2_2, false,
                                            null);
                                }
                                else if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftAlt))
                                    SelectedShip.AI.OrderMoveDirectlyTowardsPosition(vector2_1, num2, vector2_2, true);
                                else if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftControl))
                                {
                                    SelectedShip.AI.OrderMoveTowardsPosition(vector2_1, num2, vector2_2, true, null);
                                    SelectedShip.AI.OrderQueue.Enqueue(new ShipAI.ShipGoal(ShipAI.Plan.HoldPosition,
                                        vector2_1, num2));
                                    SelectedShip.AI.HasPriorityOrder = true;
                                    SelectedShip.AI.IgnoreCombat = true;
                                }
                                else
                                    SelectedShip.AI.OrderMoveTowardsPosition(vector2_1, num2, vector2_2, true, null);
                            }
                        }
                        else if (SelectedShipList.Count > 0)
                        {
                            SelectedSomethingTimer = 3f;
                            foreach (Ship ship in SelectedShipList)
                            {
                                if (ship.loyalty != player || ship.isConstructor ||
                                    ship.shipData.Role == ShipData.RoleName.supply)
                                {
                                    AudioManager.PlayCue("UI_Misc20");
                                    return;
                                }
                            }
                            AudioManager.PlayCue("echo_affirm1");
                            Ship ship1 = CheckShipClick(startDrag, input);
                            Planet planet;
                            lock (GlobalStats.ClickableSystemsLock)
                                planet = CheckPlanetClick(startDrag);
                            if (ship1 != null || planet != null)
                            {
                                foreach (Ship ship2 in SelectedShipList)
                                {
                                    player.GetGSAI().DefensiveCoordinator.Remove(ship2);
                                    if (ship1 != null && ship1 != ship2)
                                    {
                                        if (ship1.loyalty == player)
                                        {
                                            if (ship2.shipData.Role == ShipData.RoleName.troop)
                                            {
                                                if (ship1.TroopList.Count < ship1.TroopCapacity)
                                                    ship2.AI.OrderTroopToShip(ship1);
                                                else
                                                    ship2.DoEscort(ship1);
                                            }
                                            else
                                                ship2.DoEscort(ship1);
                                        }
                                        else if (ship2.shipData.Role == ShipData.RoleName.troop)
                                            ship2.AI.OrderTroopToBoardShip(ship1);
                                        else if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
                                            ship2.AI.OrderQueueSpecificTarget(ship1);
                                        else
                                            ship2.AI.OrderAttackSpecificTarget(ship1);
                                    }
                                    else if (planet != null)
                                    {
                                        RightClickship(ship2, planet, false);
                                    }
                                }
                            }
                            else
                            {
                                SelectedSomethingTimer = 3f;
                                foreach (Ship ship2 in SelectedShipList)
                                {
                                    if (ship2.isConstructor || ship2.shipData.Role == ShipData.RoleName.supply)
                                    {
                                        SelectedShipList.Clear();
                                        AudioManager.PlayCue("UI_Misc20");
                                        return;
                                    }
                                }
                                AudioManager.PlayCue("echo_affirm1");
                                endDragWorld = UnprojectToWorldPosition(input.CursorPosition);
                                SelectedShipList.OrderBy(ship => ship.Center.X);
                                Vector2 fVec = new Vector2(-vector2_2.Y, vector2_2.X);
                                float num3 = Vector2.Distance(endDragWorld, startDragWorld);
                                int num4 = 0;
                                int num5 = 0;
                                float num6 = 0.0f;
                                for (int i = 0; i < SelectedShipList.Count; ++i)
                                {
                                    player.GetGSAI().DefensiveCoordinator.Remove(SelectedShipList[i]);
                                    if (SelectedShipList[i].GetSO().WorldBoundingSphere.Radius > num6)
                                        num6 = SelectedShipList[i].GetSO().WorldBoundingSphere.Radius;
                                }
                                Fleet fleet = new Fleet();
                                if (SelectedShipList.Count * num6 > num3)
                                {
                                    for (int i = 0; i < SelectedShipList.Count; ++i)
                                    {
                                        fleet.AddShip(SelectedShipList[i]);
                                        fleet.Ships[i].RelativeFleetOffset =
                                            new Vector2((num6 + 200f) * num5, num4 * (num6 + 200f));
                                        ++num5;
                                        if (fleet.Ships[i].RelativeFleetOffset.X + num6 > num3)
                                        {
                                            num5 = 0;
                                            ++num4;
                                        }
                                    }
                                }
                                else
                                {
                                    float num7 = num3 / SelectedShipList.Count;
                                    for (int i = 0; i < SelectedShipList.Count; ++i)
                                    {
                                        fleet.AddShip(SelectedShipList[i]);
                                        fleet.Ships[i].RelativeFleetOffset = new Vector2(num7 * i, 0.0f);
                                    }
                                }
                                fleet.ProjectPos(endDragWorld, num2 - 1.570796f, fVec);
                                foreach (Ship ship2 in fleet.Ships)
                                {
                                    foreach (Ship ship3 in SelectedShipList)
                                    {
                                        if (ship2.guid == ship3.guid)
                                        {
                                            if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
                                            {
                                                if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftAlt))
                                                    ship3.AI.OrderMoveDirectlyTowardsPosition(ship2.projectedPosition,
                                                        num2 - 1.570796f, fVec, false);
                                                else
                                                    ship3.AI.OrderMoveTowardsPosition(ship2.projectedPosition,
                                                        num2 - 1.570796f, fVec, false, null);
                                            }
                                            else if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftAlt))
                                                ship3.AI.OrderMoveDirectlyTowardsPosition(ship2.projectedPosition,
                                                    num2 - 1.570796f, fVec, true);
                                            else
                                                ship3.AI.OrderMoveTowardsPosition(ship2.projectedPosition,
                                                    num2 - 1.570796f, fVec, true, null);
                                        }
                                    }
                                }
                                projectedGroup = fleet;
                                fleet.Reset();
                            }
                        }
                        if (SelectedFleet == null && SelectedItem == null &&
                            SelectedShip == null && SelectedPlanet == null && SelectedShipList.Count == 0)
                        {
                            Ship ship = CheckShipClick(input.CursorPosition, input);
                            if (ship != null && ship.Mothership == null &&
                                !ship.isConstructor) //fbedard: prevent hangar ship and constructor
                            {
                                if (SelectedShip != null && previousSelection != SelectedShip &&
                                    SelectedShip != ship) //fbedard
                                    previousSelection = SelectedShip;
                                SelectedShip = ship;
                                if (ship.loyalty == player)
                                    LoadShipMenuNodes(1);
                                else
                                    LoadShipMenuNodes(0);
                                if (!pieMenu.Visible)
                                {
                                    pieMenu.RootNode = shipMenu;
                                    pieMenu.Show(pieMenu.Position);
                                }
                                else
                                    pieMenu.ChangeTo(null);
                            }
                        }
                    }
                    else
                    {
                        ProjectingPosition = true;
                        if (SelectedFleet != null && SelectedFleet.Owner == player)
                        {
                            SelectedSomethingTimer = 3f;
                            if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
                                SelectedFleet.FormationWarpTo(ProjectedPosition, num2, vector2_2, true);
                            else
                                SelectedFleet.FormationWarpTo(ProjectedPosition, num2, vector2_2);
                            AudioManager.PlayCue("echo_affirm1");
                            foreach (Ship ship in SelectedFleet.Ships)
                                player.GetGSAI().DefensiveCoordinator.Remove(ship);
                        }
                        else if (SelectedShip != null && SelectedShip.loyalty == player)
                        {
                            player.GetGSAI().DefensiveCoordinator.Remove(SelectedShip);
                            SelectedSomethingTimer = 3f;
                            if (SelectedShip.isConstructor || SelectedShip.shipData.Role == ShipData.RoleName.supply)
                            {
                                if (SelectedShip != null && previousSelection != SelectedShip) //fbedard
                                    previousSelection = SelectedShip;
                                SelectedShip = null;
                                AudioManager.PlayCue("UI_Misc20");
                                return;
                            }
                            else
                            {
                                AudioManager.PlayCue("echo_affirm1");
                                if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
                                {
                                    if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftAlt))
                                        SelectedShip.AI.OrderMoveDirectlyTowardsPosition(ProjectedPosition, num2,
                                            vector2_2, false);
                                    else
                                        SelectedShip.AI.OrderMoveTowardsPosition(ProjectedPosition, num2, vector2_2,
                                            false, null);
                                }
                                else if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftAlt))
                                    SelectedShip.AI.OrderMoveDirectlyTowardsPosition(ProjectedPosition, num2, vector2_2,
                                        true);
                                else
                                    SelectedShip.AI.OrderMoveTowardsPosition(ProjectedPosition, num2, vector2_2, true,
                                        null);
                            }
                        }
                        else if (SelectedShipList.Count > 0)
                        {
                            SelectedSomethingTimer = 3f;
                            foreach (Ship ship in SelectedShipList)
                            {
                                if (ship.loyalty != player)
                                    return;
                                if (ship.isConstructor || ship.shipData.Role == ShipData.RoleName.supply)
                                {
                                    SelectedShipList.Clear();
                                    AudioManager.PlayCue("UI_Misc20");
                                    return;
                                }
                            }
                            AudioManager.PlayCue("echo_affirm1");
                            endDragWorld = UnprojectToWorldPosition(input.CursorPosition);
                            Vector2 fVec = new Vector2(-vector2_2.Y, vector2_2.X);
                            float num3 = Vector2.Distance(endDragWorld, startDragWorld);
                            int num4 = 0;
                            int num5 = 0;
                            float num6 = 0.0f;
                            for (int i = 0; i < SelectedShipList.Count; ++i)
                            {
                                player.GetGSAI().DefensiveCoordinator.Remove(SelectedShipList[i]);
                                if (SelectedShipList[i].GetSO().WorldBoundingSphere.Radius > num6)
                                    num6 = SelectedShipList[i].GetSO().WorldBoundingSphere.Radius;
                            }
                            Fleet fleet = new Fleet();
                            if (SelectedShipList.Count * num6 > num3)
                            {
                                for (int i = 0; i < SelectedShipList.Count; ++i)
                                {
                                    fleet.AddShip(SelectedShipList[i]);
                                    fleet.Ships[i].RelativeFleetOffset =
                                        new Vector2((num6 + 200f) * num5, num4 * (num6 + 200f));
                                    ++num5;
                                    if (SelectedShipList[i].RelativeFleetOffset.X + num6 > num3)
                                    {
                                        num5 = 0;
                                        ++num4;
                                    }
                                }
                            }
                            else
                            {
                                float num7 = num3 / SelectedShipList.Count;
                                for (int i = 0; i < SelectedShipList.Count; ++i)
                                {
                                    fleet.AddShip(SelectedShipList[i]);
                                    fleet.Ships[i].RelativeFleetOffset = new Vector2(num7 * i, 0.0f);
                                }
                            }
                            fleet.ProjectPos(ProjectedPosition, num2 - 1.570796f, fVec);
                            foreach (Ship ship1 in fleet.Ships)
                            {
                                foreach (Ship ship2 in SelectedShipList)
                                {
                                    if (ship1.guid == ship2.guid)
                                    {
                                        if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
                                        {
                                            if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftAlt))
                                                ship2.AI.OrderMoveDirectlyTowardsPosition(ship1.projectedPosition,
                                                    num2 - 1.570796f, fVec, false);
                                            else
                                                ship2.AI.OrderMoveTowardsPosition(ship1.projectedPosition,
                                                    num2 - 1.570796f, fVec, false, null);
                                        }
                                        else if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftAlt))
                                            ship2.AI.OrderMoveDirectlyTowardsPosition(ship1.projectedPosition,
                                                num2 - 1.570796f, fVec, true);
                                        else
                                            ship2.AI.OrderMoveTowardsPosition(ship1.projectedPosition, num2 - 1.570796f,
                                                fVec, true, null);
                                    }
                                }
                            }
                            projectedGroup = fleet;
                        }
                    }
                }
                if (input.CurrentMouseState.RightButton == ButtonState.Pressed &&
                    input.LastMouseState.RightButton == ButtonState.Pressed)
                {
                    var target = new Vector2(input.CurrentMouseState.X, input.CurrentMouseState.Y);
                    float facing = startDrag.RadiansToTarget(target);
                    Vector2 fVec1 = Vector2.Normalize(target - startDrag);
                    if (input.RightMouseTimer > 0.0)
                        return;
                    ProjectingPosition = true;
                    if (SelectedFleet != null && SelectedFleet.Owner == player)
                    {
                        ProjectingPosition = true;
                        SelectedFleet.ProjectPos(ProjectedPosition, facing, fVec1);
                        projectedGroup = SelectedFleet;
                    }
                    else if (SelectedShip != null && SelectedShip.loyalty == player)
                    {
                        if (SelectedShip.isConstructor || SelectedShip.shipData.Role == ShipData.RoleName.supply)
                        {
                            if (SelectedShip != null && previousSelection != SelectedShip) //fbedard
                                previousSelection = SelectedShip;
                            SelectedShip = null;
                            AudioManager.PlayCue("UI_Misc20");
                        }
                        else
                        {
                            ShipGroup shipGroup = new ShipGroup();
                            shipGroup.Ships.Add(SelectedShip);
                            shipGroup.ProjectPos(ProjectedPosition, facing, fVec1);
                            projectedGroup = shipGroup;
                        }
                    }
                    else
                    {
                        if (SelectedShipList.Count <= 0)
                            return;
                        foreach (Ship ship in SelectedShipList)
                        {
                            if (ship.loyalty != player)
                                return;
                        }
                        endDragWorld = UnprojectToWorldPosition(input.CursorPosition);
                        Vector2 fVec2 = new Vector2(-fVec1.Y, fVec1.X);
                        float num1 = Vector2.Distance(endDragWorld, startDragWorld);
                        int num2 = 0;
                        int num3 = 0;
                        float num4 = 0.0f;
                        for (int i = 0; i < SelectedShipList.Count; ++i)
                        {
                            if (SelectedShipList[i].GetSO().WorldBoundingSphere.Radius > num4)
                                num4 = SelectedShipList[i].GetSO().WorldBoundingSphere.Radius;
                        }
                        Fleet fleet = new Fleet();
                        if (SelectedShipList.Count * num4 > num1)
                        {
                            for (int i = 0; i < SelectedShipList.Count; ++i)
                            {
                                fleet.AddShip(SelectedShipList[i]);
                                fleet.Ships[i].RelativeFleetOffset =
                                    new Vector2((num4 + 200f) * num3, num2 * (num4 + 200f));
                                ++num3;
                                if (SelectedShipList[i].RelativeFleetOffset.X + num4 > num1)
                                {
                                    num3 = 0;
                                    ++num2;
                                }
                            }
                        }
                        else
                        {
                            float num5 = num1 / SelectedShipList.Count;
                            for (int i = 0; i < SelectedShipList.Count; ++i)
                            {
                                fleet.AddShip(SelectedShipList[i]);
                                fleet.Ships[i].RelativeFleetOffset = new Vector2(num5 * i, 0.0f);
                            }
                        }
                        fleet.ProjectPos(ProjectedPosition, facing - 1.570796f, fVec2);
                        projectedGroup = fleet;
                    }
                }
                else
                    ProjectingPosition = false;
            }
        }

        private void HandleSelectionBox(InputState input)
        {
            if (this.LookingAtPlanet)
                return;
            if (this.SelectedShip != null && this.SelectedShip.Mothership == null &&
                !this.SelectedShip.isConstructor) //fbedard: prevent hangar ship and constructor
            {
                //if (input.CurrentKeyboardState.IsKeyDown(Keys.R) && !input.LastKeyboardState.IsKeyDown(Keys.R))  //fbedard: what is that !!!!
                //    this.SelectedShip.FightersOut = !this.SelectedShip.FightersOut;
                if (input.CurrentKeyboardState.IsKeyDown(Keys.Q) && !input.LastKeyboardState.IsKeyDown(Keys.Q))
                {
                    if (!this.pieMenu.Visible)
                    {
                        if (this.SelectedShip != null)
                            this.LoadShipMenuNodes(this.SelectedShip.loyalty == this.player ? 1 : 0);
                        this.pieMenu.RootNode = this.shipMenu;
                        this.pieMenu.Show(this.pieMenu.Position);
                    }
                    else
                        this.pieMenu.ChangeTo((PieMenuNode) null);
                }
            }
            Vector2 vector2 = input.CursorPosition - this.pieMenu.Position;
            vector2.Y *= -1f;
            Vector2 selectionVector = vector2 / this.pieMenu.Radius;
            this.pieMenu.HandleInput(input, selectionVector);
            if (input.CurrentMouseState.LeftButton == ButtonState.Pressed &&
                input.LastMouseState.LeftButton == ButtonState.Released && !this.pieMenu.Visible)
            {
                if (this.SelectedShip != null && this.previousSelection != this.SelectedShip) //fbedard
                    this.previousSelection = this.SelectedShip;
                this.SelectedShip = (Ship) null;
                this.SelectedPlanet = (Planet) null;
                this.SelectedFleet = (Fleet) null;
                this.SelectedSystem = (SolarSystem) null;
                this.SelectedItem = (UniverseScreen.ClickableItemUnderConstruction) null;
                this.ProjectingPosition = false;
                this.projectedGroup = (ShipGroup) null;
                bool flag1 = false;
                if (this.viewState >= UniverseScreen.UnivScreenState.SectorView)
                {
                    lock (GlobalStats.ClickableSystemsLock)
                    {
                        for (int local_2 = 0; local_2 < this.ClickableSystems.Count; ++local_2)
                        {
                            UniverseScreen.ClickableSystem local_3 = this.ClickableSystems[local_2];
                            if ((double) Vector2.Distance(input.CursorPosition, local_3.ScreenPos) <=
                                (double) local_3.Radius)
                            {
                                AudioManager.PlayCue("mouse_over4");
                                this.SelectedSystem = local_3.systemToClick;
                                this.sInfoUI.SetSystem(this.SelectedSystem);
                                flag1 = true;
                            }
                        }
                    }
                }
                bool flag2 = false;
                if (!flag1)
                {
                    foreach (UniverseScreen.ClickableFleet clickableFleet in this.ClickableFleetsList)
                    {
                        if ((double) Vector2.Distance(input.CursorPosition, clickableFleet.ScreenPos) <=
                            (double) clickableFleet.ClickRadius)
                        {
                            this.SelectedShipList.Clear();
                            this.SelectedFleet = clickableFleet.fleet;
                            flag2 = true;
                            this.pickedSomethingThisFrame = true;
                            AudioManager.PlayCue("techy_affirm1");
                            SelectedShipList.AddRange(SelectedFleet.Ships);
                            break;
                        }
                    }
                    if (!flag2)
                    {
                        foreach (UniverseScreen.ClickableShip clickableShip in this.ClickableShipsList)
                        {
                            if ((double) Vector2.Distance(input.CursorPosition, clickableShip.ScreenPos) <=
                                (double) clickableShip.Radius)
                            {
                                if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftControl) &&
                                    this.SelectedShipList.Count > 1 &&
                                    this.SelectedShipList.Contains(clickableShip.shipToClick))
                                {
                                    this.SelectedShipList.Remove(clickableShip.shipToClick);
                                    this.pickedSomethingThisFrame = true;
                                    AudioManager.GetCue("techy_affirm1").Play();
                                    break;
                                }
                                else
                                {
                                    if (this.SelectedShipList.Count > 0 &&
                                        !input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift) &&
                                        !this.pickedSomethingThisFrame)
                                        this.SelectedShipList.Clear();
                                    this.pickedSomethingThisFrame = true;
                                    AudioManager.GetCue("techy_affirm1").Play();
                                    //this.SelectedShip = clickableShip.shipToClick;  removed by fbedard
                                    SelectedSomethingTimer = 3f;
                                    if (clickableShip.shipToClick?.inSensorRange == true)
                                    {
                                        SelectedShipList.AddUnique(clickableShip.shipToClick);
                                    }
                                    break;
                                }
                            }
                        }
                        if (this.SelectedShip != null && this.SelectedShipList.Count == 1)
                            this.ShipInfoUIElement.SetShip(this.SelectedShip);
                        else if (this.SelectedShipList.Count > 1)
                            this.shipListInfoUI.SetShipList((Array<Ship>) this.SelectedShipList, false);
                        bool flag3 = false;
                        if (this.SelectedShipList.Count == 1)
                        {
                            if (this.SelectedShipList[0] == this.playerShip)
                                this.LoadShipMenuNodes(1);
                            else if (this.SelectedShipList[0].loyalty == this.player)
                                this.LoadShipMenuNodes(1);
                            else
                                this.LoadShipMenuNodes(0);
                        }
                        else
                        {
                            lock (GlobalStats.ClickableSystemsLock)
                            {
                                foreach (UniverseScreen.ClickablePlanets item_2 in this.ClickPlanetList)
                                {
                                    if ((double) Vector2.Distance(input.CursorPosition, item_2.ScreenPos) <=
                                        (double) item_2.Radius)
                                    {
                                        if ((double) this.ClickTimer2 < (double) this.TimerDelay)
                                        {
                                            this.SelectedPlanet = item_2.planetToClick;
                                            this.pInfoUI.SetPlanet(this.SelectedPlanet);
                                            this.SelectedSomethingTimer = 3f;
                                            flag3 = true;
                                            this.ViewPlanet((object) null);
                                            this.SelectionBox = new Rectangle();
                                        }
                                        else
                                        {
                                            AudioManager.GetCue("techy_affirm1").Play();
                                            this.SelectedPlanet = item_2.planetToClick;
                                            this.pInfoUI.SetPlanet(this.SelectedPlanet);
                                            this.SelectedSomethingTimer = 3f;
                                            flag3 = true;
                                            this.ClickTimer2 = 0.0f;
                                        }
                                    }
                                }
                            }
                        }
                        if (!flag3)
                        {
                            lock (GlobalStats.ClickableItemLocker)
                            {
                                for (int local_17 = 0; local_17 < this.ItemsToBuild.Count; ++local_17)
                                {
                                    UniverseScreen.ClickableItemUnderConstruction local_18 =
                                        this.ItemsToBuild[local_17];
                                    if (local_18 != null &&
                                        (double) Vector2.Distance(input.CursorPosition, local_18.ScreenPos) <=
                                        (double) local_18.Radius)
                                    {
                                        AudioManager.GetCue("techy_affirm1").Play();
                                        this.SelectedItem = local_18;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if (this.SelectedShip == null && this.SelectedShipList.Count == 0 &&
                (this.SelectedPlanet != null && input.CurrentKeyboardState.IsKeyDown(Keys.Q)) &&
                !input.LastKeyboardState.IsKeyDown(Keys.Q))
            {
                if (!this.pieMenu.Visible)
                {
                    this.pieMenu.RootNode = this.planetMenu;
                    if (this.SelectedPlanet.Owner == null && this.SelectedPlanet.habitable)
                        this.LoadMenuNodes(false, true);
                    else
                        this.LoadMenuNodes(false, false);
                    this.pieMenu.Show(this.pieMenu.Position);
                }
                else
                    this.pieMenu.ChangeTo((PieMenuNode) null);
            }
            if (input.CurrentMouseState.LeftButton == ButtonState.Pressed &&
                input.LastMouseState.LeftButton == ButtonState.Released)
                this.SelectionBox = new Rectangle(input.CurrentMouseState.X, input.CurrentMouseState.Y, 0, 0);
            if (this.SelectedShipList.Count == 1)
            {
                if (this.SelectedShip != null && this.previousSelection != this.SelectedShip &&
                    this.SelectedShip != this.SelectedShipList[0]) //fbedard
                    this.previousSelection = this.SelectedShip;
                this.SelectedShip = this.SelectedShipList[0];
            }
            if (input.CurrentMouseState.LeftButton == ButtonState.Pressed)
            {
                this.SelectingWithBox = true;
                if (this.SelectionBox.X == 0 || this.SelectionBox.Y == 0)
                    return;
                this.SelectionBox = new Rectangle(this.SelectionBox.X, this.SelectionBox.Y,
                    input.CurrentMouseState.X - this.SelectionBox.X, input.CurrentMouseState.Y - this.SelectionBox.Y);
            }
            else if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift) &&
                     input.CurrentMouseState.LeftButton == ButtonState.Released &&
                     input.LastMouseState.LeftButton == ButtonState.Pressed)
            {
                if (input.CurrentMouseState.X < this.SelectionBox.X)
                    this.SelectionBox.X = input.CurrentMouseState.X;
                if (input.CurrentMouseState.Y < this.SelectionBox.Y)
                    this.SelectionBox.Y = input.CurrentMouseState.Y;
                this.SelectionBox.Width = Math.Abs(this.SelectionBox.Width);
                this.SelectionBox.Height = Math.Abs(this.SelectionBox.Height);
                bool flag1 = true;
                Array<Ship> list = new Array<Ship>();
                foreach (UniverseScreen.ClickableShip clickableShip in this.ClickableShipsList)
                {
                    if (this.SelectionBox.Contains(
                            new Point((int) clickableShip.ScreenPos.X, (int) clickableShip.ScreenPos.Y)) &&
                        !this.SelectedShipList.Contains(clickableShip.shipToClick))
                    {
                        this.SelectedPlanet = (Planet) null;
                        this.SelectedShipList.Add(clickableShip.shipToClick);
                        this.SelectedSomethingTimer = 3f;
                        list.Add(clickableShip.shipToClick);
                    }
                }
                if (this.SelectedShipList.Count > 0 && flag1)
                {
                    bool flag2 = false;
                    bool flag3 = false;
                    foreach (Ship ship in list)
                    {
                        if (ship.shipData.Role <= ShipData.RoleName.supply)
                            flag2 = true;
                        else
                            flag3 = true;
                    }
                    if (flag3 && flag2)
                    {
                        foreach (Ship ship in (Array<Ship>) this.SelectedShipList)
                        {
                            if (ship.shipData.Role <= ShipData.RoleName.supply)
                                this.SelectedShipList.QueuePendingRemoval(ship);
                        }
                    }
                    this.SelectedShipList.ApplyPendingRemovals();
                }
                if (this.SelectedShipList.Count > 1)
                {
                    bool flag2 = false;
                    bool flag3 = false;
                    foreach (Ship ship in (Array<Ship>) this.SelectedShipList)
                    {
                        if (ship.loyalty == this.player)
                            flag2 = true;
                        if (ship.loyalty != this.player)
                            flag3 = true;
                    }
                    if (flag2 && flag3)
                    {
                        foreach (Ship ship in (Array<Ship>) this.SelectedShipList)
                        {
                            if (ship.loyalty != this.player)
                                this.SelectedShipList.QueuePendingRemoval(ship);
                        }
                        this.SelectedShipList.ApplyPendingRemovals();
                    }
                    if (this.SelectedShip != null && this.previousSelection != this.SelectedShip) //fbedard
                        this.previousSelection = this.SelectedShip;
                    this.SelectedShip = (Ship) null;
                    //this.shipListInfoUI.SetShipList((Array<Ship>)this.SelectedShipList, true);
                    this.shipListInfoUI.SetShipList((Array<Ship>) this.SelectedShipList,
                        false); //fbedard: this is not a fleet!
                }
                else if (this.SelectedShipList.Count == 1)
                {
                    if (this.SelectedShip != null && this.previousSelection != this.SelectedShip &&
                        this.SelectedShip != this.SelectedShipList[0]) //fbedard
                        this.previousSelection = this.SelectedShip;
                    this.SelectedShip = this.SelectedShipList[0];
                    this.ShipInfoUIElement.SetShip(this.SelectedShip);
                }
                this.SelectionBox = new Rectangle(0, 0, -1, -1);
            }
            else
            {
                if (input.CurrentMouseState.LeftButton != ButtonState.Released ||
                    input.LastMouseState.LeftButton != ButtonState.Pressed)
                    return;
                this.SelectingWithBox = false;
                if (input.CurrentMouseState.X < this.SelectionBox.X)
                    this.SelectionBox.X = input.CurrentMouseState.X;
                if (input.CurrentMouseState.Y < this.SelectionBox.Y)
                    this.SelectionBox.Y = input.CurrentMouseState.Y;
                this.SelectionBox.Width = Math.Abs(this.SelectionBox.Width);
                this.SelectionBox.Height = Math.Abs(this.SelectionBox.Height);
                bool flag1 = false;
                if (this.SelectedShipList.Count == 0)
                    flag1 = true;
                foreach (UniverseScreen.ClickableShip clickableShip in this.ClickableShipsList)
                {
                    if (this.SelectionBox.Contains(
                        new Point((int) clickableShip.ScreenPos.X, (int) clickableShip.ScreenPos.Y)))
                    {
                        this.SelectedPlanet = (Planet) null;
                        this.SelectedShipList.Add(clickableShip.shipToClick);
                        this.SelectedSomethingTimer = 3f;
                    }
                }
                if (this.SelectedShipList.Count > 0 && flag1)
                {
                    bool flag2 = false;
                    bool flag3 = false;
                    try
                    {
                        foreach (Ship ship in (Array<Ship>) this.SelectedShipList)
                        {
                            if (ship.shipData.Role <= ShipData.RoleName.freighter ||
                                ship.shipData.ShipCategory == ShipData.Category.Civilian ||
                                ship.AI.State == AIState.Colonize)
                                flag2 = true;
                            else
                                flag3 = true;
                        }
                    }
                    catch { }
                    if (flag3)
                    {
                        if (flag2)
                        {
                            try
                            {
                                foreach (Ship ship in (Array<Ship>) this.SelectedShipList)
                                {
                                    if (ship.shipData.Role <= ShipData.RoleName.freighter ||
                                        ship.shipData.ShipCategory == ShipData.Category.Civilian ||
                                        ship.AI.State == AIState.Colonize)
                                        this.SelectedShipList.QueuePendingRemoval(ship);
                                }
                            }
                            catch { }
                        }
                    }
                    this.SelectedShipList.ApplyPendingRemovals();
                }
                if (this.SelectedShipList.Count > 1)
                {
                    bool flag2 = false;
                    bool flag3 = false;
                    foreach (Ship ship in (Array<Ship>) this.SelectedShipList)
                    {
                        if (ship.loyalty == this.player)
                            flag2 = true;
                        if (ship.loyalty != this.player)
                            flag3 = true;
                    }
                    if (flag2 && flag3)
                    {
                        foreach (Ship ship in (Array<Ship>) this.SelectedShipList)
                        {
                            if (ship.loyalty != this.player)
                                this.SelectedShipList.QueuePendingRemoval(ship);
                        }
                        this.SelectedShipList.ApplyPendingRemovals();
                    }
                    if (this.SelectedShip != null && this.previousSelection != this.SelectedShip) //fbedard
                        this.previousSelection = this.SelectedShip;
                    this.SelectedShip = (Ship) null;
                    bool flag4 = true;
                    if (this.SelectedShipList.Count > 0)
                    {
                        if (this.SelectedShipList[0].fleet != null)
                        {
                            if (this.SelectedShipList.Count == this.SelectedShipList[0].fleet.Ships.Count)
                            {
                                try
                                {
                                    foreach (Ship ship in SelectedShipList)
                                    {
                                        if (ship.fleet == null || ship.fleet != this.SelectedShipList[0].fleet)
                                            flag4 = false;
                                    }
                                    if (flag4)
                                        this.SelectedFleet = this.SelectedShipList[0].fleet;
                                }
                                catch { }
                            }
                        }
                        if (this.SelectedFleet != null)
                            this.shipListInfoUI.SetShipList(SelectedShipList, true);
                        else
                            this.shipListInfoUI.SetShipList(SelectedShipList, false);
                    }
                    if (this.SelectedFleet == null)
                        this.ShipInfoUIElement.SetShip(this.SelectedShipList[0]);
                }
                else if (this.SelectedShipList.Count == 1)
                {
                    if (this.SelectedShip != null && this.previousSelection != this.SelectedShip &&
                        this.SelectedShip != this.SelectedShipList[0]) //fbedard
                        this.previousSelection = this.SelectedShip;
                    this.SelectedShip = this.SelectedShipList[0];
                    this.ShipInfoUIElement.SetShip(this.SelectedShip);
                    if (this.SelectedShipList[0] == this.playerShip)
                        this.LoadShipMenuNodes(1);
                    else if (this.SelectedShipList[0].loyalty == this.player)
                        this.LoadShipMenuNodes(1);
                    else
                        this.LoadShipMenuNodes(0);
                }
                this.SelectionBox = new Rectangle(0, 0, -1, -1);
            }
        }

        private void RightClickship(Ship ship, Planet planet, bool audio)
        {
            if (ship.isConstructor)
            {
                if (!audio)
                    return;
                AudioManager.PlayCue("UI_Misc20");
            }
            else
            {
                if (audio)
                    AudioManager.PlayCue("echo_affirm1");
                if (ship.isColonyShip)
                {
                    if (planet.Owner == null && planet.habitable)
                        ship.AI.OrderColonization(planet);
                    else
                        ship.AI.OrderToOrbit(planet, true);
                }
                else if (ship.shipData.Role == ShipData.RoleName.troop || (ship.TroopList.Count > 0 && (ship.HasTroopBay || ship.hasTransporter)))
                {
                    if (planet.Owner != null && planet.Owner == this.player && (!ship.HasTroopBay && !ship.hasTransporter))
                    {
                        if (Input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
                            ship.AI.OrderToOrbit(planet, false);
                        else
                            ship.AI.OrderRebase(planet, true);
                    }
                    else if (planet.habitable && (planet.Owner == null || planet.Owner != player && (ship.loyalty.GetRelations(planet.Owner).AtWar || planet.Owner.isFaction || planet.Owner.data.Defeated)))
                    {
                        //add new right click troop and troop ship options on planets
                        if (Input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
                            ship.AI.OrderToOrbit(planet, false);
                        else
                        {
                            ship.AI.State = AIState.AssaultPlanet;
                            ship.AI.OrderLandAllTroops(planet);
                        }
                    }
                    else
                    {
                        ship.AI.OrderOrbitPlanet(planet);// OrderRebase(planet, true);
                    }
                }
                else if (ship.BombBays.Count > 0)
                {
                    float enemies = planet.GetGroundStrengthOther(this.player) * 1.5f;
                    float friendlies = planet.GetGroundStrength(this.player);
                    if (planet.Owner != this.player)
                    {
                        if (planet.Owner == null || this.player.GetRelations(planet.Owner).AtWar || planet.Owner.isFaction || planet.Owner.data.Defeated)
                        {
                            if (Input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
                                ship.AI.OrderBombardPlanet(planet);
                            else if (enemies > friendlies || planet.Population > 0f)
                                ship.AI.OrderBombardPlanet(planet);
                            else
                            {
                                ship.AI.OrderToOrbit(planet, false);
                            }
                        }
                        else
                        {
                            ship.AI.OrderToOrbit(planet, false);
                        }


                    }
                    else if (enemies > friendlies && Input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
                    {
                        ship.AI.OrderBombardPlanet(planet);
                    }
                    else
                        ship.AI.OrderToOrbit(planet, true);
                }
                else if (Input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
                    ship.AI.OrderToOrbit(planet, false);
                else
                    ship.AI.OrderToOrbit(planet, true);
            }
                            



        }

        public void UpdateClickableItems()
        {
            lock (GlobalStats.ClickableItemLocker)
                this.ItemsToBuild.Clear();
            for (int index = 0; index < EmpireManager.Player.GetGSAI().Goals.Count; ++index)
            {
                Goal goal = player.GetGSAI().Goals[index];
                if (goal.GoalName == "BuildConstructionShip")
                {
                    float radius = 100f;
                    Vector3 vector3_1 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(goal.BuildPosition, 0.0f), this.projection, this.view, Matrix.Identity);
                    Vector2 vector2 = new Vector2(vector3_1.X, vector3_1.Y);
                    Vector3 vector3_2 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(goal.BuildPosition.PointOnCircle(90f, radius), 0.0f), this.projection, this.view, Matrix.Identity);
                    float num = Vector2.Distance(new Vector2(vector3_2.X, vector3_2.Y), vector2) + 10f;
                    ClickableItemUnderConstruction underConstruction = new ClickableItemUnderConstruction
                    {
                        Radius         = num,
                        BuildPos       = goal.BuildPosition,
                        ScreenPos      = vector2,
                        UID            = goal.ToBuildUID,
                        AssociatedGoal = goal
                    };
                    lock (GlobalStats.ClickableItemLocker)
                        ItemsToBuild.Add(underConstruction);
                }
            }
        }

        private void DefineAO(InputState input)
        {
            this.HandleScrolls(input);
            if (this.SelectedShip == null)
            {
                this.DefiningAO = false;
            }
            else
            {
                if (input.Escaped)      //Easier out from defining an AO. Used to have to left and Right click at the same time.    -Gretman
                {
                    this.DefiningAO = false;
                    return;
                }
                Vector3 position = this.ScreenManager.GraphicsDevice.Viewport.Unproject(new Vector3((float)input.CurrentMouseState.X, (float)input.CurrentMouseState.Y, 0.0f), this.projection, this.view, Matrix.Identity);
                Vector3 direction = this.ScreenManager.GraphicsDevice.Viewport.Unproject(new Vector3((float)input.CurrentMouseState.X, (float)input.CurrentMouseState.Y, 1f), this.projection, this.view, Matrix.Identity) - position;
                direction.Normalize();
                Ray ray = new Ray(position, direction);
                float num = -ray.Position.Z / ray.Direction.Z;
                Vector3 vector3 = new Vector3(ray.Position.X + num * ray.Direction.X, ray.Position.Y + num * ray.Direction.Y, 0.0f);
                if (input.CurrentMouseState.LeftButton == ButtonState.Pressed && input.LastMouseState.LeftButton == ButtonState.Released)
                    this.AORect = new Rectangle((int)vector3.X, (int)vector3.Y, 0, 0);
                if (input.CurrentMouseState.LeftButton == ButtonState.Pressed)
                {
                    this.AORect = new Rectangle(this.AORect.X, this.AORect.Y, (int)vector3.X - this.AORect.X, (int)vector3.Y - this.AORect.Y);
                }
                if (input.CurrentMouseState.LeftButton == ButtonState.Released && input.LastMouseState.LeftButton == ButtonState.Pressed)
                {
                    if (this.AORect.X > vector3.X)
                        this.AORect.X = (int)vector3.X;
                    if (this.AORect.Y > vector3.Y)
                        this.AORect.Y = (int)vector3.Y;
                    this.AORect.Width = Math.Abs(this.AORect.Width);
                    this.AORect.Height = Math.Abs(this.AORect.Height);
                    if (this.AORect.Width > 100 && this.AORect.Height > 100)
                    {
                        AudioManager.PlayCue("echo_affirm");
                        this.SelectedShip.AreaOfOperation.Add(this.AORect);
                    }
                }
                for (int index = 0; index < this.SelectedShip.AreaOfOperation.Count; ++index)
                {
                    if (HelperFunctions.CheckIntersection(this.SelectedShip.AreaOfOperation[index], new Vector2(vector3.X, vector3.Y)) && input.CurrentMouseState.RightButton == ButtonState.Pressed && input.LastMouseState.RightButton == ButtonState.Released)
                        this.SelectedShip.AreaOfOperation.Remove(this.SelectedShip.AreaOfOperation[index]);
                }
            }
        }

        private void InputClickableItems(InputState input)
        {
            if (ClickTimer >= TimerDelay)
            {
                if (SelectedShip != null)
                    ClickTimer = 0.0f;
                return;
            }
            SelectedShipList.Clear();
            if (SelectedShip != null && previousSelection != SelectedShip) //fbedard
                previousSelection = SelectedShip;
            SelectedShip = null;
            if (viewState <= UnivScreenState.SystemView)
            {
                foreach (ClickablePlanets clickablePlanets in ClickPlanetList)
                {
                    if (input.CursorPosition.InRadius(clickablePlanets.ScreenPos, clickablePlanets.Radius))
                    {
                        AudioManager.PlayCue("sub_bass_whoosh");
                        SelectedPlanet = clickablePlanets.planetToClick;
                        if (!SnapBackToSystem)
                            HeightOnSnap = camHeight;
                        ViewPlanet(SelectedPlanet);
                    }
                }
            }
            foreach (ClickableShip clickableShip in ClickableShipsList)
            {
                if (input.CursorPosition.InRadius(clickableShip.ScreenPos, clickableShip.Radius))
                {
                    pickedSomethingThisFrame = true;
                    SelectedShipList.AddUnique(clickableShip.shipToClick);

                    foreach (ClickableShip ship in ClickableShipsList)
                    {
                        if (clickableShip.shipToClick != ship.shipToClick &&
                            ship.shipToClick.loyalty == clickableShip.shipToClick.loyalty &&
                            ship.shipToClick.shipData.Role == clickableShip.shipToClick.shipData.Role)
                        {
                            SelectedShipList.AddUnique(ship.shipToClick);
                        }
                    }
                    break;
                }
            }
            if (viewState > UnivScreenState.SystemView)
            {
                for (int i = 0; i < ClickableSystems.Count; ++i)
                {
                    ClickableSystem system = ClickableSystems[i];
                    if (input.CursorPosition.InRadius(system.ScreenPos, system.Radius))
                    {
                        if (system.systemToClick.ExploredDict[player])
                        {
                            AudioManager.GetCue("sub_bass_whoosh").Play();
                            HeightOnSnap = camHeight;
                            ViewSystem(system.systemToClick);
                        }
                        else
                            PlayNegativeSound();
                    }
                }
            }
        }

        private void PreviousTargetSelection(InputState input)
        {
            if (previousSelection.Active)
            {
                Ship tempship = previousSelection;
                if (SelectedShip != null && SelectedShip != previousSelection)
                    previousSelection = SelectedShip;
                SelectedShip = tempship;
                ShipInfoUIElement.SetShip(SelectedShip);
                SelectedFleet  = null;
                SelectedItem   = null;
                SelectedSystem = null;
                SelectedPlanet = null;
                SelectedShipList.Clear();
                SelectedShipList.Add(SelectedShip);
                ViewingShip = false;
                return;
            }
            else
                previousSelection = null;  //fbedard: remove inactive ship
        }

        private void CycleShipsInCombat(InputState input)
        {
            ShipsInCombat.State = UIButton.PressState.Hover;
            ToolTip.CreateTooltip("Cycle through ships not in fleet that are in combat", ScreenManager);
            if (input.InGameSelect)
            {
                if (player.empireShipCombat > 0)
                {
                    AudioManager.PlayCue("echo_affirm");
                    int nbrship = 0;
                    if (lastshipcombat >= player.empireShipCombat)
                        lastshipcombat = 0;
                    foreach (Ship ship in EmpireManager.Player.GetShips())
                    {
                        if (ship.fleet != null || !ship.InCombat || ship.Mothership != null || !ship.Active)
                            continue;
                        else
                        {
                            if (nbrship == lastshipcombat)
                            {
                                if (SelectedShip != null && SelectedShip != previousSelection && SelectedShip != ship)
                                    previousSelection = SelectedShip;
                                SelectedShip = ship;
                                ViewToShip(null);
                                SelectedShipList.Add(SelectedShip);
                                lastshipcombat++;
                                break;
                            }
                            else nbrship++;
                        }
                    }
                }
                else
                {
                    AudioManager.PlayCue("blip_click");
                }
            }
        }

        private void CyclePlanetsInCombat(InputState input)
        {
            PlanetsInCombat.State = UIButton.PressState.Hover;
            ToolTip.CreateTooltip("Cycle through planets that are in combat", ScreenManager);
            if (input.InGameSelect)
            {
                if (player.empirePlanetCombat > 0)
                {
                    AudioManager.PlayCue("echo_affirm");
                    Planet PlanetToView = (Planet)null;
                    int nbrplanet = 0;
                    if (lastplanetcombat >= player.empirePlanetCombat)
                        lastplanetcombat = 0;
                    bool flagPlanet;

                    foreach (SolarSystem system in UniverseScreen.SolarSystemList)
                    {
                        foreach (Planet p in system.PlanetList)
                        {
                            if (p.IsExploredBy(EmpireManager.Player) && p.RecentCombat)
                            {
                                if (p.Owner == Empire.Universe.PlayerEmpire)
                                {
                                    if (nbrplanet == lastplanetcombat)
                                        PlanetToView = p;
                                    nbrplanet++;
                                }
                                else
                                {
                                    flagPlanet = false;
                                    foreach (Troop troop in p.TroopsHere)
                                    {
                                        if (troop.GetOwner() != null && troop.GetOwner() == Empire.Universe.PlayerEmpire)
                                        {
                                            flagPlanet = true;
                                            break;
                                        }
                                    }
                                    if (flagPlanet)
                                    {
                                        if (nbrplanet == lastplanetcombat)
                                            PlanetToView = p;
                                        nbrplanet++;
                                    }
                                }
                            }
                        }
                    }
                    if (PlanetToView != null)
                    {
                        if (SelectedShip != null && previousSelection != SelectedShip) //fbedard
                            previousSelection = SelectedShip;
                        SelectedShip = (Ship)null;
                        SelectedFleet = (Fleet)null;
                        SelectedItem = (UniverseScreen.ClickableItemUnderConstruction)null;
                        SelectedSystem = (SolarSystem)null;
                        SelectedPlanet = PlanetToView;
                        SelectedShipList.Clear();
                        pInfoUI.SetPlanet(PlanetToView);
                        lastplanetcombat++;

                        transitionDestination = new Vector3(SelectedPlanet.Position.X, SelectedPlanet.Position.Y, 9000f);
                        LookingAtPlanet = false;
                        transitionStartPosition = camPos;
                        AdjustCamTimer = 2f;
                        transitionElapsedTime = 0.0f;
                        transDuration = 5f;
                        returnToShip = false;
                        ViewingShip = false;
                        snappingToShip = false;
                        SelectedItem = (UniverseScreen.ClickableItemUnderConstruction)null;
                    }
                }
                else
                {
                    AudioManager.PlayCue("blip_click");
                }
            }
        }

        private void ResetToolTipTimer(ref bool toolTipToReset, float timer = 0.5f)
        {
            toolTipToReset = false;
            TooltipTimer = 0.5f;
        }

        private void InputCheckPreviousShip(Ship ship = null)
        {
            if (SelectedShip != null  && previousSelection != SelectedShip && SelectedShip != ship) //fbedard
                previousSelection = SelectedShip;
        }

        private void HandleInputScrap(InputState input)
        {
            player.GetGSAI().Goals.QueuePendingRemoval(SelectedItem.AssociatedGoal);
            bool flag = false;
            foreach (Ship ship in player.GetShips())
            {
                if (ship.isConstructor && ship.AI.OrderQueue.NotEmpty)
                {
                    for (int index = 0; index < ship.AI.OrderQueue.Count; ++index)
                    {
                        if (ship.AI.OrderQueue[index].goal == SelectedItem.AssociatedGoal)
                        {
                            flag = true;
                            ship.AI.OrderScrapShip();
                            break;
                        }
                    }
                }
            }
            if (!flag)
            {
                foreach (Planet planet in player.GetPlanets())
                {
                    foreach (QueueItem queueItem in planet.ConstructionQueue)
                    {
                        if (queueItem.Goal == SelectedItem.AssociatedGoal)
                        {
                            planet.ProductionHere += queueItem.productionTowards;
                            if ((double)planet.ProductionHere > (double)planet.MAX_STORAGE)
                                planet.ProductionHere = planet.MAX_STORAGE;
                            planet.ConstructionQueue.QueuePendingRemoval(queueItem);
                        }
                    }
                    planet.ConstructionQueue.ApplyPendingRemovals();
                }
            }
            lock (GlobalStats.ClickableItemLocker)
            {
                for (int local_10 = 0; local_10 < ItemsToBuild.Count; ++local_10)
                {
                    ClickableItemUnderConstruction local_11 = ItemsToBuild[local_10];
                    if (local_11.BuildPos == SelectedItem.BuildPos)
                    {
                        ItemsToBuild.QueuePendingRemoval(local_11);
                        AudioManager.PlayCue("blip_click");
                    }
                }
                ItemsToBuild.ApplyPendingRemovals();
            }
            player.GetGSAI().Goals.ApplyPendingRemovals();
            SelectedItem = null;
        }

        private void AddSelectedShipsToFleet(Fleet fleet)
        {
            foreach (Ship ship in SelectedShipList)
            {
                if (ship.loyalty == player && !ship.isConstructor && ship.Mothership == null && ship.fleet == null)  //fbedard: cannot add ships from hangar in fleet
                    fleet.Ships.Add(ship);
            }
            fleet.AutoArrange();
            InputCheckPreviousShip();

            SelectedShip = (Ship)null;
            SelectedShipList.Clear();
            if (fleet.Ships.Count > 0)
            {
                SelectedFleet = fleet;
                AudioManager.PlayCue("techy_affirm1");
            }
            else
                SelectedFleet = (Fleet)null;
            foreach (Ship ship in fleet.Ships)
            {
                SelectedShipList.Add(ship);
                ship.fleet = fleet;
            }
            RecomputeFleetButtons(true);
            shipListInfoUI.SetShipList(SelectedShipList, true);  //fbedard:display new fleet in UI            
        }

        public void RecomputeFleetButtons(bool now)
        {
            ++FBTimer;
            if (FBTimer <= 60 && !now)
                return;
            lock (GlobalStats.FleetButtonLocker)
            {
                int local_0 = 0;
                int local_1 = 60;
                int local_2 = 20;
                FleetButtons.Clear();
                foreach (KeyValuePair<int, Fleet> item_0 in player.GetFleetsDict())
                {
                    if (item_0.Value.Ships.Count > 0)
                    {
                        FleetButtons.Add(new FleetButton()
                        {
                            ClickRect = new Rectangle(local_2, local_1 + local_0 * local_1, 52, 48),
                            Fleet = item_0.Value,
                            Key = item_0.Key
                        });
                        ++local_0;
                    }
                }
                FBTimer = 0;
            }
        }

        private void HandleEdgeDetection(InputState input)
        {
            if (this.LookingAtPlanet || ViewingShip )
                return;
            PresentationParameters presentationParameters = this.ScreenManager.GraphicsDevice.PresentationParameters;
            Vector2 spaceFromScreenSpace1 = this.UnprojectToWorldPosition(new Vector2(0.0f, 0.0f));
            float num = this.UnprojectToWorldPosition(new Vector2((float)presentationParameters.BackBufferWidth, (float)presentationParameters.BackBufferHeight)).X - spaceFromScreenSpace1.X;
            input.Repeat = true;
            if (input.CursorPosition.X <= 1f || input.Left )
            {
                this.transitionDestination.X -= 0.008f * num;
                this.snappingToShip = false;
                this.ViewingShip = false;
            }
            if (input.CursorPosition.X >= (presentationParameters.BackBufferWidth - 1) || input.Right )
            {
                this.transitionDestination.X += 0.008f * num;
                this.snappingToShip = false;
                this.ViewingShip = false;
            }
            if (input.CursorPosition.Y <= 0.0f || input.Up )
            {
                this.snappingToShip = false;
                this.ViewingShip = false;
                this.transitionDestination.Y -= 0.008f * num;
            }
            if (input.CursorPosition.Y >= (presentationParameters.BackBufferHeight - 1) || input.Down )
            {
                this.transitionDestination.Y += 0.008f * num;
                this.snappingToShip = false;
                this.ViewingShip = false;
            }
            input.Repeat = false;
            //fbedard: remove middle button scrolling
            //if (input.CurrentMouseState.MiddleButton == ButtonState.Pressed)
            //{
            //    this.snappingToShip = false;
            //    this.ViewingShip = false;
            //}
            //if (input.CurrentMouseState.MiddleButton != ButtonState.Pressed || input.LastMouseState.MiddleButton != ButtonState.Released)
            //    return;
            //Vector2 spaceFromScreenSpace2 = this.GetWorldSpaceFromScreenSpace(input.CursorPosition);
            //this.transitionDestination.X = spaceFromScreenSpace2.X;
            //this.transitionDestination.Y = spaceFromScreenSpace2.Y;
            //this.transitionDestination.Z = this.camHeight;
            //this.AdjustCamTimer = 1f;
            //this.transitionElapsedTime = 0.0f;
        }

        private void HandleScrolls(InputState input)
        {
            if ((double)this.AdjustCamTimer >= 0.0)
                return;

            float scrollAmount = 1500.0f * camHeight / 3000.0f + 100.0f;

            if ((input.ScrollOut || input.BButtonHeld) && !this.LookingAtPlanet)
            {
                this.transitionDestination.X = this.camPos.X;
                this.transitionDestination.Y = this.camPos.Y;
                this.transitionDestination.Z = this.camHeight + scrollAmount;
                if ((double)this.camHeight > 12000.0)
                {
                    this.transitionDestination.Z += 3000f;
                    this.viewState = UniverseScreen.UnivScreenState.SectorView;
                    if ((double)this.camHeight > 32000.0)
                        this.transitionDestination.Z += 15000f;
                    if ((double)this.camHeight > 100000.0)
                        this.transitionDestination.Z += 40000f;
                }
                if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftControl))
                {
                    if ((double)this.camHeight < 55000.0)
                    {
                        this.transitionDestination.Z = 60000f;
                        this.AdjustCamTimer = 1f;
                        this.transitionElapsedTime = 0.0f;
                    }
                    else
                    {
                        this.transitionDestination.Z = 4200000f * this.GameScale;
                        this.AdjustCamTimer = 1f;
                        this.transitionElapsedTime = 0.0f;
                    }
                }
            }
            if (!input.YButtonHeld && !input.ScrollIn || this.LookingAtPlanet)
                return;

            this.transitionDestination.Z = this.camHeight - scrollAmount;
            if ((double)this.camHeight >= 16000.0)
            {
                this.transitionDestination.Z -= 2000f;
                if ((double)this.camHeight > 32000.0)
                    this.transitionDestination.Z -= 7500f;
                if ((double)this.camHeight > 150000.0)
                    this.transitionDestination.Z -= 40000f;
            }
            if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftControl) && (double)this.camHeight > 10000.0)
                this.transitionDestination.Z = (double)this.camHeight <= 65000.0 ? 10000f : 60000f;
            if (this.ViewingShip)
                return;
            if ((double)this.camHeight <= 450.0f)
                this.camHeight = 450f;
            float num2 = this.transitionDestination.Z;
            
            //fbedard: add a scroll on selected object
            if ((!input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift) && GlobalStats.ZoomTracking) || (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift) && !GlobalStats.ZoomTracking))
            {
                if (this.SelectedShip != null && this.SelectedShip.Active)
                {
                    this.transitionDestination = new Vector3(this.SelectedShip.Position.X, this.SelectedShip.Position.Y, num2);
                }
                else
                if (this.SelectedPlanet != null)
                {
                    this.transitionDestination = new Vector3(this.SelectedPlanet.Position.X, this.SelectedPlanet.Position.Y, num2);
                }  
                else
                if (this.SelectedFleet != null && this.SelectedFleet.Ships.Count > 0)
                {
                    this.transitionDestination = new Vector3(this.SelectedFleet.FindAveragePosition().X, this.SelectedFleet.FindAveragePosition().Y, num2);
                }
                else
                if (this.SelectedShipList.Count > 0 && this.SelectedShipList[0] != null && this.SelectedShipList[0].Active)
                {
                    this.transitionDestination = new Vector3(this.SelectedShipList[0].Position.X, this.SelectedShipList[0].Position.Y, num2);
                }
                else
                    this.transitionDestination = new Vector3(this.CalculateCameraPositionOnMouseZoom(new Vector2((float)input.CurrentMouseState.X, (float)input.CurrentMouseState.Y), num2), num2);
            }
            else
                this.transitionDestination = new Vector3(this.CalculateCameraPositionOnMouseZoom(new Vector2((float)input.CurrentMouseState.X, (float)input.CurrentMouseState.Y), num2), num2);
        }

        private void HandleScrollsSectorMiniMap(InputState input)
        {
            this.SectorMiniMapHeight = MathHelper.SmoothStep(this.SectorMiniMapHeight, this.desiredSectorZ, 0.2f);
            if ((double)this.SectorMiniMapHeight < 6000.0)
                this.SectorMiniMapHeight = 6000f;
            if (input.InGameSelect)
            {
                this.transitionDestination.Z = this.SectorMiniMapHeight;
                this.transitionDestination.X = this.playerShip.Center.X;
                this.transitionDestination.Y = this.playerShip.Center.Y;
            }
            if ((input.ScrollOut || input.BButtonHeld) && !this.LookingAtPlanet)
            {
                float num = (float)(1500.0 * (double)this.SectorMiniMapHeight / 3000.0 + 550.0);
                if ((double)this.SectorMiniMapHeight < 10000.0)
                    num -= 200f;
                this.desiredSectorZ = this.SectorMiniMapHeight + num;
                if ((double)this.SectorMiniMapHeight > 12000.0)
                {
                    this.desiredSectorZ += 3000f;
                    this.viewState = UniverseScreen.UnivScreenState.SectorView;
                    if ((double)this.SectorMiniMapHeight > 32000.0)
                        this.desiredSectorZ += 15000f;
                    if ((double)this.SectorMiniMapHeight > 100000.0)
                        this.desiredSectorZ += 40000f;
                }
            }
            if ((input.YButtonHeld || input.ScrollIn) && !this.LookingAtPlanet)
            {
                float num = (float)(1500.0 * (double)this.SectorMiniMapHeight / 3000.0 + 550.0);
                if ((double)this.SectorMiniMapHeight < 10000.0)
                    num -= 200f;
                this.desiredSectorZ = this.SectorMiniMapHeight - num;
                if ((double)this.SectorMiniMapHeight >= 16000.0)
                {
                    this.desiredSectorZ -= 3000f;
                    if ((double)this.SectorMiniMapHeight > 32000.0)
                        this.desiredSectorZ -= 7500f;
                    if ((double)this.SectorMiniMapHeight > 150000.0)
                        this.desiredSectorZ -= 40000f;
                }
            }
            if ((double)this.camHeight <= 168471840.0 * (double)this.GameScale)
                return;
            this.camHeight = 1.684718E+08f * this.GameScale;
        }

        public bool IsShipUnderFleetIcon(Ship ship, Vector2 screenPos, float fleetIconScreenRadius)
        {
            foreach (ClickableFleet clickableFleet in ClickableFleetsList)
                if (clickableFleet.fleet == ship.fleet && screenPos.InRadius(clickableFleet.ScreenPos, fleetIconScreenRadius))
                    return true;
            return false;
        }

        private Circle GetSelectionCircles(Vector2 WorldPos, float WorldRadius, float radiusMin = 0, float radiusIncrease = 0 )
        {
            ProjectToScreenCoords(WorldPos, WorldRadius, out Vector2 screenPos, out float screenRadius);
            if (radiusMin > 0)
                screenRadius = screenRadius < radiusMin ? radiusMin : screenRadius;            
            return new Circle(screenPos, screenRadius + radiusIncrease);

        }

        private Circle GetSelectionCirclesAroundShip(Ship ship)
            => GetSelectionCircles(ship.Center, ship.GetSO().WorldBoundingSphere.Radius, 5, 0);
    }
}
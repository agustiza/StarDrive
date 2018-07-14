// Type: Ship_Game.Gameplay.Fleet
// Assembly: StarDrive, Version=1.0.9.0, Culture=neutral, PublicKeyToken=null
// MVID: C34284EE-F947-460F-BF1D-3C6685B19387
// Assembly location: E:\Games\Steam\steamapps\common\StarDrive\oStarDrive.exe

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Ship_Game.AI.Tasks;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game.AI
{
    public sealed partial class Fleet : ShipGroup
    {
        public BatchRemovalCollection<FleetDataNode> DataNodes = new BatchRemovalCollection<FleetDataNode>();
        public Guid Guid = Guid.NewGuid();
        public string Name = "";

        private Array<Ship> CenterShips = new Array<Ship>();
        private Array<Ship> LeftShips = new Array<Ship>();
        private Array<Ship> RightShips = new Array<Ship>();
        private Array<Ship> RearShips = new Array<Ship>();
        private Array<Ship> ScreenShips = new Array<Ship>();
        public Array<Squad> CenterFlank = new Array<Squad>();
        public Array<Squad> LeftFlank = new Array<Squad>();
        public Array<Squad> RightFlank = new Array<Squad>();
        public Array<Squad> ScreenFlank = new Array<Squad>();
        public Array<Squad> RearFlank = new Array<Squad>();
        public Array<Array<Squad>> AllFlanks = new Array<Array<Squad>>();

        private Map<Vector2, Ship[]> EnemyClumpsDict = new Map<Vector2, Ship[]>();
        private Map<Ship, Array<Ship>> InterceptorDict = new Map<Ship, Array<Ship>>();
        private int DefenseTurns = 50;
        private Vector2 TargetPosition = Vector2.Zero;
        public Tasks.MilitaryTask FleetTask;
        public FleetCombatStatus Fcs;


        public int FleetIconIndex;
        public static UniverseScreen Screen;
        public int TaskStep;
        public bool IsCoreFleet;

        private Array<Ship> AllButRearShips => Ships.Except(RearShips).ToArrayList();
        public bool HasRepair;  //fbedard: ships in fleet with repair capability will not return for repair.
        public bool ReadyForWarp { get; private set; }
        public override string ToString() => $"Fleet {Name} size={Ships.Count} pos={Position} guid={Guid}";

        //This file refactored by Gretman

        public Fleet()
        {
            FleetIconIndex = RandomMath.IntBetween(1, 10);
            InitializeGoalStack();
        }

        public void SetNameByFleetIndex(int index)
        {
            string suffix = "th";
            switch (index % 10) {
                case 1: suffix = "st"; break;
                case 2: suffix = "nd"; break;
                case 3: suffix = "rd"; break;
            }
            Name = index + suffix + " fleet";
        }

        public override void AddShip(Ship ship) => AddShip(ship, false);

        public void AddShip(Ship shiptoadd, bool updateOnly)
        {
            //Finding a bug. Added ship should never be null
            if (shiptoadd == null)
            {
                Log.WarningWithCallStack($"Ship Was Null for {Name}");
                return;
            }

            if (InvalidFleetShip(shiptoadd)) return;

            FleetShipAddsRepair(shiptoadd);

            if (updateOnly && Ships.Contains(shiptoadd)) return;

            //This is finding a logic bug: Ship is already in a fleet or this fleet already contains the ship.
            //This should likely be two different checks. There is also the possibilty that the ship is in another
            //Fleet ship list. 
            if (shiptoadd.fleet != null || Ships.Contains(shiptoadd))
            {
                Log.Warning("ship already in a fleet");
                return; // recover
            }       
            
            AddShipToNodes(shiptoadd);
            AssignPositions(Facing);            
        }

        private void FleetShipAddsRepair(Ship ship)
        {
            HasRepair = HasRepair || ship.hasRepairBeam || (ship.HasRepairModule && ship.Ordinance > 0);
        }
        private bool InvalidFleetShip(Ship ship) => ship.shipData.Role == ShipData.RoleName.station || ship.IsPlatform;

        public void AddExistingShip(Ship ship) => AddShipToNodes(ship);

        private void AddShipToNodes(Ship shiptoadd)
        {            
            base.AddShip(shiptoadd);
            shiptoadd.fleet = this;
            SetSpeed();
            AddShipToDataNode(shiptoadd);
        }


        public int CountCombatSquads => CenterFlank.Count + LeftFlank.Count + RightFlank.Count + ScreenFlank.Count;

        private void ClearFlankList()
        {
            CenterShips.Clear();
            LeftShips.Clear();
            RightShips.Clear();
            ScreenShips.Clear();
            RearShips.Clear();
            CenterFlank.Clear();
            LeftFlank.Clear();
            RightFlank.Clear();
            ScreenFlank.Clear();
            RearFlank.Clear();
            AllFlanks.Add(CenterFlank);
            AllFlanks.Add(LeftFlank);
            AllFlanks.Add(RightFlank);
            AllFlanks.Add(ScreenFlank);
            AllFlanks.Add(RearFlank);
        }

        private void ResetFlankLists()
        {
            ClearFlankList();

            var mainShipList = new Array<Ship>(Ships);
            
            ShipData.RoleName largest = mainShipList.FindMax(role => (int)role.DesignRole)?.DesignRole ?? ShipData.RoleName.disabled;

            for (int i = mainShipList.Count - 1; i >= 0; i--)
            {
                Ship ship = mainShipList[i];

                if (ship.DesignRole >= ShipData.RoleName.fighter && (int)ship.DesignRole > (int)largest - 1)
                {
                    this.ScreenShips.Add(ship);
                    mainShipList.RemoveSwapLast(ship);
                }
                else if (ship.DesignRole            == ShipData.RoleName.troop ||
                         ship.DesignRole            == ShipData.RoleName.freighter ||
                         ship.shipData.ShipCategory == ShipData.Category.Civilian ||
                         ship.DesignRole            == ShipData.RoleName.troopShip
                )
                {
                    this.RearShips.Add(ship);
                    mainShipList.RemoveSwapLast(ship);
                }
                else if (ship.shipData.Role < ShipData.RoleName.fighter)
                {
                    this.CenterShips.Add(ship);
                    mainShipList.RemoveSwapLast(ship);
                }
                else
                {
                    int leftOver = mainShipList.Count;
                    if (leftOver % 2 == 0)
                        RightShips.Add(ship);
                    else
                        LeftShips.Add(ship);
                    mainShipList.RemoveSwapLast(ship);
                }
            }

            int totalShips = this.CenterShips.Count;
            foreach (Ship ship in mainShipList.OrderByDescending(ship => ship.GetStrength() + ship.Size))
            {
                if (totalShips < 4) this.CenterShips.Add(ship);
                else if (totalShips < 8) this.LeftShips.Add(ship);
                else if (totalShips < 12) this.RightShips.Add(ship);
                else if (totalShips < 16) this.ScreenShips.Add(ship);
                else if (totalShips < 20 && this.RearShips.Count == 0) this.RearShips.Add(ship);

                ++totalShips;
                if (totalShips != 16) continue;
                //so far as i can tell this has zero effect. 
                ship.FleetCombatStatus = FleetCombatStatus.Maintain;
                totalShips = 0;
            }
        }

        public void AutoArrange()
        {
            ResetFlankLists();

            SetSpeed();

            CenterFlank = SortSquadBySize(CenterShips);
            LeftFlank   = SortSquadBySpeed(LeftShips);
            RightFlank  = SortSquadBySpeed(RightShips);
            ScreenFlank = SortSquadBySpeed(ScreenShips);
            RearFlank   = SortSquadBySpeed(RearShips);

            Position = FindAveragePosition();

            ArrangeSquad(CenterFlank, Vector2.Zero);
            ArrangeSquad(ScreenFlank, new Vector2(0.0f, -2500f));
            ArrangeSquad(RearFlank  , new Vector2(0.0f, 2500f));

            for (int index = 0; index < LeftFlank.Count; ++index)            
                LeftFlank[index].Offset = new Vector2(-CenterFlank.Count * 1400 - (LeftFlank.Count == 1 ? 1400 : index * 1400), 0.0f);
            

            for (int index = 0; index < RightFlank.Count; ++index)            
                RightFlank[index].Offset = new Vector2(CenterFlank.Count * 1400 + (RightFlank.Count == 1 ? 1400 : index * 1400), 0.0f);
            

            AutoAssembleFleet(0.0f);
            for (int x = 0; x < Ships.Count; x++)
            {
                Ship s = Ships[x];
                if (!s.InCombat)
                {
                    lock (s.AI.WayPointLocker)
                        s.AI.OrderThrustTowardsPosition(Position + s.FleetOffset, Facing, new Vector2(0.0f, -1f), true);
                }

                AddShipToDataNode(s);
            }
        }

        private void AddShipToDataNode(Ship ship)
        {
            FleetDataNode fleetDataNode = DataNodes.Find(newship => newship.Ship == ship) ??
                                          DataNodes.Find(newship => newship.Ship == null && newship.ShipName == ship.Name);
            if (fleetDataNode == null)
            {
                fleetDataNode = new FleetDataNode
                {
                    FleetOffset  = ship.RelativeFleetOffset,
                    OrdersOffset = ship.RelativeFleetOffset
                };

                DataNodes.Add(fleetDataNode);
            }
            ship.RelativeFleetOffset = fleetDataNode.FleetOffset;            

            fleetDataNode.Ship         = ship;
            fleetDataNode.ShipName     = ship.Name;
            fleetDataNode.OrdersRadius = fleetDataNode.OrdersRadius < 2 ? ship.AI.GetSensorRadius() : fleetDataNode.OrdersRadius;            
            ship.AI.FleetNode          = fleetDataNode;
        }

        private Array<Squad> SortSquadBySpeed(Array<Ship> allShips) => SortSquad(allShips, false);
        private Array<Squad> SortSquadBySize(Array<Ship> allShips) => SortSquad(allShips, true);

        private Array<Squad> SortSquad(Array<Ship> allShips, bool sizeOverSpeed)
        {
            var destSquad = new Array<Squad>();
            if (allShips.IsEmpty) return destSquad;
            Ship[] orderedShips = allShips.OrderByDescending(ship => sizeOverSpeed ? ship.Size: ship.Speed).ToArray();            
            Squad squad         = new Squad { Fleet = this };

            for (int index = 0; index < orderedShips.Length; ++index)
            {
                if (squad.Ships.Count < 4)
                    squad.Ships.Add(orderedShips[index]);
                if (squad.Ships.Count != 4 && index != orderedShips.Length - 1) continue;

                squad = new Squad { Fleet = this };
                destSquad.Add(squad);                
            }
            return destSquad;
        }

        private void ArrangeSquad(Array<Squad> squad, Vector2 squadOffset)
        {
            int leftSide  = 0;
            int rightSide = 0;

            for (int index = 0; index < squad.Count; ++index)
            {
                if (index == 0)
                    squad[index].Offset = squadOffset;
                else if (index % 2 == 1)
                {
                    ++leftSide;
                    squad[index].Offset = new Vector2(leftSide * (-1400 + squadOffset.X), squadOffset.Y);
                }
                else
                {
                    ++rightSide;
                    squad[index].Offset = new Vector2(rightSide * (1400 + squadOffset.X), squadOffset.Y);
                }
            }
        }

        private void AutoAssembleFleet(float facing)
        {
            for (int i = 0; i < AllFlanks.Count; i++)
            {
                Array<Squad> list = AllFlanks[i];
                foreach (Squad squad in list)
                {
                    for (int index = 0; index < squad.Ships.Count; ++index)
                    {
                        float radiansAngle;
                        switch (index)
                        {
                            case 0:
                                radiansAngle = new Vector2(0.0f, -500f).ToRadians();
                                break;
                            case 1:
                                radiansAngle = new Vector2(-500f, 0.0f).ToRadians();
                                break;
                            case 2:
                                radiansAngle = new Vector2(500f, 0.0f).ToRadians();
                                break;
                            case 3:
                                radiansAngle = new Vector2(0.0f, 500f).ToRadians();
                                break;
                            default:
                                radiansAngle = new Vector2(0.0f, 0.0f).ToRadians();
                                break;
                        }

                        Vector2 distanceUsingRadians =
                            Vector2.Zero.PointFromRadians((squad.Offset.ToRadians() + facing), squad.Offset.Length());
                        squad.Ships[index].FleetOffset =
                            distanceUsingRadians + Vector2.Zero.PointFromRadians(radiansAngle + facing, 500f);

                        distanceUsingRadians                   = Vector2.Zero.PointFromRadians(radiansAngle, 500f);
                        squad.Ships[index].RelativeFleetOffset = squad.Offset + distanceUsingRadians;
                    }
                }
            }
        }

        public void AssembleFleet(float facing, Vector2 facingVec) => AssembleFleet(facing, facingVec, IsCoreFleet);

        public void Reset()
        {
            while (Ships.Count > 0) {
                var ship = Ships.PopLast();
                ship.ClearFleet();
            }
            TaskStep  = 0;
            FleetTask = null;
            GoalStack.Clear();
        }

        private void EvaluateTask(float elapsedTime)

        {
            if (Ships.Count == 0)
                FleetTask.EndTask();
            if (FleetTask == null)
                return;
            switch (FleetTask.type)
            {
                case MilitaryTask.TaskType.ClearAreaOfEnemies:         DoClearAreaOfEnemies(FleetTask); break;
                case MilitaryTask.TaskType.AssaultPlanet:              DoAssaultPlanet(FleetTask); break;
                case MilitaryTask.TaskType.CorsairRaid:                DoCorsairRaid(elapsedTime); break;
                case MilitaryTask.TaskType.CohesiveClearAreaOfEnemies: DoCohesiveClearAreaOfEnemies(FleetTask); break;
                case MilitaryTask.TaskType.Exploration:                DoExplorePlanet(FleetTask); break;
                case MilitaryTask.TaskType.DefendSystem:               DoDefendSystem(FleetTask); break;
                case MilitaryTask.TaskType.DefendClaim:                DoClaimDefense(FleetTask); break;
                case MilitaryTask.TaskType.DefendPostInvasion:         DoPostInvasionDefense(FleetTask); break;
                case MilitaryTask.TaskType.GlassPlanet:                DoGlassPlanet(FleetTask); break;
            }
            this.Owner.GetGSAI().TaskList.ApplyPendingRemovals();
        }

        private bool IsInFormationWarp() => !Ships.Any(ship => ship.AI.State != AIState.FormationWarp);
        
        private bool EndInvalidTask(bool condition)
        {
            if (!condition) return false;
            FleetTask.EndTask();
            return true;
        }

        private void DoCorsairRaid(float elapsedTime)
        {
            if (TaskStep != 0)
                return;

            FleetTask.TaskTimer -= elapsedTime;
            Ship station = Owner.GetShips().Find(ship => ship.Name == "Corsair Asteroid Base");
            if (FleetTask.TaskTimer > 0.0)
            {
                EndInvalidTask(Ships.Count == 0);
                return;
            }
            if (EndInvalidTask(station == null)) return;                            

            AssembleFleet(0.0f, Vector2.One);
            // ReSharper disable once PossibleNullReferenceException station should never be null here
            FormationWarpTo(station.Position, 0.0f, Vector2.One);
            FleetTask.EndTaskWithMove();
        }

        private void SetPriorityOrderTo(Array<Ship> ships)
        {
            for (int index = 0; index < Ships.Count; index++)
            {
                Ship ship = Ships[index];
                ship.AI.SetPriorityOrder(true);
            }
        }
        private void SetAllShipsPriorityOrder() => SetPriorityOrderTo(Ships);

        private void FleetTaskGatherAtRally(MilitaryTask task)
        {
            Planet planet           = Owner.FindNearestRallyPoint(task.AO);
            Vector2 nearestShipyard = planet.Center;
            Vector2 fVec            = Vector2.Normalize(task.AO - nearestShipyard);

            SetAllShipsPriorityOrder();
            MoveToNow(nearestShipyard, nearestShipyard.RadiansToTarget(task.AO), fVec);            
        }               

        private bool HasArrivedAtRallySafely(MilitaryTask task)
        {
            if (!IsFleetAssembled(5000f, out bool endTask))
                return false;
            return !EndInvalidTask(endTask);
        }

        private void GatherAtAORally(MilitaryTask task, float distanceFromAO)
        {
            Vector2 movePosition = task.GetTargetPlanet().Center +
                                   Vector2.Normalize(FindAveragePosition() - task.GetTargetPlanet().Center) *
                                   distanceFromAO;
            Position = movePosition;
            FormationWarpTo(movePosition, FindAveragePosition().RadiansToTarget(task.AO),
                Vector2.Normalize(task.AO - FindAveragePosition()));
        }

        private void HoldFleetPosition()
        {
            for (int index = 0; index < this.Ships.Count; index++)
            {
                Ship ship = Ships[index];
                ship.AI.State = AIState.HoldPosition;
                if (ship.shipData.Role == ShipData.RoleName.troop)
                    ship.AI.HoldPosition();
            }
        }

        private bool ArrivedAtAORally(MilitaryTask task)
        {
            if (!IsFleetAssembled(5000f))
                return false;

            HoldFleetPosition();

            this.InterceptorDict.Clear();            
            return true;
        }

        private void ExplorePlanet3ClearHazards(MilitaryTask task)
        {
            EnemyClumpsDict = Owner.GetGSAI().ThreatMatrix
                        .PingRadarShipClustersByVector(Position, FindAveragePosition().Distance(task.AO), 10000, this.Owner);

            if (EnemyClumpsDict.Count == 0)
            {
                this.TaskStep = 5;
                return;
            }

            var availableShips = AllButRearShips;
            foreach (var kv in EnemyClumpsDict.OrderBy(dis => dis.Key.SqDist(FindAveragePosition())))
            {
                if (availableShips.Count == 0) break;
                foreach (var toAttack in kv.Value)
                {
                    float attackStr = 0.0f;
                    for (int x = availableShips.Count - 1; x >= 0; x--)
                    {
                        if (attackStr > toAttack.GetStrength() * 2) break;

                        Ship ship = availableShips[x];
                        ship.AI.Intercepting = true;
                        ship.AI.OrderAttackSpecificTarget(toAttack);
                        availableShips.RemoveAtSwapLast(x);
                        attackStr += ship.GetStrength();
                    }
                }
            }
            foreach (Ship needEscort in RearShips)
            {
                if (availableShips.IsEmpty) break;
                Ship ship = availableShips.PopLast();
                ship.DoEscort(needEscort);
                
            }

            foreach (Ship ship in availableShips)
            {
                ship.AI.OrderMoveDirectlyTowardsPosition(task.AO, 0, true);
            }

            this.TaskStep = 4;
        }
        private bool ShipsOffMission(MilitaryTask task)
        {
            return Ships.Any(ship => !ship.AI.HasPriorityOrder
                                     && (!ship.InCombat
                                         || FindAveragePosition().OutsideRadius(ship.Center, FindAveragePosition().Distance(task.AO))));
        }
        private void DoExplorePlanet(MilitaryTask task) //Mer Gretman Left off here
        {
            Log.Info("DoExplorePlanet called!  " + this.Owner.PortraitName);
            bool eventBuildingFound = task.GetTargetPlanet().EventsOnBuildings();            

            bool weHaveTroops = task.GetTargetPlanet().AnyOfOurTroops(Owner) ||
                                Ships.Any(ship => ship.Carrier.AnyPlanetAssaultAvailable);

            if (EndInvalidTask(!eventBuildingFound || !weHaveTroops 
                                                   || task.GetTargetPlanet().Owner != null 
                                                   && task.GetTargetPlanet().Owner != Owner))
                return;

            switch (TaskStep)
            {
                case 0:
                    FleetTaskGatherAtRally(task);
                    TaskStep = 1;
                    break;
                case 1:
                    if (!HasArrivedAtRallySafely(task)) break;
                    GatherAtAORally(task, distanceFromAO: 50000f);
                    TaskStep = 2;
                    break;
                case 2:
                    if (ArrivedAtAORally(task))
                        TaskStep = 3;
                    break;
                case 3:
                    EscortingToPlanet(task);
                    TaskStep = 4;
                    break;
                        
                case 4:
                    if (EndInvalidTask(!IsFleetSupplied())) break;
                    if (ShipsOffMission(task))
                        TaskStep = 3;                    
                    break;
                case 5:
                    for (int x = 0; x < Ships.Count; x++)
                    {
                        Ship ship = Ships[x];
                        ship.AI.SetIntercepting();
                        ship.AI.OrderLandAllTroops(task.GetTargetPlanet());
                    }

                    Position = task.GetTargetPlanet().Center;
                    AssembleFleet(Facing, Vector2.Normalize(Position - FindAveragePosition()));
                    break;
            }
        }
        private void SetFleetCombatWeights()
        {
            for (int x = 0; x < Ships.Count; x++)
            {
                var ship = Ships[x];

                ship.AI.FleetNode.AssistWeight   = 1f;
                ship.AI.FleetNode.DefenderWeight = 1f;
                ship.AI.FleetNode.OrdersRadius   = ship.maxWeaponsRange;
            }
        }

        private void DoAssaultPlanet(MilitaryTask task)
        {
            if (!Owner.IsEmpireAttackable(task.GetTargetPlanet().Owner))
            {
                if (task.GetTargetPlanet().Owner == Owner || task.GetTargetPlanet().AnyOfOurTroops(Owner))
                {
                    var militaryTask = MilitaryTask.CreatePostInvasion(task.GetTargetPlanet().Center, task.WhichFleet, Owner);
                    Owner.GetGSAI().RemoveFromTaskList(task);
                    FleetTask = militaryTask;
                    Owner.GetGSAI().AddToTaskList(militaryTask);
                }
                else
                    task.EndTask();
                return;
            }
            
            if (EndInvalidTask(!StillMissionEffective(task)) | !StillCombatEffective(task))
            {
                task.IsCoreFleetTask = false;
                FleetTask = null;
                TaskStep = 0;
                return;
            }

            switch (this.TaskStep)
            {
                case 0:
                    FleetTaskGatherAtRally(task);
                    SetFleetCombatWeights();
                    TaskStep = 1;
                    break;
                case 1:
                    if (!HasArrivedAtRallySafely(task))
                        break;
                    GatherAtAORally(task, distanceFromAO: 125000f);
                    TaskStep = 2;
                    break;
                case 2:
                    if(!ArrivedAtAORally(task)) break;
                    TaskStep = 3;
               
                    Position = task.GetTargetPlanet().Center;
                    AssembleFleet(Facing, Vector2.Normalize(Position - FindAveragePosition()));
                    break;
                case 3:
                    EscortingToPlanet(task);
                    TaskStep = 4;                                         
                    break;
                case 4:
                    WaitingForPlanetAssault(task);
                    if(ShipsOffMission(task))                    
                        TaskStep = 3;
                    if (!IsFleetSupplied())
                        TaskStep = 5;
                    break;

                case 5:
                    SendFleetToResupply();
                    TaskStep = 3;
                    break;
            }
        }

        private void WaitingForPlanetAssault(MilitaryTask task)
        {
            float theirGroundStrength = GetGroundStrOfPlanet(task.GetTargetPlanet());
            float ourGroundStrength   = FleetTask.GetTargetPlanet().GetGroundStrength(Owner);
            bool invading             = IsInvading(theirGroundStrength, ourGroundStrength, task);
            bool bombing              = BombPlanet(ourGroundStrength, task);
            if(!bombing && !invading)
                EndInvalidTask(true);            
        }

        private void SendFleetToResupply()
        {
            Planet rallyPoint = Owner.RallyPoints.FindMin(planet => Position.SqDist(planet.Center));
            for (int x = 0; x < Ships.Count; x++)
            {
                Ship ship = Ships[x];
                if (ship.AI.HasPriorityOrder) continue;                
                ship.AI.OrderResupply(rallyPoint, true);
            }
        }

        private bool StillCombatEffective(MilitaryTask task)
        {
            float targetStrength =
                Owner.GetGSAI().ThreatMatrix.PingRadarStr(task.GetTargetPlanet().Center, task.AORadius, Owner);
            return targetStrength < GetStrength() * 2;            
        }

        private bool StillMissionEffective(MilitaryTask task)
        {
            bool troopsOnPlanet = task.GetTargetPlanet().AnyOfOurTroops(Owner);
            bool stillHaveTroops = Ships.Any(troops => troops.Carrier.AnyPlanetAssaultAvailable);
            return troopsOnPlanet | stillHaveTroops;
        }

        private void InvadeTactics(Array<Ship> flankShips, string type, Vector2 moveTo)
        {
            foreach (var ship in flankShips)
            {
                ship.AI.CombatState = ship.shipData.CombatState;
                if (ship.Center.OutsideRadius(FleetTask.GetTargetPlanet().Center, FleetTask.AORadius))
                    ship.AI.HasPriorityTarget = false;
                else  continue;

                ship.AI.Intercepting = false;
                ship.AI.FleetNode.AssistWeight = 1f;
                ship.AI.FleetNode.DefenderWeight = 1f;
                ship.AI.FleetNode.OrdersRadius = ship.maxWeaponsRange;
                switch (type) {
                    case "screen":
                        if (!ship.InCombat)
                            ship.AI.OrderMoveDirectlyTowardsPosition(moveTo + ship.FleetOffset, 1, false);
                        break;
                    case "rear":
                        if (!ship.AI.HasPriorityOrder)
                            ship.AI.OrderMoveDirectlyTowardsPosition(moveTo + ship.FleetOffset, Facing, Vector2.Zero, false, Speed * .75f);
                        break;
                    case "center":
                        if (ship.AI.State != AIState.Bombard && ship.DesignRole != ShipData.RoleName.bomber)
                            ship.AI.OrderMoveDirectlyTowardsPosition(moveTo + ship.FleetOffset, 1, false);
                        else if (!ship.InCombat)
                            ship.AI.OrderMoveDirectlyTowardsPosition(moveTo + ship.FleetOffset, 1, false);
                        break;
                    case "side":
                        if (ship.InCombat) continue;

                        ship.AI.OrderMoveDirectlyTowardsPosition(moveTo + ship.FleetOffset, 1, false);
                        break;
                }
            }
        }

        private bool EscortingToPlanet(MilitaryTask task)
        {

            //EnemyClumpsDict = Owner.GetGSAI().ThreatMatrix.PingRadarShipClustersByVector(center, radius, granularity, Owner);
            //experimental. Rather than making the fleet attack specific targets. Use the fleet combat weights and range to control 
            //what is a valid target. 
            var center = task.GetTargetPlanet().Center;
            InvadeTactics(ScreenShips, "screen", center);
            InvadeTactics(CenterShips, "center", center);
            InvadeTactics(RearShips, "rear", center);
            InvadeTactics(RightShips, "side", center);
            InvadeTactics(LeftShips, "side", center);

            return !task.GetTargetPlanet().AnyOfOurTroops(Owner) || Ships.Any(bombers => bombers.AI.State == AIState.Bombard);            
        }

        private bool StartBombPlanet(MilitaryTask task) => StartStopBombing(true, task);
        private bool StopBombPlanet(MilitaryTask task) => StartStopBombing(false, task);
        private bool StartStopBombing(bool doBombing, MilitaryTask task)
        {
            var bombers = Ships.FilterBy(ship => ship.BombBays.Count > 0);
            foreach (var ship in bombers)
            {
                if (doBombing)
                    ship.AI.OrderBombardPlanet(task.GetTargetPlanet());
                else if (ship.AI.State == AIState.Bombard)
                    ship.AI.ClearOrdersNext = true;
            }
            return bombers.Length > 0;
        }

        private bool BombPlanet(float ourGroundStrength, MilitaryTask task , int freeSpacesNeeded = 5)
        {            
            bool doBombs = !(ourGroundStrength > 0 && freeSpacesNeeded >= task.GetTargetPlanet().GetGroundLandingSpots());
            return StartStopBombing(doBombs, task);                                   
        }

        private bool IsInvading(float thierGroundStrength, float ourGroundStrength, Tasks.MilitaryTask task, int LandingspotsNeeded =5)
        {            
            int freeLandingSpots = task.GetTargetPlanet().GetGroundLandingSpots();
            if (freeLandingSpots < 1)
                return false;

            float planetAssaultStrength = 0.0f;
            foreach (Ship ship in RearShips)
                planetAssaultStrength += ship.Carrier.PlanetAssaultStrength;
            
            planetAssaultStrength += ourGroundStrength;
            if (planetAssaultStrength < thierGroundStrength) return false;
            if (freeLandingSpots < LandingspotsNeeded) return false;

            if (ourGroundStrength > 1)
                StopBombPlanet(task);

            OrderShipsToInvade(RearShips, task);
            return true;
        }

        private void OrderShipsToInvade(Array<Ship> ships, MilitaryTask task)
        {
            foreach (Ship ship in ships)
            {
                ship.AI.OrderLandAllTroops(task.GetTargetPlanet());
                ship.AI.HasPriorityOrder = true;
            }
        }

        private float GetGroundStrOfPlanet(Planet p) => p.GetGroundStrengthOther(Owner);

        private void SetPostInvasionFleetCombat()
        {
            foreach (var node in DataNodes)
            {
                node.OrdersRadius = FleetTask.AORadius;
                node.AssistWeight = 1;
                node.DPSWeight = -1;
            }
        }

        private void PostInvasionStayInAO()
        {
            foreach (var ship in Ships)
            {

                if (ship.Center.SqDist(FleetTask.GetTargetPlanet().Center) > ship.AI.FleetNode.OrdersRadius)
                    ship.AI.OrderThrustTowardsPosition(FleetTask.GetTargetPlanet().Center + ship.FleetOffset, 1f, Vector2.Zero, true);
            }
        }

        private bool PostInvastionAnyShipsOutOfAO(MilitaryTask task) =>
            Ships.Any(ship => task.GetTargetPlanet().Center.OutsideRadius(ship.Center, ship.AI.FleetNode.OrdersRadius));

        private void DoPostInvasionDefense(Tasks.MilitaryTask task)
        {
            if (EndInvalidTask(--DefenseTurns <= 0))
                return;            
            
            switch (this.TaskStep)
            {
                case 0:
                    if (EndInvalidTask(FleetTask.GetTargetPlanet() == null))
                        break;

                    SetPostInvasionFleetCombat();

                    TaskStep = 1;
                    break;
                case 1:

                    if (EndInvalidTask(!IsFleetSupplied()))
                        break;
                    PostInvasionStayInAO();
                    TaskStep = 2;
                    break;
                case 2:
                    if (PostInvastionAnyShipsOutOfAO(task))
                    {
                        TaskStep = 1;
                        break;
                    }
                    if (Ships.Any(ship => ship.InCombat))
                        break;
                    AssembleFleet(1, Vector2.Zero);
                    break;
            }
        }

        private void DoDefendSystem(Tasks.MilitaryTask task)
        {
            switch (this.TaskStep)
            {
                case -1:
                    bool flag1 = true;
                    foreach (Ship ship in this.Ships)
                    {
                        if (!ship.EMPdisabled && ship.hasCommand && ship.Active)
                        {
                            if (Vector2.Distance(ship.Center, this.Position + ship.FleetOffset) > 5000.0)
                                flag1 = false;
                            if (!flag1)
                                break;
                        }
                    }
                    if (!flag1)
                        break;
                    this.TaskStep = 2;
                    this.FormationWarpTo(task.AO, FindAveragePosition().RadiansToTarget(task.AO), Vector2.Normalize(task.AO - this.FindAveragePosition()));
                    foreach (Ship ship in Ships)
                        ship.AI.HasPriorityOrder = true;
                    break;
                case 0:
                    Array<Planet> list1 = new Array<Planet>();
                    foreach (Planet planet in this.Owner.GetPlanets())
                    {
                        if (planet.HasShipyard)
                            list1.Add(planet);
                    }
                    IOrderedEnumerable<Planet> orderedEnumerable1 = list1.OrderBy(planet => Vector2.Distance(task.AO, planet.Center));
                    if (orderedEnumerable1.Count() <= 0)
                        break;
                    Vector2 fVec = Vector2.Normalize(task.AO - orderedEnumerable1.First().Center);
                    Vector2 vector2 = orderedEnumerable1.First().Center;
                    this.MoveToNow(vector2, vector2.RadiansToTarget(task.AO), fVec);
                    this.TaskStep = 1;
                    break;
                case 1:
                    bool flag2 = true;
                    foreach (Ship ship in this.Ships)
                    {
                        if (!ship.EMPdisabled && ship.hasCommand && ship.Active)
                        {
                            if (Vector2.Distance(ship.Center, this.Position + ship.FleetOffset) > 5000.0)
                                flag2 = false;
                            int num = ship.InCombat ? 1 : 0;
                            if (!flag2)
                                break;
                        }
                    }
                    if (!flag2)
                        break;
                    this.TaskStep = 2;
                    this.FormationWarpTo(task.AO, FindAveragePosition().RadiansToTarget(task.AO), Vector2.Normalize(task.AO - this.FindAveragePosition()));
                    foreach (Ship ship in Ships)
                        ship.AI.HasPriorityOrder = true;
                    break;
                case 2:
                    bool flag3 = false;
                    if (Vector2.Distance(this.FindAveragePosition(), task.AO) < 15000.0)
                    {
                        foreach (Ship ship in this.Ships)
                        {
                            lock (ship)
                            {
                                if (ship.InCombat)
                                {
                                    flag3 = true;
                                    ship.HyperspaceReturn();
                                    ship.AI.OrderQueue.Clear();
                                    break;
                                }
                            }
                        }
                    }
                    if (!flag3 && Vector2.Distance(this.FindAveragePosition(), task.AO) >= 5000.0)
                        break;
                    this.TaskStep = 3;
                    break;
                case 3:
                    this.EnemyClumpsDict = Owner.GetGSAI().ThreatMatrix.PingRadarShipClustersByVector(Position, 150000, 10000, this.Owner);

                    if (this.EnemyClumpsDict.Count == 0)
                    {
                        if (Vector2.Distance(this.FindAveragePosition(), task.AO) <= 10000.0)
                            break;
                        this.FormationWarpTo(task.AO, 0.0f, new Vector2(0.0f, -1f));
                        break;
                    }
                    else
                    {
                        Array<Vector2> list3 = new Array<Vector2>();
                        foreach (var keyValuePair in this.EnemyClumpsDict)
                            list3.Add(keyValuePair.Key);
                        IOrderedEnumerable<Vector2> orderedEnumerable2 = list3.OrderBy(clumpPos => Vector2.Distance(this.FindAveragePosition(), clumpPos));
                        Array<Ship> list4 = new Array<Ship>();
                        foreach (Ship toAttack in this.EnemyClumpsDict[orderedEnumerable2.First()].OrderByDescending(ship => ship.Size))
                        {
                            float num = 0.0f;
                            foreach (Ship ship in this.Ships.OrderByDescending(ship => ship.Size))
                            {
                                if (!list4.Contains(ship) && (num == 0.0 || num < (double)toAttack.GetStrength()))
                                {
                                    ship.AI.OrderAttackSpecificTarget(toAttack);
                                    ship.AI.Intercepting = true;
                                    list4.Add(ship);
                                    num += ship.GetStrength();
                                }
                            }
                        }
                        Array<Ship> list5 = new Array<Ship>();
                        foreach (Ship ship in this.Ships)
                        {
                            if (!list4.Contains(ship))
                                list5.Add(ship);
                        }
                        foreach (Ship ship in list5)
                        {
                            ship.AI.OrderAttackSpecificTarget(list4[0].AI.Target as Ship);
                            ship.AI.Intercepting = true;
                        }
                        this.TaskStep = 4;
                        break;
                    }
                case 4:
                    if (IsFleetSupplied())
                    {
                        this.TaskStep = 5;
                        break;
                    }
                    else
                    {
                        bool flag4 = false;
                        foreach (Ship ship in this.Ships)
                        {
                            if (!ship.InCombat)
                            {
                                flag4 = true;
                                break;
                            }
                        }
                        if (!flag4)
                            break;
                        this.TaskStep = 3;
                        break;
                    }
                case 5:
                    Array<Planet> list6 = new Array<Planet>();
                    foreach (Planet planet in this.Owner.GetPlanets())
                    {
                        if (planet.HasShipyard)
                            list6.Add(planet);
                    }
                    IOrderedEnumerable<Planet> orderedEnumerable3 = list6.OrderBy(p => Vector2.Distance(this.Position, p.Center));
                    if (orderedEnumerable3.Count() <= 0)
                        break;
                    this.Position = orderedEnumerable3.First().Center;
                    foreach (Ship ship in this.Ships)
                        ship.AI.OrderResupply(orderedEnumerable3.First(), true);
                    this.TaskStep = 6;
                    break;
                case 6:
                    float num6 = 0.0f;
                    float num7 = 0.0f;
                    foreach (Ship ship in this.Ships)
                    {
                        ship.AI.HasPriorityOrder = true;
                        num6 += ship.Ordinance;
                        num7 += ship.OrdinanceMax;
                    }
                    if (num6 != (double)num7)
                        break;
                    this.TaskStep = 0;
                    break;
            }
        }

        private void DoClaimDefense(Tasks.MilitaryTask task)
        {
            switch (TaskStep)
            {
                case 0:
                    Planet rallyPoint = Owner.RallyPoints.FindMin(planet => planet.Center.SqDist(task.AO));
                    if (rallyPoint == null) return;
                    Position = rallyPoint.Center;
                    Vector2 fVec = Vector2.Normalize(task.GetTargetPlanet().Center - Position);
                    MoveToNow(Position, Position.RadiansToTarget(task.GetTargetPlanet().Center), fVec);
                    TaskStep = 1;
                    break;
                case 1:                    
                    if (!IsFleetAssembled(5000, out bool endtask))
                        break;                    
                    TaskStep = 2;
                    Position = task.GetTargetPlanet().Center;
                    foreach (Ship ship in Ships)
                        ship.AI.SetPriorityOrder(true);
                    FormationWarpTo(Position, FindAveragePosition().RadiansToTarget(Position), Vector2.Normalize(Position - FindAveragePosition()));                   
                    break;
                case 2:
                    if (IsFleetAssembled(15000f, out bool incombat) && incombat)
                    {
                        foreach (Ship ship in Ships)
                        {
                            if (ship.InCombat)
                            {                                   
                                ship.HyperspaceReturn();
                                ship.AI.OrderQueue.Clear();
                                break;
                            }
                        }
                    }
                    if (!incombat && FindAveragePosition().OutsideRadius(task.GetTargetPlanet().Center, 5000f))
                        break;
                    TaskStep = 3;
                    break;
                case 3:                    
                    EnemyClumpsDict = Owner.GetGSAI().ThreatMatrix.PingRadarShipClustersByVector(FleetTask.GetTargetPlanet().Center, 150000,10000,Owner);

                    if (EnemyClumpsDict.Count == 0)
                    {
                        foreach (Ship ship in Ships)
                        {
                            var ai = ship.AI;
                            var target = task.GetTargetPlanet();
                            if (!ship.InCombat)
                            {
                                if (ai.State != AIState.Orbit || ai.OrbitTarget == null || ai.OrbitTarget != target)
                                    ai.OrderOrbitPlanet(target);
                            }
                            else // if (current.GetAI().TargetQueue.Count == 0)
                            {
                                ai.OrderMoveDirectlyTowardsPosition(target.ParentSystem.Position, 0, Vector2.Zero, true);
                                ai.HasPriorityOrder = true;
                            }
                        }
                        break;
                    }
                    else
                    {
                        var list3 = new Array<Vector2>();
                        foreach (var keyValuePair in this.EnemyClumpsDict)
                            list3.Add(keyValuePair.Key);
                        IOrderedEnumerable<Vector2> orderedEnumerable2 = list3.OrderBy(clumpPos => Vector2.Distance(this.FindAveragePosition(), clumpPos));
                        Array<Ship> list4 = new Array<Ship>();
                        foreach (Ship toAttack in this.EnemyClumpsDict[orderedEnumerable2.First()])
                        {
                            float num = 0.0f;
                            foreach (Ship ship in this.Ships)
                            {
                                if (ship.AI.HasPriorityOrder) continue;
                                if (list4.Contains(ship) || num > 1 && num >= toAttack.GetStrength())
                                    continue;
                                ship.AI.Intercepting = true;
                                ship.AI.OrderAttackSpecificTarget(toAttack);
                                list4.Add(ship);
                                num += ship.GetStrength();
                            }
                        }
                        TaskStep = 4;

                        if (list4.IsEmpty) break;

                        Ship[] uniqueShips = Ships.UniqueGameObjects();
                        foreach (Ship ship in uniqueShips)
                        {
                            ship.AI.Intercepting = true;
                            ship.AI.OrderAttackSpecificTarget(list4[0].AI.Target as Ship);
                        }

                        break;
                    }
                case 4:
                    if (!IsFleetAssembled(150000, out bool combat, FleetTask.GetTargetPlanet().ParentSystem.Position))
                    {
                        foreach (Ship ship in this.Ships)
                        {
                            if (ship.AI.HasPriorityOrder) continue;
                            if (ship.Center.InRadius(FleetTask.GetTargetPlanet().ParentSystem.Position, 150000f))
                                continue;
                            ship.AI.OrderQueue.Clear();
                            ship.AI.OrderMoveDirectlyTowardsPosition(this.FleetTask.GetTargetPlanet().ParentSystem.Position, 0, Vector2.Zero, true);
                            //ship.GetAI().HasPriorityOrder = true;
                        }
                    }
                    if (!IsFleetSupplied())                    
                    {
                        this.TaskStep = 5;
                        break;
                    }
                    else
                    {
                        if (combat) break;
                        this.TaskStep = 3;
                        break;                        
                    }
                case 5:

                    rallyPoint = Owner.RallyPoints.FindMin(planet => Position.SqDist(planet.Center));
                    this.Position = rallyPoint.Center;
                    foreach (Ship ship in this.Ships)
                        ship.AI.OrderResupply(rallyPoint, true);
                    this.TaskStep = 6;
                    break;
                case 6:
                    float num6 = 0.0f;
                    float num7 = 0.0f;
                    foreach (Ship ship in this.Ships)
                    {
                        ship.AI.HasPriorityOrder = true;
                        num6 += ship.Ordinance;
                        num7 += ship.OrdinanceMax;
                    }
                    if (num6 < num7)
                        break;
                    this.TaskStep = 0;
                    break;
            }
        }

        private void DoCohesiveClearAreaOfEnemies(Tasks.MilitaryTask task)
        {
            switch (this.TaskStep)
            {
                case 0:
                    //this.TaskStep = 1;
                    //this.DoCohesiveClearAreaOfEnemies(task);
                    //break;
                case 1:
                    
                    Map<Vector2, float> threatDict = this.Owner.GetGSAI().ThreatMatrix.PingRadarStrengthClusters(this.FleetTask.AO, this.FleetTask.AORadius, 10000f, this.Owner);
                    float strength = this.GetStrength();                    

                    //TODO: add this to threat dictionary. find max in strength
                    var targetSpot = new KeyValuePair<Vector2, float>(Vector2.Zero,float.MaxValue);
                    float distance = float.MaxValue;
                    foreach (var kv in threatDict)
                    {
                        float tempDis = FindAveragePosition().SqDist(kv.Key) ;
                        if (kv.Value < strength && tempDis <  distance)
                        {
                            targetSpot = kv;
                            distance = tempDis;
                        }
                    }

                    if (targetSpot.Value < strength)
                    {
                        TargetPosition = targetSpot.Key;
                        Vector2 fvec = Vector2.Normalize(task.AO - this.TargetPosition);
                        this.FormationWarpTo(this.TargetPosition, TargetPosition.RadiansToTarget(task.AO), fvec);
                        this.TaskStep = 2;

                    }
                    else                    
                        this.FleetTask.EndTask();
                    

                    break;
                    
                    
                case 2:
                    
                    if (this.Owner.GetGSAI().ThreatMatrix.PingRadarStr(TargetPosition, 75000, Owner) <1)
                    {
                        this.TaskStep = 1;
                        break;
                    }
                    else
                    {
                        
                        if (Vector2.Distance(this.TargetPosition, this.FindAveragePosition()) > 10000
                        && IsInFormationWarp()
                            )
                            break;
                        this.TaskStep = 3;
                        break;
                    }
                case 3:
                    this.EnemyClumpsDict = this.Owner.GetGSAI().ThreatMatrix.PingRadarShipClustersByVector
                        (this.Position, 150000, 10000, this.Owner, truePosition: true);
                   
                    if (this.EnemyClumpsDict.Count == 0)
                    {
                        task.Step = 1;
                        break;
                    }
                    else
                    {
                        Vector2[] list3 = EnemyClumpsDict.Keys.ToArray();
                        //foreach (var keyValuePair in this.EnemyClumpsDict)
                        //    list3.Add(keyValuePair.Key);
                        var orderedEnumerable = list3.OrderBy(clumpPos => this.FindAveragePosition().SqDist(clumpPos)).FirstOrDefault();
                        Array<Ship> list4 = new Array<Ship>();
                        
                        foreach (Ship toAttack in this.EnemyClumpsDict[orderedEnumerable])
                        {
                            Ship flag = null;
                            float num = 0.0f;
                            foreach (Ship ship in this.Ships)
                            {
                                if (!list4.Contains(ship) && (num < 1 || num < toAttack.GetStrength()))
                                {
                                    ship.AI.CombatState = ship.shipData.CombatState;
                                    ship.AI.Intercepting = false;
                                    if (flag == null)
                                    {
                                        ship.AI.Intercepting = true;
                                        ship.AI.OrderAttackSpecificTarget(toAttack);                                        
                                        list4.Add(ship);
                                        num += ship.GetStrength();
                                        flag = ship;
                                    }
                                    else
                                    {
                                        ship.DoEscort(flag);                                        

                                    }
                                }
                            }
                        }
                        Array<Ship> list5 = new Array<Ship>();
                        foreach (Ship ship in this.Ships)
                        {                            
                            if (!list4.Contains(ship))
                                list5.Add(ship);
                        }
                        foreach (Ship ship in list5)
                        {
                            ship.AI.Intercepting = true;
                            ship.AI.OrderAttackSpecificTarget(list4[0].AI.Target as Ship);
                        }
                        
                        this.TaskStep = 4;
                        break;
                    }
                case 4:
                    if (!IsFleetSupplied())
                    {
                        this.TaskStep = 5;
                        break;
                    }

                    bool allInCombat = true;
                    foreach (Ship ship in this.Ships)
                    {
                        if (!ship.AI.BadGuysNear )
                        {
                            allInCombat = false;
                            break;
                        }
                    }
                    if (this.Owner.GetGSAI().ThreatMatrix.PingRadarStr(this.Position, 150000, this.Owner) > 0)
                    {
                        
                        if(!allInCombat )
                        {
                            this.TaskStep = 3;
                            break;
                        }
                        this.TaskStep = 4;
                        break;
                    }
                    else
                    {
                        this.TaskStep = 2;
                        break;
                    }
                       
                    
                    
                case 5:
                    foreach (Ship ship in this.Ships)
                        ship.AI.OrderResupplyNearest(true);
                    this.TaskStep = 6;
                    break;
                case 6:                  
                    if (!IsFleetSupplied(wantedSupplyRatio: .9f))
                        break;
                    this.TaskStep = 1;
                    break;
            }
        }

        private void DoGlassPlanet(Tasks.MilitaryTask task)
        {
            if (task.GetTargetPlanet().Owner == this.Owner || task.GetTargetPlanet().Owner == null)
                task.EndTask();
            else if (task.GetTargetPlanet().Owner != null & task.GetTargetPlanet().Owner != this.Owner && !task.GetTargetPlanet().Owner.GetRelations(this.Owner).AtWar)
            {
                task.EndTask();
            }
            else
            {
                switch (this.TaskStep)
                {
                    case 0:
                        Array<Planet> list1 = new Array<Planet>();
                        foreach (Planet planet in this.Owner.GetPlanets())
                        {
                            if (planet.HasShipyard)
                                list1.Add(planet);
                        }
                        IOrderedEnumerable<Planet> orderedEnumerable1 = list1.OrderBy(planet => Vector2.Distance(task.AO, planet.Center));
                        if (!orderedEnumerable1.Any())
                            break;
                        Vector2 fVec = Vector2.Normalize(task.AO - orderedEnumerable1.First().Center);
                        Vector2 vector2 = orderedEnumerable1.First().Center;
                        this.MoveToNow(vector2, vector2.RadiansToTarget(task.AO), fVec);
                        this.TaskStep = 1;
                        break;
                    case 1:

                        int step = MoveToPositionIfAssembled(task, task.AO, 15000f, 150000f);
                        if (step == -1)
                            task.EndTask();
                        TaskStep += step;                        
                        break;
                    case 2:
                        if (task.WaitForCommand && this.Owner.GetGSAI().ThreatMatrix.PingRadarStr(task.GetTargetPlanet().Center, 30000f, this.Owner) > 250.0)
                            break;
                        foreach (Ship ship in this.Ships)
                            ship.AI.OrderBombardPlanet(task.GetTargetPlanet());
                        this.TaskStep = 4;
                        break;
                    case 4:
                        if (!IsFleetSupplied())
                        {
                            this.TaskStep = 5;
                            break;
                        }
                        else
                        {
                            this.TaskStep = 2;
                            break;
                        }
                    case 5:
                        Array<Planet> list2 = new Array<Planet>();
                        foreach (Planet planet in this.Owner.GetPlanets())
                        {
                            if (planet.HasShipyard)
                                list2.Add(planet);
                        }
                        IOrderedEnumerable<Planet> orderedEnumerable2 = list2.OrderBy(p => Vector2.Distance(this.Position, p.Center));
                        if (!orderedEnumerable2.Any())
                            break;
                        this.Position = orderedEnumerable2.First().Center;
                        foreach (Ship ship in this.Ships)
                            ship.AI.OrderResupply(orderedEnumerable2.First(), true);
                        this.TaskStep = 6;
                        break;
                    case 6:
                        float num6 = 0.0f;
                        float num7 = 0.0f;
                        foreach (Ship ship in this.Ships)
                        {
                            if (ship.AI.State != AIState.Resupply)
                            {
                                this.TaskStep = 5;
                                return;
                            }
                            ship.AI.HasPriorityOrder = true;
                            num6 += ship.Ordinance;
                            num7 += ship.OrdinanceMax;
                        }
                        if ((int)num6 != (int)num7)
                            break;
                        this.TaskStep = 0;
                        break;
                }
            }
        }

        private void DoClearAreaOfEnemies(Tasks.MilitaryTask task)
        {
            switch (TaskStep)
            {
                case 0:
                    Array<Planet> list1 = new Array<Planet>();
                    foreach (Planet planet in this.Owner.GetPlanets())
                    {
                        if (planet.HasShipyard)
                            list1.Add(planet);
                    }
                    IOrderedEnumerable<Planet> orderedEnumerable1 = list1.OrderBy(planet => Vector2.Distance(task.AO, planet.Center));
                    if (!orderedEnumerable1.Any())
                        break;
                    Vector2 fVec = Vector2.Normalize(task.AO - orderedEnumerable1.First().Center);
                    Vector2 vector2 = orderedEnumerable1.First().Center;
                    this.MoveToNow(vector2, vector2.RadiansToTarget(task.AO), fVec);
                    this.TaskStep = 1;
                    break;
                case 1:
                    int step = MoveToPositionIfAssembled(task, task.AO, 5000f, 7500f);
                    if (step == -1)
                        task.EndTask();
                    TaskStep += step;
     
                    break;
                case 2:
                    if (IsFleetSupplied())
                    {
                        this.TaskStep = 5;
                        break;
                    }
                    else
                    {
                        bool flag2 = false;
                        if (Vector2.Distance(this.FindAveragePosition(), task.AO) < 15000.0)
                        {
                            foreach (Ship ship in this.Ships)
                            {
                                lock (ship)
                                {
                                    if (ship.InCombat)
                                    {
                                        flag2 = true;
                                        ship.HyperspaceReturn();
                                        ship.AI.OrderQueue.Clear();
                                        break;
                                    }
                                }
                            }
                        }
                        if (!flag2 && Vector2.Distance(this.FindAveragePosition(), task.AO) >= 10000.0)
                            break;
                        this.TaskStep = 3;
                        break;
                    }
                case 3:
                    this.EnemyClumpsDict = Owner.GetGSAI().ThreatMatrix.PingRadarShipClustersByVector(Ships[0].Center, 150000, 10000, this.Owner);

                    if (this.EnemyClumpsDict.Count == 0 || Vector2.Distance(this.FindAveragePosition(), task.AO) > 25000.0)
                    {
                        Vector2 enemyWithinRadius = this.Owner.GetGSAI().ThreatMatrix.GetPositionOfNearestEnemyWithinRadius(this.Position, task.AORadius, this.Owner);
                        if (enemyWithinRadius == Vector2.Zero)
                        {
                            task.EndTask();
                            break;
                        }
                        this.MoveDirectlyNow(enemyWithinRadius, FindAveragePosition().RadiansToTarget(enemyWithinRadius), Vector2.Normalize(enemyWithinRadius - this.Position));
                        this.TaskStep = 2;
                        break;
                    }
                    else
                    {
                        Array<Vector2> list3 = new Array<Vector2>();
                        foreach (var keyValuePair in this.EnemyClumpsDict)
                            list3.Add(keyValuePair.Key);
                        IOrderedEnumerable<Vector2> orderedEnumerable2 = list3.OrderBy(clumpPos => Vector2.Distance(this.FindAveragePosition(), clumpPos));
                        Array<Ship> list4 = new Array<Ship>();
                        foreach (Ship toAttack in this.EnemyClumpsDict[orderedEnumerable2.First()])
                        {
                            float num6 = 0.0f;
                            foreach (Ship ship in this.Ships)
                            {
                                if (!list4.Contains(ship) && (num6 == 0.0 || num6 < (double)toAttack.GetStrength()))
                                {
                                    ship.AI.Intercepting = true;
                                    ship.AI.OrderAttackSpecificTarget(toAttack);
                                    list4.Add(ship);
                                    num6 += ship.GetStrength();
                                }
                            }
                        }
                        Array<Ship> list5 = new Array<Ship>();
                        foreach (Ship ship in this.Ships)
                        {
                            if (!list4.Contains(ship))
                                list5.Add(ship);
                        }
                        foreach (Ship ship in list5)
                        {
                            ship.AI.Intercepting = true;
                            ship.AI.OrderAttackSpecificTarget(list4[0].AI.Target as Ship);
                        }
                        this.TaskStep = 4;
                        break;
                    }
                case 4:
                    if (!IsFleetSupplied())
                    {
                        this.TaskStep = 5;
                        break;
                    }
                    else
                    {
                        bool flag2 = false;
                        foreach (Ship ship in this.Ships)
                        {
                            if (!ship.InCombat)
                            {
                                flag2 = true;
                                break;
                            }
                        }
                        if (!flag2)
                            break;
                        this.TaskStep = 3;
                        break;
                    }
                case 5:
                    Array<Planet> list6 = new Array<Planet>();
                    foreach (Planet planet in this.Owner.GetPlanets())
                    {
                        if (planet.HasShipyard)
                            list6.Add(planet);
                    }
                    IOrderedEnumerable<Planet> orderedEnumerable3 = list6.OrderBy(p => Vector2.Distance(this.Position, p.Center));
                    if (orderedEnumerable3.Count() <= 0)
                        break;
                    this.Position = orderedEnumerable3.First().Center;
                    foreach (Ship ship in this.Ships)
                        ship.AI.OrderResupply(orderedEnumerable3.First(), true);
                    this.TaskStep = 6;
                    break;
                case 6:
                    float num12 = 0.0f;
                    float num13 = 0.0f;
                    foreach (Ship ship in this.Ships)
                    {
                        if (ship.AI.State != AIState.Resupply)
                        {
                            this.TaskStep = 5;
                            return;
                        }
                        ship.AI.HasPriorityOrder = true;
                        num12 += ship.Ordinance;
                        num13 += ship.OrdinanceMax;
                    }
                    if (num12 != (double)num13)
                        break;
                    this.TaskStep = 0;
                    break;
            }
        }

        private int MoveToPositionIfAssembled(MilitaryTask task, Vector2 position, float assemblyRadius = 5000f, float moveToWithin = 7500f )
        {
            bool nearFleet = IsFleetAssembled(assemblyRadius, out bool endTask);

            if (endTask)
                return -1;

            if (nearFleet)
            {                
                Vector2 movePosition = position + Vector2.Normalize(FindAveragePosition() - position) * moveToWithin;
                Position = movePosition;
                FormationWarpTo(movePosition, FindAveragePosition().RadiansToTarget(position),
                    Vector2.Normalize(position - FindAveragePosition()));
                return 1;
            }
            return 0;
        }

        public void UpdateAI(float elapsedTime, int which)
        {
            if (FleetTask != null)
            {
                EvaluateTask(elapsedTime);
            }
            else
            {
                if (EmpireManager.Player == Owner || IsCoreFleet )
                {
                    if (!IsCoreFleet) return;
                    foreach (Ship ship in Ships)
                    {
                        ship.AI.HasPriorityTarget = false;
                        ship.AI.Intercepting = false;
                    }
                    return;
                }
                Owner.GetGSAI().UsedFleets.Remove(which);                
                for (int i = 0; i < Ships.Count; ++i)
                {
                    Ship s = Ships[i];
                    RemoveShipAt(s, i--);

                    s.AI.OrderQueue.Clear();
                    s.AI.State = AIState.AwaitingOrders;
                    s.HyperspaceReturn();
                    s.isSpooling = false;
                    if (s.shipData.Role == ShipData.RoleName.troop)
                        s.AI.OrderRebaseToNearest();
                    else
                        Owner.ForcePoolAdd(s);
                }
                Reset();
            }
        }

        private void RemoveFromAllSquads(Ship ship)
        {

            if (DataNodes != null)
                using (DataNodes.AcquireWriteLock())
                    foreach (FleetDataNode fleetDataNode in DataNodes)
                    {
                        if (fleetDataNode.Ship == ship)
                            fleetDataNode.Ship = null;
                    }
            if (AllFlanks == null) return;
            foreach (var list in AllFlanks)
            {
                foreach (Squad squad in list)
                {
                    if (squad.Ships.Contains(ship))
                        squad.Ships.QueuePendingRemoval(ship);
                    if (squad.DataNodes == null) continue;
                    using (squad.DataNodes.AcquireWriteLock())
                        foreach (FleetDataNode fleetDataNode in squad.DataNodes)
                        {
                            if (fleetDataNode.Ship == ship)
                                fleetDataNode.Ship = (Ship)null;
                        }
                }
            }
        }

        private void RemoveShipAt(Ship ship, int index)
        {
            ship.fleet = null;
            RemoveFromAllSquads(ship);
            Ships.RemoveAtSwapLast(index);
        }

        public bool RemoveShip(Ship ship)
        {
            if (ship == null) return false;
            if (ship.Active && ship.fleet != this)
                Log.Error("{0} : not equal {1}", ship.fleet?.Name, Name);
            if (ship.AI.State != AIState.AwaitingOrders && ship.Active)
                Log.Info($"Fleet RemoveShip: Ship not awaiting orders and removed from fleet State: {ship.AI.State}");
            ship.fleet = null;
            RemoveFromAllSquads(ship);
            if (Ships.Remove(ship) || !ship.Active) return true;
            Log.Info("Fleet RemoveShip: Ship is not in this fleet");
            return false;
        }

        
        public void Update(float elapsedTime)
        {
            HasRepair = false;
            ReadyForWarp = true;
            for (int index = Ships.Count - 1; index >= 0; index--)
            {
                Ship ship = Ships[index];
                if (!ship.Active)
                {
                    RemoveShip(ship);
                    continue;
                }
                AddShip(ship, true);
                ReadyForWarp = ReadyForWarp && ship.ShipReadyForWarp();                
            }
            Ships.ApplyPendingRemovals();

            if (Ships.Count <= 0 || GoalStack.Count <= 0)
                return;
            GoalStack.Peek().Evaluate(elapsedTime);
        }

        public enum FleetCombatStatus
        {
            Maintain,
            Loose,
            Free
        }

        public sealed class Squad : IDisposable
        {
            public FleetDataNode MasterDataNode = new FleetDataNode();
            public BatchRemovalCollection<FleetDataNode> DataNodes = new BatchRemovalCollection<FleetDataNode>();
            public BatchRemovalCollection<Ship> Ships = new BatchRemovalCollection<Ship>();
            public Fleet Fleet;
            public Vector2 Offset;

            public FleetCombatStatus FleetCombatStatus;
            
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            ~Squad() { Dispose(false); }

            private void Dispose(bool disposing)
            {
    
                DataNodes?.Dispose(ref DataNodes);
                Ships?.Dispose(ref Ships);
            }
        }

        public enum FleetGoalType
        {
            AttackMoveTo,
            MoveTo
        }

        protected override void Destroy()
        {
            if (Ships != null)
                for (int x = Ships.Count - 1; x >= 0; x--)
                {
                    var ship = Ships[x];
                    RemoveShip(ship);
                }

            DataNodes?.Dispose(ref DataNodes);
            GoalStack       = null;
            CenterShips     = null;
            LeftShips       = null;
            RightShips      = null;
            RearShips       = null;
            ScreenShips     = null;
            CenterFlank     = null;
            LeftFlank       = null;
            RightFlank      = null;
            ScreenFlank     = null;
            RearFlank       = null;
            AllFlanks       = null;
            EnemyClumpsDict = null;
            InterceptorDict = null;
            FleetTask       = null;
            base.Destroy();
        }
        public static string GetDefaultFleetNames(int index)
        {
            switch (index)
            {
                case 1:
                    return "First";
                    
                case 2:
                    return "Second";
                    
                case 3:
                    return "Third";
                    
                case 4:
                    return "Fourth";
                    
                case 5:
                    return "Fifth";
                    
                case 6:
                    return "Sixth";
                    
                case 7:
                    return "Seventh";
                    
                case 8:
                    return "Eigth";
                    
                case 9:
                    return "Ninth";
                    
            }
            return "";
        }
    }
}

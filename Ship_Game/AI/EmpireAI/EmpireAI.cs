using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;

namespace Ship_Game.AI
{
    [System.Runtime.InteropServices.Guid("2CC355DF-EA7A-49C8-8940-00AA0713EFE3")]
    public sealed partial class EmpireAI : IDisposable
    {

        private int NumberOfShipGoals  = 6;
        private int NumberTroopGoals   = 2;
        private float BuildCapacity    = 0;
        private readonly float MinimumWarpRange = GlobalStats.MinimumWarpRange;
        
        private readonly Empire OwnerEmpire;
        private readonly BatchRemovalCollection<SolarSystem> MarkedForExploration = new BatchRemovalCollection<SolarSystem>();
        private readonly OffensiveForcePoolManager OffensiveForcePoolManager;

        public string EmpireName;
        public DefensiveCoordinator DefensiveCoordinator;        
        public BatchRemovalCollection<Goal> Goals            = new BatchRemovalCollection<Goal>();
        public ThreatMatrix ThreatMatrix                     = new ThreatMatrix();        
        public Array<AO> AreasOfOperations                   = new Array<AO>();
        public Array<int> UsedFleets                         = new Array<int>();
        public BatchRemovalCollection<MilitaryTask> TaskList = new BatchRemovalCollection<MilitaryTask>();
        public Array<MilitaryTask> TasksToAdd                = new Array<MilitaryTask>();        
        public float FreighterUpkeep                         = 0f;
        public float PlatformUpkeep                          = 0f;
        public float StationUpkeep                           = 0f;
        public float Toughnuts                               = 0;                
        public int Recyclepool                               = 0;
        public float DefStr;

        public EmpireAI(Empire e)
        {
            this.EmpireName = e.data.Traits.Name;
            this.OwnerEmpire = e;
            this.DefensiveCoordinator = new DefensiveCoordinator(e);
            OffensiveForcePoolManager = new OffensiveForcePoolManager(e);
            if (OwnerEmpire.data.EconomicPersonality != null)            
                NumberOfShipGoals = NumberOfShipGoals + OwnerEmpire.data.EconomicPersonality.ShipGoalsPlus;
            
        }

        private void RunManagers()
        {
            if (this.OwnerEmpire.data.IsRebelFaction || this.OwnerEmpire.data.Defeated)            
                return;
            
            if (!this.OwnerEmpire.isPlayer)
            {
                OffensiveForcePoolManager.ManageAOs();
                foreach (AO ao in AreasOfOperations)                
                    ao.Update();
                
            }
            if (!this.OwnerEmpire.isFaction && !OwnerEmpire.MinorRace)
            {
                if (!OwnerEmpire.isPlayer || OwnerEmpire.AutoColonize)
                    RunExpansionPlanner();
                if (!OwnerEmpire.isPlayer || OwnerEmpire.AutoBuild)
                    RunInfrastructurePlanner();
            }
            this.DefensiveCoordinator.ManageForcePool();
            if (!this.OwnerEmpire.isPlayer)
            {
                this.RunEconomicPlanner();
                this.RunDiplomaticPlanner();
                if (this.OwnerEmpire.isFaction)
                {

                }
                if (!this.OwnerEmpire.MinorRace)
                {

                    this.RunMilitaryPlanner();
                    this.RunResearchPlanner();
                    this.RunAgentManager();
                    this.RunWarPlanner();
                }
            }
            else
            {
                if (this.OwnerEmpire.AutoResearch)
                    this.RunResearchPlanner();
                if (this.OwnerEmpire.data.AutoTaxes)
                    this.RunEconomicPlanner();
            }
        }

        public Array<Planet> GetKnownPlanets()
        {
            var knownPlanets = new Array<Planet>();
            foreach (SolarSystem s in UniverseScreen.SolarSystemList)
            {
                if (!s.ExploredDict[OwnerEmpire]) continue;
                knownPlanets.AddRange(s.PlanetList);
            }
            return knownPlanets;
        }
        
        public SolarSystem AssignExplorationTarget(Ship queryingShip)
        {
            Array<SolarSystem> potentials = new Array<SolarSystem>();
            foreach (SolarSystem s in UniverseScreen.SolarSystemList)
            {
                if (s.ExploredDict[this.OwnerEmpire])                
                    continue;
                
                potentials.Add(s);
            }
            
            using (MarkedForExploration.AcquireReadLock())
            foreach (SolarSystem s in this.MarkedForExploration)            
                potentials.Remove(s);
            
            IOrderedEnumerable<SolarSystem> sortedList =
                from system in potentials
                orderby Vector2.Distance(this.OwnerEmpire.GetWeightedCenter(), system.Position)
                select system;
            if (!sortedList.Any())
            {
                queryingShip.AI.OrderQueue.Clear();
                return null;
            }
            SolarSystem nearesttoHome = sortedList.OrderBy(furthest => Vector2.Distance(this.OwnerEmpire.GetWeightedCenter(), furthest.Position)).FirstOrDefault(); ;
            foreach (SolarSystem nearest in sortedList)
            {
                if (nearest.CombatInSystem) continue;
                float distanceToScout = Vector2.Distance(queryingShip.Center, nearest.Position);
                float distanceToEarth = Vector2.Distance(this.OwnerEmpire.GetWeightedCenter(), nearest.Position);

                if (distanceToScout > distanceToEarth + 50000f)                
                    continue;
                
                nearesttoHome = nearest;
                break;

            }
            this.MarkedForExploration.Add(nearesttoHome);
            return nearesttoHome;
        }

        public void RemoveShipFromForce(Ship ship, AO ao = null)
        {
            if (ship == null) return;
            OwnerEmpire.ForcePoolRemove(ship);
            ao?.RemoveShip(ship);
            DefensiveCoordinator.Remove(ship);

        }

        public void AssignShipToForce(Ship toAdd)
        {            
            if (toAdd.fleet != null ||OwnerEmpire.GetShipsFromOffensePools().Contains(toAdd) )            
                Log.Error("ship in {0}", toAdd.fleet?.Name ?? "force Pool");
            
            int numWars = OwnerEmpire.AtWarCount;
            
            float baseDefensePct = 0.1f;
            baseDefensePct = baseDefensePct + 0.15f * numWars;
            if(toAdd .hasAssaultTransporter || toAdd.BombCount >0 || toAdd.HasTroopBay || toAdd.BaseStrength ==0 || toAdd.WarpThrust <= 0 || toAdd.GetStrength() < toAdd.BaseStrength || !toAdd.BaseCanWarp && !OwnerEmpire.GetForcePool().Contains(toAdd))
            {
                OwnerEmpire.GetForcePool().Add(toAdd);
                return;
            }

            if (baseDefensePct > 0.35f)            
                baseDefensePct = 0.35f;            
            
            bool needDef = OwnerEmpire.currentMilitaryStrength * baseDefensePct - DefStr >0 && DefensiveCoordinator.DefenseDeficit >0;

            if (needDef)
            {
                DefensiveCoordinator.AddShip(toAdd);
                return;
            }
            //need to rework this better divide the ships. 
            AO area = AreasOfOperations.FindMin(ao => toAdd.Position.SqDist(ao.Position));
            if (!area?.AddShip(toAdd) ?? false )
                OwnerEmpire.GetForcePool().Add(toAdd);

        }

        private Vector2 FindAveragePosition(Empire e)
        {
            Vector2 avgPos = new Vector2();
            foreach (Planet p in e.GetPlanets())            
                avgPos = avgPos + p.Position;
            
            if (e.GetPlanets().Count <= 0)            
                return Vector2.Zero;
            
            Vector2 count = avgPos / e.GetPlanets().Count;
            return count;
        }

        //Added by McShooterz: used for AI to get defensive structures to build around planets
        public string GetDefenceSatellite()
        {
            Array<Ship> potentialSatellites = new Array<Ship>();
            foreach (string platform in this.OwnerEmpire.structuresWeCanBuild)
            {
                Ship orbitalDefense = ResourceManager.ShipsDict[platform];
                if (platform != "Subspace Projector" && orbitalDefense.shipData.Role == ShipData.RoleName.platform && orbitalDefense.BaseStrength > 0)
                    potentialSatellites.Add(orbitalDefense);
            }
            if (!potentialSatellites.Any())
                return "";
            int index = RandomMath.InRange(potentialSatellites.Count());
            return potentialSatellites[index].Name;
        }

        public string GetStarBase()
        {
            Array<Ship> potentialSatellites = new Array<Ship>();
            foreach (string platform in this.OwnerEmpire.structuresWeCanBuild)
            {
                Ship orbitalDefense = ResourceManager.ShipsDict[platform];
                if (orbitalDefense.shipData.Role == ShipData.RoleName.station && (orbitalDefense.shipData.IsOrbitalDefense || !orbitalDefense.shipData.IsShipyard))                
                    potentialSatellites.Add(orbitalDefense);                
            }
            if (!potentialSatellites.Any())
                return "";
            int index = RandomMath.InRange((int)(potentialSatellites.Count()*.5f));
            return potentialSatellites.OrderByDescending(tech=> tech.shipData.TechScore).ThenByDescending(stre=>stre.shipData.BaseStrength).Skip(index).FirstOrDefault().Name;
        }
        
        public float GetDistanceFromOurAO(Planet p)
        {
            IOrderedEnumerable<AO> sortedList = 
                from area in this.AreasOfOperations
                orderby Vector2.Distance(p.Position, area.Position)
                select area;
            if (!sortedList.Any())            
                return 0f;
            
            return Vector2.Distance(p.Position, sortedList.First<AO>().Position);
        }
        
        public void InitialzeAOsFromSave(UniverseData data)
        {
            foreach (AO area in AreasOfOperations)
            {
                area.InitFromSave(data, OwnerEmpire);                
            }
        }
        
        public void ManageAOs()
        {
            Array<AO> aOs = new Array<AO>();
            float empireStr = this.OwnerEmpire.currentMilitaryStrength;
            foreach (AO areasOfOperation in this.AreasOfOperations)
            {
                
                if (areasOfOperation.GetPlanet().Owner != OwnerEmpire)
                {
                    aOs.Add(areasOfOperation);
                    continue;
                }                
                areasOfOperation.ThreatLevel = (int)ThreatMatrix.PingRadarStrengthLargestCluster(areasOfOperation.Position, areasOfOperation.Radius, OwnerEmpire);

                int min = (int)(areasOfOperation.GetOffensiveForcePool().Sum(str => str.BaseStrength) *.5f);
                if (areasOfOperation.ThreatLevel < min)
                    areasOfOperation.ThreatLevel = min;
                
            }
            foreach (AO aO1 in aOs)
            {
                this.AreasOfOperations.Remove(aO1);
            }
            Array<Planet> planets = new Array<Planet>();
            foreach (Planet planet1 in this.OwnerEmpire.GetPlanets())
            {
                if (planet1.GetMaxProductionPotential() <= 5f || !planet1.HasShipyard)                
                    continue;
                
                bool flag = false;
                foreach (AO areasOfOperation1 in this.AreasOfOperations)
                {
                    if (areasOfOperation1.GetPlanet() != planet1)                    
                        continue;
                    
                    flag = true;
                    break;
                }
                if (flag)                
                    continue;
                
                planets.Add(planet1);
            }
            if (planets.Count == 0)
            {
                return;
            }
            IOrderedEnumerable<Planet> maxProductionPotential =
                from planet in planets
                orderby planet.GetMaxProductionPotential() descending
                select planet;
            
            foreach (Planet planet2 in maxProductionPotential)
            {
                float aoSize = 0;
                foreach (SolarSystem system in planet2.system.FiveClosestSystems)
                {
                    if (aoSize < Vector2.Distance(planet2.Position, system.Position))
                        aoSize = Vector2.Distance(planet2.Position, system.Position);
                }
                float aomax = Empire.Universe.UniverseRadius * .2f;
                if (aoSize > aomax)
                    aoSize = aomax;
                bool flag1 = true;
                foreach (AO areasOfOperation2 in this.AreasOfOperations)
                {

                    if (Vector2.Distance(areasOfOperation2.GetPlanet().Position, planet2.Position) >= aoSize)
                        continue;
                    flag1 = false;
                    break;
                }
                if (!flag1)
                {
                    continue;
                }

                AO aO2 = new AO(planet2, aoSize);
                this.AreasOfOperations.Add(aO2);
            }
        }

        public void RunEventChecker(KeyValuePair<Empire, Relationship> them)
        {
            if (OwnerEmpire == Empire.Universe.PlayerEmpire || OwnerEmpire.isFaction || !them.Value.Known)
                return;

            Array<Planet> ourTargetPlanets = new Array<Planet>();
            Array<Planet> theirTargetPlanets = new Array<Planet>();
            foreach (Goal g in this.Goals)
            {
                if (g.type == GoalType.Colonize)
                    ourTargetPlanets.Add(g.GetMarkedPlanet());
            }
            foreach (Goal g in them.Key.GetGSAI().Goals)
            {
                if (g.type == GoalType.Colonize)
                    theirTargetPlanets.Add(g.GetMarkedPlanet());
            }
            SolarSystem sharedSystem = null;
            them.Key.GetShips().ForEach(ship =>
            {
                if (ship.AI.State != AIState.Colonize || ship.AI.ColonizeTarget == null)                
                    return;
                
                theirTargetPlanets.Add(ship.AI.ColonizeTarget);
            }, false, false);

            foreach (Planet p in ourTargetPlanets)
            {
                bool matchFound = false;
                foreach (Planet other in theirTargetPlanets)
                {
                    if (p == null || other == null || p.system != other.system)                    
                        continue;
                    
                    sharedSystem = p.system;
                    matchFound = true;
                    break;
                }
                if (matchFound)
                    break;
            }

            if (sharedSystem != null && !them.Value.AtWar && !them.Value.WarnedSystemsList.Contains(sharedSystem.guid))
            {
                bool theyAreThereAlready = false;
                foreach (Planet p in sharedSystem.PlanetList)
                {
                    if (p.Owner == null || p.Owner != Empire.Universe.PlayerEmpire)                    
                        continue;                    
                    theyAreThereAlready = true;
                }
                if (!theyAreThereAlready)
                {
                    if (them.Key == Empire.Universe.PlayerEmpire)                    
                        Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, OwnerEmpire, Empire.Universe.PlayerEmpire, "Claim System", sharedSystem));
                    
                    them.Value.WarnedSystemsList.Add(sharedSystem.guid);
                }
            }
        }

        public void TriggerRefit()
        {

            bool TechCompare(int[] original, int[] newTech)
            {
                bool Compare(int o, int n) => o > 0 && o > n;
                
                for (int x = 0; x < 4; x++)                
                    if (!Compare(original[x], newTech[x])) return false;
                
                return true;
            }

            int upgrades = 0;
            var offPool =OwnerEmpire.GetShipsFromOffensePools();
            for (int i = offPool.Count - 1; i >= 0; i--)
            {
                Ship ship = offPool[i];
                if (upgrades < 5)
                {
                    int techScore = ship.GetTechScore(out int[] origTechs);
                    string name = "";

                    foreach (string shipName in OwnerEmpire.ShipsWeCanBuild)
                    {
                        Ship newTemplate = ResourceManager.GetShipTemplate(shipName);
                        if (newTemplate.GetShipData().Hull != ship.GetShipData().Hull)                                                        
                            continue;
                        int newScore =newTemplate.GetTechScore(out int[] newTech);

                        if(newScore <= techScore || !TechCompare(origTechs, newTech)) continue;                        

                        name      = shipName;
                        techScore = newScore;
                        origTechs = newTech;
                    }
                    if (!string.IsNullOrEmpty(name))
                    {
                        ship.AI.OrderRefitTo(name);
                        ++upgrades;
                    }                    
                }
                else
                    break;
            }
        }

        public void Update()
        {		    
            DefStr = this.DefensiveCoordinator.GetForcePoolStrength();
            if (!this.OwnerEmpire.isFaction)            
                this.RunManagers();
            
            foreach (Goal g in this.Goals) g.Evaluate();
            
            this.Goals.ApplyPendingRemovals();
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~EmpireAI() { Dispose(false); }

        private void Dispose(bool disposing)
        {
            TaskList?.Dispose(ref TaskList);
            DefensiveCoordinator?.Dispose(ref DefensiveCoordinator);
            Goals?.Dispose(ref Goals);
        }
    }
}
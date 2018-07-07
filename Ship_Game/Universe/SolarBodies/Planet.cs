using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.Universe.SolarBodies;

namespace Ship_Game
{

    public sealed class Planet : SolarSystemBody, IDisposable
    {
        public enum ColonyType
        {
            Core,
            Colony,
            Industrial,
            Research,
            Agricultural,
            Military,
            TradeHub,
        }
        public GeodeticManager GeodeticManager;
        public SBCommodities SbCommodities;

        public TroopManager TroopManager;
        public bool GovBuildings = true;
        public bool GovSliders = true;
        public float ProductionHere
        {
            get => SbCommodities.ProductionHere;
            set => SbCommodities.ProductionHere = value;
        }

        public float FoodHere
        {
            get => SbCommodities.FoodHere;
            set => SbCommodities.FoodHere = value;
        }
        public float Population
        {
            get => SbCommodities.Population;
            set => SbCommodities.Population = value;
        }

        public GoodState FS = GoodState.STORE;      //I dont like these names, but changing them will affect a lot of files
        public GoodState PS = GoodState.STORE;
        public GoodState GetGoodState(string good)
        {
            switch (good)
            {
                case "Food":
                    return FS;
                case "Production":
                    return PS;
            }
            return 0;
        }        
        public SpaceStation Station = new SpaceStation();        
        
        public float FarmerPercentage = 0.34f;
        public float WorkerPercentage = 0.33f;
        public float ResearcherPercentage = 0.33f;        
        public float MaxStorage = 10f;                
        public bool FoodLocked;
        public bool ProdLocked;
        public bool ResLocked;
        
        public int CrippledTurns;        
        
        public float NetFoodPerTurn;
        public float GetNetGoodProd(string good)
        {
            switch (good)
            {
                case "Food":
                    return NetFoodPerTurn;
                case "Production":
                    return NetProductionPerTurn;
            }
            return 0;
        }
        public float GetMaxGoodProd(string good)
        {
            switch (good)
            {
                case "Food":
                    return NetFoodPerTurn;
                case "Production":
                    return MaxProductionPerTurn;
            }
            return 0;
        }
        //public float FoodPercentAdded;  //This variable is never used... -Gretman
        public float FlatFoodAdded;
        public float NetProductionPerTurn;
        private float MaxProductionPerTurn;
        public float GrossProductionPerTurn;
        public float PlusFlatProductionPerTurn;
        public float NetResearchPerTurn;
        public float PlusTaxPercentage;
        public float PlusFlatResearchPerTurn;
        //public float ResearchPercentAdded;      //This is never used
        public float PlusResearchPerColonist;
        public float TotalMaintenanceCostsPerTurn;
        public float PlusFlatMoneyPerTurn;
        private float PlusFoodPerColonist;
        public float PlusProductionPerColonist;
        public float MaxPopBonus;
        public bool AllowInfantry;
        public float PlusFlatPopulationPerTurn;
        public int TotalDefensiveStrength;
        public float GrossFood;
        public float GrossMoneyPT;
        public float GrossIncome =>
                    (this.GrossMoneyPT + this.GrossMoneyPT * (float)this.Owner?.data.Traits.TaxMod) * (float)this.Owner?.data.TaxRate
                    + this.PlusFlatMoneyPerTurn + (this.Population / 1000f * this.PlusCreditsPerColonist);
        public float GrossUpkeep =>
                    (float)((double)this.TotalMaintenanceCostsPerTurn + (double)this.TotalMaintenanceCostsPerTurn
                    * (double)this.Owner?.data.Traits.MaintMod);
        public float NetIncome => this.GrossIncome - this.GrossUpkeep;
        public float PlusCreditsPerColonist;
        public bool HasWinBuilding;
        public float ShipBuildingModifier;
        public float Consumption;
        private float Unfed;
        
        public float GetGoodHere(string good)
        {
            switch (good)
            {
                case "Food":
                    return FoodHere;
                case "Production":
                    return ProductionHere;
            }
            return 0;
        }
        public Array<string> CommoditiesPresent => SbCommodities.CommoditiesPresent;
        public bool CorsairPresence;
        public bool QueueEmptySent = true;
        public float RepairPerTurn = 0;        

        public float TradeIncomingColonists = 0;

        public bool RecentCombat                                          => TroopManager.RecentCombat;
        public float GetDefendingTroopStrength()                          => TroopManager.GetDefendingTroopStrength();
        public int CountEmpireTroops(Empire us)                           => TroopManager.CountEmpireTroops(us);
        public int GetDefendingTroopCount()                               => TroopManager.GetDefendingTroopCount();
        public bool AnyOfOurTroops(Empire us)                             => TroopManager.AnyOfOurTroops(us);
        public float GetGroundStrength(Empire empire)                     => TroopManager.GetGroundStrength(empire);
        public int GetPotentialGroundTroops()                             => TroopManager.GetPotentialGroundTroops();
        public float GetGroundStrengthOther(Empire AllButThisEmpire)      => TroopManager.GetGroundStrengthOther(AllButThisEmpire);
        public bool TroopsHereAreEnemies(Empire empire)                   => TroopManager.TroopsHereAreEnemies(empire);
        public int GetGroundLandingSpots()                                => TroopManager.GetGroundLandingSpots();
        public Array<Troop> GetEmpireTroops(Empire empire, int maxToTake) => TroopManager.GetEmpireTroops(empire, maxToTake);
        public void HealTroops()                                          => TroopManager.HealTroops();

        public Planet()
        {
            TroopManager = new TroopManager(this, Habitable);
            GeodeticManager = new GeodeticManager(this);
            SbCommodities = new SBCommodities(this);
            base.SbProduction = new SBProduction(this);
            HasShipyard = false;

            foreach (KeyValuePair<string, Good> keyValuePair in ResourceManager.GoodsDict)
                AddGood(keyValuePair.Key, 0);
        }

        public Planet(SolarSystem system, float randomAngle, float ringRadius, string name, float ringMax, Empire owner = null)
        {                        
            var newOrbital = this;
            TroopManager = new TroopManager(this, Habitable);
            GeodeticManager = new GeodeticManager(this);
            SbCommodities = new SBCommodities(this);
            SbProduction = new SBProduction(this);
            Name = name;
            OrbitalAngle = randomAngle;
            ParentSystem = system;
                
            
            SunZone sunZone;
            float zoneSize = ringMax;
            if (ringRadius < zoneSize * .15f)
                sunZone = SunZone.Near;
            else if (ringRadius < zoneSize * .25f)
                sunZone = SunZone.Habital;
            else if (ringRadius < zoneSize * .7f)
                sunZone = SunZone.Far;
            else
                sunZone = SunZone.VeryFar;
            if (owner != null && owner.Capital == null && sunZone >= SunZone.Habital)
            {
                PlanetType = RandomMath.IntBetween(0, 1) == 0 ? 27 : 29;
                owner.SpawnHomePlanet(newOrbital);
                Name = ParentSystem.Name + " " + NumberToRomanConvertor.NumberToRoman(1);
            }
            else            
            {
                GenerateType(sunZone);
                newOrbital.SetPlanetAttributes(true);
            }
            
            float zoneBonus = ((int)sunZone +1) * .2f * ((int)sunZone +1);
            float scale = RandomMath.RandomBetween(0f, zoneBonus) + .9f;
            if (newOrbital.PlanetType == 2 || newOrbital.PlanetType == 6 || newOrbital.PlanetType == 10 ||
                newOrbital.PlanetType == 12 || newOrbital.PlanetType == 15 || newOrbital.PlanetType == 20 ||
                newOrbital.PlanetType == 26)
                scale += 2.5f;

            float planetRadius       = 1000f * (float)(1 + (Math.Log(scale) / 1.5));
            newOrbital.ObjectRadius  = planetRadius;
            newOrbital.OrbitalRadius = ringRadius + planetRadius;
            Vector2 planetCenter     = MathExt.PointOnCircle(randomAngle, ringRadius);
            newOrbital.Center        = planetCenter;
            newOrbital.Scale         = scale;            
            newOrbital.PlanetTilt    = RandomMath.RandomBetween(45f, 135f);


            GenerateMoons(newOrbital);

            if (RandomMath.RandomBetween(1f, 100f) < 15f)
            {
                newOrbital.HasRings = true;
                newOrbital.RingTilt = RandomMath.RandomBetween(-80f, -45f);
            }
          
        }

        public void SetInGroundCombat()
        {
            TroopManager.SetInCombat();
        }

        public Goods ImportPriority()
        {
            if (NetFoodPerTurn <= 0 || FarmerPercentage > .5f)
            {
                if (ConstructingGoodsBuilding(Goods.Food))
                    return Goods.Production;
                return Goods.Food;
            }
            if (ConstructionQueue.Count > 0) return Goods.Production;
            if (PS == GoodState.IMPORT) return Goods.Production;
            if (FS == GoodState.IMPORT) return Goods.Food;
            return Goods.Food;
        }

        public bool ConstructingGoodsBuilding(Goods goods)
        {
            if (ConstructionQueue.IsEmpty) return false;
            switch (goods)
            {
                case Goods.Production:
                    foreach (var item in ConstructionQueue)
                    {
                        if (item.isBuilding && item.Building.ProducesProduction)
                        {
                            return true;
                        }
                    }
                    break;
                case Goods.Food:
                    foreach (var item in ConstructionQueue)
                    {
                        if (item.isBuilding && item.Building.ProducesFood)
                        {
                            return true;
                        }
                    }
                    break;
                case Goods.Colonists:
                    break;
                default:
                    break;
            }
            return false;
        }

        public float EmpireFertility(Empire empire) =>
            (empire.data?.Traits.Cybernetic ?? 0) > 0 ? MineralRichness : Fertility;            

        public float EmpireBaseValue(Empire empire) => (
            CommoditiesPresent.Count +
            (1 + EmpireFertility(empire))
            * (1 + MineralRichness )
            * (float)Math.Ceiling(MaxPopulation / 1000f)
            );

        public bool NeedsFood()
        {
            if (Owner?.isFaction ?? true) return false;
            bool cyber = Owner.data.Traits.Cybernetic > 0;
            float food = cyber ? ProductionHere : FoodHere;
            bool badProduction = cyber ? NetProductionPerTurn <= 0 && WorkerPercentage > .5f : 
                (NetFoodPerTurn <= 0 && FarmerPercentage >.5f);
            return food / MaxStorage < .10f || badProduction;
        }
        
        public void AddProjectile(Projectile projectile)
        {
            Projectiles.Add(projectile);
        }

        //added by gremlin deveks drop bomb
        public void DropBomb(Bomb bomb) => GeodeticManager.DropBomb(bomb);        

        public float GetNetFoodPerTurn()
        {
            if (Owner != null && Owner.data.Traits.Cybernetic == 1)
                return NetFoodPerTurn;
            else
                return NetFoodPerTurn - Consumption;
        }

        public void ApplyAllStoredProduction(int index) => SbProduction.ApplyAllStoredProduction(index);

        public bool ApplyStoredProduction(int index) => SbProduction.ApplyStoredProduction(index);

        public void ApplyProductiontoQueue(float howMuch, int whichItem) => SbProduction.ApplyProductiontoQueue(howMuch, whichItem);

        public float GetNetProductionPerTurn()
        {
            if (Owner != null && Owner.data.Traits.Cybernetic == 1)
                return NetProductionPerTurn - Consumption;
            else
                return NetProductionPerTurn;
        }

        public bool TryBiosphereBuild(Building b, QueueItem qi) => SbProduction.TryBiosphereBuild(b, qi);

        public void Update(float elapsedTime)
        {
    
            Array<Guid> list = new Array<Guid>();
            foreach (KeyValuePair<Guid, Ship> keyValuePair in Shipyards)
            {
                if (!keyValuePair.Value?.Active ?? true //Remove this null check later. 
                    || keyValuePair.Value.Size == 0)
                    list.Add(keyValuePair.Key);
            }
            foreach (Guid key in list)
                Shipyards.Remove(key);
            TroopManager.Update(elapsedTime);
            GeodeticManager.Update(elapsedTime);
           
            for (int index1 = 0; index1 < BuildingList.Count; ++index1)
            {
                //try
                {
                    Building building = BuildingList[index1];
                    if (building.isWeapon)
                    {
                        building.WeaponTimer -= elapsedTime;
                        if (building.WeaponTimer < 0 && ParentSystem.ShipList.Count > 0)
                        {
                            if (Owner != null)
                            {
                                Ship target = null;
                                Ship troop = null;
                                float currentD = 0;
                                float previousD = building.theWeapon.Range + 1000f;
                                //float currentT = 0;
                                float previousT = building.theWeapon.Range + 1000f;
                                //this.system.ShipList.thisLock.EnterReadLock();
                                for (int index2 = 0; index2 < ParentSystem.ShipList.Count; ++index2)
                                {
                                    Ship ship = ParentSystem.ShipList[index2];
                                    if (ship.loyalty == Owner || (!ship.loyalty.isFaction && Owner.GetRelations(ship.loyalty).Treaty_NAPact) )
                                        continue;
                                    currentD = Vector2.Distance(Center, ship.Center);                                   
                                    if (ship.shipData.Role == ShipData.RoleName.troop && currentD  < previousT)
                                    {
                                        previousT = currentD;
                                        troop = ship;
                                        continue;
                                    }
                                    if(currentD < previousD && troop ==null)
                                    {
                                        previousD = currentD;
                                        target = ship;
                                    }

                                }

                                if (troop != null)
                                    target = troop;
                                if(target != null)
                                {
                                    building.theWeapon.Center = Center;
                                    building.theWeapon.FireFromPlanet(this, target);
                                    building.WeaponTimer = building.theWeapon.fireDelay;
                                    break;
                                }


                            }
                        }
                    }
                }
            }
            for (int index = 0; index < Projectiles.Count; ++index)
            {
                Projectile projectile = Projectiles[index];
                if (projectile.Active)
                {
                    if (elapsedTime > 0)
                        projectile.Update(elapsedTime);
                }
                else
                    Projectiles.QueuePendingRemoval(projectile);
            }
            Projectiles.ApplyPendingRemovals();
            UpdatePosition(elapsedTime);
        }
   
        public void TerraformExternal(float amount)
        {
            Fertility += amount;
            if (Fertility <= 0.0)
            {
                Fertility = 0.0f;
                PlanetType = 7;
                Terraform();
            }
            else if (Type == "Barren" && Fertility > 0.01)
            {
                PlanetType = 14;
                Terraform();
            }
            else if (Type == "Desert" && Fertility > 0.35)
            {
                PlanetType = 18;
                Terraform();
            }
            else if (Type == "Ice" && Fertility > 0.35)
            {
                PlanetType = 19;
                Terraform();
            }
            else if (Type == "Swamp" && Fertility > 0.75)
            {
                PlanetType = 21;
                Terraform();
            }
            else if (Type == "Steppe" && Fertility > 0.6)
            {
                PlanetType = 11;
                Terraform();
            }
            else
            {
                if (!(Type == "Tundra") || Fertility <= 0.95)
                    return;
                PlanetType = 22;
                Terraform();
            }
        }

        public void UpdateOwnedPlanet()
        {
            ++TurnsSinceTurnover;
            if (CrippledTurns > 0) CrippledTurns--;
            else CrippledTurns = 0;

            ConstructionQueue.ApplyPendingRemovals();
            UpdateDevelopmentStatus();
            Description = DevelopmentStatus;
            GeodeticManager.AffectNearbyShips();
            TerraformPoints += TerraformToAdd;
            if (TerraformPoints > 0.0f && Fertility < 1.0)
            {
                Fertility += TerraformToAdd;
                if (Type == "Barren" && Fertility > 0.01)
                {
                    PlanetType = 14;
                    Terraform();
                }
                else if (Type == "Desert" && Fertility > 0.35)
                {
                    PlanetType = 18;
                    Terraform();
                }
                else if (Type == "Ice" && Fertility > 0.35)
                {
                    PlanetType = 19;
                    Terraform();
                }
                else if (Type == "Swamp" && Fertility > 0.75)
                {
                    PlanetType = 21;
                    Terraform();
                }
                else if (Type == "Steppe" && Fertility > 0.6)
                {
                    PlanetType = 11;
                    Terraform();
                }
                else if (Type == "Tundra" && Fertility > 0.95)
                {
                    PlanetType = 22;
                    Terraform();
                }
                if (Fertility > 1.0)
                    Fertility = 1f;
            }
            DoGoverning();
            UpdateIncomes(false);

            // notification about empty queue
            if (GlobalStats.ExtraNotifications && Owner != null && Owner.isPlayer)
            {
                if (ConstructionQueue.Count == 0 && !QueueEmptySent)
                {
                    if (colonyType == ColonyType.Colony || colonyType == ColonyType.Core || colonyType == ColonyType.Industrial || !GovernorOn)
                    {
                        QueueEmptySent = true;
                        Empire.Universe.NotificationManager.AddEmptyQueueNotification(this);
                    }
                }
                else if (ConstructionQueue.Count > 0)
                {
                    QueueEmptySent = false;
                }
            }

            if (ShieldStrengthCurrent < ShieldStrengthMax)
            {
                Planet shieldStrengthCurrent = this;

                if (!RecentCombat)
                {

                    if (ShieldStrengthCurrent > ShieldStrengthMax / 10)
                    {
                        shieldStrengthCurrent.ShieldStrengthCurrent += shieldStrengthCurrent.ShieldStrengthMax / 10;
                    }
                    else
                    {
                        shieldStrengthCurrent.ShieldStrengthCurrent++;
                    }
                }
                if (ShieldStrengthCurrent > ShieldStrengthMax)
                    ShieldStrengthCurrent = ShieldStrengthMax;
            }

            //this.UpdateTimer = 10f;
            HarvestResources();
            ApplyProductionTowardsConstruction();
            GrowPopulation();
            HealTroops();
            CalculateIncomingTrade();
        }

        public float IncomingFood = 0;
        public float IncomingProduction = 0;
        public float IncomingColonists = 0;

        public void UpdateDevelopmentStatus()
        {
            Density = Population / 1000f;
            float maxPop = MaxPopulation / 1000f;
            if (Density <= 0.5f)
            {
                DevelopmentLevel = 1;
                DevelopmentStatus = Localizer.Token(1763);
                if (maxPop >= 2 && Type != "Barren")
                {
                    var planet = this;
                    string str = planet.DevelopmentStatus + Localizer.Token(1764);
                    planet.DevelopmentStatus = str;
                }
                else if (maxPop >= 2f && Type == "Barren")
                {
                    var planet = this;
                    string str = planet.DevelopmentStatus + Localizer.Token(1765);
                    planet.DevelopmentStatus = str;
                }
                else if (maxPop < 0 && Type != "Barren")
                {
                    var planet = this;
                    string str = planet.DevelopmentStatus + Localizer.Token(1766);
                    planet.DevelopmentStatus = str;
                }
                else if (maxPop < 0.5f && Type == "Barren")
                {
                    var planet = this;
                    string str = planet.DevelopmentStatus + Localizer.Token(1767);
                    planet.DevelopmentStatus = str;
                }
            }
            else if (Density > 0.5f && Density <= 2)
            {
                DevelopmentLevel = 2;
                DevelopmentStatus = Localizer.Token(1768);
                if (maxPop >= 2)
                {
                    var planet = this;
                    string str = planet.DevelopmentStatus + Localizer.Token(1769);
                    planet.DevelopmentStatus = str;
                }
                else if (maxPop < 2)
                {
                    var planet = this;
                    string str = planet.DevelopmentStatus + Localizer.Token(1770);
                    planet.DevelopmentStatus = str;
                }
            }
            else if (Density > 2.0 && Density <= 5.0)
            {
                DevelopmentLevel = 3;
                DevelopmentStatus = Localizer.Token(1771);
                if (maxPop >= 5.0)
                {
                    var planet = this;
                    string str = planet.DevelopmentStatus + Localizer.Token(1772);
                    planet.DevelopmentStatus = str;
                }
                else if (maxPop < 5.0)
                {
                    var planet = this;
                    string str = planet.DevelopmentStatus + Localizer.Token(1773);
                    planet.DevelopmentStatus = str;
                }
            }
            else if (Density > 5.0 && Density <= 10.0)
            {
                DevelopmentLevel = 4;
                DevelopmentStatus = Localizer.Token(1774);
            }
            else if (Density > 10.0)
            {
                DevelopmentLevel = 5;
                DevelopmentStatus = Localizer.Token(1775);
            }
            if (NetProductionPerTurn >= 10.0 && HasShipyard)
            {
                var planet = this;
                string str = planet.DevelopmentStatus + Localizer.Token(1776);
                planet.DevelopmentStatus = str;
            }
            else if (Fertility >= 2.0 && NetFoodPerTurn > (double)MaxPopulation)
            {
                var planet = this;
                string str = planet.DevelopmentStatus + Localizer.Token(1777);
                planet.DevelopmentStatus = str;
            }
            else if (NetResearchPerTurn > 5.0)
            {
                var planet = this;
                string str = planet.DevelopmentStatus + Localizer.Token(1778);
                planet.DevelopmentStatus = str;
            }
            if (!AllowInfantry || TroopsHere.Count <= 6)
                return;
            var planet1 = this;
            string str1 = planet1.DevelopmentStatus + Localizer.Token(1779);
            planet1.DevelopmentStatus = str1;
        }

        private static bool AddToIncomingTrade(ref float type, float amount)
        {
            if (amount < 1) return false;
            type += amount;
            return true;
        }

        private void CalculateIncomingTrade()
        {
            if (Owner == null || Owner.isFaction) return;
            IncomingProduction = 0;
            IncomingFood = 0;
            TradeIncomingColonists = 0;
            using (Owner.GetShips().AcquireReadLock())
            {
                foreach (var ship in Owner.GetShips())
                {
                    if (ship.DesignRole != ShipData.RoleName.freighter) continue;
                    if (ship.AI.end != this) continue;
                    if (ship.AI.State != AIState.SystemTrader && ship.AI.State != AIState.PassengerTransport) continue;

                    if (AddToIncomingTrade(ref IncomingFood, ship.GetFood())) return;
                    if (AddToIncomingTrade(ref IncomingProduction, ship.GetProduction())) return;
                    if (AddToIncomingTrade(ref IncomingColonists, ship.GetColonists())) return;

                    if (AddToIncomingTrade(ref IncomingFood, ship.CargoSpaceMax * (ship.AI.FoodOrProd == "Food" ? 1 : 0))) return;
                    if (AddToIncomingTrade(ref IncomingProduction, ship.CargoSpaceMax * (ship.AI.FoodOrProd == "Prod" ? 1 : 0))) return;
                    if (AddToIncomingTrade(ref IncomingColonists, ship.CargoSpaceMax)) return;
                }
            }
        }  

        public void RefreshBuildingsWeCanBuildHere()
        {
            if (Owner == null) return;
            BuildingsCanBuild.Clear();

            //See if it already has a command building or not.
            bool needCommandBuilding = true;
            foreach (Building building in BuildingList)
            {
                if (building.Name == "Capital City" || building.Name == "Outpost")
                {
                    needCommandBuilding = false;
                    break;
                }
            }

            foreach (KeyValuePair<string, bool> keyValuePair in Owner.GetBDict())
            {
                if (!keyValuePair.Value) continue;
                Building building1 = ResourceManager.BuildingsDict[keyValuePair.Key];

                //Skip adding +food buildings for cybernetic races
                if (Owner.data.Traits.Cybernetic > 0 && (building1.PlusFlatFoodAmount > 0 || building1.PlusFoodPerColonist > 0)) continue;

                //Skip adding command buildings if planet already has one
                if (!needCommandBuilding && (building1.Name == "Outpost" || building1.Name == "Capital City")) continue;

                bool foundIt = false;

                //Make sure the building isn't already built on this planet
                foreach (Building building2 in BuildingList)
                {
                    if (!building2.Unique) continue;

                    if (building2.Name == building1.Name)
                    {
                        foundIt = true;
                        break;
                    }
                }
                if (foundIt) continue;

                //Make sure the building isn't already being built on this planet
                for (int index = 0; index < ConstructionQueue.Count; ++index)
                {
                    QueueItem queueItem = ConstructionQueue[index];
                    if (queueItem.isBuilding && queueItem.Building.Name == building1.Name && queueItem.Building.Unique)
                    {
                        foundIt = true;
                        break;
                    }
                }
                if (foundIt) continue;

                //Hide Biospheres if the entire planet is already habitable
                if (building1.Name == "Biosphers")
                {
                    bool allHabitable = true;
                    foreach (PlanetGridSquare tile in TilesList)
                    {
                        if (!tile.Habitable)
                        {
                            allHabitable = false;
                            break;
                        }
                    }
                    if (allHabitable) continue;
                }

                //If this is a one-per-empire building, make sure it hasn't been built already elsewhere
                //Reusing fountIt bool from above
                if (building1.BuildOnlyOnce)
                {
                    //Check for this unique building across the empire
                    foreach (Planet planet in Owner.GetPlanets())
                    {
                        //First check built buildings
                        foreach (Building building2 in planet.BuildingList)
                        {
                            if (building2.Name == building1.Name)
                            {
                                foundIt = false;
                                break;
                            }
                        }
                        if (foundIt) break;

                        //Then check production queue
                        foreach (QueueItem queueItem in planet.ConstructionQueue)
                        {
                            if (queueItem.isBuilding && queueItem.Building.Name == building1.Name)
                            {
                                foundIt = true;
                                break;
                            }
                        }
                        if (foundIt) break;
                    }
                    if (foundIt) continue;
                }

                //If the building is still a candidate after all that, then add it to the list!
                BuildingsCanBuild.Add(building1);
            }
        }

        public void AddBuildingToCQ(Building b) => SbProduction.AddBuildingToCQ(b);
     
        public void AddBuildingToCQ(Building b, bool PlayerAdded) => SbProduction.AddBuildingToCQ(b, PlayerAdded);     

        public bool BuildingInQueue(string UID)
        {
            for (int index = 0; index < ConstructionQueue.Count; ++index)
            {
                if (ConstructionQueue[index].isBuilding && ConstructionQueue[index].Building.Name == UID)
                    return true;
            }
            return false;
        }

        public bool BuildingExists(string buildingName)
        {
            for (int i = 0; i < BuildingList.Count; ++i)
                if (BuildingList[i].Name == buildingName)
                    return true;
            return BuildingInQueue(buildingName);
            
        }

        public bool WeCanAffordThis(Building building, Planet.ColonyType governor)
        {
            if (governor == ColonyType.TradeHub)
                return true;
            if (building == null)
                return false;
            if (building.IsPlayerAdded)
                return true;
            Empire empire = Owner;
            float buildingMaintenance = empire.GetTotalBuildingMaintenance();
            float grossTaxes = empire.GrossTaxes;
          
            bool itsHere = BuildingList.Contains(building);
            
            foreach (QueueItem queueItem in ConstructionQueue)
            {
                if (queueItem.isBuilding)
                {
                    buildingMaintenance += Owner.data.Traits.MaintMod * queueItem.Building.Maintenance;
                    bool added =queueItem.Building == building;
                    if (added) itsHere = true;
                }
                
            }
            buildingMaintenance += building.Maintenance + building.Maintenance * Owner.data.Traits.MaintMod;
            
            bool LowPri = buildingMaintenance / grossTaxes < .25f;
            bool MedPri = buildingMaintenance / grossTaxes < .60f;
            bool HighPri = buildingMaintenance / grossTaxes < .80f;
            float income = GrossMoneyPT + Owner.data.Traits.TaxMod * GrossMoneyPT - (TotalMaintenanceCostsPerTurn + TotalMaintenanceCostsPerTurn * Owner.data.Traits.MaintMod);           
            float maintCost = GrossMoneyPT + Owner.data.Traits.TaxMod * GrossMoneyPT - building.Maintenance- (TotalMaintenanceCostsPerTurn + TotalMaintenanceCostsPerTurn * Owner.data.Traits.MaintMod);
            bool makingMoney = maintCost > 0;
      
            int defensiveBuildings = BuildingList.Count(combat => combat.SoftAttack > 0 || combat.PlanetaryShieldStrengthAdded >0 ||combat.theWeapon !=null);           
           int possibleoffensiveBuilding = BuildingsCanBuild.Count(b => b.PlanetaryShieldStrengthAdded > 0 || b.SoftAttack > 0 || b.theWeapon != null);
           bool isdefensive = building.SoftAttack > 0 || building.PlanetaryShieldStrengthAdded > 0 || building.isWeapon ;
           float defenseratio =0;
            if(defensiveBuildings+possibleoffensiveBuilding >0)
                defenseratio = (defensiveBuildings + 1) / (float)(defensiveBuildings + possibleoffensiveBuilding + 1);
            SystemCommander SC;
            bool needDefense =false;
            
            if (Owner.data.TaxRate > .5f)
                makingMoney = false;
            //dont scrap buildings if we can use treasury to pay for it. 
            if (building.AllowInfantry && !BuildingList.Contains(building) && (AllowInfantry || governor == ColonyType.Military))
                return false;

            //determine defensive needs.
            if (Owner.GetGSAI().DefensiveCoordinator.DefenseDict.TryGetValue(ParentSystem, out SC))
            {
                if (makingMoney)
                    needDefense = SC.RankImportance >= defenseratio *10; ;// / (defensiveBuildings + offensiveBuildings+1)) >defensiveNeeds;
            }
            
            if (!string.IsNullOrEmpty(building.ExcludesPlanetType) && building.ExcludesPlanetType == Type)
                return false;
            

            if (itsHere && building.Unique && (makingMoney || building.Maintenance < Owner.Money * .001))
                return true;

            if (building.PlusTaxPercentage * GrossMoneyPT >= building.Maintenance 
                || building.CreditsProduced(this) >= building.Maintenance 

                
                ) 
                return true;
            if (building.Name == "Outpost" || building.WinsGame  )
                return true;
            //dont build +food if you dont need to

            if (Owner.data.Traits.Cybernetic <= 0 && building.PlusFlatFoodAmount > 0)// && this.Fertility == 0)
            {

                if (NetFoodPerTurn > 0 && FarmerPercentage < .3 || BuildingExists(building.Name))

                    return false;
                else
                    return true;
               
            }
            if (Owner.data.Traits.Cybernetic < 1 && income > building.Maintenance ) 
            {
                float food = building.FoodProduced(this);
                if (food * FarmerPercentage > 1)
                {
                    return true;
                }
                else
                {
                    
                }
            }
            if(Owner.data.Traits.Cybernetic >0)
            {
                if(NetProductionPerTurn - Consumption <0)
                {
                    if(building.PlusFlatProductionAmount >0 && (WorkerPercentage > .5 || income >building.Maintenance*2))
                    {
                        return true;
                    }
                    if (building.PlusProdPerColonist > 0 && building.PlusProdPerColonist * (Population / 1000) > building.Maintenance *(2- WorkerPercentage))
                    {
                        if (income > ShipBuildingModifier * 2)
                            return true;

                    }
                    if (building.PlusProdPerRichness * MineralRichness > building.Maintenance )
                        return true;
                }
            }
            if(building.PlusTerraformPoints >0)
            {
                if (!makingMoney || Owner.data.Traits.Cybernetic>0|| BuildingList.Contains(building) || BuildingInQueue(building.Name))
                    return false;
                
            }
            if(!makingMoney || DevelopmentLevel < 3)
            {
                if (building.Name == "Biospheres")
                    return false;
            }
                
            bool iftrue = false;
            switch  (governor)
            {
                case ColonyType.Agricultural:
                    #region MyRegion
                    {
                        if (building.AllowShipBuilding && GetMaxProductionPotential()>20 )
                        {
                            return true;
                        }
                        if (Fertility > 0 && building.MinusFertilityOnBuild > 0 && Owner.data.Traits.Cybernetic <=0)
                            return false;
                        if (HighPri)
                        {
                            if (building.PlusFlatFoodAmount > 0
                                || (building.PlusFoodPerColonist > 0 && Population > 500f)
                                
                                //|| this.developmentLevel > 4
                                || ((building.MaxPopIncrease > 0
                                || building.PlusFlatPopulation > 0 || building.PlusTerraformPoints > 0) && Population > MaxPopulation * .5f)
                                || building.PlusFlatFoodAmount > 0
                                || building.PlusFlatProductionAmount > 0
                                || building.StorageAdded > 0 
                               // || (this.Owner.data.Traits.Cybernetic > 0 && (building.PlusProdPerRichness > 0 || building.PlusProdPerColonist > 0 || building.PlusFlatProductionAmount>0))
                                || (needDefense && isdefensive && DevelopmentLevel > 3)
                                )
                                return true;
                                //iftrue = true;
                            
                        }
                        if (!iftrue && MedPri && DevelopmentLevel > 2 && makingMoney)
                        {
                            if (
                                building.Name == "Biospheres"||
                                ( building.PlusTerraformPoints > 0 && Fertility < 3)
                                || building.MaxPopIncrease > 0 
                                || building.PlusFlatPopulation > 0
                                || DevelopmentLevel > 3
                                || building.PlusFlatResearchAmount > 0
                                || (building.PlusResearchPerColonist > 0 && MaxPopulation > 999)
                                || (needDefense && isdefensive )

                                )
                                return true;
                        }
                        if (LowPri && DevelopmentLevel > 4 && makingMoney)
                        {
                            iftrue = true;
                        }
                        break;
                    } 
                    #endregion
                case ColonyType.Core:
                    #region MyRegion
                    {
                        if (Fertility > 0 && building.MinusFertilityOnBuild > 0 && Owner.data.Traits.Cybernetic <= 0)
                            return false;
                        if (HighPri)
                        {

                            if (building.StorageAdded > 0
                                || (Owner.data.Traits.Cybernetic <=0 && (building.PlusTerraformPoints > 0 && Fertility < 1) && MaxPopulation > 2000)
                                || ((building.MaxPopIncrease > 0 || building.PlusFlatPopulation > 0) && Population == MaxPopulation && income > building.Maintenance)                             
                                || (Owner.data.Traits.Cybernetic <=0 && building.PlusFlatFoodAmount > 0)
                                || (Owner.data.Traits.Cybernetic <=0 && building.PlusFoodPerColonist > 0)                                
                                || building.PlusFlatProductionAmount > 0
                                || building.PlusProdPerRichness >0
                                || building.PlusProdPerColonist >0
                                || building.PlusFlatResearchAmount>0
                                || (building.PlusResearchPerColonist>0 && Population / 1000 > 1)
                                //|| building.Name == "Biospheres"                                
                                
                                || (needDefense && isdefensive && DevelopmentLevel > 3)                                
                                || (Owner.data.Traits.Cybernetic > 0 && (building.PlusProdPerRichness > 0 || building.PlusProdPerColonist > 0 || building.PlusFlatProductionAmount > 0))
                                )
                                return true;
                        }
                        if (MedPri && DevelopmentLevel > 3 &&makingMoney )
                        {
                            if (DevelopmentLevel > 2 && needDefense && (building.theWeapon != null || building.Strength > 0))
                                return true;
                            iftrue = true;
                        }
                        if (!iftrue && LowPri && DevelopmentLevel > 4 && makingMoney && income > building.Maintenance)
                        {
                            
                            iftrue = true;
                        }
                        break;
                    } 
                    #endregion
                case ColonyType.Industrial:
                    #region MyRegion
                    {
                        if (building.AllowShipBuilding && GetMaxProductionPotential() > 20)
                        {
                            return true;
                        }
                        if (HighPri)
                        {
                            if (building.PlusFlatProductionAmount > 0
                                || building.PlusProdPerRichness > 0
                                || building.PlusProdPerColonist > 0
                                || building.PlusFlatProductionAmount > 0
                                || (Owner.data.Traits  .Cybernetic <=0 && Fertility < 1f && building.PlusFlatFoodAmount > 0)                             
                                || building.StorageAdded > 0
                                || (needDefense && isdefensive && DevelopmentLevel > 3)
                                )
                                return true;
                        }
                        if (MedPri && DevelopmentLevel > 2 && makingMoney)
                        {
                            if (building.PlusResearchPerColonist * Population / 1000 >building.Maintenance
                            || ((building.MaxPopIncrease > 0 || building.PlusFlatPopulation > 0) && Population == MaxPopulation && income > building.Maintenance)
                            || (Owner.data.Traits.Cybernetic <= 0 && building.PlusTerraformPoints > 0 && Fertility < 1 && Population == MaxPopulation && MaxPopulation > 2000 && income>building.Maintenance)
                               || (building.PlusFlatFoodAmount > 0 && NetFoodPerTurn < 0)
                                ||building.PlusFlatResearchAmount >0
                                || (building.PlusResearchPerColonist >0 && MaxPopulation > 999)
                                )
                               
                            {
                                iftrue = true;
                            }

                        }
                        if (!iftrue && LowPri && DevelopmentLevel > 3 && makingMoney && income >building.Maintenance)
                        {
                            if (needDefense && isdefensive && DevelopmentLevel > 2)
                                return true;
                            
                        }
                        break;
                    } 
                    #endregion
                case ColonyType.Military:
                    #region MyRegion
                    {
                        if (Fertility > 0 && building.MinusFertilityOnBuild > 0 && Owner.data.Traits.Cybernetic <= 0)
                            return false;
                        if (HighPri)
                        {
                            if (building.isWeapon
                                || building.IsSensor
                                || building.Defense > 0
                                || (Fertility < 1f && building.PlusFlatFoodAmount > 0)
                                || (MineralRichness < 1f && building.PlusFlatFoodAmount > 0)
                                || building.PlanetaryShieldStrengthAdded > 0
                                || (building.AllowShipBuilding  && GrossProductionPerTurn > 1)
                                || (building.ShipRepair > 0&& GrossProductionPerTurn > 1)
                                || building.Strength > 0
                                || (building.AllowInfantry && GrossProductionPerTurn > 1)
                                || needDefense &&(building.theWeapon !=null || building.Strength >0)
                                || (Owner.data.Traits.Cybernetic > 0 && (building.PlusProdPerRichness > 0 || building.PlusProdPerColonist > 0 || building.PlusFlatProductionAmount > 0))
                                )
                                iftrue = true;
                        }
                        if (!iftrue && MedPri)
                        {
                            if (building.PlusFlatProductionAmount > 0
                                || building.PlusProdPerRichness > 0
                                || building.PlusProdPerColonist > 0
                                || building.PlusFlatProductionAmount > 0)
                                iftrue = true;
                        }
                        if (!iftrue && LowPri && DevelopmentLevel > 4)
                        {
                            //if(building.Name!= "Biospheres")
                            iftrue = true;

                        }
                        break;
                    } 
                    #endregion
                case ColonyType.Research:
                    #region MyRegion
                    {
                        if (building.AllowShipBuilding && GetMaxProductionPotential() > 20)
                        {
                            return true;
                        }
                        if (Fertility > 0 && building.MinusFertilityOnBuild > 0 && Owner.data.Traits.Cybernetic <= 0)
                            return false;

                        if (HighPri)
                        {
                            if (building.PlusFlatResearchAmount > 0
                                || (Fertility < 1f && building.PlusFlatFoodAmount > 0)
                                || (Fertility < 1f && building.PlusFlatFoodAmount > 0)
                                || building.PlusFlatProductionAmount >0
                                || building.PlusResearchPerColonist > 0
                                || (Owner.data.Traits.Cybernetic > 0 && (building.PlusFlatProductionAmount > 0 || building.PlusProdPerColonist > 0 ))
                                || (needDefense && isdefensive && DevelopmentLevel > 3)
                                )
                                return true;

                        }
                        if ( MedPri && DevelopmentLevel > 3 && makingMoney)
                        {
                            if (((building.MaxPopIncrease > 0 || building.PlusFlatPopulation > 0) && Population > MaxPopulation * .5f)
                            || Owner.data.Traits.Cybernetic <=0 &&( (building.PlusTerraformPoints > 0 && Fertility < 1 && Population > MaxPopulation * .5f && MaxPopulation > 2000)
                                || (building.PlusFlatFoodAmount > 0 && NetFoodPerTurn < 0))
                                )
                                return true;
                        }
                        if ( LowPri && DevelopmentLevel > 4 && makingMoney)
                        {
                            if (needDefense && isdefensive && DevelopmentLevel > 2)
                                
                            return true;
                        }
                        break;
                    } 
                    #endregion
            }
            return iftrue;

        }

        private void DetermineFoodState(float importThreshold, float exportThreshold)
        {
            if (Owner.data.Traits.Cybernetic != 0) return;

            if (Owner.NumPlanets == 1)
            {
                FS = GoodState.STORE;       //Easy out for solo planets
                return;
            }

            if (FlatFoodAdded > Population / 1000)     //Account for possible overproduction from FlatFood
            {
                float offestAmount = (FlatFoodAdded - (Population / 1000)) * 0.05f; //5% of excess FlatFood
                offestAmount = offestAmount.Clamp(0.00f, 0.15f);       //Tame offset to prevent huge change
                importThreshold -= offestAmount;
                importThreshold = importThreshold.Clamp(0.10f, 1.00f);
                exportThreshold -= offestAmount;        //Note that overproduction is the only way for a planet with an ExportThreshold of 1 to ever decide to export
                exportThreshold = exportThreshold.Clamp(0.10f, 1.00f);
            }

            float ratio = FoodHere / MaxStorage;

            //This will allow a buffer for import / export, so they dont constantly switch between them
            if      (ratio < importThreshold) FS = GoodState.IMPORT;                                //if below importThreshold, its time to import.
            else if (FS == GoodState.IMPORT && ratio >= importThreshold * 2) FS = GoodState.STORE;  //until you reach 2x importThreshold, then switch to Store
            else if (FS == GoodState.EXPORT && ratio <= exportThreshold / 2) FS = GoodState.STORE;  //If we were exporing, and drop below half exportThreshold, stop exporting
            else if (ratio > exportThreshold) FS = GoodState.EXPORT;                                //until we get back to the Threshold, then export
        }

        private void DetermineProdState(float importThreshold, float exportThreshold)
        {
            if (Owner.NumPlanets == 1)
            {
                PS = GoodState.STORE;       //Easy out for solo planets
                return;
            }

            if (PlusFlatProductionPerTurn > 0)
            {
                if (Owner.data.Traits.Cybernetic != 0)  //Account for excess food for the filthy Opteris
                {
                    if (PlusFlatProductionPerTurn > Population / 1000)
                    {
                        float offestAmount = (PlusFlatProductionPerTurn - (Population / 1000)) * 0.05f;
                        offestAmount = offestAmount.Clamp(0.00f, 0.15f);
                        importThreshold -= offestAmount;
                        importThreshold = importThreshold.Clamp(0.10f, 1.00f);
                        exportThreshold -= offestAmount;
                        exportThreshold = exportThreshold.Clamp(0.10f, 1.00f);
                    }
                }
                else
                {
                    float offestAmount = PlusFlatProductionPerTurn * 0.05f;
                    offestAmount = offestAmount.Clamp(0.00f, 0.15f);
                    importThreshold -= offestAmount;
                    importThreshold = importThreshold.Clamp(0.10f, 1.00f);        //Account for FlatProd, which will pile up
                    exportThreshold -= offestAmount;            //This will allow a planet that normally wouldn't export prod to do so if it is full,
                    exportThreshold = exportThreshold.Clamp(0.10f, 1.00f);        //since the Production still being produced would otherwise be wasted
                }
            }

            float ratio = ProductionHere / MaxStorage;

            if (ratio < importThreshold) PS = GoodState.IMPORT;
            else if (PS == GoodState.IMPORT && ratio >= importThreshold * 2) PS = GoodState.STORE;
            else if (PS == GoodState.EXPORT && ratio <= exportThreshold / 2) PS = GoodState.STORE;
            else if (ratio > exportThreshold) PS = GoodState.EXPORT;
        }

        private void BuildShipyardifAble()
        {
            if (RecentCombat)
            if (Owner != Empire.Universe.PlayerEmpire
                && !Shipyards.Any(ship => ship.Value.shipData.IsShipyard)
                && Owner.ShipsWeCanBuild.Contains(Owner.data.DefaultShipyard))
            {
                bool hasShipyard = false;
                foreach (QueueItem queueItem in ConstructionQueue)
                {
                    if (queueItem.isShip && queueItem.sData.IsShipyard)
                    {
                        hasShipyard = true;
                        break;
                    }
                }
                if (!hasShipyard && DevelopmentLevel > 2)
                    ConstructionQueue.Add(new QueueItem()
                    {
                        isShip = true,
                        sData = ResourceManager.ShipsDict[Owner.data.DefaultShipyard].shipData,
                        Cost = ResourceManager.ShipsDict[Owner.data.DefaultShipyard].GetCost(Owner) *
                                UniverseScreen.GamePaceStatic
                    });
            }
        }

        private void BuildOutpostifAble() //A Gretman function to support DoGoverning()
        {
            bool foundOutpost = false;

            //First check the existing buildings
            foreach (Building building in BuildingList)
            {
                if (building.Name == "Outpost" || building.Name == "Capital City")
                {
                    foundOutpost = true;
                    break;
                }
            }
            if (foundOutpost) return;

            //Then check the queue
            foreach (QueueItem queueItem in ConstructionQueue)
            {
                if (queueItem.isBuilding && queueItem.Building.Name == "Outpost")
                {
                    foundOutpost = true;
                    break;
                }
            }
            if (foundOutpost) return;

            //Still no? Build it!
            AddBuildingToCQ(ResourceManager.CreateBuilding("Outpost"), false);

            //Move Outpost to the top of the list, and rush production
            for (int index = 0; index < ConstructionQueue.Count; ++index)
            {
                QueueItem queueItem1 = ConstructionQueue[index];
                if (index == 0 && queueItem1.isBuilding)
                {
                    if (queueItem1.Building.Name == "Outpost")
                    {
                        SbProduction.ApplyAllStoredProduction(0);
                    }
                    break;
                }
                else if (queueItem1.isBuilding && queueItem1.Building.Name == "Outpost")
                {
                    ConstructionQueue.Remove(queueItem1);
                    ConstructionQueue.Insert(0, queueItem1);
                    break;
                }
            }
        }

        private float CalculateFoodWorkers()    //Simply calculates what percentage of workers are needed for farming (between 0.0 and 0.9)
        {
            if (Owner.data.Traits.Cybernetic != 0 || Fertility + PlusFoodPerColonist <= 0.5) return 0.0f;

            float workers = (Consumption - FlatFoodAdded) / (Population / 1000) / (Fertility + PlusFoodPerColonist);
            if (workers > 0.9f) workers = 0.9f;     //Dont allow farmers to consume all labor
            return workers;
        }

        //This will calculate a smooth transition to maintain [percent]% of stored food. It will under-farm if over
        //[percent]% of storage, or over-farm if under it. Returns labor needed
        private float FarmToPercentage(float percent)   //Production and Research
        {
            if (MaxStorage == 0 || percent == 0) return 0;
            if (Fertility + PlusFoodPerColonist <= 0.5f || Owner.data.Traits.Cybernetic > 0) return 0; //No farming here, so never mind
            float minFarmers = CalculateFoodWorkers();          //Nominal Farmers needed to neither gain nor lose storage
            float storedFoodRatio = FoodHere / MaxStorage;      //Percentage of Food Storage currently filled

            if (FlatFoodAdded > 0)
            {
                //Stop producing food a little early, since the flat food will continue to pile up
                float maxPop = (MaxPopulation + MaxPopBonus) / 1000;
                if (FlatFoodAdded > maxPop) storedFoodRatio += 0.15f * Math.Min(FlatFoodAdded - maxPop, 3);
                storedFoodRatio = storedFoodRatio.Clamp(0, 1);
            }

            float modFarmers = (percent - storedFoodRatio) * 2;             //Percentage currently over or under desired storage
            if (modFarmers >  0 && modFarmers <   0.05) modFarmers = 0.05f;	//Avoid crazy small percentage
            if (modFarmers <  0 && modFarmers >  -0.05) modFarmers = 0.00f;	//Avoid bounce (stop if slightly over)
            modFarmers = modFarmers.Clamp(-0.35f, 0.50f);                                //Also avoid large percentages

            minFarmers += modFarmers;             //modify nominal farmers by overage or underage
            minFarmers = minFarmers.Clamp(0, 0.9f);                  //Tame resulting value, dont let farming completely consume all labor
            return minFarmers;                          //Return labor % of farmers to progress toward goal
        }

        private float WorkToPercentage(float percent)   //Production and Research
        {
            if (MaxStorage == 0 || percent == 0) return 0;
            float minWorkers = 0;
            if (Owner.data.Traits.Cybernetic > 0)
            {											//Nominal workers needed to feed all of the the filthy Opteris
                minWorkers = (Consumption - PlusFlatProductionPerTurn) / (Population / 1000) / (MineralRichness + PlusProductionPerColonist);
                minWorkers = minWorkers.Clamp(0, 1);
            }

            float storedProdRatio = ProductionHere / MaxStorage;      //Percentage of Prod Storage currently filled

            if (PlusFlatProductionPerTurn > 0)      //Stop production early, since the flat production will continue to pile up
            {
                if (Owner.data.Traits.Cybernetic > 0)
                {
                    float maxPop = (MaxPopulation + MaxPopBonus) / 1000;
                    if (PlusFlatProductionPerTurn > maxPop) storedProdRatio += 0.15f * Math.Min(PlusFlatProductionPerTurn - maxPop, 3);
                }
                else
                {
                    storedProdRatio += 0.15f * Math.Min(PlusFlatProductionPerTurn, 3);
                }
                storedProdRatio = storedProdRatio.Clamp(0, 1);
            }

            float modWorkers = (percent - storedProdRatio) * 2;             //Percentage currently over or under desired storage used as mod
            if (modWorkers >  0 && modWorkers <   0.05) modWorkers = 0.05f; //Avoid crazy small percentage
            if (modWorkers <  0 && modWorkers >  -0.05) modWorkers = 0.00f; //Avoid bounce (stop if slightly over)
            minWorkers = minWorkers.Clamp(-0.35f,1);

            minWorkers += modWorkers;             //modify nominal workers by overage or underage
            minWorkers = minWorkers.Clamp(0, 1);
            return minWorkers;                          //Return labor % to progress toward goal
        }

        private void FillOrResearch(float labor)    //Core and TradeHub
        {
            FarmOrResearch(labor / 2);
            WorkOrResearch(labor / 2);
        }

        private void FarmOrResearch (float labor)   //Agreculture
        {
            if (MaxStorage == 0 || labor == 0) return;
            if (Owner.data.Traits.Cybernetic > 0)
			{
                WorkOrResearch(labor);  //Hand off to Prod instead;
				return;
			}
            float maxPop = (MaxPopulation + MaxPopBonus) / 1000;
            float storedFoodRatio = FoodHere / MaxStorage;      //How much of Storage is filled
            if (Fertility + PlusFoodPerColonist <= 0.5f) storedFoodRatio = 1; //No farming here, so skip it

                   //Stop producing food a little early, since the flat food will continue to pile up
            if (FlatFoodAdded > maxPop) storedFoodRatio += 0.15f * Math.Min(FlatFoodAdded - maxPop, 3);
            if (storedFoodRatio > 1) storedFoodRatio = 1;
			
            float farmers = 1 - storedFoodRatio;    //How much storage is left to fill
            if (farmers >= 0.5f) farmers = 1;		//Work out percentage of [labor] to allocate
			else farmers = farmers * 2;
			if (farmers > 0 && farmers < 0.1f) farmers = 0.1f;    //Avoid crazy small percentage of labor
			
            FarmerPercentage += farmers * labor;	//Assign Farmers
			ResearcherPercentage += labor - (farmers * labor);//Leftovers go to Research
        }
		
		private void WorkOrResearch(float labor)    //Industrial
        {
            if (MaxStorage == 0 || labor == 0) return;
			float storedProdRatio = ProductionHere / MaxStorage;      //How much of Storage is filled

            if (Owner.data.Traits.Cybernetic > 0)       //Stop production early, since the flat production will continue to pile up
            {
				float maxPop = (MaxPopulation + MaxPopBonus) / 1000;
                if (PlusFlatProductionPerTurn > maxPop) storedProdRatio += 0.15f * Math.Min(PlusFlatProductionPerTurn - maxPop, 3);
            }
            else
            {
                if (PlusFlatProductionPerTurn > 0) storedProdRatio += 0.15f * Math.Min(PlusFlatProductionPerTurn, 3);
            }
			if (storedProdRatio > 1) storedProdRatio = 1;

            float workers = 1 - storedProdRatio;    //How much storage is left to fill
            if (workers >= 0.5f) workers = 1;		//Work out percentage of [labor] to allocate
			else workers = workers * 2;
			if (workers > 0 && workers < 0.1f) workers = 0.1f;    //Avoid crazy small percentage of labor

            if (ConstructionQueue.Count > 1 && workers < 0.75f) workers = 0.75f;  //Minimum value if construction is going on

            WorkerPercentage += workers * labor;	//Assign workers
			ResearcherPercentage += labor - (workers * labor);//Leftovers go to Research
        }

        private float EvaluateBuilding(Building building, float income)     //Gretman function, to support DoGoverning()
        {
            float finalScore = 0.0f;    //End result score for entire building
            float score = 0.0f;         //Reused variable for each step

            float maxPopulation = (MaxPopulation + MaxPopBonus) / 1000f;
            bool doingResearch = !string.IsNullOrEmpty(Owner.ResearchTopic);

            if (Name == "MerVilleI")
                  { double spotForABreakPoint = Math.PI; }

            //First things first! How much is it gonna' cost?
            if (building.Maintenance != 0)
            {
                score += building.Maintenance * 2;  //Base of 2x maintenance -- Also, I realize I am not calculating MaintMod here. It throws the algorithm off too much
                if (income < building.Maintenance + building.Maintenance * Owner.data.Traits.MaintMod)
                    score += score + (building.Maintenance + building.Maintenance * Owner.data.Traits.MaintMod);   //Really dont want this if we cant afford it
                score -= Owner.data.FlatMoneyBonus * 0.015f;      //Acceptible loss (Note what this will do at high Difficulty)

                finalScore -= score;
                if (Name == "MerVille") Log.Info($"Evaluated {building.Name} Maintenance : Score was {-score}");
            }

            //Flat Food
            if (building.PlusFlatFoodAmount != 0 && Owner.data.Traits.Cybernetic == 0)
            {
                score = 0;
                if (building.PlusFlatFoodAmount < 0) score = building.PlusFlatFoodAmount * 2;   //For negative Flat Food (those crazy modders...)
                else
                {
                    float farmers = CalculateFoodWorkers();
                    score += building.PlusFlatFoodAmount / maxPopulation;   //Percentage of population this will feed
                    score += 1.5f - (Fertility + PlusFoodPerColonist);   //Bonus for low Fertility planets
                    if (score < building.PlusFlatFoodAmount * 0.1f) score = building.PlusFlatFoodAmount * 0.1f; //A little flat food is always useful
                    if (building.PlusFlatFoodAmount + FlatFoodAdded - 0.5f > maxPopulation) score = 0;   //Dont want this if a lot would go to waste
                }
                finalScore += score;
                if (Name == "MerVille") Log.Info($"Evaluated {building.Name} FlatFood : Score was {score}");
            }

            //Food per Colonist
            if (building.PlusFoodPerColonist != 0 && Owner.data.Traits.Cybernetic == 0)
            {
                score = 0;
                if (building.PlusFoodPerColonist < 0) score = building.PlusFoodPerColonist * maxPopulation * 2; //for negative value
                else
                {
                    score += building.PlusFoodPerColonist * maxPopulation - FlatFoodAdded;  //How much food could this create (with penalty for FlatFood)
                    score += Fertility - 0.5f;  //Bonus for high fertility planets
                    if (score < building.PlusFoodPerColonist * 0.1f) score = building.PlusFoodPerColonist * 0.1f; //A little food production is always useful
                    if (Fertility + building.PlusFoodPerColonist + PlusFoodPerColonist <= 1.0f) score = 0;     //Dont try to add farming to a planet without enough to sustain itself
                }
                finalScore += score;
                if (Name == "MerVille") Log.Info($"Evaluated {building.Name} FoodPerCol : Score was {score}");
            }

            //Flat Prod
            if (building.PlusFlatProductionAmount != 0)
            {
                score = 0;
                if (building.PlusFlatProductionAmount < 0) score = building.PlusFlatProductionAmount * 2; //for negative value
                else
                {
                    if (Owner.data.Traits.Cybernetic > 0)
                        score += building.PlusFlatProductionAmount / maxPopulation;   //Percentage of the filthy Opteris population this will feed
                    float farmers = CalculateFoodWorkers();
                    score += farmers;   //Bonus the fewer workers there are available
                    score += 1.5f - (MineralRichness + PlusProductionPerColonist);     //Bonus for low richness planets
                    score += (building.PlusFlatProductionAmount - PlusFlatProductionPerTurn) - ((MineralRichness + PlusProductionPerColonist) * ((1 - farmers) * maxPopulation));   //How much flat Prod compared to labor prod
                    if (score < building.PlusFlatProductionAmount * 0.1f) score = building.PlusFlatProductionAmount * 0.1f; //A little production is always useful
                }
                finalScore += score;
                if (Name == "MerVille") Log.Info($"Evaluated {building.Name} FlatProd : Score was {score}");
            }

            //Prod per Colonist
            if (building.PlusProdPerColonist != 0)
            {
                score = 0;
                if (building.PlusProdPerColonist < 0) score = building.PlusProdPerColonist * maxPopulation * 2;
                else
                {
                    float farmers = CalculateFoodWorkers();
                    score += 1 - farmers;   //Bonus the more workers there are available
                    score += building.PlusProdPerColonist * maxPopulation * farmers;    //Prod this building will add
                    if (score < building.PlusProdPerColonist * 0.1f) score = building.PlusProdPerColonist * 0.1f; //A little production is always useful
                }
                finalScore += score;
                if (Name == "MerVille") Log.Info($"Evaluated {building.Name} ProdPerCol : Score was {score}");
            }

            //Prod per Richness
            if (building.PlusProdPerRichness != 0)  //This one can produce a pretty high building value, which is normally offset by its huge maintenance cost and Fertility loss
            {
                score = 0;
                if (building.PlusProdPerRichness < 0) score = building.PlusProdPerRichness * MineralRichness * 2;
                else
                {
                    score += building.PlusProdPerRichness * MineralRichness;        //Production this would generate
                    if (!HasShipyard) score *= 0.75f;       //Do we have a use for all this production?
                }
                finalScore += score;
                if (Name == "MerVille") Log.Info($"Evaluated {building.Name} ProdPerRich : Score was {score}");
            }

            //Storage
            if (building.StorageAdded != 0)
            {
                score = 0;

                float desiredStorage = 50.0f;
                if (Fertility + PlusFoodPerColonist > 2.5f || MineralRichness + PlusProductionPerColonist > 2.5f || PlusFlatProductionPerTurn > 5) desiredStorage += 100.0f;  //Potential high output
                if (HasShipyard) desiredStorage += 100.0f;      //For buildin' ships 'n shit
                if (MaxStorage < desiredStorage) score += (building.StorageAdded * 0.002f) - (building.Cost * 0.001f);  //If we need more storage, rate this building
                if (building.Maintenance > 0) score *= 0.25f;       //Prefer free storage

                finalScore += score;
                if (Name == "MerVille") Log.Info($"Evaluated {building.Name} StorageAdd : Score was {score}");
            }

            //Plus Population Growth
            if (building.PlusFlatPopulation != 0)
            {
                score = 0;
                if (building.PlusFlatPopulation < 0) score = building.PlusFlatPopulation * 0.02f;  //Which is sorta like     0.01f * 2
                else
                {
                    score += (maxPopulation * 0.02f - 1.0f) + (building.PlusFlatPopulation * 0.01f);        //More desireable on high pop planets
                    if (score < building.PlusFlatPopulation * 0.01f) score = building.PlusFlatPopulation * 0.01f;     //A little extra is still good
                }
                if (Owner.data.Traits.PhysicalTraitLessFertile) score *= 2;     //These are calculated outside the else, so they will affect negative flatpop too
                if (Owner.data.Traits.PhysicalTraitFertile) score *= 0.5f;
                finalScore += score;
                if (Name == "MerVille") Log.Info($"Evaluated {building.Name} PopGrowth : Score was {score}");
            }

            //Plus Max Population
            if (building.MaxPopIncrease != 0)
            {
                score = 0;
                if (building.MaxPopIncrease < 0) score = building.MaxPopIncrease * 0.002f;      //Which is sorta like     0.001f * 2
                else
                {
                    //Basically, only add to the score if we would be able to feed the extra people
                    if ((Fertility + PlusFoodPerColonist + building.PlusFoodPerColonist) * (maxPopulation + (building.MaxPopIncrease / 1000))
                        >= (maxPopulation + (building.MaxPopIncrease / 1000) - FlatFoodAdded - building.PlusFlatFoodAmount)   )
                        score += building.MaxPopIncrease * 0.001f;
                }
                finalScore += score;
                if (Name == "MerVille") Log.Info($"Evaluated {building.Name} MaxPop : Score was {score}");
            }

            //Flat Research
            if (building.PlusFlatResearchAmount != 0 && doingResearch)
            {
                score = 0.001f;
                if (building.PlusFlatResearchAmount < 0)            //Surly no one would make a negative research building
                {
                    if (ResearcherPercentage > 0 || PlusFlatResearchPerTurn > 0) score += building.PlusFlatResearchAmount * 2;
                    else score += building.PlusFlatResearchAmount;
                }
                else
                {                   //Can we easily afford this
                    if ((building.Maintenance + building.Maintenance * Owner.data.Traits.MaintMod) * 1.5 <= income) score += building.PlusFlatResearchAmount * 2;
                    if (score < building.PlusFlatResearchAmount * 0.1f) score = building.PlusFlatResearchAmount * 0.1f; //A little extra research is always useful
                }
                finalScore += score;
                if (Name == "MerVille") Log.Info($"Evaluated {building.Name} FlatResearch : Score was {score}");
            }

            //Research per Colonist
            if (building.PlusResearchPerColonist != 0 && doingResearch)
            {
                score = 0;
                if (building.PlusResearchPerColonist < 0)
                {
                    if (ResearcherPercentage > 0 || PlusFlatResearchPerTurn > 0) score += building.PlusResearchPerColonist * (ResearcherPercentage * maxPopulation) * 2;
                    else score += building.PlusResearchPerColonist * (ResearcherPercentage * maxPopulation);
                }
                else
                {
                    score += building.PlusResearchPerColonist * (ResearcherPercentage * maxPopulation);       //Research this will generate
                }
                finalScore += score;
                if (Name == "MerVille") Log.Info($"Evaluated {building.Name} ResPerCol : Score was {score}");
            }

            //Credits per Colonist
            if (building.CreditsPerColonist != 0)
            {
                score = 0;
                if (building.CreditsPerColonist < 0) score += building.CreditsPerColonist * maxPopulation * 2;
                else score += (building.CreditsPerColonist * maxPopulation) / 2;        //Dont want to cause this to have building preference over infrastructure buildings
                finalScore += score;
                if (Name == "MerVille") Log.Info($"Evaluated {building.Name} CredsPerCol : Score was {score}");
            }

            //Plus Tax Percentage
            if (building.PlusTaxPercentage != 0)
            {
                score = 0;

                if (building.PlusTaxPercentage < 0) score += building.PlusTaxPercentage * GrossMoneyPT * 2;
                else score += building.PlusTaxPercentage * GrossMoneyPT / 2;
                finalScore += score;
                if (Name == "MerVille") Log.Info($"Evaluated {building.Name} PlusTaxPercent : Score was {score}");
            }

            //Allow Ship Building
            if (building.AllowShipBuilding || building.Name == "Space Port")        //Had to also add the name, because some parts of the code look at the name instead of the 'AllowShipBuilding' tag
            {
                score = 0;              //This one probably wont produce overwhelming building value, so will rely on other building tags to overcome the maintenance cost
                float farmers = CalculateFoodWorkers();
                float prodFromLabor = ((1 - farmers) * maxPopulation * (MineralRichness + PlusProductionPerColonist + building.PlusProdPerColonist));
                float prodFromFlat  = PlusFlatProductionPerTurn + building.PlusFlatProductionAmount + (building.PlusProdPerRichness * MineralRichness);
                //Do we have enough production capability to really justify trying to build ships
                if (prodFromLabor + prodFromFlat > 5.0f) score += prodFromLabor + prodFromFlat - 5.0f;
                finalScore += score;
                if (Name == "MerVille") Log.Info($"Evaluated {building.Name} AllowShipBuilding : Score was {score}");
            }

            if (false && building.PlusTerraformPoints != 0)
            {
                score = 0;
                //Still working on this one...
                if (Name == "MerVille") Log.Info($"Evaluated {building.Name} Terraform : Score was {score}");
            }

            //Fertility loss on build
            if (building.MinusFertilityOnBuild != 0 && Owner.data.Traits.Cybernetic == 0)       //Cybernetic dont care.
            {
                score = 0;
                if (building.MinusFertilityOnBuild < 0) score += building.MinusFertilityOnBuild * 2;    //Negative loss means positive gain!!
                else
                {                                   //How much fertility will actually be lost
                    float fertLost = Math.Min(Fertility, building.MinusFertilityOnBuild);
                    float foodFromLabor = maxPopulation * ((Fertility - fertLost) + PlusFoodPerColonist + building.PlusFoodPerColonist);
                    float foodFromFlat = FlatFoodAdded + building.PlusFlatFoodAmount;
                                //Will we still be able to feed ourselves?
                    if (foodFromFlat + foodFromLabor < Consumption) score += fertLost * 10;
                    else score += fertLost * 4;
                }
                finalScore -= score;
                if (Name == "MerVille") Log.Info($"Evaluated {building.Name} FertLossOnBuild : Score was {score}");
            }

            if (Name == "MerVille") Log.Info(ConsoleColor.Cyan, $"Evaluated {building.Name} Final Score was : {finalScore}");
            return finalScore;
        }

        public void DoGoverning()
        {
            BuildOutpostifAble();   //If there is no Outpost or Capital, build it

            if (colonyType == Planet.ColonyType.Colony) return; //No Governor? Nevermind!

            RefreshBuildingsWeCanBuildHere();
            float income = GrossMoneyPT - TotalMaintenanceCostsPerTurn;

            //Do some existing bulding recon
            int openTiles = 0;
            int totalbuildings = 0; //To account for buildings that can be build anywhere
            foreach (PlanetGridSquare pgs in TilesList)
            {
                if (pgs.Habitable)
                {
                    if (pgs.building == null) openTiles++;  //Habitable spot, without a building
                }

                if (pgs.building != null && pgs.building.Name != "Biospheres") totalbuildings++;
            }

            //Construction queue recon
            bool biosphereInTheWorks = false;
            bool buildingInTheWorks = false;
            foreach (var thingie in ConstructionQueue)
            {
                if (!thingie.isBuilding) continue;          //Include buildings in queue in income calculations
                income -= thingie.Building.Maintenance + thingie.Building.Maintenance * Owner.data.Traits.MaintMod;
                buildingInTheWorks = true;
                if (thingie.Building.Name == "Biospheres") biosphereInTheWorks = true;
            }

            //Stuff we can build recon
            Building bioSphere = null;
            foreach (var building in BuildingsCanBuild)
            {
                if (building.Name == "Biospheres")
                {
                    bioSphere = building;
                    break;
                }
            }

            bool notResearching = string.IsNullOrEmpty(Owner.ResearchTopic);
            bool lotsInQueueToBuild = ConstructionQueue.Count >= 4;
            bool littleInQueueToBuild = ConstructionQueue.Count >= 1;
            float foodMinimum = CalculateFoodWorkers();

            //Switch to Industrial if there is nothing in the research queue (Does not actually change assigned Governor)
            if (colonyType == ColonyType.Research && notResearching)
                colonyType = ColonyType.Industrial;

            if (Name == "MerVilleII")
            { double spotForABreakpoint = Math.PI; }

            switch (colonyType)
            {
                case Planet.ColonyType.TradeHub:
                case Planet.ColonyType.Core:
                    {
                        //New resource management by Gretman
                        FarmerPercentage = CalculateFoodWorkers();
                        WorkerPercentage = 0.0f;
                        ResearcherPercentage = 0.0f;

                        FillOrResearch(1 - FarmerPercentage);

                        if (colonyType == Planet.ColonyType.TradeHub)
                        {
                            DetermineFoodState(0.15f, 1.0f);   //Minimal Intervention for the Tradehub, so the player can control it except in extreme cases
                            DetermineProdState(0.15f, 1.0f);
                            break;
                        }






                        //New Build Logic by Gretman
                        if (!lotsInQueueToBuild) BuildShipyardifAble(); //If we can build a shipyard but dont have one, build it

                        if (openTiles > 0)
                        {
                            if (!buildingInTheWorks)
                            {
                                Building bestBuilding = null;
                                float bestValue = 0.0f;     //So a building with a value of 0 will not be built.
                                float buildingScore = 0.0f;
                                for (int i = 0; i < BuildingsCanBuild.Count; i++)
                                {
                                    //Find the building with the highest score
                                    buildingScore = EvaluateBuilding(BuildingsCanBuild[i], income);
                                    if (buildingScore > bestValue)
                                    {
                                        bestBuilding = BuildingsCanBuild[i];
                                        bestValue = buildingScore;
                                    }
                                }
                                if (bestBuilding != null) AddBuildingToCQ(bestBuilding);
                            }
                        }
                        else
                        {
                            if (bioSphere != null && !biosphereInTheWorks && totalbuildings < 35 && bioSphere.Maintenance < income + 0.3f) //No habitable tiles, and not too much in debt
                                AddBuildingToCQ(bioSphere);
                        }

                        DetermineFoodState(0.25f, 0.666f);   //these will evaluate to: Start Importing if stores drop below 25%, and stop importing once stores are above 50%.
                        DetermineProdState(0.25f, 0.666f);   //                        Start Exporting if stores are above 66%, but dont stop exporting unless stores drop below 33%.

                        break;
                    }

                case Planet.ColonyType.Industrial:
                    {
                        //Farm to 33% storage, then devote the rest to Work, then to research when that starts to fill up
                        FarmerPercentage = FarmToPercentage(0.333f);
                        WorkerPercentage = Math.Min(1 - FarmerPercentage, WorkToPercentage(1));
                        if (ConstructionQueue.Count > 0) WorkerPercentage = Math.Max(WorkerPercentage, (1 - FarmerPercentage) * 0.5f);
                        ResearcherPercentage = Math.Max(1 - FarmerPercentage - WorkerPercentage, 0);

                        





                        if (!lotsInQueueToBuild) BuildShipyardifAble(); //If we can build a shipyard but dont have one, build it

                        if (openTiles > 0)
                        {
                            if (!buildingInTheWorks)
                            {
                                Building bestBuilding = null;
                                float bestValue = 0.0f;     //So a building with a value of 0 will not be built.
                                float buildingScore = 0.0f;
                                for (int i = 0; i < BuildingsCanBuild.Count; i++)
                                {
                                    //Find the building with the highest score
                                    buildingScore = EvaluateBuilding(BuildingsCanBuild[i], income);
                                    if (buildingScore > bestValue)
                                    {
                                        bestBuilding = BuildingsCanBuild[i];
                                        bestValue = buildingScore;
                                    }
                                }
                                if (bestBuilding != null) AddBuildingToCQ(bestBuilding);
                            }
                        }
                        else
                        {
                            if (bioSphere != null && !biosphereInTheWorks && totalbuildings < 35 && bioSphere.Maintenance < income + 0.3f) //No habitable tiles, and not too much in debt
                                AddBuildingToCQ(bioSphere);
                        }

                        DetermineFoodState(0.50f, 1.0f);     //Start Importing if food drops below 50%, and stop importing once stores reach 100%. Will only export food due to excess FlatFood.
                        DetermineProdState(0.15f, 0.666f);   //Start Importing if prod drops below 15%, stop importing at 30%. Start exporting at 66%, and dont stop unless below 33%.

                        break;
                    }

                case Planet.ColonyType.Research:
                    {
                        //This governor will rely on imports, focusing on research as long as no one is starving
                        FarmerPercentage = FarmToPercentage(0.333f);    //Farm to a small savings, and prevent starvation
                        WorkerPercentage = Math.Min(1 - FarmerPercentage, WorkToPercentage(0.333f));        //Save a litle production too
                        if (ConstructionQueue.Count > 0) WorkerPercentage = Math.Max(WorkerPercentage, (1 - FarmerPercentage) * 0.5f);
                        ResearcherPercentage = Math.Max(1 - FarmerPercentage - WorkerPercentage, 0);    //Otherwise, research!






                        if (!lotsInQueueToBuild) BuildShipyardifAble(); //If we can build a shipyard but dont have one, build it

                        if (openTiles > 0)
                        {
                            if (!buildingInTheWorks)
                            {
                                Building bestBuilding = null;
                                float bestValue = 0.0f;     //So a building with a value of 0 will not be built.
                                float buildingScore = 0.0f;
                                for (int i = 0; i < BuildingsCanBuild.Count; i++)
                                {
                                    //Find the building with the highest score
                                    buildingScore = EvaluateBuilding(BuildingsCanBuild[i], income);
                                    if (buildingScore > bestValue)
                                    {
                                        bestBuilding = BuildingsCanBuild[i];
                                        bestValue = buildingScore;
                                    }
                                }
                                if (bestBuilding != null) AddBuildingToCQ(bestBuilding);
                            }
                        }
                        else
                        {
                            if (bioSphere != null && !biosphereInTheWorks && totalbuildings < 35 && bioSphere.Maintenance < income + 0.3f) //No habitable tiles, and not too much in debt
                                AddBuildingToCQ(bioSphere);
                        }

                        DetermineFoodState(0.50f, 1.0f);     //Import if either drops below 50%, and stop importing once stores reach 100%.
                        DetermineProdState(0.50f, 1.0f);     //This planet will only export Food or Prod if there is excess FlatFood or FlatProd

                        break;
                    }

                case Planet.ColonyType.Agricultural:
                    {
                        FarmerPercentage = FarmToPercentage(1);     //Farm all you can
                        WorkerPercentage = Math.Min(1 - FarmerPercentage, WorkToPercentage(0.333f));    //Then work to a small savings
                        if (ConstructionQueue.Count > 0) WorkerPercentage = Math.Max(WorkerPercentage, (1 - FarmerPercentage) * 0.5f);
                        ResearcherPercentage = Math.Max(1 - FarmerPercentage - WorkerPercentage, 0);    //Otherwise, research!

                        






                        if (!lotsInQueueToBuild) BuildShipyardifAble(); //If we can build a shipyard but dont have one, build it

                        if (openTiles > 0)
                        {
                            if (!buildingInTheWorks)
                            {
                                Building bestBuilding = null;
                                float bestValue = 0.0f;     //So a building with a value of 0 will not be built.
                                float buildingScore = 0.0f;
                                for (int i = 0; i < BuildingsCanBuild.Count; i++)
                                {
                                    //Find the building with the highest score
                                    buildingScore = EvaluateBuilding(BuildingsCanBuild[i], income);
                                    if (buildingScore > bestValue)
                                    {
                                        bestBuilding = BuildingsCanBuild[i];
                                        bestValue = buildingScore;
                                    }
                                }
                                if (bestBuilding != null) AddBuildingToCQ(bestBuilding);
                            }
                        }
                        else
                        {
                            if (bioSphere != null && !biosphereInTheWorks && totalbuildings < 35 && bioSphere.Maintenance < income + 0.3f) //No habitable tiles, and not too much in debt
                                AddBuildingToCQ(bioSphere);
                        }

                        DetermineFoodState(0.15f, 0.666f);   //Start Importing if food drops below 15%, stop importing at 30%. Start exporting at 66%, and dont stop unless below 33%.
                        DetermineProdState(0.50f, 1.000f);   //Start Importing if prod drops below 50%, and stop importing once stores reach 100%. Will only export prod due to excess FlatProd.

                        break;
                    }

                case Planet.ColonyType.Military:    //This on is incomplete
                    {
                        FarmerPercentage = FarmToPercentage(0.5f);     //Keep everyone fed, but dont be desperate for imports
                        WorkerPercentage = Math.Min(1 - FarmerPercentage, WorkToPercentage(0.5f));    //Keep some prod handy
                        if (ConstructionQueue.Count > 0) WorkerPercentage = Math.Max(WorkerPercentage, (1 - FarmerPercentage) * 0.5f);
                        ResearcherPercentage = Math.Max(1 - FarmerPercentage - WorkerPercentage, 0);    //Research if bored

                        





                        if (!lotsInQueueToBuild) BuildShipyardifAble(); //If we can build a shipyard but dont have one, build it

                        if (openTiles > 0)
                        {
                            if (!buildingInTheWorks)
                            {
                                Building bestBuilding = null;
                                float bestValue = 0.0f;     //So a building with a value of 0 will not be built.
                                float buildingScore = 0.0f;
                                for (int i = 0; i < BuildingsCanBuild.Count; i++)
                                {
                                    //Find the building with the highest score
                                    buildingScore = EvaluateBuilding(BuildingsCanBuild[i], income);
                                    if (buildingScore > bestValue)
                                    {
                                        bestBuilding = BuildingsCanBuild[i];
                                        bestValue = buildingScore;
                                    }
                                }
                                if (bestBuilding != null) AddBuildingToCQ(bestBuilding);
                            }
                        }
                        else
                        {
                            if (bioSphere != null && !biosphereInTheWorks && totalbuildings < 35 && bioSphere.Maintenance < income + 0.3f) //No habitable tiles, and not too much in debt
                                AddBuildingToCQ(bioSphere);
                        }

                        DetermineFoodState(0.4f, 1.0f);     //Import if either drops below 40%, and stop importing once stores reach 80%.
                        DetermineProdState(0.4f, 1.0f);     //This planet will only export Food or Prod due to excess FlatFood or FlatProd

                        break;
                    }

                    //Leaving there here for reference -Gretman
                    //Added by McShooterz: Colony build troops
                    if (false && Owner.isPlayer && colonyType == ColonyType.Military)
                    {
                        bool addTroop = false;
                        foreach (PlanetGridSquare planetGridSquare in TilesList)
                        {
                            if (planetGridSquare.TroopsHere.Count < planetGridSquare.number_allowed_troops)
                            {
                                addTroop = true;
                                break;
                            }
                        }
                        if (addTroop && AllowInfantry)
                        {
                            foreach (string troopType in ResourceManager.TroopTypes)
                            {
                                if (!Owner.WeCanBuildTroop(troopType))
                                    continue;
                                QueueItem qi = new QueueItem();
                                qi.isTroop = true;
                                qi.troopType = troopType;
                                qi.Cost = ResourceManager.GetTroopCost(troopType);
                                qi.productionTowards = 0f;
                                qi.NotifyOnEmpty = false;
                                ConstructionQueue.Add(qi);
                                break;
                            }
                        }
                    }

            } //End Gov type Switch

            if (ConstructionQueue.Count < 5 && !ParentSystem.CombatInSystem && DevelopmentLevel > 2 &&
                colonyType != ColonyType.Research) 

                #region Troops and platforms

            {

                //Added by McShooterz: build defense platforms

                if (HasShipyard && !ParentSystem.CombatInSystem
                    && (!Owner.isPlayer || colonyType == ColonyType.Military))
                {
                    SystemCommander systemCommander;
                    if (Owner.GetGSAI().DefensiveCoordinator.DefenseDict.TryGetValue(ParentSystem, out systemCommander))
                    {
                        float defBudget = Owner.data.DefenseBudget * systemCommander.PercentageOfValue;

                        float maxProd = GetMaxProductionPotential();
                        float platformUpkeep = ResourceManager.ShipRoles[ShipData.RoleName.platform].Upkeep;
                        float stationUpkeep = ResourceManager.ShipRoles[ShipData.RoleName.station].Upkeep;
                        string station = Owner.GetGSAI().GetStarBase();
                        int PlatformCount = 0;
                        int stationCount = 0;
                        foreach (QueueItem queueItem in ConstructionQueue)
                        {
                            if (!queueItem.isShip)
                                continue;
                            if (queueItem.sData.HullRole == ShipData.RoleName.platform)
                            {
                                if (defBudget - platformUpkeep < -platformUpkeep * .5
                                ) 
                                {
                                    ConstructionQueue.QueuePendingRemoval(queueItem);
                                    continue;
                                }
                                defBudget -= platformUpkeep;
                                PlatformCount++;
                            }
                            if (queueItem.sData.HullRole == ShipData.RoleName.station)
                            {
                                if (defBudget - stationUpkeep < -stationUpkeep)
                                {
                                    ConstructionQueue.QueuePendingRemoval(queueItem);
                                    continue;
                                }
                                defBudget -= stationUpkeep;
                                stationCount++;
                            }
                        }

                        foreach (Ship platform in Shipyards.Values)
                        {
                            
                            if (platform.AI.State == AIState.Scrap)
                                continue;
                            if (platform.shipData.HullRole == ShipData.RoleName.station )
                            {
                                stationUpkeep = platform.GetMaintCost();
                                if (defBudget - stationUpkeep < -stationUpkeep)
                                {
                                    platform.AI.OrderScrapShip();
                                    continue;
                                }
                                defBudget -= stationUpkeep;
                                stationCount++;
                            }
                            if (platform.shipData.HullRole == ShipData.RoleName.platform
                            ) 
                            {
                                platformUpkeep = platform.GetMaintCost();
                                if (defBudget - platformUpkeep < -platformUpkeep)
                                {
                                    platform.AI.OrderScrapShip();

                                    continue;
                                }
                                defBudget -= platformUpkeep;
                                PlatformCount++;
                            }
                        }

                        if (defBudget > stationUpkeep &&
                            stationCount < (int) (systemCommander.RankImportance * .5f)
                            && stationCount < GlobalStats.ShipCountLimit * GlobalStats.DefensePlatformLimit)
                        {                            
                            if (!string.IsNullOrEmpty(station))
                            {
                                Ship ship = ResourceManager.ShipsDict[station];
                                if (ship.GetCost(Owner) / GrossProductionPerTurn < 10)
                                    ConstructionQueue.Add(new QueueItem()
                                    {
                                        isShip = true,
                                        sData = ship.shipData,
                                        Cost = ship.GetCost(Owner)
                                    });
                            }
                            defBudget -= stationUpkeep;
                        }
                        if (defBudget > platformUpkeep 
                            && PlatformCount <
                            systemCommander.RankImportance 
                            && PlatformCount < GlobalStats.ShipCountLimit * GlobalStats.DefensePlatformLimit)
                        {
                            string platform = Owner.GetGSAI().GetDefenceSatellite();
                            if (!string.IsNullOrEmpty(platform))
                            {
                                Ship ship = ResourceManager.ShipsDict[platform];
                                ConstructionQueue.Add(new QueueItem()
                                {
                                    isShip = true,
                                    sData = ship.shipData,
                                    Cost = ship.GetCost(Owner)
                                });
                            }
                        }
                    }
                }
            }

            #endregion

            #region Scrap

            {
                Array<Building> list1 = new Array<Building>();
                if (Fertility >= 1)
                {
                    foreach (Building building in BuildingList)
                    {
                        if (building.PlusTerraformPoints > 0.0f && building.Maintenance > 0)
                            list1.Add(building);
                    }
                }


                {
                    using (ConstructionQueue.AcquireReadLock())
                        foreach (PlanetGridSquare PGS in TilesList)
                        {
                            bool qitemTest = PGS.QItem != null;
                            if (qitemTest && PGS.QItem.IsPlayerAdded)
                                continue;
                            if (PGS.building != null && PGS.building.IsPlayerAdded)
                                continue;
                            if ((qitemTest && PGS.QItem.Building.Name == "Biospheres") ||
                                (PGS.building != null && PGS.building.Name == "Biospheres"))
                                continue;
                            if ((PGS.building != null && PGS.building.PlusFlatProductionAmount > 0) ||
                                (PGS.building != null && PGS.building.PlusFlatProductionAmount > 0))
                                continue;
                            if ((PGS.building != null && PGS.building.PlusFlatFoodAmount > 0) ||
                                (PGS.building != null && PGS.building.PlusFlatFoodAmount > 0))
                                continue;
                            if ((PGS.building != null && PGS.building.PlusFlatResearchAmount > 0) ||
                                (PGS.building != null && PGS.building.PlusFlatResearchAmount > 0))
                                continue;
                            if (PGS.building != null && !qitemTest && PGS.building.Scrappable &&
                                !WeCanAffordThis(PGS.building, colonyType)
                            ) 
                            {
                                PGS.building.ScrapBuilding(this);
                            }
                            if (qitemTest && !WeCanAffordThis(PGS.QItem.Building, colonyType))
                            {
                                ProductionHere += PGS.QItem.productionTowards;
                                ConstructionQueue.QueuePendingRemoval(PGS.QItem);
                                PGS.QItem = null;
                            }
                        }

                    ConstructionQueue.ApplyPendingRemovals();
                }

                #endregion
            }
        }

        public float GetMaxProductionPotential() { return MaxProductionPerTurn; }

        private float GetMaxProductionPotentialCalc()
        {
            float bonusProd = 0.0f;
            float baseProd = MineralRichness * Population / 1000;
            for (int index = 0; index < BuildingList.Count; ++index)
            {
                Building building = BuildingList[index];
                if (building.PlusProdPerRichness > 0.0)
                    bonusProd += building.PlusProdPerRichness * MineralRichness;
                bonusProd += building.PlusFlatProductionAmount;
                if (building.PlusProdPerColonist > 0.0)
                    baseProd += building.PlusProdPerColonist;
            }
            float finalProd = baseProd + bonusProd * Population / 1000;
            if (Owner.data.Traits.Cybernetic > 0)
                return finalProd + Owner.data.Traits.ProductionMod * finalProd - Consumption;
            return finalProd + Owner.data.Traits.ProductionMod * finalProd;
        }

        public float GetMaxResearchPotential =>
            (Population / 1000) * PlusResearchPerColonist + PlusFlatResearchPerTurn;

        public void ApplyProductionTowardsConstruction() => SbProduction.ApplyProductionTowardsConstruction();

        public void InitializeSliders(Empire o)
        {
            if (o.data.Traits.Cybernetic == 1 || Type == "Barren")
            {
                FarmerPercentage = 0.0f;
                WorkerPercentage = 0.5f;
                ResearcherPercentage = 0.5f;
            }
            else
            {
                FarmerPercentage = 0.55f;
                ResearcherPercentage = 0.2f;
                WorkerPercentage = 0.25f;
            }
        }

        public bool CanBuildInfantry()
        {
            for (int i = 0; i < BuildingList.Count; i++)
            {
                if (BuildingList[i].AllowInfantry)
                    return true;
            }
            return false;
        }

        public void UpdateIncomes(bool LoadUniverse)
        {
            if (Owner == null)
                return;
            PlusFlatPopulationPerTurn = 0f;
            ShieldStrengthMax = 0f;
            TotalMaintenanceCostsPerTurn = 0f;
            float storageAdded = 0;
            AllowInfantry = false;
            TotalDefensiveStrength = 0;
            GrossFood = 0f;
            PlusResearchPerColonist = 0f;
            PlusFlatResearchPerTurn = 0f;
            PlusFlatProductionPerTurn = 0f;
            PlusProductionPerColonist = 0f;
            FlatFoodAdded = 0f;
            PlusFoodPerColonist = 0f;
            PlusFlatPopulationPerTurn = 0f;
            ShipBuildingModifier = 0f;
            CommoditiesPresent.Clear();
            float shipbuildingmodifier = 1f;
            Array<Guid> list = new Array<Guid>();
            float shipyards =1;
            
            if (!LoadUniverse)
            foreach (KeyValuePair<Guid, Ship> keyValuePair in Shipyards)
            {
                if (keyValuePair.Value == null)
                    list.Add(keyValuePair.Key);
                    
                else if (keyValuePair.Value.Active && keyValuePair.Value.shipData.IsShipyard)
                {

                    if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.ShipyardBonus > 0)
                    {
                        shipbuildingmodifier *= (1 - (GlobalStats.ActiveModInfo.ShipyardBonus / shipyards)); //+= GlobalStats.ActiveModInfo.ShipyardBonus;
                    }
                    else
                    {
                        shipbuildingmodifier *= (1-(.25f/shipyards));
                    }
                    shipyards += .2f;
                }
                else if (!keyValuePair.Value.Active)
                    list.Add(keyValuePair.Key);
            }
            ShipBuildingModifier = shipbuildingmodifier;
            foreach (Guid key in list)
            {
                Shipyards.Remove(key);
            }
            PlusCreditsPerColonist = 0f;
            MaxPopBonus = 0f;
            PlusTaxPercentage = 0f;
            TerraformToAdd = 0f;
            bool shipyard = false;
            RepairPerTurn = 0;
            for (int index = 0; index < BuildingList.Count; ++index)
            {
                Building building = BuildingList[index];
                if (building.WinsGame)
                    HasWinBuilding = true;
                //if (building.NameTranslationIndex == 458)
                if (building.AllowShipBuilding || building.Name == "Space Port" )
                    shipyard= true;
                
                PlusFlatPopulationPerTurn += building.PlusFlatPopulation;
                ShieldStrengthMax += building.PlanetaryShieldStrengthAdded;
                PlusCreditsPerColonist += building.CreditsPerColonist;
                TerraformToAdd += building.PlusTerraformPoints;
                TotalDefensiveStrength += building.CombatStrength;
                PlusTaxPercentage += building.PlusTaxPercentage;
                CommoditiesPresent.Add(building.Name);
                if (building.AllowInfantry) AllowInfantry = true;
                storageAdded += building.StorageAdded;
                PlusFoodPerColonist += building.PlusFoodPerColonist;
                PlusResearchPerColonist += building.PlusResearchPerColonist;
                PlusFlatResearchPerTurn += building.PlusFlatResearchAmount;
                PlusFlatProductionPerTurn += building.PlusProdPerRichness * MineralRichness;
                PlusFlatProductionPerTurn += building.PlusFlatProductionAmount;
                PlusProductionPerColonist += building.PlusProdPerColonist;
                MaxPopBonus += building.MaxPopIncrease;
                TotalMaintenanceCostsPerTurn += building.Maintenance;
                FlatFoodAdded += building.PlusFlatFoodAmount;
                RepairPerTurn += building.ShipRepair;
                //Repair if no combat
                if(!RecentCombat)
                {
                    building.CombatStrength = Ship_Game.ResourceManager.BuildingsDict[building.Name].CombatStrength;
                    building.Strength = Ship_Game.ResourceManager.BuildingsDict[building.Name].Strength;
                }
            }
            //Added by Gretman -- This will keep a planet from still having sheilds even after the shield building has been scrapped.
            if (ShieldStrengthCurrent > ShieldStrengthMax) ShieldStrengthCurrent = ShieldStrengthMax;

            if (shipyard && (colonyType != ColonyType.Research || Owner.isPlayer))
                HasShipyard = true;
            else
                HasShipyard = false;
            //Research
            NetResearchPerTurn = (ResearcherPercentage * Population / 1000) * PlusResearchPerColonist + PlusFlatResearchPerTurn;
            NetResearchPerTurn = NetResearchPerTurn + Owner.data.Traits.ResearchMod * NetResearchPerTurn;
            NetResearchPerTurn = NetResearchPerTurn - Owner.data.TaxRate * NetResearchPerTurn;
            //Food
            NetFoodPerTurn =  (FarmerPercentage * Population / 1000 * (Fertility + PlusFoodPerColonist)) + FlatFoodAdded;
            GrossFood = NetFoodPerTurn;     //NetFoodPerTurn is finished being calculated in another file...
            //Production
            NetProductionPerTurn = (WorkerPercentage * Population / 1000f * (MineralRichness + PlusProductionPerColonist)) + PlusFlatProductionPerTurn;
            NetProductionPerTurn = NetProductionPerTurn + Owner.data.Traits.ProductionMod * NetProductionPerTurn;
            MaxProductionPerTurn = GetMaxProductionPotentialCalc();

            Consumption =  (Population / 1000 + Owner.data.Traits.ConsumptionModifier * Population / 1000);

            if (Owner.data.Traits.Cybernetic > 0)
                NetProductionPerTurn = NetProductionPerTurn - Owner.data.TaxRate * (NetProductionPerTurn - Consumption) ;
            else
                NetProductionPerTurn = NetProductionPerTurn - Owner.data.TaxRate * NetProductionPerTurn;

            GrossProductionPerTurn =  (Population / 1000  * (MineralRichness + PlusProductionPerColonist)) + PlusFlatProductionPerTurn;
            GrossProductionPerTurn = GrossProductionPerTurn + Owner.data.Traits.ProductionMod * GrossProductionPerTurn;


            if (Station != null && !LoadUniverse)
            {
                if (!HasShipyard)
                    Station.SetVisibility(false, Empire.Universe.ScreenManager, this);
                else
                    Station.SetVisibility(true, Empire.Universe.ScreenManager, this);
            }
            
            //Money
            GrossMoneyPT = Population / 1000f;
            GrossMoneyPT += PlusTaxPercentage * GrossMoneyPT;
            //this.GrossMoneyPT += this.GrossMoneyPT * this.Owner.data.Traits.TaxMod;
            //this.GrossMoneyPT += this.PlusFlatMoneyPerTurn + this.Population / 1000f * this.PlusCreditsPerColonist;
            MaxStorage = storageAdded;
            if (MaxStorage < 10) MaxStorage = 10f;
        }

        private void HarvestResources()
        {
            Unfed = SbCommodities.HarvestFood();
            SbCommodities.BuildingResources();              //Building resources is unused?
        }

        public float GetGoodAmount(string good) => SbCommodities.GetGoodAmount(good);
        
        private void GrowPopulation()
        {
            if (Owner == null) return;
            
            float normalRepRate = Owner.data.BaseReproductiveRate * Population;
            if ( normalRepRate > Owner.data.Traits.PopGrowthMax * 1000  && Owner.data.Traits.PopGrowthMax != 0 )
                normalRepRate = Owner.data.Traits.PopGrowthMax * 1000f;
            if ( normalRepRate < Owner.data.Traits.PopGrowthMin * 1000 )
                normalRepRate = Owner.data.Traits.PopGrowthMin * 1000f;
            normalRepRate += PlusFlatPopulationPerTurn;
            float adjustedRepRate = normalRepRate + Owner.data.Traits.ReproductionMod * normalRepRate;
            if (Unfed == 0) Population += adjustedRepRate;  //Unfed is calculated so it is 0 if everyone got food (even if just from storage)
            else        //  ^-- This one increases population if there is enough food to feed everyone
                Population += Unfed * 10f;      //So this else would only happen if there was not enough food. <-- This reduces population due to starvation.
            if (Population < 100.0) Population = 100f;      //Minimum population. I guess they wont all dire from starvation
        }

        public void AddGood(string goodId, int amount) => SbCommodities.AddGood(goodId, amount);
        

        public bool EventsOnBuildings()
        {
            bool events = false;
            foreach (Building building in BuildingList)
            {
                if (building.EventTriggerUID != null && !building.EventWasTriggered)
                {
                    events = true;
                    break;
                }
            }
            return events;
        }

        public enum GoodState
        {
            STORE,
            IMPORT,
            EXPORT,
        }



        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~Planet() { Dispose(false); }

        private void Dispose(bool disposing)
        {
            ActiveCombats?.Dispose(ref ActiveCombats);
            OrbitalDropList?.Dispose(ref OrbitalDropList);
            SbProduction    = null;
            SbCommodities   = null;
            TroopManager    = null;
            GeodeticManager = null;
            BasedShips?.Dispose(ref BasedShips);
            Projectiles?.Dispose(ref Projectiles);
            TroopsHere?.Dispose(ref TroopsHere);            
        }
    }
}


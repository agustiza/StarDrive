﻿using System;
using System.Collections.Generic;
using Ship_Game.AI;

namespace Ship_Game.Ships
{
    public partial class Ship
    {
        public float TradeTimer = 300;
        public bool TransportingColonists  { get; set; }
        public bool TransportingFood       { get; set; }
        public bool TransportingProduction { get; set; }
        public bool AllowInterEmpireTrade  { get; set; }
        public Array<Guid> TradeRoutes     { get; private set; } = new Array<Guid>();

        public bool IsCandidateForTradingBuild => IsFreighter && !IsConstructor;

        public bool IsFreighter => DesignRole == ShipData.RoleName.freighter
                                   && shipData.ShipCategory == ShipData.Category.Civilian;

        public bool IsIdleFreighter => IsFreighter
                                       && AI != null
                                       && !AI.HasPriorityOrder
                                       && AI.State != AIState.SystemTrader
                                       && AI.State != AIState.Flee
                                       && AI.State != AIState.Refit;

        public bool AddTradeRoute(Planet planet)
        {
            if (planet.Owner == null)
                return false;

            if (planet.Owner == loyalty || loyalty.GetRelations(planet.Owner).Treaty_Trade)
            {
                TradeRoutes.AddUnique(planet.guid);
                return true;
            }

            return false;
        }

        public void RemoveTradeRoute(Planet planet)
        {
            TradeRoutes.Remove(planet.guid);
        }

        public void RefreshTradeRoutes()
        {
            if (!loyalty.isPlayer)
                return; // Trade routes are available only for players

            foreach (Guid planetGuid in TradeRoutes)
            {
                Planet planet = Empire.Universe.PlanetsDict[planetGuid];
                if (planet.Owner == loyalty)
                    continue;

                if (planet.Owner == null || !loyalty.GetRelations(planet.Owner).Treaty_Trade)
                    RemoveTradeRoute(planet);
            }
        }

        public bool IsValidTradeRoute(Planet planet)
        {
            if (TradeRoutes.Count < 2)
                return true; // need at least 2 routes or AO for it to filter

            foreach (Guid g in TradeRoutes)
            {
                if (g == planet.guid)
                    return true;
            }
            return false;
        }

        public bool InTradingZones(Planet planet)
        {
            if (TradeRoutes.Count >= 2 && AreaOfOperation.NotEmpty) // ship has both AO trade routes, so check this or that.
                return IsValidTradeRoute(planet) || InsideAreaOfOperation(planet);

            return IsValidTradeRoute(planet) && InsideAreaOfOperation(planet);
        }

        public float BestFreighterValue(Empire empire, float fastVsBig)
        {
            float warpK          = maxFTLSpeed / 1000;
            float movementWeight = warpK + GetSTLSpeed() / 10 + rotationRadiansPerSecond.ToDegrees() - GetCost(empire) / 5;
            float cargoWeight    = CargoSpaceMax.Clamped(0, 80) - (float)SurfaceArea / 25;

            // For faster , cheaper ships vs big and maybe slower ships
            return movementWeight * fastVsBig + cargoWeight * (1 - fastVsBig);
        }

        public void DownloadTradeRoutes(Array<Guid> tradeRoutes)
        {
            TradeRoutes = tradeRoutes;
        }
    }
}

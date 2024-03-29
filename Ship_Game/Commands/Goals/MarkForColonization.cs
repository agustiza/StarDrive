﻿using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Ships;
using System;

namespace Ship_Game.Commands.Goals
{
    public class MarkForColonization : Goal
    {
        public const string ID = "MarkForColonization";
        public override string UID => ID;

        public MarkForColonization() : base(GoalType.Colonize)
        {
            Steps = new Func<GoalStep>[]
            {
                CreateClaimTask,
                CheckIfColonizationIsSafe,
                OrderShipForColonization,
                EnsureBuildingColonyShip,
                OrderShipToColonize,
                WaitForColonizationComplete
            };

        }
        public MarkForColonization(Planet toColonize, Empire e) : this()
        {
            empire             = e;
            ColonizationTarget = toColonize;
            if (PositiveEnemyPresence(out _)) 
                return;

            // Fast track to colonize if planet is safe and we have a ready Colony Ship
            FinishedShip = FindIdleColonyShip();
            if (FinishedShip != null)
            {
                ChangeToStep(OrderShipToColonize);
                Evaluate();
            }
        }

        GoalStep TargetPlanetStatus()
        {
            if (ColonizationTarget.Owner != null)
            {
                if (ColonizationTarget.Owner == empire)
                    return GoalStep.GoalComplete;

                foreach (var relationship in empire.AllRelations)
                    empire.GetEmpireAI().ExpansionAI.CheckClaim(relationship.Key, relationship.Value, ColonizationTarget);

                ReleaseShipFromGoal();
                Log.Info($"Colonize: {ColonizationTarget.Owner.Name} got there first");
                return GoalStep.GoalFailed;
            }

            return GoalStep.GoToNextStep;
        }

        GoalStep CreateClaimTask()
        {
            if (PositiveEnemyPresence(out float spaceStrength))
            {
                if (empire.isPlayer)
                    return GoalStep.GoalFailed;

                var task = MilitaryTask.CreateClaimTask(empire, ColonizationTarget, spaceStrength * 2);
                task.Priority = -2;
                empire.GetEmpireAI().AddPendingTask(task);
            }

            return GoalStep.GoToNextStep;
        }

        GoalStep CheckIfColonizationIsSafe()
        {
            if (TargetPlanetStatus() == GoalStep.GoalFailed)
                return GoalStep.GoalFailed;

            if (empire.GetEmpireAI().GetDefendClaimTaskFor(ColonizationTarget, out MilitaryTask task))
            {
                if (!PositiveEnemyPresence(out _) || task.Fleet?.TaskStep == 4)
                    return GoalStep.GoToNextStep;

                return GoalStep.TryAgain; // Claim task still in progress
            }

            // Check if there is enemy presence without a claim task
            if (PositiveEnemyPresence(out _))
            {
                ReleaseShipFromGoal();
                return GoalStep.GoalFailed;
            }

            return GoalStep.GoToNextStep;
        }

        GoalStep OrderShipForColonization()
        {
            if (TargetPlanetStatus() == GoalStep.GoalFailed)
                return GoalStep.GoalFailed;

            FinishedShip = FindIdleColonyShip();
            if (FinishedShip != null)
                return GoalStep.GoToNextStep;

            if (!ShipBuilder.PickColonyShip(empire, out Ship colonyShip))
                return GoalStep.GoalFailed;

            if (!empire.FindPlanetToBuildAt(empire.SafeSpacePorts, colonyShip, out Planet planet))
                return GoalStep.TryAgain;

            planet.Construction.Enqueue(colonyShip, this, notifyOnEmpty:empire.isPlayer);
            planet.Construction.PrioritizeShip(colonyShip);
            return GoalStep.GoToNextStep;
        }

        GoalStep EnsureBuildingColonyShip()
        {
            if (TargetPlanetStatus() == GoalStep.GoalFailed)
            {
                if (empire.GetEmpireAI().GetDefendClaimTaskFor(ColonizationTarget, out MilitaryTask task))
                    task.EndTask();

                return GoalStep.GoalFailed;
            }

            if (FinishedShip != null) // we already have a ship
                return GoalStep.GoToNextStep;

            if (!IsPlanetBuildingColonyShip())
            {
                PlanetBuildingAt = null;
                return GoalStep.RestartGoal;
            }

            if (ColonizationTarget.Owner == null)
                return GoalStep.TryAgain;

            return GoalStep.GoalComplete;
        }

        GoalStep OrderShipToColonize()
        {
            if (TargetPlanetStatus() == GoalStep.GoalFailed)
                return GoalStep.GoalFailed;

            if (FinishedShip == null) // @todo This is a workaround for possible safequeue bug causing this to fail on save load
            {
                if (empire.GetEmpireAI().GetDefendClaimTaskFor(ColonizationTarget, out MilitaryTask task))
                    task.EndTask();

                return GoalStep.GoalFailed;
            }

            FinishedShip.DoColonize(ColonizationTarget, this);
            return GoalStep.GoToNextStep;
        }

        GoalStep WaitForColonizationComplete()
        {
            if (TargetPlanetStatus() == GoalStep.GoalFailed)
                return GoalStep.GoalFailed;

            if (FinishedShip == null 
                || FinishedShip.AI.State != AIState.Colonize
                || !FinishedShip.AI.FindGoal(ShipAI.Plan.Colonize, out ShipAI.ShipGoal goal)
                || goal.TargetPlanet != ColonizationTarget)
            {
                if (empire.GetEmpireAI().GetDefendClaimTaskFor(ColonizationTarget, out MilitaryTask task))
                    task.EndTask();

                return GoalStep.GoalFailed;
            }

            if (ColonizationTarget.Owner == null) 
                return GoalStep.TryAgain;

            return GoalStep.GoalComplete;
        }

        void ReleaseShipFromGoal()
        {
            if (FinishedShip != null)
            {
                FinishedShip.AI.ClearOrdersAndWayPoints(AIState.AwaitingOrders);
                var nearestRallyPoint = empire.FindNearestRallyPoint(FinishedShip.Center);
                if (nearestRallyPoint != null)
                    FinishedShip.AI.OrderOrbitPlanet(nearestRallyPoint);
            }
        }

        bool PositiveEnemyPresence(out float spaceStrength)
        {
            spaceStrength = empire.KnownEnemyStrengthIn(ColonizationTarget.ParentSystem);
            return spaceStrength > 10 || ColonizationTarget.GetGroundStrengthOther(empire).Greater(0);
        }

        bool IsPlanetBuildingColonyShip()
        {
            if (PlanetBuildingAt == null)
                return false;

            return PlanetBuildingAt.IsColonyShipInQueue();
        }

        Ship FindIdleColonyShip()
        {
            if (FinishedShip != null)
                return FinishedShip;

            foreach (Ship ship in empire.GetShips())
            {
                if (ship.isColonyShip && ship.AI != null && ship.AI.State != AIState.Colonize)
                    return ship;
            }

            return null;
        }
    }
}

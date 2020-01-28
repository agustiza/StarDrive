﻿using System;
using System.Collections.Generic;
using Ship_Game.AI.Tasks;
using Ship_Game.Gameplay;

namespace Ship_Game.AI
{
    public class EmpireRiskAssessment
    {
        public float Expansion   { get; private set; }
        public float Border      { get; private set; }
        public float KnownThreat { get; private set; }

        public float Risk        { get; private set; }
        public float MaxRisk     { get; private set; }
        private readonly Empire Them;
        private readonly Relationship Relation;

        public EmpireRiskAssessment(Relationship relation)
        {
            Them = EmpireManager.GetEmpireByName(relation.Name);
            Relation = relation;
        }

        public void UpdateRiskAssessment(Empire us)
        {
            Expansion = ExpansionRiskAssessment(us);
            Border      = BorderRiskAssessment(us);
            KnownThreat = RiskAssessment(us);
            Risk        = Expansion + Border + KnownThreat;
            MaxRisk     = MathExt.Max3(Expansion, Border, KnownThreat);

        }

        private float ExpansionRiskAssessment(Empire us)
        {
            if (!Relation.Known  || Them == null || Them.NumPlanets == 0 || Them.data.Defeated)
                return 0;

            float themStrength = 0;
            float usStrength   = 0;

            foreach (Planet p in Them.GetPlanets())
            {
                if (!p.IsExploredBy(us)) continue;
                themStrength += p.ColonyValue;
            }

            foreach (Planet p in us.GetPlanets())
            {
                usStrength += p.ColonyValue;
            }
            float strength = ((themStrength - usStrength) / themStrength).Clamped(0,1);
            return strength;
        }

        private float BorderRiskAssessment(Empire us, float riskLimit = 2)
        {
            if (!Relation.Known || Them.data.Defeated)
                return 0;

            float strength = 0;
            foreach (SolarSystem ss in us.GetBorderSystems(Them))
            {
                strength += us.GetEmpireAI().ThreatMatrix.StrengthOfEmpireInSystem(Them, ss);
            }
            strength = ((strength - us.CurrentMilitaryStrength) / strength.ClampMin(1)).Clamped(0,1);
            return strength; 
        }

        private float RiskAssessment(Empire us, float riskLimit = 2)
        {
            if (!Relation.Known || Them.data.Defeated)
                return 0;
            //&& !(Relation.TotalAnger > (us.data.DiplomaticPersonality?.Territorialism ?? 50f))
            float risk = 0; 
            float strength = Math.Max(100, us.CurrentMilitaryStrength);
            if (!Them.isFaction && !Relation.AtWar && !Relation.PreparingForWar)
                return 0;

            if (!Them.isFaction)
            {
                risk = us.GetEmpireAI().ThreatMatrix.StrengthOfEmpire(Them) / strength;
                return risk; 
            }

            var s = new HashSet<SolarSystem>();
            var list = us.GetEmpireAI().TaskList;
            for (int i = 0; i < list.Count; i++)
            {
                MilitaryTask task = list[i];
                if (task.type != MilitaryTask.TaskType.DefendClaim)
                    continue;

                Planet p = task.TargetPlanet;
                SolarSystem ss = p.ParentSystem;
                if (!s.Add(ss))
                    continue;
                float test = us.GetEmpireAI().ThreatMatrix.StrengthOfEmpireInSystem(Them, ss);
                if (test > 0 && test < risk)
                    risk = test;
            }

            risk /= strength;
            return risk; 
        }

    }
}
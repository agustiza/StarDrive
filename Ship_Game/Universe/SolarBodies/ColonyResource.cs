﻿using System;

namespace Ship_Game.Universe.SolarBodies
{
    public abstract class ColonyResource
    {
        protected readonly Planet Planet;
        public bool Initialized { get; private set; }

        float PercentValue;
        public float Percent // Percentage workers allocated [0.0-1.0]
        {
            get => PercentValue;
            set => PercentValue = value.NaNChecked(0f, "Resource.Percent");
        }

        //public float Percent; // Percentage workers allocated [0.0-1.0]
        public bool PercentLock; // Percentage slider locked by user

        // Per Turn: Raw value produced before we apply any taxes or consume stuff
        public float GrossIncome { get; protected set; }

        // Per Turn: NetIncome = GrossIncome - (taxes + consumption)
        public float NetIncome { get; protected set; }

        // Per Turn: GrossIncome assuming we have Percent=1
        public float GrossMaxPotential { get; protected set; }

        // Per Turn: NetMaxPotential = GrossMaxPotential - (taxes + consumption)
        public float NetMaxPotential { get; protected set; }

        // Per Turn: Flat income added; no taxes applied
        public float FlatBonus { get; protected set; }

        // Per Turn: NetFlatBonus = FlatBonus - tax
        public float NetFlatBonus { get; protected set; }

        // Per Turn: Resources generated by colonists
        public float YieldPerColonist { get; protected set; }

        // Per Turn: NetYieldPerColonist = YieldPerColonist - taxes
        public float NetYieldPerColonist { get; protected set; }

        protected float Tax; // ex: 0.25 for 25% tax rate
        public float AfterTax(float grossValue) => grossValue - grossValue*Tax;

        protected ColonyResource(Planet planet) { Planet = planet; }

        protected abstract void RecalculateModifiers();

        // Purely used for estimation
        protected virtual float AvgResourceConsumption() => 0.0f;

        public virtual void Update(float consumption)
        {
            Initialized = true;
            FlatBonus = 0f;
            RecalculateModifiers();

            GrossMaxPotential = YieldPerColonist * Planet.PopulationBillion;
            GrossIncome = FlatBonus + Percent * GrossMaxPotential;

            // taxes get applied before consumption
            // because government gets to eat their pie first :)))
            NetIncome           = AfterTax(GrossIncome) - consumption;
            NetMaxPotential     = AfterTax(GrossMaxPotential) - consumption;
            NetFlatBonus        = AfterTax(FlatBonus);
            NetYieldPerColonist = AfterTax(YieldPerColonist);
        }

        public float ColonistIncome(float yieldPerColonist)
        {
            return Percent * yieldPerColonist * Planet.PopulationBillion;
        }

        // Nominal workers needed to neither gain nor lose storage
        // @param flat Extra flat bonus to use in calculation
        // @param perCol Extra per colonist bonus to use in calculation
        public float WorkersNeededForEquilibrium(float flat = 0.0f, float perCol = 0.0f)
        {
            if (Planet.Population <= 0)
                return 0;

            float grossColo = (YieldPerColonist + perCol) * Planet.PopulationBillion;
            float grossFlat = (FlatBonus + flat);

            float netColo = AfterTax(grossColo);
            float netFlat = AfterTax(grossFlat);

            float needed = AvgResourceConsumption() - netFlat;
            float minWorkers = netColo.AlmostZero() ? 0f : (needed / netColo);
            return minWorkers.NaNChecked(0f, "WorkersNeededForEquilibrium").Clamped(0.0f, 0.9f);
        }

        public float EstPercentForNetIncome(float targetNetIncome)
        {
            // give negative flat bonus to shift the equilibrium point
            // towards targetNetIncome
            float flat = (-targetNetIncome) / (1f - Tax);
            return WorkersNeededForEquilibrium(flat);
        }


        public void AutoBalanceWorkers(float otherWorkers)
        {
            Percent = Math.Max(1f - otherWorkers, 0f);
        }

        public void AutoBalanceWorkers()
        {
            bool noResearch = Planet.Owner.Research.NoTopic
                              && Planet.colonyType != Planet.ColonyType.Colony
                              && Planet.colonyType != Planet.ColonyType.TradeHub;

            ColonyResource a, b;
            if      (this == Planet.Food) { a = Planet.Prod; b = Planet.Res;  }
            else if (this == Planet.Prod) { a = Planet.Food; b = Planet.Res;  }
            else if (this == Planet.Res)  { a = Planet.Food; b = Planet.Prod; }
            else return; // we're not Food,Prod,Res, so bail out

            if (this == Planet.Res && (Planet.Res.YieldPerColonist.AlmostZero() || noResearch))
                // no need to assign research since no capacity available or no research in progress.
                AutoBalanceWithZeroResearch(a, b);
            else
                AutoBalanceWorkers(a.Percent + b.Percent);
        }

        public void AutoBalanceWithZeroResearch(ColonyResource food, ColonyResource prod)
        {
            float remainder = 1 - (food.Percent + prod.Percent);
            if (Planet.Owner.IsCybernetic)
                prod.Percent += remainder;
            else
            {
                food.Percent += remainder / 2;
                prod.Percent += remainder / 2;
            }
        }
    }


    public class ColonyFood : ColonyResource
    {
        public ColonyFood(Planet planet) : base(planet)
        {
        }

        protected override void RecalculateModifiers()
        {
            float plusPerColonist = 0f;
            foreach (Building b in Planet.BuildingList)
            {
                plusPerColonist += b.PlusFoodPerColonist;
                FlatBonus       += b.PlusFlatFoodAmount;
            }


            YieldPerColonist = Planet.Fertility * (1 + plusPerColonist);
            Tax = 0f;
            // If we use tax effects with Food resource,
            // we need a base yield offset for balance
            //YieldPerColonist += 0.25f;
        }

        protected override float AvgResourceConsumption()
        {
            return Planet.NonCybernetic ? Planet.Consumption : 0f;
        }

        public override void Update(float consumption)
        {
            base.Update(Planet.NonCybernetic ? consumption : 0f);
        }
    }

    public class ColonyProduction : ColonyResource
    {
        public ColonyProduction(Planet planet) : base(planet)
        {
        }

        protected override void RecalculateModifiers()
        {
            float richness = Planet.MineralRichness;
            float plusPerColonist = 0f;
            foreach (Building b in Planet.BuildingList)
            {
                plusPerColonist += b.PlusProdPerColonist;
                FlatBonus += b.PlusProdPerRichness * richness;
                FlatBonus += b.PlusFlatProductionAmount;
            }
            float productMod = Planet.Owner.data.Traits.ProductionMod;
            YieldPerColonist = richness * (1+ plusPerColonist) * (1 + productMod);

            // Cybernetics consume production and will starve at 100% tax, so ease up on them
            Tax = Planet.NonCybernetic ? Planet.Owner.data.TaxRate : Planet.Owner.data.TaxRate  * 0.5f;
        }

        protected override float AvgResourceConsumption()
        {
            return Planet.IsCybernetic ? Planet.Consumption : 0f;
        }

        public override void Update(float consumption)
        {
            base.Update(Planet.IsCybernetic ? consumption : 0f);
        }
    }

    public class ColonyResearch : ColonyResource
    {
        public ColonyResearch(Planet planet) : base(planet)
        {
        }

        protected override void RecalculateModifiers()
        {
            float plusPerColonist = 0f;
            foreach (Building b in Planet.BuildingList)
            {
                plusPerColonist += b.PlusResearchPerColonist;
                FlatBonus       += b.PlusFlatResearchAmount;
            }
            float researchMod = Planet.Owner.data.Traits.ResearchMod;
            // @note Research only comes from buildings
            // Outposts and Capital Cities always grant a small bonus
            YieldPerColonist = plusPerColonist * (1 + researchMod);
            Tax = Planet.Owner.data.TaxRate * Planet.Owner.data.Traits.ResearchTaxMultiplier;
        }

        // @todo Estimate how much research we need
        protected override float AvgResourceConsumption() => 4.0f; // This is a good MINIMUM research value for estimation
    }

    public class ColonyMoney
    {
        readonly Planet Planet;
        public float IncomePerColonist { get; private set; }

        // The current tax rate applied by empire tax rate and planet tax rate modifiers
        public float TaxRate { get; private set; }

        // revenue before maintenance is deducted
        public float GrossRevenue { get; private set; }

        // maintenance costs from all buildings, with maintenance multiplier applied
        public float Maintenance { get; private set; }

        // revenue after maintenance was deducted
        public float NetRevenue { get; private set; }

        // Maximum Revenue from this planet if Tax is 100%
        public float PotentialRevenue { get; private set; }

        public ColonyMoney(Planet planet) { Planet = planet; }

        public float NetRevenueGain(Building b)
        {
            float newPopulation = b.MaxPopIncrease/1000f;
            float grossIncome   = newPopulation * IncomePerColonist * TaxRate;
            return grossIncome - b.Maintenance;
        }

        public void Update()
        {
            // Base tax rate comes from current empire tax %
            TaxRate = Planet.Owner.data.TaxRate;

            Maintenance             = 0f;
            IncomePerColonist       = 1f;
            float taxRateMultiplier = 1f + Planet.Owner.data.Traits.TaxMod;
            foreach (Building b in Planet.BuildingList)
            {
                IncomePerColonist += b.CreditsPerColonist;
                taxRateMultiplier += b.PlusTaxPercentage;
                Maintenance       += b.Maintenance;
            }

            // And finally we adjust local TaxRate by the bonus multiplier
            TaxRate     *= taxRateMultiplier;
            Maintenance *= Planet.Owner.data.Traits.MaintMultiplier;

            GrossRevenue = Planet.PopulationBillion * IncomePerColonist * TaxRate;
            NetRevenue   = GrossRevenue - Maintenance;

            // Needed for empire treasury goal
            PotentialRevenue = Planet.PopulationBillion * IncomePerColonist * taxRateMultiplier;
        }
    }
}

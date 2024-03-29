﻿using Ship_Game.AI.Budget;

namespace Ship_Game
{
    public partial class Planet
    {

        public float EstimatedAverageProduction => (Prod.NetMaxPotential / 3).LowerBound(0.1f);
        float EstimatedAverageFood              => (Food.NetMaxPotential / 3).LowerBound(0.1f);

        public void DoGoverning()
        {
            RefreshBuildingsWeCanBuildHere();
            if (RecentCombat)
                return; // Cant Build stuff when there is combat on the planet

            BuildTroops();

            if (colonyType == ColonyType.Colony)
                return; // No Governor? Never mind!

            BuildOutpostIfAble();   //If there is no Outpost or Capital, build it
            bool noResearch = Owner.Research.NoTopic;

            // Switch to Core if there is nothing in the research queue (Does not actually change assigned Governor)
            if (colonyType == ColonyType.Research && noResearch)
                colonyType = ColonyType.Core;

            Food.Percent = 0;
            Prod.Percent = 0;
            Res.Percent  = 0;

            PlanetBudget budget = AllocateColonyBudget();
            switch (colonyType) // New resource management by Gretman
            {
                case ColonyType.TradeHub:
                    AssignCoreWorldWorkers();
                    DetermineFoodState(0.2f, 0.8f);
                    DetermineProdState(0.2f, 0.8f);
                    break;
                case ColonyType.Core:
                    AssignCoreWorldWorkers();
                    BuildAndScrapBuildings(budget);
                    DetermineFoodState(0.2f, 0.5f); // Start Importing if stores drop below 20%, and stop importing once stores are above 50%.
                    DetermineProdState(0.2f, 0.5f); // Start Exporting if stores are above 50%, but dont stop exporting unless stores drop below 25%.
                    break;
                case ColonyType.Industrial:
                    // Farm to 33% storage, then devote the rest to Work, then to research when that starts to fill up
                    AssignOtherWorldsWorkers(0.333f, 1);
                    BuildAndScrapBuildings(budget);
                    DetermineFoodState(0.4f, 1f);    // Start Importing if food drops below 50%, and stop importing once stores reach 100%. Will only export food due to excess FlatFood.
                    DetermineProdState(0.15f, 0.666f); // Start Importing if prod drops below 15%, stop importing at 30%. Start exporting at 66%, and dont stop unless below 33%.
                    break;
                case ColonyType.Research:
                    //This governor will rely on imports, focusing on research as long as no one is starving
                    AssignOtherWorldsWorkers(0.15f, 0.15f);
                    BuildAndScrapBuildings(budget);
                    DetermineFoodState(0.4f, 1f); // Import if either drops below 50%, and stop importing once stores reach 100%.
                    DetermineProdState(0.2f, 0.6f); // This planet will export when stores reach 60%
                    break;
                case ColonyType.Agricultural:
                    AssignOtherWorldsWorkers(1, 0.333f);
                    BuildAndScrapBuildings(budget);
                    DetermineFoodState(0.15f, 0.5f); // Start Importing if food drops below 15%, stop importing at 30%. Start exporting at 50%, and dont stop unless below 33%.
                    DetermineProdState(0.25f, 1);    // Start Importing if prod drops below 25%, and stop importing once stores reach 100%. Will only export prod due to excess FlatProd.
                    break;
                case ColonyType.Military:
                    AssignOtherWorldsWorkers(0.3f, 0.7f);
                    BuildAndScrapBuildings(budget);
                    DetermineFoodState(0.4f, 0.95f); // Import if either drops below 40%, and stop importing once stores reach 95%.
                    DetermineProdState(0.75f, 1); // This planet will only export Food or Prod due to excess FlatFood or FlatProd
                    break;
            } // End Gov type Switch

            BuildPlatformsAndStations(budget);
        }

        public PlanetBudget AllocateColonyBudget() => Owner.GetEmpireAI().PlanetBudget(this);
        public float CivilianBuildingsMaintenance  => Money.Maintenance - MilitaryBuildingsMaintenance;

        public float ColonyDebtTolerance
        {
            get
            {
                float debtTolerance = 3 * (1 - PopulationRatio); // the bigger the colony, the less debt tolerance it has, it should be earning money
                if (MaxPopulationBillion < 2)
                    debtTolerance += 2f - MaxPopulationBillion;

                return debtTolerance.LowerBound(0); // Note - dept tolerance is a positive number added to the budget for small colonies
            }
        }

        //New Build Logic by Gretman, modified by Fat Bastard
        void BuildAndScrapBuildings(PlanetBudget colonyBudget)
        {
            if (RecentCombat)
                return; // Do not build or scrap when in combat

            BuildAndScrapCivilianBuildings(colonyBudget.CivilianBuildings);
            BuildAndScrapMilitaryBuildings(colonyBudget.MilitaryBuildings);
        }

        // returns the amount of production to spend in the build queue based on import/export state
        public float LimitedProductionExpenditure()
        {
            float prodToSpend;
            if (colonyType == ColonyType.Colony)
            {
                switch (PS)
                {
                    default: // Importing
                        prodToSpend = ProdHere; // we are manually importing, so let's spend it all
                        break;
                    case GoodState.STORE:
                        if (Storage.ProdRatio.AlmostEqual(1))
                            prodToSpend = Prod.NetIncome; // Spend all our Income since storage is full
                        else
                            prodToSpend = Prod.NetIncome * 0.5f; // Store 50% of our prod income
                        break;
                    case GoodState.EXPORT:
                        if (OutgoingProdFreighters > 0)
                            prodToSpend = Prod.NetIncome * Storage.ProdRatio; // We are actively exporting so save some for storage
                        else
                            prodToSpend = ProdHere * 0.5f; // Spend 50% from our current stores and net production
                        break;
                }
            }
            else // Governor is auto managing good state
            {
                switch (PS)
                {
                    default: // Importing
                        if (IncomingProdFreighters > 0)
                            prodToSpend = ProdHere + Prod.NetIncome; // We have incoming prod, so we can spend more now
                        else
                            prodToSpend = (ProdHere + Prod.NetIncome) * 0.5f; // Spend less since nothing is coming
                        break;
                    case GoodState.STORE:
                        prodToSpend = ProdHere * 0.5f; // Spend some of our store since we are storing for building stuff
                        break;
                    case GoodState.EXPORT:
                        if (OutgoingProdFreighters > 0 && ConstructionQueue.Count < 6)
                        {
                            if (Storage.ProdRatio > 0.8f)
                                prodToSpend = Prod.NetIncome + Storage.Prod * 0.1f; // We are actively exporting but can afford some storage spending
                            else
                                prodToSpend = Prod.NetIncome * Storage.ProdRatio; // We are actively exporting so save some for storage
                        }
                        else
                            prodToSpend = ProdHere; // We are exporting but there is no demand or we are building many things, so let's spend it ourselves
                        break;
                }
            }

            return prodToSpend;
        }
    }
}

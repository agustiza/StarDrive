﻿namespace Ship_Game
{
    public struct DifficultyModifiers
    {
        public readonly int SysComModifier;
        public readonly int DiploWeightVsPlayer;
        public readonly float BaseColonyGoals;
        public readonly float ColonyGoalMultiplier; // Multiplier to the colony goals by rank
        public readonly float ShipBuildStrMin;
        public readonly float ShipBuildStrMax;
        public readonly int ColonyRankModifier;
        public readonly float TaskForceStrength;
        public readonly bool DataVisibleToPlayer;
        public readonly float Anger;
        public readonly int ShipLevel;
        public readonly bool HideTacticalData;
        public readonly float MaxDesiredPlanets;
        public readonly float CreditsMultiplier;
        public readonly float EnemyTroopStrength;
        public readonly int MineralDecayDivider;
        public readonly float PiratePayModifier;
        public readonly int MinStartingColonies; // Starting colonies what we want
        public readonly int ExpandSearchTurns; // For Expansion
        public readonly int RemnantTurnsLevelUp; // How many turns should pass before Remnants level up
        public readonly float RemnantResourceMod; // Multiplier to Remnant Prod generation
        public readonly float RemnantNumBombers; // Multiplier to Remnant bombers wanted
        public readonly int StandByColonyShips;
        public readonly int TrustLostStoleColony; // Vs players

        // AI Buffs/Nerfs
        public readonly float FlatMoneyBonus;
        public readonly float ProductionMod;
        public readonly float ResearchMod;
        public readonly float TaxMod;
        public readonly float ShipCostMod;
        public readonly float ResearchTaxMultiplier;
        public readonly float ModHpModifier;

        /// <summary>
        /// Higher than 1 will decrease colonization pace and vice versa
        /// </summary>
        public readonly float ExpansionMultiplier; 

        public DifficultyModifiers(Empire empire, UniverseData.GameDifficulty difficulty)
        {
            DataVisibleToPlayer   = false;
            FlatMoneyBonus        = 0;
            ProductionMod         = 0;
            ResearchMod           = 0;
            TaxMod                = 0;
            ShipCostMod           = 0;
            ResearchTaxMultiplier = 0;
            ModHpModifier         = 0;
            switch (difficulty)
            {
                case UniverseData.GameDifficulty.Easy:
                    ShipBuildStrMin      = 0.3f;
                    ShipBuildStrMax      = 0.8f;
                    ColonyRankModifier   = -2;
                    TaskForceStrength    = 0.8f;
                    DataVisibleToPlayer  = true;
                    ShipLevel            = 0;
                    HideTacticalData     = false;
                    MaxDesiredPlanets    = 0.25f;
                    CreditsMultiplier    = empire.isPlayer ? 0.1f : 0.25f;
                    EnemyTroopStrength   = 1f;
                    MineralDecayDivider  = 100;
                    PiratePayModifier    = 0.5f;
                    ExpansionMultiplier  = 1.25f;
                    MinStartingColonies  = 2;
                    ExpandSearchTurns    = 50;
                    RemnantTurnsLevelUp  = 540;
                    RemnantResourceMod   = 0.2f;
                    RemnantNumBombers    = 0.5f;
                    BaseColonyGoals      = 2;
                    ColonyGoalMultiplier = 0;
                    StandByColonyShips   = 1;
                    TrustLostStoleColony = 0;

                    if (!empire.isPlayer)
                    {
                        ProductionMod = -0.1f;
                        ResearchMod   = -0.1f;
                        TaxMod        = -0.1f;
                        ModHpModifier = -0.1f;
                    }

                    break;
                default:
                case UniverseData.GameDifficulty.Normal:
                    ShipBuildStrMin      = 0.7f;
                    ShipBuildStrMax      = 1;
                    ColonyRankModifier   = 0;
                    TaskForceStrength    = 1.2f;
                    ShipLevel            = 0;
                    HideTacticalData     = false;
                    MaxDesiredPlanets    = 0.5f;
                    CreditsMultiplier    = 0.2f;
                    EnemyTroopStrength   = 1.1f;
                    MineralDecayDivider  = 50;
                    PiratePayModifier    = 0.75f;
                    ExpansionMultiplier  = 0.75f;
                    MinStartingColonies  = 3;
                    ExpandSearchTurns    = 30;
                    RemnantTurnsLevelUp  = 480;
                    RemnantResourceMod   = 0.35f;
                    RemnantNumBombers    = 0.75f;
                    BaseColonyGoals      = 2;
                    ColonyGoalMultiplier = 0.5f;
                    StandByColonyShips   = 2;
                    TrustLostStoleColony = 5;
                    break;
                case UniverseData.GameDifficulty.Hard:
                    ShipBuildStrMin      = 0.8f;
                    ShipBuildStrMax      = 1f;
                    ColonyRankModifier   = 1;
                    TaskForceStrength    = 1.5f;
                    ShipLevel            = 2;
                    HideTacticalData     = true;
                    MaxDesiredPlanets    = 0.75f;
                    CreditsMultiplier    = empire.isPlayer ? 0.3f : 0.1f;
                    EnemyTroopStrength   = 1.25f;
                    MineralDecayDivider  = 25;
                    PiratePayModifier    = 1f;
                    ExpansionMultiplier  = 0.25f;
                    MinStartingColonies  = 5;
                    ExpandSearchTurns    = 20;
                    RemnantTurnsLevelUp  = 400;
                    RemnantResourceMod   = 0.45f;
                    RemnantNumBombers    = 1f;
                    BaseColonyGoals      = 4;
                    ColonyGoalMultiplier = 0.75f;
                    StandByColonyShips   = 3;
                    TrustLostStoleColony = 10;

                    if (!empire.isPlayer)
                    {
                        FlatMoneyBonus        = 10;
                        ProductionMod         = 0.5f;
                        ResearchMod           = 0.75f;
                        TaxMod                = 0.5f;
                        ShipCostMod           = -0.2f;
                        ResearchTaxMultiplier = 0.7f;
                    }

                    break;
                case UniverseData.GameDifficulty.Brutal:
                    ShipBuildStrMin      = 0.9f;
                    ShipBuildStrMax      = 1f;
                    ColonyRankModifier   = 2;
                    TaskForceStrength    = 1.75f;
                    ShipLevel            = 3;
                    HideTacticalData     = true;
                    MaxDesiredPlanets    = 1f;
                    CreditsMultiplier    = empire.isPlayer ? 0.5f : 0.05f;
                    EnemyTroopStrength   = 1.5f;
                    MineralDecayDivider  = 15;
                    PiratePayModifier    = 1.5f;
                    ExpansionMultiplier  = 0.1f;
                    MinStartingColonies  = 6;
                    ExpandSearchTurns    = 10;
                    RemnantTurnsLevelUp  = 400;
                    RemnantResourceMod   = 0.6f;
                    RemnantNumBombers    = 1.5f;
                    BaseColonyGoals      = 5;
                    ColonyGoalMultiplier = 1;
                    StandByColonyShips   = 3;
                    TrustLostStoleColony = 15;

                    if (!empire.isPlayer)
                    {
                        FlatMoneyBonus        = 20;
                        ProductionMod         = 1f;
                        ResearchMod           = 1.33f;
                        TaxMod                = 1f;
                        ShipCostMod           = -0.5f;
                        ResearchTaxMultiplier = 0.3f;
                    }

                    break;
            }

            SysComModifier      = (int)(((int)difficulty + 1) * 0.5f + 0.5f);
            DiploWeightVsPlayer = (int)difficulty + 1;
            Anger               = 1 + ((int)difficulty + 1) * 0.2f;

            if (empire.isPlayer)
            {
                BaseColonyGoals    = 5;
                ShipBuildStrMin    = 1f;
                ShipBuildStrMax    = 1f;
                ColonyRankModifier = 0;
                TaskForceStrength  = 1f;

                if (GlobalStats.FixedPlayerCreditCharge && difficulty > UniverseData.GameDifficulty.Easy)
                    CreditsMultiplier = 0.2f;
            }
        }
    }
}

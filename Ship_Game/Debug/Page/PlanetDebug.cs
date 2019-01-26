using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game.Debug.Page
{
    internal class PlanetDebug : DebugPage
    {
        readonly UniverseScreen Screen;
        public PlanetDebug(UniverseScreen screen, DebugInfoScreen parent) : base(parent, DebugModes.Planets)
        {
            Screen = screen;
        }

        public override void Update(float deltaTime)
        {
            Planet planet = Screen.SelectedPlanet;
            if (planet == null)
            {
                var text = new Array<DebugTextBlock>();
                foreach (Empire empire in EmpireManager.Empires)
                {
                    if (!empire.isFaction)
                        text.Add(empire.DebugEmpirePlanetInfo());
                }
                SetTextColumns(text);
            }
            else
            {
                HideAllDebugText();
                SetTextColumns(new Array<DebugTextBlock>{ planet.DebugPlanetInfo() });
            }
            base.Update(deltaTime);
        }

        public override void Draw(SpriteBatch batch)
        {
            if (!Visible)
                return;

            foreach (Empire e in EmpireManager.Empires)
            {
                foreach (Ship ship in e.GetShips())
                {
                    if (ship?.Active != true) continue;
                    ShipAI ai = ship.AI;
                    if (ai.State != AIState.SystemTrader) continue;
                    if (ai.OrderQueue.Count == 0) continue;

                    switch (ai.OrderQueue.PeekLast.Plan)
                    {
                        case ShipAI.Plan.DropOffGoods:
                            Screen.DrawCircleProjectedZ(ship.Center, 50f, ai.IsFood ? Color.GreenYellow : Color.SteelBlue, 6);
                            break;
                        case ShipAI.Plan.PickupGoods:
                            Screen.DrawCircleProjectedZ(ship.Center, 50f, ai.IsFood ? Color.GreenYellow : Color.SteelBlue, 3);
                            break;
                        case ShipAI.Plan.PickupPassengers:
                        case ShipAI.Plan.DropoffPassengers:
                            Screen.DrawCircleProjectedZ(ship.Center, 50f, e.EmpireColor, 32);
                            break;
                    }
                }
            }
            base.Draw(batch);
        }
    }
}
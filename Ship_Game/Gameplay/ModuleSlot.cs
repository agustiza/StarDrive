using Microsoft.Xna.Framework;
using System;
using System.Xml.Serialization;

namespace Ship_Game.Gameplay
{
    public sealed class ModuleSlotData
    {
        public Vector2 Position;
        public string InstalledModuleUID;
        public Guid HangarshipGuid;
        public float Health;
        [XmlElement(ElementName = "Shield_Power")]
        public float ShieldPower;
        [XmlElement(ElementName = "facing")]
        public float Facing;
        [XmlElement(ElementName = "state")]
        public string Orientation;
        public Restrictions Restrictions;
        public string SlotOptions;
        

        public override string ToString() => $"{InstalledModuleUID} {Position} {Facing} {Restrictions}";

        public ShipDesignScreen.ActiveModuleState GetOrientation()
        {
            if (Orientation.NotEmpty() && Orientation != "Normal")
                return (ShipDesignScreen.ActiveModuleState)
                    Enum.Parse(typeof(ShipDesignScreen.ActiveModuleState), Orientation);
            return ShipDesignScreen.ActiveModuleState.Normal;
        }
    }
}
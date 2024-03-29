﻿// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Shadows.Forward.ShadowDirectionalMap
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using Microsoft.Xna.Framework.Graphics;

namespace SynapseGaming.LightingSystem.Shadows.Forward
{
    /// <summary>
    /// Shadow map class that implements cascading level-of-detail
    /// directional shadows. Used for directional lights.
    /// </summary>
    public class ShadowDirectionalMap : BaseShadowDirectionalMap
    {
        /// <summary>
        /// Creates a new effect that performs rendering specific to the shadow
        /// mapping implementation used by this object.
        /// </summary>
        /// <returns></returns>
        protected override Effect CreateEffect()
        {
            Effect fx;
            try
            {
                fx = new ShadowEffect(Device);
            }
            catch (Exception e)
            {
                Console.WriteLine($"ShadowDirectionalMap.Create failed. Scheduling full Garbage Collect. Error was: " + e.Message);
                GC.Collect(3, GCCollectionMode.Forced, true);
                fx = new ShadowEffect(Device);
            }
            return fx;
        }
    }
}

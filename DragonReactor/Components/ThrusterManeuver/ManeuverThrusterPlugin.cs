﻿using UnityEngine;

namespace ContentMod.Components.ManeuverThruster
{
    public abstract class ManeuverThrusterPlugin : ComponentPluginBase
    {
        public ManeuverThrusterPlugin()
        {
        }
        public override Texture2D IconTexture
        {
            get { return (Texture2D)Resources.Load("Icons/81_Thrusters"); }
        }
        public virtual float MaxOutput
        {
            get { return .1f; }
        }
        public virtual float MaxPowerUsage_Watts
        {
            get { return 2000f; }
        }
    }
}

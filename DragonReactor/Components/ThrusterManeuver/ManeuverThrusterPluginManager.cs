﻿using CodeStage.AntiCheat.ObscuredTypes;
using HarmonyLib;
using PulsarPluginLoader;
using System;
using System.Collections.Generic;
using System.Reflection;
using Logger = PulsarPluginLoader.Utilities.Logger;

namespace ContentMod.Components.ManeuverThruster
{
    public class ManeuverThrusterPluginManager
    {
        public readonly int VanillaManeuverThrusterMaxType = 0;
        private static ManeuverThrusterPluginManager m_instance = null;
        public readonly List<ManeuverThrusterPlugin> ManeuverThrusterTypes = new List<ManeuverThrusterPlugin>();
        public static ManeuverThrusterPluginManager Instance
        {
            get
            {
                if (m_instance == null)
                {
                    m_instance = new ManeuverThrusterPluginManager();
                }
                return m_instance;
            }
        }

        ManeuverThrusterPluginManager()
        {
            VanillaManeuverThrusterMaxType = Enum.GetValues(typeof(EManeuverThrusterType)).Length;
            Logger.Info($"MaxTypeint = {VanillaManeuverThrusterMaxType - 1}");
            foreach (PulsarPlugin plugin in PluginManager.Instance.GetAllPlugins())
            {
                Assembly asm = plugin.GetType().Assembly;
                Type ManeuverThrusterPlugin = typeof(ManeuverThrusterPlugin);
                foreach (Type t in asm.GetTypes())
                {
                    if (ManeuverThrusterPlugin.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                    {
                        Logger.Info("Loading ManeuverThruster from assembly");
                        ManeuverThrusterPlugin ManeuverThrusterPluginHandler = (ManeuverThrusterPlugin)Activator.CreateInstance(t);
                        if (GetManeuverThrusterIDFromName(ManeuverThrusterPluginHandler.Name) == -1)
                        {
                            ManeuverThrusterTypes.Add(ManeuverThrusterPluginHandler);
                            Logger.Info($"Added ManeuverThruster: '{ManeuverThrusterPluginHandler.Name}' with ID '{GetManeuverThrusterIDFromName(ManeuverThrusterPluginHandler.Name)}'");
                        }
                        else
                        {
                            Logger.Info($"Could not add ManeuverThruster from {plugin.Name} with the duplicate name of '{ManeuverThrusterPluginHandler.Name}'");
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Finds ManeuverThruster type equivilent to given name and returns Subtype ID needed to spawn. Returns -1 if couldn't find ManeuverThruster.
        /// </summary>
        /// <param name="ManeuverThrusterName"></param>
        /// <returns></returns>
        public int GetManeuverThrusterIDFromName(string ManeuverThrusterName)
        {
            for (int i = 0; i < ManeuverThrusterTypes.Count; i++)
            {
                if (ManeuverThrusterTypes[i].Name == ManeuverThrusterName)
                {
                    return i + VanillaManeuverThrusterMaxType;
                }
            }
            return -1;
        }
        public static PLManeuverThruster CreateManeuverThruster(int Subtype, int level)
        {
            PLManeuverThruster InManeuverThruster;
            if (Subtype >= Instance.VanillaManeuverThrusterMaxType)
            {
                InManeuverThruster = new PLManeuverThruster(EManeuverThrusterType.E_MAX, level);
                int subtypeformodded = Subtype - Instance.VanillaManeuverThrusterMaxType;
                if (Global.DebugLogging)
                {
                    Logger.Info($"Subtype for modded is {subtypeformodded}");
                }
                if (subtypeformodded <= Instance.ManeuverThrusterTypes.Count && subtypeformodded > -1)
                {
                    if (Global.DebugLogging)
                    {
                        Logger.Info("Creating ManeuverThruster from list info");
                    }
                    ManeuverThrusterPlugin ManeuverThrusterType = Instance.ManeuverThrusterTypes[Subtype - Instance.VanillaManeuverThrusterMaxType];
                    InManeuverThruster.SubType = Subtype;
                    InManeuverThruster.Name = ManeuverThrusterType.Name;
                    InManeuverThruster.Desc = ManeuverThrusterType.Description;
                    InManeuverThruster.GetType().GetField("m_IconTexture", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(InManeuverThruster, ManeuverThrusterType.IconTexture);
                    InManeuverThruster.GetType().GetField("m_MaxOutput", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(InManeuverThruster, ManeuverThrusterType.MaxOutput);
                    InManeuverThruster.GetType().GetField("m_MaxPowerUsage_Watts", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(InManeuverThruster, ManeuverThrusterType.MaxPowerUsage_Watts);
                    InManeuverThruster.GetType().GetField("m_MarketPrice", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(InManeuverThruster, (ObscuredInt)ManeuverThrusterType.MarketPrice);
                    InManeuverThruster.CargoVisualPrefabID = ManeuverThrusterType.CargoVisualID;
                    InManeuverThruster.CanBeDroppedOnShipDeath = ManeuverThrusterType.CanBeDroppedOnShipDeath;
                    InManeuverThruster.Experimental = ManeuverThrusterType.Experimental;
                    InManeuverThruster.Unstable = ManeuverThrusterType.Unstable;
                    InManeuverThruster.Contraband = ManeuverThrusterType.Contraband;
                    InManeuverThruster.GetType().GetMethod("UpdateMaxPowerWatts", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(InManeuverThruster, new object[0]);
                }
            }
            else
            {
                InManeuverThruster = new PLManeuverThruster((EManeuverThrusterType)Subtype, level);
            }
            return InManeuverThruster;
        }
    }
    //Converts hashes to ManeuverThrusters.
    [HarmonyPatch(typeof(PLManeuverThruster), "CreateManeuverThrusterFromHash")]
    class ManeuverThrusterHashFix
    {
        static bool Prefix(int inSubType, int inLevel, ref PLShipComponent __result)
        {
            __result = ManeuverThrusterPluginManager.CreateManeuverThruster(inSubType, inLevel);
            return false;
        }
    }
    /*[HarmonyPatch(typeof(PLManeuverThruster), "Tick")]
    class TickPatch
    {
        static void Postfix(PLManeuverThruster __instance)
        {
            int subtypeformodded = __instance.SubType - ManeuverThrusterPluginManager.Instance.VanillaManeuverThrusterMaxType;
            if (subtypeformodded > -1 && subtypeformodded < ManeuverThrusterPluginManager.Instance.ManeuverThrusterTypes.Count && __instance.ShipStats != null && __instance.ShipStats.ManeuverThrusterTempMax != 0f)
            {
                ManeuverThrusterPluginManager.Instance.ManeuverThrusterTypes[subtypeformodded].ManeuverThrusterPowerCode(__instance);
            }
        }
    }*/
}

﻿using CodeStage.AntiCheat.ObscuredTypes;
using HarmonyLib;
using PulsarPluginLoader;
using System;
using System.Collections.Generic;
using System.Reflection;
using Logger = PulsarPluginLoader.Utilities.Logger;

namespace DragonReactor.Components.Thruster
{
    class ThrusterPluginManager
    {
        public readonly int VanillaThrusterMaxType = 0;
        private static ThrusterPluginManager m_instance = null;
        public readonly List<ThrusterPlugin> ThrusterTypes = new List<ThrusterPlugin>();
        public static ThrusterPluginManager Instance
        {
            get
            {
                if (m_instance == null)
                {
                    m_instance = new ThrusterPluginManager();
                }
                return m_instance;
            }
        }

        ThrusterPluginManager()
        {
            VanillaThrusterMaxType = Enum.GetValues(typeof(EThrusterType)).Length;
            Logger.Info($"MaxTypeint = {VanillaThrusterMaxType - 1}");
            foreach (PulsarPlugin plugin in PluginManager.Instance.GetAllPlugins())
            {
                Assembly asm = plugin.GetType().Assembly;
                Type ThrusterPlugin = typeof(ThrusterPlugin);
                foreach (Type t in asm.GetTypes())
                {
                    if (ThrusterPlugin.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                    {
                        Logger.Info("Loading Thruster from assembly");
                        ThrusterPlugin ThrusterPluginHandler = (ThrusterPlugin)Activator.CreateInstance(t);
                        if (GetThrusterIDFromName(ThrusterPluginHandler.Name) == -1)
                        {
                            ThrusterTypes.Add(ThrusterPluginHandler);
                            Logger.Info($"Added Thruster: '{ThrusterPluginHandler.Name}' with ID '{GetThrusterIDFromName(ThrusterPluginHandler.Name)}'");
                        }
                        else
                        {
                            Logger.Info($"Could not add Thruster from {plugin.Name} with the duplicate name of '{ThrusterPluginHandler.Name}'");
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Finds Thruster type equivilent to given name and returns Subtype ID needed to spawn. Returns -1 if couldn't find Thruster.
        /// </summary>
        /// <param name="ThrusterName"></param>
        /// <returns></returns>
        public int GetThrusterIDFromName(string ThrusterName)
        {
            for (int i = 0; i < ThrusterTypes.Count; i++)
            {
                if (ThrusterTypes[i].Name == ThrusterName)
                {
                    return i + VanillaThrusterMaxType;
                }
            }
            return -1;
        }
        public static PLThruster CreateThruster(int Subtype, int level)
        {
            PLThruster InThruster;
            if (Subtype >= Instance.VanillaThrusterMaxType)
            {
                InThruster = new PLThruster(EThrusterType.MAX, level);
                int subtypeformodded = Subtype - Instance.VanillaThrusterMaxType;
                Logger.Info($"Subtype for modded is {subtypeformodded}");
                if (subtypeformodded <= Instance.ThrusterTypes.Count && subtypeformodded > -1)
                {
                    Logger.Info("Creating Thruster from list info");
                    ThrusterPlugin ThrusterType = Instance.ThrusterTypes[Subtype - Instance.VanillaThrusterMaxType];
                    InThruster.SubType = Subtype;
                    InThruster.Name = ThrusterType.Name;
                    InThruster.Desc = ThrusterType.Description;
                    InThruster.GetType().GetField("m_IconTexture", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(InThruster, ThrusterType.IconTexture);
                    InThruster.GetType().GetField("m_MaxOutput", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(InThruster, ThrusterType.MaxOutput);
                    InThruster.GetType().GetField("m_MaxPowerUsage_Watts", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(InThruster, ThrusterType.MaxPowerUsage_Watts);
                    InThruster.GetType().GetField("m_MarketPrice", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(InThruster, (ObscuredInt)ThrusterType.MarketPrice);
                    InThruster.CargoVisualPrefabID = ThrusterType.CargoVisualID;
                    InThruster.CanBeDroppedOnShipDeath = ThrusterType.CanBeDroppedOnShipDeath;
                    InThruster.Experimental = ThrusterType.Experimental;
                    InThruster.Unstable = ThrusterType.Unstable;
                    InThruster.Contraband = ThrusterType.Contraband;
                    InThruster.GetType().GetMethod("UpdateMaxPowerWatts", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(InThruster, new object[0]);
                }
            }
            else
            {
                InThruster = new PLThruster((EThrusterType)Subtype, level);
            }
            return InThruster;
        }
    }
    //Converts hashes to Thrusters.
    [HarmonyPatch(typeof(PLThruster), "CreateThrusterFromHash")]
    class ThrusterHashFix
    {
        static bool Prefix(int inSubType, int inLevel, ref PLShipComponent __result)
        {
            __result = ThrusterPluginManager.CreateThruster(inSubType, inLevel);
            return false;
        }
    }
    /*[HarmonyPatch(typeof(PLThruster), "Tick")]
    class TickPatch
    {
        static void Postfix(PLThruster __instance)
        {
            int subtypeformodded = __instance.SubType - ThrusterPluginManager.Instance.VanillaThrusterMaxType;
            if (subtypeformodded > -1 && subtypeformodded < ThrusterPluginManager.Instance.ThrusterTypes.Count && __instance.ShipStats != null && __instance.ShipStats.ThrusterTempMax != 0f)
            {
                ThrusterPluginManager.Instance.ThrusterTypes[subtypeformodded].ThrusterPowerCode(__instance);
            }
        }
    }*/
}

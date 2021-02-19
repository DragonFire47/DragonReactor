﻿using CodeStage.AntiCheat.ObscuredTypes;
using HarmonyLib;
using PulsarPluginLoader;
using System;
using System.Collections.Generic;
using System.Reflection;
using Logger = PulsarPluginLoader.Utilities.Logger;

namespace DragonReactor.Hull
{
    class HullPluginManager
    {
        public readonly int VanillaHullMaxType = 0;
        private static HullPluginManager m_instance = null;
        public readonly List<HullPlugin> HullTypes = new List<HullPlugin>();
        public static HullPluginManager Instance
        {
            get
            {
                if (m_instance == null)
                {
                    m_instance = new HullPluginManager();
                }
                return m_instance;
            }
        }

        HullPluginManager()
        {
            VanillaHullMaxType = Enum.GetValues(typeof(EHullType)).Length;
            Logger.Info($"MaxTypeint = {VanillaHullMaxType - 1}");
            foreach (PulsarPlugin plugin in PluginManager.Instance.GetAllPlugins())
            {
                Assembly asm = plugin.GetType().Assembly;
                Type HullPlugin = typeof(HullPlugin);
                foreach (Type t in asm.GetTypes())
                {
                    if (HullPlugin.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                    {
                        Logger.Info("Loading Hull from assembly");
                        HullPlugin HullPluginHandler = (HullPlugin)Activator.CreateInstance(t);
                        if (GetHullIDFromName(HullPluginHandler.Name) == -1)
                        {
                            HullTypes.Add(HullPluginHandler);
                            Logger.Info($"Added Hull: '{HullPluginHandler.Name}' with ID '{GetHullIDFromName(HullPluginHandler.Name)}'");
                        }
                        else
                        {
                            Logger.Info($"Could not add Hull from {plugin.Name} with the duplicate name of '{HullPluginHandler.Name}'");
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Finds Hull type equivilent to given name and returns Subtype ID needed to spawn. Returns -1 if couldn't find Hull.
        /// </summary>
        /// <param name="HullName"></param>
        /// <returns></returns>
        public int GetHullIDFromName(string HullName)
        {
            for (int i = 0; i < HullTypes.Count; i++)
            {
                if (HullTypes[i].Name == HullName)
                {
                    return i + VanillaHullMaxType;
                }
            }
            return -1;
        }
        public static PLHull CreateHull(int Subtype, int level)
        {
            PLHull InHull;
            if (Subtype >= Instance.VanillaHullMaxType)
            {
                InHull = new PLHull(EHullType.E_MAX, level);
                int subtypeformodded = Subtype - Instance.VanillaHullMaxType;
                Logger.Info($"Subtype for modded is {subtypeformodded}");
                if (subtypeformodded <= Instance.HullTypes.Count && subtypeformodded > -1)
                {
                    Logger.Info("Creating Hull from list info");
                    HullPlugin HullType = Instance.HullTypes[Subtype - Instance.VanillaHullMaxType];
                    InHull.SubType = Subtype;
                    InHull.Name = HullType.Name;
                    InHull.Desc = HullType.Description;
                    InHull.GetType().GetField("m_IconTexture", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(InHull, HullType.IconTexture);
                    InHull.Max = HullType.HullMax;
                    InHull.Armor = HullType.Armor;
                    InHull.Defense = HullType.Defense;
                    InHull.GetType().GetField("m_MarketPrice", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(InHull, (ObscuredInt)HullType.MarketPrice);
                    InHull.CargoVisualPrefabID = HullType.CargoVisualID;
                    InHull.CanBeDroppedOnShipDeath = HullType.CanBeDroppedOnShipDeath;
                    InHull.Experimental = HullType.Experimental;
                    InHull.Unstable = HullType.Unstable;
                    InHull.Contraband = HullType.Contraband;
                    InHull.Max *= 2f;
                    InHull.Current = InHull.Max;
                }
            }
            else
            {
                InHull = new PLHull((EHullType)Subtype, level);
            }
            return InHull;
        }
    }
    //Converts hashes to Hulls.
    [HarmonyPatch(typeof(PLHull), "CreateHullFromHash")]
    class HullHashFix
    {
        static bool Prefix(int inSubType, int inLevel, ref PLShipComponent __result)
        {
            __result = HullPluginManager.CreateHull(inSubType, inLevel);
            return false;
        }
    }
    /*[HarmonyPatch(typeof(PLShieldGenerator), "Tick")]
    class TickPatch
    {
        static void Postfix(PLShieldGenerator __instance)
        {
            int subtypeformodded = __instance.SubType - ShieldPluginManager.Instance.VanillaShieldMaxType;
            if (subtypeformodded > -1 && subtypeformodded < ShieldPluginManager.Instance.ShieldTypes.Count && __instance.ShipStats != null && __instance.ShipStats.ShieldTempMax != 0f)
            {
                ShieldPluginManager.Instance.ShieldTypes[subtypeformodded].ShieldPowerCode(__instance);
            }
        }
    }*/
}

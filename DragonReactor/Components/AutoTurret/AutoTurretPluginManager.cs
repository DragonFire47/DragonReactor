﻿using HarmonyLib;
using PulsarPluginLoader;
using System;
using System.Collections.Generic;
using System.Reflection;
using Logger = PulsarPluginLoader.Utilities.Logger;

namespace ContentMod.Components.AutoTurret
{
    public class AutoTurretPluginManager
    {
        public readonly int VanillaAutoTurretMaxType = 0;
        private static AutoTurretPluginManager m_instance = null;
        public readonly List<AutoTurretPlugin> AutoTurretTypes = new List<AutoTurretPlugin>();
        public static AutoTurretPluginManager Instance
        {
            get
            {
                if (m_instance == null)
                {
                    m_instance = new AutoTurretPluginManager();
                }
                return m_instance;
            }
        }

        AutoTurretPluginManager()
        {
            VanillaAutoTurretMaxType = 1;
            Logger.Info($"MaxTypeint = {VanillaAutoTurretMaxType - 1}");
            foreach (PulsarPlugin plugin in PluginManager.Instance.GetAllPlugins())
            {
                Assembly asm = plugin.GetType().Assembly;
                Type AutoTurretPlugin = typeof(AutoTurretPlugin);
                foreach (Type t in asm.GetTypes())
                {
                    if (AutoTurretPlugin.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                    {
                        Logger.Info("Loading AutoTurret from assembly");
                        AutoTurretPlugin AutoTurretPluginHandler = (AutoTurretPlugin)Activator.CreateInstance(t);
                        if (GetAutoTurretIDFromName(AutoTurretPluginHandler.Name) == -1)
                        {
                            AutoTurretTypes.Add(AutoTurretPluginHandler);
                            Logger.Info($"Added AutoTurret: '{AutoTurretPluginHandler.Name}' with ID '{GetAutoTurretIDFromName(AutoTurretPluginHandler.Name)}'");
                        }
                        else
                        {
                            Logger.Info($"Could not add AutoTurret from {plugin.Name} with the duplicate name of '{AutoTurretPluginHandler.Name}'");
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Finds AutoTurret type equivilent to given name and returns Subtype ID needed to spawn. Returns -1 if couldn't find AutoTurret.
        /// </summary>
        /// <param name="AutoTurretName"></param>
        /// <returns></returns>
        public int GetAutoTurretIDFromName(string AutoTurretName)
        {
            for (int i = 0; i < AutoTurretTypes.Count; i++)
            {
                if (AutoTurretTypes[i].Name == AutoTurretName)
                {
                    return i + VanillaAutoTurretMaxType;
                }
            }
            return -1;
        }
        /*public static PLShipComponent CreateAutoTurret(int Subtype, int level)
        {
            PLShipComponent InAutoTurret;
            if (Subtype >= Instance.VanillaAutoTurretMaxType)
            {
                InAutoTurret = new PLAutoTurret(level);
                int subtypeformodded = Subtype - Instance.VanillaAutoTurretMaxType;
                Logger.Info($"Subtype for modded is {subtypeformodded}");
                if (subtypeformodded <= Instance.AutoTurretTypes.Count && subtypeformodded > -1)
                {
                    Logger.Info("Creating AutoTurret from list info");
                }
            }
            else
            {
                InAutoTurret = new PLAutoTurret(level);
            }
            return InAutoTurret;
        }*/
    }
    //Converts hashes to AutoTurrets.
    [HarmonyPatch(typeof(PLAutoTurret), "CreateAutoTurretFromHash")]
    class AutoTurretHashFix
    {
        static bool Prefix(int inSubType, int inLevel, ref PLShipComponent __result)
        {
            int subtypeformodded = inSubType - AutoTurretPluginManager.Instance.VanillaAutoTurretMaxType;
            if (subtypeformodded <= AutoTurretPluginManager.Instance.AutoTurretTypes.Count && subtypeformodded > -1)
            {
                Logger.Info("Creating AutoTurret from list info");
                __result = AutoTurretPluginManager.Instance.AutoTurretTypes[subtypeformodded].PLAutoTurret;
                __result.Level = inLevel;
                return false;
            }
            return true;
        }
    }
    /*[HarmonyPatch(typeof(PLAutoTurret), "LateAddStats")]
    class AutoTurretLateAddStatsPatch
    {
        static void Postfix(PLShipStats inStats, PLAutoTurret __instance)
        {
            int subtypeformodded = __instance.SubType - AutoTurretPluginManager.Instance.VanillaAutoTurretMaxType;
            if (subtypeformodded > -1 && subtypeformodded < AutoTurretPluginManager.Instance.AutoTurretTypes.Count && inStats != null)
            {
                AutoTurretPluginManager.Instance.AutoTurretTypes[subtypeformodded].LateAddStats(inStats);
            }
        }
    }*/
}

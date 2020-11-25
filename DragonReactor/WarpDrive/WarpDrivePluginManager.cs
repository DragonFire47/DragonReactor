﻿using CodeStage.AntiCheat.ObscuredTypes;
using HarmonyLib;
using PulsarPluginLoader;
using PulsarPluginLoader.Utilities;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Logger = PulsarPluginLoader.Utilities.Logger;

namespace DragonReactor
{
    class WarpDrivePluginManager
    {
        public readonly int VanillaWarpDriveMaxType = 0;
        private static WarpDrivePluginManager m_instance = null;
        public readonly List<WarpDrivePlugin> WarpDriveTypes = new List<WarpDrivePlugin>();
        public static WarpDrivePluginManager Instance
        {
            get
            {
                if (m_instance == null)
                {
                    m_instance = new WarpDrivePluginManager();
                }
                return m_instance;
            }
        }

        WarpDrivePluginManager()
        {
            VanillaWarpDriveMaxType = Enum.GetValues(typeof(EWarpDriveType)).Length;
            Logger.Info($"MaxTypeint = {VanillaWarpDriveMaxType - 1}");
            foreach (PulsarPlugin plugin in PluginManager.Instance.GetAllPlugins())
            {
                Assembly asm = plugin.GetType().Assembly;
                Type WarpDrivePlugin = typeof(WarpDrivePlugin);
                foreach (Type t in asm.GetTypes())
                {
                    if (WarpDrivePlugin.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                    {
                        Logger.Info("Loading WarpDrive from assembly");
                        WarpDrivePlugin WarpDrivePluginHandler = (WarpDrivePlugin)Activator.CreateInstance(t);
                        if (GetWarpDriveIDFromName(WarpDrivePluginHandler.Name) == -1)
                        {
                            WarpDriveTypes.Add(WarpDrivePluginHandler);
                            Logger.Info($"Added WarpDrive: '{WarpDrivePluginHandler.Name}'");
                        }
                        else
                        {
                            Logger.Info($"Could not add WarpDrive from {plugin.Name} with the duplicate name of '{WarpDrivePluginHandler.Name}'");
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Finds WarpDrive type equivilent to given name and returns Subtype ID needed to spawn. Returns -1 if couldn't find WarpDrive.
        /// </summary>
        /// <param name="WarpDriveName"></param>
        /// <returns></returns>
        public int GetWarpDriveIDFromName(string WarpDriveName)
        {
            for (int i = 0; i < WarpDriveTypes.Count; i++)
            {
                if (WarpDriveTypes[i].Name == WarpDriveName)
                {
                    return i + VanillaWarpDriveMaxType;
                }
            }
            return -1;
        }
        public static PLWarpDrive CreateWarpDrive(int Subtype, int level)
        {
            PLWarpDrive InWarpDrive;
            if (Subtype >= Instance.VanillaWarpDriveMaxType)
            {
                InWarpDrive = new PLWarpDrive(EWarpDriveType.E_MAX, level);
            }
            else
            {
                InWarpDrive = new PLWarpDrive((EWarpDriveType)Subtype, level);
            }
            int subtypeformodded = Subtype - Instance.VanillaWarpDriveMaxType;
            Logger.Info($"Subtype for modded is {subtypeformodded}");
            if (InWarpDrive.SubType == 15 && subtypeformodded <= Instance.WarpDriveTypes.Count && subtypeformodded > -1)
            {
                Logger.Info("Creating WarpDrive from list info");
                WarpDrivePlugin WarpDriveType = Instance.WarpDriveTypes[Subtype - Instance.VanillaWarpDriveMaxType];
                InWarpDrive.SubType = Subtype;
                InWarpDrive.Name = WarpDriveType.Name;
                InWarpDrive.Desc = WarpDriveType.Description;
                InWarpDrive.ChargeSpeed = WarpDriveType.ChargeSpeed;
                InWarpDrive.WarpRange = WarpDriveType.WarpRange;
                InWarpDrive.EnergySignatureAmt = WarpDriveType.EnergySignature;
                InWarpDrive.NumberOfChargingNodes = WarpDriveType.NumberOfChargesPerFuel;
                InWarpDrive.GetType().GetField("m_MarketPrice", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(InWarpDrive, (ObscuredInt)WarpDriveType.MarketPrice);
                InWarpDrive.CargoVisualPrefabID = WarpDriveType.CargoVisualID;
                InWarpDrive.CanBeDroppedOnShipDeath = WarpDriveType.CanBeDroppedOnShipDeath;
                InWarpDrive.Experimental = WarpDriveType.Experimental;
                InWarpDrive.Unstable = WarpDriveType.Unstable;
                InWarpDrive.Contraband = WarpDriveType.Contraband;
            }
            return InWarpDrive;
        }
    }
    //Converts hashes to WarpDrives.
    [HarmonyPatch(typeof(PLWarpDrive), "CreateWarpDriveFromHash")]
    class WarpDriveHashFix
    {
        static bool Prefix(int inSubType, int inLevel, ref PLShipComponent __result)
        {
            __result = WarpDrivePluginManager.CreateWarpDrive(inSubType, inLevel);
            return false;
        }
    }
    /*[HarmonyPatch(typeof(PLWarpDrive), "Tick")]
    class TickPatch
    {
        static void Postfix(PLWarpDrive __instance)
        {
            int subtypeformodded = __instance.SubType - WarpDrivePluginManager.Instance.VanillaWarpDriveMaxType;
            if (subtypeformodded > -1 && subtypeformodded < WarpDrivePluginManager.Instance.WarpDriveTypes.Count && __instance.ShipStats != null && __instance.ShipStats.WarpDriveTempMax != 0f)
            {
                WarpDrivePluginManager.Instance.WarpDriveTypes[subtypeformodded].WarpDrivePowerCode(__instance);
            }
        }
    }*/
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FrontierDevelopments.General;
using Harmony;
using UnityEngine;
using Verse;

namespace FrontierDevelopments.Shields.Module.RimworldModule
{
    public class ProjectileHandler
    {
        private static readonly bool Enabled = true;
        
        private static readonly FieldInfo OriginField = typeof(Projectile).GetField("origin", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo DestinationField = typeof(Projectile).GetField("destination", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo TicksToImpactField = typeof(Projectile).GetField("ticksToImpact", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo UsedTargetField = typeof(Projectile).GetField("usedTarget", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo IntendedTargetField = typeof(Projectile).GetField("intendedTarget", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly PropertyInfo StartingTicksToImpactProperty = typeof(Projectile).GetProperty("StartingTicksToImpact", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly List<string> BlacklistedDefs = new List<string>();

        static ProjectileHandler()
        {
            if (OriginField == null)
            {
                Enabled = false;
                Log.Error("Frontier Developments Shields :: Projectile handler reflection error on field Projectile.origin");
            }
            if (DestinationField == null)
            {
                Enabled = false;
                Log.Error("Frontier Developments Shields :: Projectile handler reflection error on field Projectile.destination");
            }
            if (TicksToImpactField == null)
            {
                Enabled = false;
                Log.Error("Frontier Developments Shields :: Projectile handler reflection error on field Projectile.ticksToImpact");
            }
            if (UsedTargetField == null)
            {
                Enabled = false;
                Log.Error("Frontier Developments Shields :: Projectile handler reflection error on field Projectile.assignedTarget");
            }
            if (IntendedTargetField == null)
            {
                Enabled = false;
                Log.Error("Frontier Developments Shields :: Projectile handler reflection error on field Projectile.intendedTarget");
            }
            if (StartingTicksToImpactProperty == null)
            {
                Enabled = false;
                Log.Error("Frontier Developments Shields :: Projectile handler reflection error on property Projectile.StartingTicksToImpact");
            }
            
            Log.Message("Frontier Developments Shields :: Projectile handler " + (Enabled ? "enabled" : "disabled due to errors"));
        }
        
        [HarmonyPatch(typeof(Projectile), "Tick")]
        static class Patch_Projectile_Tick
        {
            static bool Prefix(Projectile __instance)
            {
                if (!Enabled || BlacklistedDefs.Contains(__instance.def.defName)) return true;
                
                var projectile = __instance;
                    
                var ticksToImpact = (int)TicksToImpactField.GetValue(projectile);
                var startingTicksToImpact = (int)StartingTicksToImpactProperty.GetValue(projectile, null);

                var origin = Common.ToVector2((Vector3) OriginField.GetValue(projectile));
                var destination = Common.ToVector2((Vector3) DestinationField.GetValue(projectile));

                var position3 = Common.ToVector3(Vector2.Lerp(origin, destination, 1.0f - ticksToImpact / (float)startingTicksToImpact));
                var origin3 = Common.ToVector3(origin);
                var destination3 = Common.ToVector3(destination);
                
                try
                {
                    if (projectile.def.projectile.flyOverhead)
                    {
                        if (ticksToImpact <= 1 && Mod.ShieldManager.Block(projectile.Map, position3, origin, projectile.def.projectile.GetDamageAmount(1f)))
                        {
                            projectile.Destroy();
                            return false;
                        }
                    }
                    else
                    {
                        var ray = new Ray(
                            position3, 
                            Vector3.Lerp(origin3, destination3, 1.0f - (ticksToImpact - 1) / (float) startingTicksToImpact));
                    
                        var impactPoint = Mod.ShieldManager.Block(projectile.Map, origin3, ray, 1, projectile.def.projectile.GetDamageAmount(1f));
                        if (impactPoint != null)
                        {
                            DestinationField.SetValue(projectile, Common.ToVector3(impactPoint.Value, projectile.def.Altitude));
                            TicksToImpactField.SetValue(projectile, 0);
                            UsedTargetField.SetValue(projectile, null);
                            IntendedTargetField.SetValue(projectile, null);
                        }
                    }
                }
                catch (InvalidOperationException) {}
                return true;
            }
        }
    }
}
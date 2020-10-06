using System.Collections.Generic;
using System.Linq;
using AvoidFriendlyFire;
using FrontierDevelopments.General;
using HarmonyLib;
using Verse;

namespace FrontierDevelopments.Shields.AvoidFriendlyFireIntegration.Harmony
{
    public class Harmony_FireCalculations
    {
        private static bool IsCellShielded(IntVec3 origin, int cellIndex, Map map, IEnumerable<IShield> shields)
        {
            return new ShieldQuery(shields)
                .IsActive()
                .Intersects(
                    PositionUtility.ToVector3WithY(origin, 0),
                    PositionUtility.ToVector3WithY(map.cellIndices.IndexToCell(cellIndex), 0))
                .Get()
                .Any();
        }

        [HarmonyPatch(typeof(FireCalculations), "GetShootablePointsBetween")]
        static class Patch_FireCalculations_old
        {
            [HarmonyPostfix]
            static IEnumerable<int> AddShieldCheck(IEnumerable<int> results, IntVec3 origin, IntVec3 target, Map map)
            {
                if (Shields.Mod.Settings.EnableAIVerbFindShotLine)
                {
                    var shields = map.GetComponent<ShieldManager>().Shields.ToList();

                    foreach (var cellIndex in results)
                    {
                        if(!IsCellShielded(origin, cellIndex, map, shields))
                        {
                            yield return cellIndex;
                        }
                        else
                        {
                            yield break;
                        }
                    }
                }
                else
                {
                    foreach (var cellIndex in results)
                    {
                        yield return cellIndex;
                    }
                }
            }
        }
    }
}
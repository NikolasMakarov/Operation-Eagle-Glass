using HarmonyLib;
using Verse;

namespace OperationEagleGlass
{
    public class OperationEagleGlassMod : Mod
    {
        public OperationEagleGlassMod(ModContentPack pack) : base(pack)
        {
            new Harmony("OperationEagleGlassMod").PatchAll();
        }
    }
}
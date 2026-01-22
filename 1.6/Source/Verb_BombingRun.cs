using System.Linq;

namespace OperationEagleGlass
{
    [HotSwappable]
    public class Verb_BombingRun : Verb_LinearRunBase
    {
        protected override CompAbilityEffect_LinearRunBase GetComp() => ability.comps.OfType<CompAbilityEffect_BombingRun>().First();
    }
}

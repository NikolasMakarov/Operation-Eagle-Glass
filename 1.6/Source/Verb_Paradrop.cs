using System.Linq;

namespace OperationEagleGlass
{
    [HotSwappable]
    public class Verb_Paradrop : Verb_LinearRunBase
    {
        protected override CompAbilityEffect_LinearRunBase GetComp() => ability.comps.OfType<CompAbilityEffect_Paradrop>().First();
    }
}

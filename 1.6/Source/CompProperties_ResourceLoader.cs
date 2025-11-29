using System.Collections.Generic;
using Verse;

namespace OperationEagleGlass
{
    public class CompProperties_ResourceLoader : CompProperties
    {
        public List<ThingDefCountClass> maxResources;
        public List<ThingDefCountClass> startingResources;

        public CompProperties_ResourceLoader()
        {
            compClass = typeof(CompResourceLoader);
        }
    }
}

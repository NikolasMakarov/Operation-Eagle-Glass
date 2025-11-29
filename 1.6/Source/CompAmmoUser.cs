using RimWorld;
using Verse;

namespace OperationEagleGlass
{
    public class CompAmmoUser : ThingComp
    {
        private int currentAmmo = 500;
        public CompProperties_AmmoUser Props => (CompProperties_AmmoUser)props;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                Reload();
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref currentAmmo, "currentAmmo", 500);
        }

        public bool ConsumeAmmo(int amount = 1)
        {
            if (currentAmmo >= amount)
            {
                currentAmmo -= amount;
                return true;
            }
            return false;
        }

        public bool HasAmmo(int amount = 1)
        {
            return currentAmmo >= amount;
        }
        
        public int GetAmmoCount()
        {
            return currentAmmo;
        }

        public void Reload()
        {
            currentAmmo = Props.ammoCapacity;
        }
    }

    public class CompProperties_AmmoUser : CompProperties
    {
        public int ammoCapacity = 500;
        public int ammoPerShot = 1;

        public CompProperties_AmmoUser()
        {
            compClass = typeof(CompAmmoUser);
        }
    }
}

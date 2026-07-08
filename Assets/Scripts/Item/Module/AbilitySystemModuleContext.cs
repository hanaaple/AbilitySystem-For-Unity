using Core.AbilitySystem;
using Core.ItemSystem.Module;

namespace Item.Module
{
    public class AbilitySystemModuleContext : ModuleContext
    {
        public readonly AbilitySystemComponent AbilitySystem;

        public AbilitySystemModuleContext(AbilitySystemComponent abilitySystem)
        {
            AbilitySystem = abilitySystem;
        }
    }
}

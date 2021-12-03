using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Nutrition.Components;
using Content.Server.Nutrition.EntitySystems;
using Content.Shared.Administration;
using Content.Shared.Damage;
using Content.Shared.Jittering;
using Content.Shared.MobState.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.StatusEffect;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public class RejuvenateCommand : IConsoleCommand
    {
        public string Command => "rejuvenate";

        public string Description => Loc.GetString("rejuvenate-command-description");

        public string Help => Loc.GetString("rejuvenate-command-help-text");

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            if (args.Length < 1 && player != null) //Try to heal the users mob if applicable
            {
                shell.WriteLine(Loc.GetString("rejuvenate-command-self-heal-message"));
                if (player.AttachedEntity == null)
                {
                    shell.WriteLine(Loc.GetString("rejuvenate-command-no-entity-attached-message"));
                    return;
                }
                PerformRejuvenate(player.AttachedEntity);
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();
            foreach (var arg in args)
            {
                if(!EntityUid.TryParse(arg, out var uid) || !entityManager.TryGetEntity(uid, out var entity))
                {
                    shell.WriteLine(Loc.GetString("shell-could-not-find-entity",("entity", arg)));
                    continue;
                }
                PerformRejuvenate(entity);
            }
        }

        public static void PerformRejuvenate(IEntity target)
        {
            var targetUid = target.Uid;
            var entMan = IoCManager.Resolve<IEntityManager>();
            entMan.GetComponentOrNull<MobStateComponent>(targetUid)?.UpdateState(0);
            entMan.GetComponentOrNull<HungerComponent>(targetUid)?.ResetFood();
            entMan.GetComponentOrNull<ThirstComponent>(targetUid)?.ResetThirst();

            EntitySystem.Get<StatusEffectsSystem>().TryRemoveAllStatusEffects(target.Uid);

            if (IoCManager.Resolve<IEntityManager>().TryGetComponent(target.Uid, out FlammableComponent? flammable))
            {
                EntitySystem.Get<FlammableSystem>().Extinguish(target.Uid, flammable);
            }

            if (IoCManager.Resolve<IEntityManager>().TryGetComponent(target.Uid, out DamageableComponent? damageable))
            {
                EntitySystem.Get<DamageableSystem>().SetAllDamage(damageable, 0);
            }

            if (IoCManager.Resolve<IEntityManager>().TryGetComponent(target.Uid, out CreamPiedComponent? creamPied))
            {
                EntitySystem.Get<CreamPieSystem>().SetCreamPied(target.Uid, creamPied, false);
            }

            if (IoCManager.Resolve<IEntityManager>().HasComponent<JitteringComponent>(target.Uid))
            {
                IoCManager.Resolve<IEntityManager>().RemoveComponent<JitteringComponent>(target.Uid);
            }
        }
    }
}

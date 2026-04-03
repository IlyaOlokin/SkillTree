using Battle;
using TooltipSystem;
using UnityEngine;
using Zenject;

public static class TargetIds
{
    public const string Player = "PlayerTarget";
    public const string Enemies = "EnemiesTarget";
}

public class GameSceneInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        Container.Bind<BattleTickSystem>()
            .FromComponentInHierarchy()
            .AsSingle()
            .NonLazy();

        Container.Bind<PlayerUnit>().FromComponentInHierarchy().AsSingle();
        Container.Bind<UnitLevel>()
            .FromResolveGetter<PlayerUnit>(p => p.UnitLevel)
            .AsSingle();

        Container.Bind<AttackResolver>().FromComponentInHierarchy().AsSingle();
        Container.Bind<SkillTreeUI>().FromComponentInHierarchy().AsSingle();
        Container.Bind<TooltipUI>().FromComponentInHierarchy().AsSingle();

        Container.Bind<ITarget>().WithId(TargetIds.Player).To<PlayerUnit>().FromResolve();
        Container.Bind<ITarget>().WithId(TargetIds.Enemies).To<AttackResolver>().FromResolve();
    }
}

using Battle;
using InventorySystem;
using SaveSystem;
using SkillTree;
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
        Container.Bind<EnemySpawner>().FromComponentInHierarchy().AsSingle();
        Container.Bind<PlayerInventory>().FromComponentInHierarchy().AsSingle();
        Container.Bind<UnitLevel>()
            .FromResolveGetter<PlayerUnit>(p => p.UnitLevel)
            .AsSingle();
        Container.Bind<MainSkillTree>()
            .FromResolveGetter<PlayerUnit>(p => p.SkillTree)
            .AsSingle();

        Container.Bind<AttackResolver>().FromComponentInHierarchy().AsSingle();
        Container.Bind<SkillTreeUI>().FromComponentInHierarchy().AsSingle();
        Container.Bind<SkillTreeSearchController>()
            .FromComponentInHierarchy()
            .AsSingle()
            .NonLazy();
        Container.Bind<TooltipUI>().FromComponentInHierarchy().AsSingle();
        Container.Bind<InventorySocketService>().AsSingle();
        Container.Bind<InventorySelectionState>().AsSingle();
        Container.Bind<GemPlacementService>().AsSingle();
        Container.Bind<InventoryItemUseService>().AsSingle();
        Container.Bind<NodeItemUseService>().AsSingle();
        Container.Bind<SaveFileCodec>().AsSingle();
        Container.Bind<SaveFileStorage>().AsSingle();
        Container.Bind<GemDefinitionCatalog>().AsSingle();
        Container.Bind<ItemDefinitionCatalog>().AsSingle();
        Container.Bind<SaveProfileManager>().AsSingle();
        Container.Bind<CloudSettingsService>().AsSingle();
        Container.Bind<LocalSettingsService>().AsSingle();
        Container.BindInterfacesAndSelfTo<global::SaveSystem.GameSaveCoordinator>().AsSingle().NonLazy();

        Container.Bind<ITarget>().WithId(TargetIds.Player).To<PlayerUnit>().FromResolve();
        Container.Bind<ITarget>().WithId(TargetIds.Enemies).To<AttackResolver>().FromResolve();
    }
}

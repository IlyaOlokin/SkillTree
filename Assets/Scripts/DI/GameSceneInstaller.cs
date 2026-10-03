using Battle;
using CurrencySystem;
using InventorySystem;
using SaveSystem;
using ShopSystem;
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
    [SerializeField] private Tutorials.TutorialCatalog tutorialCatalog;

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
        Container.BindInterfacesAndSelfTo<GemPlacementService>().AsSingle();
        Container.Bind<InventoryItemUseService>().AsSingle();
        Container.Bind<NodeItemUseService>().AsSingle();
        Container.BindInterfacesAndSelfTo<PlayerWallet>().AsSingle();
        Container.Bind<ShopService>().AsSingle();
        Container.Bind<SkillTreeNodeHighlightService>().AsSingle();
        Container.BindInterfacesAndSelfTo<SelectedNodeItemHighlightController>().AsSingle().NonLazy();
        Container.Bind<SaveFileCodec>().AsSingle();
        Container.Bind<SaveFileStorage>().AsSingle();
        Container.Bind<GemDefinitionCatalog>().AsSingle();
        Container.Bind<ItemDefinitionCatalog>().AsSingle();
        Container.Bind<SaveProfileManager>().AsSingle();
        Container.Bind<CloudSettingsService>().AsSingle();
        Container.Bind<LocalSettingsService>().AsSingle();
        Container.Bind<Tutorials.TutorialService>().AsSingle().WithArguments((object)
            (tutorialCatalog != null ? tutorialCatalog.Tutorials : System.Array.Empty<Tutorials.TutorialDefinition>()));
        Container.BindInterfacesAndSelfTo<global::SaveSystem.GameSaveCoordinator>().AsSingle().NonLazy();
        Container.BindInterfacesTo<Tutorials.TutorialEventAdapter>().AsSingle().NonLazy();

        Container.Bind<ITarget>().WithId(TargetIds.Player).To<PlayerUnit>().FromResolve();
        Container.Bind<ITarget>().WithId(TargetIds.Enemies).To<AttackResolver>().FromResolve();
    }
}

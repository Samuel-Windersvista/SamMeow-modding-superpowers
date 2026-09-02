using System;
using System.Linq;
using System.Threading;
using Comfort.Common;
using Cysharp.Threading.Tasks;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using JetBrains.Annotations;
using SamSWAT.HeliCrash.ArysReloaded.Utils;
using SPT.Reflection.Utils;
using ZLinq;

namespace SamSWAT.HeliCrash.ArysReloaded;

[UsedImplicitly]
public class LootContainerFactory(
    Logger logger,
    ConfigurationService configService,
    LocalizationService localizationService
)
{
    /// <summary>
    /// Creates the loot container and loads the bundles for the container and its contents.
    /// </summary>
    /// <param name="container">The container's <see cref="LootableContainer"/> component which allows player interaction.</param>
    /// <param name="containerItem">The container's <see cref="Item"/> which includes all its <see cref="Item"/> contents.</param>
    /// <param name="cancellationToken">Token to cancel the task.</param>
    /// <returns>The NetId of the container.</returns>
    /// <exception cref="NullReferenceException">When the <see cref="container"/> is null.</exception>
    public async UniTask<int> CreateContainer(
        LootableContainer container,
        Item containerItem,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            if (containerItem == null)
            {
                throw new NullReferenceException("Container item is null!");
            }

            if (configService.LoggingEnabled.Value)
            {
                logger.LogInfo("Creating loot container...");
            }

            container.Id = containerItem.Id;
            int netId = container.NetId;

            GameWorld gameWorld = Singleton<GameWorld>.Instance;

            if (gameWorld.World_0 != null)
            {
                gameWorld.RegisterWorldInteractionObject(container);
            }

            LootItem.CreateLootContainer(
                container,
                containerItem,
                localizationService.Localize("containerName"),
                gameWorld
            );

            ResourceKey[] resourceKeys = GetResourceKeys(containerItem);

            await LoadItemBundles(resourceKeys, cancellationToken);

            if (configService.LoggingEnabled.Value)
            {
                logger.LogInfo(
                    $"Created loot container and loaded item bundles! NetId={netId.ToString()}"
                );
            }

            return netId;
        }
        catch (OperationCanceledException ex)
        {
            if (configService.LoggingEnabled.Value)
            {
                logger.LogInfo($"Canceled creating HeliCrash loot crate. {ex.Message}");
            }
            return 0;
        }
        catch (Exception ex)
        {
            logger.LogError(
                $"Failed to create HeliCrash loot crate! {ex.Message}\n{ex.StackTrace}"
            );
            return 0;
        }
    }

    private static async UniTask LoadItemBundles(
        ResourceKey[] resourceKeys,
        CancellationToken cancellationToken
    )
    {
        await Singleton<PoolManagerClass>.Instance.LoadBundlesAndCreatePools(
            PoolManagerClass.PoolsCategory.Raid,
            PoolManagerClass.AssemblyType.Local,
            resourceKeys,
            JobPriorityClass.Immediate,
            ct: cancellationToken
        );
    }

    private static ResourceKey[] GetResourceKeys(Item containerItem)
    {
        ResourceKey[] resourceKeys;
        if (containerItem is ContainerData container)
        {
            resourceKeys = container
                .GetAllItemsFromCollection()
                .AsValueEnumerable()
                .SelectMany(item => item.Template.AllResources)
                .ToArray();
        }
        else
        {
            resourceKeys = containerItem.Template.AllResources.ToArray();
        }

        return resourceKeys;
    }

    public async UniTask<Item> RequestContainerItem(CancellationToken cancellationToken = default)
    {
        try
        {
            if (configService.LoggingEnabled.Value)
            {
                logger.LogInfo("Requesting container item...");
            }

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            AirdropLootResponse lootResponse = (
                await (
                    (ProfileEndpointFactoryAbstractClass)
                        ClientAppUtils.GetClientApp().GetClientBackEndSession()
                )
                    .LoadLootContainerData(null)
                    .AsUniTask()
                    .Timeout(
                        TimeSpan.FromSeconds(10),
                        DelayType.Realtime,
                        taskCancellationTokenSource: cts
                    )
            ).Value;

            cancellationToken.ThrowIfCancellationRequested();

            if (lootResponse?.data == null)
            {
                return await UniTask.FromException<Item>(
                    new NullReferenceException("Heli crash loot response is null")
                );
            }

            Item containerItem = Singleton<ItemFactoryClass>
                .Instance.FlatItemsToTree(lootResponse.data)
                .Items[lootResponse.data[0]._id];

            if (configService.LoggingEnabled.Value)
            {
                logger.LogInfo($"Container item generated! Item={containerItem}");
            }

            return containerItem;
        }
        catch (OperationCanceledException ex)
        {
            if (configService.LoggingEnabled.Value)
            {
                logger.LogInfo($"Canceled creating container item. {ex.Message}");
            }
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(
                $"Request failed trying to create container item! {ex.Message}\n{ex.StackTrace}"
            );
            return null;
        }
    }
}

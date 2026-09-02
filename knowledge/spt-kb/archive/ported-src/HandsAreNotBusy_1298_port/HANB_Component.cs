using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using System;
using System.Linq;
using UnityEngine;
using static EFT.Player;

namespace HandsAreNotBusy;

internal class HANB_Component : MonoBehaviour
{
    private LocalPlayer _player;

    protected void Awake()
    {
        _player = (LocalPlayer)Singleton<GameWorld>.Instance.AllPlayersEverExisted.FirstOrDefault(p => p.IsYourPlayer);

        if (_player == null)
        {
            HANB_Plugin.HANB_Logger.LogError("Unable to find LocalPlayer, destroying module.");
            Destroy(this);
        }

        if (!_player.IsYourPlayer)
        {
            HANB_Plugin.HANB_Logger.LogError("MainPlayer is not your player, destroying module");
            Destroy(this);
        }
    }

    protected void Update()
    {
        if (!Singleton<GameWorld>.Instantiated)
        {
            return;
        }

        if (_player == null)
        {
            return;
        }

        if (HANB_Plugin.ResetKey.Value.IsDown())
        {
            FixHandsController(_player);
        }
    }

    private void FixHandsController(Player player)
    {
        InventoryController inventoryController = player.InventoryController;
        if (inventoryController != null)
        {
            int length = inventoryController.ActiveEvents.Count;
            if (length > 0)
            {
                ItemEventArgs[] args = new ItemEventArgs[length];
                inventoryController.ActiveEvents.CopyTo(args);
                foreach (ItemEventArgs queuedEvent in args)
                {
                    inventoryController.RemoveActiveEvent(queuedEvent);
                }
                HANB_Plugin.HANB_Logger.LogInfo($"Cleared {length} stuck inventory operations.");
            }

            AbstractHandsController handsController = player.HandsController;

            if (handsController is FirearmController currentFirearmController)
            {
                player.MovementContext.OnStateChanged -= currentFirearmController.StateChangedHandler;
                player.Physical.OnSprintStateChangedEvent -= currentFirearmController.SprintStateChangedEvent;
                currentFirearmController.RemoveBallisticCalculator();
            }

            try
            {
                player.SpawnController(CreateFreshHandsController(player), () => { });
            }
            catch (Exception ex)
            {
                HANB_Plugin.HANB_Logger.LogWarning("Stopped exception when spawning controller. InnerException: " + ex.InnerException);
            }

            if (player.LastEquippedWeaponOrKnifeItem != null)
            {
                ItemManipulator.Discard(player.LastEquippedWeaponOrKnifeItem, inventoryController, true);

                player.ProcessStatus = EProcessStatus.None;
                player.TrySetLastEquippedWeapon(true, result => { });
            }
            else
            {
                player.ProcessStatus = EProcessStatus.None;
                player.SetFirstAvailableItem((result) => { });
            }

            player.SetInventoryOpened(false);
            handsController?.Destroy();

            if (handsController != null)
            {
                Destroy(handsController);
            }

            // This fixes a null ref error
            if (player.HandsController is FirearmController firearmController && firearmController.Weapon != null)
            {
                Traverse.Create(player.ProceduralWeaponAnimation)
                    .Field("_firearmAnimationData")
                    .SetValue(firearmController);
            }
        }
        else
        {
            HANB_Plugin.HANB_Logger.LogError("FixHandsController: could not find '_inventoryController' field!");
        }
    }

    private static AbstractHandsController CreateFreshHandsController(Player player)
    {
        switch (player.HandsController)
        {
            case FirearmController firearmController when firearmController.Weapon != null:
                return FirearmController.CreateController<FirearmController>(player, firearmController.Weapon);
            case KnifeController knifeController:
                return KnifeController.CreateController<KnifeController>(player, knifeController.Knife);
            default:
                return EmptyHandsController.CreateController<EmptyHandsController>(player);
        }
    }
}

using System.Linq;
using EFT;
using EFT.Interactive;
using SkillsExtended.Skills.Core;

namespace SkillsExtended.Skills.LockPicking.Actions;

public sealed class LockPickActionHandler
{
    public GamePlayerOwner Owner;
    public WorldInteractiveObject InteractiveObject;
    
    public void PickLockAction(bool unlocked)
    {
        if (unlocked)
        {
            LockPickingHelpers.ApplyLockPickActionXp(InteractiveObject, Owner);
            InteractiveObject.Unlock();
            return;
        }
        
        Owner.DisplayPreloaderUiNotification("You failed to pick the lock...");

        AddFailedAttemptToCounter();
                
        // Apply failure xp
        LockPickingHelpers.ApplyLockPickActionXp(InteractiveObject, Owner, isFailure: true);
        
        RemoveUseFromLockPick();
    }
    
    private void AddFailedAttemptToCounter()
    {
        // Add to the counter
        if (!LockPickingHelpers.DoorAttempts.ContainsKey(InteractiveObject.Id))
        {
            LockPickingHelpers.DoorAttempts.Add(InteractiveObject.Id, 1);
        }
        else
        {
            LockPickingHelpers.DoorAttempts[InteractiveObject.Id]++;
        }

        // Break the lock if more than 3 failed attempts
        if (LockPickingHelpers.DoorAttempts[InteractiveObject.Id] < SkillsPlugin.SkillData.LockPicking.AttemptsBeforeBreak) 
            return;
        
        Owner.DisplayPreloaderUiNotification("You broke the lock...");
        InteractiveObject.KeyId = string.Empty;
        InteractiveObject.Operatable = false;
        InteractiveObject.DoorStateChanged(EDoorState.None);
    }
    
    private void RemoveUseFromLockPick()
    {
        // We are elite level, do not remove a use.
        if (SkillManagerExt.Instance(EPlayerSide.Usec).LockPickingUseBuffElite.Value > 0) return;
        
        // Remove a use from a lock pick in the inventory
        var lockPicks = LockPickingHelpers.GetLockPicksInInventory();
        
        var lockPick = lockPicks.First();

        // 4.1: lock pick 物品通过 ItemComponent 访问（不再有 KeyItemClass 子类模式匹配）
        var pick = lockPick.GetItemComponent<EFT.InventoryLogic.KeyComponent>();
        if (pick is null) return;
        
        pick.NumberOfUsages++;

        // lock pick has no uses left, destroy it
        if (pick.NumberOfUsages >= pick.Template.MaximumNumberOfUsage && pick.Template.MaximumNumberOfUsage > 0)
        {
            // TODO: Is ThrowItem() the correct method?
            Owner.Player.InventoryController.ThrowItem(lockPick);
        }
    }
}
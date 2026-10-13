using System.Text;
using EFT;
using EFT.InventoryLogic;
using EFT.Quests;

namespace SamMeow.DebugToolkit.Modules.Tooltip
{
    /// <summary>
    /// Tooltip 调试信息的数据模型（移植自 DebugTooltip/DebugInfo.cs）。
    /// 每个子类负责把某一类游戏对象渲染成带颜色的调试块，并提供短 ID / 备用 ID 供剪贴板使用。
    /// </summary>
    /// <remarks>
    /// 4.1.6 = Mono：全程托管调用，无 IL2CPP 代理约束——
    /// 类型判定用原生 <c>is</c>、virtual 属性直读、MongoID 隐式转 string 可靠。
    /// 所有渲染结果最终经补丁体的 try/catch fail-open 包裹（见 Patches）。
    /// </remarks>
    internal abstract class DebugInfo
    {
        /// <summary>短 ID（ctrl-c 复制）。</summary>
        public abstract string ToShortString();

        /// <summary>备用 ID（ctrl-alt-c 复制）；无则返回空串。</summary>
        public virtual string ToAltShortString() => string.Empty;

        public virtual string CopyPrompt => "<color=grey>ctrl-c to copy ID</color>";

        public virtual string AltCopyPrompt => null;

        public virtual string FullCopyPrompt => null;
    }

    /// <summary>物品：Item ID / Tpl / Owner 容器名与 ID / Address（grid 坐标+grid ID，或 slot ID+父物品）。</summary>
    internal sealed class ItemDebugInfo : DebugInfo
    {
        private readonly Item _item;

        public ItemDebugInfo(Item item)
        {
            _item = item;
        }

        public override string AltCopyPrompt => "<color=grey>ctrl-alt-c to copy Tpl</color>";

        public override string FullCopyPrompt => "<color=grey>ctrl-shift-c to copy all</color>";

        public override string ToShortString() => _item?.Id;

        public override string ToAltShortString()
        {
            if (_item == null)
            {
                return null;
            }

            // Mono 下 TemplateId 隐式转 string 可靠；优先 StringTemplateId（与 5.0 线一致，便于统一文案）。
            string templateId = _item.StringTemplateId;
            return string.IsNullOrEmpty(templateId) ? _item.TemplateId.ToString() : templateId;
        }

        public override string ToString()
        {
            if (_item == null)
            {
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("Item ID: <color=yellow>{0}</color>\n", _item.Id);
            sb.AppendFormat("Item Tpl: <color=green>{0}</color>\n", ToAltShortString());
            sb.AppendFormat("Owner: <color=#ffa500>{0}</color>\n", _item.Owner?.ContainerName);
            sb.AppendFormat("Owner ID: <color=#ffa500>{0}</color>\n", _item.Owner?.ID);

            // 0.16.9：GridItemAddress / SlotItemAddress / OwnerItself 均为 abstract 基类，
            // 具体运行时实例为其嵌套派生类（Grid.ProtectedGridItemAddress 等）；Mono 的 is 判定可靠。
            ItemAddress address = _item.CurrentAddress;
            if (address is GridItemAddress gridAddress)
            {
                sb.AppendFormat(
                    "Address: <color=#00ffff>({0}, {1}) {2}</color>",
                    gridAddress.LocationInGrid.x,
                    gridAddress.LocationInGrid.y,
                    gridAddress.LocationInGrid.r);
                sb.AppendFormat(" Grid: <color=#00ffff>{0}</color>\n", gridAddress.Grid.ID);
                sb.AppendFormat("Parent: <color=#00ffff>{0}</color>\n", gridAddress.Grid.ParentItem.Name.Localized());
                sb.AppendFormat("Parent ID: <color=#00ffff>{0}</color>\n", gridAddress.Grid.ParentItem.Id);
            }
            else if (address is SlotItemAddress slotAddress)
            {
                sb.AppendFormat("Slot: <color=#00ffff>{0}</color>\n", slotAddress.Slot.ID);
                sb.AppendFormat("Parent: <color=#00ffff>{0}</color>\n", slotAddress.Slot.ParentItem?.Name.Localized());
                sb.AppendFormat("Parent ID: <color=#00ffff>{0}</color>\n", slotAddress.Slot.ParentItem?.Id);
            }
            else if (address != null && address is not OwnerItself)
            {
                sb.AppendFormat("Address: <color=red>{0}</color>\n", address);
            }

            return sb.ToString();
        }
    }

    /// <summary>空槽位：Slot ID / 父物品名与 ID。</summary>
    internal sealed class EmptySlotDebugInfo : DebugInfo
    {
        private readonly Slot _slot;

        public EmptySlotDebugInfo(Slot slot)
        {
            _slot = slot;
        }

        public override string CopyPrompt => "<color=grey>ctrl-c to copy slot</color>";

        public override string AltCopyPrompt => "<color=grey>ctrl-alt-c to copy parent ID</color>";

        public override string FullCopyPrompt => "<color=grey>ctrl-shift-c to copy all</color>";

        public override string ToShortString() => _slot?.ID;

        public override string ToAltShortString() => _slot?.ParentItem?.Id;

        public override string ToString()
        {
            if (_slot == null)
            {
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("Empty slot: <color=#00ffff>{0}</color>\n", _slot.ID);
            sb.AppendFormat("Parent: <color=#00ffff>{0}</color>\n", _slot.ParentItem?.Name.Localized());
            sb.AppendFormat("Parent ID: <color=#00ffff>{0}</color>\n", _slot.ParentItem?.Id);

            return sb.ToString();
        }
    }

    /// <summary>商人：Trader ID。</summary>
    internal sealed class TraderDebugInfo : DebugInfo
    {
        private readonly Profile.TraderInfo _traderInfo;

        public TraderDebugInfo(Profile.TraderInfo traderInfo)
        {
            _traderInfo = traderInfo;
        }

        public override string ToShortString() => _traderInfo?.Id;

        public override string ToString()
        {
            if (_traderInfo == null)
            {
                return string.Empty;
            }

            return string.Format("Trader ID: <color=yellow>{0}</color>\n", _traderInfo.Id);
        }
    }

    /// <summary>任务：Quest ID + 状态。</summary>
    internal sealed class QuestDebugInfo : DebugInfo
    {
        private readonly Quest _quest;

        public QuestDebugInfo(Quest quest)
        {
            _quest = quest;
        }

        public override string FullCopyPrompt => "<color=grey>ctrl-shift-c to copy all</color>";

        public override string ToShortString() => _quest?.Id;

        public override string ToString()
        {
            if (_quest == null)
            {
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("Quest ID: <color=yellow>{0}</color>\n", _quest.Id);
            sb.AppendFormat("Status: <color=#00ffff>{0}</color>\n", _quest.QuestStatus);

            return sb.ToString();
        }
    }

    /// <summary>任务条件：Condition ID。</summary>
    internal sealed class QuestConditionDebugInfo : DebugInfo
    {
        private readonly Condition _condition;

        public QuestConditionDebugInfo(Condition condition)
        {
            _condition = condition;
        }

        public override string ToShortString() => _condition?.id;

        public override string ToString()
        {
            if (_condition == null)
            {
                return string.Empty;
            }

            return string.Format("Condition ID: <color=yellow>{0}</color>\n", _condition.id);
        }
    }
}

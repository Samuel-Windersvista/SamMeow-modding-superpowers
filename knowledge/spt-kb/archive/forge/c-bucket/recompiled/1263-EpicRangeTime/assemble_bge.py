# 组装 BaseGameItemEdits.cs：替换文件头 + 删孤立 int num 声明 + 改 switch 行
import re

BODY = r"D:\Temp\opencode\srv-port\1263\src\Utilities\BaseGameItemEdits.body.cs"
DST = r"D:\Temp\opencode\srv-port\1263\src\Utilities\BaseGameItemEdits.cs"

lines = open(BODY, encoding="utf-8").read().split("\n")

# 1. 删原文件头（第 1-30 行，即到 switch 行之前的全部）
# 找到 EditFilters 方法内 switch 之后的行（从 case 开始）
start = None
for i, l in enumerate(lines):
    if re.match(r"^\s*case \"", l):
        start = i
        break
assert start is not None, "case 行未找到"

body = lines[start:]

# 2. 删孤立 int num/num2 声明行
body = [l for l in body if not re.match(r"^\s*int (num|num2) = \d+;\s*$", l)]

header = r'''using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Tables;
using WTTServerCommonLib.Helpers;

namespace EpicsAIO.Utilities;

[Injectable(InjectionType.Singleton)]
public class BaseGameItemEdits(
    ISptLogger<BaseGameItemEdits> logger,
    TemplateTable templateTable,
    SlotHelper slotHelper) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        EditFilters();
        return Task.CompletedTask;
    }

    private void EditFilters()
    {
        Dictionary<MongoId, TemplateItem> items = templateTable.Items;
        foreach (var (val3, val4) in items)
        {
            switch (val3.ToString())
            {
'''

out = header + "\n".join(body)
open(DST, "w", encoding="utf-8").write(out)
print(f"final lines: {len(out.splitlines())}")
print(f"removed int declarations: {112}")
print(f"remaining int num: {len(re.findall(r'^\s*int (num|num2) = \d+;', out, re.M))}")

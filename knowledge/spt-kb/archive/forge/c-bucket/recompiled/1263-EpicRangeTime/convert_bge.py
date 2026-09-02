# 转换 BaseGameItemEdits 反编译代码：删 IL 注释 + MongoId.op_Implicit → new MongoId + CollectionsMarshal 模式 → List 字面量
import re

SRC = r"D:\Temp\opencode\srv-port\1263\src\Utilities.BaseGameItemEdits.cs"
DST = r"D:\Temp\opencode\srv-port\1263\src\Utilities\BaseGameItemEdits.body.cs"

text = open(SRC, encoding="utf-8").read()
lines = text.split("\n")

# 1. 删除 //IL_ 注释行
lines = [l for l in lines if not re.match(r"^\s*//IL_", l)]

# 2. MongoId.op_Implicit("x") -> new MongoId("x")
lines = [re.sub(r'MongoId\.op_Implicit\("([0-9a-f]+)"\)', r'new MongoId("\1")', l) for l in lines]

# 3. CollectionsMarshal 块 -> List<MongoId> 字面量
out = []
i = 0
n = len(lines)
converted = 0
while i < n:
    line = lines[i]
    m = re.match(r'^(\s*)List<MongoId> (list\w*) = new List<MongoId>\([^)]*\);\s*$', line)
    if not m:
        out.append(line)
        i += 1
        continue
    indent, lname = m.group(1), m.group(2)
    j = i + 1
    # 跳过 SetCount 行（可能一行或多行）
    while j < n and not re.match(r'^\s*(?:Span<MongoId> )?span = CollectionsMarshal\.AsSpan\((' + re.escape(lname) + r'|' + re.escape(lname) + r')\);\s*$', lines[j]):
        j += 1
    if j >= n or not re.match(r'^\s*(?:Span<MongoId> )?span = CollectionsMarshal\.AsSpan\([^)]*\);\s*$', lines[j]):
        out.append(line)
        i += 1
        continue
    j += 1  # 跳过 AsSpan 行
    # 收集 span 赋值（允许中间有 X = N; 重置行）
    ids = []
    while j < n:
        if re.match(r'^\s*span\[(?:num|num2)\] = new MongoId\("([0-9a-f]+)"\);\s*$', lines[j]):
            ids.append(re.match(r'^\s*span\[(?:num|num2)\] = new MongoId\("([0-9a-f]+)"\);\s*$', lines[j]).group(1))
            j += 1
            # 可选 ++ 行
            if j < n and re.match(r'^\s*(?:num|num2)\+\+;\s*$', lines[j]):
                j += 1
            continue
        # 可选重置行（int X = N; 或 X = N;），仅当后面还跟 span 赋值时跳过
        if j < n and re.match(r'^\s*(?:int )?(?:num|num2) = \d+;\s*$', lines[j]) and j + 1 < n and re.match(r'^\s*span\[(?:num|num2)\] = new MongoId\("', lines[j + 1]):
            j += 1
            continue
        break
    # 现在 j 应指向 ModifySlotFilters 调用
    m_call = None
    if j < n:
        m_call = re.match(r'^(\s*)ModifySlotFilters\(val4, (\d+), (\d+), ' + re.escape(lname) + r'(, isCartridge: true)?\);\s*$', lines[j])
    if not m_call or not ids:
        out.append(line)
        i += 1
        continue
    call_indent = m_call.group(1)
    a, b = m_call.group(2), m_call.group(3)
    extra = m_call.group(4) or ""
    idstr = ", ".join(f'new MongoId("{x}")' for x in ids)
    out.append(f'{indent}ModifySlotFilters(val4, {a}, {b}, new List<MongoId> {{ {idstr} }}{extra});')
    converted += 1
    i = j + 1

result = "\n".join(out)
open(DST, "w", encoding="utf-8").write(result)
print(f"converted blocks: {converted}")
print(f"remaining CollectionsMarshal: {len(re.findall('CollectionsMarshal', result))}")
print(f"remaining op_Implicit: {len(re.findall('op_Implicit', result))}")
print(f"remaining IL_ comments: {len(re.findall(r'//IL_', result))}")
print(f"total lines: {len(out)}")

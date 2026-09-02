import { afterAll, beforeAll, describe, expect, it } from "vitest";
import { existsSync, mkdirSync, mkdtempSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";

import {
  listClientMods,
  listServerMods,
  readClientModMetadata,
  readConfigKeys,
  readModMetadata,
  readPackageJson,
  readServerModMetadata,
  scanModFiles,
  toPosix,
} from "../../src/mod-reader.js";

let root: string;

beforeAll(() => {
  root = mkdtempSync(join(tmpdir(), "spt-mcp-modreader-"));
});

afterAll(() => {
  rmSync(root, { recursive: true, force: true });
});

function write(dir: string, rel: string, content: string | Buffer) {
  const full = join(dir, rel);
  mkdirSync(join(dir, rel.split("/").slice(0, -1).join("/")), { recursive: true });
  writeFileSync(full, content);
}

// -----------------------------------------------------------------------------
// package.json 解析
// -----------------------------------------------------------------------------

describe("mod-reader: package.json 解析", () => {
  it("解析合法 package.json 并提取字段", () => {
    const modDir = join(root, "good-mod");
    write(
      modDir,
      "package.json",
      JSON.stringify({
        name: "Test Mod",
        version: "1.2.3",
        guid: "com.example.test",
        dependencies: { "com.other.dep": "1.0.0" },
        typePriority: 100,
        sptVersion: "~4.1.0",
      }),
    );
    const pkg = readPackageJson(modDir);
    expect(pkg).not.toBeNull();
    expect(pkg?.name).toBe("Test Mod");

    const meta = readServerModMetadata(modDir);
    expect(meta).not.toBeNull();
    expect(meta?.type).toBe("server");
    expect(meta?.name).toBe("Test Mod");
    expect(meta?.version).toBe("1.2.3");
    expect(meta?.guid).toBe("com.example.test");
    expect(meta?.dependencies).toEqual(["com.other.dep"]);
    expect(meta?.typePriority).toBe(100);
    expect(meta?.sptVersion).toBe("~4.1.0");
  });

  it("package.json 缺失 -> 返回 null", () => {
    expect(readPackageJson(join(root, "no-such-dir"))).toBeNull();
    expect(readServerModMetadata(join(root, "no-such-dir"))).toBeNull();
  });

  it("package.json 非法 JSON -> 返回 null", () => {
    const modDir = join(root, "bad-json");
    write(modDir, "package.json", "{ not json !!");
    expect(readPackageJson(modDir)).toBeNull();
    expect(readServerModMetadata(modDir)).toBeNull();
  });

  it("name 缺失时回退为目录名", () => {
    const modDir = join(root, "fallback-name");
    write(modDir, "package.json", JSON.stringify({ version: "1.0.0" }));
    const meta = readServerModMetadata(modDir);
    expect(meta?.name).toBe("fallback-name");
  });

  it("dependencies 为字符串数组时原样返回", () => {
    const modDir = join(root, "arr-deps");
    write(modDir, "package.json", JSON.stringify({ dependencies: ["a", "b"] }));
    expect(readServerModMetadata(modDir)?.dependencies).toEqual(["a", "b"]);
  });

  it("compatibleVersion 作为 sptVersion 的兜底", () => {
    const modDir = join(root, "compat-mod");
    write(modDir, "package.json", JSON.stringify({ compatibleVersion: ">=4.0" }));
    const meta = readServerModMetadata(modDir);
    expect(meta?.compatibleVersion).toBe(">=4.0");
    expect(meta?.sptVersion).toBeUndefined();
  });
});

// -----------------------------------------------------------------------------
// 目录扫描
// -----------------------------------------------------------------------------

describe("mod-reader: 目录扫描", () => {
  it("listServerMods 只返回含 package.json 的子目录", () => {
    const base = join(root, "server-scan");
    write(base, "mod-a/package.json", JSON.stringify({ name: "Mod A", guid: "com.a" }));
    write(base, "mod-b/package.json", JSON.stringify({ name: "Mod B", guid: "com.b" }));
    write(base, "not-a-mod/readme.txt", "hello");
    const mods = listServerMods(base);
    expect(mods.map((m) => m.name).sort()).toEqual(["Mod A", "Mod B"]);
    expect(mods[0].type).toBe("server");
  });

  it("listServerMods 对不存在的目录返回空数组", () => {
    expect(listServerMods(join(root, "missing"))).toEqual([]);
  });

  it("listClientMods 递归找 .dll（一个 DLL 一个条目），忽略大小写", () => {
    const base = join(root, "client-scan");
    write(base, "plugin-a.dll", "");
    write(base, "sub/plugin-b.dll", "");
    write(base, "notes.txt", "x");
    const mods = listClientMods(base);
    expect(mods.map((m) => m.name).sort()).toEqual(["plugin-a", "plugin-b"]);
    expect(mods.every((m) => m.type === "client")).toBe(true);
  });

  it("readClientModMetadata 从 DLL 文件名派生 name，非 DLL 返回 null", () => {
    const dll = join(root, "client-scan", "plugin-a.dll");
    const meta = readClientModMetadata(dll);
    expect(meta).not.toBeNull();
    expect(meta?.name).toBe("plugin-a");
    expect(readClientModMetadata(join(root, "client-scan", "notes.txt"))).toBeNull();
    expect(readClientModMetadata(join(root, "client-scan", "missing.dll"))).toBeNull();
  });

  it("readModMetadata 支持目录输入（client 取第一个 DLL）与 server 分发", () => {
    const base = join(root, "client-dir");
    write(base, "zz.dll", "");
    write(base, "aa.dll", "");
    const meta = readModMetadata(base, "client");
    expect(meta?.type).toBe("client");
    expect(meta?.name).toBe("aa"); // 按文件名排序取第一个
    expect(readModMetadata(join(root, "server-scan", "mod-a"), "server")?.name).toBe("Mod A");
    expect(readModMetadata(join(root, "server-scan", "mod-a"), "client")).toBeNull();
  });
});

// -----------------------------------------------------------------------------
// 文件扫描
// -----------------------------------------------------------------------------

describe("mod-reader: 文件扫描", () => {
  it("scanModFiles 递归列出相对路径（正斜杠）与大小", () => {
    const modDir = join(root, "file-scan");
    write(modDir, "package.json", "{}");
    write(modDir, "db/config.json", '{"a":1}');
    write(modDir, "readme.md", "hello world");
    const files = scanModFiles(modDir);
    const rels = files.map((f) => f.relativePath);
    expect(rels).toContain("package.json");
    expect(rels).toContain("db/config.json");
    expect(rels).toContain("readme.md");
    expect(rels.every((r) => !r.includes("\\"))).toBe(true);
    const readme = files.find((f) => f.relativePath === "readme.md");
    expect(readme?.sizeBytes).toBe(11);
    expect(files.every((f) => f.sizeBytes >= 0)).toBe(true);
  });

  it("scanModFiles 对不存在的目录返回空数组", () => {
    expect(scanModFiles(join(root, "missing"))).toEqual([]);
  });

  it("toPosix 统一为 / 分隔", () => {
    expect(toPosix("a\\b\\c")).toBe("a/b/c");
    expect(toPosix("a/b/c")).toBe("a/b/c");
  });
});

// -----------------------------------------------------------------------------
// 配置文件 key 提取
// -----------------------------------------------------------------------------

describe("mod-reader: 配置文件 key 提取", () => {
  it("readConfigKeys 收集 JSON 顶层 key，跳过 package.json 与非法 JSON", () => {
    const modDir = join(root, "config-scan");
    write(modDir, "package.json", JSON.stringify({ name: "x" }));
    write(modDir, "config/config.json", JSON.stringify({ enabled: true, price: 5 }));
    write(modDir, "config/broken.json", "{{{");
    const keys = readConfigKeys(modDir);
    expect(keys).toHaveLength(1);
    expect(keys[0].filename).toBe("config/config.json");
    expect(keys[0].keys.sort()).toEqual(["enabled", "price"]);
  });

  it("readConfigKeys 对不存在目录返回空数组", () => {
    expect(readConfigKeys(join(root, "missing"))).toEqual([]);
  });
});

// -----------------------------------------------------------------------------
// 辅助
// -----------------------------------------------------------------------------

describe("mod-reader: 辅助函数", () => {
  it("存在性检查正确", () => {
    write(root, "dir-check/file.txt", "x");
    expect(existsSync(join(root, "dir-check"))).toBe(true);
  });
});

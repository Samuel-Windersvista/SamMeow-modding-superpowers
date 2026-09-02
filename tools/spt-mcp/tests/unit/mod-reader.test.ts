import { afterAll, beforeAll, describe, expect, it } from "vitest";
import { existsSync, mkdirSync, mkdtempSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";

import {
  findAnyDll,
  findTopLevelDll,
  listClientMods,
  listNestedServerMods,
  listServerMods,
  readClientModMetadata,
  readConfigKeys,
  readDllMetadata,
  readModMetadata,
  readPackageJson,
  readServerModMetadataFromPkg,
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
// package.json parsing
// -----------------------------------------------------------------------------

describe("mod-reader: package.json parsing", () => {
  it("parses valid package.json and extracts fields", () => {
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

    const meta = readServerModMetadataFromPkg(modDir);
    expect(meta).not.toBeNull();
    expect(meta?.type).toBe("server");
    expect(meta?.name).toBe("Test Mod");
    expect(meta?.version).toBe("1.2.3");
    expect(meta?.guid).toBe("com.example.test");
    expect(meta?.dependencies).toEqual(["com.other.dep"]);
    expect(meta?.typePriority).toBe(100);
    expect(meta?.sptVersion).toBe("~4.1.0");
  });

  it("missing package.json -> null", () => {
    expect(readPackageJson(join(root, "no-such-dir"))).toBeNull();
    expect(readServerModMetadataFromPkg(join(root, "no-such-dir"))).toBeNull();
  });

  it("invalid package.json -> null", () => {
    const modDir = join(root, "bad-json");
    write(modDir, "package.json", "{ not json !!");
    expect(readPackageJson(modDir)).toBeNull();
    expect(readServerModMetadataFromPkg(modDir)).toBeNull();
  });

  it("falls back to dir name when name missing", () => {
    const modDir = join(root, "fallback-name");
    write(modDir, "package.json", JSON.stringify({ version: "1.0.0" }));
    const meta = readServerModMetadataFromPkg(modDir);
    expect(meta?.name).toBe("fallback-name");
  });

  it("string-array dependencies returned as-is", () => {
    const modDir = join(root, "arr-deps");
    write(modDir, "package.json", JSON.stringify({ dependencies: ["a", "b"] }));
    expect(readServerModMetadataFromPkg(modDir)?.dependencies).toEqual(["a", "b"]);
  });

  it("compatibleVersion as fallback for sptVersion", () => {
    const modDir = join(root, "compat-mod");
    write(modDir, "package.json", JSON.stringify({ compatibleVersion: ">=4.0" }));
    const meta = readServerModMetadataFromPkg(modDir);
    expect(meta?.compatibleVersion).toBe(">=4.0");
    expect(meta?.sptVersion).toBeUndefined();
  });
});

// -----------------------------------------------------------------------------
// directory scanning
// -----------------------------------------------------------------------------

describe("mod-reader: directory scanning", () => {
  it("listServerMods returns dirs with package.json (legacy structure)", () => {
    const base = join(root, "server-scan");
    write(base, "mod-a/package.json", JSON.stringify({ name: "Mod A", guid: "com.a" }));
    write(base, "mod-b/package.json", JSON.stringify({ name: "Mod B", guid: "com.b" }));
    write(base, "not-a-mod/readme.txt", "hello");
    const mods = listServerMods(base);
    expect(mods.map((m) => m.name).sort()).toEqual(["Mod A", "Mod B"]);
    expect(mods[0].type).toBe("server");
  });

  it("listServerMods on missing dir returns empty", () => {
    expect(listServerMods(join(root, "missing"))).toEqual([]);
  });

  it("findTopLevelDll only finds top-level dll, ignores subdirs", () => {
    const modDir = join(root, "dll-structure");
    write(modDir, "top.dll", "");
    write(modDir, "sub/nested.dll", "");
    expect(findTopLevelDll(modDir)).toBe(join(modDir, "top.dll"));
    expect(findTopLevelDll(join(root, "missing"))).toBeNull();
  });

  it("listServerMods recognizes 4.1 dirs with top-level DLL; falls back to dir name when helper can't read", () => {
    const base = join(root, "dll-server-scan");
    write(base, "mod-a/SomeMod.dll", "fake dll bytes");
    write(base, "mod-b/package.json", JSON.stringify({ name: "Mod B", guid: "com.b" }));
    const mods = listServerMods(base);
    // mod-a has DLL but helper cannot read real metadata -> dir-name fallback
    expect(mods.map((m) => m.name).sort()).toEqual(["Mod B", "mod-a"]);
    const modA = mods.find((m) => m.name === "mod-a");
    expect(modA?.type).toBe("server");
    expect(modA?.path).toBe(join(base, "mod-a"));
  });

  it("readDllMetadata returns error marker when helper missing (no throw)", () => {
    const metas = readDllMetadata([join(root, "missing.dll")]);
    expect(metas).toHaveLength(1);
    expect(metas[0].ok).toBe(false);
    expect(metas[0].error).toBeTruthy();
  });

  it("listNestedServerMods finds 3.11 nested user/mods/<name>/package.json", () => {
    const modDir = join(root, "nested-mod");
    write(modDir, "BepInEx/plugins/x.dll", "");
    write(modDir, "user/mods/foo/package.json", JSON.stringify({ name: "Foo Server", guid: "com.foo" }));
    write(modDir, "user/mods/bar/package.json", JSON.stringify({ name: "Bar Server", guid: "com.bar" }));
    const nested = listNestedServerMods(modDir);
    expect(nested.map((m) => m.name).sort()).toEqual(["Bar Server", "Foo Server"]);
    expect(nested.every((m) => m.type === "server")).toBe(true);
  });

  it("listNestedServerMods on mod without user/mods returns empty", () => {
    expect(listNestedServerMods(join(root, "missing"))).toEqual([]);
  });

  it("findAnyDll finds dll in subdirs (3.11 client mod)", () => {
    const modDir = join(root, "nested-dll");
    write(modDir, "BepInEx/plugins/plugin.dll", "");
    expect(findAnyDll(modDir)).toBe(join(modDir, "BepInEx/plugins/plugin.dll"));
  });

  it("readModMetadata server mode recognizes 3.11 nested server mod", () => {
    const modDir = join(root, "nested-server-mod");
    write(modDir, "user/mods/foo/package.json", JSON.stringify({ name: "Foo", guid: "com.foo", sptVersion: "~3.11" }));
    const meta = readModMetadata(modDir, "server");
    expect(meta?.type).toBe("server");
    expect(meta?.name).toBe("Foo");
    expect(meta?.guid).toBe("com.foo");
    expect(meta?.sptVersion).toBe("~3.11");
  });

  it("listClientMods recursively finds .dll (one per dll), case-insensitive", () => {
    const base = join(root, "client-scan");
    write(base, "plugin-a.dll", "");
    write(base, "sub/plugin-b.dll", "");
    write(base, "notes.txt", "x");
    const mods = listClientMods(base);
    expect(mods.map((m) => m.name).sort()).toEqual(["plugin-a", "plugin-b"]);
    expect(mods.every((m) => m.type === "client")).toBe(true);
  });

  it("readClientModMetadata derives name from dll filename; non-dll -> null", () => {
    const dll = join(root, "client-scan", "plugin-a.dll");
    const meta = readClientModMetadata(dll);
    expect(meta).not.toBeNull();
    expect(meta?.name).toBe("plugin-a");
    expect(readClientModMetadata(join(root, "client-scan", "notes.txt"))).toBeNull();
    expect(readClientModMetadata(join(root, "client-scan", "missing.dll"))).toBeNull();
  });

  it("readModMetadata supports dir input (client takes first dll) and server dispatch", () => {
    const base = join(root, "client-dir");
    write(base, "zz.dll", "");
    write(base, "aa.dll", "");
    const meta = readModMetadata(base, "client");
    expect(meta?.type).toBe("client");
    expect(meta?.name).toBe("aa"); // sorted by filename, first
    expect(readModMetadata(join(root, "server-scan", "mod-a"), "server")?.name).toBe("Mod A");
    expect(readModMetadata(join(root, "server-scan", "mod-a"), "client")).toBeNull();
  });
});

// -----------------------------------------------------------------------------
// file scanning
// -----------------------------------------------------------------------------

describe("mod-reader: file scanning", () => {
  it("scanModFiles recursively lists relative paths (posix) and sizes", () => {
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

  it("scanModFiles on missing dir returns empty", () => {
    expect(scanModFiles(join(root, "missing"))).toEqual([]);
  });

  it("toPosix normalizes separators", () => {
    expect(toPosix("a\\b\\c")).toBe("a/b/c");
    expect(toPosix("a/b/c")).toBe("a/b/c");
  });
});

// -----------------------------------------------------------------------------
// config key extraction
// -----------------------------------------------------------------------------

describe("mod-reader: config key extraction", () => {
  it("readConfigKeys collects top-level keys, skips package.json and invalid json", () => {
    const modDir = join(root, "config-scan");
    write(modDir, "package.json", JSON.stringify({ name: "x" }));
    write(modDir, "config/config.json", JSON.stringify({ enabled: true, price: 5 }));
    write(modDir, "config/broken.json", "{{{");
    const keys = readConfigKeys(modDir);
    expect(keys).toHaveLength(1);
    expect(keys[0].filename).toBe("config/config.json");
    expect(keys[0].keys.sort()).toEqual(["enabled", "price"]);
  });

  it("readConfigKeys on missing dir returns empty", () => {
    expect(readConfigKeys(join(root, "missing"))).toEqual([]);
  });
});

// -----------------------------------------------------------------------------
// helpers
// -----------------------------------------------------------------------------

describe("mod-reader: helpers", () => {
  it("existence checks work", () => {
    write(root, "dir-check/file.txt", "x");
    expect(existsSync(join(root, "dir-check"))).toBe(true);
  });
});

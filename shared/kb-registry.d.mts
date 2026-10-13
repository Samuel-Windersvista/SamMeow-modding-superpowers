// =============================================================================
// kb-registry.d.mts — shared/kb-registry.mjs 的类型声明
//
// 与 .mjs 同目录同名（仅扩展名不同），供 TS（NodeNext）解析导入类型；
// 声明文件不参与 tsc 输出，故不触发 rootDir 限制。
// =============================================================================

export type ResourceSource = "env" | "default";

export type ResourceStatus =
  | { path: string; source: ResourceSource; ok: true }
  | { path: string; source: ResourceSource; ok: false; reason: string };

export interface KbScanSpec {
  dirs?: string[];
  exclude?: string[];
}

export interface KbSectionSpec {
  dir: string;
  topic: string;
  source?: string;
}

export interface KbExtractSpec {
  keywords?: string[];
  keywordsMax?: number;
  summary?: string;
  summaryMaxChars?: number;
}

/** 已解析的注册表条目（路径已展开、状态已校验） */
export interface ResolvedKbEntry {
  id: string;
  title: string;
  authority: string;
  enabled: boolean;
  envKey: string | null;
  scan: KbScanSpec;
  sections: KbSectionSpec[];
  versionDefaults: string[];
  versionPatterns: string[];
  domainDefault: string;
  extract: KbExtractSpec;
  indexPolicy: string;
  builder: string | null;
  root: ResourceStatus;
  index: ResourceStatus;
}

export const REGISTRY_SCHEMA_VERSION: number;
export const DEFAULT_REGISTRY_REL: string;

export function isAbsolutePath(p: string): boolean;
export function resolveFromRoot(pluginRoot: string, p: string): string;
export function globToRegExp(pattern: string): RegExp;
export function matchesExclude(relPath: string, patterns: string[]): boolean;
export function loadRegistry(
  registryPath: string,
): { ok: true; registry: { kbs: unknown[]; [key: string]: unknown } } | { ok: false; reason: string };
export function resolveKbEntry(
  pluginRoot: string,
  entry: unknown,
  env?: NodeJS.ProcessEnv,
): ResolvedKbEntry;
export function resolveKbs(
  pluginRoot: string,
  registryPath: string,
  env?: NodeJS.ProcessEnv,
): { ok: true; kbs: ResolvedKbEntry[]; registryPath: string } | { ok: false; reason: string; registryPath: string };
export function listKbMarkdown(kbRoot: string, scan: KbScanSpec): string[];
export function newestSourceMtime(kbRoot: string, scan: KbScanSpec): number | null;
export function fileMtime(path: string): number | null;
export function defaultRegistryPath(pluginRoot: string): string;

export const CONTENT_SCAN_MAX_FILE_BYTES: number;

export interface ContentHit {
  path: string;
  hits: number;
  snippet: string;
}

export function searchKbContent(
  kbRoot: string,
  scan: KbScanSpec,
  needle: string,
  options?: { maxFileBytes?: number },
): ContentHit[];

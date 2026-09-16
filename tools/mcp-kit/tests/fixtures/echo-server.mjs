// =============================================================================
// echo-server.mjs — bootstrap.test.ts 的 stdio fixture
//
// 用 runStdioServer 起一个最小 server（单工具 echo），供测试做
// initialize / tools/list / tools/call 往返。注意：fixture 导入的是构建产物
// dist/index.js（Node 不能直接跑 TS），因此 package.json 里 pretest 会先 build。
// =============================================================================

import { jsonResult, runMain, runStdioServer } from "../../dist/index.js";

// S1 引导时序护栏：onBeforeConnect 必须先于任何工具面调用执行。
// listTools 在标志未置位时抛错 —— onBeforeConnect 在 connect 之前运行，
// 因此 tools/list 往返成功即确定性证明「钩子先于工具面调用」。
let beforeConnectRan = false;

async function main() {
  await runStdioServer({
    name: "echo-fixture",
    version: "0.0.0",
    onBeforeConnect: () => {
      beforeConnectRan = true;
    },
    listTools: () => {
      if (!beforeConnectRan) {
        throw new Error("引导时序违规：listTools 在 onBeforeConnect 之前被调用");
      }
      return [
        {
          name: "echo",
          description: "回显 text 参数",
          inputSchema: {
            type: "object",
            properties: { text: { type: "string" } },
            additionalProperties: true,
          },
        },
      ];
    },
    callTool: (name, args) => {
      if (name !== "echo") {
        return jsonResult({ ok: false, message: `未知工具：${name}` }, true);
      }
      return jsonResult({ echo: args.text ?? null });
    },
  });
}

runMain(import.meta.url, main, "echo-fixture");

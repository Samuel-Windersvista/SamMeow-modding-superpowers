import { defineConfig } from "vitest/config";

export default defineConfig({
  test: {
    include: ["tests/**/*.test.ts"],
    environment: "node",
    // bootstrap.test.ts 需要 spawn 子进程做 stdio 往返，放宽超时
    testTimeout: 20_000,
  },
});

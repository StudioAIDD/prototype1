import { createServer } from "node:http";
import { CopilotRuntime } from "@copilotkit/runtime/v2";
import { createCopilotNodeListener } from "@copilotkit/runtime/v2/node";
import { HttpAgent } from "@ag-ui/client";

// Points at the Microsoft Agent Framework AG-UI server (agent_framework_proto1).
const agentUrl = process.env.AGENT_URL ?? "http://localhost:8888";

const runtime = new CopilotRuntime({
  agents: {
    default: new HttpAgent({ url: agentUrl }),
  },
});

const PORT = 8200;

const listener = createCopilotNodeListener({
  runtime,
  basePath: "/api/copilotkit",
  cors: true, // the Vite app and this runtime run on different origins
});

createServer(listener).listen(PORT, () => {
  console.log(`Copilot Runtime ready at http://localhost:${PORT}/api/copilotkit`);
  console.log(`Forwarding to Agent Framework AG-UI server at ${agentUrl}`);
});

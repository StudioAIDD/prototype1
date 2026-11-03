import { CopilotKitProvider, CopilotChat } from "@copilotkit/react-core/v2";

// Absolute URL to the standalone runtime server (server.ts), which forwards
// to the Microsoft Agent Framework AG-UI server (agent_framework_proto1).
const runtimeUrl =
  import.meta.env.VITE_COPILOT_RUNTIME_URL ?? "http://localhost:8200/api/copilotkit";

export default function App() {
  return (
    <CopilotKitProvider runtimeUrl={runtimeUrl}>
      <div style={{ height: "100vh" }}>
        <CopilotChat />
      </div>
    </CopilotKitProvider>
  );
}

using System;
using System.Threading.Tasks;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Services;
using UnityEditor;
using UnityEngine;

namespace SillyBoy.Editor.MCPTools
{
    [InitializeOnLoad]
    internal static class UnityMcpAutoStart
    {
        private const string SessionInitializedKey = "SillyBoy.UnityMcpAutoStart.SessionInitialized";
        private const string AutoStartOnLoadKey = "MCPForUnity.AutoStartOnLoad";

        static UnityMcpAutoStart()
        {
            EditorPrefs.SetBool(AutoStartOnLoadKey, true);

            if (SessionState.GetBool(SessionInitializedKey, false))
            {
                return;
            }

            if (Application.isBatchMode &&
                string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("UNITY_MCP_ALLOW_BATCH")))
            {
                return;
            }

            SessionState.SetBool(SessionInitializedKey, true);
            EditorApplication.delayCall += StartWhenEditorIsReady;
        }

        private static void StartWhenEditorIsReady()
        {
            _ = StartAsync();
        }

        private static async Task StartAsync()
        {
            try
            {
                EditorConfigurationCache.Instance.SetUseHttpTransport(true);
                EditorConfigurationCache.Instance.SetHttpTransportScope("local");

                if (MCPServiceLocator.Bridge.IsRunning)
                {
                    return;
                }

                string localUrl = HttpEndpointUtility.GetLocalBaseUrl();
                if (!HttpEndpointUtility.IsHttpLocalUrlAllowedForLaunch(localUrl, out string policyError))
                {
                    Debug.LogWarning($"[Unity MCP AutoStart] Local MCP server URL is blocked: {policyError}");
                    return;
                }

                if (!MCPServiceLocator.Server.IsLocalHttpServerReachable())
                {
                    bool serverStarted = MCPServiceLocator.Server.StartLocalHttpServer(quiet: true);
                    if (!serverStarted)
                    {
                        Debug.LogWarning("[Unity MCP AutoStart] Failed to start the local MCP HTTP server.");
                        return;
                    }
                }

                await WaitForServerAndStartBridgeAsync();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Unity MCP AutoStart] Failed: {ex.Message}");
            }
        }

        private static async Task WaitForServerAndStartBridgeAsync()
        {
            const int maxAttempts = 30;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                if (MCPServiceLocator.Bridge.IsRunning)
                {
                    return;
                }

                bool canTryBridge = MCPServiceLocator.Server.IsLocalHttpServerReachable()
                    || attempt >= 20 && (attempt - 20) % 3 == 0;

                if (canTryBridge)
                {
                    bool bridgeStarted = await MCPServiceLocator.Bridge.StartAsync();
                    if (bridgeStarted)
                    {
                        var verification = await MCPServiceLocator.Bridge.VerifyAsync();
                        if (!verification.Success)
                        {
                            Debug.LogWarning($"[Unity MCP AutoStart] MCP bridge started but verification reported: {verification.Message}");
                        }

                        Debug.Log("[Unity MCP AutoStart] Local MCP server and bridge session are running.");
                        return;
                    }
                }

                TimeSpan delay = attempt < 6
                    ? TimeSpan.FromMilliseconds(500)
                    : TimeSpan.FromSeconds(3);
                await Task.Delay(delay);
            }

            Debug.LogWarning("[Unity MCP AutoStart] Local MCP server did not become reachable in time.");
        }
    }
}

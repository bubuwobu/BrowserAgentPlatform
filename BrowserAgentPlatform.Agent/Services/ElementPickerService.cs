using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Playwright;

namespace BrowserAgentPlatform.Agent.Services;

public class ElementPickerService
{
    private readonly HttpClient _httpClient;

    public ElementPickerService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    // 兼容旧调用：第五个参数直接传 CancellationToken
    public Task StartPickerAsync(
        IPage page,
        string apiBaseUrl,
        string sessionId,
        long profileId,
        CancellationToken cancellationToken)
        => StartPickerAsync(page, apiBaseUrl, sessionId, profileId, false, false, cancellationToken);

    // 兼容旧调用：省略 sessionId
    public Task StopPickerAsync(IPage page, CancellationToken cancellationToken = default)
        => StopPickerAsync(page, string.Empty, cancellationToken);

    public async Task StartPickerAsync(
        IPage page,
        string apiBaseUrl,
        string sessionId,
        long profileId,
        bool continuous = false,
        bool resume = false,
        CancellationToken cancellationToken = default)
    {
        var scriptPath = Path.Combine(AppContext.BaseDirectory, "Scripts", "element-picker.js");
        var script = await File.ReadAllTextAsync(scriptPath, cancellationToken);

        try
        {
            await page.ExposeFunctionAsync("__BAP_PICKER_BRIDGE__", async (string payloadJson) =>
            {
                using var doc = JsonDocument.Parse(payloadJson);
                var root = doc.RootElement;
                var element = root.GetProperty("element");

                var selectors = new List<object>();
                if (root.TryGetProperty("selectors", out var selectorsEl) && selectorsEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in selectorsEl.EnumerateArray())
                    {
                        selectors.Add(new
                        {
                            selector = item.TryGetProperty("selector", out var s) ? s.GetString() ?? string.Empty : string.Empty,
                            level = item.TryGetProperty("level", out var l) ? l.GetString() ?? "medium" : "medium",
                            source = item.TryGetProperty("source", out var so) ? so.GetString() ?? string.Empty : string.Empty
                        });
                    }
                }

                var payload = new
                {
                    sessionId,
                    profileId,
                    url = root.TryGetProperty("url", out var urlEl) ? urlEl.GetString() ?? string.Empty : string.Empty,
                    continuous = root.TryGetProperty("continuous", out var continuousEl) ? continuousEl.GetBoolean() : continuous,
                    element = new
                    {
                        tagName = element.TryGetProperty("tagName", out var tagName) ? tagName.GetString() ?? string.Empty : string.Empty,
                        text = element.TryGetProperty("text", out var text) ? text.GetString() ?? string.Empty : string.Empty,
                        id = element.TryGetProperty("id", out var id) ? id.GetString() ?? string.Empty : string.Empty,
                        name = element.TryGetProperty("name", out var name) ? name.GetString() ?? string.Empty : string.Empty,
                        ariaLabel = element.TryGetProperty("ariaLabel", out var ariaLabel) ? ariaLabel.GetString() ?? string.Empty : string.Empty,
                        dataTestId = element.TryGetProperty("dataTestId", out var dataTestId) ? dataTestId.GetString() ?? string.Empty : string.Empty,
                        role = element.TryGetProperty("role", out var role) ? role.GetString() ?? string.Empty : string.Empty,
                        placeholder = element.TryGetProperty("placeholder", out var placeholder) ? placeholder.GetString() ?? string.Empty : string.Empty,
                        href = element.TryGetProperty("href", out var href) ? href.GetString() ?? string.Empty : string.Empty,
                        src = element.TryGetProperty("src", out var src) ? src.GetString() ?? string.Empty : string.Empty,
                        cssPath = element.TryGetProperty("cssPath", out var cssPath) ? cssPath.GetString() ?? string.Empty : string.Empty,
                        classList = element.TryGetProperty("classList", out var classList) && classList.ValueKind == JsonValueKind.Array
                            ? classList.EnumerateArray().Select(x => x.GetString() ?? string.Empty).Where(x => !string.IsNullOrWhiteSpace(x)).ToList()
                            : new List<string>()
                    },
                    selectors,
                    recommendedNodeType = root.TryGetProperty("nodeType", out var nodeType) ? nodeType.GetString() : null,
                    recommendedTargetField = root.TryGetProperty("targetField", out var targetField) ? targetField.GetString() : null
                };

                var response = await _httpClient.PostAsJsonAsync($"{apiBaseUrl.TrimEnd('/')}/api/picker/result", payload, cancellationToken);
                response.EnsureSuccessStatusCode();
            });
        }
        catch
        {
        }

        await page.EvaluateAsync($"window.__BAP_PICKER_SESSION_ID__ = '{sessionId}';");
        await page.EvaluateAsync($"window.__BAP_PICKER_CONTINUOUS__ = {(continuous ? "true" : "false")};");
        await page.AddInitScriptAsync(script);
        await page.EvaluateAsync(script);
    }

    public async Task StopPickerAsync(
        IPage page,
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        const string cleanupScript = @"
(() => {
  try {
    if (window.__BAP_PICKER_MOUSEMOVE__) {
      document.removeEventListener('mousemove', window.__BAP_PICKER_MOUSEMOVE__, true);
      window.__BAP_PICKER_MOUSEMOVE__ = null;
    }
    if (window.__BAP_PICKER_CLICK__) {
      document.removeEventListener('click', window.__BAP_PICKER_CLICK__, true);
      window.__BAP_PICKER_CLICK__ = null;
    }
    const overlay = document.getElementById('__bap_picker_overlay__');
    if (overlay) overlay.remove();
    const badge = document.getElementById('__bap_picker_badge__');
    if (badge) badge.remove();
    window.__BAP_PICKER_INSTALLED__ = false;
    window.__BAP_PICKER_SESSION_ID__ = '';
    window.__BAP_PICKER_CONTINUOUS__ = false;
  } catch (e) {
    console.warn('Stop picker cleanup failed', e);
  }
})();
";
        await page.EvaluateAsync(cleanupScript);
    }
}

using BrowserAgentPlatform.Api.Models;

namespace BrowserAgentPlatform.Api.Services;

public class PickerRecommendationService
{
    public PickerEnrichedResultDto Enrich(PickerResultRequest request, int sequenceNo)
    {
        var selectors = RankSelectors(request).ToList();
        var bestSelector = selectors.FirstOrDefault()?.Selector ?? request.Element.CssPath ?? string.Empty;
        var (nodeType, targetField) = RecommendNode(request.Element);
        var (flowTemplate, flowSteps) = RecommendFlowTemplate(request, nodeType, bestSelector);

        return new PickerEnrichedResultDto
        {
            SessionId = request.SessionId,
            ProfileId = request.ProfileId,
            Url = request.Url,
            Continuous = request.Continuous,
            Element = request.Element,
            Selectors = selectors,
            RecommendedNodeType = nodeType,
            RecommendedTargetField = targetField,
            RecommendedFlowTemplate = flowTemplate,
            RecommendedFlowSteps = flowSteps,
            SequenceNo = sequenceNo
        };
    }

    private IEnumerable<PickerSelectorCandidateDto> RankSelectors(PickerResultRequest request)
    {
        var list = new List<PickerSelectorCandidateDto>();
        foreach (var item in request.Selectors ?? new List<PickerSelectorCandidateDto>())
        {
            var score = item.Source switch
            {
                "id" => 100,
                "data-testid" => 95,
                "tag+name" => 90,
                "aria-label" => 80,
                "role+text" => 72,
                "tag+text" => 68,
                "text" => 60,
                "css-path" => 35,
                _ => 50
            };
            item.Score = score;
            item.Level = score >= 90 ? "high" : score >= 65 ? "medium" : "low";
            list.Add(item);
        }

        if (!list.Any() && !string.IsNullOrWhiteSpace(request.Element.CssPath))
        {
            list.Add(new PickerSelectorCandidateDto
            {
                Selector = request.Element.CssPath,
                Source = "css-path",
                Score = 35,
                Level = "low"
            });
        }

        return list.GroupBy(x => x.Selector).Select(g => g.OrderByDescending(x => x.Score).First()).OrderByDescending(x => x.Score);
    }

    private static (string nodeType, string targetField) RecommendNode(PickerElementDto element)
    {
        var tag = (element.TagName ?? string.Empty).Trim().ToLowerInvariant();
        var role = (element.Role ?? string.Empty).Trim().ToLowerInvariant();

        if (tag is "input" or "textarea") return ("type", "selector");
        if (tag == "select") return ("select_option", "selector");
        if (tag == "img") return ("extract_attr", "selector");
        if (tag == "ul" || tag == "ol") return ("loop_list", "itemSelector");
        if (tag == "button" || tag == "a" || role == "button") return ("click", "selector");
        if (!string.IsNullOrWhiteSpace(element.Text)) return ("extract_text", "selector");
        return ("click", "selector");
    }

    private static (string flowTemplate, List<PickerFlowStepDto> steps) RecommendFlowTemplate(
        PickerResultRequest request,
        string nodeType,
        string bestSelector)
    {
        var tag = (request.Element.TagName ?? string.Empty).ToLowerInvariant();
        var text = request.Element.Text ?? string.Empty;
        var placeholder = request.Element.Placeholder ?? string.Empty;
        var url = string.IsNullOrWhiteSpace(request.Url) ? "https://example.com" : request.Url;

        if (tag == "img")
        {
            return ("image_extract_flow", new List<PickerFlowStepDto>
            {
                Step("open", new() { ["label"] = "打开页面", ["url"] = url }),
                Step("wait_for_element", new() { ["label"] = "等待图片元素", ["selector"] = bestSelector, ["timeout"] = 10000 }),
                Step("extract_attr", new() { ["label"] = "提取图片链接", ["selector"] = bestSelector, ["attr"] = "src" }),
                Step("end_success", new() { ["label"] = "结束" })
            });
        }

        if (tag is "ul" or "ol")
        {
            return ("list_loop_extract", new List<PickerFlowStepDto>
            {
                Step("open", new() { ["label"] = "打开页面", ["url"] = url }),
                Step("wait_for_element", new() { ["label"] = "等待列表容器", ["selector"] = bestSelector, ["timeout"] = 10000 }),
                Step("loop_list", new() { ["label"] = "遍历列表", ["itemSelector"] = bestSelector, ["maxItems"] = 10 }),
                Step("extract_text", new() { ["label"] = "提取文本", ["selector"] = bestSelector }),
                Step("end_success", new() { ["label"] = "结束" })
            });
        }

        if (tag is "input" or "textarea")
        {
            var looksLikeLogin = bestSelector.Contains("user", StringComparison.OrdinalIgnoreCase)
                || bestSelector.Contains("email", StringComparison.OrdinalIgnoreCase)
                || bestSelector.Contains("pass", StringComparison.OrdinalIgnoreCase)
                || placeholder.Contains("密码", StringComparison.OrdinalIgnoreCase)
                || placeholder.Contains("账号", StringComparison.OrdinalIgnoreCase)
                || placeholder.Contains("邮箱", StringComparison.OrdinalIgnoreCase);

            if (looksLikeLogin)
            {
                return ("login_form_flow", new List<PickerFlowStepDto>
                {
                    Step("open", new() { ["label"] = "打开登录页", ["url"] = url }),
                    Step("wait_for_element", new() { ["label"] = "等待输入框", ["selector"] = bestSelector, ["timeout"] = 10000 }),
                    Step("type", new() { ["label"] = "填写账号", ["selector"] = bestSelector, ["value"] = "demo_user" }),
                    Step("type", new() { ["label"] = "填写密码", ["selector"] = "input[type='password']", ["value"] = "demo_pass" }),
                    Step("click", new() { ["label"] = "点击登录", ["selector"] = "button[type='submit']" }),
                    Step("wait_for_url", new() { ["label"] = "等待跳转", ["urlPart"] = "/", ["timeout"] = 10000 }),
                    Step("end_success", new() { ["label"] = "结束" })
                });
            }

            return ("form_fill_submit", new List<PickerFlowStepDto>
            {
                Step("open", new() { ["label"] = "打开页面", ["url"] = url }),
                Step("wait_for_element", new() { ["label"] = "等待输入框", ["selector"] = bestSelector, ["timeout"] = 10000 }),
                Step("type", new() { ["label"] = "填写内容", ["selector"] = bestSelector, ["value"] = placeholder != "" ? $"填写 {placeholder}" : "demo_value" }),
                Step("click", new() { ["label"] = "点击提交", ["selector"] = "button[type='submit']" }),
                Step("end_success", new() { ["label"] = "结束" })
            });
        }

        if (nodeType == "click")
        {
            return ("single_click_action", new List<PickerFlowStepDto>
            {
                Step("open", new() { ["label"] = "打开页面", ["url"] = url }),
                Step("wait_for_element", new() { ["label"] = "等待目标元素", ["selector"] = bestSelector, ["timeout"] = 10000 }),
                Step("click", new() { ["label"] = string.IsNullOrWhiteSpace(text) ? "点击元素" : $"点击 {text}", ["selector"] = bestSelector }),
                Step("end_success", new() { ["label"] = "结束" })
            });
        }

        return ("open_wait_action", new List<PickerFlowStepDto>
        {
            Step("open", new() { ["label"] = "打开页面", ["url"] = url }),
            Step("wait_for_element", new() { ["label"] = "等待目标元素", ["selector"] = bestSelector, ["timeout"] = 10000 }),
            Step(nodeType, new() { ["label"] = $"{nodeType} 动作", ["selector"] = bestSelector }),
            Step("end_success", new() { ["label"] = "结束" })
        });
    }

    private static PickerFlowStepDto Step(string type, Dictionary<string, object?> data)
        => new PickerFlowStepDto { Type = type, Data = data };
}

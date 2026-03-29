namespace BrowserAgentPlatform.Api.Models;

public class PickerStartRequest
{
    public long ProfileId { get; set; }
    public string? PageUrl { get; set; }
    public string? NodeId { get; set; }
    public string? NodeType { get; set; }
    public bool Continuous { get; set; }
    public bool ResumeIfExists { get; set; }
}

public class PickerStopRequest
{
    public string SessionId { get; set; } = string.Empty;
    public long ProfileId { get; set; }
}

public class PickerResumeRequest
{
    public string SessionId { get; set; } = string.Empty;
    public long ProfileId { get; set; }
    public bool Continuous { get; set; } = true;
}

public class PickerStartResponse
{
    public string SessionId { get; set; } = string.Empty;
    public string Status { get; set; } = "started";
    public long ProfileId { get; set; }
    public string? NodeId { get; set; }
    public string? NodeType { get; set; }
    public bool Continuous { get; set; }
    public bool Restored { get; set; }
}

public class PickerSessionDto
{
    public string SessionId { get; set; } = string.Empty;
    public long ProfileId { get; set; }
    public long? AgentId { get; set; }
    public string Status { get; set; } = "created";
    public string? PageUrl { get; set; }
    public string? NodeId { get; set; }
    public string? NodeType { get; set; }
    public bool Continuous { get; set; }
    public bool IsPaused { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastEventAtUtc { get; set; }
    public int PickCount { get; set; }
}

public class PickerElementDto
{
    public string TagName { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AriaLabel { get; set; } = string.Empty;
    public string DataTestId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Placeholder { get; set; } = string.Empty;
    public string Href { get; set; } = string.Empty;
    public string Src { get; set; } = string.Empty;
    public string CssPath { get; set; } = string.Empty;
    public List<string> ClassList { get; set; } = new();
}

public class PickerSelectorCandidateDto
{
    public string Selector { get; set; } = string.Empty;
    public string Level { get; set; } = "medium";
    public string Source { get; set; } = string.Empty;
    public int Score { get; set; }
}

public class PickerFlowStepDto
{
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, object?> Data { get; set; } = new();
}

public class PickerResultRequest
{
    public string SessionId { get; set; } = string.Empty;
    public long ProfileId { get; set; }
    public string Url { get; set; } = string.Empty;
    public bool Continuous { get; set; }
    public PickerElementDto Element { get; set; } = new();
    public List<PickerSelectorCandidateDto> Selectors { get; set; } = new();
    public string? RecommendedNodeType { get; set; }
    public string? RecommendedTargetField { get; set; }
}

public class PickerAgentCommand
{
    public string CommandType { get; set; } = "start_element_picker";
    public string SessionId { get; set; } = string.Empty;
    public long ProfileId { get; set; }
    public string ApiBaseUrl { get; set; } = string.Empty;
    public string? PageUrl { get; set; }
    public bool Continuous { get; set; }
    public bool Resume { get; set; }
}

public class PickerEnrichedResultDto
{
    public string SessionId { get; set; } = string.Empty;
    public long ProfileId { get; set; }
    public string Url { get; set; } = string.Empty;
    public bool Continuous { get; set; }
    public PickerElementDto Element { get; set; } = new();
    public List<PickerSelectorCandidateDto> Selectors { get; set; } = new();
    public string RecommendedNodeType { get; set; } = "click";
    public string RecommendedTargetField { get; set; } = "selector";
    public string RecommendedFlowTemplate { get; set; } = "single_click_action";
    public List<PickerFlowStepDto> RecommendedFlowSteps { get; set; } = new();
    public int SequenceNo { get; set; }
}

public class PickerStateSnapshotDto
{
    public string SessionId { get; set; } = string.Empty;
    public long ProfileId { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool Continuous { get; set; }
    public bool IsPaused { get; set; }
    public int PickCount { get; set; }
    public DateTime? LastEventAtUtc { get; set; }
}

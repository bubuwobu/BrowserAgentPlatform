# Phase 6.2 集成说明

## API Program.cs
```csharp
builder.Services.AddSingleton<PickerSessionService>();
builder.Services.AddSingleton<PickerRecommendationService>();
builder.Services.AddSignalR();
app.MapHub<PickerHub>("/hubs/picker");
```

## Agent Program.cs
```csharp
builder.Services.AddHttpClient<ElementPickerService>();
```

## 前端读取新字段
- recommendedFlowTemplate
- recommendedFlowSteps

## AgentWorker 继续沿用
- start_element_picker
- stop_element_picker
- resume_element_picker

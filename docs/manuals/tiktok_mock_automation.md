# TikTok Mock 自动化说明（随机浏览/点赞/评论）

本项目已新增 `tiktok_mock_session` 步骤类型，专用于 `tiktok_mock_site`。

## 支持能力

- 打开并登录 TikTok Mock 站点（`/login` -> `/feed`）
- 随机浏览视频（可配置最少/最多视频数）
- 每条视频随机停留时长（可配置最小/最大毫秒）
- 随机点赞与评论（分别可配置最小/最大数量）
- 评论文本“AI 风格生成”：
  - 读取当前视频评论区已有评论
  - 结合视频 caption 组装语义更贴合的评论文本

> 说明：当前是内置“轻量 AI 风格生成”（规则生成），不依赖外部大模型服务。

## 任务 Payload 示例

```json
{
  "isolationGate": {
    "enforce": true,
    "requireRecentCheckMinutes": 120
  },
  "steps": [
    {
      "id": "tiktok_session",
      "type": "tiktok_mock_session",
      "data": {
        "label": "执行 TikTok Mock 自动化会话",
        "baseUrl": "http://localhost:3001",
        "username": "alice",
        "password": "123456",
        "minVideos": 3,
        "maxVideos": 8,
        "minWatchMs": 3000,
        "maxWatchMs": 9000,
        "minLikes": 1,
        "maxLikes": 4,
        "minComments": 1,
        "maxComments": 3
      }
    },
    { "id": "done", "type": "end_success", "data": { "label": "完成" } }
  ],
  "edges": [
    { "source": "tiktok_session", "target": "done" }
  ],
  "assertions": [
    { "type": "number_range", "label": "浏览数范围校验", "sourcePath": "tiktok_session.watchedVideos", "min": 3, "max": 8 },
    { "type": "number_range", "label": "点赞数范围校验", "sourcePath": "tiktok_session.likedVideos", "min": 1, "max": 4 },
    { "type": "number_range", "label": "评论数范围校验", "sourcePath": "tiktok_session.commentedVideos", "min": 1, "max": 3 }
  ]
}
```

## 质量门禁（新）

- `isolationGate`：要求 Profile 在最近 N 分钟内完成隔离检查，否则任务在 Pull 阶段直接失败（错误码 `isolation_gate_failed`）。
- `assertions`：执行后自动校验结果；任何断言失败都会把 run 标记为 `failed`，并在 `resultJson.assertions` 中给出失败原因。

## Web 页面使用

任务中心已新增：

- `TikTok示例` 按钮：一键填充 Payload；
- `TikTok 快速配置` 面板：可直接设置最小/最大视频数、停留时长、点赞/评论数量，再点击 `TikTok示例` 生成任务。

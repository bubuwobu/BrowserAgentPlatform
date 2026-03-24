这是一套优化后的 TikTok 仿站补丁：

改进点：
1. 右侧增加“上一条 / 下一条”固定按钮，测试更稳。
2. 增加当前视频状态区：当前视频ID、作者、停留秒数、总视频数。
3. 激活视频时自动增加浏览量，页面显示 view count。
4. 增加进度条，便于观察浏览停留。
5. JSON 已改成针对 .active 当前视频元素操作，切换下一条更稳定。

覆盖文件：
- server.js
- views/feed.ejs
- public/app.js
- public/styles.css
- tiktok_mock_browse_optimized.json

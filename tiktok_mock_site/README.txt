这是“几乎 1:1 TikTok Web clone + 可自动化测试”前端升级补丁包。

包含：
- views/feed.ejs
- views/profile.ejs
- public/styles.css
- public/app.js
- README.txt

特性：
1. 更接近 TikTok Web 的三栏布局
2. 视频切换滑动过渡动画
3. 切换时的 loading 蒙层
4. 推荐流式 active 视频切换
5. 右侧悬浮上下切换按钮
6. 支持滚轮和键盘上下键切视频
7. 评论区独立右侧面板
8. 保留自动化 data-testid：
   - tt-video-card
   - tt-nav-prev
   - tt-nav-next
   - tt-like-btn
   - tt-comment-toggle
   - tt-comment-input
   - tt-comment-submit
   - tt-favorite-btn
   - tt-share-btn
   - tt-author-link
   - tt-profile-name
   - tt-view-count
   - tt-favorite-count
   - tt-music-name
   - tt-duration
   - tt-created-at
   - tt-author-verified
   - tt-comment-list

使用：
直接覆盖你当前 mock 站点对应文件，重启服务即可。
如果你当前 server.js 已经兼容这些字段，无需改接口。

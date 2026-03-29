# Phase 6.0：元素拾取器 + 常用节点扩展第一批

## 一、本批节点扩展
本次新增/强化节点：
- hover
- select_option
- upload_file
- wait_for_text
- wait_for_url
- exists
- screenshot
- log
- extract_attr
- loop_list
- scroll_to_element
- retry
- manual_review
- refresh_page
- switch_tab
- press_key
- clear_input

## 二、元素拾取器 V1
### 当前可交付
- 选择目标页面 URL
- 输入元素文本 / id / name / aria-label / data-testid / class
- 自动生成多组 selector 候选
- 标记推荐级别
- 一键回填到当前节点

### 下一阶段升级
- 可视化 hover 高亮
- 点击目标元素回填
- DOM 唯一性校验
- 本地 Agent 注入拾取

## 三、验收目标
- 非技术用户不再需要手写大部分 selector
- Builder 节点数量够支撑真实流程
- 对常见任务：登录、点击、提取、滚动、等待、截图，都可以纯表单完成

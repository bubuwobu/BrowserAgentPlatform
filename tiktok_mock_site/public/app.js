(() => {
  const state = {
    activeVideoId: window.__INITIAL_STATE__?.activeVideoId ?? null,
    progressTimer: null,
    progressValue: 0,
    progressDuration: 4800,
    loadingLock: false
  };

  const loadingLayer = document.querySelector("[data-testid='tt-loading-layer']");
  const commentPanel = document.querySelector("[data-testid='tt-comment-panel']");

  function cards() {
    return Array.from(document.querySelectorAll("[data-testid='tt-video-card']"));
  }

  function getCardById(id) {
    return cards().find(card => Number(card.dataset.videoId) === Number(id));
  }

  function getActiveCard() {
    return document.querySelector("[data-testid='tt-video-card'].active");
  }

  function showLoading() {
    if (loadingLayer) loadingLayer.classList.add("show");
  }

  function hideLoading() {
    if (loadingLayer) loadingLayer.classList.remove("show");
  }

  function startProgress() {
    clearInterval(state.progressTimer);
    state.progressValue = 0;
    const bar = getActiveCard()?.querySelector("[data-testid='tt-progress-bar']");
    if (bar) bar.style.width = "0%";
    const step = 100 / (state.progressDuration / 120);
    state.progressTimer = setInterval(() => {
      const active = getActiveCard();
      if (!active) return;
      const activeBar = active.querySelector("[data-testid='tt-progress-bar']");
      state.progressValue = Math.min(100, state.progressValue + step);
      if (activeBar) activeBar.style.width = `${state.progressValue}%`;
      if (state.progressValue >= 100) {
        clearInterval(state.progressTimer);
      }
    }, 120);
  }

  function rebuildCommentPanel(card) {
    if (!commentPanel || !card) return;

    const cover = card.querySelector(".tt-video-cover")?.getAttribute("src") || "";
    const author = card.querySelector("[data-testid='tt-author-name']")?.textContent || "";
    const caption = card.querySelector("[data-testid='tt-caption']")?.textContent || "";
    const count = card.querySelector("[data-testid='tt-comment-count']")?.textContent || "0";
    const list = card.querySelector("[data-testid='tt-comment-list']");

    commentPanel.querySelector("[data-testid='tt-comment-total']").textContent = count;
    commentPanel.querySelector(".tt-comment-hero-image").setAttribute("src", cover);
    commentPanel.querySelector(".tt-comment-author").textContent = author;
    commentPanel.querySelector(".tt-comment-preview").textContent = caption;

    const targetList = commentPanel.querySelector("[data-testid='tt-comment-list']");
    targetList.innerHTML = list ? list.innerHTML : "";
  }

  function syncActiveCard(id, direction = "next") {
    const all = cards();
    const current = getActiveCard();
    const next = getCardById(id);
    if (!next || current === next) return;

    if (current) {
      current.classList.remove("active");
      current.classList.add(direction === "next" ? "leaving-up" : "leaving-down");
      current.dataset.active = "false";
      setTimeout(() => {
        current.classList.remove("leaving-up", "leaving-down");
        current.classList.add("inactive");
      }, 380);
    }

    next.classList.remove("inactive");
    next.classList.add(direction === "next" ? "entering-down" : "entering-up");
    next.dataset.active = "true";

    requestAnimationFrame(() => {
      requestAnimationFrame(() => {
        next.classList.remove("entering-down", "entering-up");
        next.classList.add("active");
      });
    });

    state.activeVideoId = id;
    history.replaceState({}, "", `/feed?videoId=${id}`);
    rebuildCommentPanel(next);
    startProgress();
  }

  function currentIndex() {
    return cards().findIndex(card => card.classList.contains("active"));
  }

  function goNav(direction) {
    if (state.loadingLock) return;
    const all = cards();
    const idx = currentIndex();
    if (idx < 0) return;
    const delta = direction === "next" ? 1 : -1;
    let target = idx + delta;
    if (target < 0) target = 0;
    if (target >= all.length) target = all.length - 1;
    if (target === idx) return;

    state.loadingLock = true;
    showLoading();
    setTimeout(() => {
      syncActiveCard(Number(all[target].dataset.videoId), direction);
      hideLoading();
      state.loadingLock = false;
    }, 260);
  }

  async function postJson(url, body = {}) {
    const res = await fetch(url, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body)
    });
    return res.json();
  }

  function updateCardCount(card, testid, value) {
    const el = card.querySelector(`[data-testid='${testid}']`);
    if (el) el.textContent = value;
    if (card.classList.contains("active")) {
      rebuildCommentPanel(card);
    }
  }

  function appendCommentToCard(card, comment) {
    const list = card.querySelector("[data-testid='tt-comment-list']");
    if (!list) return;
    const item = document.createElement("div");
    item.className = "tt-comment-item";
    item.innerHTML = `
      <img class="tt-comment-avatar" src="${comment.avatar}" alt="" />
      <div class="tt-comment-content">
        <div class="tt-comment-name">${comment.displayName}</div>
        <div class="tt-comment-text">${comment.text}</div>
        <div class="tt-comment-sub">${comment.createdAtLabel}前 回复</div>
      </div>
      <div class="tt-comment-like">♡</div>
    `;
    list.appendChild(item);
  }

  document.addEventListener("click", async (event) => {
    const btn = event.target.closest("button");
    if (!btn) return;

    if (btn.dataset.nav === "next" || btn.dataset.nav === "prev") {
      goNav(btn.dataset.nav);
      return;
    }

    if (btn.matches("[data-testid='tt-comment-close']")) {
      if (commentPanel) commentPanel.style.display = "none";
      return;
    }

    const card = btn.closest("[data-testid='tt-video-card']");
    const active = getActiveCard();
    if (!card || !active || card !== active) return;

    const id = Number(card.dataset.videoId);

    if (btn.dataset.action === "toggle-comment") {
      if (commentPanel) {
        commentPanel.style.display = "";
        rebuildCommentPanel(card);
      }
      return;
    }

    if (btn.dataset.action === "like") {
      const data = await postJson(`/api/videos/${id}/like`);
      if (data.ok) {
        updateCardCount(card, "tt-like-count", data.likeCountLabel);
      }
      return;
    }

    if (btn.dataset.action === "favorite") {
      const data = await postJson(`/api/videos/${id}/favorite`);
      if (data.ok) {
        updateCardCount(card, "tt-favorite-count", data.favoriteCountLabel);
      }
      return;
    }

    if (btn.dataset.action === "share") {
      const data = await postJson(`/api/videos/${id}/share`);
      if (data.ok) {
        updateCardCount(card, "tt-share-count", data.shareCountLabel);
      }
      return;
    }

    if (btn.matches("[data-testid='tt-comment-submit']")) {
      const input = commentPanel?.querySelector("[data-testid='tt-comment-input']");
      const notice = commentPanel?.querySelector("[data-testid='tt-comment-notice']");
      const text = input?.value?.trim();
      if (!text) return;
      const data = await postJson(`/api/videos/${id}/comment`, { text });
      if (data.ok) {
        updateCardCount(card, "tt-comment-count", data.commentsCountLabel);
        appendCommentToCard(card, data.comment);
        rebuildCommentPanel(card);
        if (input) input.value = "";
        if (notice) notice.textContent = data.notice;
        setTimeout(() => { if (notice) notice.textContent = ""; }, 1800);
      }
    }
  });

  window.addEventListener("wheel", (event) => {
    if (Math.abs(event.deltaY) < 20) return;
    goNav(event.deltaY > 0 ? "next" : "prev");
  }, { passive: true });

  window.addEventListener("keydown", (event) => {
    if (event.key === "ArrowDown") goNav("next");
    if (event.key === "ArrowUp") goNav("prev");
  });

  rebuildCommentPanel(getActiveCard());
  startProgress();
})();

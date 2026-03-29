(function () {
  const requestedSessionId = window.__BAP_PICKER_SESSION_ID__ || '';
  const requestedContinuous = !!window.__BAP_PICKER_CONTINUOUS__;

  if (window.__BAP_PICKER_INSTALLED__ && window.__BAP_PICKER_SESSION_ID__ === requestedSessionId) return;

  function cleanupExisting() {
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
    } catch {}
  }

  cleanupExisting();
  window.__BAP_PICKER_INSTALLED__ = true;
  window.__BAP_PICKER_CONTINUOUS__ = requestedContinuous;

  const overlay = document.createElement('div');
  overlay.id = '__bap_picker_overlay__';
  overlay.style.position = 'fixed';
  overlay.style.pointerEvents = 'none';
  overlay.style.zIndex = '2147483647';
  overlay.style.border = '2px solid #22c55e';
  overlay.style.background = 'rgba(34,197,94,0.08)';
  overlay.style.left = '-9999px';
  overlay.style.top = '-9999px';
  document.body.appendChild(overlay);

  const badge = document.createElement('div');
  badge.id = '__bap_picker_badge__';
  badge.style.position = 'fixed';
  badge.style.zIndex = '2147483647';
  badge.style.right = '12px';
  badge.style.bottom = '12px';
  badge.style.padding = '10px 12px';
  badge.style.borderRadius = '10px';
  badge.style.background = 'rgba(15,23,42,.95)';
  badge.style.color = '#e2e8f0';
  badge.style.fontSize = '12px';
  badge.style.maxWidth = '420px';
  badge.style.pointerEvents = 'none';
  badge.textContent = requestedContinuous ? 'BAP Picker Ready (continuous)' : 'BAP Picker Ready';
  document.body.appendChild(badge);

  function cssPath(el) {
    if (!el || el.nodeType !== 1) return '';
    const path = [];
    let current = el;
    while (current && current.nodeType === 1 && current !== document.body) {
      let selector = current.nodeName.toLowerCase();
      if (current.id) { selector += '#' + current.id; path.unshift(selector); break; }
      const classes = Array.from(current.classList || []).slice(0, 2);
      if (classes.length) selector += '.' + classes.join('.');
      path.unshift(selector);
      current = current.parentElement;
    }
    return path.join(' > ');
  }

  function selectorCandidates(el) {
    const candidates = [];
    const tag = (el.tagName || '').toLowerCase();
    const text = (el.innerText || el.textContent || '').trim().slice(0, 60);
    const dataTestId = el.getAttribute('data-testid') || '';
    const id = el.id || '';
    const name = el.getAttribute('name') || '';
    const ariaLabel = el.getAttribute('aria-label') || '';
    const role = el.getAttribute('role') || '';

    if (id) candidates.push({ selector: `#${id}`, level: 'high', source: 'id' });
    if (dataTestId) candidates.push({ selector: `[data-testid="${dataTestId}"]`, level: 'high', source: 'data-testid' });
    if (tag && name) candidates.push({ selector: `${tag}[name="${name}"]`, level: 'high', source: 'tag+name' });
    if (ariaLabel) candidates.push({ selector: `[aria-label="${ariaLabel}"]`, level: 'medium', source: 'aria-label' });
    if (role && text) candidates.push({ selector: `[role="${role}"]:has-text("${text.replace(/"/g, '\\"')}")`, level: 'medium', source: 'role+text' });
    if (text) candidates.push({ selector: `text=${text}`, level: 'medium', source: 'text' });
    if (tag && text) candidates.push({ selector: `${tag}:has-text("${text.replace(/"/g, '\\"')}")`, level: 'medium', source: 'tag+text' });
    const path = cssPath(el);
    if (path) candidates.push({ selector: path, level: 'low', source: 'css-path' });

    const seen = new Set();
    return candidates.filter(x => { if (seen.has(x.selector)) return false; seen.add(x.selector); return true; });
  }

  function recommend(el) {
    const tag = (el.tagName || '').toLowerCase();
    const type = (el.getAttribute('type') || '').toLowerCase();
    const role = (el.getAttribute('role') || '').toLowerCase();
    if (tag === 'input' || tag === 'textarea') { if (type === 'file') return { nodeType: 'upload_file', targetField: 'selector' }; return { nodeType: 'type', targetField: 'selector' }; }
    if (tag === 'select') return { nodeType: 'select_option', targetField: 'selector' };
    if (tag === 'img') return { nodeType: 'extract_attr', targetField: 'selector' };
    if (tag === 'ul' || tag === 'ol') return { nodeType: 'loop_list', targetField: 'itemSelector' };
    if (tag === 'button' || tag === 'a' || role === 'button') return { nodeType: 'click', targetField: 'selector' };
    return { nodeType: 'extract_text', targetField: 'selector' };
  }

  function updateOverlay(el) {
    if (!el || el === overlay || el === badge) return;
    const rect = el.getBoundingClientRect();
    overlay.style.left = rect.left + 'px';
    overlay.style.top = rect.top + 'px';
    overlay.style.width = rect.width + 'px';
    overlay.style.height = rect.height + 'px';
    const txt = (el.innerText || el.textContent || '').trim().slice(0, 40);
    const tag = (el.tagName || '').toLowerCase();
    const id = el.id ? `#${el.id}` : '';
    badge.textContent = `${requestedContinuous ? '[C]' : ''} ${tag}${id} ${txt}`.trim();
  }

  window.__BAP_PICKER_MOUSEMOVE__ = function (e) { updateOverlay(e.target); };
  window.__BAP_PICKER_CLICK__ = function (e) {
    e.preventDefault(); e.stopPropagation();
    const el = e.target;
    const payload = {
      url: location.href,
      continuous: requestedContinuous,
      element: {
        tagName: (el.tagName || '').toLowerCase(),
        text: (el.innerText || el.textContent || '').trim().slice(0, 200),
        id: el.id || '',
        name: el.getAttribute('name') || '',
        ariaLabel: el.getAttribute('aria-label') || '',
        dataTestId: el.getAttribute('data-testid') || '',
        role: el.getAttribute('role') || '',
        placeholder: el.getAttribute('placeholder') || '',
        href: el.getAttribute('href') || '',
        src: el.getAttribute('src') || '',
        cssPath: cssPath(el),
        classList: Array.from(el.classList || [])
      },
      selectors: selectorCandidates(el),
      ...recommend(el)
    };
    if (window.__BAP_PICKER_BRIDGE__) window.__BAP_PICKER_BRIDGE__(JSON.stringify(payload));
    if (!requestedContinuous) {
      try {
        document.removeEventListener('mousemove', window.__BAP_PICKER_MOUSEMOVE__, true);
        document.removeEventListener('click', window.__BAP_PICKER_CLICK__, true);
      } catch {}
    }
  };

  document.addEventListener('mousemove', window.__BAP_PICKER_MOUSEMOVE__, true);
  document.addEventListener('click', window.__BAP_PICKER_CLICK__, true);
})();

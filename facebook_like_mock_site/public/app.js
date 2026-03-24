async function likePost(postId, button) {
  const res = await fetch(`/api/posts/${postId}/like`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' }
  });
  const data = await res.json();
  if (!data.ok) return;

  button.classList.add('liked');
  button.setAttribute('aria-pressed', 'true');
  button.innerText = 'Liked';

  const likesEl = document.querySelector(`[data-post-likes="${postId}"]`);
  if (likesEl) likesEl.innerText = `${data.likes} likes`;
}

function toggleComment(postId) {
  const wrap = document.querySelector(`[data-post-comments="${postId}"]`);
  if (!wrap) return;
  wrap.classList.toggle('hidden');
}

async function submitComment(postId) {
  const input = document.querySelector(`[data-post-comment-input="${postId}"]`);
  const notice = document.querySelector(`[data-post-notice="${postId}"]`);
  const list = document.querySelector(`[data-post-comment-list="${postId}"]`);
  const text = input.value.trim();
  if (!text) return;

  const res = await fetch(`/api/posts/${postId}/comment`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ text })
  });

  const data = await res.json();
  if (!data.ok) {
    notice.innerText = data.error || 'Failed';
    notice.classList.remove('hidden');
    return;
  }

  const item = document.createElement('div');
  item.className = 'comment-item';
  item.setAttribute('data-testid', 'post-comment-item');
  item.innerHTML = `
    <div class="avatar" style="width:34px;height:34px"></div>
    <div class="comment-bubble">
      <div><strong>${data.item.user.displayName}</strong></div>
      <div>${data.item.text}</div>
    </div>
  `;
  list.appendChild(item);

  input.value = '';
  notice.innerText = data.message;
  notice.classList.remove('hidden');
}

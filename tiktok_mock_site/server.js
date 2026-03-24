const express = require('express');
const session = require('express-session');
const path = require('path');
const seed = require('./tiktok.seed.realistic.json');

const app = express();
const PORT = process.env.PORT || 3001;

app.set('view engine', 'ejs');
app.set('views', path.join(__dirname, 'views'));

app.use(express.urlencoded({ extended: true }));
app.use(express.json());
app.use('/public', express.static(path.join(__dirname, 'public')));
app.use(
  session({
    secret: 'tiktok-mock-secret',
    resave: false,
    saveUninitialized: false
  })
);

const users = seed.users.map(x => ({ ...x }));
const videos = seed.videos.map(v => ({
  ...v,
  likedBy: [],
  favoritedBy: [],
  sharedBy: [],
  viewedBy: []
}));

function fmtCount(n) {
  if (n >= 1000000) return `${(n / 1000000).toFixed(1)}M`;
  if (n >= 1000) return `${(n / 1000).toFixed(1)}K`;
  return String(n);
}

function getUserById(id) {
  return users.find(x => x.id === id);
}

function getUserByUsername(username) {
  return users.find(x => x.username === username);
}

function enrichVideo(video, viewerId) {
  const author = getUserById(video.authorId);
  return {
    ...video,
    author,
    likeCountLabel: fmtCount(video.likes),
    favoriteCountLabel: fmtCount(video.favorites),
    shareCountLabel: fmtCount(video.shares),
    viewCountLabel: fmtCount(video.views),
    commentsCountLabel: fmtCount(video.commentsCount),
    isLiked: viewerId ? video.likedBy.includes(viewerId) : false,
    isFavorited: viewerId ? video.favoritedBy.includes(viewerId) : false,
    isShared: viewerId ? video.sharedBy.includes(viewerId) : false
  };
}

function requireAuth(req, res, next) {
  if (!req.session.userId) {
    return res.redirect('/login');
  }
  next();
}

app.get('/', (req, res) => res.redirect('/feed'));

app.get('/login', (req, res) => {
  if (req.session.userId) {
    return res.redirect('/feed');
  }
  res.render('login', { error: '', next: req.query.next || '/feed' });
});

app.post('/login', (req, res) => {
  const { username, password, next } = req.body;
  const user = users.find(x => x.username === username && x.password === password);
  if (!user) {
    return res.status(400).render('login', {
      error: '用户名或密码错误',
      next: next || '/feed'
    });
  }
  req.session.userId = user.id;
  res.redirect(next || '/feed');
});

app.post('/logout', (req, res) => {
  req.session.destroy(() => res.redirect('/login'));
});

app.get('/feed', requireAuth, (req, res) => {
  const currentUser = getUserById(req.session.userId);
  const feedVideos = videos.map(v => enrichVideo(v, currentUser.id));
  const activeId = Number(req.query.videoId || feedVideos[0]?.id || 0);
  const activeIndex = Math.max(feedVideos.findIndex(x => x.id === activeId), 0);
  const activeVideo = feedVideos[activeIndex] || feedVideos[0];
  if (activeVideo) {
    const raw = videos.find(x => x.id === activeVideo.id);
    if (raw && !raw.viewedBy.includes(currentUser.id)) {
      raw.viewedBy.push(currentUser.id);
      raw.views += 1;
      activeVideo.views = raw.views;
      activeVideo.viewCountLabel = fmtCount(raw.views);
    }
  }
  res.render('feed', {
    user: currentUser,
    videos: feedVideos,
    activeVideoId: activeVideo?.id || null,
    activeIndex
  });
});

app.get('/profile/:username', requireAuth, (req, res) => {
  const profileUser = getUserByUsername(req.params.username);
  if (!profileUser) return res.status(404).render('not-found');
  const profileVideos = videos
    .filter(v => v.authorId === profileUser.id)
    .map(v => enrichVideo(v, req.session.userId));
  res.render('profile', {
    user: getUserById(req.session.userId),
    profileUser,
    profileVideos
  });
});

app.get('/api/feed-state', requireAuth, (req, res) => {
  const currentUser = getUserById(req.session.userId);
  const feedVideos = videos.map(v => enrichVideo(v, currentUser.id));
  res.json({ ok: true, videos: feedVideos });
});

app.post('/api/videos/:id/like', requireAuth, (req, res) => {
  const video = videos.find(v => v.id === Number(req.params.id));
  if (!video) return res.status(404).json({ ok: false, error: 'VIDEO_NOT_FOUND' });
  const uid = req.session.userId;
  if (video.likedBy.includes(uid)) {
    video.likedBy = video.likedBy.filter(x => x !== uid);
    video.likes = Math.max(0, video.likes - 1);
  } else {
    video.likedBy.push(uid);
    video.likes += 1;
  }
  res.json({ ok: true, likes: video.likes, likeCountLabel: fmtCount(video.likes), isLiked: video.likedBy.includes(uid) });
});

app.post('/api/videos/:id/favorite', requireAuth, (req, res) => {
  const video = videos.find(v => v.id === Number(req.params.id));
  if (!video) return res.status(404).json({ ok: false, error: 'VIDEO_NOT_FOUND' });
  const uid = req.session.userId;
  if (video.favoritedBy.includes(uid)) {
    video.favoritedBy = video.favoritedBy.filter(x => x !== uid);
    video.favorites = Math.max(0, video.favorites - 1);
  } else {
    video.favoritedBy.push(uid);
    video.favorites += 1;
  }
  res.json({ ok: true, favorites: video.favorites, favoriteCountLabel: fmtCount(video.favorites), isFavorited: video.favoritedBy.includes(uid) });
});

app.post('/api/videos/:id/share', requireAuth, (req, res) => {
  const video = videos.find(v => v.id === Number(req.params.id));
  if (!video) return res.status(404).json({ ok: false, error: 'VIDEO_NOT_FOUND' });
  const uid = req.session.userId;
  if (!video.sharedBy.includes(uid)) {
    video.sharedBy.push(uid);
    video.shares += 1;
  }
  res.json({ ok: true, shares: video.shares, shareCountLabel: fmtCount(video.shares), isShared: true });
});

app.post('/api/videos/:id/comment', requireAuth, (req, res) => {
  const video = videos.find(v => v.id === Number(req.params.id));
  if (!video) return res.status(404).json({ ok: false, error: 'VIDEO_NOT_FOUND' });
  const text = String(req.body.text || '').trim();
  if (!text) return res.status(400).json({ ok: false, error: 'EMPTY_COMMENT' });

  const user = getUserById(req.session.userId);
  const comment = {
    id: video.id * 1000 + video.comments.length + 1,
    userId: user.id,
    username: user.username,
    displayName: user.displayName,
    avatar: user.avatar,
    text,
    createdAtLabel: 'now'
  };
  video.comments.push(comment);
  video.commentsCount += 1;
  res.json({
    ok: true,
    notice: 'Comment posted successfully.',
    commentsCount: video.commentsCount,
    commentsCountLabel: fmtCount(video.commentsCount),
    comment
  });
});

app.get('/health', (req, res) => res.json({ ok: true }));

app.listen(PORT, () => {
  console.log(`TikTok mock site running on http://localhost:${PORT}`);
});

const express = require("express");
const path = require("path");
const session = require("express-session");
const cookieParser = require("cookie-parser");

const app = express();
const PORT = 3000;

app.set("view engine", "ejs");
app.set("views", path.join(__dirname, "views"));
app.use(express.urlencoded({ extended: true }));
app.use(express.json());
app.use(cookieParser());
app.use(session({
  secret: "facebook-like-local-secret",
  resave: false,
  saveUninitialized: true
}));
app.use(express.static(path.join(__dirname, "public")));

const users = [
  { id: 1, username: "alice", password: "123456", displayName: "Alice Chen" },
  { id: 2, username: "bob", password: "123456", displayName: "Bob Li" },
  { id: 3, username: "cindy", password: "123456", displayName: "Cindy Wu" }
];

let posts = [
  {
    id: 1,
    authorId: 1,
    content: "今天把新的页面样式整理了一遍，交互终于更顺手了。",
    image: "https://picsum.photos/seed/fblike1/960/540",
    likes: [2],
    comments: [{ id: 101, userId: 2, text: "看起来很不错。" }],
    createdAt: "Just now"
  },
  {
    id: 2,
    authorId: 2,
    content: "我们正在用自动化 Agent 跑浏览、点赞、评论流程，方便验证前端逻辑。",
    image: "https://picsum.photos/seed/fblike2/960/540",
    likes: [1,3],
    comments: [],
    createdAt: "3 mins"
  },
  {
    id: 3,
    authorId: 3,
    content: "这条帖子专门用来测试评论展开、提交、刷新和个人主页跳转。",
    image: "https://picsum.photos/seed/fblike3/960/540",
    likes: [],
    comments: [{ id: 301, userId: 1, text: "这个测试场景很清晰。" }],
    createdAt: "8 mins"
  }
];

function currentUser(req) {
  return req.session.user || null;
}

function findUser(id) {
  return users.find(x => x.id === id) || null;
}

function buildFeedView(userId) {
  return posts.map(post => ({
    ...post,
    author: findUser(post.authorId),
    likedByCurrentUser: !!userId && post.likes.includes(userId),
    likeCount: post.likes.length,
    commentsDetailed: post.comments.map(c => ({
      ...c,
      user: findUser(c.userId)
    }))
  }));
}

app.use((req, res, next) => {
  res.locals.currentUser = currentUser(req);
  next();
});

app.get("/", (req, res) => {
  if (!currentUser(req)) return res.redirect("/login");
  return res.redirect("/feed");
});

app.get("/login", (req, res) => {
  res.render("login", { error: "" });
});

app.post("/login", (req, res) => {
  const { username, password } = req.body;
  const user = users.find(x => x.username === username && x.password === password);
  if (!user) return res.render("login", { error: "Invalid username or password" });
  req.session.user = user;
  return res.redirect("/feed");
});

app.post("/logout", (req, res) => {
  req.session.destroy(() => res.redirect("/login"));
});

app.get("/feed", (req, res) => {
  const user = currentUser(req);
  if (!user) return res.redirect("/login");
  res.render("feed", {
    posts: buildFeedView(user.id)
  });
});

app.get("/profile/:username", (req, res) => {
  const user = currentUser(req);
  if (!user) return res.redirect("/login");

  const profileUser = users.find(x => x.username === req.params.username);
  if (!profileUser) return res.status(404).send("Profile not found");

  const profilePosts = buildFeedView(user.id).filter(p => p.authorId === profileUser.id);
  res.render("profile", {
    profileUser,
    posts: profilePosts
  });
});

app.post("/api/posts/:id/like", (req, res) => {
  const user = currentUser(req);
  if (!user) return res.status(401).json({ ok: false, error: "login required" });

  const id = Number(req.params.id);
  const post = posts.find(x => x.id === id);
  if (!post) return res.status(404).json({ ok: false, error: "post not found" });

  if (!post.likes.includes(user.id)) {
    post.likes.push(user.id);
  }

  return res.json({
    ok: true,
    liked: true,
    likes: post.likes.length
  });
});

app.post("/api/posts/:id/comment", (req, res) => {
  const user = currentUser(req);
  if (!user) return res.status(401).json({ ok: false, error: "login required" });

  const id = Number(req.params.id);
  const post = posts.find(x => x.id === id);
  if (!post) return res.status(404).json({ ok: false, error: "post not found" });

  const text = String(req.body.text || "").trim();
  if (!text) return res.status(400).json({ ok: false, error: "comment is required" });

  const item = {
    id: Date.now(),
    userId: user.id,
    text
  };

  post.comments.push(item);

  return res.json({
    ok: true,
    message: "Comment posted successfully.",
    item: {
      ...item,
      user: user
    }
  });
});

app.listen(PORT, () => {
  console.log(`facebooklike mock app running at http://localhost:${PORT}`);
  console.log("Accounts: alice/bob/cindy with password 123456");
});

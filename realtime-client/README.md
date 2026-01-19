# GearUp Realtime Client

Minimal React + Vite app to test SignalR PostHub events.

How to run:

1. cd realtime-client
2. npm install
3. npm start

By default this connects to https://localhost:5001/hubs/post. Update the URL in `src/App.jsx` if your API runs on a different URL/port.

Signals listened:
- CommentCreated
- CommentLikeUpdated
- PostLikeUpdated

Client methods invoked on the hub:
- JoinGroup(postId)
- JoinCommentsGroup(postId)
- LeaveCommentsGroup(postId)
- LeaveGroup(postId)


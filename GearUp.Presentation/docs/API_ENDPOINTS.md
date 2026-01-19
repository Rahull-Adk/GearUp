# GearUp API — Endpoints Documentation

This document lists HTTP API endpoints and realtime (SignalR) events for the GearUp project. It is generated from the Presentation controllers and SignalR hub implementation. Use this as a reference when building frontend clients (React, other).

---

Summary / How to authenticate

- Base API prefix: `/api/v1`
- All endpoints returning a result use HTTP status codes with a wrapper (some controllers use `ToApiResponse()` helper) — expect JSON responses. Many endpoints require Authorization; the project uses JWT tokens stored in cookies:
  - access_token (cookie) — HttpOnly: true for most flows (AuthController sets HttpOnly true), short-lived (~15 minutes)
  - refresh_token (cookie) — HttpOnly: true, longer lived (~7 days)

- Authentication header: Endpoints marked [Authorize] require a valid JWT in the default scheme (the server reads claims from the validated token). Some controller methods read user id from the `id` claim (User.FindFirst(c => c.Type == "id")).

- Roles/policies used:
  - AdminOnly (admin role/policy)
  - DealerOnly (dealer role/policy)
  - CustomerOnly (customer role/policy)
  - Authorize (any authenticated user)


---

Controllers & endpoints

NOTE: For each endpoint below the format is: HTTP_METHOD  /route  — Description
Authentication requirement | Query/Route/Body parameters | Example request body DTO name (when available)

1) AuthController — route prefix: `/api/v1/auth`

- POST /register — Register a new user
  - Public
  - Body: RegisterRequestDto
  - Response: status + JSON (use ToApiResponse format)

- POST /login — Login user
  - Public
  - Body: LoginRequestDto
  - Response: on success, status + cookies set: `access_token`, `refresh_token`.

- POST /logout — Logout current user
  - [Authorize]
  - Reads `refresh_token` cookie and calls logout service
  - Response: 200 with message on success, clears `access_token` cookie.

- POST /verify-email?token={token} — Verify user email
  - Public
  - Query: token (string)

- POST /resend-verification-email?email={email} — Resend verification
  - Public
  - Query: email (string)

- POST /refresh — Rotate refresh token
  - Public
  - Body: refresh token (string)
  - Response: sets new `access_token` and `refresh_token` cookies on success

- POST /send-password-reset-token?email={email}
  - Public
  - Query: email (string)

- POST /reset-password?token={token}
  - Public
  - Query: token (string), Body: PasswordResetReqDto


2) AdminController — route prefix: `/api/v1/admin`

- POST /login — Admin login
  - Public
  - Body: AdminLoginRequestDto
  - Response: on success sets `access_token` and `refresh_token` cookies (similar to Auth login)

- GET /kyc — Get all KYC requests
  - [Authorize(Policy = "AdminOnly")]

- GET /kyc/{kycId} — Get a KYC request by id
  - [Authorize(Policy = "AdminOnly")]
  - Route: kycId (guid)

- PUT /kyc/{kycId} — Review / update KYC status
  - [Authorize(Policy = "AdminOnly")]
  - Body: kycRequestDto (includes Status and RejectionReason used in UpdateKycStatus)
  - Route: kycId (guid)

- GET /kyc/status/{status} — Get KYC requests by status
  - [Authorize(Policy = "AdminOnly")]
  - Route: status (enum KycStatus)


3) UserController — route prefix: `/api/v1/users`

- GET /me — Get current user profile
  - [Authorize]
  - Reads user id from token claims

- GET /{username} — Get user profile by username
  - [Authorize]
  - Route: username (string)

- PUT /me — Update current user profile
  - [Authorize]
  - Body: UpdateUserRequestDto (multipart/form-data supported via [FromForm])

- POST /kyc — Submit KYC documents (customer only)
  - [Authorize(Policy = "CustomerOnly")]
  - Body: KycRequestDto (multipart/form-data)


4) PostController — route prefix: `/api/v1/posts`

- GET /{postId} — Get post by id
  - [Authorize]
  - Route: postId (guid)
  - Response: Post details (uses PostService.GetPostByIdAsync). Note: controller returns StatusCode(result.Status, result)

- GET / — Get all posts (paged)
  - [Authorize]
  - Query: pageNumber (int, default 1)
  - Response: PageResult (paginated posts)

- POST / — Create a post
  - [Authorize(Policy = "DealerOnly")]
  - Body: CreatePostRequestDto

- POST /{postId}/like — Like a post
  - [Authorize]
  - Route: postId (guid)
  - Response: result from LikeService.LikePostAsync


5) CommentController — route prefix: `/api/v1/comments`

- POST /{commentId}/like — Like a comment
  - [Authorize]
  - Route: commentId (guid)

- POST / — Comment on a post
  - [Authorize]
  - Body: CreateCommentDto

- PUT /{commentId} — Update a comment
  - [Authorize]
  - Body: string (comment text) — Note: controller accepts raw string body for update

- DELETE /{commentId} — Delete a comment
  - [Authorize]

- GET /{postId}/top — Get top-level (parent) comments for a post
  - [Authorize]
  - Route: postId (guid)

- GET /{parentCommentId}/childrens — Get child comments for a parent comment
  - [Authorize]
  - Route: parentCommentId (guid)


6) CarController — route prefix: `/api/v1` (note: many endpoints start with /cars)

- POST /cars — Create a new car
  - [Authorize(Policy = "DealerOnly")]
  - Body: CreateCarRequestDto (multipart/form-data)

- PUT /cars/{carId} — Update car
  - [Authorize(Policy = "DealerOnly")]
  - Body: UpdateCarDto (multipart/form-data)

- GET /cars — Get all cars (paged)
  - Public
  - Query: pageNum (int)

- GET /cars/search — Search cars
  - Public
  - Query params: CarSearchDto properties

- GET /cars/{carId} — Get car by id
  - Public
  - Route: carId (guid)

- DELETE /cars/{carId} — Delete car
  - [Authorize(Policy = "DealerOnly")]


Realtime (SignalR) Hub

- Hub route: `/hubs/post` (mapped in Program.cs: app.MapHub<PostHub>("/hubs/post"))
- Hub class: `GearUp.Infrastructure.SignalR.PostHub` (server-side)

Client-to-server hub methods (call from client):
- JoinGroup(Guid postId)
  - Call to join viewers of a particular post. Server will add caller to group named `post-{postId}`.
  - Usage: connection.invoke("JoinGroup", postId)

- JoinCommentsGroup(Guid postId)
  - Call to join comment viewers of a particular post. Server will add caller to group named `post-{postId}-comments`.
  - Usage: connection.invoke("JoinCommentsGroup", postId)

- LeaveCommentsGroup(Guid postId)
  - Remove caller from `post-{postId}-comments` group.
  - Usage: connection.invoke("LeaveCommentsGroup", postId)

- LeaveGroup(Guid postId)
  - Remove caller from `post-{postId}` group.
  - Usage: connection.invoke("LeaveGroup", postId)

Server-to-client events (server broadcasts to group):
- "CommentCreated" — Sent when a new comment is created on the post. Clients subscribed to `post-{postId}-comments` should refresh comments or fetch the new comment.
- "CommentLikeUpdated" — Sent when a comment's likes change for that post. Clients subscribed to `post-{postId}-comments` will receive this event.
- "PostLikeUpdated" — Sent when the post's like count changes. Clients subscribed to `post-{postId}` will receive this event.

Important notes about the realtime contract:
- The server currently sends the events without payloads (see SignalRRealTimeNotifier.SendAsync calls which only pass event names). The client should either request fresh data from the API on event receipt, or the backend can be extended to include payloads (e.g., the new comment DTO, updated counts).
- Group naming: 
  - `post-{postId}` — join this group to receive post-related events (like PostLikeUpdated)
  - `post-{postId}-comments` — join this group to receive comment-related events (like CommentCreated, CommentLikeUpdated)


Error handling & common response shape

- Many controllers use `ToApiResponse()` extension to wrap service results. Expect a common shape like:
  {
    "isSuccess": bool,
    "message": string,
    "data": object | null
  }
- Status codes reflect operation status (200, 201, 400, 401, 403, 404, 500). Inspect service layer for precise codes per operation.


Example: React client integration (short)

- Connect to hub (SignalR JS client):
  - Endpoint: `${API_BASE_URL.replace(/\/$/, '')}/hubs/post` (respecting HTTPS)
  - If using cookies for auth, configure SignalR to send credentials. Example options: { withCredentials: true } when establishing connection (or pass access token via accessTokenFactory if using Authorization header).

- Example flow for viewing post details:
  1. Establish signalR connection
  2. Invoke `JoinGroup(postId)` to receive post-related events
  3. Invoke `JoinCommentsGroup(postId)` to receive comment-related events
  4. Listen for `CommentCreated`, `CommentLikeUpdated`, `PostLikeUpdated` events and refresh post/comments via REST API endpoints.
  5. When leaving the page, invoke `LeaveGroup(postId)`, `LeaveCommentsGroup(postId)` and stop the connection.


Appendix — DTO names used by controllers

- Auth: LoginRequestDto, RegisterRequestDto, PasswordResetReqDto
- Admin: AdminLoginRequestDto, kycRequestDto
- User: UpdateUserRequestDto, KycRequestDto
- Post: CreatePostRequestDto, PostResponseDto, PostCountsDto
- Comment: CreateCommentDto
- Car: CreateCarRequestDto, UpdateCarDto, CarSearchDto

(For precise property definitions open files under `GearUp.Application/ServiceDtos/` if you need field-level docs.)


Next steps and optional improvements

- Add example request/response JSON for each DTO (I can generate these by scanning the DTO files).
- Add an OpenAPI/Swagger export snapshot (Program.cs already registers Swagger in Development).
- Extend SignalR server to send payloads with events to reduce extra REST calls from clients.


Generated on: (auto-generated) — use as a living document and update if routes change.


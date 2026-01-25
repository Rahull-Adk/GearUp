import React, {useEffect, useState, useRef} from 'react'
import * as signalR from '@microsoft/signalr'

export default function App({ initialToken = '' }) {
    const [isPostHubConnected, setIsPostHubConnected] = useState(false)
    const [isNotificationHubConnected, setIsNotificationHubConnected] = useState(false)
    const [postId, setPostId] = useState('')
    const [commentId, setCommentId] = useState('')
    const [messages, setMessages] = useState([])
    const [token, setToken] = useState(initialToken)
    const [receiverId, setReceiverId] = useState('')
    const [messageText, setMessageText] = useState('')
    const postConnectionRef = useRef(null)
    const notificationConnectionRef = useRef(null)

    useEffect(() => {
        // Ask for browser notification permission once
        if (typeof window !== 'undefined' && 'Notification' in window) {
            if (Notification.permission === 'default') {
                Notification.requestPermission().then(() => {})
            }
        }
    }, [])

    // PostHub connection (no auth required)
    useEffect(() => {
        const conn = new signalR.HubConnectionBuilder()
            .withUrl('http://localhost:5255/hubs/post', token ? { accessTokenFactory: () => token } : { withCredentials: true })
            .withAutomaticReconnect()
            .configureLogging(signalR.LogLevel.Information)
            .build()

        const pushMessage = (msg) => {
            setMessages(m => [...m, msg])
            // browser notification
            try {
                if (typeof window !== 'undefined' && 'Notification' in window && Notification.permission === 'granted') {
                    new Notification(msg.type, { body: msg.text })
                }
            } catch (e) {
                console.warn('Notification failed', e)
            }
        }

        conn.on('CommentCreated', (payload) => {
            console.info('CommentCreated payload:', payload)
            const id = payload?.id ?? payload?.Id
            const content = payload?.content ?? payload?.Content ?? payload?.text ?? payload?.Text ?? 'A new comment was added'
            pushMessage({type: 'CommentAdded', text: `${content} (commentId: ${id ?? 'unknown'})`, data: payload})
        })

        conn.on('CommentLikeUpdated', (payload) => {
            console.info('CommentLikeUpdated payload:', payload)
            const id = payload?.commentId ?? payload?.CommentId
            const count = payload?.likeCount ?? payload?.LikeCount ?? payload?.like_count ?? 'unknown'
            pushMessage({type: 'UpdatedCommentLike', text: `Comment ${id} like updated — ${count} likes`, data: payload})
        })

        conn.on('PostLikeUpdated', (payload) => {
            console.info('PostLikeUpdated payload:', payload)
            const id = payload?.postId ?? payload?.PostId
            const count = payload?.likeCount ?? payload?.LikeCount ?? 'unknown'
            pushMessage({type: 'UpdatedPostLike', text: `Post ${id} like updated — ${count} likes`, data: payload})
        })

        conn.onreconnected(() => {
            console.info('PostHub Reconnected')
            setIsPostHubConnected(true)
        })

        conn.onreconnecting(() => {
            console.warn('PostHub Reconnecting...')
            setIsPostHubConnected(false)
        })

        conn.onclose(() => {
            console.warn('PostHub Connection closed')
            setIsPostHubConnected(false)
        })

        conn.start().then(() => {
            console.info('Connected to PostHub')
            setIsPostHubConnected(true)
            pushMessage({type: 'info', text: 'Connected to PostHub'})
        }).catch(err => console.error('PostHub SignalR start failed', err))

        postConnectionRef.current = conn

        return () => {
            if (postConnectionRef.current) {
                postConnectionRef.current.stop().catch(() => {})
            }
        }
    }, [token])

    // NotificationHub connection (requires auth)
    useEffect(() => {
        if (!token) {
            console.info('No token provided, NotificationHub not connected (requires auth)')
            setIsNotificationHubConnected(false)
            return
        }

        const conn = new signalR.HubConnectionBuilder()
            .withUrl('http://localhost:5255/hubs/notification', { accessTokenFactory: () => token })
            .withAutomaticReconnect()
            .configureLogging(signalR.LogLevel.Information)
            .build()

        const pushMessage = (msg) => {
            setMessages(m => [...m, msg])
            try {
                if (typeof window !== 'undefined' && 'Notification' in window && Notification.permission === 'granted') {
                    new Notification(msg.type, { body: msg.text })
                }
            } catch (e) {
                console.warn('Notification failed', e)
            }
        }

        conn.on('NotificationCreated', (payload) => {
            console.info('NotificationCreated payload:', payload)
            const title = payload?.title ?? payload?.Title ?? 'New notification'
            const typeNum = payload?.notificationType ?? payload?.NotificationType ?? 0

            // Map notification type numbers to readable names
            const notificationTypes = {
                0: 'Default',
                1: 'PostLiked',
                2: 'PostCommented',
                3: 'CommentReplied',
                4: 'CommentLiked',
                5: 'KycInfo',
                6: 'AppointmentRequested',
                7: 'AppointmentAccepted',
                8: 'AppointmentRejected'
            }
            const typeName = notificationTypes[typeNum] || `Unknown(${typeNum})`
            pushMessage({type: `🔔 ${typeName}`, text: title, data: payload})
        })

        conn.on('MessageReceived', (payload) => {
            console.info('MessageReceived payload:', payload)
            const senderName = payload?.senderName ?? payload?.SenderName ?? 'Someone'
            const text = payload?.text ?? payload?.Text ?? 'sent you a message'
            const conversationId = payload?.conversationId ?? payload?.ConversationId
            pushMessage({type: `💬 Message from ${senderName}`, text: text, data: payload})
        })

        conn.onreconnected(() => {
            console.info('NotificationHub Reconnected')
            setIsNotificationHubConnected(true)
        })

        conn.onreconnecting(() => {
            console.warn('NotificationHub Reconnecting...')
            setIsNotificationHubConnected(false)
        })

        conn.onclose(() => {
            console.warn('NotificationHub Connection closed')
            setIsNotificationHubConnected(false)
        })

        conn.start().then(() => {
            console.info('Connected to NotificationHub')
            setIsNotificationHubConnected(true)
            pushMessage({type: 'info', text: 'Connected to NotificationHub (authenticated)'})
        }).catch(err => {
            console.error('NotificationHub SignalR start failed', err)
            pushMessage({type: 'error', text: 'Failed to connect to NotificationHub - check your JWT token'})
        })

        notificationConnectionRef.current = conn

        return () => {
            if (notificationConnectionRef.current) {
                notificationConnectionRef.current.stop().catch(() => {})
            }
        }
    }, [token])

    // Join only the post group
    const joinPostGroup = async () => {
        if (!postId) return alert('Enter post id (guid)')
        if (!postConnectionRef.current || !isPostHubConnected) {
            alert('PostHub not connected yet')
            return
        }

        try {
            console.info(`Invoking JoinGroup for postId=${postId}`)
            await postConnectionRef.current.invoke('JoinGroup', postId)
            setMessages(m => [...m, {type: 'info', text: `Joined group post-${postId}`}])
        } catch (err) {
            console.log(err)
            alert('Failed to join post group')
        }
    }

    // Join comments group for the post
    const joinCommentsGroup = async () => {
        if (!postId) return alert('Enter post id (guid)')
        if (!postConnectionRef.current || !isPostHubConnected) {
            alert('PostHub not connected yet')
            return
        }

        try {
            console.info(`Invoking JoinCommentsGroup for postId=${postId}`)
            await postConnectionRef.current.invoke('JoinCommentsGroup', postId)
            setMessages(m => [...m, {type: 'info', text: `Joined group post-${postId}-comments`}])
        } catch (err) {
            console.log(err)
            alert('Failed to join comments group')
        }
    }

    const leavePostGroup = async () => {
        if (!postId) return alert('Enter post id (guid)')
        try {
            await postConnectionRef.current.invoke('LeaveGroup', postId)
            setMessages(m => [...m, {type: 'info', text: `Left group post-${postId}`}])
        } catch (err) {
            console.error(err)
            alert('Failed to leave post group')
        }
    }

    const leaveCommentsGroup = async () => {
        if (!postId) return alert('Enter post id (guid)')
        try {
            await postConnectionRef.current.invoke('LeaveCommentsGroup', postId)
            setMessages(m => [...m, {type: 'info', text: `Left group post-${postId}-comments`}])
        } catch (err) {
            console.error(err)
            alert('Failed to leave comments group')
        }
    }

    // Send a message to another user via REST API
    const sendMessage = async () => {
        if (!receiverId) return alert('Enter receiver id (dealer or customer guid)')
        if (!messageText) return alert('Enter a message')
        if (!token) return alert('JWT token is required to send messages')

        try {
            const response = await fetch('http://localhost:5255/api/v1/messages', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${token}`
                },
                body: JSON.stringify({
                    receiverId: receiverId,
                    text: messageText
                })
            })

            const data = await response.json()

            if (response.ok) {
                setMessages(m => [...m, {type: '✅ Message Sent', text: `Sent to ${receiverId}: ${messageText}`, data: data.data}])
                setMessageText('')
            } else {
                setMessages(m => [...m, {type: '❌ Send Failed', text: data.message || 'Failed to send message', data: data}])
            }
        } catch (err) {
            console.error('Failed to send message:', err)
            alert('Failed to send message: ' + err.message)
        }
    }

    // Get conversations
    const getConversations = async () => {
        if (!token) return alert('JWT token is required')

        try {
            const response = await fetch('http://localhost:5255/api/v1/messages/conversations', {
                headers: {
                    'Authorization': `Bearer ${token}`
                }
            })

            const data = await response.json()
            setMessages(m => [...m, {type: '📋 Conversations', text: `Found ${data.data?.length || 0} conversations`, data: data.data}])
        } catch (err) {
            console.error('Failed to get conversations:', err)
            alert('Failed to get conversations: ' + err.message)
        }
    }

    // Local test helpers so you can simulate events. If you provide commentId it will be used.
    const sendLocalTest = (type) => {
        if (type === 'post-like') {
            setMessages(m => [...m, {type: 'UpdatedPostLike', text: 'Test: someone liked a post'}])
            if (Notification.permission === 'granted') new Notification('Post liked', { body: 'Test: someone liked a post' })
        } else if (type === 'comment-like') {
            const id = commentId || 'test-comment-id'
            setMessages(m => [...m, {type: 'UpdatedCommentLike', text: `Test: someone liked comment ${id}`, data: { commentId: id, likeCount: Math.floor(Math.random() * 10) }}])
            if (Notification.permission === 'granted') new Notification('Comment liked', { body: `Test: someone liked comment ${id}` })
        } else if (type === 'comment-added') {
            const cid = commentId || 'test-comment-id'
            const dto = { id: cid, postId: postId || 'test-post-id', content: 'This is a test comment', commentedUserId: 'test-user', commentedUserName: 'Tester' }
            setMessages(m => [...m, {type: 'CommentAdded', text: `Test: comment ${cid} added to post ${dto.postId}`, data: dto}])
            if (Notification.permission === 'granted') new Notification('New comment', { body: `Test comment on post ${dto.postId}` })
        }
    }

    return (
        <div style={{padding: 20, fontFamily: 'Arial, sans-serif'}}>
            <h1>GearUp Realtime Test</h1>

            <div style={{marginBottom: 12}}>
                <label style={{display: 'block', marginBottom: 6}}>JWT Access Token (optional for auth):</label>
                <input placeholder="paste jwt here (optional)" value={token} onChange={e => setToken(e.target.value)}
                       style={{width: '80%'}} />
            </div>

            <div style={{marginBottom: 8}}>
                <input placeholder="post id (guid)" value={postId} onChange={e => setPostId(e.target.value)}
                       style={{width: 400}}/>
                <button onClick={joinPostGroup} style={{marginLeft: 8}}>Join Post Group</button>
                <button onClick={leavePostGroup} style={{marginLeft: 8}}>Leave Post Group</button>
            </div>

            <div style={{marginBottom: 8}}>
                <input placeholder="comment id (guid) - optional for tests" value={commentId} onChange={e => setCommentId(e.target.value)}
                       style={{width: 400}}/>
                <button onClick={joinCommentsGroup} style={{marginLeft: 8}}>Join Comments Group</button>
                <button onClick={leaveCommentsGroup} style={{marginLeft: 8}}>Leave Comments Group</button>
            </div>

            <div style={{marginTop: 14}}>
                <strong>PostHub:</strong> {isPostHubConnected ? '✅ Connected' : '❌ Disconnected'}
                <span style={{marginLeft: 16}}><strong>NotificationHub:</strong> {isNotificationHubConnected ? '✅ Connected' : token ? '❌ Disconnected' : '⚠️ No token (auth required)'}</span>
            </div>

            <div style={{marginTop: 20, padding: 16, backgroundColor: '#f5f5f5', borderRadius: 8}}>
                <h3>💬 Messaging (Customer ↔ Dealer)</h3>
                <div style={{marginBottom: 8}}>
                    <input
                        placeholder="receiver id (dealer or customer guid)"
                        value={receiverId}
                        onChange={e => setReceiverId(e.target.value)}
                        style={{width: 400}}
                    />
                </div>
                <div style={{marginBottom: 8}}>
                    <input
                        placeholder="message text"
                        value={messageText}
                        onChange={e => setMessageText(e.target.value)}
                        style={{width: 400}}
                        onKeyPress={e => e.key === 'Enter' && sendMessage()}
                    />
                    <button onClick={sendMessage} style={{marginLeft: 8}}>Send Message</button>
                </div>
                <button onClick={getConversations}>Get My Conversations</button>
                <div style={{marginTop: 8, color: '#666', fontSize: 13}}>
                    Note: Messages can only be sent between customers and dealers. Both users need to be authenticated.
                </div>
            </div>

            <div style={{marginTop: 20}}>
                <h3>Quick tests (local)</h3>
                <button onClick={() => sendLocalTest('post-like')}>Simulate Post Like</button>
                <button onClick={() => sendLocalTest('comment-like')} style={{marginLeft: 6}}>Simulate Comment Like</button>
                <button onClick={() => sendLocalTest('comment-added')} style={{marginLeft: 6}}>Simulate Comment Added</button>
                <div style={{marginTop: 8, color: '#666', fontSize: 13}}>Use the comment id input so simulated events include a comment id.</div>
            </div>

            <div style={{marginTop: 20}}>
                <h3>Messages</h3>
                <ul>
                    {messages.map((m, idx) => (
                        <li key={idx}><strong>{m.type}:</strong> {m.text} {m.data ? <pre style={{display:'inline', marginLeft:6}}>{JSON.stringify(m.data)}</pre> : null}</li>
                    ))}
                </ul>
            </div>
        </div>
    )
}

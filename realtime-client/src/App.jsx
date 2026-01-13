import React, {useEffect, useState, useRef} from 'react'
import * as signalR from '@microsoft/signalr'

export default function App() {
    const [isConnected, setIsConnected] = useState(false)
    const [postId, setPostId] = useState('')
    const [messages, setMessages] = useState([])
    const connectionRef = useRef(null)

    useEffect(() => {
        const conn = new signalR.HubConnectionBuilder()
            .withUrl('http://localhost:5255/hubs/post', {withCredentials: true})
            .withAutomaticReconnect()
            .configureLogging(signalR.LogLevel.Information)
            .build()

        conn.on('CommentAdded', () => {
            setMessages(m => [...m, {type: 'CommentAdded', text: 'A new comment was added'}])
            console.info('CommentAdded received')
        })

        conn.on('UpdatedCommentLike', () => {
            setMessages(m => [...m, {type: 'UpdatedCommentLike', text: 'Comment like updated'}])
            console.info('UpdatedCommentLike received')
        })

        conn.on('UpdatedPostLike', () => {
            setMessages(m => [...m, {type: 'UpdatedPostLike', text: 'Post like updated'}])
            console.info('UpdatedPostLike received')
        })

        conn.onreconnected(() => {
            console.info('Reconnected')
            setIsConnected(true)
        })

        conn.onreconnecting(() => {
            console.warn('Reconnecting...')
            setIsConnected(false)
        })

        conn.onclose(() => {
            console.warn('Connection closed')
            setIsConnected(false)
        })

        conn.start().then(() => {
            console.info('Connected to PostHub')
            setIsConnected(true)
        }).catch(err => console.error(err))

        connectionRef.current = conn

        return () => {
            if (connectionRef.current) {
                connectionRef.current.stop()
            }
        }
    }, [])

    const joinGroup = async () => {
        if (!postId) return alert('Enter post id (guid)')
        if (!connectionRef.current || !isConnected) {
            alert('SignalR not connected yet')
            return
        }

        try {
            await connectionRef.current.invoke('JoinGroup', postId)
            setMessages(m => [...m, {type: 'info', text: `Joined group post-${postId}`}])
        } catch (err) {
            console.log(err)
            alert('Failed to join group')
        }
    }


    const leaveGroup = async () => {
        if (!postId) return alert('Enter post id (guid)')
        try {
            await connectionRef.current.invoke('LeaveGroup', postId)
            setMessages(m => [...m, {type: 'info', text: `Left group post-${postId}`}])
        } catch (err) {
            console.error(err)
            alert('Failed to leave group')
        }
    }

    return (
        <div style={{padding: 20}}>
            <h1>GearUp Realtime Test</h1>
            <div>
                <input placeholder="post id (guid)" value={postId} onChange={e => setPostId(e.target.value)}
                       style={{width: 400}}/>
                <button onClick={joinGroup}>Join</button>
                <button onClick={leaveGroup}>Leave</button>
            </div>

            <div style={{marginTop: 20}}>
                <h3>Messages</h3>
                <ul>
                    {messages.map((m, idx) => (
                        <li key={idx}>{m.type}: {m.text}</li>
                    ))}
                </ul>
            </div>
        </div>
    )
}


import { useEffect, useRef, useState } from "react";
import { jwtDecode } from "jwt-decode";
import './css/PostsPage.css';

import heartIcon from '../assets/heart-svgrepo-com.svg';
import commentIcon from '../assets/comment-alt-dots-svgrepo-com.svg';
import arrowDownIcon from '../assets/arrow-down-circle-svgrepo-com.svg';

export default function PostsPage({ token, onCreatePost }) {
    const [posts, setPosts] = useState([]);
    const [likesMap, setLikesMap] = useState({});
    const [commentsMap, setCommentsMap] = useState({});
    const [commentsVisible, setCommentsVisible] = useState({});
    const [newComments, setNewComments] = useState({});
    const [allPosts, setAllPosts] = useState([]);
    const [visibleCount, setVisibleCount] = useState(5); // post that are loaded
    const [usersMap, setUsersMap] = useState({});
    const isLoading = useRef(false);

    const userData = jwtDecode(token);

    useEffect(() => {
        if (!token || isLoading.current) return;

        const fetchAllPosts = async () => {
            isLoading.current = true;

            try {
                const res = await fetch('http://localhost:5125/api/gateway/posts/list', {
                    headers: { Authorization: `Bearer ${token}` }
                });
                const data = await res.json();
                const reversed = data.reverse(); // new posts should be first
                
                setAllPosts(reversed);
                
                await fetchAuthorsMap(reversed);
                
                const initialSlice = reversed.slice(0, 5);
                setPosts(initialSlice);
                
                await fetchLikesForPosts(initialSlice);
            } catch (err) {
                console.error("Error fetching posts", err);
            } finally {
                isLoading.current = false;
            }
        };

        fetchAllPosts();
    }, [token]);

    const fetchAuthorsMap = async (postsList) => {
        const uniqueUserIds = [...new Set(postsList.map(p => p.user_id))];
        const newUsersMap = { ...usersMap };

        for (const userId of uniqueUserIds) {
            if (newUsersMap[userId]) continue;

            try {
                const res = await fetch(`http://localhost:5125/api/gateway/user/${userId}`, {
                    headers: { Authorization: `Bearer ${token}` }
                });
                const user = await res.json();
                newUsersMap[userId] = user.name || `User ${userId}`;
            } catch (err) {
                console.error(`Failed to fetch user ${userId}`, err);
                newUsersMap[userId] = `User ${userId}`; // fallback
            }
        }

        setUsersMap(newUsersMap);
    };

    const fetchLikesForPosts = async (postsList) => {
        const newLikesMap = { ...likesMap };

        for (let post of postsList) {
            try {
                const res = await fetch(`http://localhost:5125/api/gateway/posts/${post.id}/likes`, {
                    headers: { Authorization: `Bearer ${token}` }
                });
                const likes = await res.json();
                newLikesMap[post.id] = likes.length;
            } catch (err) {
                console.error(`Error fetching likes for post ${post.id}`, err);
                newLikesMap[post.id] = 0;
            }
            await delay(100); // this prevents overload
        }

        setLikesMap(newLikesMap);
    };

    const fetchCommentsForPost = async (postId) => {
        try {
            const res = await fetch(`http://localhost:5125/api/gateway/posts/${postId}/comments`, {
                headers: { Authorization: `Bearer ${token}` }
            });
            const comments = await res.json();

            const commentUserIds = [...new Set(comments.map(c => c.user_id))];
            const newUsersMap = { ...usersMap };

            for (const userId of commentUserIds) {
                if (!newUsersMap[userId]) {
                    try {
                        const userRes = await fetch(`http://localhost:5125/api/gateway/user/${userId}`, {
                            headers: { Authorization: `Bearer ${token}` }
                        });
                        const user = await userRes.json();
                        newUsersMap[userId] = user.name || `User ${userId}`;
                    } catch (err) {
                        console.error(`Failed to fetch user ${userId}`, err);
                        newUsersMap[userId] = `User ${userId}`; // fallback
                    }
                }
            }
            
            setUsersMap(newUsersMap);
            setCommentsMap(prev => ({ ...prev, [postId]: comments }));
        } catch (err) {
            console.error(`Error fetching comments for post ${postId}`, err);
            setCommentsMap(prev => ({ ...prev, [postId]: [] }));
        }
    };

    const delay = (ms) => new Promise(resolve => setTimeout(resolve, ms));

    const loadMorePosts = async () => {
        const nextCount = visibleCount + 5;
        const nextSlice = allPosts.slice(visibleCount, nextCount);

        setPosts(prev => [...prev, ...nextSlice]);
        setVisibleCount(nextCount);
        await fetchLikesForPosts(nextSlice);
    };

    const toggleLike = async (postId) => {
        try {
            await fetch(`http://localhost:5125/api/gateway/posts/${postId}/like`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${token}`
                },
                body: JSON.stringify({
                    id: 0,
                    post_id: postId,
                    user_id: userData.sub,
                    created_at: new Date().toISOString()
                })
            });

            const res = await fetch(`http://localhost:5125/api/gateway/posts/${postId}/likes`, {
                headers: { Authorization: `Bearer ${token}` }
            });
            const likes = await res.json();
            setLikesMap(prev => ({ ...prev, [postId]: likes.length }));
        } catch (err) {
            console.error("Failed to like/unlike post", err);
        }
    };

    const toggleComments = async (postId) => {
        const isShown = commentsVisible[postId];
        if (!isShown && !commentsMap[postId]) {
            await fetchCommentsForPost(postId);
        }
        setCommentsVisible(prev => ({ ...prev, [postId]: !isShown }));
    };

    const submitComment = async (postId) => {
        const text = newComments[postId];
        if (!text?.trim()) return;

        try {
            await fetch(`http://localhost:5125/api/gateway/posts/${postId}/comment`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${token}`
                },
                body: JSON.stringify({
                    id: 0,
                    post_id: postId,
                    user_id: userData.sub,
                    text: text.trim(),
                    created_at: new Date().toISOString()
                })
            });

            setNewComments(prev => ({ ...prev, [postId]: '' }));
            await fetchCommentsForPost(postId);
        } catch (err) {
            console.error("Failed to submit comment", err);
        }
    };

    return (
        <div className="posts-container">
            <h2>Posts Feed</h2>
            <button onClick={onCreatePost} style={{ marginBottom: "25px" }}>
                ➕ Create New Post
            </button>

            {posts.length === 0 ? (
                <p>No posts found.</p>
            ) : (
                posts.map(post => (
                    <div key={post.id} className="post-card">
                        <div><strong>Post {post.id}</strong></div>
                        <div><strong>Posted by: {usersMap[post.user_id] || `User ${post.user_id}`}</strong></div>
                        <p>{post.text}</p>
                        {post.image_url && (
                            <img className="postimage" src={post.image_url} alt="Post image" style={{ maxWidth: '300px' }} />
                        )}
                        <div className="post-date">
                            Posted at: {new Date(post.created_at).toLocaleString()}
                        </div>
                        <div className="post-actions">
                            <button onClick={() => toggleLike(post.id)} style={{cursor: "pointer", marginRight: "10px"}}>
                                <img src={heartIcon} alt="Like" className="svg" style={{width: '18px', verticalAlign: 'middle', marginRight: '6px', transform: 'translateY(-1px)'}}/>
                                {likesMap[post.id] ?? 0} likes
                            </button>
                            <button onClick={() => toggleComments(post.id)} style={{cursor: "pointer"}}>
                                <img src={commentIcon} alt="Comment" style={{width: '22px', verticalAlign: 'middle', marginRight: '4px'}}/>
                                View comments
                            </button>
                        </div>

                        {commentsVisible[post.id] && (
                            <div className="comments-section" style={{ marginTop: "10px", paddingLeft: "10px" }}>
                                <h4>Comments:</h4>
                                {commentsMap[post.id]?.length > 0 ? (
                                    commentsMap[post.id].map(comment => (
                                        <div key={comment.id} className="comment" style={{ marginBottom: "8px" }}>
                                            <strong>{usersMap[comment.user_id] || `User ${comment.user_id}`}:</strong> {comment.text}
                                            <div style={{ fontSize: "0.85em", color: "#666" }}>
                                                {new Date(comment.created_at).toLocaleString()}
                                            </div>
                                        </div>
                                    ))
                                ) : (
                                    <p>No comments yet.</p>
                                )}

                                <div className="comment-input-container">
                                    <input
                                        type="text"
                                        value={newComments[post.id] || ''}
                                        onChange={(e) =>
                                            setNewComments(prev => ({ ...prev, [post.id]: e.target.value }))
                                        }
                                        placeholder="Write your comment here..."
                                        style={{ padding: "5px", width: "70%" }}
                                    />
                                    <button
                                        onClick={() => submitComment(post.id)}
                                        style={{marginLeft: "10px", padding: "5px 10px"}}
                                    >
                                        <img src={commentIcon} alt="Comment"style={{width: '18px', verticalAlign: 'middle', marginRight: '4px', transform: 'translateY(-1px)'}}/>
                                        Comment
                                    </button>
                                </div>
                            </div>
                        )}
                    </div>
                ))
            )}

            {visibleCount < allPosts.length && (
                <button onClick={loadMorePosts} style={{marginTop: "20px"}}>
                    <img src={arrowDownIcon} alt="Load more"style={{width: '18px', verticalAlign: 'middle', marginRight: '6px', transform: 'translateY(-1.5px)'}}/>
                    Load More
                </button>
            )}
        </div>
    );
}
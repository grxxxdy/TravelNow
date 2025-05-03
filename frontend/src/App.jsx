import {useEffect, useState} from 'react'
import './App.css'

import Header from './components/Header.jsx';
import LoginForm from "./components/LoginForm.jsx";
import PostsPage from "./components/PostsPage.jsx";
import ProfilePage from "./components/ProfilePage.jsx";
import CreatePostPage from "./components/CreatePostPage.jsx";
import {jwtDecode} from "jwt-decode";

function App() {
    const [token, setToken] = useState(null);
    const [page, setPage] = useState('login');
    
    // Check token
    useEffect(() => {
        const savedToken = localStorage.getItem('token');
        if (savedToken) {
            try {
                const decoded = jwtDecode(savedToken);
                const exp = decoded.exp * 1000;
                const now = Date.now();
                if (exp > now) {
                    setToken(savedToken);
                    setPage('posts');
                } else {
                    localStorage.removeItem('token');
                    setToken(null);
                    setPage('login');
                }
            } catch (e) {
                console.error("Invalid token:", e);
                localStorage.removeItem('token');
                setToken(null);
                setPage('login');
            }
        }
    }, []);
    const handleLogin = (newToken) => {
        localStorage.setItem('token', newToken);
        setToken(newToken);
        setPage('posts');
    };
    
    const handleLogout = () => {
        localStorage.removeItem('token');
        setToken(null);
        setPage('login');
    };
    
    const renderContent = () => {
        if (!token) return <LoginForm onLogin={handleLogin} />;
        
        switch (page) {
            case 'posts':
                return <PostsPage token={token} onCreatePost = {() => setPage('create')} />;
            case 'profile':
                return <ProfilePage token={token} />;
            case 'create':
                return <CreatePostPage token={token} onPostCreated={() => setPage('posts')}/>;
            default:
                return <PostsPage token={token} />;
        }
    };

    return (
        <div>
            {token && <Header onLogout={handleLogout} onProfile={() => setPage('profile')} onHome={() => setPage('posts')} />}
            {renderContent()}
        </div>
    );
}

export default App

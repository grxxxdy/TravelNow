import { useState } from 'react'
import "./css/LoginForm.css"

export default function LoginForm({ onLogin }) {
    // Fields
    const [name, setName] = useState('');
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [confirmPassword, setConfirmPassword] = useState('');

    const [isRegistering, setIsRegistering] = useState(false);
    
    // On submit
    const handleSubmit = async (e) => {
        e.preventDefault();

        if (isRegistering) {
            // validation
            if (!name || !email || !password) {
                alert("All fields are required.");
                return;
            }

            if (password !== confirmPassword) {
                alert("Passwords do not match.");
                return;
            }

            // Register user
            const response = await fetch('http://localhost:5125/api/gateway/user/register', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    id: 0,
                    name,
                    email,
                    password,
                    role: "user",
                    created_at: new Date().toISOString()
                })
            });

            const data = await response.json();

            if (data.success) {
                alert("Registration successful! You can now log in.");
                setIsRegistering(false);
                setPassword('');
                setConfirmPassword('');
            } else {
                alert("Registration failed: " + data.message);
            }
        } else {
            // Login
            const response = await fetch('http://localhost:5125/api/gateway/user/login', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ email, password })
            });

            const data = await response.json();

            if (data.success) {
                onLogin(data.message);
            } else {
                alert("Login failed: " + data.message);
            }
        }
    };

    return (
        <form onSubmit={handleSubmit} className="loginform">
            <h2>{isRegistering ? "Register" : "Login"}</h2>
            
            {isRegistering && (
                <>
                    <input
                        type="text"
                        placeholder="Name"
                        value={name}
                        onChange={e => setName(e.target.value)}
                        style={{ marginBottom: 10 }}
                    /><br />
                </>
            )}

            <input
                type="email"
                placeholder="Email"
                value={email}
                onChange={e => setEmail(e.target.value)}
                style={{ marginBottom: 10 }}
            /><br />

            <input
                type="password"
                placeholder="Password"
                value={password}
                onChange={e => setPassword(e.target.value)}
                style={{ marginBottom: 10 }}
            /><br />

            {isRegistering && (
                <>
                    <input
                        type="password"
                        placeholder="Confirm Password"
                        value={confirmPassword}
                        onChange={e => setConfirmPassword(e.target.value)}
                        style={{ marginBottom: 10 }}
                    /><br />
                </>
            )}
            
            <button type="submit">{isRegistering ? "Register" : "Login"}</button>

            <p style={{marginTop: 10}}>
                {isRegistering ? (
                    <>
                        Already have an account?{" "}
                        <span onClick={() => setIsRegistering(false)}
                              style={{color: "blue", cursor: "pointer"}}>Log in</span>
                    </>
                ) : (
                    <>
                        Don't have an account?{" "}
                        <span onClick={() => setIsRegistering(true)}
                              style={{color: "blue", cursor: "pointer"}}>Register</span>
                    </>
                )}
            </p>
        </form>
    );
}


import { useEffect, useState } from "react";
import { jwtDecode } from "jwt-decode";
import "./css/ProfilePage.css";

export default function ProfilePage ({ token }) {
    if (!token) return <p>No token provided</p>;

    const userData = jwtDecode(token);

    const [isEditing, setIsEditing] = useState(false);
    const [isChangingPassword, setIsChangingPassword] = useState(false);
    
    const [name, setName] = useState(userData.name);
    const [email, setEmail] = useState(userData.email);
    const [existingPassword, setExistingPassword] = useState("");
    const [oldPasswordInput, setOldPasswordInput] = useState("");
    const [newPassword, setNewPassword] = useState("");
    
    const [message, setMessage] = useState("");

    useEffect(() => {
        const fetchPassword = async () => {
            const response = await fetch(`http://localhost:5125/api/gateway/user/${userData.sub}`, {
                method: "GET",
                headers: {
                    Authorization: `Bearer ${token}`,
                },
            });
            const data = await response.json();
            if (data && data.password) {
                setExistingPassword(data.password);
            }
        };
        fetchPassword();
    }, [userData.sub, token]);
    const handleSave = async () => {
        try {
            const response = await fetch(`http://localhost:5125/api/gateway/user/update/${userData.sub}`, {
                method: "PUT",
                headers: {
                    "Content-Type": "application/json",
                    "Authorization": `Bearer ${token}`
                },
                body: JSON.stringify({
                    id: userData.sub,
                    name: name,
                    email: email,
                    password: existingPassword,
                    role: userData.role,
                    created_at: userData.user_created_at
                })
            });

            const data = await response.json();

            if (data.success) {
                localStorage.setItem("token", data.data.token)
                window.location.reload();
                
                setMessage("Profile updated successfully.");
                setIsEditing(false);
            } else {
                setMessage("Update failed: " + data.data.message);
            }
        } catch (error) {
            setMessage("Error updating profile." + error.toString());
        }
    };

    const handlePasswordChange = async () => {
        if (oldPasswordInput !== existingPassword) {
            setMessage("Incorrect current password.");
            return;
        }

        try {
            const response = await fetch(`http://localhost:5125/api/gateway/user/update/${userData.sub}`, {
                method: "PUT",
                headers: {
                    "Content-Type": "application/json",
                    "Authorization": `Bearer ${token}`
                },
                body: JSON.stringify({
                    id: userData.sub,
                    name,
                    email,
                    password: newPassword,
                    role: userData.role,
                    created_at: userData.user_created_at
                })
            });

            const data = await response.json();

            if (data.success) {
                setMessage("Password changed successfully.");
                setIsChangingPassword(false);
                setExistingPassword(newPassword);
                setOldPasswordInput("");
                setNewPassword("");
                localStorage.setItem("token", data.data.token);
            } else {
                setMessage("Password change failed: " + data.message);
            }
        } catch (error) {
            setMessage("Error changing password.");
        }
    };

    return (
        <div className="profile-card">
            <h2>Your Profile</h2>
            {isEditing ? (
                <>
                    <input value={name} onChange={e => setName(e.target.value)} />
                    <input value={email} onChange={e => setEmail(e.target.value)} />
                    <button onClick={handleSave}>Save</button>
                    <button onClick={() => setIsEditing(false)}>Cancel</button>
                </>
            ) : (
                <>
                    <p>Name: {userData.name}</p>
                    <p>Email: {userData.email}</p>
                    <p>Role: {userData.role}</p>
                    <p>Register date: {new Date(userData.user_created_at).toLocaleString()}</p>
                    <button onClick={() => setIsEditing(true)}>Edit Profile</button>
                </>
            )}

            <hr />

            {isChangingPassword ? (
                <>
                    <input
                        type="password"
                        placeholder="Current password"
                        value={oldPasswordInput}
                        onChange={(e) => setOldPasswordInput(e.target.value)}
                    />
                    <input
                        type="password"
                        placeholder="New password"
                        value={newPassword}
                        onChange={(e) => setNewPassword(e.target.value)}
                    />
                    <button onClick={handlePasswordChange}>Change Password</button>
                    <button onClick={() => setIsChangingPassword(false)}>Cancel</button>
                </>
            ) : (
                <button onClick={() => setIsChangingPassword(true)}>Change Password</button>
            )}

            {message && <p>{message}</p>}
        </div>
    );
}
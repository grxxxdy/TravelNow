import { useState } from "react";
import "./css/CreatePostPage.css";

export default function CreatePostPage({ token, onPostCreated }) {
    const [text, setText] = useState('');
    const [imageFile, setImageFile] = useState(null);
    const [previewUrl, setPreviewUrl] = useState(null);

    const handleImageChange = (e) => {
        const file = e.target.files[0];
        if (file) {
            setImageFile(file);
            setPreviewUrl(URL.createObjectURL(file));
        }
    };
    const handleSubmit = async (e) => {
        e.preventDefault();

        const formData = new FormData();
        
        formData.append("text", text);
        formData.append("image_url", "q");      // is is overwritten later
        
        if (imageFile) {
            formData.append("image", imageFile);
        }
        
        const response = await fetch("http://localhost:5125/api/gateway/posts/create", {
            method: "POST",
            headers: {
                "Authorization": `Bearer ${token}`
            },
            body: formData
        });

        if (response.ok) {
            onPostCreated();
        } else {
            alert("Failed to create post");
        }
    };

    return (
        <form onSubmit={handleSubmit}>
            <h2>Create New Post</h2>
            <textarea
                value={text}
                onChange={e => setText(e.target.value)}
                placeholder="Write something..."
                rows="4"
                cols="50"
            />

            {previewUrl && (
                <div style={{ marginTop: "0px" }}>
                    <p>Image preview:</p>
                    <img src={previewUrl} alt="Preview" style={{ maxWidth: "100px", borderRadius: "8px", marginBottom: "10px" }} />
                </div>
            )}
            
            <label className="file-upload">
                Choose Image
                <input
                    type="file"
                    accept="image/*"
                    onChange={handleImageChange}
                    hidden
                />
            </label>
            
            <button type="submit">Create</button>
        </form>
    );
}
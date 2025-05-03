import './css/Header.css';

export default function Header({ onLogout, onProfile, onHome}) {
    return (
        <header className="header">
            <div className="header-element"></div>
            <div className="header-element">
                <a href={"/"} onClick={(e) => { 
                    e.preventDefault();
                    onHome();
                }}>TravelNow</a>
            </div>
            <div className="header-element buttons-wrapper">
                <button className="header-button" onClick={onProfile}>Profile</button>
                <button className="header-button" onClick={onLogout}>Logout</button>
            </div>
        </header>
    );
}
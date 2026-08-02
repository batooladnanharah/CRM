import "./Header.css";

function Header() {
  return (
    <header className="header">
      <div className="logo">
        <h1>My App</h1>
      </div>

      <nav className="nav">
        <a href="/">Home</a>
        <a href="/">About</a>
        <a href="/">Services</a>
        <a href="/">Contact</a>
      </nav>

      <button className="login-btn">Login</button>
    </header>
  );
}

export default Header;
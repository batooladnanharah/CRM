// Lesson 12: Multiple State Variables
// A component can hold several independent useState calls at once,
// each managing its own piece of unrelated data.

import { useState } from "react";

function MultipleState() {
  const [count, setCount] = useState(0);
  const [username, setUsername] = useState("");
  const [theme, setTheme] = useState<"light" | "dark">("light");

  return (
    <div>
      <p>Count: {count}</p>
      <button className="state-btn" onClick={() => setCount(count + 1)}>
        Add Count
      </button>

      <p>Username: {username || "(none)"}</p>
      <input
        className="state-input"
        type="text"
        placeholder="Username"
        value={username}
        onChange={(e) => setUsername(e.target.value)}
      />

      <p>Theme: {theme}</p>
      <button
        className="state-btn"
        onClick={() => setTheme(theme === "light" ? "dark" : "light")}
      >
        Toggle Theme
      </button>
    </div>
  );
}

export default MultipleState;

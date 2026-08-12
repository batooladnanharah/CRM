// Lesson 1: Basic Events (onClick, onMouseEnter, onFocus/onBlur, onKeyDown)

import { useState } from "react";

function handleClick() {
  // Defined outside the component since it doesn't need any state here.
  alert("Button clicked!");
}

function BasicEvents() {
  const [hovering, setHovering] = useState(false);
  const [focused, setFocused] = useState(false);
  const [lastKey, setLastKey] = useState("");

  return (
    <div>
      {/* onClick={handleClick} passes the FUNCTION ITSELF as the handler.
          React calls it later, only when the button is clicked.
          onClick={handleClick()} would call it immediately during render
          and pass its return value (undefined) as the handler instead. */}
      <button className="ef-btn" onClick={handleClick}>
        Click me (correct)
      </button>

      <div
        className={`ef-hover-box ${hovering ? "hovering" : ""}`}
        onMouseEnter={() => setHovering(true)}
        onMouseLeave={() => setHovering(false)}
        style={{ marginTop: 12 }}
      >
        {hovering ? "You're hovering!" : "Move your mouse here"}
      </div>

      <div style={{ marginTop: 12 }}>
        <input
          className={`ef-input ef-focus-input ${focused ? "focused" : "blurred"}`}
          placeholder="Focus me..."
          onFocus={() => setFocused(true)}
          onBlur={() => setFocused(false)}
        />
        <span>{focused ? "Focused" : "Not focused"}</span>
      </div>

      <div style={{ marginTop: 12 }}>
        <input
          className="ef-input"
          placeholder="Press Enter..."
          onKeyDown={(e) => {
            if (e.key === "Enter") {
              setLastKey("Enter was pressed!");
            }
          }}
        />
        <span>{lastKey}</span>
      </div>
    </div>
  );
}

export default BasicEvents;

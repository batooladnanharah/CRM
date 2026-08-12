// Lesson 2: The React Event Object
// Every event handler receives a "synthetic event" object with useful
// info about what happened. Typing it correctly gives us autocomplete
// and type safety on things like event.target.value.

import { useState } from "react";

function EventObject() {
  const [value, setValue] = useState("");
  const [lastKey, setLastKey] = useState("");
  const [clickInfo, setClickInfo] = useState("");

  // React.ChangeEvent<HTMLInputElement> describes an onChange event
  // coming from an <input> element.
  function handleChange(e: React.ChangeEvent<HTMLInputElement>) {
    setValue(e.target.value);
  }

  // React.KeyboardEvent<HTMLInputElement> gives us access to e.key.
  function handleKeyDown(e: React.KeyboardEvent<HTMLInputElement>) {
    setLastKey(e.key);
  }

  // React.MouseEvent<HTMLButtonElement> gives us mouse coordinates, etc.
  function handleClick(e: React.MouseEvent<HTMLButtonElement>) {
    setClickInfo(`Clicked at (${e.clientX}, ${e.clientY})`);
  }

  return (
    <div>
      <input
        className="ef-input"
        placeholder="Type here..."
        value={value}
        onChange={handleChange}
        onKeyDown={handleKeyDown}
      />
      <p>Current value: {value || "(empty)"}</p>
      <p>Last key pressed: {lastKey || "(none)"}</p>

      <button className="ef-btn" onClick={handleClick}>
        Click to see coordinates
      </button>
      <p>{clickInfo}</p>
    </div>
  );
}

export default EventObject;

// Lesson 5: Controlled Input
// The input's value is controlled by React state, so every keystroke
// updates state and re-renders the live preview below.

import { useState } from "react";

function ControlledInput() {
  const [text, setText] = useState("");

  return (
    <div>
      <input
        className="state-input"
        type="text"
        placeholder="Type something..."
        value={text}
        onChange={(e) => setText(e.target.value)}
      />
      <p>Live preview: {text}</p>
    </div>
  );
}

export default ControlledInput;

// Lesson 2: Toggle Example
// A boolean state variable flips between true and false to show/hide content.

import { useState } from "react";

function Toggle() {
  const [isVisible, setIsVisible] = useState(false);

  return (
    <div>
      <button className="state-btn" onClick={() => setIsVisible(!isVisible)}>
        {isVisible ? "Hide" : "Show"} Text
      </button>
      {isVisible && <p>👋 Here is the hidden text!</p>}
    </div>
  );
}

export default Toggle;

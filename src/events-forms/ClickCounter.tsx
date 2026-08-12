// Lesson 4: Click Counter
// Combines onClick with the "functional update" pattern from Phase 4:
// updating state based on the previous state is safer than reading
// the outer `count` variable directly, especially for rapid clicks.

import { useState } from "react";

function ClickCounter() {
  const [count, setCount] = useState(0);

  function handleIncrement() {
    setCount((prev) => prev + 1);
  }

  function handleReset() {
    setCount(0);
  }

  return (
    <div>
      <p className="count-display">Clicks: {count}</p>
      <button className="ef-btn" onClick={handleIncrement}>
        Click me
      </button>
      <button className="ef-btn danger" onClick={handleReset}>
        Reset
      </button>
    </div>
  );
}

export default ClickCounter;

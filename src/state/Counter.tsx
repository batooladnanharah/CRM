// Lesson 1: Counter
// useState creates a piece of state ("count") and a function to update it ("setCount").
// Calling setCount triggers React to re-render this component with the new value.

import { useState } from "react";

function Counter() {
  const [count, setCount] = useState(0);

  return (
    <div>
      <p className="count-display">{count}</p>
      <button className="state-btn" onClick={() => setCount(count + 1)}>
        Increase
      </button>
      <button className="state-btn" onClick={() => setCount(count - 1)}>
        Decrease
      </button>
      <button className="state-btn" onClick={() => setCount(0)}>
        Reset
      </button>
    </div>
  );
}

export default Counter;

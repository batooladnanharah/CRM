// Lesson 13: Functional Updates
// setCount(count + 1) reads "count" from the render that scheduled the update.
// If several updates fire before React re-renders (e.g. calling the setter
// multiple times in a row), they all use the same stale "count" and only the
// last one wins.
//
// setCount(prev => prev + 1) instead receives the latest state value directly
// from React, so each update builds correctly on the previous one.

import { useState } from "react";

function FunctionalUpdate() {
  const [count, setCount] = useState(0);

  function addThreeWrong() {
    // Bug: all three calls close over the same "count" value.
    setCount(count + 1);
    setCount(count + 1);
    setCount(count + 1);
  }

  function addThreeCorrect() {
    // Correct: each call gets the freshest value via "prev".
    setCount((prev) => prev + 1);
    setCount((prev) => prev + 1);
    setCount((prev) => prev + 1);
  }

  return (
    <div>
      <p className="count-display">{count}</p>
      <button className="state-btn" onClick={addThreeWrong}>
        +3 (wrong way, only adds 1)
      </button>
      <button className="state-btn" onClick={addThreeCorrect}>
        +3 (functional update, adds 3)
      </button>
      <button className="state-btn" onClick={() => setCount(0)}>
        Reset
      </button>
    </div>
  );
}

export default FunctionalUpdate;

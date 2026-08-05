// Lesson 11: Boolean State
// A simple true/false flag can enable or disable another element.

import { useState } from "react";

function BooleanState() {
  const [isEnabled, setIsEnabled] = useState(false);

  return (
    <div>
      <label>
        <input
          type="checkbox"
          checked={isEnabled}
          onChange={(e) => setIsEnabled(e.target.checked)}
        />
        {" "}Enable button
      </label>
      <br />
      <button className="state-btn" disabled={!isEnabled}>
        {isEnabled ? "Enabled" : "Disabled"}
      </button>
    </div>
  );
}

export default BooleanState;

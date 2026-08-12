// Lesson 5: Controlled Input
// A "controlled" input means React state is the single source of truth
// for its value. The input never manages its own value internally;
// every keystroke fires onChange, which updates state, which then
// re-renders the input with the new value.

import { useState } from "react";

function ControlledInput() {
  const [name, setName] = useState("");

  return (
    <div>
      <input
        className="ef-input"
        placeholder="Your name..."
        value={name}
        onChange={(e: React.ChangeEvent<HTMLInputElement>) => setName(e.target.value)}
      />
      <p>Hello, {name || "stranger"}!</p>
    </div>
  );
}

export default ControlledInput;

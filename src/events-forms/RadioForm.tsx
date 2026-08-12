// Lesson 8: Radio Buttons
// All radio inputs in a group share the same "name" so only one can
// be selected. Each is controlled by comparing its value to state.

import { useState } from "react";

function RadioForm() {
  const [gender, setGender] = useState("");

  const options = ["Male", "Female", "Other"];

  return (
    <div>
      {options.map((option) => (
        <label key={option} style={{ marginRight: 12 }}>
          <input
            type="radio"
            name="gender"
            value={option}
            checked={gender === option}
            onChange={(e) => setGender(e.target.value)}
          />{" "}
          {option}
        </label>
      ))}
      <p>Selected: {gender || "(none)"}</p>
    </div>
  );
}

export default RadioForm;

// Lesson 7: Checkbox
// A checkbox is controlled with a boolean state variable and the
// "checked" prop (not "value"), paired with onChange.

import { useState } from "react";

function CheckboxForm() {
  const [accepted, setAccepted] = useState(false);

  return (
    <div>
      <label>
        <input
          type="checkbox"
          checked={accepted}
          onChange={(e) => setAccepted(e.target.checked)}
        />{" "}
        I accept the terms and conditions
      </label>
      <p>{accepted ? "Thanks for accepting!" : "Please accept to continue."}</p>
    </div>
  );
}

export default CheckboxForm;

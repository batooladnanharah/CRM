// Lesson 9: Select Dropdown
// A <select> is controlled the same way as a text input: value + onChange.

import { useState } from "react";

const countries = ["Egypt", "Jordan", "Saudi Arabia", "UAE"];

function SelectForm() {
  const [country, setCountry] = useState("");

  return (
    <div>
      <select
        className="ef-select"
        value={country}
        onChange={(e: React.ChangeEvent<HTMLSelectElement>) => setCountry(e.target.value)}
      >
        <option value="">-- Choose a country --</option>
        {countries.map((c) => (
          <option key={c} value={c}>
            {c}
          </option>
        ))}
      </select>
      <p>Selected country: {country || "(none)"}</p>
    </div>
  );
}

export default SelectForm;

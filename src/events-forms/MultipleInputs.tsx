// Lesson 6: Multiple Inputs with One State Object
// Instead of one useState per field, we can keep a single object in
// state and update only the changed property using the spread
// operator. This avoids mutating the existing state directly.

import { useState } from "react";

type FormData = {
  name: string;
  email: string;
  age: string;
};

function MultipleInputs() {
  const [formData, setFormData] = useState<FormData>({
    name: "",
    email: "",
    age: "",
  });

  function handleChange(e: React.ChangeEvent<HTMLInputElement>) {
    const { name, value } = e.target;
    // Spread the previous state, then overwrite just the changed field.
    setFormData((prev) => ({ ...prev, [name]: value }));
  }

  return (
    <div>
      <input
        className="ef-input"
        name="name"
        placeholder="Name"
        value={formData.name}
        onChange={handleChange}
      />
      <input
        className="ef-input"
        name="email"
        placeholder="Email"
        value={formData.email}
        onChange={handleChange}
      />
      <input
        className="ef-input"
        name="age"
        placeholder="Age"
        value={formData.age}
        onChange={handleChange}
      />
      <p>
        {formData.name || "..."}, {formData.email || "..."}, {formData.age || "..."}
      </p>
    </div>
  );
}

export default MultipleInputs;

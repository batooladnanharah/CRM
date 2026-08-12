// Lesson 12: Basic Form Validation
// Validation happens in plain functions/state, no external library.
// We keep an "errors" object in state and check it on submit and
// display messages next to each field.

import { useState } from "react";

type Errors = {
  name?: string;
  email?: string;
  password?: string;
};

function FormValidation() {
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [errors, setErrors] = useState<Errors>({});
  const [success, setSuccess] = useState(false);

  function validate(): Errors {
    const newErrors: Errors = {};
    if (!name.trim()) newErrors.name = "Name is required.";
    if (!email.trim()) newErrors.email = "Email is required.";
    if (!password) {
      newErrors.password = "Password is required.";
    } else if (password.length < 8) {
      newErrors.password = "Password must be at least 8 characters.";
    }
    return newErrors;
  }

  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const newErrors = validate();
    setErrors(newErrors);
    setSuccess(Object.keys(newErrors).length === 0);
  }

  return (
    <form onSubmit={handleSubmit}>
      <div className="ef-field">
        <label>Name</label>
        <input className="ef-input" value={name} onChange={(e) => setName(e.target.value)} />
        {errors.name && <p className="ef-error">{errors.name}</p>}
      </div>

      <div className="ef-field">
        <label>Email</label>
        <input className="ef-input" value={email} onChange={(e) => setEmail(e.target.value)} />
        {errors.email && <p className="ef-error">{errors.email}</p>}
      </div>

      <div className="ef-field">
        <label>Password</label>
        <input
          className="ef-input"
          type="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
        />
        {errors.password && <p className="ef-error">{errors.password}</p>}
      </div>

      <button className="ef-btn" type="submit">
        Submit
      </button>

      {success && <p style={{ color: "#16a34a" }}>Form is valid!</p>}
    </form>
  );
}

export default FormValidation;

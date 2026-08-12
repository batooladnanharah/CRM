// Lesson 11: Form Submission
// event.preventDefault() stops the browser's default behavior of
// reloading the page when a form is submitted, so React can handle
// the submission itself.

import { useState } from "react";

type Submitted = {
  name: string;
  email: string;
  password: string;
};

function UserForm() {
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [submitted, setSubmitted] = useState<Submitted | null>(null);

  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setSubmitted({ name, email, password });
  }

  return (
    <div>
      <form onSubmit={handleSubmit}>
        <div className="ef-field">
          <label>Name</label>
          <input
            className="ef-input"
            value={name}
            onChange={(e) => setName(e.target.value)}
          />
        </div>
        <div className="ef-field">
          <label>Email</label>
          <input
            className="ef-input"
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
          />
        </div>
        <div className="ef-field">
          <label>Password</label>
          <input
            className="ef-input"
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
          />
        </div>
        <button className="ef-btn" type="submit">
          Submit
        </button>
      </form>

      {submitted && (
        <div className="ef-success-box">
          <p>Submitted!</p>
          <p>Name: {submitted.name}</p>
          <p>Email: {submitted.email}</p>
          <p>Password: {"*".repeat(submitted.password.length)}</p>
        </div>
      )}
    </div>
  );
}

export default UserForm;

// Lesson 6: User Form
// Two separate controlled inputs, each backed by its own state variable.

import { useState } from "react";

function UserForm() {
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");

  return (
    <div>
      <input
        className="state-input"
        type="text"
        placeholder="Name"
        value={name}
        onChange={(e) => setName(e.target.value)}
      />
      <input
        className="state-input"
        type="email"
        placeholder="Email"
        value={email}
        onChange={(e) => setEmail(e.target.value)}
      />
      <p>
        Preview: {name || "..."} ({email || "..."})
      </p>
    </div>
  );
}

export default UserForm;

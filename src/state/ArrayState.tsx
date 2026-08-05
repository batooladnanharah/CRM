// Lesson 10: Array State
// Similar to TodoList, but managing a list of user objects instead of strings.

import { useState } from "react";

interface Person {
  id: number;
  name: string;
}

function ArrayState() {
  const [users, setUsers] = useState<Person[]>([
    { id: 1, name: "Ali" },
    { id: 2, name: "Mona" },
  ]);
  const [name, setName] = useState("");

  function addUser() {
    if (name.trim() === "") return;
    setUsers([...users, { id: Date.now(), name }]);
    setName("");
  }

  function removeUser(id: number) {
    setUsers(users.filter((user) => user.id !== id));
  }

  return (
    <div>
      <input
        className="state-input"
        type="text"
        placeholder="New user"
        value={name}
        onChange={(e) => setName(e.target.value)}
      />
      <button className="state-btn" onClick={addUser}>
        Add User
      </button>
      <ul className="state-list">
        {users.map((user) => (
          <li key={user.id}>
            {user.name}
            <button className="state-btn" onClick={() => removeUser(user.id)}>
              Remove
            </button>
          </li>
        ))}
      </ul>
    </div>
  );
}

export default ArrayState;

// Lesson 9: Object State
// When state is an object, we must spread the old object and override
// only the changed field — otherwise the rest of the object would be lost.

import { useState } from "react";

interface User {
  name: string;
  city: string;
}

function ObjectState() {
  const [user, setUser] = useState<User>({ name: "Batool", city: "Riyadh" });

  function updateName(name: string) {
    setUser({ ...user, name });
  }

  function updateCity(city: string) {
    setUser({ ...user, city });
  }

  return (
    <div>
      <input
        className="state-input"
        type="text"
        value={user.name}
        onChange={(e) => updateName(e.target.value)}
      />
      <input
        className="state-input"
        type="text"
        value={user.city}
        onChange={(e) => updateCity(e.target.value)}
      />
      <p>
        {user.name} lives in {user.city}
      </p>
    </div>
  );
}

export default ObjectState;

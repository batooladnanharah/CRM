// Lesson 3: Passing Arguments to Event Handlers
// onClick expects a function, not the RESULT of calling a function.
// To pass a custom argument (like an id), we wrap the call in an
// arrow function: onClick={() => handleClick(id)}.
// This creates a new function that React calls on click, which then
// calls handleClick with the id we want.

import { useState } from "react";

const items = [
  { id: 1, name: "Apple" },
  { id: 2, name: "Banana" },
  { id: 3, name: "Cherry" },
];

function EventArguments() {
  const [selected, setSelected] = useState("");

  function handleSelect(id: number, name: string) {
    setSelected(`You picked #${id}: ${name}`);
  }

  return (
    <div>
      {items.map((item) => (
        <button
          key={item.id}
          className="ef-btn"
          onClick={() => handleSelect(item.id, item.name)}
        >
          {item.name}
        </button>
      ))}
      <p>{selected || "Pick a fruit above"}</p>
    </div>
  );
}

export default EventArguments;

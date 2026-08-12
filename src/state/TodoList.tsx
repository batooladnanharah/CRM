// Lesson 8: Todo List
// State holds an array of todos. Adding/removing creates a NEW array
// instead of mutating the old one, which is required for React to detect the change.

import { useState } from "react";

function TodoList() {
  const [todos, setTodos] = useState<string[]>(["Learn React", "Learn TypeScript"]);
  const [newTodo, setNewTodo] = useState("");

  function addTodo() {
    if (newTodo.trim() === "") return;
    setTodos([...todos, newTodo]);
    setNewTodo("");
  }

  function removeTodo(index: number) {
    setTodos(todos.filter((_, i) => i !== index));
  }

  return (
    <div>
      <input
        className="state-input"
        type="text"
        placeholder="New todo"
        value={newTodo}
        onChange={(e) => setNewTodo(e.target.value)}
      />
      <button className="state-btn" onClick={addTodo}>
        Add
      </button>
      <ul className="state-list">
        {todos.map((todo, index) => (
          <li key={index}>
            {todo}
            <button className="state-btn" onClick={() => removeTodo(index)}>
              Remove
            </button>
          </li>
        ))}
      </ul>
    </div>
  );
}

export default TodoList;

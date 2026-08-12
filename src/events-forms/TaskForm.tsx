// Mini Task Manager - TaskForm
// Receives an onAddTask callback via props (passing functions through
// props is how child components trigger updates in a parent's state).

import { useState } from "react";

type TaskFormProps = {
  onAddTask: (title: string) => void;
};

function TaskForm({ onAddTask }: TaskFormProps) {
  const [title, setTitle] = useState("");

  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    if (!title.trim()) return;
    onAddTask(title.trim());
    setTitle("");
  }

  return (
    <form onSubmit={handleSubmit}>
      <input
        className="ef-input"
        placeholder="New task..."
        value={title}
        onChange={(e) => setTitle(e.target.value)}
      />
      <button className="ef-btn" type="submit">
        Add Task
      </button>
    </form>
  );
}

export default TaskForm;

// Mini Task Manager - TaskCard
// Displays one task and reports user actions upward via onToggle/onDelete
// callback props, rather than modifying state itself.

import type { Task } from "./TaskList";

type TaskCardProps = {
  task: Task;
  onToggle: (id: number) => void;
  onDelete: (id: number) => void;
};

function TaskCard({ task, onToggle, onDelete }: TaskCardProps) {
  return (
    <li className={`task-card ${task.completed ? "completed" : ""}`}>
      <div className="task-card-left">
        <input
          type="checkbox"
          checked={task.completed}
          onChange={() => onToggle(task.id)}
        />
        <span className="task-title">{task.title}</span>
      </div>
      <button className="ef-btn danger" onClick={() => onDelete(task.id)}>
        Delete
      </button>
    </li>
  );
}

export default TaskCard;
